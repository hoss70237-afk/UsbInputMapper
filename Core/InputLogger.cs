// FILE: Core/InputLogger.cs
using System;

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

        public static void Log(string message)
        {
            if (IsLoggingEnabled) OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public static void LogDiagnostic(DiagnosticEvent evt)
        {
            if (IsLoggingEnabled) OnDiagnostic?.Invoke(evt);
        }
    }
}
