using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public event EventHandler<string> OnForegroundAppChanged;
        
        private string _lastAppPath = string.Empty;
        private IntPtr _lastHwnd = IntPtr.Zero;
        
        private Thread _workerThread;
        private volatile bool _isRunning = false;

        public ForegroundAppWatcher()
        {
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _workerThread = new Thread(PollingLoop) { IsBackground = true, Priority = ThreadPriority.Lowest, Name = "ForegroundWatcherThread" };
            _workerThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private void PollingLoop()
        {
            while (_isRunning)
            {
                try
                {
                    CheckCurrentForeground();
                }
                catch { }
                Thread.Sleep(500);
            }
        }

        private void CheckCurrentForeground()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            if (hwnd == _lastHwnd && !string.IsNullOrEmpty(_lastAppPath)) return;

            IntPtr originalHwnd = hwnd;
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
            
            if (!string.IsNullOrEmpty(currentAppPath))
            {
                _lastHwnd = originalHwnd;
                if (!string.Equals(currentAppPath, _lastAppPath, StringComparison.OrdinalIgnoreCase))
                {
                    _lastAppPath = currentAppPath;
                    OnForegroundAppChanged?.Invoke(this, currentAppPath);
                }
            }
            else
            {
                _lastHwnd = IntPtr.Zero;
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
            _workerThread?.Join(1000);
        }
    }
}
