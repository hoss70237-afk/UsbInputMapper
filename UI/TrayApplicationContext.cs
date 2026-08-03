using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

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

        private RadialMenuHudForm _activeRadialHud = null;
        private ActionDef _activeRadialAction = null;

        public TrayApplicationContext()
        {
            Instance = this;

            try { Thread.CurrentThread.Priority = ThreadPriority.Highest; } catch { }

            try
            {
                _profileManager = new ProfileManager();
                _profileManager.Load();

                _diManager = new DirectInputManager();
                _rawManager = new RawInputManager();
                _hookManager = new GlobalHookManager();

                _vigem = new ViGEmOutput();
                _vigem.Initialize();
                _dispatcher = new OutputDispatcher(_vigem);

                _mainForm = new MainForm(_profileManager, _diManager);
                IntPtr forceHandleCreation = _mainForm.Handle;

                _appWatcher = new ForegroundAppWatcher();
                _appWatcher.OnForegroundAppChanged += (s, appPath) => { _profileManager.SwitchToAppProfile(appPath); };
                _appWatcher.Start();

                _rawManager.OnInputEvent += (s, e) => RouteToCaptureOrProcess(e);
                _diManager.OnInputEvent += (s, e) => RouteToCaptureOrProcess(new InputEvent { 
                    Type = e.Type, Code = e.Code, Value = e.Value, IsDown = e.IsDown, DeviceIdentifier = e.DeviceIdentifier 
                });
                
                _hookManager.OnBlockedInputFired += (s, e) => RouteToCaptureOrProcess(new InputEvent { 
                    Type = e.Type, Code = e.Code, IsDown = e.IsDown, X = e.X, Y = e.Y, Timestamp = e.Timestamp 
                });

                BezelWindowManager.Instance.OnBezelFired += (s, code) => {
                    long ts = (long)GetTickCount64();
                    RouteToCaptureOrProcess(new InputEvent { Type = 5, Code = code, IsDown = true, Timestamp = ts });
                    RouteToCaptureOrProcess(new InputEvent { Type = 5, Code = code, IsDown = false, Timestamp = ts + 1 });
                };

                _profileManager.OnProfileChanged += ProfileManager_OnProfileChanged;

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

                _profileManager.NotifyProfileSwitchedManually();
            }
            catch (Exception ex)
            {
                InputLogger.LogError("Initialization Failed", ex);
                MessageBox.Show("起動に失敗しました。\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitApplication();
            }
        }

        private void InvokeOnUI(Action action)
        {
            if (_mainForm == null || _mainForm.IsDisposed) return;
            if (!_mainForm.IsHandleCreated) { IntPtr h = _mainForm.Handle; }
            if (_mainForm.InvokeRequired) _mainForm.BeginInvoke(action);
            else action();
        }

        private void RouteToCaptureOrProcess(InputEvent e)
        {
            if (CaptureForm.IsCapturing && CaptureForm.CurrentInstance != null)
            {
                CaptureForm.CurrentInstance.ProcessInput(e);
                return;
            }

            if (_mainForm != null && !_mainForm.IsDisposed && _mainForm.Visible)
            {
                _mainForm.HighlightBinding(e.Type, e.Code, e.IsDown);
            }

            ProcessInput(e);
        }

        private void ProfileManager_OnProfileChanged(object sender, EventArgs e)
        {
            OutputDispatcher.Instance?.ReleaseAllInputs();

            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;

            BezelWindowManager.Instance.UpdateBezelWindows(profile);

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
            bool needMouseHook = false;

            foreach (var b in profile.Bindings)
            {
                if (b.InputType == 0) needMouseHook = true;

                if (b.BlockOriginalInput)
                {
                    if (b.InputType == 0 || b.InputType == 1)
                    {
                        long key = ((long)b.InputType << 32) | (uint)b.InputCode;
                        blockList.Add(key);
                    }
                }
            }
            
            _hookManager.SetBlockList(blockList, needMouseHook);

            if (profile.OverlayShowMark || profile.OverlayShowName)
            {
                Task.Run(() => {
                    try { using (var overlay = new ProfileOverlayForm(profile)) { Application.Run(overlay); } }
                    catch { }
                });
            }
        }

        private void ProcessInput(InputEvent e)
        {
            if (_hookManager.IsRecording || _hookManager.IsCoordinateCapturing) return;

            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;

            if (profile.EnableXInput && (e.Type == 10 || e.Type == 11 || e.Type == 12))
            {
                foreach (var b in _profileManager.ControllerBaseBindings)
                {
                    if (b.InputType == e.Type && b.InputCode == e.Code)
                    {
                        if (e.Type == 11) OutputDispatcher.Instance?.DispatchAnalog(b.Action, e.Value, b);
                        else OutputDispatcher.Instance?.Dispatch(b.Action, e.IsDown);
                    }
                }
            }

            if (_activeRadialHud != null && _activeRadialAction != null)
            {
                if (_activeRadialAction.RadialMenuMode == 1)
                {
                    if (e.IsDown && _activeRadialAction.RadialMenuConfirmKeys != null)
                    {
                        bool match = false;
                        var keys = _activeRadialAction.RadialMenuConfirmKeys;
                        int count = keys.Count;
                        for (int i = 0; i < count; i++)
                        {
                            if (keys[i].Type == e.Type && keys[i].Code == e.Code)
                            {
                                match = true;
                                break;
                            }
                        }

                        if (match)
                        {
                            ExecuteAndCloseRadialHudUI();
                            return; 
                        }
                    }
                }
            }

            foreach (var b in profile.Bindings)
            {
                if (b.RequiredLayer != 0 && b.RequiredLayer != LayerManager.CurrentLayer) continue;

                if (b.InputType == e.Type && b.InputCode == e.Code)
                {
                    if (b.SubTriggers != null && b.SubTriggers.Count > 0)
                    {
                        bool modsPressed = true;
                        foreach (var mod in b.SubTriggers)
                        {
                            if (!_hookManager.IsKeyPressed(mod.Type, mod.Code))
                            {
                                modsPressed = false;
                                break;
                            }
                        }
                        if (!modsPressed) continue;
                    }

                    if (b.Action.ActionType == ActionType.RadialMenu)
                    {
                        if (b.Action.RadialMenuMode == 0)
                        {
                            if (e.IsDown) { if (_activeRadialHud == null) ShowRadialHudUI(b.Action); }
                            else ExecuteAndCloseRadialHudUI();
                        }
                        else
                        {
                            if (e.IsDown)
                            {
                                if (_activeRadialHud == null) ShowRadialHudUI(b.Action);
                                else CloseRadialHudUI();
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
            InvokeOnUI(() => {
                if (_activeRadialHud != null) return;
                _activeRadialAction = action;
                _activeRadialHud = new RadialMenuHudForm(action);
                _activeRadialHud.Show();
            });
        }

        private void ExecuteAndCloseRadialHudUI()
        {
            InvokeOnUI(() => {
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
            });
        }

        private void CloseRadialHudUI()
        {
            InvokeOnUI(() => {
                if (_activeRadialHud != null)
                {
                    _activeRadialHud.Close();
                    _activeRadialHud.Dispose();
                    _activeRadialHud = null;
                }
                _activeRadialAction = null;
            });
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
