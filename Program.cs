using System;
using System.Windows.Forms;
using UsbInputMapper.UI;
using UsbInputMapper.Util;

namespace UsbInputMapper
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 多重起動防止
            if (!SingleInstance.Initialize("UsbInputMapper_Unique_Mutex_7A8B9C"))
            {
                MessageBox.Show("既に起動しています。", "UsbInputMapper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 必須環境チェック(ViGEmなど)
            if (!PrerequisiteChecker.CheckAll())
            {
                // チェックで致命的エラーがあった場合は終了
                SingleInstance.Release();
                return;
            }

            try
            {
                // タスクトレイ常駐型アプリケーションコンテキストで起動
                using (var trayContext = new TrayApplicationContext())
                {
                    Application.Run(trayContext);
                }
            }
            finally
            {
                SingleInstance.Release();
            }
        }
    }
}
