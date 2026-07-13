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
        private readonly ViGEmOutput _viGEmOutput;
        private readonly Stack<SendInputNative.POINT> _mousePositionStack = new Stack<SendInputNative.POINT>();

        public OutputDispatcher(ViGEmOutput viGEmOutput)
        {
            _viGEmOutput = viGEmOutput;
        }

        // isDownフラグを受け取り、押す/離すを正確に処理する
        public void Dispatch(ActionDef action, bool isDown)
        {
            switch (action.ActionType)
            {
                case ActionType.Keyboard:
                case ActionType.ToggleHold: // トグルもキーボードと同様に処理
                    SendKeyboardInput((ushort)action.ArgumentNum, isDown);
                    break;
                case ActionType.MouseClick:
                    SendMouseClick(action.ArgumentNum, isDown);
                    break;
                case ActionType.MouseMove:
                    if (isDown) SendMouseMove(action.MouseX, action.MouseY, action.IsAbsolutePosition);
                    break;
                case ActionType.MousePosSave:
                    if (isDown && SendInputNative.GetCursorPos(out var pt))
                    {
                        _mousePositionStack.Push(pt);
                    }
                    break;
                case ActionType.MousePosRestore:
                    if (isDown && _mousePositionStack.Count > 0)
                    {
                        var popPt = _mousePositionStack.Pop();
                        SendMouseMove(popPt.X, popPt.Y, true);
                    }
                    break;
                case ActionType.AppLaunch:
                    if (isDown) LaunchApp(action.ArgumentStr, action.ArgumentExtraStr);
                    break;
                case ActionType.XboxController:
                    Xbox360Button btn = GetXboxButton(action.ArgumentNum);
                    _viGEmOutput.SetButton(btn, isDown);
                    break;
            }
        }

        public void SendKeyboardInput(ushort vKey, bool isDown)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_KEYBOARD;
            
            ushort scanCode = (ushort)SendInputNative.MapVirtualKey(vKey, 0);
            
            // ★ 通常アプリ向けにvKeyも残しつつ、ゲーム向けにスキャンコードも送る最強設定
            inputs[0].ki.wVk = vKey;
            inputs[0].ki.wScan = scanCode;
            
            uint flags = SendInputNative.KEYEVENTF_SCANCODE;
            
            if (vKey == 37 || vKey == 38 || vKey == 39 || vKey == 40 || vKey == 33 || vKey == 34 || vKey == 35 || vKey == 36 || vKey == 45 || vKey == 46)
            {
                flags |= SendInputNative.KEYEVENTF_EXTENDEDKEY;
            }

            if (!isDown) flags |= SendInputNative.KEYEVENTF_KEYUP;

            inputs[0].ki.dwFlags = flags;
            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        private void SendMouseClick(int buttonId, bool isDown)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;

            if (buttonId == 1) inputs[0].mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_LEFTDOWN : SendInputNative.MOUSEEVENTF_LEFTUP;
            else if (buttonId == 2) inputs[0].mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_RIGHTDOWN : SendInputNative.MOUSEEVENTF_RIGHTUP;
            else if (buttonId == 3) inputs[0].mi.dwFlags = isDown ? SendInputNative.MOUSEEVENTF_MIDDLEDOWN : SendInputNative.MOUSEEVENTF_MIDDLEUP;
            else if (buttonId == 4 && isDown) { inputs[0].mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].mi.mouseData = 120; }
            else if (buttonId == 5 && isDown) { inputs[0].mi.dwFlags = SendInputNative.MOUSEEVENTF_WHEEL; inputs[0].mi.mouseData = unchecked((uint)-120); }
            
            // ホイールの場合はアップ処理が不要
            if ((buttonId == 4 || buttonId == 5) && !isDown) return;

            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        private void SendMouseMove(int x, int y, bool isAbsolute)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;

            if (isAbsolute)
            {
                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                inputs[0].mi.dx = (x * 65535) / screenWidth;
                inputs[0].mi.dy = (y * 65535) / screenHeight;
                inputs[0].mi.dwFlags = SendInputNative.MOUSEEVENTF_MOVE | SendInputNative.MOUSEEVENTF_ABSOLUTE | SendInputNative.MOUSEEVENTF_VIRTUALDESK;
            }
            else
            {
                inputs[0].mi.dx = x;
                inputs[0].mi.dy = y;
                inputs[0].mi.dwFlags = SendInputNative.MOUSEEVENTF_MOVE;
            }

            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        private void LaunchApp(string path, string args)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, Arguments = args ?? "", UseShellExecute = true });
            }
            catch { }
        }

        private Xbox360Button GetXboxButton(int id)
        {
            switch(id)
            {
                case 1: return Xbox360Button.A; case 2: return Xbox360Button.B;
                case 3: return Xbox360Button.X; case 4: return Xbox360Button.Y;
                case 5: return Xbox360Button.LeftShoulder; case 6: return Xbox360Button.RightShoulder;
                case 7: return Xbox360Button.Back; case 8: return Xbox360Button.Start;
                case 9: return Xbox360Button.LeftThumb; case 10: return Xbox360Button.RightThumb;
                case 11: return Xbox360Button.Up; case 12: return Xbox360Button.Down;
                case 13: return Xbox360Button.Left; case 14: return Xbox360Button.Right;
                case 15: return Xbox360Button.Guide; default: return Xbox360Button.A;
            }
        }
    }
}
