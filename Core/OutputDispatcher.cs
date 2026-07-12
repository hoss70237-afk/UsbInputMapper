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
                    // ArgumentNum as button enum
                    _viGEmOutput.SetButton((Xbox360Button)action.ArgumentNum, true);
                    System.Threading.Thread.Sleep(20);
                    _viGEmOutput.SetButton((Xbox360Button)action.ArgumentNum, false);
                    break;
                // 他のタイプは必要に応じて拡張
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
