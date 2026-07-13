using System;
using System.Collections.Concurrent;
using System.Drawing;
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

        // 状態管理
        private int _currentLayer = 0; // 0=通常, 1〜5=レイヤー
        private ConcurrentDictionary<string, CancellationTokenSource> _activeLoops = new ConcurrentDictionary<string, CancellationTokenSource>();
        private ConcurrentDictionary<string, bool> _toggleStates = new ConcurrentDictionary<string, bool>();

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
            
            // 現在のレイヤーに対応するバインディングを探す（なければ通常のレイヤー0を探す）
            var binding = _profileManager.FindBinding(e.DeviceIdentifier, e.Type, inputCode);
            if (binding == null) return; // 割り当てなし

            string loopKey = $"{e.DeviceIdentifier}_{e.Type}_{inputCode}";
            bool isDown = (e.Type == 1 && e.IsKeyDown) || (e.Type == 0);

            if (isDown)
            {
                // レイヤー切り替えボタンを押した場合
                if (binding.Action.ActionType == ActionType.LayerShift)
                {
                    _currentLayer = binding.Action.ArgumentNum;
                    return;
                }

                // すでにループが回っている場合は二重起動を防ぐ
                if (_activeLoops.ContainsKey(loopKey)) return;

                var cts = new CancellationTokenSource();
                _activeLoops[loopKey] = cts;

                Task.Run(async () =>
                {
                    try
                    {
                        // 1. 長押し判定
                        if (binding.Condition == TriggerCondition.Hold)
                        {
                            await Task.Delay(binding.ConditionParam, cts.Token);
                            ExecuteAction(binding.Action, true);
                        }
                        // 2. 連打モード
                        else if (binding.Condition == TriggerCondition.RapidFire)
                        {
                            while (!cts.Token.IsCancellationRequested)
                            {
                                ExecuteAction(binding.Action, true);
                                await Task.Delay(20);
                                ExecuteAction(binding.Action, false);
                                await Task.Delay(Math.Max(10, binding.ConditionParam), cts.Token);
                            }
                        }
                        // 3. 通常入力 または トグル
                        else
                        {
                            if (binding.Action.ActionType == ActionType.ToggleHold)
                            {
                                bool currentState = _toggleStates.GetOrAdd(loopKey, false);
                                _toggleStates[loopKey] = !currentState;
                                ExecuteActionToggleTarget(binding.Action, !currentState);
                            }
                            else
                            {
                                ExecuteAction(binding.Action, true);
                            }
                        }
                    }
                    catch (TaskCanceledException) { /* 離されたら終了 */ }
                }, cts.Token);
            }
            else
            {
                // キーを離した時の処理
                if (binding.Action.ActionType == ActionType.LayerShift)
                {
                    _currentLayer = 0; // レイヤー元に戻す
                    return;
                }

                if (_activeLoops.TryRemove(loopKey, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                // トグルモードでなければキーアップを送信
                if (binding.Action.ActionType != ActionType.ToggleHold)
                {
                    ExecuteAction(binding.Action, false);
                }
            }
        }

        private void ExecuteAction(ActionDef action, bool isDown)
        {
            if (action.ActionType == ActionType.Macro && isDown)
            {
                // マクロはディスパッチャー内で別スレッドで処理させるなどの拡張が必要ですが、
                // ここでは簡単のために一括実行します。
                Task.Run(() => {
                    foreach (var step in action.MacroSteps)
                    {
                        Thread.Sleep(step.DelayMs);
                        var tempDef = new ActionDef { 
                            ActionType = step.ActionType, ArgumentNum = step.ArgumentNum, ArgumentStr = step.ArgumentStr,
                            MouseX = step.MouseX, MouseY = step.MouseY, IsAbsolutePosition = step.IsAbsolutePosition
                        };
                        _dispatcher.Dispatch(tempDef);
                    }
                });
                return;
            }
            
            // キーボードダウン・アップの制御はディスパッチャ内で行っているためそのまま流す
            if (isDown) _dispatcher.Dispatch(action);
        }

        private void ExecuteActionToggleTarget(ActionDef action, bool isDown)
        {
            // トグルの場合は対象のキーを押しっぱなしにする
            if (action.ActionType == ActionType.Keyboard || action.ActionType == ActionType.ToggleHold)
            {
                _dispatcher.SendKeyboardInput((ushort)action.ArgumentNum, isDown);
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
