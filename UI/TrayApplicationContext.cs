using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private MainForm _mainForm;

        // Core
        private RawInputManager _rawInputManager;
        private InputStateMachine _stateMachine;
        private ViGEmOutput _viGEmOutput;
        private OutputDispatcher _dispatcher;
        private PanicKeyWatcher _panicWatcher;

        // Profiles
        private ProfileManager _profileManager;
        private ForegroundAppWatcher _appWatcher;

        private bool _isPanicMode = false;

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
            _appWatcher.OnForegroundAppChanged += (s, appPath) => 
            {
                if (!_isPanicMode) _profileManager.SwitchToAppProfile(appPath);
            };
            _appWatcher.Start();

            _viGEmOutput = new ViGEmOutput();
            _viGEmOutput.Initialize();
            
            _dispatcher = new OutputDispatcher(_viGEmOutput);

            _stateMachine = new InputStateMachine();

            _panicWatcher = new PanicKeyWatcher();
            _panicWatcher.OnPanicTriggered += (s, e) => 
            {
                _isPanicMode = true;
                _trayIcon.ShowBalloonTip(3000, "パニックモード", "全機能を停止しました。", ToolTipIcon.Warning);
            };

            _rawInputManager = new RawInputManager();
            _rawInputManager.OnInputEvent += RawInputManager_OnInputEvent;
        }

        private void RawInputManager_OnInputEvent(object sender, InputEvent e)
        {
            // キャプチャ中ならメインフォームへ流す
            if (CaptureForm.IsCapturing)
            {
                CaptureForm.CurrentInstance?.ProcessInput(e);
                return;
            }

            if (_isPanicMode) return;

            // パニックキーの監視(マウス入力の場合)
            if (e.Type == 0)
            {
                _panicWatcher.ProcessMouseInput(e.MouseButtonFlags);
            }

            // ステートマシン経由で入力処理
            _stateMachine.Process(e, detectedEvent =>
            {
                // バインディング検索のためのキー・ボタン抽出
                int inputCode = 0;
                if (detectedEvent.Type == 1) inputCode = detectedEvent.VKey;
                else if (detectedEvent.Type == 0) inputCode = (int)detectedEvent.MouseButtonFlags;
                // HIDの場合は本来詳細な解析が必要だが、ここでは簡略化

                var binding = _profileManager.FindBinding(detectedEvent.DeviceIdentifier, detectedEvent.Type, inputCode);
                
                // ダウン状態（押下時）のみ反応させる制御（キーボード等）
                bool isActionTriggered = false;
                if (detectedEvent.Type == 1 && detectedEvent.IsKeyDown) isActionTriggered = true;
                else if (detectedEvent.Type == 0) isActionTriggered = true; // マウスフラグはとりあえず通す

                if (binding != null && isActionTriggered)
                {
                    _dispatcher.Dispatch(binding.Action);
                }
            });
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "UsbInputMapper",
                Visible = true
            };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("設定を開く", null, ShowMainForm);
            menu.Items.Add("再起動 (パニック解除)", null, RestartApp);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("終了", null, ExitApp);

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += ShowMainForm;
        }

        private void ShowMainForm(object sender, EventArgs e)
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                _mainForm = new MainForm(_profileManager);
            }
            _mainForm.Show();
            _mainForm.Activate();
        }

        private void RestartApp(object sender, EventArgs e)
        {
            _isPanicMode = false;
            _trayIcon.ShowBalloonTip(3000, "再起動", "パニック状態を解除し動作を再開しました。", ToolTipIcon.Info);
        }

        private void ExitApp(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _rawInputManager?.Dispose();
            _viGEmOutput?.Dispose();
            _appWatcher?.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _rawInputManager?.Dispose();
                _viGEmOutput?.Dispose();
                _appWatcher?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
