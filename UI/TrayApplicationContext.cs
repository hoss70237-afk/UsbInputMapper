using System;
using System.Collections.Concurrent;
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

        // --- 状態管理 ---
        private int _currentLayer = 0; 
        private ConcurrentDictionary<string, bool> _physicalKeysDown = new ConcurrentDictionary<string, bool>();
        
        private ConcurrentDictionary<string, CancellationTokenSource> _activeLoops = new ConcurrentDictionary<string, CancellationTokenSource>();
        private ConcurrentDictionary<string, bool> _toggleStates = new ConcurrentDictionary<string, bool>();
        
        // ステップマクロ用の状態管理 (キーID => [現在のステップ番号, 最終更新時刻のTick])
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
            if (CaptureForm.IsCapturing)
            {
                CaptureForm.CurrentInstance?.ProcessInput(e);
                return;
            }

            int inputCode = (e.Type == 1) ? e.VKey : (int)e.MouseButtonFlags;
            string keyId = $"{e.Type}_{inputCode}";

            // 物理キーの状態を更新
            if (e.IsKeyDown) _physicalKeysDown[keyId] = true;
            else _physicalKeysDown.TryRemove(keyId, out _);

            var binding = FindBestMatchingBinding(e.DeviceIdentifier, e.Type, inputCode);
            if (binding == null) return;

            string loopKey = $"{e.DeviceIdentifier}_{e.Type}_{inputCode}";

            if (e.IsKeyDown)
            {
                if (binding.Action.ActionType == ActionType.LayerShift)
                {
                    _currentLayer = binding.Action.ArgumentNum;
                    return;
                }

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
                if (binding.Action.ActionType == ActionType.LayerShift)
                {
                    _currentLayer = 0;
                    return;
                }

                if (_activeLoops.TryRemove(loopKey, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                if (binding.Action.ActionType != ActionType.ToggleHold)
                {
                    ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                }
            }
        }

        private UsbInputMapper.Profiles.Binding FindBestMatchingBinding(string deviceId, int inputType, int inputCode)
        {
            if (_profileManager.CurrentProfile == null) return null;

            var bindingsInLayer = _profileManager.CurrentProfile.Bindings
                .Where(b => b.DeviceIdentifier == deviceId && b.InputType == inputType && b.InputCode == inputCode)
                .Where(b => b.TargetLayer == 0 || b.TargetLayer == _currentLayer)
                .ToList();

            if (bindingsInLayer.Count == 0) return null;

            foreach (var b in bindingsInLayer.OrderByDescending(b => b.SubInputCode > 0 ? 1 : 0))
            {
                if (b.SubInputCode > 0)
                {
                    string subKeyId = $"{b.SubInputType}_{b.SubInputCode}";
                    if (_physicalKeysDown.ContainsKey(subKeyId)) return b;
                }
                else return b;
            }
            return null;
        }

        // --- 強力なマクロエンジン・連続移動エンジン ---
        private void ExecuteAction(ActionDef action, bool isDown, CancellationToken token, string loopKey)
        {
            if (action.ActionType == ActionType.Macro && action.MacroSteps.Count > 0)
            {
                if (!isDown) return; // マクロ起動のトリガーはダウンのみ

                Task.Run(() => {
                    var steps = action.MacroSteps;

                    // 1. 一括再生 (最後まで必ず再生)
                    if (action.PlaybackMode == MacroPlaybackMode.Sequence)
                    {
                        foreach (var step in steps)
                        {
                            PlayMacroStep(step);
                        }
                    }
                    // 2. 順次再生 (ボタンを離したら中断)
                    else if (action.PlaybackMode == MacroPlaybackMode.Hold)
                    {
                        foreach (var step in steps)
                        {
                            if (token.IsCancellationRequested) break;
                            PlayMacroStep(step);
                        }
                    }
                    // 3. リピート再生 (ボタンを押している間ループ)
                    else if (action.PlaybackMode == MacroPlaybackMode.Repeat)
                    {
                        while (!token.IsCancellationRequested)
                        {
                            foreach (var step in steps)
                            {
                                if (token.IsCancellationRequested) break;
                                PlayMacroStep(step);
                            }
                        }
                    }
                    // 4. ステップ再生 (押すたびに1つ進む)
                    else if (action.PlaybackMode == MacroPlaybackMode.StepByStep)
                    {
                        int currentIndex = 0;
                        long now = Environment.TickCount;

                        if (_macroStepStates.TryGetValue(loopKey, out var state))
                        {
                            if ((now - state.Item2) < action.StepTimeoutMs)
                            {
                                currentIndex = (state.Item1 + 1) % steps.Count;
                            }
                        }
                        
                        _macroStepStates[loopKey] = new Tuple<int, long>(currentIndex, now);
                        PlayMacroStep(steps[currentIndex]);
                    }
                });
                return;
            }
            else if (action.ActionType == ActionType.MouseContinuousMove)
            {
                if (isDown)
                {
                    Task.Run(async () => {
                        while (!token.IsCancellationRequested)
                        {
                            var tempDef = new ActionDef { ActionType = ActionType.MouseMove, MouseX = action.MouseX, MouseY = action.MouseY, IsAbsolutePosition = false };
                            _dispatcher.Dispatch(tempDef, true);
                            await Task.Delay(16); // 約60FPSで滑らかに移動
                        }
                    });
                }
                return;
            }
            
            _dispatcher.Dispatch(action, isDown);
        }

        private void PlayMacroStep(MacroStep step)
        {
            Thread.Sleep(step.DelayMs);
            var tempDef = new ActionDef { 
                ActionType = step.ActionType, ArgumentNum = step.ArgumentNum, ArgumentStr = step.ArgumentStr,
                MouseX = step.MouseX, MouseY = step.MouseY, IsAbsolutePosition = step.IsAbsolutePosition
            };
            
            // 押し・離しを再現
            _dispatcher.Dispatch(tempDef, true);
            Thread.Sleep(20); // ゲームが認識できる最低限の打鍵時間
            _dispatcher.Dispatch(tempDef, false);
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
