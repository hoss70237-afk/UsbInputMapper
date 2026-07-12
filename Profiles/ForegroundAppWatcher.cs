using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

namespace UsbInputMapper.Profiles
{
    public class ForegroundAppWatcher : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return;

            try
            {
                using (Process proc = Process.GetProcessById((int)pid))
                {
                    string currentAppPath = proc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(currentAppPath) && currentAppPath != _lastAppPath)
                    {
                        _lastAppPath = currentAppPath;
                        OnForegroundAppChanged?.Invoke(this, currentAppPath);
                    }
                }
            }
            catch
            {
                // セキュリティ制限（管理者権限で実行されている他プロセス等）により
                // MainModuleが取得できない場合は無視する
            }
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
