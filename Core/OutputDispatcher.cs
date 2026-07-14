using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace UsbInputMapper.Core
{
    public class OutputDispatcher
    {
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

        private readonly ViGEmOutput _viGEmOutput;
        private readonly Stack<SendInputNative.POINT> _mousePositionStack = new Stack<SendInputNative.POINT>();

        public OutputDispatcher(ViGEmOutput viGEmOutput) { _viGEmOutput = viGEmOutput; }

        public void Dispatch(ActionDef action, bool isDown)
        {
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
                case ActionType.MousePosSave: if (isDown && SendInputNative.GetCursorPos(out var pt)) _mousePositionStack.Push(pt); break;
                case ActionType.MousePosRestore:
                    if (isDown && _mousePositionStack.Count > 0)
                    {
                        var popPt = _mousePositionStack.Pop();
                        SendMouseMove(popPt.X, popPt.Y, true, false);
                    }
                    break;
                case ActionType.AppLaunch: if (isDown) LaunchApp(action.ArgumentStr, action.ArgumentExtraStr); break;
                case ActionType.XboxController: _viGEmOutput.SetButton(GetXboxButton(action.ArgumentNum), isDown); break;
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
                inputs[i].type = SendInputNative.INPUT_KEYBOARD;
                inputs[i].u.ki.wVk = vKey;
                uint flags = 0;
                if (vKey == 37 || vKey == 38 || vKey == 39 || vKey == 40 || vKey == 33 || vKey == 34 || vKey == 35 || vKey == 36 || vKey == 45 || vKey == 46) flags |= SendInputNative.KEYEVENTF_EXTENDEDKEY;
                if (!isDown) flags |= SendInputNative.KEYEVENTF_KEYUP;
                inputs[i].u.ki.dwFlags = flags;
            }
            SendInputNative.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        private void SendMouseClick(int buttonId, bool isDown)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;
            if (buttonId == 1) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_LEFTDOWN : SendInputNative.MOUSEEVENTF_LEFTUP;
            else if (buttonId == 2) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_RIGHTDOWN : SendInputNative.MOUSEEVENTF_RIGHTUP;
            else if (buttonId == 3) inputs[0].u.mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_MIDDLEDOWN : SendInputNative.MOUSEEVENTF_MIDDLEUP;
            else if (buttonId == 4 && isDown) { inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].u.mi.mouseData = 120; }
            else if (buttonId == 5 && isDown) { inputs[0].u.mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].u.mi.mouseData = unchecked((uint)-120); }
            if ((buttonId == 4 || buttonId == 5) && !isDown) return;
            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        private void SendMouseMove(int x, int y, bool isAbsolute, bool isWindowRelative)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;
            if (isAbsolute)
            {
                int targetX = x; int targetY = y;
                if (isWindowRelative)
                {
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT rect))
                    {
                        targetX = rect.Left + x; targetY = rect.Top + y;
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

        private void LaunchApp(string path, string args)
        {
            if (!string.IsNullOrEmpty(path)) try { Process.Start(new ProcessStartInfo { FileName = path, Arguments = args ?? "", UseShellExecute = true }); } catch { }
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
