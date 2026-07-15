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
            // 通常のSystem32パス
            string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            // 64bitOS環境で32bitアプリとして動いたとき用のSysnativeパス
            string sysnative = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");
            
            string driverPath1 = Path.Combine(system32, @"drivers\ViGEmBus.sys");
            string driverPath2 = Path.Combine(sysnative, @"drivers\ViGEmBus.sys");

            if (!File.Exists(driverPath1) && !File.Exists(driverPath2))
            {
                var result = MessageBox.Show(
                    "ViGEmBusドライバが見つかりません。\r\n" +
                    "仮想コントローラー(Xbox出力)機能を使用するには、ViGEmBusのインストールが必要です。\r\n\r\n" +
                    "※キーボードやマウスの出力のみを使用する場合は、このまま「はい」を押して続行できます。\r\n\r\n" +
                    "起動を続行しますか？",
                    "UsbInputMapper - 環境警告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                return result == DialogResult.Yes;
            }

            return true;
        }
    }
}
