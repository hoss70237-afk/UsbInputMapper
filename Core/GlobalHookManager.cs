using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace UsbInputMapper.Core
{
    public unsafe class GlobalHookManager : IDisposable
    {
        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

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
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        private const uint LLKHF_INJECTED = 0x00000010;

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

        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;
        private LowLevelHookProc _keyboardProc;
        private LowLevelHookProc _mouseProc;
        private bool _requireMouseHook = false;

        private ConcurrentDictionary<long, byte> _blockList = new ConcurrentDictionary<long, byte>();
        private ConcurrentDictionary<long, long> _recentBlocked = new ConcurrentDictionary<long, long>();
        private ConcurrentDictionary<int, long> _lastKeyClickTime = new ConcurrentDictionary<int, long>();

        public bool EnableChatteringCanceler { get; set; } = false;
        public int ChatteringThresholdMs { get; set; } = 20;
        public int BlockedChatterCount { get; private set; } = 0; 

        public bool IsRecording { get; set; }
        public bool IsCoordinateCapturing { get; private set; }
        private Action<POINT, bool> _coordinateCaptureCallback;
        private bool _waitingForUp = false;
        private bool _waitingForRightUp = false;
        private POINT _capturePoint;

        public event EventHandler<HookInputEvent> OnRecordedInput;
        public event EventHandler<HookInputEvent> OnBlockedInputFired;

        private Thread _notifyThread;
        private volatile bool _isRunning = true;
        private AutoResetEvent _notifyEvent = new AutoResetEvent(false);
        private ConcurrentQueue<Action> _eventQueue = new ConcurrentQueue<Action>();

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

            IntPtr hMod = Marshal.GetHINSTANCE(typeof(GlobalHookManager).Module);
            _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hMod, 0);

            _notifyThread = new Thread(NotifyLoop) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "HookNotifyThread" };
            _notifyThread.Start();
        }

        private void NotifyLoop()
        {
            while (_isRunning)
            {
                _notifyEvent.WaitOne(100);
                while (_eventQueue.TryDequeue(out var action))
                {
                    try { action(); } catch { }
                }
            }
        }

        private void EnqueueEvent(Action action)
        {
            _eventQueue.Enqueue(action);
            _notifyEvent.Set();
        }

        private long GetHookKey(int type, int code) => ((long)type << 32) | (uint)code;

        private void UpdateMouseHookState()
        {
            bool shouldBeHooked = _requireMouseHook || IsCoordinateCapturing;
            
            if (shouldBeHooked && _mouseHookID == IntPtr.Zero)
            {
                IntPtr hMod = Marshal.GetHINSTANCE(typeof(GlobalHookManager).Module);
                _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            }
            else if (!shouldBeHooked && _mouseHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }
        }

        public void SetBlockList(HashSet<long> blockList, bool requireMouseHook) 
        { 
            _blockList.Clear();
            if (blockList != null)
            {
                foreach (var item in blockList) _blockList.TryAdd(item, 1);
            }
            _requireMouseHook = requireMouseHook;
            UpdateMouseHookState();
        }

        public void ResetChatterCount() { BlockedChatterCount = 0; }

        public void StartCoordinateCapture(Action<POINT, bool> onCaptured) 
        { 
            _coordinateCaptureCallback = onCaptured; 
            IsCoordinateCapturing = true; 
            _waitingForUp = false; 
            _waitingForRightUp = false; 
            UpdateMouseHookState();
        }
        
        public void StopCoordinateCapture() 
        { 
            IsCoordinateCapturing = false; 
            _coordinateCaptureCallback = null; 
            UpdateMouseHookState();
        }

        private int CalculateBezelCode(int x, int y)
        {
            int sW = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int sH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            int m = 25;

            bool isLeft = x < m;
            bool isRight = x > sW - m;
            bool isTop = y < m;
            bool isBottom = y > sH - m;

            if (!isLeft && !isRight && !isTop && !isBottom) return -1;

            if (isLeft && isTop) return 0;
            if (isRight && isTop) return 4;
            if (isRight && isBottom) return 8;
            if (isLeft && isBottom) return 12;

            if (isTop) { if (x < sW / 3) return 1; if (x < (sW * 2) / 3) return 2; return 3; }
            if (isRight) { if (y < sH / 3) return 5; if (y < (sH * 2) / 3) return 6; return 7; }
            if (isBottom) { if (x > (sW * 2) / 3) return 9; if (x > sW / 3) return 10; return 11; }
            if (isLeft) { if (y > (sH * 2) / 3) return 13; if (y > sH / 3) return 14; return 15; }

            return -1;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && lParam != IntPtr.Zero)
            {
                try
                {
                    KBDLLHOOKSTRUCT* kb = (KBDLLHOOKSTRUCT*)lParam;
                    bool isInjected = (kb->flags & LLKHF_INJECTED) != 0;
                    int msg = (int)wParam;
                    bool isDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                    int vkCode = (int)kb->vkCode;
                    long now = (long)GetTickCount64();

                    if (!isInjected)
                    {
                        if (isDown && EnableChatteringCanceler) 
                        {
                            if (_lastKeyClickTime.TryGetValue(vkCode, out long lastTime))
                            {
                                if (now - lastTime < ChatteringThresholdMs)
                                {
                                    BlockedChatterCount++;
                                    return (IntPtr)1; 
                                }
                            }
                            _lastKeyClickTime[vkCode] = now;
                        }

                        if (IsRecording)
                        {
                            var evt = new HookInputEvent { Type = 1, Code = vkCode, IsDown = isDown, Timestamp = now };
                            EnqueueEvent(() => OnRecordedInput?.Invoke(this, evt));
                        }

                        long key = GetHookKey(1, vkCode);
                        if (_blockList.ContainsKey(key)) 
                        { 
                            _recentBlocked[key] = now;
                            var evt = new HookInputEvent { Type = 1, Code = vkCode, IsDown = isDown, Timestamp = now };
                            EnqueueEvent(() => OnBlockedInputFired?.Invoke(this, evt));
                            return (IntPtr)1; 
                        }
                    }
                    
                    if (InputLogger.IsLoggingEnabled)
                    {
                        var diagEvt = new DiagnosticEvent { IsPhysical = false, Timestamp = now, Type = 1, Code = vkCode, IsDown = isDown };
                        EnqueueEvent(() => InputLogger.LogDiagnostic(diagEvt));
                    }
                }
                catch (Exception ex)
                {
                    EnqueueEvent(() => InputLogger.LogError("KeyboardHookCallback Exception", ex));
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && lParam != IntPtr.Zero)
            {
                MSLLHOOKSTRUCT* ms = (MSLLHOOKSTRUCT*)lParam;
                int msg = (int)wParam;
                bool isInjected = (ms->flags & LLKHF_INJECTED) != 0;

                if (IsCoordinateCapturing)
                {
                    var hookPt = new POINT { x = ms->pt.x, y = ms->pt.y };
                    if (msg == WM_LBUTTONDOWN) { _capturePoint = hookPt; _waitingForUp = true; return (IntPtr)1; }
                    else if (msg == WM_LBUTTONUP && _waitingForUp) { 
                        _waitingForUp = false; 
                        StopCoordinateCapture(); 
                        EnqueueEvent(() => _coordinateCaptureCallback?.Invoke(_capturePoint, false)); 
                        return (IntPtr)1; 
                    }
                    else if (msg == WM_RBUTTONDOWN) { _waitingForRightUp = true; return (IntPtr)1; }
                    else if (msg == WM_RBUTTONUP && _waitingForRightUp) { 
                        _waitingForRightUp = false; 
                        StopCoordinateCapture(); 
                        EnqueueEvent(() => _coordinateCaptureCallback?.Invoke(hookPt, true)); 
                        return (IntPtr)1; 
                    }
                }

                if (_requireMouseHook && !isInjected)
                {
                    int mouseCode = -1;

                    if (msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP) mouseCode = 1;
                    else if (msg == WM_RBUTTONDOWN || msg == WM_RBUTTONUP) mouseCode = 2;
                    else if (msg == WM_MBUTTONDOWN || msg == WM_MBUTTONUP) mouseCode = 3;
                    else if (msg == WM_MOUSEWHEEL)
                    {
                        short delta = (short)(ms->mouseData >> 16);
                        mouseCode = delta > 0 ? 4 : 5;
                    }
                    else if (msg == WM_MOUSEHWHEEL)
                    {
                        short delta = (short)(ms->mouseData >> 16);
                        mouseCode = delta > 0 ? 8 : 9;
                    }
                    else if (msg == WM_XBUTTONDOWN || msg == WM_XBUTTONUP)
                    {
                        int xbtn = (int)(ms->mouseData >> 16);
                        mouseCode = xbtn == 1 ? 6 : 7;
                    }

                    if (mouseCode != -1)
                    {
                        if (mouseCode == 1 || mouseCode == 2)
                        {
                            int bezelCode = CalculateBezelCode(ms->pt.x, ms->pt.y);
                            if (bezelCode != -1)
                            {
                                long bezelKey = GetHookKey(5, bezelCode);
                                if (_blockList.ContainsKey(bezelKey))
                                {
                                    return (IntPtr)1;
                                }
                            }
                        }

                        long btnKey = GetHookKey(0, mouseCode);
                        if (_blockList.ContainsKey(btnKey))
                        {
                            return (IntPtr)1;
                        }
                    }
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            _isRunning = false;
            _notifyEvent.Set();
            _notifyThread?.Join(500);

            if (_keyboardHookID != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHookID);
            if (_mouseHookID != IntPtr.Zero) UnhookWindowsHookEx(_mouseHookID);
            _notifyEvent.Dispose();
        }
    }
}
