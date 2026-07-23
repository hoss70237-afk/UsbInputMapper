using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace UsbInputMapper.Core
{
    public static class SystemMouseManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const uint SPI_GETMOUSESPEED = 0x0070;
        private const uint SPI_SETMOUSESPEED = 0x0071;
        private const uint SPI_GETWHEELSCROLLLINES = 0x0068;
        private const uint SPI_SETWHEELSCROLLLINES = 0x0069;
        private const uint SPI_SETCURSORS = 0x0057;

        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        private static uint _originalSpeed = 10;
        private static uint _originalScrollLines = 3;
        
        private static bool _isSpeedModified = false;
        private static bool _isScrollModified = false;
        public static bool IsCursorHidden { get; private set; } = false;

        public static int OffsetX { get; set; } = 0;
        public static int OffsetY { get; set; } = 0;
        public static bool IsOffsetActive => OffsetX != 0 || OffsetY != 0;

        static SystemMouseManager()
        {
            // 起動時に現在の安全な設定を保存
            uint speed = 0;
            if (SystemParametersInfo(SPI_GETMOUSESPEED, 0, ref speed, 0)) _originalSpeed = speed;

            uint scroll = 0;
            if (SystemParametersInfo(SPI_GETWHEELSCROLLLINES, 0, ref scroll, 0)) _originalScrollLines = scroll;

            // アプリ異常終了時に確実に元に戻すためのセーフティネット
            AppDomain.CurrentDomain.ProcessExit += (s, e) => RestoreAllSafely();
            AppDomain.CurrentDomain.UnhandledException += (s, e) => RestoreAllSafely();
        }

        public static void SetMouseSpeed(int speed)
        {
            if (speed < 1) speed = 1; if (speed > 20) speed = 20;
            uint val = (uint)speed;
            SystemParametersInfo(SPI_SETMOUSESPEED, 0, (IntPtr)val, SPIF_SENDCHANGE);
            _isSpeedModified = true;
        }

        public static void SetScrollLines(int lines)
        {
            if (lines < 1) lines = 1; if (lines > 100) lines = 100;
            uint val = (uint)lines;
            SystemParametersInfo(SPI_SETWHEELSCROLLLINES, val, IntPtr.Zero, SPIF_SENDCHANGE);
            _isScrollModified = true;
        }

        public static void HideCursor()
        {
            if (IsCursorHidden) return;
            
            // 1x1の透明なカーソルを生成してシステム全体に適用する
            byte[] andMask = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] xorMask = new byte[] { 0x00, 0x00, 0x00, 0x00 };

            IntPtr hAndMask = CreateBitmap(1, 1, 1, 1, Marshal.UnsafeAddrOfPinnedArrayElement(andMask, 0));
            IntPtr hXorMask = CreateBitmap(1, 1, 1, 1, Marshal.UnsafeAddrOfPinnedArrayElement(xorMask, 0));

            ICONINFO iconInfo = new ICONINFO
            {
                fIcon = false,
                xHotspot = 0,
                yHotspot = 0,
                hbmMask = hAndMask,
                hbmColor = hXorMask
            };

            IntPtr hTransparentCursor = CreateIconIndirect(ref iconInfo);

            DeleteObject(hAndMask);
            DeleteObject(hXorMask);

            // 代表的なカーソルIDを全て透明カーソルで上書き
            uint[] cursorIds = { 32512, 32513, 32514, 32515, 32516, 32640, 32641, 32642, 32643, 32644, 32645, 32646, 32648, 32649, 32650, 32651 };
            
            foreach (uint id in cursorIds)
            {
                IntPtr hCurCopy = CopyIcon(hTransparentCursor);
                SetSystemCursor(hCurCopy, id);
            }
            
            DeleteObject(hTransparentCursor);
            IsCursorHidden = true;
        }

        public static void ShowCursor()
        {
            if (!IsCursorHidden) return;
            // OSのデフォルトカーソルをリロードして元に戻す
            SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);
            IsCursorHidden = false;
        }

        public static void SetCursorOffset(int x, int y)
        {
            OffsetX = x;
            OffsetY = y;
        }

        public static void ClearCursorOffset()
        {
            OffsetX = 0;
            OffsetY = 0;
        }

        public static void RestoreAllSafely()
        {
            if (_isSpeedModified)
            {
                SystemParametersInfo(SPI_SETMOUSESPEED, 0, (IntPtr)_originalSpeed, SPIF_SENDCHANGE);
                _isSpeedModified = false;
            }

            if (_isScrollModified)
            {
                SystemParametersInfo(SPI_SETWHEELSCROLLLINES, _originalScrollLines, IntPtr.Zero, SPIF_SENDCHANGE);
                _isScrollModified = false;
            }

            if (IsCursorHidden)
            {
                ShowCursor();
            }

            ClearCursorOffset();
        }
    }
}
