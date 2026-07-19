using System;

namespace UsbInputMapper.Core
{
    public static class InputLogger
    {
        public static event Action<string> OnLog;
        public static bool IsLoggingEnabled { get; set; } = false;

        public static void Log(string message)
        {
            if (IsLoggingEnabled) OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
    }
}
