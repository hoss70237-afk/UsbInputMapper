using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace UsbInputMapper.Core
{
    public static class LayerManager
    {
        public static volatile int CurrentLayer = 0;
    }

    public unsafe class OutputDispatcher
    {
        public static OutputDispatcher Instance { get; private set; }

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(SendInputNative.POINT p);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref SendInputNative.POINT lpPoint);
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int GetDlgCtrlID(IntPtr hwnd);

        private readonly ViGEmOutput _viGEmOutput;
        private readonly ConcurrentStack<SendInputNative.POINT> _mousePositionStack = new ConcurrentStack<SendInputNative.POINT>();
        private readonly Random _random = new Random();

        private readonly ConcurrentDictionary<int, byte> _pressedKeys = new ConcurrentDictionary<int, byte>();
        private readonly ConcurrentDictionary<int, byte> _pressedMouseButtons = new ConcurrentDictionary<int, byte>();
        private readonly ConcurrentDictionary<string, bool> _toggleStates = new ConcurrentDictionary<string, bool>();

        public OutputDispatcher(ViGEmOutput viGEmOutput) 
        { 
            _viGEmOutput = viGEmOutput; 
            Instance = this;
        }

        public void ReleaseAllInputs()
        {
            try
            {
                var keysToRelease = _pressedKeys.Keys.ToList();
                if (keysToRelease.Count > 0) 
                { 
                    SendKeyboardInputs(keysToRelease, false); 
                    _pressedKeys.Clear(); 
                }
                
                var mouseBtnsToRelease = _pressedMouseButtons.Keys.ToList();
                foreach (var mb in mouseBtnsToRelease) 
                { 
                    SendMouseClick(mb, false); 
                }
                _pressedMouseButtons.Clear();
                
                _toggleStates.Clear();
                LayerManager.CurrentLayer = 0;
                _viGEmOutput?.Reset();
            }
            catch (Exception ex) { InputLogger.LogError("Failed to release inputs", ex); }
        }

        public void Dispatch(ActionDef action, bool isDown)
        {
            if (action == null) return;
            try
            {
                if (action.ActionState == 1) { if (!isDown) return; isDown = true; }
                else if (action.ActionState == 2) { if (!isDown) return; isDown = false; }

                switch (action.ActionType)
                {
                    case ActionType.Keyboard:
                        if (action.MultipleKeys != null && action.MultipleKeys.Count > 0) SendKeyboardInputs(action.MultipleKeys, isDown);
                        else SendKeyboardInputs(new List<int> { action.ArgumentNum }, isDown);
                        break;
                        
                    case ActionType.ToggleHold:
                        if (!isDown) return; 
                        string key = $"{action.ArgumentNum}_{string.Join(",", action.MultipleKeys ?? new List<int>())}";
                        bool nextState = !_toggleStates.GetOrAdd(key, false);
                        _toggleStates[key] = nextState;
                        
                        if (action.MultipleKeys != null && action.MultipleKeys.Count > 0) SendKeyboardInputs(action.MultipleKeys, nextState);
                        else SendKeyboardInputs(new List<int> { action.ArgumentNum }, nextState);
                        
                        string actName = action.MultipleKeys?.Count > 0 ? string.Join("+", action.MultipleKeys.Select(k => ((Keys)k).ToString())) : ((Keys)action.ArgumentNum).ToString();
                        UI.ToggleOverlayForm.ShowNotification($"Toggle: {actName}", nextState);
                        break;
                        
                    case ActionType.LayerShift:
                        LayerManager.CurrentLayer = isDown ? action.LayerIndex : 0;
                        UI.ToggleOverlayForm.ShowNotification($"Layer {action.LayerIndex}", isDown);
                        break;

                    case ActionType.MouseClick: SendMouseClick(action.ArgumentNum, isDown); break;
                    case ActionType.MouseMoveRelative: if (isDown) SendMouseMove(action.MouseX, action.MouseY, false, false, action.JiggleCursor); break;
                    case ActionType.MouseMoveAbsoluteDesk: if (isDown) SendMouseMove(action.MouseX, action.MouseY, true, false, action.JiggleCursor); break;
                    case ActionType.MouseMoveAbsoluteWin: if (isDown) SendMouseMove(action.MouseX, action.MouseY, true, true, action.JiggleCursor); break;
                    case ActionType.MouseMoveAbsoluteHoverWin: if (isDown) SendMouseMoveHover(action.MouseX, action.MouseY, action.JiggleCursor); break;
                    case ActionType.MousePosSave: if (isDown && SendInputNative.GetCursorPos(out var pt)) _mousePositionStack.Push(pt); break;
                    case ActionType.MousePosRestore:
                        if (isDown && _mousePositionStack.TryPop(out var popPt)) { SendMouseMove(popPt.X, popPt.Y, true, false, false); } break;
                    case ActionType.AppLaunch: 
                    case ActionType.FileOpen:
                    case ActionType.AhkRun:
                        if (isDown) LaunchApp(action.ArgumentStr, action.ArgumentExtraStr); break;
                    case ActionType.FolderOpen:
                        if (isDown && !string.IsNullOrEmpty(action.ArgumentStr)) { try { Process.Start("explorer.exe", action.ArgumentStr); } catch { } } break;
                    case ActionType.XboxController: _viGEmOutput.SetButton(GetXboxButton(action.ArgumentNum), isDown); break;
                    case ActionType.Macro: if (isDown) _ = ExecuteMacroAsync(action); break; 
                    case ActionType.BackgroundControl: DispatchBackground(action, isDown); break; 
                    
                    case ActionType.CursorVisibility: 
                        if (isDown) { 
                            int mode = action.CursorVisMode;
                            if (mode == 2) mode = SystemMouseManager.IsCursorHidden ? 1 : 0;
                            if (mode == 1) SystemMouseManager.ShowCursor(); else SystemMouseManager.HideCursor(); 
                        } 
                        break;
                    case ActionType.SystemMouseSettings: 
                        if (isDown) { 
                            SystemMouseManager.SetMouseSpeed(action.SystemMouseSpeed); 
                            SystemMouseManager.SetScrollLines(action.SystemScrollLines, action.SystemScrollType == 1); 
                            SystemMouseManager.SetHorizontalScrollChars(action.SystemHorizontalScroll);
                        } 
                        break;
                }
            }
            catch (Exception ex) { InputLogger.LogError($"OutputDispatcher Error (Action:{action.ActionType})", ex); }
        }

        private void PlayWav(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;
            Task.Run(() => { try { using (var player = new System.Media.SoundPlayer(path)) { player.PlaySync(); } } catch { } });
        }

        private async Task ExecuteMacroAsync(ActionDef action)
        {
            if (action.MacroSteps == null) return;
            foreach (var step in action.MacroSteps)
            {
                int delay = 0;
                if (step.UseDelay)
                {
                    delay = step.DelayMs;
                    if (step.UseFluctuation && step.FluctuationMs > 0) delay += _random.Next(-step.FluctuationMs, step.FluctuationMs + 1);
                    if (delay < 0) delay = 0;
                }
                if (delay > 0) await Task.Delay(delay);

                if (!string.IsNullOrEmpty(step.PlayWavPathStart)) PlayWav(step.PlayWavPathStart);

                bool isDown = step.PressState == StepPressState.Down || step.PressState == StepPressState.Tap;
                bool isUp = step.PressState == StepPressState.Up || step.PressState == StepPressState.Tap;

                ActionDef stepAct = new ActionDef { 
                    ActionType = step.ActionType, ArgumentNum = step.ArgumentNum, MultipleKeys = step.MultipleKeys, ArgumentStr = step.ArgumentStr, ArgumentExtraStr = step.ArgumentExtraStr, MouseX = step.MouseX, MouseY = step.MouseY, BgActionMode = step.BgActionMode, BgClassName = step.BgClassName, BgControlId = step.BgControlId, BgWindowName = step.BgWindowName, ActionState = 0 
                };
                
                if (isDown) 
                {
                    if (step.ActionType == ActionType.AhkRun || step.ActionType == ActionType.AppLaunch || step.ActionType == ActionType.FileOpen || step.ActionType == ActionType.FolderOpen)
                    {
                        var proc = step.ActionType == ActionType.FolderOpen ? Process.Start("explorer.exe", step.ArgumentStr) : LaunchApp(step.ArgumentStr, step.ArgumentExtraStr);
                        if (proc != null && step.WaitForExit)
                        {
                            await Task.Run(() => { try { proc.WaitForExit(); } catch { } });
                        }
                    }
                    else Dispatch(stepAct, true);
                }
                
                if (step.PressState == StepPressState.Tap) await Task.Delay(10);
                
                if (isUp && step.ActionType != ActionType.AhkRun && step.ActionType != ActionType.AppLaunch && step.ActionType != ActionType.FileOpen && step.ActionType != ActionType.FolderOpen) 
                {
                    Dispatch(stepAct, false);
                }

                if (!string.IsNullOrEmpty(step.PlayWavPathEnd)) PlayWav(step.PlayWavPathEnd);
            }
        }

        // 【最適化4】ゼロアロケーション（stackalloc）でのキー入力送信
        public void SendKeyboardInputs(List<int> vKeys, bool isDown)
        {
            if (vKeys == null || vKeys.Count == 0) return;
            
            int count = vKeys.Count;
            // 安全のため、極端にキーが多い場合のみヒープを使用し、通常はスタックを使用
            bool useHeap = count > 32;
            SendInputNative.INPUT* inputs = useHeap 
                ? (SendInputNative.INPUT*)Marshal.AllocHGlobal(count * sizeof(SendInputNative.INPUT)) 
                : stackalloc SendInputNative.INPUT[count];

            try
            {
                var keysToProcess = new List<int>(vKeys);
                if (!isDown) keysToProcess.Reverse();

                for (int i = 0; i < keysToProcess.Count; i++)
                {
                    ushort vKey = (ushort)keysToProcess[i];
                    if (isDown) _pressedKeys.TryAdd(vKey, 1); else _pressedKeys.TryRemove(vKey, out _);
                    
                    inputs[i].type = SendInputNative.INPUT_KEYBOARD;
                    inputs[i].u.ki.wVk = vKey;
                    inputs[i].u.ki.wScan = 0;
                    inputs[i].u.ki.time = 0;
                    inputs[i].u.ki.dwExtraInfo = IntPtr.Zero;

                    uint flags = 0;
                    if (vKey == 37 || vKey == 38 || vKey == 39 || vKey == 40 || vKey == 33 || vKey == 34 || vKey == 35 || vKey == 36 || vKey == 45 || vKey == 46) 
                        flags |= SendInputNative.KEYEVENTF_EXTENDEDKEY;
                    if (!isDown) 
                        flags |= SendInputNative.KEYEVENTF_KEYUP;
                    inputs[i].u.ki.dwFlags = flags;
                }
                SendInputNative.SendInput((uint)count, inputs, sizeof(SendInputNative.INPUT));
            }
            finally
            {
                if (useHeap) Marshal.FreeHGlobal((IntPtr)inputs);
            }
        }

        // 【最適化4】ゼロアロケーション（stackalloc）でのマウス入力送信
        public void SendMouseClick(int buttonId, bool isDown)
        {
            if (buttonId >= 1 && buttonId <= 3) { if (isDown) _pressedMouseButtons.TryAdd(buttonId, 1); else _pressedMouseButtons.TryRemove(buttonId, out _); }
            
            SendInputNative.INPUT* inputs = stackalloc SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;
            inputs[0].u.mi.dx = 0;
            inputs[0].u.mi.dy = 0;
            inputs[0].u.mi.mouseData = 0;
            inputs[0].u.mi.time = 0;
            inputs[0].u.mi.dwExtraInfo = IntPtr.Zero;

            if (buttonId == 1) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_LEFTDOWN : SendInputNative.MOUSEEVENTF_LEFTUP;
            else if (buttonId == 2) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_RIGHTDOWN : SendInputNative.MOUSEEVENTF_RIGHTUP;
            else if (buttonId == 3) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_MIDDLEDOWN : SendInputNative.MOUSEEVENTF_MIDDLEUP;
            else if (buttonId == 4 && isDown) { inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].u.mi.mouseData = 120; }
            else if (buttonId == 5 && isDown) { inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].u.mi.mouseData = unchecked((uint)-120); }
            else if (buttonId == 6) { inputs[0].u.mi.dwFlags = isDown ? 0x0080U : 0x0100U; inputs[0].u.mi.mouseData = 0x0001; }
            else if (buttonId == 7) { inputs[0].u.mi.dwFlags = isDown ? 0x0080U : 0x0100U; inputs[0].u.mi.mouseData = 0x0002; }
            
            if ((buttonId == 4 || buttonId == 5) && !isDown) return;
            SendInputNative.SendInput(1, inputs, sizeof(SendInputNative.INPUT));
        }

        // 【最適化4】ゼロアロケーション（stackalloc）でのマウス移動送信
        public void SendMouseMove(int x, int y, bool isAbsolute, bool isWindowRelative, bool jiggle)
        {
            SendInputNative.INPUT* inputs = stackalloc SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;
            inputs[0].u.mi.mouseData = 0;
            inputs[0].u.mi.time = 0;
            inputs[0].u.mi.dwExtraInfo = IntPtr.Zero;

            if (isAbsolute)
            {
                int targetX = x; int targetY = y;
                if (isWindowRelative)
                {
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        SendInputNative.POINT pt = new SendInputNative.POINT { X = 0, Y = 0 };
                        ClientToScreen(hwnd, ref pt);
                        targetX = pt.X + x; targetY = pt.Y + y;
                    }
                }
                int sW = Screen.PrimaryScreen.Bounds.Width; int sH = Screen.PrimaryScreen.Bounds.Height;
                inputs[0].u.mi.dx = (targetX * 65535) / sW; inputs[0].u.mi.dy = (targetY * 65535) / sH;
                inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_MOVE | SendInputNative.MOUSEEVENTF_ABSOLUTE | SendInputNative.MOUSEEVENTF_VIRTUALDESK;
            }
            else
            {
                inputs[0].u.mi.dx = x; inputs[0].u.mi.dy = y;
                inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_MOVE;
            }
            SendInputNative.SendInput(1, inputs, sizeof(SendInputNative.INPUT));
            
            if (jiggle)
            {
                _ = Task.Run(async () => {
                    await Task.Delay(10);
                    SendMouseMove(1, 1, false, false, false);
                    await Task.Delay(10);
                    SendMouseMove(-1, -1, false, false, false);
                });
            }
        }

        private void SendMouseMoveHover(int x, int y, bool jiggle)
        {
            if (SendInputNative.GetCursorPos(out var pt))
            {
                IntPtr hwnd = WindowFromPoint(pt);
                IntPtr root = GetAncestor(hwnd, 2); 
                if (root != IntPtr.Zero)
                {
                    SendInputNative.POINT ptScreen = new SendInputNative.POINT { X = 0, Y = 0 };
                    ClientToScreen(root, ref ptScreen);
                    SendMouseMove(ptScreen.X + x, ptScreen.Y + y, true, false, jiggle);
                }
            }
        }

        private void DispatchBackground(ActionDef action, bool isDown)
        {
            IntPtr hWndParent = FindWindow(string.IsNullOrEmpty(action.BgClassName) ? null : action.BgClassName, string.IsNullOrEmpty(action.BgWindowName) ? null : action.BgWindowName);
            if (hWndParent == IntPtr.Zero) return;
            IntPtr hWndTarget = hWndParent;
            if (action.BgControlId != 0)
            {
                IntPtr found = IntPtr.Zero;
                EnumChildWindows(hWndParent, (child, lParam) => {
                    if (GetDlgCtrlID(child) == action.BgControlId) { found = child; return false; } return true;
                }, IntPtr.Zero);
                if (found != IntPtr.Zero) hWndTarget = found;
            }
            if (action.BgActionMode == 0) { if (isDown) SendMessage(hWndTarget, 0x00F5 /* BM_CLICK */, IntPtr.Zero, IntPtr.Zero); }
            else if (action.BgActionMode == 1) SendMessage(hWndTarget, isDown ? 0x0100u /* WM_KEYDOWN */ : 0x0101u /* WM_KEYUP */, (IntPtr)action.ArgumentNum, IntPtr.Zero);
        }

        private Process LaunchApp(string path, string args)
        {
            if (!string.IsNullOrEmpty(path)) {
                try { bool useShell = path.ToLower().EndsWith(".ahk") || !path.ToLower().EndsWith(".exe");
                    return Process.Start(new ProcessStartInfo { FileName = path, Arguments = args ?? "", UseShellExecute = useShell }); 
                } catch { }
            }
            return null;
        }

        private Xbox360Button GetXboxButton(int id)
        {
            switch(id)
            {
                case 1: return Xbox360Button.A; case 2: return Xbox360Button.B; case 3: return Xbox360Button.X; case 4: return Xbox360Button.Y;
                case 5: return Xbox360Button.LeftShoulder; case 6: return Xbox360Button.RightShoulder; case 7: return Xbox360Button.Back; case 8: return Xbox360Button.Start;
                case 9: return Xbox360Button.LeftThumb; case 10: return Xbox360Button.RightThumb; case 11: return Xbox360Button.Up; case 12: return Xbox360Button.Down;
                case 13: return Xbox360Button.Left; case 14: return Xbox360Button.Right; case 15: return Xbox360Button.Guide; default: return Xbox360Button.A;
            }
        }
    }
}
