using System;
using System.Diagnostics;
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
            // 【最適化7】プロセスの優先度をHighに設定し、システム高負荷時でも入力変換が遅れないようにする
            // (RealTimeはOS全体の不全を起こす危険があるためHighに留めます)
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            }
            catch (Exception ex)
            {
                InputLogger.LogError("Failed to set process priority.", ex);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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

            if (!SingleInstance.Initialize("UsbInputMapper_Unique_Mutex_7A8B9C"))
            {
                MessageBox.Show("UsbInputMapper は既に起動しています。", "UsbInputMapper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!PrerequisiteChecker.CheckAll())
            {
                SingleInstance.Release();
                return;
            }

            try
            {
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
