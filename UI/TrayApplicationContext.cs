using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private MainForm _mainForm;
        private OutputDispatcher _dispatcher;

        public TrayApplicationContext(MainForm mainForm, OutputDispatcher dispatcher)
        {
            _mainForm = mainForm;
            _dispatcher = dispatcher;
            
            var menu = new ContextMenuStrip();
            
            var mnuOpen = new ToolStripMenuItem("設定を開く");
            mnuOpen.Click += (s, e) => ShowMainForm();
            
            var mnuPanic = new ToolStripMenuItem("緊急停止 (パニックボタン)");
            mnuPanic.Click += (s, e) => TriggerPanic();
            
            var mnuExit = new ToolStripMenuItem("終了");
            mnuExit.Click += (s, e) => ExitApplication();

            menu.Items.Add(mnuOpen);
            menu.Items.Add(mnuPanic);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(mnuExit);

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Text = "UsbInputMapper",
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => ShowMainForm();
            
            // Xボタンで閉じられた際はタスクトレイに格納
            _mainForm.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing) 
                {
                    e.Cancel = true;
                    _mainForm.Hide();
                }
            };
        }

        public void ShowMainForm()
        {
            _mainForm.Show();
            if (_mainForm.WindowState == FormWindowState.Minimized)
            {
                _mainForm.WindowState = FormWindowState.Normal;
            }
            _mainForm.Activate();
        }

        private void TriggerPanic()
        {
            // すべての仮想キーボード・マウス・XInputの状態を即座に強制リセット
            _dispatcher?.ReleaseAllInputs();
            _trayIcon.ShowBalloonTip(2000, "緊急停止", "すべての仮想入力をリセットし、キーを解放しました。", ToolTipIcon.Warning);
            InputLogger.Log("Panic Button Triggered by User.");
        }

        // OutputDispatcher等から呼び出されるトグル状態可視化用
        public void ShowToggleNotification(string actionName, bool isOn)
        {
            string state = isOn ? "ON" : "OFF";
            _trayIcon.ShowBalloonTip(1000, "トグル状態変更", $"{actionName} が {state} になりました。", ToolTipIcon.Info);
        }

        private void ExitApplication()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            
            // アプリ終了時に押しっぱなしを防ぐクリーンアップ
            _dispatcher?.ReleaseAllInputs(); 
            
            // 物理コントローラーの隠蔽を解除
            HidHideManager.EnableHiding(false); 
            
            Application.Exit();
        }
    }
}
