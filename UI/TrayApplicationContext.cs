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

        private struct TriggerKeyHash : IEquatable<TriggerKeyHash>
        {
            public int Type;
            public int Code;
            public TriggerKeyHash(int type, int code) { Type = type; Code = code; }
            public bool Equals(TriggerKeyHash other) => Type == other.Type && Code == other.Code;
            public override int GetHashCode() => (Type * 397) ^ Code;
        }

        private struct InputKey : IEquatable<InputKey>
        {
            public string DeviceIdentifier;
            public int Type;
            public int Code;
            public InputKey(string deviceIdentifier, int type, int code) { DeviceIdentifier = deviceIdentifier; Type = type; Code = code; }
            public bool Equals(InputKey other) => Type == other.Type && Code == other.Code && string.Equals(DeviceIdentifier, other.DeviceIdentifier, StringComparison.Ordinal);
            public override int GetHashCode() => (((DeviceIdentifier != null ? DeviceIdentifier.GetHashCode() : 0) * 397) ^ Type) * 397 ^ Code;
        }

        private struct PovKey : IEquatable<PovKey>
        {
            public string DeviceIdentifier;
            public int Code;
            public PovKey(string deviceIdentifier, int code) { DeviceIdentifier = deviceIdentifier; Code = code; }
            public bool Equals(PovKey other) => Code == other.Code && string.Equals(DeviceIdentifier, other.DeviceIdentifier, StringComparison.Ordinal);
            public override int GetHashCode() => ((DeviceIdentifier != null ? DeviceIdentifier.GetHashCode() : 0) * 397) ^ Code;
        }

        // ★ GCスパイク防止: Stringではなく構造体をキーにする
        private struct LoopKey : IEquatable<LoopKey>
        {
            public string DeviceId;
            public int Type;
            public int Code;
            public int BindingHash;
            public LoopKey(string deviceId, int type, int code, int bindingHash) { DeviceId = deviceId; Type = type; Code = code; BindingHash = bindingHash; }
            public bool Equals(LoopKey other) => Type == other.Type && Code == other.Code && BindingHash == other.BindingHash && string.Equals(DeviceId, other.DeviceId, StringComparison.Ordinal);
            public override int GetHashCode() => (((((DeviceId != null ? DeviceId.GetHashCode() : 0) * 397) ^ Type) * 397) ^ Code) * 397 ^ BindingHash;
        }

        private ConcurrentDictionary<TriggerKeyHash, bool> _physicalKeysDown = new ConcurrentDictionary<TriggerKeyHash, bool>();
        private ConcurrentDictionary<LoopKey, CancellationTokenSource> _activeLoops = new ConcurrentDictionary<LoopKey, CancellationTokenSource>();
        private Dictionary<PovKey, int> _lastPovStates = new Dictionary<PovKey, int>();
        private Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>> _bindingCache = new Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>>();

        public TrayApplicationContext()
        {
            InitializeCore();
            InitializeTrayIcon();
        }

        private void InitializeCore()
        {
            _profileManager = new ProfileManager();
            _profileManager.Load();
            
            _profileManager.OnProfileChanged += (s, e) => { UpdateHookBlockList(); UpdateBindingCache(); };
            _profileManager.OnSettingsChanged += (s, e) => { UpdateHookBlockList(); UpdateBindingCache(); };

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

            UpdateBindingCache();
        }

        private void UpdateHookBlockList()
        {
            var blockList = new HashSet<long>();
            var profile = _profileManager.CurrentActiveProfile;
            if (profile != null)
            {
                foreach (var b in profile.Bindings)
                    if (b.BlockOriginalInput) blockList.Add(((long)b.InputType << 32) | (uint)b.InputCode);
            }
            _globalHookManager.SetBlockList(blockList);
        }

        private void UpdateBindingCache()
        {
            _bindingCache.Clear();
            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;

            foreach (var b in profile.Bindings)
            {
                var key = new InputKey(b.DeviceIdentifier, b.InputType, b.InputCode);
                if (!_bindingCache.TryGetValue(key, out var list))
                {
                    list = new List<UsbInputMapper.Profiles.Binding>();
                    _bindingCache[key] = list;
                }
                list.Add(b);
            }

            if (profile.EnableXInput)
            {
                foreach (var b in _profileManager.ControllerBaseBindings)
                {
                    var key = new InputKey(b.DeviceIdentifier, b.InputType, b.InputCode);
                    if (!_bindingCache.TryGetValue(key, out var list))
                    {
                        list = new List<UsbInputMapper.Profiles.Binding>();
                        _bindingCache[key] = list;
                    }
                    
                    bool existsInProfile = profile.Bindings.Any(pb => pb.DeviceIdentifier == b.DeviceIdentifier && pb.InputType == b.InputType && pb.InputCode == b.InputCode);
                    if (!existsInProfile)
                    {
                        list.Add(b);
                    }
                }
            }

            if (_diManager != null)
            {
                _diManager.HasAxisBindings = _bindingCache.Keys.Any(k => k.Type == 11);
            }
        }

        private void RawInputManager_OnInputEvent(object sender, InputEvent e)
        {
            if (CaptureForm.IsCapturing) { CaptureForm.CurrentInstance?.ProcessInput(e); return; }
            int inputCode = (e.Type == 1) ? e.VKey : (int)e.MouseButtonFlags;
            
            var tKey = new TriggerKeyHash(e.Type, inputCode);
            if (e.IsKeyDown) _physicalKeysDown[tKey] = true; else _physicalKeysDown.TryRemove(tKey, out _);

            if (!_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, inputCode), out var bindings) || bindings.Count == 0)
            {
                if (_globalHookManager.WasRecentlyBlocked(e.Type, inputCode))
                {
                    if (e.Type == 1) _dispatcher.SendKeyboardInputs(new List<int> { inputCode }, e.IsKeyDown);
                    else if (e.Type == 0) _dispatcher.SendMouseClick(inputCode, e.IsKeyDown);
                }
                return;
            }

            foreach (var b in bindings) 
            {
                if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                {
                    ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, inputCode, e.IsKeyDown);
                }
            }
        }

        private void DiManager_OnInputEvent(object sender, DirectInputEvent e)
        {
            if (CaptureForm.IsCapturing) return;

            if (e.Type == 12)
            {
                var povKey = new PovKey(e.DeviceIdentifier, e.Code);
                if (e.Value == -1) 
                {
                    if (_lastPovStates.TryGetValue(povKey, out int lastVal))
                    {
                        if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, lastVal), out var rBindings))
                        {
                            foreach(var b in rBindings)
                            {
                                if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                                    ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, lastVal, false);
                            }
                        }
                        _lastPovStates.Remove(povKey);
                    }
                    return;
                }
                else 
                {
                    if (_lastPovStates.TryGetValue(povKey, out int lastVal) && lastVal != e.Value)
                    {
                        if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, lastVal), out var rBindings))
                        {
                            foreach(var b in rBindings)
                            {
                                if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                                    ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, lastVal, false);
                            }
                        }
                    }
                    _lastPovStates[povKey] = e.Value;
                    if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, e.Value), out var dBindings))
                    {
                        foreach(var b in dBindings)
                        {
                            if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                                ProcessBindingExecution(b, e.DeviceIdentifier, e.Type, e.Value, true);
                        }
                    }
                }
                return;
            }

            if (e.Type == 10) 
            { 
                var tKey = new TriggerKeyHash(e.Type, e.Code);
                if (e.IsDown) _physicalKeysDown[tKey] = true; 
                else _physicalKeysDown.TryRemove(tKey, out _); 
            }
            
            if (_bindingCache.TryGetValue(new InputKey(e.DeviceIdentifier, e.Type, e.Code), out var bindings))
            {
                foreach (var binding in bindings)
                {
                    if (binding.SubTriggers != null && !binding.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                    {
                        continue;
                    }

                    var profile = _profileManager.CurrentActiveProfile;
                    if (profile != null && !profile.EnableXInput &&
                       (binding.Action.ActionType == ActionType.XboxController || binding.Action.ActionType == ActionType.XboxAxis || binding.Action.ActionType == ActionType.XboxTrigger))
                    {
                        continue; 
                    }

                    if (e.Type == 11) 
                    {
                        ProcessAnalogAxis(binding, e.Value);
                    }
                    else if (e.Type == 10) 
                    {
                        if (binding.Action.ActionType == ActionType.XboxTrigger)
                        {
                            _viGEmOutput.SetSlider(binding.Action.ArgumentNum == 1 ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger, e.IsDown ? (byte)255 : (byte)0);
                        }
                        else
                        {
                            ProcessBindingExecution(binding, e.DeviceIdentifier, e.Type, e.Code, e.IsDown);
                        }
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
                if (_activeLoops.ContainsKey(loopKey)) return;
                var cts = new CancellationTokenSource();
                _activeLoops[loopKey] = cts;

                Task.Run(async () =>
                {
                    try
                    {
                        if (binding.Condition == TriggerCondition.RapidFire)
                        {
                            while (!cts.Token.IsCancellationRequested)
                            {
                                ExecuteAction(binding.Action, true);
                                await Task.Delay(20);
                                ExecuteAction(binding.Action, false);
                                await Task.Delay(Math.Max(10, binding.ConditionParam), cts.Token);
                            }
                        }
                        else { ExecuteAction(binding.Action, true); }
                    } catch { }
                }, cts.Token);
            }
            else
            {
                if (_activeLoops.TryRemove(loopKey, out var cts)) { cts.Cancel(); cts.Dispose(); }
                if (binding.Condition == TriggerCondition.Release)
                {
                    ExecuteAction(binding.Action, true); Thread.Sleep(20); ExecuteAction(binding.Action, false);
                }
                else { ExecuteAction(binding.Action, false); }
            }
        }

        private void ProcessAnalogAxis(UsbInputMapper.Profiles.Binding binding, int rawValue)
        {
            double normalized = 0;
            if (binding.AxisRange == 0) normalized = (rawValue - 32767.5) / 32767.5;
            else if (binding.AxisRange == 1) normalized = (rawValue < 32767) ? 0 : (rawValue - 32767.5) / 32767.5;
            else if (binding.AxisRange == 2) normalized = (rawValue > 32767) ? 0 : (32767.5 - rawValue) / 32767.5;

            if (binding.InvertAxis) normalized *= -1;

            double deadZone = binding.DeadZone / 100.0;
            if (Math.Abs(normalized) < deadZone) normalized = 0;
            else normalized = Math.Sign(normalized) * ((Math.Abs(normalized) - deadZone) / (1.0 - deadZone));

            if (binding.AccelerationCurve == 1) normalized = Math.Sign(normalized) * Math.Sqrt(Math.Abs(normalized));
            else if (binding.AccelerationCurve == 2) normalized = Math.Sign(normalized) * Math.Pow(Math.Abs(normalized), 2);

            if (binding.Action.ActionType == ActionType.XboxAxis)
            {
                short outValue = (short)(normalized * 32767);
                Xbox360Axis axis = Xbox360Axis.LeftThumbX;
                switch(binding.Action.ArgumentNum)
                {
                    case 1: axis = Xbox360Axis.LeftThumbX; break; case 2: axis = Xbox360Axis.LeftThumbY; outValue = (short)-outValue; break; 
                    case 3: axis = Xbox360Axis.RightThumbX; break; case 4: axis = Xbox360Axis.RightThumbY; outValue = (short)-outValue; break;
                }
                _viGEmOutput.SetAxis(axis, outValue);
            }
            else if (binding.Action.ActionType == ActionType.XboxTrigger)
            {
                double trigNorm = (binding.AxisRange == 0) ? (normalized + 1.0) / 2.0 : Math.Abs(normalized);
                _viGEmOutput.SetSlider(binding.Action.ArgumentNum == 1 ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger, (byte)(trigNorm * 255));
            }
        }

        private void ExecuteAction(ActionDef action, bool isDown)
        {
            if (action.ActionType == ActionType.XboxController) _viGEmOutput.SetButton(GetXboxButton(action.ArgumentNum), isDown);
            else _dispatcher.Dispatch(action, isDown);
        }

        private Xbox360Button GetXboxButton(int id)
        {
            switch(id)
            {
                case 1: return Xbox360Button.A; case 2: return Xbox360Button.B; case 3: return Xbox360Button.X; case 4: return Xbox360Button.Y;
                case 5: return Xbox360Button.LeftShoulder; case 6: return Xbox360Button.RightShoulder; case 7: return Xbox360Button.Back; case 8: return Xbox360Button.Start;
                case 9: return Xbox360Button.LeftThumb; case 10: return Xbox360Button.RightThumb; case 11: return Xbox360Button.Up; case 12: return Xbox360Button.Down;
                case 13: return Xbox360Button.Left; case 14: return Xbox360Button.Right; default: return Xbox360Button.A;
            }
        }

        private void InitializeTrayIcon() { _trayIcon = new NotifyIcon { Icon = SystemIcons.Application, Text = "UsbInputMapper", Visible = true }; ContextMenuStrip menu = new ContextMenuStrip(); menu.Items.Add("設定を開く", null, ShowMainForm); menu.Items.Add("終了", null, ExitApp); _trayIcon.ContextMenuStrip = menu; _trayIcon.DoubleClick += ShowMainForm; }
        private void ShowMainForm(object sender, EventArgs e) { if (_mainForm == null || _mainForm.IsDisposed) _mainForm = new MainForm(_profileManager, _diManager); _mainForm.Show(); _mainForm.Activate(); }
        private void ExitApp(object sender, EventArgs e) { _trayIcon.Visible = false; _globalHookManager?.Dispose(); _rawInputManager?.Dispose(); _diManager?.Dispose(); _viGEmOutput?.Dispose(); Application.Exit(); }
    }
}
