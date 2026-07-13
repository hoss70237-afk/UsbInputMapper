using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UsbInputMapper.Profiles;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace UsbInputMapper.Core
{
    public class OutputDispatcher
    {
        private readonly ViGEmOutput _viGEmOutput;

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
                case ActionType.AppLaunch:
                    if (!string.IsNullOrEmpty(action.ArgumentStr))
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = action.ArgumentStr,
                                Arguments = action.ArgumentExtraStr ?? "",
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }
                    break;
                case ActionType.XboxController:
                    // 修正箇所: 数値を元に適切なXboxのボタンオブジェクトを取得する
                    Xbox360Button btn = GetXboxButton(action.ArgumentNum);
                    _viGEmOutput.SetButton(btn, true);
                    System.Threading.Thread.Sleep(20);
                    _viGEmOutput.SetButton(btn, false);
                    break;
            }
        }

        private Xbox360Button GetXboxButton(int id)
        {
            // 設定画面で 1～15 などの数値を入力することで割り当て可能
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

        private void SendKeyboardInput(ushort vKey, bool isDown)
        {
            var inputs = new SendInputNative.INPUT[1];
            inputs[0].type = SendInputNative.INPUT_KEYBOARD;
            inputs[0].ki.wVk = vKey;
            inputs[0].ki.dwFlags = isDown ? 0 : SendInputNative.KEYEVENTF_KEYUP;

            SendInputNative.SendInput(1, inputs, Marshal.SizeOf(typeof(SendInputNative.INPUT)));
        }
    }
}
