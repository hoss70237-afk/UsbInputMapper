using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace UsbInputMapper.Core
{
    public class OutputDispatcher
    {
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
        private readonly Stack<SendInputNative.POINT> _mousePositionStack = new Stack<SendInputNative.POINT>();
        private readonly Random _random = new Random();

        private HashSet<int> _pressedKeys = new HashSet<int>();
        private HashSet<int> _pressedMouseButtons = new HashSet<int>();

        public OutputDispatcher(ViGEmOutput viGEmOutput) { _viGEmOutput = viGEmOutput; }

        public void ReleaseAllInputs()
        {
            if (_pressedKeys.Count > 0) { SendKeyboardInputs(_pressedKeys.ToList(), false); _pressedKeys.Clear(); }
            foreach (var mb in _pressedMouseButtons.ToList()) { SendMouseClick(mb, false); }
            _pressedMouseButtons.Clear();
            _viGEmOutput.Reset();
        }

        private void PlayWav(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;
            Task.Run(() => {
                try {
                    using (var player = new System.Media.SoundPlayer(path)) {
                        player.Play();
                    }
                } catch { }
            });
        }

        public void Dispatch(ActionDef action, bool isDown)
        {
            if (isDown && !string.IsNullOrEmpty(action.PlayWavPath)) PlayWav(action.PlayWavPath);

            switch (action.ActionType)
            {
                case ActionType.Keyboard:
                case ActionType.ToggleHold:
                    if (action.MultipleKeys != null && action.MultipleKeys.Count > 0) SendKeyboardInputs(action.MultipleKeys, isDown);
                    else SendKeyboardInputs(new List<int> { action.ArgumentNum }, isDown);
                    break;
                case ActionType.MouseClick: SendMouseClick(action.ArgumentNum, isDown); break;
                case ActionType.MouseMoveRelative: if (isDown) SendMouseMove(action.MouseX, action.MouseY, false, false); break;
                case ActionType.MouseMoveAbsoluteDesk: if (isDown) SendMouseMove(action.MouseX, action.MouseY, true, false); break;
                case ActionType.MouseMoveAbsoluteWin: if (isDown) SendMouseMove(action.MouseX, action.MouseY, true, true); break;
                case ActionType.MouseMoveAbsoluteHoverWin: if (isDown) SendMouseMoveHover(action.MouseX, action.MouseY); break;
                case ActionType.MousePosSave: if (isDown && SendInputNative.GetCursorPos(out var pt)) _mousePositionStack.Push(pt); break;
                case ActionType.MousePosRestore:
                    if (isDown && _mousePositionStack.Count > 0) { var popPt = _mousePositionStack.Pop(); SendMouseMove(popPt.X, popPt.Y, true, false); } break;
                case ActionType.AppLaunch: 
                case ActionType.FileLaunch:
                case ActionType.AhkLaunch:
                    if (isDown) LaunchApp(action.ArgumentStr, action.ArgumentExtraStr); break;
                case ActionType.XboxController: _viGEmOutput.SetButton(GetXboxButton(action.ArgumentNum), isDown); break;
                case ActionType.Macro: if (isDown) _ = ExecuteMacroAsync(action); break; 
                case ActionType.BackgroundControl: DispatchBackground(action, isDown); break;
            }
        }

        private async Task ExecuteMacroAsync(ActionDef action)
        {
            if (action.MacroSteps == null) return;
            foreach (var step in action.MacroSteps)
            {
                if (!string.IsNullOrEmpty(step.PlayWavPathStart)) PlayWav(step.PlayWavPathStart);

                int delay = 0;
                if (step.UseDelay)
                {
                    delay = step.DelayMs;
                    if (step.UseFluctuation && step.FluctuationMs > 0) delay += _random.Next(-step.FluctuationMs, step.FluctuationMs + 1);
                    if (delay < 0) delay = 0;
                }
                if (delay > 0) await Task.Delay(delay);

                bool isDown = step.PressState == StepPressState.Down || step.PressState == StepPressState.Tap;
                bool isUp = step.PressState == StepPressState.Up || step.PressState == StepPressState.Tap;

                ActionDef stepAct = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.ArgumentNum, MultipleKeys = step.MultipleKeys, ArgumentStr = step.ArgumentStr, ArgumentExtraStr = step.ArgumentExtraStr, MouseX = step.MouseX, MouseY = step.MouseY, BgActionMode = step.BgActionMode, BgClassName = step.BgClassName, BgControlId = step.BgControlId, BgWindowName = step.BgWindowName };
                
                Task launchTask = null;

                if (isDown) 
                {
                    if (stepAct.ActionType == ActionType.AhkLaunch) launchTask = LaunchAppAsync(stepAct.ArgumentStr, stepAct.ArgumentExtraStr, step.WaitForExit);
                    else Dispatch(stepAct, true);
                }
                
                if (step.PressState == StepPressState.Tap) await Task.Delay(10);
                
                if (isUp) 
                {
                    if (stepAct.ActionType == ActionType.AhkLaunch) {
                        if (launchTask == null) launchTask = LaunchAppAsync(stepAct.ArgumentStr, stepAct.ArgumentExtraStr, step.WaitForExit);
                    } else Dispatch(stepAct, false);
                }

                if (step.WaitForExit && launchTask != null)
                {
                    await launchTask;
                }

                if (!string.IsNullOrEmpty(step.PlayWavPathEnd)) PlayWav(step.PlayWavPathEnd);
            }
        }

        public void SendKeyboardInputs(List<int> vKeys, bool isDown)
        {
            if (vKeys == null || vKeys.Count == 0) return;
            var inputs = new SendInputNative.INPUT[vKeys.Count];
            var keysToProcess = new List<int>(vKeys);
            if (!isDown) keysToProcess.Reverse();
            for (int i = 0; i < keysToProcess.Count; i++)
            {
                ushort vKey = (ushort)keysToProcess[i];
                if (isDown) _pressedKeys.Add(vKey); else _pressedKeys.Remove(vKey);

                inputs[i].type = SendInputNative.INPUT_KEYBOARD;
                inputs[i].u.ki.wVk = vKey;
                uint flags = 0;
                if (vKey == 37 || vKey == 38 || vKey == 39 || vKey == 40 || vKey == 33 || vKey == 34 || vKey == 35 || vKey == 36 || vKey == 45 || vKey == 46) flags |= SendInputNative.KEYEVENTF_EXTENDEDKEY;
                if (!isDown) flags |= SendInputNative.KEYEVENTF_KEYUP;
                inputs[i].u.ki.dwFlags = flags;
            }
            SendInputNative.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        public void SendMouseClick(int buttonId, bool isDown)
        {
            if (buttonId >= 1 && buttonId <= 3) { if (isDown) _pressedMouseButtons.Add(buttonId); else _pressedMouseButtons.Remove(buttonId); }

            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;
            if (buttonId == 1) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_LEFTDOWN : SendInputNative.MOUSEEVENTF_LEFTUP;
            else if (buttonId == 2) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_RIGHTDOWN : SendInputNative.MOUSEEVENTF_RIGHTUP;
            else if (buttonId == 3) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_MIDDLEDOWN : SendInputNative.MOUSEEVENTF_MIDDLEUP;
            else if (buttonId == 4 && isDown) { inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].u.mi.mouseData = 120; }
            else if (buttonId == 5 && isDown) { inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].u.mi.mouseData = unchecked((uint)-120); }
            else if (buttonId == 6) { inputs[0].u.mi.dwFlags = isDown ? 0x0080U : 0x0100U; inputs[0].u.mi.mouseData = 0x0001; }
            else if (buttonId == 7) { inputs[0].u.mi.dwFlags = isDown ? 0x0080U : 0x0100U; inputs[0].u.mi.mouseData = 0x0002; }
            
            if ((buttonId == 4 || buttonId == 5) && !isDown) return;
            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        public void SendMouseMove(int x, int y, bool isAbsolute, bool isWindowRelative)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;
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
            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        private void SendMouseMoveHover(int x, int y)
        {
            if (SendInputNative.GetCursorPos(out var pt))
            {
                IntPtr hwnd = WindowFromPoint(pt);
                IntPtr root = GetAncestor(hwnd, 2); 
                if (root != IntPtr.Zero)
                {
                    SendInputNative.POINT ptScreen = new SendInputNative.POINT { X = 0, Y = 0 };
                    ClientToScreen(root, ref ptScreen);
                    SendMouseMove(ptScreen.X + x, ptScreen.Y + y, true, false);
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

            if (action.BgActionMode == 0) // Click
            {
                if (isDown) SendMessage(hWndTarget, 0x00F5 /* BM_CLICK */, IntPtr.Zero, IntPtr.Zero);
            }
            else if (action.BgActionMode == 1) // Key
            {
                SendMessage(hWndTarget, isDown ? 0x0100u /* WM_KEYDOWN */ : 0x0101u /* WM_KEYUP */, (IntPtr)action.ArgumentNum, IntPtr.Zero);
            }
        }

        private void LaunchApp(string path, string args)
        {
            if (!string.IsNullOrEmpty(path)) try { Process.Start(new ProcessStartInfo { FileName = path, Arguments = args ?? "", UseShellExecute = true }); } catch { }
        }

        private async Task LaunchAppAsync(string path, string args, bool waitForExit)
        {
            if (string.IsNullOrEmpty(path)) return;
            try {
                var p = Process.Start(new ProcessStartInfo { FileName = path, Arguments = args ?? "", UseShellExecute = true });
                if (waitForExit && p != null) {
                    await Task.Run(() => p.WaitForExit());
                }
            } catch { }
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
