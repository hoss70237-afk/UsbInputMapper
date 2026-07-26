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
        private ForegroundAppWatcher _appWatcher; // ★ アクティブアプリ監視
        private ViGEmOutput _vigem;
        private OutputDispatcher _dispatcher;
        private MainForm _mainForm;

        // ラジアルメニューHUD管理
        private RadialMenuHudForm _activeRadialHud = null;

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

            // フックのチャタリングキャンセラー設定の同期
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

            // フックのブロックリストを更新
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

            // ★ OSDオーバーレイ表示の実行
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
            if (_hookManager.IsRecording || _hookManager.IsCoordinateCapturing || _hookManager.IsRadialMenuClickCapturing) return;

            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;

            // コントローラーベース設定 (XInput有効時)
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
                    if (b.Action.ActionType == ActionType.RadialMenu)
                    {
                        if (e.IsDown)
                        {
                            if (_activeRadialHud == null)
                            {
                                _activeRadialHud = new RadialMenuHudForm(b.Action);
                                _activeRadialHud.Show();
                            }
                        }
                        else
                        {
                            if (_activeRadialHud != null)
                            {
                                int selectedIdx = _activeRadialHud.SelectedDirectionIndex;
                                _activeRadialHud.Close();
                                _activeRadialHud.Dispose();
                                _activeRadialHud = null;

                                if (selectedIdx >= 0 && selectedIdx < b.Action.RadialMenuDirections.Count)
                                {
                                    var dirAction = b.Action.RadialMenuDirections[selectedIdx].Action;
                                    if (dirAction != null && dirAction.ActionType != ActionType.None)
                                    {
                                        OutputDispatcher.Instance?.Dispatch(dirAction, true);
                                        OutputDispatcher.Instance?.Dispatch(dirAction, false);
                                    }
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
