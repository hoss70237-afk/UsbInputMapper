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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
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
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000; // 32bit/64bitを越えて取得するための権限

        public event EventHandler<string> OnForegroundAppChanged;
        
        private readonly WinEventDelegate _winEventProc;
        private IntPtr _hWinEventHookFore = IntPtr.Zero;
        private IntPtr _hWinEventHookMin = IntPtr.Zero;
        
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
                if (_hWinEventHookFore == IntPtr.Zero)
                {
                    // フォアグラウンド変更イベント
                    _hWinEventHookFore = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
                    
                    // 最小化・復元イベント
                    _hWinEventHookMin = SetWinEventHook(EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
                    
                    _queue.Add(GetForegroundWindow());
                }
            }
        }

        public void Stop()
        {
            lock (_lockObj)
            {
                if (_hWinEventHookFore != IntPtr.Zero)
                {
                    UnhookWinEvent(_hWinEventHookFore);
                    UnhookWinEvent(_hWinEventHookMin);
                    _hWinEventHookFore = IntPtr.Zero;
                    _hWinEventHookMin = IntPtr.Zero;
                }
            }
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (_isRunning)
            {
                _queue.Add(hwnd != IntPtr.Zero ? hwnd : GetForegroundWindow());
            }
        }

        private void ProcessQueue()
        {
            while (_isRunning)
            {
                IntPtr hwnd = IntPtr.Zero;
                
                // イベントキューに要素がある場合は処理するが、250msイベントが来ない場合はタイムアウトする（定期ポーリング用）
                if (_queue.TryTake(out hwnd, 250))
                {
                    if (hwnd == IntPtr.Zero) hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        try { CheckCurrentForeground(hwnd); } catch { }
                    }
                }
                else
                {
                    // タイムアウト（250ms間新しいイベントが来なかった）場合は、ポーリングとして現在の状態を再確認
                    try { CheckCurrentForeground(GetForegroundWindow()); } catch { }
                }
            }
        }

        private void CheckCurrentForeground(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                NotifyPathChanged(string.Empty);
                return;
            }

            // アイコン化（最小化）されていたり、非表示の場合はプロファイル対象から外す
            if (IsIconic(hwnd) || !IsWindowVisible(hwnd))
            {
                IntPtr fg = GetForegroundWindow();
                if (fg != hwnd && fg != IntPtr.Zero && !IsIconic(fg) && IsWindowVisible(fg))
                {
                    hwnd = fg;
                }
                else
                {
                    NotifyPathChanged(string.Empty);
                    return;
                }
            }

            if (_hwndToPathCache.Count > 1000) _hwndToPathCache.Clear();

            // キャッシュからパスを取得（高速化）
            if (_hwndToPathCache.TryGetValue(hwnd, out string cachedPath))
            {
                NotifyPathChanged(cachedPath);
                return;
            }

            IntPtr originalHwnd = hwnd;

            StringBuilder className = new StringBuilder(256);
            GetClassName(hwnd, className, className.Capacity);

            // Windows 8以降のUWPアプリ（ApplicationFrameWindow）への対応
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
            if (pid == 0)
            {
                NotifyPathChanged(string.Empty);
                return;
            }

            string currentAppPath = GetExecutablePathProcessId(pid);
            if (!string.IsNullOrEmpty(currentAppPath))
            {
                _hwndToPathCache[originalHwnd] = currentAppPath;
                NotifyPathChanged(currentAppPath);
            }
            else
            {
                // アクセス拒否等でパスが取得できない場合（システムプロセス等）
                NotifyPathChanged(string.Empty);
            }
        }

        private void NotifyPathChanged(string newPath)
        {
            if (!string.Equals(newPath, _lastAppPath, StringComparison.OrdinalIgnoreCase))
            {
                _lastAppPath = newPath;
                OnForegroundAppChanged?.Invoke(this, newPath);
            }
        }

        private string GetExecutablePathProcessId(uint pid)
        {
            // PROCESS_QUERY_LIMITED_INFORMATION (0x1000) を使用することで、32bit/64bitのアーキテクチャ境界を越えてプロセスパスを取得可能
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
