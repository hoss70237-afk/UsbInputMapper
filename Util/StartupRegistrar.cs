using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UsbInputMapper.Util
{
    public static class StartupRegistrar
    {
        private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "UsbInputMapper";

        public static bool IsRegistered()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(AppName);
                        if (value != null)
                        {
                            string expectedPath = $"\"{Application.ExecutablePath}\"";
                            return value.ToString().Equals(expectedPath, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            catch
            {
                // 権限エラーなどは未登録として扱う
            }
            return false;
        }

        public static void Register(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string path = $"\"{Application.ExecutablePath}\"";
                            key.SetValue(AppName, path);
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"スタートアップ登録の変更に失敗しました。\r\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
