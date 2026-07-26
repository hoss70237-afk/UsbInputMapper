using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace UsbInputMapper.Core
{
    public class DiagnosticEvent
    {
        public bool IsPhysical { get; set; }
        public long Timestamp { get; set; }
        public int Type { get; set; }
        public int Code { get; set; }
        public int Value { get; set; }
        public bool IsDown { get; set; }
    }

    public static class InputLogger
    {
        public static event Action<string> OnLog;
        public static event Action<DiagnosticEvent> OnDiagnostic;
        
        public static bool IsLoggingEnabled { get; set; } = false;

        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private static readonly Thread _logThread;
        private static readonly AutoResetEvent _logSignal = new AutoResetEvent(false);
        private static volatile bool _isRunning = true;
        private static readonly string _logFilePath;

        static InputLogger()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string baseFolder = Path.Combine(appData, "UsbInputMapper");
                if (!Directory.Exists(baseFolder)) Directory.CreateDirectory(baseFolder);
                _logFilePath = Path.Combine(baseFolder, "error.log");

                _logThread = new Thread(ProcessLogQueue) { IsBackground = true, Name = "AsyncLoggerThread" };
                _logThread.Start();

                AppDomain.CurrentDomain.ProcessExit += (s, e) => FlushAndStop();
            }
            catch { }
        }

        public static void Log(string message)
        {
            if (IsLoggingEnabled) OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public static void LogError(string message, Exception ex = null)
        {
            string logMsg = $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}] ERROR: {message}";
            if (ex != null) logMsg += $"\n{ex.ToString()}";
            
            _logQueue.Enqueue(logMsg);
            _logSignal.Set();
            
            if (IsLoggingEnabled) OnLog?.Invoke(logMsg);
        }

        public static void LogDiagnostic(DiagnosticEvent evt)
        {
            if (IsLoggingEnabled) OnDiagnostic?.Invoke(evt);
        }

        private static void ProcessLogQueue()
        {
            while (_isRunning || !_logQueue.IsEmpty)
            {
                _logSignal.WaitOne(100);
                while (_logQueue.TryDequeue(out string msg))
                {
                    try
                    {
                        File.AppendAllText(_logFilePath, msg + Environment.NewLine);
                    }
                    catch { }
                }
            }
        }

        public static void FlushAndStop()
        {
            _isRunning = false;
            _logSignal.Set();
            _logThread?.Join(500);
        }
    }
}
