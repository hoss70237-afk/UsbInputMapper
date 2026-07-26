using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        public static TrayApplicationContext Instance { get; private set; }

        // 引数なしコンストラクタに戻し、Program.cs のエラーを解消
        public TrayApplicationContext()
        {
            Instance = this;
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
        }

        public void ShowMainForm()
        {
            // 開かれているMainFormを探して表示する
            foreach (Form form in Application.OpenForms)
            {
                if (form is MainForm mainForm)
                {
                    mainForm.Show();
                    if (mainForm.WindowState == FormWindowState.Minimized)
                    {
                        mainForm.WindowState = FormWindowState.Normal;
                    }
                    mainForm.Activate();
                    return;
                }
            }
        }

        private void TriggerPanic()
        {
            OutputDispatcher.Instance?.ReleaseAllInputs();
            _trayIcon.ShowBalloonTip(2000, "緊急停止", "すべての仮想入力をリセットし、キーを解放しました。", ToolTipIcon.Warning);
            InputLogger.Log("Panic Button Triggered by User.");
        }

        public void ShowToggleNotification(string actionName, bool isOn)
        {
            string state = isOn ? "ON" : "OFF";
            _trayIcon.ShowBalloonTip(1000, "トグル状態変更", $"{actionName} が {state} になりました。", ToolTipIcon.Info);
        }

        private void ExitApplication()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            OutputDispatcher.Instance?.ReleaseAllInputs(); 
            HidHideManager.EnableHiding(false); 
            Application.Exit();
        }
    }
}
