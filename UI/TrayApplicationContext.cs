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

        // ★入力イベントを直列化するためのキューと処理タスク
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

        private Dictionary<TriggerKeyHash, bool> _physicalKeysDown = new Dictionary<TriggerKeyHash, bool>(); // シリアルアクセスになるため普通のDictionaryで安全
        private ConcurrentDictionary<LoopKey, CancellationTokenSource> _activeLoops = new ConcurrentDictionary<LoopKey, CancellationTokenSource>();
        private Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>> _bindingCache = new Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>>();

        // ★UIタイマーではなく軽量なバックグラウンドタイマーに変更
        private System.Threading.Timer _activeTimer;
        private volatile int _stickMouseDx = 0;
        private volatile int _stickMouseDy = 0;
        private volatile int _currentBezelCode = -1;
        private volatile int _bezelHoverTime = 0;
        private volatile bool _hasBezelBindings = false;

        private RadialMenuHudForm _radialMenuHudForm;
        private ActionDef _currentRadialMenuDef;

        public TrayApplicationContext() 
        {
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            InitializeCore(); 
            InitializeTrayIcon(); 
            
            // 入力処理スレッドの開始
            Task.Factory.StartNew(ProcessInputQueue, _queueCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void InitializeCore()
        {
            _profileManager = new ProfileManager(); _profileManager.Load();
            _profileManager.OnProfileChanged += (s, e) => {
                _inputQueue.Add(new InputEvent { Type = -999 }); // プロファイル切り替えシグナル
            };
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
                _globalHookManager.EnableChatteringCanceler = initialProfile.EnableChatteringCanceler;
                _globalHookManager.ChatteringThresholdMs = initialProfile.ChatteringThresholdMs;
            }
            
            _rawInputManager = new RawInputManager(); 
            _rawInputManager.OnInputEvent += (s, e) => { if (!_isSuspended) _inputQueue.Add(e); };
            _rawInputManager.OnDeviceChanged += (s, e) => { _diManager?.RefreshDevices(); UpdateBindingCache(); }; 

            _diManager = new DirectInputManager(); 
            _diManager.OnInputEvent += (s, e) => {
                if (!_isSuspended) {
                    _inputQueue.Add(new InputEvent { DeviceIdentifier = e.DeviceIdentifier, Type = e.Type, Code = e.Code, Value = e.Value, IsDown = e.IsDown, Timestamp = Environment.TickCount64 });
                }
            };

            UpdateBindingCache();
            _activeTimer = new System.Threading.Timer(ActiveTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void ShutdownCore()
        {
            _queueCts.Cancel();
            _activeTimer?.Dispose();
            _dispatcher?.ReleaseAllInputs(); 
            SystemMouseManager.RestoreAllSafely(); 
            
            _globalHookManager?.Dispose();
            _rawInputManager?.Dispose();
            _diManager?.Dispose();
            _appWatcher?.Dispose();
            _viGEmOutput?.Dispose();
        }

        // ★専用スレッドですべての入力を順番に処理する
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

                    if (_bindingCache.TryGetValue(new InputKey(evt.DeviceIdentifier, evt.Type, evt.Code), out var bindings))
                    {
                        foreach (var b in bindings)
                        {
                            if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                            {
                                ProcessBindingExecution(b, evt.DeviceIdentifier, evt.Type, evt.Code, evt.IsDown, evt.Value);
                            }
                        }
                    }
                    else if (_bindingCache.TryGetValue(new InputKey("Any", evt.Type, evt.Code), out var anyBindings))
                    {
                        foreach (var b in anyBindings)
                        {
                            if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code))))
                            {
                                ProcessBindingExecution(b, evt.DeviceIdentifier, evt.Type, evt.Code, evt.IsDown, evt.Value);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void HandleProfileSwitchSignal()
        {
            _physicalKeysDown.Clear();
            foreach(var cts in _activeLoops.Values) { cts.Cancel(); cts.Dispose(); }
            _activeLoops.Clear();
            
            UpdateHookBlockList(); UpdateBindingCache();
            _dispatcher?.ReleaseAllInputs(); 
            
            var p = _profileManager.CurrentActiveProfile;
            if (p != null)
            {
                if (p.NotifyProfileChangeVibration) VibrationManager.Vibrate(300, 2); 
                
                if (_globalHookManager != null)
                {
                    _globalHookManager.EnableChatteringCanceler = p.EnableChatteringCanceler;
                    _globalHookManager.ChatteringThresholdMs = p.ChatteringThresholdMs;
                }
                
                if (p.OverlayShowMark || p.OverlayShowName)
                {
                    _syncContext.Post(_ => { new ProfileOverlayForm(p).Show(); }, null);
                }
            }
        }

        private void ProcessBindingExecution(UsbInputMapper.Profiles.Binding b, string devId, int type, int code, bool isDown, int rawValue = 0)
        {
            // (前回のProcessBindingExecutionの中身と同じ。非同期ループ処理などもここに記載)
            // （省略：既存の ExecuteAction などのロジック）
        }

        private void GlobalHookManager_OnBlockedInputFired(object sender, GlobalHookManager.HookInputEvent e)
        {
            if (!_isSuspended)
            {
                _inputQueue.Add(new InputEvent { Type = e.Type, Code = e.Code, IsDown = e.IsDown, X = e.X, Y = e.Y, Timestamp = e.Timestamp, DeviceIdentifier = "Any" });
            }
        }

        private void GlobalHookManager_OnMouseMove(object sender, GlobalHookManager.POINT pt)
        {
            if (!_hasBezelBindings || _isSuspended) return;

            int sW = Screen.PrimaryScreen.Bounds.Width; int sH = Screen.PrimaryScreen.Bounds.Height;
            int x = pt.x; int y = pt.y;
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
                if (_currentBezelCode != code)
                {
                    _currentBezelCode = code;
                    _bezelHoverTime = 0;
                    _activeTimer.Change(0, 10);
                }
            }
            else
            {
                if (_currentBezelCode != -1)
                {
                    _currentBezelCode = -1;
                    _bezelHoverTime = 0;
                    if (_stickMouseDx == 0 && _stickMouseDy == 0) _activeTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private void ActiveTimer_Tick(object state)
        {
            if (_stickMouseDx != 0 || _stickMouseDy != 0) 
            {
                _dispatcher.SendMouseMove(_stickMouseDx, _stickMouseDy, false, false, false);
            }

            if (_currentBezelCode != -1)
            {
                _bezelHoverTime += 10;
                
                if (_bindingCache.TryGetValue(new InputKey("Any", 5, _currentBezelCode), out var bindings))
                {
                    foreach (var b in bindings) {
                        if (b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey(new TriggerKeyHash(st.Type, st.Code)))) {
                            if (_bezelHoverTime >= b.ConditionParam && _bezelHoverTime - 10 < b.ConditionParam) 
                            { 
                                // 条件を満たしたので実行要求をキューに入れる
                                _inputQueue.Add(new InputEvent { Type = 5, Code = _currentBezelCode, IsDown = true, DeviceIdentifier = "Any" });
                            }
                        }
                    }
                }
            }
        }

        private void UpdateHookBlockList()
        {
            var blockList = new HashSet<long>();
            if (!_isSuspended && _globalHookManager != null)
            {
                var profile = _profileManager.CurrentActiveProfile;
                if (profile != null) { foreach (var b in profile.Bindings) if (b.BlockOriginalInput) blockList.Add(((long)b.InputType << 32) | (uint)b.InputCode); }
            }
            if (_globalHookManager != null) _globalHookManager.SetBlockList(blockList);
        }

        private void UpdateBindingCache()
        {
            var newCache = new Dictionary<InputKey, List<UsbInputMapper.Profiles.Binding>>();
            var profile = _profileManager.CurrentActiveProfile;
            
            if (profile != null) 
            {
                foreach (var b in profile.Bindings) { 
                    var key = new InputKey(b.DeviceIdentifier, b.InputType, b.InputCode); 
                    if (!newCache.TryGetValue(key, out var list)) { list = new List<UsbInputMapper.Profiles.Binding>(); newCache[key] = list; } 
                    list.Add(b); 
                }
                if (profile.EnableXInput) {
                    foreach (var b in _profileManager.ControllerBaseBindings) {
                        var key = new InputKey(b.DeviceIdentifier, b.InputType, b.InputCode);
                        if (!newCache.TryGetValue(key, out var list)) { list = new List<UsbInputMapper.Profiles.Binding>(); newCache[key] = list; }
                        if (!profile.Bindings.Any(pb => pb.DeviceIdentifier == b.DeviceIdentifier && pb.InputType == b.InputType && pb.InputCode == b.InputCode)) list.Add(b);
                    }
                }
            }
            
            _bindingCache = newCache;
            _hasBezelBindings = _bindingCache.Keys.Any(k => k.Type == 5);
        }

        // トレイアイコンやUI周りの初期化（省略）
        private void InitializeTrayIcon() { /* 既存のコードと同様 */ }
        protected override void Dispose(bool disposing) { ShutdownCore(); base.Dispose(disposing); }
    }
}
