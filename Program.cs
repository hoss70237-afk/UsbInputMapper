using System;
using System.Threading;
using System.Windows.Forms;
using UsbInputMapper.Core;
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

            // ★ 未処理例外のグローバルキャッチとログ記録
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => {
                InputLogger.LogError("Application UI Thread Exception", e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                if (e.ExceptionObject is Exception ex)
                {
                    InputLogger.LogError("AppDomain Unhandled Exception", ex);
                }
            };

            // 多重起動防止
            if (!SingleInstance.Initialize("UsbInputMapper_Unique_Mutex_7A8B9C"))
            {
                MessageBox.Show("UsbInputMapper は既に起動しています。", "UsbInputMapper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 必須環境チェック(ViGEmBus等)
            if (!PrerequisiteChecker.CheckAll())
            {
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
            catch (Exception ex)
            {
                InputLogger.LogError("Fatal Application Crash", ex);
            }
            finally
            {
                SingleInstance.Release();
            }
        }
    }
}
