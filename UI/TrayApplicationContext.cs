using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        public static TrayApplicationContext Instance { get; private set; }

        private ProfileManager _profileManager;
        private DirectInputManager _diManager;
        private RawInputManager _rawManager;
        private GlobalHookManager _hookManager;
        private ForegroundAppWatcher _appWatcher;
        private ViGEmOutput _vigem;
        private OutputDispatcher _dispatcher;
        private MainForm _mainForm;

        // ラジアルメニューHUD管理
        private RadialMenuHudForm _activeRadialHud = null;
        private ActionDef _activeRadialAction = null;

        public TrayApplicationContext()
        {
            Instance = this;

            try
            {
                // 1. コアコンポーネントの初期化
                _profileManager = new ProfileManager();
                _profileManager.Load();

                _diManager = new DirectInputManager();
                _rawManager = new RawInputManager();
                _hookManager = new GlobalHookManager();

                _vigem = new ViGEmOutput();
                _vigem.Initialize();
                _dispatcher = new OutputDispatcher(_vigem);

                _mainForm = new MainForm(_profileManager, _diManager);

                // 2. アクティブウィンドウ監視の開始（自動プロファイル切替）
                _appWatcher = new ForegroundAppWatcher();
                _appWatcher.OnForegroundAppChanged += (s, appPath) => {
                    _profileManager.SwitchToAppProfile(appPath);
                };
                _appWatcher.Start();

                // 3. イベントルーティングの設定
                _rawManager.OnInputEvent += (s, e) => RouteToCaptureOrProcess(e);
                _diManager.OnInputEvent += (s, e) => RouteToCaptureOrProcess(new InputEvent { 
                    Type = e.Type, Code = e.Code, Value = e.Value, IsDown = e.IsDown, DeviceIdentifier = e.DeviceIdentifier 
                });
                
                _hookManager.OnBlockedInputFired += (s, e) => RouteToCaptureOrProcess(new InputEvent { 
                    Type = e.Type, Code = e.Code, IsDown = e.IsDown, X = e.X, Y = e.Y, Timestamp = e.Timestamp 
                });

                _profileManager.OnProfileChanged += ProfileManager_OnProfileChanged;

                // 4. タスクトレイアイコンの設定
                var menu = new ContextMenuStrip();
                var mnuOpen = new ToolStripMenuItem("設定を開く");
                mnuOpen.Click += (s, e) => ShowMainForm();
                
                var mnuPanic = new ToolStripMenuItem("緊急停止 (パニックボタン)");
                mnuPanic.Click += (s, e) => TriggerPanic();
                
                var mnuExit = new ToolStripMenuItem("終了");
                mnuExit.Click += (s, e) => ExitApplication();

                menu.Items.Add(mnuOpen);
                menu.Items.Add(mnuPanic);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(mnuExit);

                _trayIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Application,
                    ContextMenuStrip = menu,
                    Text = "UsbInputMapper",
                    Visible = true
                };

                _trayIcon.DoubleClick += (s, e) => ShowMainForm();

                // 初期プロファイル適用
                _profileManager.NotifyProfileSwitchedManually();
            }
            catch (Exception ex)
            {
                InputLogger.LogError("Initialization Failed", ex);
                MessageBox.Show("起動に失敗しました。\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitApplication();
            }
        }

        private void RouteToCaptureOrProcess(InputEvent e)
        {
            if (CaptureForm.IsCapturing && CaptureForm.CurrentInstance != null)
            {
                CaptureForm.CurrentInstance.ProcessInput(e);
                return;
            }

            ProcessInput(e);
        }

        private void ProfileManager_OnProfileChanged(object sender, EventArgs e)
        {
            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;

            if (profile.OverrideGlobalChattering)
            {
                _hookManager.EnableChatteringCanceler = profile.EnableChatteringCanceler;
                _hookManager.ChatteringThresholdMs = profile.ChatteringThresholdMs;
            }
            else
            {
                _hookManager.EnableChatteringCanceler = _profileManager.GlobalConfig.EnableChatteringCanceler;
                _hookManager.ChatteringThresholdMs = _profileManager.GlobalConfig.ChatteringThresholdMs;
            }

            var blockList = new HashSet<long>();
            foreach (var b in profile.Bindings.Where(x => x.BlockOriginalInput))
            {
                if (b.InputType == 0 || b.InputType == 1 || b.InputType == 5)
                {
                    long key = ((long)b.InputType << 32) | (uint)b.InputCode;
                    blockList.Add(key);
                }
            }
            _hookManager.SetBlockList(blockList);

            if (profile.OverlayShowMark || profile.OverlayShowName)
            {
                Task.Run(() => {
                    try
                    {
                        using (var overlay = new ProfileOverlayForm(profile))
                        {
                            Application.Run(overlay);
                        }
                    }
                    catch { }
                });
            }
        }

        private void ProcessInput(InputEvent e)
        {
            if (_hookManager.IsRecording || _hookManager.IsCoordinateCapturing) return;

            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;

            // コントローラーベース設定
            if (profile.EnableXInput && (e.Type == 10 || e.Type == 11 || e.Type == 12))
            {
                foreach (var b in _profileManager.ControllerBaseBindings)
                {
                    if (b.InputType == e.Type && b.InputCode == e.Code)
                    {
                        if (e.Type == 11) // Axis
                        {
                            bool isPositive = e.Value > 32767;
                            if (b.AxisRange == 1 && !isPositive) continue;
                            if (b.AxisRange == 2 && isPositive) continue;

                            int val = e.Value - 32767;
                            if (Math.Abs(val) > (32767 * b.DeadZone / 100)) OutputDispatcher.Instance?.Dispatch(b.Action, true);
                            else OutputDispatcher.Instance?.Dispatch(b.Action, false);
                        }
                        else
                        {
                            OutputDispatcher.Instance?.Dispatch(b.Action, e.IsDown);
                        }
                    }
                }
            }

            // プロファイル固有バインディング
            foreach (var b in profile.Bindings)
            {
                if (b.RequiredLayer != 0 && b.RequiredLayer != LayerManager.CurrentLayer) continue;

                if (b.InputType == e.Type && b.InputCode == e.Code)
                {
                    // ★ ラジアルメニュー発火処理（オートリピート保護＆モード分岐）
                    if (b.Action.ActionType == ActionType.RadialMenu)
                    {
                        int mode = b.Action.RadialMenuMode; // 0: 離して確定, 1: クリック確定

                        if (mode == 0) // 離して確定モード
                        {
                            if (e.IsDown)
                            {
                                // ★ 表示中（長押し中）の場合はキーリピートイベントを無視する
                                if (_activeRadialHud == null)
                                {
                                    ShowRadialHudUI(b.Action);
                                }
                            }
                            else
                            {
                                ExecuteAndCloseRadialHudUI();
                            }
                        }
                        else // クリック確定モード
                        {
                            if (e.IsDown)
                            {
                                if (_activeRadialHud == null)
                                {
                                    ShowRadialHudUI(b.Action);
                                    _hookManager.IsRadialMenuClickCapturing = true;
                                    _hookManager.OnRadialMenuClickCaptured = () => {
                                        ExecuteAndCloseRadialHudUI();
                                    };
                                }
                                else
                                {
                                    CloseRadialHudUI();
                                }
                            }
                        }
                        continue;
                    }

                    if (b.Condition == TriggerCondition.Normal)
                    {
                        OutputDispatcher.Instance?.Dispatch(b.Action, e.IsDown);
                    }
                    else if (b.Condition == TriggerCondition.Release && !e.IsDown)
                    {
                        OutputDispatcher.Instance?.Dispatch(b.Action, true); 
                    }
                }
            }
        }

        private void ShowRadialHudUI(ActionDef action)
        {
            if (_mainForm == null || _mainForm.IsDisposed) return;

            _mainForm.BeginInvoke(new Action(() => {
                // 重複表示・連続リリフレッシュのガード
                if (_activeRadialHud != null) return;

                _activeRadialAction = action;
                _activeRadialHud = new RadialMenuHudForm(action);
                _activeRadialHud.Show();
            }));
        }

        private void ExecuteAndCloseRadialHudUI()
        {
            if (_mainForm == null || _mainForm.IsDisposed) return;

            _mainForm.BeginInvoke(new Action(() => {
                if (_activeRadialHud != null && _activeRadialAction != null)
                {
                    int selectedIdx = _activeRadialHud.SelectedDirectionIndex;
                    _activeRadialHud.Close();
                    _activeRadialHud.Dispose();
                    _activeRadialHud = null;

                    if (selectedIdx >= 0 && selectedIdx < _activeRadialAction.RadialMenuDirections.Count)
                    {
                        var dirAction = _activeRadialAction.RadialMenuDirections[selectedIdx].Action;
                        if (dirAction != null && dirAction.ActionType != ActionType.None)
                        {
                            OutputDispatcher.Instance?.Dispatch(dirAction, true);
                            OutputDispatcher.Instance?.Dispatch(dirAction, false);
                        }
                    }
                    _activeRadialAction = null;
                }
                _hookManager.IsRadialMenuClickCapturing = false;
            }));
        }

        private void CloseRadialHudUI()
        {
            if (_mainForm == null || _mainForm.IsDisposed) return;

            _mainForm.BeginInvoke(new Action(() => {
                if (_activeRadialHud != null)
                {
                    _activeRadialHud.Close();
                    _activeRadialHud.Dispose();
                    _activeRadialHud = null;
                }
                _activeRadialAction = null;
                _hookManager.IsRadialMenuClickCapturing = false;
            }));
        }

        public void ShowMainForm()
        {
            if (_mainForm != null)
            {
                _mainForm.Show();
                if (_mainForm.WindowState == FormWindowState.Minimized) _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
        }

        private void TriggerPanic()
        {
            OutputDispatcher.Instance?.ReleaseAllInputs();
            _trayIcon?.ShowBalloonTip(2000, "緊急停止", "すべての仮想入力をリセットし、キーを解放しました。", ToolTipIcon.Warning);
            InputLogger.Log("Panic Button Triggered by User.");
        }

        private void ExitApplication()
        {
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
            
            OutputDispatcher.Instance?.ReleaseAllInputs(); 
            HidHideManager.EnableHiding(false); 
            
            _appWatcher?.Dispose();
            _hookManager?.Dispose();
            _diManager?.Dispose();
            _rawManager?.Dispose();
            _vigem?.Dispose();
            
            Application.Exit();
        }
    }
}
