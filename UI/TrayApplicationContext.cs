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

        private SynchronizationContext _syncContext;

        private struct TriggerKeyHash : IEquatable<TriggerKeyHash>
        {
            public int Type; public int Code;
            public TriggerKeyHash(int type, int code) { Type = type; Code = code; }
            public bool Equals(TriggerKeyHash other) => Type == other.Type && Code == other.Code;
            public override int GetHashCode() => (Type * 397) ^ Code;
        }
        private struct InputKey : IEquatable<InputKey>
        {
            public string DeviceIdentifier; public int Type; public int Code;
            public InputKey(string deviceIdentifier, int type, int code) { DeviceIdentifier = deviceIdentifier; Type = type; Code = code; }
            public bool Equals(InputKey other) => Type == other.Type && Code == other.Code && string.Equals(DeviceIdentifier, other.DeviceIdentifier, StringComparison.Ordinal);
            public override int GetHashCode() => (((DeviceIdentifier != null ? DeviceIdentifier.GetHashCode() : 0) * 397) ^ Type) * 397 ^ Code;
        }
        private struct PovKey : IEquatable<PovKey>
        {
            public string DeviceIdentifier; public int Code;
            public PovKey(string deviceIdentifier, int code) { DeviceIdentifier = deviceIdentifier; Code = code; }
            public bool Equals(PovKey other) => Code == other.Code && string.Equals(DeviceIdentifier, other.DeviceIdentifier, StringComparison.Ordinal);
            public override int GetHashCode() => ((DeviceIdentifier != null ? DeviceIdentifier.GetHashCode() : 0) * 397) ^ Code;
        }
        private struct LoopKey : IEquatable<LoopKey>
        {
            public string DeviceId; public int Type; public int Code; public int BindingHash;
            public LoopKey(string deviceId, int type, int code, int bindingHash) { DeviceId = deviceId; Type = type; Code = code; BindingHash = bindingHash; }
            public bool Equals(LoopKey other) => Type == other.Type && Code == other.Code && BindingHash == other.BindingHash && string.Equals(DeviceId, other.DeviceId, StringComparison.Ordinal);
            public override int GetHashCode() => (((((DeviceId != null ? DeviceId.GetHashCode() : 0) * 397) ^ Type) * 397) ^ Code) * 397 ^ BindingHash;
        }

        private ConcurrentDictionary<TriggerKeyHash, bool> _physicalKeysDown = new ConcurrentDictionary<TriggerKeyHash, bool>();
        private ConcurrentDictionary<LoopKey, CancellationTokenSource> _activeLoops = new ConcurrentDictionary<LoopKey, CancellationTokenSource>();
        private Dictionary<PovKey, int> _lastPovStates = new Dictionary<PovKey, int>();
        private Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>> _bindingCache = new Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>>();

        private System.Windows.Forms.Timer _loopTimer;
        private volatile int _stickMouseDx = 0;
        private volatile int _stickMouseDy = 0;
        private int _currentBezelCode = -1;
        private int _bezelHoverTime = 0;

        private RadialMenuHudForm _radialMenuHudForm;
        private ActionDef _currentRadialMenuDef;

        private long _lastStandardInputTime = 0;
        private List<InputEvent> _pendingHidEvents = new List<InputEvent>();

        public TrayApplicationContext() 
        {
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            InitializeCore(); 
            InitializeTrayIcon(); 
        }

        private void InitializeCore()
        {
            _profileManager = new ProfileManager(); _profileManager.Load();
            _profileManager.OnProfileChanged += (s, e) => {
                UpdateHookBlockList(); UpdateBindingCache();
                _dispatcher?.ReleaseAllInputs(); 
                var p = _profileManager.CurrentActiveProfile;
                if (p != null && p.NotifyProfileChangeVibration) VibrationManager.Vibrate(300, 2); 
            };
            _profileManager.OnSettingsChanged += (s, e) => { UpdateHookBlockList(); UpdateBindingCache(); };

            _appWatcher = new ForegroundAppWatcher(); _appWatcher.OnForegroundAppChanged += (s, appPath) => _profileManager.SwitchToAppProfile(appPath); _appWatcher.Start();

            _viGEmOutput = new ViGEmOutput(); _viGEmOutput.Initialize();
            _dispatcher = new OutputDispatcher(_viGEmOutput);
            _globalHookManager = new GlobalHookManager(); UpdateHookBlockList();
            
            _rawInputManager = new RawInputManager(); 
            _rawInputManager.OnInputEvent += RawInputManager_OnInputEvent;
            _rawInputManager.OnDeviceChanged += (s, e) => { _diManager?.RefreshDevices(); UpdateBindingCache(); }; 

            _diManager = new DirectInputManager(); _diManager.OnInputEvent += DiManager_OnInputEvent;

            UpdateBindingCache();

            _loopTimer = new System.Windows.Forms.Timer { Interval = 10 };
            _loopTimer.Tick += LoopTimer_Tick;
            _loopTimer.Start();
        }

        private void LoopTimer_Tick(object sender, EventArgs e)
        {
            if (_stickMouseDx != 0 || _stickMouseDy != 0) _dispatcher.SendMouseMove(_stickMouseDx, _stickMouseDy, false, false);

            if (SendInputNative.GetCursorPos(out var pt))
            {
                int sW = Screen.PrimaryScreen.Bounds.Width; int sH = Screen.PrimaryScreen.Bounds.Height;
                int x = pt.X; int y = pt.Y;
                int code = -1;

                if (x <= 3 && y <= 3) code = 0;
                else if (x >= sW - 4 && y <= 3) code = 4;
                else if (x >= sW - 4 && y >= sH - 4) code = 8;
                else if (x <= 3 && y >= sH - 4) code = 12;
                else if (y <= 0) { if (x < sW / 3) code = 1; else if (x < sW * 2 / 3) code = 2; else code = 3; }
                else if (x >= sW - 1) { if (y < sH / 3) code = 5; else if (y < sH * 2 / 3) code = 6; else code = 7; }
                else if (y >= sH - 1) { if (x >= sW * 2 / 3) code = 9; else if (x >= sW / 3) code = 10; else code = 11; }
                else if (x <= 0) { if (y >= sH * 2 / 3) code = 13; else if (y >= sH / 3) code = 14; else code = 15; }

                if (code != -1)
                {
                    if (_currentBezelCode == code) { _bezelHoverTime += _loopTimer.Interval; } else { _currentBezelCode = code; _bezelHoverTime = 0; }
                    if (_bindingCache.TryGetValue(new InputKey("Any", 5, code), out var bindings))
                    {
                        foreach (var b in bindings) {
                            if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))) {
                                if (_bezelHoverTime >= b.ConditionParam && _bezelHoverTime - _loopTimer.Interval < b.ConditionParam) { ExecuteAction(b.Action, true); ExecuteAction(b.Action, false); }
                            }
                        }
                    }
                }
                else { _currentBezelCode = -1; _bezelHoverTime = 0; }
            }
        }

        private void UpdateHookBlockList()
        {
            var blockList = new HashSet<long>();
            var profile = _profileManager.CurrentActiveProfile;
            if (profile != null) { foreach (var b in profile.Bindings) if (b.BlockOriginalInput) blockList.Add(((long)b.InputType << 32) | (uint)b.InputCode); }
            _globalHookManager.SetBlockList(blockList);
        }

        private void UpdateBindingCache()
        {
            _bindingCache.Clear();
            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;
            foreach (var b in profile.Bindings) { var key = new InputKey(b.DeviceIdentifier, b.InputType, b.InputCode); if (!_bindingCache.TryGetValue(key, out var list)) { list = new List<UsbInputMapper.Profiles.Binding>(); _bindingCache[key] = list; } list.Add(b); }
            if (profile.EnableXInput) {
                foreach (var b in _profileManager.ControllerBaseBindings) {
                    var key = new InputKey(b.DeviceIdentifier, b.InputType, b.InputCode);
                    if (!_bindingCache.TryGetValue(key, out var list)) { list = new List<UsbInputMapper.Profiles.Binding>(); _bindingCache[key] = list; }
                    if (!profile.Bindings.Any(pb => pb.DeviceIdentifier == b.DeviceIdentifier && pb.InputType == b.InputType && pb.InputCode == b.InputCode)) list.Add(b);
                }
            }
            if (_diManager != null) _diManager.HasAxisBindings = _bindingCache.Keys.Any(k => k.Type == 11);
        }

        private void RawInputManager_OnInputEvent(object sender, InputEvent e)
        {
            if (CaptureForm.IsCapturing) { CaptureForm.CurrentInstance?.ProcessInput(e); return; }

            long now = Environment.TickCount;

            if (e.Type == 0 || e.Type == 1)
            {
                _lastStandardInputTime = now;
                _pendingHidEvents.Clear(); 
            }

            if (e.Type == 2)
            {
                if (now - _lastStandardInputTime < 50)
                {
                    InputLogger.Log($"[HID Ignored] (Simultaneous with Standard Input) Code={(int)e.MouseButtonFlags}");
                    return;
                }
                
                _pendingHidEvents.Add(e);
                Task.Run(async () => {
                    await Task.Delay(30);
                    _syncContext.Post(_ => {
                        if (_pendingHidEvents.Contains(e))
                        {
                            _pendingHidEvents.Remove(e);
                            ProcessRawInputEvent(e);
                        }
                    }, null);
                });
                return;
            }

            ProcessRawInputEvent(e);
        }

        private void ProcessRawInputEvent(InputEvent e)
        {
            int inputCode = (e.Type == 1) ? e.VKey : (int)e.MouseButtonFlags;
            
            var tKey = new TriggerKeyHash(e.Type, inputCode);
            if (e.IsKeyDown) _physicalKeysDown[tKey] = true; else _physicalKeysDown.TryRemove(tKey, out _);

            bool ctrl = _physicalKeysDown.ContainsKey(new TriggerKeyHash(1, (int)Keys.LControlKey)) || _physicalKeysDown.ContainsKey(new TriggerKeyHash(1, (int)Keys.RControlKey));
            bool shift = _physicalKeysDown.ContainsKey(new TriggerKeyHash(1, (int)Keys.LShiftKey)) || _physicalKeysDown.ContainsKey(new TriggerKeyHash(1, (int)Keys.RShiftKey));
            bool alt = _physicalKeysDown.ContainsKey(new TriggerKeyHash(1, (int)Keys.LMenu)) || _physicalKeysDown.ContainsKey(new TriggerKeyHash(1, (int)Keys.RMenu));
            string mods = $"[Ctrl:{ctrl} Shift:{shift} Alt:{alt}]";

            bool hasBinding = _bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, inputCode), out var bindings);
            bool isBlocked = _globalHookManager.WasRecentlyBlocked(e.Type, inputCode);

            InputLogger.Log($"Raw: {e.DeviceIdentifier} | {UsbInputMapper.Profiles.Binding.GetCodeName(e.Type, inputCode)} | {(e.IsKeyDown?"Down":"Up ")} | {mods} | Block:{isBlocked}");

            if (!hasBinding || bindings.Count == 0)
            {
                if (isBlocked) { if (e.Type == 1) _dispatcher.SendKeyboardInputs(new List<int> { inputCode }, e.IsKeyDown); else if (e.Type == 0) _dispatcher.SendMouseClick(inputCode, e.IsKeyDown); }
                return;
            }
            foreach (var b in bindings) { if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))) ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, inputCode, e.IsKeyDown); }
        }

        private void DiManager_OnInputEvent(object sender, DirectInputEvent e)
        {
            if (CaptureForm.IsCapturing) return;

            if (e.Type == 12)
            {
                var povKey = new PovKey(e.DeviceIdentifier, e.Code);
                if (e.Value == -1) {
                    if (_lastPovStates.TryGetValue(povKey, out int lastVal)) {
                        if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, lastVal), out var rBindings)) {
                            foreach(var b in rBindings) if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))) ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, lastVal, false);
                        }
                        _lastPovStates.Remove(povKey);
                    }
                }
                else {
                    if (_lastPovStates.TryGetValue(povKey, out int lastVal) && lastVal != e.Value) {
                        if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, lastVal), out var rBindings)) {
                            foreach(var b in rBindings) if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))) ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, lastVal, false);
                        }
                    }
                    _lastPovStates[povKey] = e.Value;
                    if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, e.Value), out var dBindings)) {
                        foreach(var b in dBindings) if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))) ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, e.Value, true);
                    }
                }
                return;
            }
            if (e.Type == 10) { var tKey = new TriggerKeyHash(e.Type, e.Code); if (e.IsDown) _physicalKeysDown[tKey] = true; else _physicalKeysDown.TryRemove(tKey, out _); }
            
            if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, e.Code), out var bindings)) {
                foreach (var binding in bindings) {
                    if (binding.SubTriggers != null && !binding.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))) continue;
                    var profile = _profileManager.CurrentActiveProfile;
                    if (profile != null && !profile.EnableXInput && (binding.Action.ActionType == ActionType.XboxController || binding.Action.ActionType == ActionType.XboxAxis || binding.Action.ActionType == ActionType.XboxTrigger)) continue; 

                    if (e.Type == 11) ProcessAnalogAxis(binding, e.Value);
                    else if (e.Type == 10) {
                        if (binding.Action.ActionType == ActionType.XboxTrigger) _viGEmOutput.SetSlider(binding.Action.ArgumentNum == 1 ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger, e.IsDown ? (byte)255 : (byte)0);
                        else ProcessBindingExecution(binding, e.DeviceIdentifier, e.Type, e.Code, e.IsDown);
                    }
                }
            }
        }

        private void ProcessBindingExecution(UsbInputMapper.Profiles.Binding binding, string deviceId, int type, int inputCode, bool isDown)
        {
            var loopKey = new LoopKey(deviceId, type, inputCode, binding.GetHashCode());

            if (isDown)
            {
                if (binding.Condition == TriggerCondition.Release) return;
                if (binding.Action.ActionType == ActionType.RadialMenu) {
                    _syncContext.Post(_ => {
                        if (_radialMenuHudForm == null || _radialMenuHudForm.IsDisposed) {
                            _currentRadialMenuDef = binding.Action; _radialMenuHudForm = new RadialMenuHudForm(_currentRadialMenuDef); _radialMenuHudForm.Show();
                            if (_currentRadialMenuDef.RadialMenuMode == 1) {
                                _globalHookManager.IsRadialMenuClickCapturing = true;
                                _globalHookManager.OnRadialMenuClickCaptured = () => {
                                    _syncContext.Post(__ => {
                                        if (_radialMenuHudForm != null && !_radialMenuHudForm.IsDisposed) {
                                            int sel = _radialMenuHudForm.SelectedDirectionIndex; _radialMenuHudForm.Hide(); _radialMenuHudForm.Dispose(); _radialMenuHudForm = null;
                                            if (sel >= 0 && sel < _currentRadialMenuDef.RadialMenuDirections.Count) { var act = _currentRadialMenuDef.RadialMenuDirections[sel].Action; if (act != null && act.ActionType != ActionType.None) { ExecuteAction(act, true); ExecuteAction(act, false); } }
                                        }
                                    }, null);
                                };
                            }
                        }
                    }, null);
                    return;
                }

                if (_activeLoops.ContainsKey(loopKey)) return;
                var cts = new CancellationTokenSource(); _activeLoops[loopKey] = cts;
                Task.Run(async () => {
                    try {
                        if (binding.Condition == TriggerCondition.RapidFire) { while (!cts.Token.IsCancellationRequested) { ExecuteAction(binding.Action, true); await Task.Delay(20); ExecuteAction(binding.Action, false); await Task.Delay(Math.Max(10, binding.ConditionParam), cts.Token); } }
                        else { ExecuteAction(binding.Action, true); }
                    } catch { }
                }, cts.Token);
            }
            else
            {
                if (binding.Action.ActionType == ActionType.RadialMenu) {
                    if (binding.Action.RadialMenuMode == 0) {
                        _syncContext.Post(_ => {
                            if (_radialMenuHudForm != null && !_radialMenuHudForm.IsDisposed) {
                                int sel = _radialMenuHudForm.SelectedDirectionIndex; _radialMenuHudForm.Hide(); _radialMenuHudForm.Dispose(); _radialMenuHudForm = null;
                                if (sel >= 0 && sel < _currentRadialMenuDef.RadialMenuDirections.Count) { var act = _currentRadialMenuDef.RadialMenuDirections[sel].Action; if (act != null && act.ActionType != ActionType.None) { ExecuteAction(act, true); ExecuteAction(act, false); } }
                            }
                        }, null);
                    }
                    return;
                }
                if (_activeLoops.TryRemove(loopKey, out var cts)) { cts.Cancel(); cts.Dispose(); }
                if (binding.Condition == TriggerCondition.Release) { ExecuteAction(binding.Action, true); Thread.Sleep(20); ExecuteAction(binding.Action, false); }
                else { ExecuteAction(binding.Action, false); }
            }
        }

        private void ProcessAnalogAxis(UsbInputMapper.Profiles.Binding binding, int rawValue)
        {
            double normalized = 0;
            if (binding.AxisRange == 0) normalized = (rawValue - 32767.5) / 32767.5; else if (binding.AxisRange == 1) normalized = (rawValue < 32767) ? 0 : (rawValue - 32767.5) / 32767.5; else if (binding.AxisRange == 2) normalized = (rawValue > 32767) ? 0 : (32767.5 - rawValue) / 32767.5;
            if (binding.InvertAxis) normalized *= -1;
            double deadZone = binding.DeadZone / 100.0;
            if (Math.Abs(normalized) < deadZone) normalized = 0; else normalized = Math.Sign(normalized) * ((Math.Abs(normalized) - deadZone) / (1.0 - deadZone));
            if (binding.AccelerationCurve == 1) normalized = Math.Sign(normalized) * Math.Sqrt(Math.Abs(normalized)); else if (binding.AccelerationCurve == 2) normalized = Math.Sign(normalized) * Math.Pow(Math.Abs(normalized), 2);

            if (binding.Action.ActionType == ActionType.XboxAxis) { short outValue = (short)(normalized * 32767); Xbox360Axis axis = Xbox360Axis.LeftThumbX; switch(binding.Action.ArgumentNum) { case 1: axis = Xbox360Axis.LeftThumbX; break; case 2: axis = Xbox360Axis.LeftThumbY; outValue = (short)-outValue; break; case 3: axis = Xbox360Axis.RightThumbX; break; case 4: axis = Xbox360Axis.RightThumbY; outValue = (short)-outValue; break; } _viGEmOutput.SetAxis(axis, outValue); }
            else if (binding.Action.ActionType == ActionType.XboxTrigger) { double trigNorm = (binding.AxisRange == 0) ? (normalized + 1.0) / 2.0 : Math.Abs(normalized); _viGEmOutput.SetSlider(binding.Action.ArgumentNum == 1 ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger, (byte)(trigNorm * 255)); }
            else if (binding.Action.ActionType == ActionType.StickToMouse) { double szDZ = binding.Action.StickDeadZone / 100.0; double spd = binding.Action.StickMaxSpeed; double norm2 = (rawValue - 32767.5) / 32767.5; if (Math.Abs(norm2) < szDZ) norm2 = 0; else norm2 = Math.Sign(norm2) * ((Math.Abs(norm2) - szDZ) / (1.0 - szDZ)); if (binding.Action.StickCurve == 1) norm2 = Math.Sign(norm2) * Math.Sqrt(Math.Abs(norm2)); else if (binding.Action.StickCurve == 2) norm2 = Math.Sign(norm2) * Math.Pow(Math.Abs(norm2), 2); int dVal = (int)(norm2 * spd); if (binding.InputCode == 0 || binding.InputCode == 3) _stickMouseDx = dVal; else if (binding.InputCode == 1 || binding.InputCode == 4) _stickMouseDy = dVal; }
        }

        private void ExecuteAction(ActionDef action, bool isDown)
        {
            if (isDown && action.UseVibration) VibrationManager.Vibrate(action.VibrateDuration, action.VibrateTimes); 
            if (action.ActionType == ActionType.XboxController) _viGEmOutput.SetButton(GetXboxButton(action.ArgumentNum), isDown);
            else _dispatcher.Dispatch(action, isDown);
        }

        private Xbox360Button GetXboxButton(int id)
        {
            switch(id) { case 1: return Xbox360Button.A; case 2: return Xbox360Button.B; case 3: return Xbox360Button.X; case 4: return Xbox360Button.Y; case 5: return Xbox360Button.LeftShoulder; case 6: return Xbox360Button.RightShoulder; case 7: return Xbox360Button.Back; case 8: return Xbox360Button.Start; case 9: return Xbox360Button.LeftThumb; case 10: return Xbox360Button.RightThumb; case 11: return Xbox360Button.Up; case 12: return Xbox360Button.Down; case 13: return Xbox360Button.Left; case 14: return Xbox360Button.Right; default: return Xbox360Button.A; }
        }

        private void InitializeTrayIcon() { _trayIcon = new NotifyIcon { Icon = SystemIcons.Application, Text = "UsbInputMapper", Visible = true }; ContextMenuStrip menu = new ContextMenuStrip(); menu.Items.Add("設定を開く", null, ShowMainForm); menu.Items.Add("終了", null, ExitApp); _trayIcon.ContextMenuStrip = menu; _trayIcon.DoubleClick += ShowMainForm; }
        private void ShowMainForm(object sender, EventArgs e) { if (_mainForm == null || _mainForm.IsDisposed) _mainForm = new MainForm(_profileManager, _diManager); _mainForm.Show(); _mainForm.Activate(); }
        private void ExitApp(object sender, EventArgs e) { _loopTimer?.Stop(); _trayIcon.Visible = false; _dispatcher?.ReleaseAllInputs(); _globalHookManager?.Dispose(); _rawInputManager?.Dispose(); _diManager?.Dispose(); _viGEmOutput?.Dispose(); Application.Exit(); }
    }
}
