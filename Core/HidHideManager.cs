using System;
using System.Diagnostics;
using System.IO;

namespace UsbInputMapper.Core
{
    public static class HidHideManager
    {
        private static string GetCliPath()
        {
            // Nefarius HidHideのデフォルトインストールパス
            string path = @"C:\Program Files\Nefarius Software Solutions\HidHide\x64\HidHideCLI.exe";
            return File.Exists(path) ? path : null;
        }

        public static bool IsInstalled => GetCliPath() != null;

        public static void WhitelistCurrentProcess()
        {
            try 
            {
                var cli = GetCliPath();
                if (cli == null) return;
                
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                using (var proc = Process.Start(new ProcessStartInfo {
                    FileName = cli,
                    Arguments = $"--app-reg \"{exePath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                })) 
                {
                    proc?.WaitForExit(2000);
                }
            } 
            catch (Exception ex) 
            {
                InputLogger.LogError("HidHide Whitelist Failed", ex);
            }
        }

        public static void EnableHiding(bool enable)
        {
            try 
            {
                var cli = GetCliPath();
                if (cli == null) return;

                using (var proc = Process.Start(new ProcessStartInfo {
                    FileName = cli,
                    Arguments = enable ? "--cloak-on" : "--cloak-off",
                    CreateNoWindow = true,
                    UseShellExecute = false
                })) 
                {
                    proc?.WaitForExit(2000);
                }
            } 
            catch (Exception ex) 
            {
                InputLogger.LogError("HidHide Cloak Toggle Failed", ex);
            }
        }
    }
}
