using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using UsbInputMapper.Core;

namespace UsbInputMapper.Profiles
{
    public class ForegroundAppWatcher : IDisposable
    {
        // --- Windows API 宣言 ---
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        // --- メンバ変数 ---
        public event EventHandler<string> OnForegroundAppChanged;
        private Timer _timer;
        private string _lastAppPath = string.Empty;

        public ForegroundAppWatcher()
        {
            // 500ミリ秒間隔でフォアグラウンドウィンドウを監視
            _timer = new Timer(500); 
            _timer.Elapsed += Timer_Elapsed;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;

                // UWPアプリ（ApplicationFrameHost）対策
                StringBuilder className = new StringBuilder(256);
                GetClassName(hwnd, className, className.Capacity);

                if (className.ToString() == "ApplicationFrameWindow")
                {
                    IntPtr realHwnd = IntPtr.Zero;
                    EnumChildWindows(hwnd, (childHwnd, lParam) =>
                    {
                        StringBuilder childClass = new StringBuilder(256);
                        GetClassName(childHwnd, childClass, childClass.Capacity);
                        if (childClass.ToString() == "Windows.UI.Core.CoreWindow")
                        {
                            realHwnd = childHwnd;
                            return false; // 列挙終了
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (realHwnd != IntPtr.Zero)
                    {
                        hwnd = realHwnd;
                    }
                }

                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return;

                string currentAppPath = GetExecutablePathProcessId(pid);
                
                // ★パスが取得できない(null)場合は、ソフトウェアキーボードやUACなどのシステムプロセスであるため、
                // プロファイル切り替えを無視して今のゲーム用プロファイルを「維持」します。
                if (!string.IsNullOrEmpty(currentAppPath) && currentAppPath != _lastAppPath)
                {
                    _lastAppPath = currentAppPath;
                    OnForegroundAppChanged?.Invoke(this, currentAppPath);
                }
            }
            catch (Exception ex)
            {
                InputLogger.Log($"[ForegroundAppWatcher Error] {ex.Message}");
            }
        }

        private string GetExecutablePathProcessId(uint pid)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProcess == IntPtr.Zero) return null;

            try
            {
                StringBuilder sb = new StringBuilder(1024);
                uint size = (uint)sb.Capacity;
                if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                {
                    return sb.ToString();
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }
            return null;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
