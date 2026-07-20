using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UsbInputMapper.Core
{
    public class GlobalHookManager : IDisposable
    {
        public static GlobalHookManager Instance { get; private set; }

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        private const uint LLKHF_INJECTED = 0x00000010;
        private const uint LLMHF_INJECTED = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT { public uint vkCode; public uint scanCode; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        private delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;
        private LowLevelHookProc _keyboardProc;
        private LowLevelHookProc _mouseProc;

        private HashSet<long> _blockList = new HashSet<long>();
        private Dictionary<long, long> _recentBlocked = new Dictionary<long, long>();

        public bool IsRecording { get; set; }
        public bool IsCoordinateCapturing { get; private set; }
        private Action<POINT, bool> _coordinateCaptureCallback;
        private bool _waitingForUp = false;
        private bool _waitingForRightUp = false;
        private POINT _capturePoint;

        public bool IsRadialMenuClickCapturing { get; set; }
        public Action OnRadialMenuClickCaptured;

        public event EventHandler<HookInputEvent> OnRecordedInput;
        public event EventHandler<HookInputEvent> OnBlockedInputFired; // ★追加: ブロックしたキーを通知

        public class HookInputEvent
        {
            public int Type { get; set; }
            public int Code { get; set; }
            public bool IsDown { get; set; }
            public int X { get; set; } 
            public int Y { get; set; }
            public long Timestamp { get; set; }
        }

        public GlobalHookManager()
        {
            Instance = this;
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hMod = GetModuleHandle(curModule.ModuleName);
                _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
                _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            }
        }

        private long GetHookKey(int type, int code) => ((long)type << 32) | (uint)code;
        public void SetBlockList(HashSet<long> blockList) { _blockList = blockList ?? new HashSet<long>(); }
        public bool WasRecentlyBlocked(int type, int code) { long key = GetHookKey(type, code); if (_recentBlocked.TryGetValue(key, out long time)) { if (Environment.TickCount - time < 200) return true; } return false; }

        public void StartCoordinateCapture(Action<POINT, bool> onCaptured) { _coordinateCaptureCallback = onCaptured; IsCoordinateCapturing = true; _waitingForUp = false; _waitingForRightUp = false; }
        public void StopCoordinateCapture() { IsCoordinateCapturing = false; _coordinateCaptureCallback = null; }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                bool isInjected = (kb.flags & LLKHF_INJECTED) != 0;
                int msg = wParam.ToInt32();
                bool isDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                int vkCode = (int)kb.vkCode;
                
                if (IsRecording) OnRecordedInput?.Invoke(this, new HookInputEvent { Type = 1, Code = vkCode, IsDown = isDown, Timestamp = Environment.TickCount });
                
                if (!isInjected)
                {
                    long key = GetHookKey(1, vkCode);
                    if (_blockList.Contains(key)) 
                    { 
                        _recentBlocked[key] = Environment.TickCount;
                        // ★ ブロックと同時に自前のロジックを発火させるための通知
                        OnBlockedInputFired?.Invoke(this, new HookInputEvent { Type = 1, Code = vkCode, IsDown = isDown, Timestamp = Environment.TickCount });
                        return (IntPtr)1; 
                    }
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var ms = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                bool isInjected = (ms.flags & LLMHF_INJECTED) != 0;
                int msg = wParam.ToInt32();
                
                int code = -1;
                bool isDown = false;
                if (msg == WM_LBUTTONDOWN) { code = 1; isDown = true; } else if (msg == WM_LBUTTONUP) { code = 1; isDown = false; } else if (msg == WM_RBUTTONDOWN) { code = 2; isDown = true; } else if (msg == WM_RBUTTONUP) { code = 2; isDown = false; } else if (msg == WM_MBUTTONDOWN) { code = 3; isDown = true; } else if (msg == WM_MBUTTONUP) { code = 3; isDown = false; } else if (msg == WM_MOUSEWHEEL) { short wheelData = (short)(ms.mouseData >> 16); code = wheelData > 0 ? 4 : 5; isDown = true; } else if (msg == WM_XBUTTONDOWN || msg == WM_XBUTTONUP) { isDown = (msg == WM_XBUTTONDOWN); int xButton = (int)(ms.mouseData >> 16); code = xButton == 1 ? 6 : 7; }

                if (!isInjected)
                {
                    if (IsRadialMenuClickCapturing && (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN))
                    {
                        IsRadialMenuClickCapturing = false;
                        OnRadialMenuClickCaptured?.Invoke();
                        return (IntPtr)1; 
                    }

                    if (IsCoordinateCapturing)
                    {
                        if (msg == WM_LBUTTONDOWN) { _capturePoint = ms.pt; _waitingForUp = true; return (IntPtr)1; }
                        else if (msg == WM_LBUTTONUP && _waitingForUp) { _waitingForUp = false; IsCoordinateCapturing = false; _coordinateCaptureCallback?.Invoke(_capturePoint, false); return (IntPtr)1; }
                        else if (msg == WM_RBUTTONDOWN) { _waitingForRightUp = true; return (IntPtr)1; }
                        else if (msg == WM_RBUTTONUP && _waitingForRightUp) { _waitingForRightUp = false; IsCoordinateCapturing = false; _coordinateCaptureCallback?.Invoke(ms.pt, true); return (IntPtr)1; }
                    }

                    if (code != -1)
                    {
                        if (IsRecording) OnRecordedInput?.Invoke(this, new HookInputEvent { Type = 0, Code = code, IsDown = isDown, X = ms.pt.x, Y = ms.pt.y, Timestamp = Environment.TickCount });
                        long key = GetHookKey(0, code);
                        if (_blockList.Contains(key)) 
                        { 
                            _recentBlocked[key] = Environment.TickCount; 
                            // ★ ブロックと同時に自前のロジックを発火させるための通知
                            OnBlockedInputFired?.Invoke(this, new HookInputEvent { Type = 0, Code = code, IsDown = isDown, X = ms.pt.x, Y = ms.pt.y, Timestamp = Environment.TickCount });
                            return (IntPtr)1; 
                        }
                    }
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_keyboardHookID != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHookID);
            if (_mouseHookID != IntPtr.Zero) UnhookWindowsHookEx(_mouseHookID);
        }
    }
}
