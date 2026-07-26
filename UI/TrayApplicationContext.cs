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

namespace UsbInputMapper.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        private NotifyIcon _trayIcon;
        private MainForm _mainForm;
        private volatile bool _isSuspended = false;

        private RawInputManager _rawInputManager;
        private DirectInputManager _diManager;
        private ViGEmOutput _viGEmOutput;
        private OutputDispatcher _dispatcher;
        private ProfileManager _profileManager;
        private ForegroundAppWatcher _appWatcher;
        private GlobalHookManager _globalHookManager;

        private SynchronizationContext _syncContext;

        private BlockingCollection<InputEvent> _inputQueue = new BlockingCollection<InputEvent>();
        private CancellationTokenSource _queueCts = new CancellationTokenSource();

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
        private struct LoopKey : IEquatable<LoopKey>
        {
            public string DeviceId; public int Type; public int Code; public int BindingHash;
            public LoopKey(string deviceId, int type, int code, int bindingHash) { DeviceId = deviceId; Type = type; Code = code; BindingHash = bindingHash; }
            public bool Equals(LoopKey other) => Type == other.Type && Code == other.Code && BindingHash == other.BindingHash && string.Equals(DeviceId, other.DeviceId, StringComparison.Ordinal);
            public override int GetHashCode() => (((((DeviceId != null ? DeviceId.GetHashCode() : 0) * 397) ^ Type) * 397) ^ Code) * 397 ^ BindingHash;
        }

        private class KeyClickState {
            public int Count;
            public long LastDownTime;
        }
        
        private class PendingExecution {
            public UsbInputMapper.Profiles.Binding Binding;
            public string DeviceIdentifier;
            public int Type;
            public int Code;
            public bool IsDown;
            public int Value;
            public long ExecuteTime;
        }

        private Dictionary<TriggerKeyHash, bool> _physicalKeysDown = new Dictionary<TriggerKeyHash, bool>(); 
        private ConcurrentDictionary<LoopKey, CancellationTokenSource> _activeLoops = new ConcurrentDictionary<LoopKey, CancellationTokenSource>();
        private Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>> _bindingCache = new Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>>();
        
        private Dictionary<InputKey, KeyClickState> _clickStates = new Dictionary<InputKey, KeyClickState>();
        private List<PendingExecution> _pendingExecutions = new List<PendingExecution>();

        private System.Threading.Timer _activeTimer;
        private volatile int _stickMouseDx = 0;
        private volatile int _stickMouseDy = 0;
        private volatile int _currentBezelCode = -1;
        private volatile int _bezelHoverTime = 0;
        private volatile bool _hasBezelBindings = false;

        private RadialMenuHudForm _radialMenuHudForm;
        private ActionDef _currentRadialMenuDef;
        
        private ProfileOverlayForm _currentOverlay;
        private readonly object _overlayLock = new object();

        public TrayApplicationContext() 
        {
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            InitializeCore(); 
            InitializeTrayIcon(); 
            Task.Factory.StartNew(ProcessInputQueue, _queueCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void InitializeCore()
        {
            _profileManager = new ProfileManager(); _profileManager.Load();
            _profileManager.OnProfileChanged += (s, e) => { _inputQueue.Add(new InputEvent { Type = -999 }); };
            _profileManager.OnSettingsChanged += (s, e) => { UpdateHookBlockList(); UpdateBindingCache(); };

            _appWatcher = new ForegroundAppWatcher(); 
            _appWatcher.OnForegroundAppChanged += (s, appPath) => _profileManager.SwitchToAppProfile(appPath); 
            _appWatcher.Start();

            _viGEmOutput = new ViGEmOutput(); _viGEmOutput.Initialize();
            _dispatcher = new OutputDispatcher(_viGEmOutput);
            
            _globalHookManager = new GlobalHookManager(); 
            _globalHookManager.OnBlockedInputFired += GlobalHookManager_OnBlockedInputFired;
            _globalHookManager.OnMouseMove += GlobalHookManager_OnMouseMove;
            UpdateHookBlockList();
            
            var initialProfile = _profileManager.CurrentActiveProfile;
            if (initialProfile != null)
            {
                _globalHookManager.EnableChatteringCanceler = initialProfile.OverrideGlobalChattering ? initialProfile.EnableChatteringCanceler : _profileManager.GlobalConfig.EnableChatteringCanceler;
                _globalHookManager.ChatteringThresholdMs = initialProfile.OverrideGlobalChattering ? initialProfile.ChatteringThresholdMs : _profileManager.GlobalConfig.ChatteringThresholdMs;
            }
            
            _rawInputManager = new RawInputManager(); 
            _rawInputManager.OnInputEvent += (s, e) => { if (!_isSuspended) _inputQueue.Add(e); };
            _rawInputManager.OnDeviceChanged += (s, e) => { _diManager?.RefreshDevices(); UpdateBindingCache(); }; 

            _diManager = new DirectInputManager(); 
            _diManager.OnInputEvent += (s, e) => {
                if (!_isSuspended) {
                    _inputQueue.Add(new InputEvent { DeviceIdentifier = e.DeviceIdentifier, Type = e.Type, Code = e.Code, Value = e.Value, IsDown = e.IsDown, Timestamp = (long)GetTickCount64() });
                }
            };

            UpdateBindingCache();
            _activeTimer = new System.Threading.Timer(ActiveTimer_Tick, null, 10, 10);
        }

        private void InitializeTrayIcon()
        {
            _mainForm = new MainForm(_profileManager, _diManager);
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "UsbInputMapper"
            };
            
            var menu = new ContextMenuStrip();
            var settingsItem = new ToolStripMenuItem("設定画面を開く");
            settingsItem.Click += (s, e) => {
                if (_mainForm == null || _mainForm.IsDisposed) _mainForm = new MainForm(_profileManager, _diManager);
                _mainForm.Show(); _mainForm.Activate();
            };
            
            var suspendItem = new ToolStripMenuItem("一時停止");
            suspendItem.CheckOnClick = true;
            suspendItem.CheckedChanged += (s, e) => {
                _isSuspended = suspendItem.Checked;
                if (_isSuspended) _dispatcher?.ReleaseAllInputs();
            };
            
            var exitItem = new ToolStripMenuItem("終了");
            exitItem.Click += (s, e) => ExitThread();
            
            menu.Items.Add(settingsItem);
            menu.Items.Add(suspendItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);
            
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, e) => settingsItem.PerformClick();
        }

        private void HandleProfileSwitchSignal()
        {
            var p = _profileManager.CurrentActiveProfile;
            if (p != null)
            {
                UpdateHookBlockList();
                
                _globalHookManager.EnableChatteringCanceler = p.OverrideGlobalChattering ? p.EnableChatteringCanceler : _profileManager.GlobalConfig.EnableChatteringCanceler;
                _globalHookManager.ChatteringThresholdMs = p.OverrideGlobalChattering ? p.ChatteringThresholdMs : _profileManager.GlobalConfig.ChatteringThresholdMs;

                if (p.NotifyProfileChangeVibration)
                    VibrationManager.Vibrate(300, 1);

                if (p.OverlayShowMark || p.OverlayShowName)
                {
                    _syncContext.Post(_ => {
                        try
                        {
                            lock (_overlayLock)
                            {
                                if (_currentOverlay != null && !_currentOverlay.IsDisposed) {
                                    _currentOverlay.Close();
                                }
                                _currentOverlay = new ProfileOverlayForm(p);
                                _currentOverlay.Show();
                            }
                        }
                        catch (Exception ex)
                        {
                            InputLogger.Log($"Overlay Error: {ex.Message}");
                        }
                    }, null);
                }
            }
        }

        private void ProcessInputQueue()
        {
            try
            {
                foreach (var evt in _inputQueue.GetConsumingEnumerable(_queueCts.Token))
                {
                    if (evt.Type == -999) 
                    {
                        HandleProfileSwitchSignal();
                        continue;
                    }

                    if (CaptureForm.IsCapturing && CaptureForm.CurrentInstance != null)
                    {
                        _syncContext.Post(_ => CaptureForm.CurrentInstance?.ProcessInput(evt), null);
                        continue;
                    }

                    var tKey = new TriggerKeyHash(evt.Type, evt.Code);
                    if (evt.IsDown) _physicalKeysDown[tKey] = true; 
                    else _physicalKeysDown.Remove(tKey);

                    var iKey = new InputKey(evt.DeviceIdentifier, evt.Type, evt.Code);

                    if (!_clickStates.TryGetValue(iKey, out var cState)) {
                        cState = new KeyClickState { Count = 0, LastDownTime = 0 };
                        _clickStates[iKey] = cState;
                    }

                    long now = evt.Timestamp;
                    int doubleTime = _profileManager.GlobalConfig.DoubleClickTimeMs;
                    int tripleTime = _profileManager.GlobalConfig.TripleClickTimeMs;

                    if (evt.IsDown) {
                        if (cState.Count == 0 || 
                            (cState.Count == 1 && now - cState.LastDownTime > doubleTime) || 
                            (cState.Count >= 2 && now - cState.LastDownTime > tripleTime)) 
                        {
                            cState.Count = 1;
                        } else {
                            cState.Count++;
                        }
                        cState.LastDownTime = now;

                        lock (_pendingExecutions) {
                            if (cState.Count == 2) {
                                _pendingExecutions.RemoveAll(p => p.Type == evt.Type && p.Code == evt.Code && p.Binding.ClickTriggerCount == 1);
                            } else if (cState.Count == 3) {
                                _pendingExecutions.RemoveAll(p => p.Type == evt.Type && p.Code == evt.Code && p.Binding.ClickTriggerCount == 2);
                            }
                        }
                    }

                    int currentClick = cState.Count;

                    var bindings = new List<UsbInputMapper.Profiles.Binding>();
                    if (_bindingCache.TryGetValue(iKey, out var exact)) bindings.AddRange(exact);
                    if (_bindingCache.TryGetValue(new InputKey("Any", evt.Type, evt.Code), out var anyB)) bindings.AddRange(anyB);

                    if (bindings.Count > 0)
                    {
                        var matchedBindings = bindings.Where(b => b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))).ToList();

                        bool delaySingle = matchedBindings.Any(b => b.ClickTriggerCount == 2 && !b.ExecuteSingleSimultaneously) ||
                                           matchedBindings.Any(b => b.ClickTriggerCount == 3 && !b.ExecuteSingleSimultaneously);
                        bool delayDouble = matchedBindings.Any(b => b.ClickTriggerCount == 3 && !b.ExecuteDoubleSimultaneously);

                        foreach (var b in matchedBindings) {
                            if (evt.IsDown) {
                                if (b.ClickTriggerCount == currentClick) {
                                    if (currentClick == 1 && delaySingle) {
                                        lock (_pendingExecutions) _pendingExecutions.Add(new PendingExecution { Binding = b, DeviceIdentifier = evt.DeviceIdentifier, Type = evt.Type, Code = evt.Code, IsDown = evt.IsDown, Value = evt.Value, ExecuteTime = now + doubleTime });
                                    } else if (currentClick == 2 && delayDouble) {
                                        lock (_pendingExecutions) _pendingExecutions.Add(new PendingExecution { Binding = b, DeviceIdentifier = evt.DeviceIdentifier, Type = evt.Type, Code = evt.Code, IsDown = evt.IsDown, Value = evt.Value, ExecuteTime = now + tripleTime });
                                    } else {
                                        ProcessBindingExecution(b, evt.DeviceIdentifier, evt.Type, evt.Code, evt.IsDown, evt.Value);
                                    }
                                }
                            } else {
                                bool isPending = false;
                                long execTime = now;
                                lock (_pendingExecutions) {
                                    var downTask = _pendingExecutions.FirstOrDefault(p => p.Binding == b && p.Type == evt.Type && p.Code == evt.Code && p.IsDown == true);
                                    if (downTask != null) {
                                        isPending = true;
                                        execTime = downTask.ExecuteTime;
                                    }
                                }
                                
                                if (isPending) {
                                    lock (_pendingExecutions) _pendingExecutions.Add(new PendingExecution { Binding = b, DeviceIdentifier = evt.DeviceIdentifier, Type = evt.Type, Code = evt.Code, IsDown = evt.IsDown, Value = evt.Value, ExecuteTime = execTime + 10 }); 
                                } else {
                                    if (b.ClickTriggerCount == currentClick) {
                                        ProcessBindingExecution(b, evt.DeviceIdentifier, evt.Type, evt.Code, evt.IsDown, evt.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void ActiveTimer_Tick(object state)
        {
            long now = (long)GetTickCount64();
            
            List<PendingExecution> toExecute = null;
            lock (_pendingExecutions) {
                if (_pendingExecutions.Count > 0) {
                    toExecute = _pendingExecutions.Where(p => now >= p.ExecuteTime).OrderBy(p => p.ExecuteTime).ToList();
                    foreach (var p in toExecute) _pendingExecutions.Remove(p);
                }
            }
            
            if (toExecute != null) {
                foreach (var p in toExecute) {
                    ProcessBindingExecution(p.Binding, p.DeviceIdentifier, p.Type, p.Code, p.IsDown, p.Value);
                }
            }

            if (_hasBezelBindings)
            {
                if (_currentBezelCode != -1)
                {
                    _bezelHoverTime += 10;
                    if (_bindingCache.TryGetValue(new InputKey("Any", 5, _currentBezelCode), out var bindings))
                    {
                        foreach (var b in bindings)
                        {
                            if (b.Condition == TriggerCondition.Hold && b.ConditionParam > 0 && _bezelHoverTime >= b.ConditionParam)
                            {
                                if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                                {
                                    ProcessBindingExecution(b, "Any", 5, _currentBezelCode, true, 0);
                                }
                            }
                        }
                    }
                }
            }

            if (_stickMouseDx != 0 || _stickMouseDy != 0)
            {
                _dispatcher?.SendMouseMove(_stickMouseDx, _stickMouseDy, false, false, false);
            }
        }

        private void GlobalHookManager_OnBlockedInputFired(object sender, GlobalHookManager.HookInputEvent e)
        {
            _inputQueue.Add(new InputEvent { DeviceIdentifier = "Any", Type = e.Type, Code = e.Code, IsDown = e.IsDown, X = e.X, Y = e.Y, Timestamp = e.Timestamp });
        }

        private void GlobalHookManager_OnMouseMove(object sender, GlobalHookManager.POINT pt)
        {
            if (!_hasBezelBindings) return;
            int screenW = Screen.PrimaryScreen.Bounds.Width;
            int screenH = Screen.PrimaryScreen.Bounds.Height;
            int x = pt.x; int y = pt.y;

            int newBezel = -1;
            if (x <= 1 && y <= 1) newBezel = 0; else if (x >= screenW - 2 && y <= 1) newBezel = 4; else if (x >= screenW - 2 && y >= screenH - 2) newBezel = 8; else if (x <= 1 && y >= screenH - 2) newBezel = 12; else if (y <= 1 && x < screenW / 3) newBezel = 1; else if (y <= 1 && x > screenW * 2 / 3) newBezel = 3; else if (y <= 1) newBezel = 2; else if (x >= screenW - 2 && y < screenH / 3) newBezel = 5; else if (x >= screenW - 2 && y > screenH * 2 / 3) newBezel = 7; else if (x >= screenW - 2) newBezel = 6; else if (y >= screenH - 2 && x > screenW * 2 / 3) newBezel = 9; else if (y >= screenH - 2 && x < screenW / 3) newBezel = 11; else if (y >= screenH - 2) newBezel = 10; else if (x <= 1 && y > screenH * 2 / 3) newBezel = 13; else if (x <= 1 && y < screenH / 3) newBezel = 15; else if (x <= 1) newBezel = 14;

            if (newBezel != _currentBezelCode)
            {
                if (_currentBezelCode != -1) _inputQueue.Add(new InputEvent { DeviceIdentifier = "Any", Type = 5, Code = _currentBezelCode, IsDown = false, Timestamp = (long)GetTickCount64() });
                _currentBezelCode = newBezel;
                _bezelHoverTime = 0;
                if (_currentBezelCode != -1) _inputQueue.Add(new InputEvent { DeviceIdentifier = "Any", Type = 5, Code = _currentBezelCode, IsDown = true, Timestamp = (long)GetTickCount64() });
            }
        }

        private void ProcessBindingExecution(UsbInputMapper.Profiles.Binding b, string devId, int type, int code, bool isDown, int value)
        {
            if (b.Condition == TriggerCondition.Normal)
            {
                if (isDown) ExecuteAction(b.Action, true);
                else ExecuteAction(b.Action, false);
            }
            else if (b.Condition == TriggerCondition.Release)
            {
                if (!isDown) ExecuteAction(b.Action, true);
            }
            else if (b.Condition == TriggerCondition.Sync)
            {
                ExecuteAction(b.Action, isDown);
            }
            else if (b.Condition == TriggerCondition.Hold || b.Condition == TriggerCondition.RapidFire)
            {
                var lKey = new LoopKey(devId, type, code, b.GetHashCode());
                if (isDown)
                {
                    if (b.InputType == 11 && b.AxisRange != 0) 
                    {
                        if (b.AxisRange == 1 && value < 10000) return;
                        if (b.AxisRange == 2 && value > -10000) return;
                    }

                    if (!_activeLoops.ContainsKey(lKey))
                    {
                        var cts = new CancellationTokenSource();
                        _activeLoops[lKey] = cts;
                        Task.Run(() => LoopTask(b, cts.Token));
                    }
                }
                else
                {
                    if (_activeLoops.TryRemove(lKey, out var cts)) { cts.Cancel(); cts.Dispose(); }
                }
            }

            if (b.InputType == 11 && b.Action.ActionType == ActionType.StickToMouse)
            {
                if (isDown)
                {
                    int val = value;
                    if (b.InvertAxis) val = -val;
                    if (b.AxisRange == 1 && val < 0) val = 0;
                    if (b.AxisRange == 2 && val > 0) val = 0;
                    
                    float ratio = (float)val / 32767f;
                    float absRatio = Math.Abs(ratio);
                    float dz = b.DeadZone / 100f;
                    if (absRatio < dz) absRatio = 0;
                    else absRatio = (absRatio - dz) / (1f - dz);

                    if (b.AccelerationCurve == 1) absRatio = (float)Math.Pow(absRatio, 0.5);
                    else if (b.AccelerationCurve == 2) absRatio = (float)Math.Pow(absRatio, 2.0);

                    int speed = (int)(absRatio * b.Action.StickMaxSpeed * Math.Sign(ratio));
                    if (b.InputCode == 1 || b.InputCode == 3) _stickMouseDx = speed;
                    else if (b.InputCode == 2 || b.InputCode == 4) _stickMouseDy = speed;
                }
                else
                {
                    if (b.InputCode == 1 || b.InputCode == 3) _stickMouseDx = 0;
                    else if (b.InputCode == 2 || b.InputCode == 4) _stickMouseDy = 0;
                }
            }
        }

        private async Task LoopTask(UsbInputMapper.Profiles.Binding b, CancellationToken token)
        {
            if (b.Condition == TriggerCondition.Hold)
            {
                try { await Task.Delay(b.ConditionParam, token); ExecuteAction(b.Action, true); } catch { }
            }
            else if (b.Condition == TriggerCondition.RapidFire)
            {
                while (!token.IsCancellationRequested)
                {
                    ExecuteAction(b.Action, true);
                    try { await Task.Delay(b.ConditionParam, token); } catch { break; }
                }
            }
        }

        private void ExecuteAction(ActionDef action, bool isDown)
        {
            if (action.UseVibration) VibrationManager.Vibrate(action.VibrateDuration, action.VibrateTimes);

            if (action.ActionType == ActionType.ProfileSwitch)
            {
                if (action.ArgumentNum == 0) 
                {
                    if (isDown) _profileManager.SwitchToProfile(action.ArgumentStr);
                }
                else if (action.ArgumentNum == 1) 
                {
                    _profileManager.SetTemporaryProfile(action.ArgumentStr, isDown);
                }
            }
            else if (action.ActionType == ActionType.RadialMenu)
            {
                if (isDown)
                {
                    _currentRadialMenuDef = action;
                    _syncContext.Post(_ => {
                        try {
                            if (_radialMenuHudForm != null && !_radialMenuHudForm.IsDisposed) _radialMenuHudForm.Close();
                            _radialMenuHudForm = new RadialMenuHudForm(action);
                            _radialMenuHudForm.Show();
                        } catch { }
                    }, null);

                    if (action.RadialMenuMode == 1) // クリックで実行
                    {
                        GlobalHookManager.Instance.IsRadialMenuClickCapturing = true;
                        GlobalHookManager.Instance.OnRadialMenuClickCaptured = () => {
                            _syncContext.Post(_ => {
                                try {
                                    if (_radialMenuHudForm != null && !_radialMenuHudForm.IsDisposed)
                                    {
                                        int dir = _radialMenuHudForm.SelectedDirectionIndex;
                                        _radialMenuHudForm.Close();
                                        _radialMenuHudForm = null;

                                        if (dir >= 0 && _currentRadialMenuDef != null && dir < _currentRadialMenuDef.RadialMenuDirections.Count)
                                        {
                                            var dAction = _currentRadialMenuDef.RadialMenuDirections[dir].Action;
                                            if (dAction != null && dAction.ActionType != ActionType.None)
                                            {
                                                Task.Run(async () => {
                                                    _dispatcher.Dispatch(dAction, true);
                                                    await Task.Delay(50);
                                                    _dispatcher.Dispatch(dAction, false);
                                                });
                                            }
                                        }
                                        _currentRadialMenuDef = null;
                                    }
                                } catch { }
                            }, null);
                        };
                    }
                }
                else
                {
                    if (action.RadialMenuMode == 0) // ホールド(離した時実行)
                    {
                        _syncContext.Post(_ => {
                            try {
                                if (_radialMenuHudForm != null && !_radialMenuHudForm.IsDisposed)
                                {
                                    int dir = _radialMenuHudForm.SelectedDirectionIndex;
                                    _radialMenuHudForm.Close();
                                    _radialMenuHudForm = null;

                                    if (dir >= 0 && _currentRadialMenuDef != null && dir < _currentRadialMenuDef.RadialMenuDirections.Count)
                                    {
                                        var dAction = _currentRadialMenuDef.RadialMenuDirections[dir].Action;
                                        if (dAction != null && dAction.ActionType != ActionType.None)
                                        {
                                            Task.Run(async () => {
                                                _dispatcher.Dispatch(dAction, true);
                                                await Task.Delay(50);
                                                _dispatcher.Dispatch(dAction, false);
                                            });
                                        }
                                    }
                                    _currentRadialMenuDef = null;
                                }
                            } catch { }
                        }, null);
                    }
                    else // クリック実行モードでボタンを離した場合はキャンセルとして消す
                    {
                        GlobalHookManager.Instance.IsRadialMenuClickCapturing = false;
                        GlobalHookManager.Instance.OnRadialMenuClickCaptured = null;
                        
                        _syncContext.Post(_ => {
                            try {
                                if (_radialMenuHudForm != null && !_radialMenuHudForm.IsDisposed)
                                {
                                    _radialMenuHudForm.Close();
                                    _radialMenuHudForm = null;
                                }
                            } catch { }
                        }, null);
                    }
                }
            }
            else
            {
                _dispatcher?.Dispatch(action, isDown);
            }
        }

        private void UpdateBindingCache()
        {
            var p = _profileManager.CurrentActiveProfile;
            _bindingCache.Clear();
            _hasBezelBindings = false;

            if (p != null)
            {
                var combined = new List<UsbInputMapper.Profiles.Binding>();
                if (p.EnableXInput) combined.AddRange(_profileManager.ControllerBaseBindings);
                combined.AddRange(p.Bindings);

                foreach (var b in combined)
                {
                    if (b.InputType == 5) _hasBezelBindings = true;

                    var k = new InputKey(b.DeviceIdentifier, b.InputType, b.InputCode);
                    if (!_bindingCache.ContainsKey(k)) _bindingCache[k] = new List<UsbInputMapper.Profiles.Binding>();
                    _bindingCache[k].Add(b);
                }
            }
        }

        private void UpdateHookBlockList()
        {
            var set = new HashSet<long>();
            var p = _profileManager.CurrentActiveProfile;
            if (p != null)
            {
                foreach (var b in p.Bindings)
                {
                    if (b.BlockOriginalInput && (b.InputType == 0 || b.InputType == 1))
                    {
                        long key = ((long)b.InputType << 32) | (uint)b.InputCode;
                        set.Add(key);
                    }
                }
            }
            GlobalHookManager.Instance?.SetBlockList(set);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _queueCts.Cancel(); _queueCts.Dispose();
                _activeTimer?.Dispose();
                _globalHookManager?.Dispose();
                _rawInputManager?.Dispose();
                _diManager?.Dispose();
                _viGEmOutput?.Dispose();
                _appWatcher?.Dispose();
                if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
                if (_mainForm != null && !_mainForm.IsDisposed) _mainForm.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
