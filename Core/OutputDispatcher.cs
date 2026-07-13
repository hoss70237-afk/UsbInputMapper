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
        
        // 座標保存用スタック（マクロ内でのPush/Popなどに使用）
        private readonly Stack<SendInputNative.POINT> _mousePositionStack = new Stack<SendInputNative.POINT>();

        public OutputDispatcher(ViGEmOutput viGEmOutput)
        {
            _viGEmOutput = viGEmOutput;
        }

        public void Dispatch(ActionDef action)
        {
            switch (action.ActionType)
            {
                case ActionType.Keyboard:
                    SendKeyboardInput((ushort)action.ArgumentNum, true);
                    System.Threading.Thread.Sleep(20);
                    SendKeyboardInput((ushort)action.ArgumentNum, false);
                    break;
                case ActionType.MouseClick:
                    // ArgumentNum (1:Left, 2:Right, 3:Middle)
                    SendMouseClick(action.ArgumentNum);
                    break;
                case ActionType.MouseMove:
                    SendMouseMove(action.MouseX, action.MouseY, action.IsAbsolutePosition);
                    break;
                case ActionType.MousePosSave:
                    if (SendInputNative.GetCursorPos(out var pt))
                    {
                        _mousePositionStack.Push(pt);
                    }
                    break;
                case ActionType.MousePosRestore:
                    if (_mousePositionStack.Count > 0)
                    {
                        var popPt = _mousePositionStack.Pop();
                        SendMouseMove(popPt.X, popPt.Y, true);
                    }
                    break;
                case ActionType.AppLaunch:
                    LaunchApp(action.ArgumentStr, action.ArgumentExtraStr);
                    break;
                case ActionType.XboxController:
                    Xbox360Button btn = GetXboxButton(action.ArgumentNum);
                    _viGEmOutput.SetButton(btn, true);
                    System.Threading.Thread.Sleep(20);
                    _viGEmOutput.SetButton(btn, false);
                    break;
                // マクロ処理などは別クラス（MacroEngine）で制御するためここでは省略
            }
        }

        // --- 強力なスキャンコード対応キーボード出力 ---
        public void SendKeyboardInput(ushort vKey, bool isDown)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_KEYBOARD;
            
            // 仮想キーをハードウェアスキャンコードに変換 (ゲームで弾かれないようにする)
            ushort scanCode = (ushort)SendInputNative.MapVirtualKey(vKey, 0);
            
            inputs[0].ki.wVk = 0; // スキャンコード優先の場合は0にする
            inputs[0].ki.wScan = scanCode;
            
            uint flags = SendInputNative.KEYEVENTF_SCANCODE;
            
            // 拡張キー (矢印キーや右Ctrlなど) の判定
            if (vKey == 37 || vKey == 38 || vKey == 39 || vKey == 40 || vKey == 33 || vKey == 34 || vKey == 35 || vKey == 36 || vKey == 45 || vKey == 46)
            {
                flags |= SendInputNative.KEYEVENTF_EXTENDEDKEY;
            }

            if (!isDown) flags |= SendInputNative.KEYEVENTF_KEYUP;

            inputs[0].ki.dwFlags = flags;
            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        // --- マウス出力 ---
        private void SendMouseClick(int buttonId)
        {
            var inputs = new SendInputNative.INPUT[2];
            inputs[0].type = SendInputNative.INPUT_MOUSE;
            inputs[1].type = SendInputNative.INPUT_MOUSE;

            if (buttonId == 1) // Left
            {
                inputs[0].mi.dwFlags = SendInputNative.MOUSEEVENTF_LEFTDOWN;
                inputs[1].mi.dwFlags = SendInputNative.MOUSEEVENTF_LEFTUP;
            }
            else if (buttonId == 2) // Right
            {
                inputs[0].mi.dwFlags = SendInputNative.MOUSEEVENTF_RIGHTDOWN;
                inputs[1].mi.dwFlags = SendInputNative.MOUSEEVENTF_RIGHTUP;
            }
            // 他ボタン拡張可能

            SendInputNative.SendInput(2, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }

        private void SendMouseMove(int x, int y, bool isAbsolute)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_MOUSE;

            if (isAbsolute)
            {
                // 絶対座標の場合、0〜65535の範囲に正規化する必要がある
                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                inputs[0].mi.dx = (x * 65535) / screenWidth;
                inputs[0].mi.dy = (y * 65535) / screenHeight;
                inputs[0].mi.dwFlags = SendInputNative.MOUSEEVENTF_MOVE | SendInputNative.MOUSEEVENTF_ABSOLUTE | SendInputNative.MOUSEEVENTF_VIRTUALDESK;
            }
            else
            {
                // 相対座標（マウスの速度指定移動など）
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
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args ?? "",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private Xbox360Button GetXboxButton(int id)
        {
            switch(id)
            {
                case 1: return Xbox360Button.A;
                case 2: return Xbox360Button.B;
                case 3: return Xbox360Button.X;
                case 4: return Xbox360Button.Y;
                case 5: return Xbox360Button.LeftShoulder;
                case 6: return Xbox360Button.RightShoulder;
                case 7: return Xbox360Button.Back;
                case 8: return Xbox360Button.Start;
                case 9: return Xbox360Button.LeftThumb;
                case 10: return Xbox360Button.RightThumb;
                case 11: return Xbox360Button.Up;
                case 12: return Xbox360Button.Down;
                case 13: return Xbox360Button.Left;
                case 14: return Xbox360Button.Right;
                case 15: return Xbox360Button.Guide;
                default: return Xbox360Button.A;
            }
        }
    }
}
