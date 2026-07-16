using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing; // ★コンパイルエラー原因: System.Drawing の不足を修正
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

            // 仮想コントローラーは起動時に1回だけ接続し、切断しない（放置状態）
            _viGEmOutput = new ViGEmOutput();
            _viGEmOutput.Initialize();
            _dispatcher = new OutputDispatcher(_viGEmOutput);

            _globalHookManager = new GlobalHookManager();

            _rawInputManager = new RawInputManager();
            _rawInputManager.OnInputEvent += RawInputManager_OnInputEvent;

            // DirectInputManagerの初期化
            _diManager = new DirectInputManager();
            _diManager.OnInputEvent += DiManager_OnInputEvent;
        }

        private void DiManager_OnInputEvent(object sender, DirectInputEvent e)
        {
            if (CaptureForm.IsCapturing) return;

            var profile = _profileManager.CurrentActiveProfile;
            if (profile == null) return;

            var bindings = profile.Bindings.Where(b => b.DeviceIdentifier == e.DeviceIdentifier && b.InputType == e.Type && b.InputCode == e.Code).ToList();

            foreach (var b in bindings)
            {
                // XInput出力がOFFのプロファイルではスルーする
                if (!profile.EnableXInput && 
                   (b.Action.ActionType == ActionType.XboxController || b.Action.ActionType == ActionType.XboxAxis || b.Action.ActionType == ActionType.XboxTrigger))
                {
                    continue; 
                }

                if (e.Type == 11) // アナログ軸 (0-65535)
                {
                    ProcessAnalogAxis(b, e.Value);
                }
                else if (e.Type == 10) // ボタン
                {
                    if (b.Action.ActionType == ActionType.XboxController)
                    {
                        _viGEmOutput.SetButton(GetXboxButton(b.Action.ArgumentNum), e.IsDown);
                    }
                }
            }
        }

        private void ProcessAnalogAxis(UsbInputMapper.Profiles.Binding binding, int rawValue)
        {
            // DirectInput 0〜65535 を % に変換 (-1.0 〜 1.0)
            double normalized = (rawValue - 32767.5) / 32767.5;
            if (binding.InvertAxis) normalized *= -1;

            // デッドゾーン処理 (0〜50%)
            double deadZone = binding.DeadZone / 100.0;
            if (Math.Abs(normalized) < deadZone)
            {
                normalized = 0;
            }
            else
            {
                // デッドゾーンを超えた分を再スケーリング
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
                    case 2: axis = Xbox360Axis.LeftThumbY; outValue = (short)-outValue; break; // Yは反転が基本
                    case 3: axis = Xbox360Axis.RightThumbX; break;
                    case 4: axis = Xbox360Axis.RightThumbY; outValue = (short)-outValue; break;
                }
                _viGEmOutput.SetAxis(axis, outValue);
            }
            else if (binding.Action.ActionType == ActionType.XboxTrigger)
            {
                // トリガーは 0.0〜1.0 にして 0〜255 にマッピング
                double trigNorm = (normalized + 1.0) / 2.0; 
                byte outValue = (byte)(trigNorm * 255);
                Xbox360Slider slider = binding.Action.ArgumentNum == 1 ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger;
                _viGEmOutput.SetSlider(slider, outValue);
            }
        }

        private Xbox360Button GetXboxButton(int id)
        {
            switch(id)
            {
                case 1: return Xbox360Button.A; case 2: return Xbox360Button.B; case 3: return Xbox360Button.X; case 4: return Xbox360Button.Y;
                case 5: return Xbox360Button.LeftShoulder; case 6: return Xbox360Button.RightShoulder; case 7: return Xbox360Button.Back; case 8: return Xbox360Button.Start;
                case 9: return Xbox360Button.LeftThumb; case 10: return Xbox360Button.RightThumb; case 11: return Xbox360Button.Up; case 12: return Xbox360Button.Down;
                case 13: return Xbox360Button.Left; case 14: return Xbox360Button.Right; case 15: return Xbox360Button.Guide; default: return Xbox360Button.A;
            }
        }

        private void RawInputManager_OnInputEvent(object sender, InputEvent e)
        {
            // キーボードやマウスは現時点では省略せず、そのまま放置
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
