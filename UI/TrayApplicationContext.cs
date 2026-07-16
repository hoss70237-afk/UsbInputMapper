using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace UsbInputMapper.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private MainForm _mainForm;

        private RawInputManager _rawInputManager;
        private DirectInputManager _diManager;
        private ViGEmOutput _viGEmOutput;
        private OutputDispatcher _dispatcher;
        private ProfileManager _profileManager;
        private ForegroundAppWatcher _appWatcher;
        private GlobalHookManager _globalHookManager;

        // 連打・ホールド・同時押しの状態管理
        private ConcurrentDictionary<string, bool> _physicalKeysDown = new ConcurrentDictionary<string, bool>();
        private ConcurrentDictionary<string, CancellationTokenSource> _activeLoops = new ConcurrentDictionary<string, CancellationTokenSource>();
        private ConcurrentDictionary<string, bool> _toggleStates = new ConcurrentDictionary<string, bool>();
        private ConcurrentDictionary<string, Tuple<int, long>> _macroStepStates = new ConcurrentDictionary<string, Tuple<int, long>>();

        public TrayApplicationContext()
        {
            InitializeCore();
            InitializeTrayIcon();
        }

        private void InitializeCore()
        {
            _profileManager = new ProfileManager();
            _profileManager.Load();
            _profileManager.OnProfileChanged += (s, e) => UpdateHookBlockList();

            _appWatcher = new ForegroundAppWatcher();
            _appWatcher.OnForegroundAppChanged += (s, appPath) => _profileManager.SwitchToAppProfile(appPath);
            _appWatcher.Start();

            _viGEmOutput = new ViGEmOutput();
            _viGEmOutput.Initialize();
            _dispatcher = new OutputDispatcher(_viGEmOutput);

            _globalHookManager = new GlobalHookManager();
            UpdateHookBlockList();

            _rawInputManager = new RawInputManager();
            _rawInputManager.OnInputEvent += RawInputManager_OnInputEvent;

            _diManager = new DirectInputManager();
            _diManager.OnInputEvent += DiManager_OnInputEvent;
        }

        private void UpdateHookBlockList()
        {
            var blockList = new HashSet<string>();
            var profile = _profileManager.CurrentActiveProfile;
            if (profile != null)
            {
                foreach (var b in profile.Bindings)
                {
                    if (b.BlockOriginalInput) blockList.Add($"{b.InputType}_{b.InputCode}");
                }
            }
            _globalHookManager.SetBlockList(blockList);
        }

        // --- RawInput (Keyboard/Mouse) 処理の完全復元 ---
        private void RawInputManager_OnInputEvent(object sender, InputEvent e)
        {
            if (CaptureForm.IsCapturing)
            {
                CaptureForm.CurrentInstance?.ProcessInput(e);
                return;
            }

            int inputCode = (e.Type == 1) ? e.VKey : (int)e.MouseButtonFlags;
            string keyId = $"{e.Type}_{inputCode}";

            if (e.IsKeyDown) _physicalKeysDown[keyId] = true;
            else _physicalKeysDown.TryRemove(keyId, out _);

            var bindings = FindAllMatchingBindings(e.DeviceIdentifier, e.Type, inputCode);
            if (bindings.Count == 0)
            {
                // ★対象外デバイスの入力がブロックされていた場合、それを実入力として再送（パススルー）
                if (_globalHookManager.WasRecentlyBlocked(e.Type, inputCode))
                {
                    if (e.Type == 1) _dispatcher.SendKeyboardInputs(new List<int> { inputCode }, e.IsKeyDown);
                    else if (e.Type == 0) _dispatcher.SendMouseClick(inputCode, e.IsKeyDown);
                }
                return;
            }

            foreach (var binding in bindings)
            {
                // XInput出力がOFFのプロファイルでは、Xboxコントローラー系の出力をスルーする
                var profile = _profileManager.CurrentActiveProfile;
                if (profile != null && !profile.EnableXInput &&
                   (binding.Action.ActionType == ActionType.XboxController || binding.Action.ActionType == ActionType.XboxAxis || binding.Action.ActionType == ActionType.XboxTrigger))
                {
                    continue; 
                }

                ProcessBindingExecution(binding, e.DeviceIdentifier, e.Type, inputCode, e.IsKeyDown);
            }
        }

        // --- DirectInput (Gamepad) 処理 ---
        private void DiManager_OnInputEvent(object sender, DirectInputEvent e)
        {
            if (CaptureForm.IsCapturing) return; // CaptureFormはRawInput用なのでスルー

            string keyId = $"{e.Type}_{e.Code}";

            // ボタンの場合は同時押し判定用に状態を記録
            if (e.Type == 10) 
            {
                if (e.IsDown) _physicalKeysDown[keyId] = true;
                else _physicalKeysDown.TryRemove(keyId, out _);
            }

            var bindings = FindAllMatchingBindings(e.DeviceIdentifier, e.Type, e.Code);

            foreach (var binding in bindings)
            {
                var profile = _profileManager.CurrentActiveProfile;
                if (profile != null && !profile.EnableXInput &&
                   (binding.Action.ActionType == ActionType.XboxController || binding.Action.ActionType == ActionType.XboxAxis || binding.Action.ActionType == ActionType.XboxTrigger))
                {
                    continue; 
                }

                if (e.Type == 11) // アナログ軸
                {
                    ProcessAnalogAxis(binding, e.Value);
                }
                else if (e.Type == 10) // ボタン
                {
                    ProcessBindingExecution(binding, e.DeviceIdentifier, e.Type, e.Code, e.IsDown);
                }
            }
        }

        // --- 共通バインディング検索 ---
        private List<UsbInputMapper.Profiles.Binding> FindAllMatchingBindings(string deviceId, int inputType, int inputCode)
        {
            if (_profileManager.CurrentActiveProfile == null) return new List<UsbInputMapper.Profiles.Binding>();
            
            var bindings = _profileManager.CurrentActiveProfile.Bindings
                .Where(b => b.DeviceIdentifier == deviceId && b.InputType == inputType && b.InputCode == inputCode)
                .ToList();

            if (bindings.Count == 0) return bindings;

            return bindings.Where(b => 
                b.SubTriggers == null || 
                b.SubTriggers.All(st => _physicalKeysDown.ContainsKey($"{st.Type}_{st.Code}"))
            ).ToList();
        }

        // --- ホールド・連打・マクロ等の制御ロジック (完全復元) ---
        private void ProcessBindingExecution(UsbInputMapper.Profiles.Binding binding, string deviceId, int type, int inputCode, bool isDown)
        {
            string loopKey = $"{deviceId}_{type}_{inputCode}_{binding.GetHashCode()}";

            // マウスホイールの特殊処理
            if (type == 0 && (inputCode == 4 || inputCode == 5))
            {
                if (isDown)
                {
                    ExecuteAction(binding.Action, true, CancellationToken.None, loopKey);
                    ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                }
                return;
            }

            if (isDown)
            {
                if (binding.Condition == TriggerCondition.Release) return;
                if (_activeLoops.ContainsKey(loopKey)) return;

                var cts = new CancellationTokenSource();
                _activeLoops[loopKey] = cts;

                Task.Run(async () =>
                {
                    try
                    {
                        if (binding.Condition == TriggerCondition.Hold)
                        {
                            await Task.Delay(binding.ConditionParam, cts.Token);
                            ExecuteAction(binding.Action, true, cts.Token, loopKey);
                        }
                        else if (binding.Condition == TriggerCondition.RapidFire)
                        {
                            while (!cts.Token.IsCancellationRequested)
                            {
                                ExecuteAction(binding.Action, true, cts.Token, loopKey);
                                await Task.Delay(20);
                                ExecuteAction(binding.Action, false, cts.Token, loopKey);
                                await Task.Delay(Math.Max(10, binding.ConditionParam), cts.Token);
                            }
                        }
                        else
                        {
                            if (binding.Action.ActionType == ActionType.ToggleHold)
                            {
                                bool currentState = _toggleStates.GetOrAdd(loopKey, false);
                                _toggleStates[loopKey] = !currentState;
                                _dispatcher.Dispatch(binding.Action, !currentState);
                            }
                            else
                            {
                                ExecuteAction(binding.Action, true, cts.Token, loopKey);
                            }
                        }
                    }
                    catch (TaskCanceledException) { }
                }, cts.Token);
            }
            else
            {
                if (_activeLoops.TryRemove(loopKey, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                if (binding.Condition == TriggerCondition.Release)
                {
                    ExecuteAction(binding.Action, true, CancellationToken.None, loopKey);
                    Thread.Sleep(20);
                    ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                }
                else if (binding.Action.ActionType != ActionType.ToggleHold)
                {
                    ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                }
            }
        }

        // --- アナログ入力の計算処理 ---
        private void ProcessAnalogAxis(UsbInputMapper.Profiles.Binding binding, int rawValue)
        {
            double normalized = (rawValue - 32767.5) / 32767.5;
            if (binding.InvertAxis) normalized *= -1;

            double deadZone = binding.DeadZone / 100.0;
            if (Math.Abs(normalized) < deadZone)
            {
                normalized = 0;
            }
            else
            {
                double sign = Math.Sign(normalized);
                normalized = sign * ((Math.Abs(normalized) - deadZone) / (1.0 - deadZone));
            }

            if (binding.Action.ActionType == ActionType.XboxAxis)
            {
                short outValue = (short)(normalized * 32767);
                Xbox360Axis axis = Xbox360Axis.LeftThumbX;
                switch(binding.Action.ArgumentNum)
                {
                    case 1: axis = Xbox360Axis.LeftThumbX; break;
                    case 2: axis = Xbox360Axis.LeftThumbY; outValue = (short)-outValue; break; 
                    case 3: axis = Xbox360Axis.RightThumbX; break;
                    case 4: axis = Xbox360Axis.RightThumbY; outValue = (short)-outValue; break;
                }
                _viGEmOutput.SetAxis(axis, outValue);
            }
            else if (binding.Action.ActionType == ActionType.XboxTrigger)
            {
                double trigNorm = (normalized + 1.0) / 2.0; 
                byte outValue = (byte)(trigNorm * 255);
                Xbox360Slider slider = binding.Action.ArgumentNum == 1 ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger;
                _viGEmOutput.SetSlider(slider, outValue);
            }
        }

        // --- 実行処理とマクロ再生 (完全復元) ---
        private void ExecuteAction(ActionDef action, bool isDown, CancellationToken token, string loopKey)
        {
            if (action.ActionType == ActionType.ProfileSwitch)
            {
                if (isDown)
                {
                    var targetProf = _profileManager.Profiles.FirstOrDefault(p => p.Name == action.ArgumentStr);
                    if (targetProf != null)
                    {
                        if (action.ArgumentNum == 0) // Toggle
                        {
                            _profileManager.TemporaryProfile = (_profileManager.TemporaryProfile == targetProf) ? null : targetProf;
                            _profileManager.NotifyProfileSwitchedManually();
                        }
                        else // Hold
                        {
                            _profileManager.TemporaryProfile = targetProf;
                            _profileManager.NotifyProfileSwitchedManually();
                        }
                    }
                }
                else
                {
                    if (action.ArgumentNum == 1) // Hold -> Release
                    {
                        _profileManager.TemporaryProfile = null;
                        _profileManager.NotifyProfileSwitchedManually();
                    }
                }
                return;
            }

            if (action.ActionType == ActionType.Macro && action.MacroSteps.Count > 0)
            {
                if (!isDown) return; 
                Task.Run(() => {
                    var steps = action.MacroSteps;
                    
                    if (action.PlaybackMode == MacroPlaybackMode.Sequence)
                    {
                        foreach (var step in steps) PlayMacroStep(step, action.PlaybackMode);
                    }
                    else if (action.PlaybackMode == MacroPlaybackMode.Hold)
                    {
                        foreach (var step in steps) { if (token.IsCancellationRequested) break; PlayMacroStep(step, action.PlaybackMode); }
                    }
                    else if (action.PlaybackMode == MacroPlaybackMode.Repeat)
                    {
                        while (!token.IsCancellationRequested)
                        {
                            foreach (var step in steps) { if (token.IsCancellationRequested) break; PlayMacroStep(step, action.PlaybackMode); }
                        }
                    }
                    else if (action.PlaybackMode == MacroPlaybackMode.StepByStep)
                    {
                        int currentIndex = 0; 
                        long now = Environment.TickCount;
                        if (_macroStepStates.TryGetValue(loopKey, out var state) && (now - state.Item2) < action.StepTimeoutMs)
                        {
                            currentIndex = (state.Item1 + 1) % steps.Count;
                        }
                        _macroStepStates[loopKey] = new Tuple<int, long>(currentIndex, now);
                        PlayMacroStep(steps[currentIndex], action.PlaybackMode);
                    }
                });
                return;
            }
            else if (action.ActionType == ActionType.MouseMoveContinuous)
            {
                if (isDown)
                {
                    Task.Run(async () => {
                        while (!token.IsCancellationRequested)
                        {
                            var tempDef = new ActionDef { ActionType = ActionType.MouseMoveRelative, MouseX = action.MouseX, MouseY = action.MouseY };
                            _dispatcher.Dispatch(tempDef, true);
                            await Task.Delay(16);
                        }
                    });
                }
                return;
            }
            
            _dispatcher.Dispatch(action, isDown);
        }

        private void PlayMacroStep(MacroStep step, MacroPlaybackMode currentMode)
        {
            Thread.Sleep(step.DelayMs);
            var tempDef = new ActionDef { 
                ActionType = step.ActionType, 
                ArgumentNum = step.ArgumentNum, 
                MultipleKeys = step.MultipleKeys, 
                ArgumentStr = step.ArgumentStr,
                MouseX = step.MouseX, 
                MouseY = step.MouseY
            };

            StepPressState state = step.PressState;
            if (currentMode != MacroPlaybackMode.StepByStep && (state == StepPressState.Down || state == StepPressState.Up))
            {
                state = StepPressState.Tap;
            }

            if (state == StepPressState.Down) _dispatcher.Dispatch(tempDef, true);
            else if (state == StepPressState.Up) _dispatcher.Dispatch(tempDef, false);
            else
            {
                _dispatcher.Dispatch(tempDef, true);
                Thread.Sleep(20); 
                _dispatcher.Dispatch(tempDef, false);
            }
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon { Icon = SystemIcons.Application, Text = "UsbInputMapper", Visible = true };
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("設定を開く", null, ShowMainForm);
            menu.Items.Add("終了", null, ExitApp);
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += ShowMainForm;
        }

        private void ShowMainForm(object sender, EventArgs e)
        {
            if (_mainForm == null || _mainForm.IsDisposed) _mainForm = new MainForm(_profileManager, _diManager);
            _mainForm.Show(); 
            _mainForm.Activate();
        }

        private void ExitApp(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _globalHookManager?.Dispose();
            _rawInputManager?.Dispose();
            _diManager?.Dispose();
            _viGEmOutput?.Dispose();
            Application.Exit();
        }
    }
}
