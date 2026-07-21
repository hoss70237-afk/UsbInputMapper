using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UsbInputMapper.Profiles
{
    public class ForegroundAppWatcher : IDisposable
    {
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

        // ★ WinEventHook 用の定義
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public event EventHandler<string> OnForegroundAppChanged;
        
        private WinEventDelegate _winEventProc;
        private IntPtr _hWinEventHook = IntPtr.Zero;
        private string _lastAppPath = string.Empty;

        public ForegroundAppWatcher()
        {
            // デリゲートがGCに回収されないように保持
            _winEventProc = new WinEventDelegate(WinEventCallback);
        }

        public void Start()
        {
            if (_hWinEventHook == IntPtr.Zero)
            {
                // アクティブウィンドウが変わった瞬間だけOSから通知を受け取る (タイマー不要、CPU 0%)
                _hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
                CheckCurrentForeground(); // 起動時の初回チェック
            }
        }

        public void Stop()
        {
            if (_hWinEventHook != IntPtr.Zero)
            {
                UnhookWinEvent(_hWinEventHook);
                _hWinEventHook = IntPtr.Zero;
            }
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            CheckCurrentForeground();
        }

        private void CheckCurrentForeground()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

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
                        return false; 
                    }
                    return true;
                }, IntPtr.Zero);

                if (realHwnd != IntPtr.Zero) hwnd = realHwnd;
            }

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return;

            string currentAppPath = GetExecutablePathProcessId(pid);
            if (!string.IsNullOrEmpty(currentAppPath) && currentAppPath != _lastAppPath)
            {
                _lastAppPath = currentAppPath;
                OnForegroundAppChanged?.Invoke(this, currentAppPath);
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
            Stop();
        }
    }
}
