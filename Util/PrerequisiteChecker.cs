using System;
using System.IO;
using System.Windows.Forms;

namespace UsbInputMapper.Util
{
    public static class PrerequisiteChecker
    {
        public static bool CheckAll()
        {
            return CheckViGEmBus();
        }

        private static bool CheckViGEmBus()
        {
            // WindowsのシステムディレクトリにあるViGEmBusのドライバファイルを簡易チェック
            string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string driverPath = Path.Combine(system32, @"drivers\ViGEmBus.sys");

            if (!File.Exists(driverPath))
            {
                var result = MessageBox.Show(
                    "ViGEmBusドライバが見つかりません。\r\n仮想ゲームパッドの出力機能が利用できない可能性があります。\r\n\r\n起動を続行しますか？",
                    "UsbInputMapper - 環境警告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                return result == DialogResult.Yes;
            }

            return true;
        }
    }
}
