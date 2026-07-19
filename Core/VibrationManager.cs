using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace UsbInputMapper.Core
{
    public static class VibrationManager
    {
        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_VIBRATION { public ushort wLeftMotorSpeed; public ushort wRightMotorSpeed; }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern int XInputSetState14(int dwUserIndex, ref XINPUT_VIBRATION pVibration);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputSetState")]
        private static extern int XInputSetState91(int dwUserIndex, ref XINPUT_VIBRATION pVibration);

        [DllImport("xinput1_3.dll", EntryPoint = "XInputSetState")]
        private static extern int XInputSetState13(int dwUserIndex, ref XINPUT_VIBRATION pVibration);

        private static void SetVibration(ushort left, ushort right)
        {
            var v = new XINPUT_VIBRATION { wLeftMotorSpeed = left, wRightMotorSpeed = right };
            for (int i = 0; i < 4; i++)
            {
                try { XInputSetState14(i, ref v); continue; } catch { }
                try { XInputSetState91(i, ref v); continue; } catch { }
                try { XInputSetState13(i, ref v); continue; } catch { }
            }
        }

        public static async void Vibrate(int durationMs, int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                SetVibration(65535, 65535);
                await Task.Delay(durationMs);
                SetVibration(0, 0);
                if (i < times - 1) await Task.Delay(100);
            }
        }
    }
}
