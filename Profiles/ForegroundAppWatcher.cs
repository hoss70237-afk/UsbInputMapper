using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace UsbInputMapper.Profiles
{
    public class ForegroundAppWatcher : IDisposable
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

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

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public event EventHandler<string> OnForegroundAppChanged;
        
        private readonly WinEventDelegate _winEventProc;
        private IntPtr _hWinEventHook = IntPtr.Zero;
        private string _lastAppPath = string.Empty;
        private readonly object _lockObj = new object();

        private readonly ConcurrentDictionary<IntPtr, string> _hwndToPathCache = new ConcurrentDictionary<IntPtr, string>();

        private BlockingCollection<IntPtr> _queue = new BlockingCollection<IntPtr>();
        private Thread _workerThread;
        private volatile bool _isRunning = true;

        public ForegroundAppWatcher()
        {
            _winEventProc = new WinEventDelegate(WinEventCallback);
            _workerThread = new Thread(ProcessQueue) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "ForegroundWatcherThread" };
            _workerThread.Start();
        }

        public void Start()
        {
            lock (_lockObj)
            {
                if (_hWinEventHook == IntPtr.Zero)
                {
                    _hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
                    _queue.Add(GetForegroundWindow());
                }
            }
        }

        public void Stop()
        {
            lock (_lockObj)
            {
                if (_hWinEventHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_hWinEventHook);
                    _hWinEventHook = IntPtr.Zero;
                }
            }
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && _isRunning)
            {
                _queue.Add(hwnd);
            }
        }

        private void ProcessQueue()
        {
            foreach (var hwnd in _queue.GetConsumingEnumerable())
            {
                if (!_isRunning) break;
                try { CheckCurrentForeground(hwnd); } catch { }
            }
        }

        private void CheckCurrentForeground(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            if (IsIconic(hwnd) || !IsWindowVisible(hwnd)) return;

            if (_hwndToPathCache.Count > 1000) _hwndToPathCache.Clear();

            if (_hwndToPathCache.TryGetValue(hwnd, out string cachedPath))
            {
                if (!string.Equals(cachedPath, _lastAppPath, StringComparison.OrdinalIgnoreCase))
                {
                    _lastAppPath = cachedPath;
                    OnForegroundAppChanged?.Invoke(this, cachedPath);
                }
                return;
            }

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
                _hwndToPathCache[originalHwnd] = currentAppPath;

                if (!string.Equals(currentAppPath, _lastAppPath, StringComparison.OrdinalIgnoreCase))
                {
                    _lastAppPath = currentAppPath;
                    OnForegroundAppChanged?.Invoke(this, currentAppPath);
                }
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
            _isRunning = false;
            _queue.CompleteAdding();
            _workerThread?.Join(500);
            _queue.Dispose();
        }
    }
}
