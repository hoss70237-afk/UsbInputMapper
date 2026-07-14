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

        private RawInputManager _rawInputManager;
        private ViGEmOutput _viGEmOutput;
        private OutputDispatcher _dispatcher;
        private ProfileManager _profileManager;
        private ForegroundAppWatcher _appWatcher;

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

            _appWatcher = new ForegroundAppWatcher();
            _appWatcher.OnForegroundAppChanged += (s, appPath) => _profileManager.SwitchToAppProfile(appPath);
            _appWatcher.Start();

            _viGEmOutput = new ViGEmOutput();
            _viGEmOutput.Initialize();
            _dispatcher = new OutputDispatcher(_viGEmOutput);

            _rawInputManager = new RawInputManager();
            _rawInputManager.OnInputEvent += RawInputManager_OnInputEvent;
        }

        private void RawInputManager_OnInputEvent(object sender, InputEvent e)
        {
            // キャプチャ画面が開いている場合は入力をそちらに渡して終了
            if (CaptureForm.IsCapturing)
            {
                CaptureForm.CurrentInstance?.ProcessInput(e);
                return;
            }

            int inputCode = (e.Type == 1) ? e.VKey : (int)e.MouseButtonFlags;
            string keyId = $"{e.Type}_{inputCode}";

            // 物理的な押下状態を記録（同時押しの判定に使う）
            if (e.IsKeyDown) _physicalKeysDown[keyId] = true;
            else _physicalKeysDown.TryRemove(keyId, out _);

            // 該当するすべてのアイテムを取得（ボタン被りがあった場合もすべて実行する）
            var bindings = FindAllMatchingBindings(e.DeviceIdentifier, e.Type, inputCode);
            if (bindings.Count == 0) return;

            foreach (var binding in bindings)
            {
                // アイテムごとに一意のループ管理キーを作成
                string loopKey = $"{e.DeviceIdentifier}_{e.Type}_{inputCode}_{binding.GetHashCode()}";

                // ホイール入力（4:上, 5:下）は長押しや離す概念がないため、即座に押して離す
                if (e.Type == 0 && (inputCode == 4 || inputCode == 5))
                {
                    if (e.IsKeyDown)
                    {
                        ExecuteAction(binding.Action, true, CancellationToken.None, loopKey);
                        ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                    }
                    continue; // ホイールの処理はここまで
                }

                if (e.IsKeyDown)
                {
                    // 離した時に発動する設定なら、押した時は何もしない
                    if (binding.Condition == TriggerCondition.Release) continue;

                    if (_activeLoops.ContainsKey(loopKey)) continue;

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
                    // ボタンが離されたらループをキャンセル
                    if (_activeLoops.TryRemove(loopKey, out var cts))
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }

                    // 離した時に発動する設定の場合、ここで1回だけ実行する
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
        }

        private List<UsbInputMapper.Profiles.Binding> FindAllMatchingBindings(string deviceId, int inputType, int inputCode)
        {
            if (_profileManager.CurrentProfile == null) return new List<UsbInputMapper.Profiles.Binding>();
            
            // まずデバイスと入力コードが一致するものを取得
            var bindings = _profileManager.CurrentProfile.Bindings
                .Where(b => b.DeviceIdentifier == deviceId && b.InputType == inputType && b.InputCode == inputCode)
                .ToList();

            if (bindings.Count == 0) return bindings;

            // 同時押し条件(SubTriggers)が設定されている場合、現在それらがすべて押されているか判定
            return bindings.Where(b => 
                b.SubTriggers == null || 
                b.SubTriggers.All(st => _physicalKeysDown.ContainsKey($"{st.Type}_{st.Code}"))
            ).ToList();
        }

        private void ExecuteAction(ActionDef action, bool isDown, CancellationToken token, string loopKey)
        {
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
                            // 連続スピード移動は相対移動として細かく送信し続ける
                            var tempDef = new ActionDef { ActionType = ActionType.MouseMoveRelative, MouseX = action.MouseX, MouseY = action.MouseY };
                            _dispatcher.Dispatch(tempDef, true);
                            await Task.Delay(16); // 約60fps
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

            // 競合防止: ステップ再生モード以外で「押す(Down)」や「離す(Up)」が指定されていた場合は強制的に「タップ(Tap)」として扱う
            StepPressState state = step.PressState;
            if (currentMode != MacroPlaybackMode.StepByStep && (state == StepPressState.Down || state == StepPressState.Up))
            {
                state = StepPressState.Tap;
            }

            if (state == StepPressState.Down)
            {
                _dispatcher.Dispatch(tempDef, true);
            }
            else if (state == StepPressState.Up)
            {
                _dispatcher.Dispatch(tempDef, false);
            }
            else // Tap
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
            if (_mainForm == null || _mainForm.IsDisposed) _mainForm = new MainForm(_profileManager);
            _mainForm.Show(); 
            _mainForm.Activate();
        }

        private void ExitApp(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _rawInputManager?.Dispose();
            _viGEmOutput?.Dispose();
            Application.Exit();
        }
    }
}
