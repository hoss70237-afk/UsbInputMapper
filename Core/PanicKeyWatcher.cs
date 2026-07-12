using System;
using System.Timers;

namespace UsbInputMapper.Core
{
    public class PanicKeyWatcher
    {
        public event EventHandler OnPanicTriggered;

        private bool _leftPressed;
        private bool _rightPressed;
        private Timer _timer;

        public PanicKeyWatcher()
        {
            _timer = new Timer(10000); // 10秒
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = false;
        }

        public void ProcessMouseInput(uint flags)
        {
            // RawInputのフラグ定数
            // RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001
            // RI_MOUSE_LEFT_BUTTON_UP   = 0x0002
            // RI_MOUSE_RIGHT_BUTTON_DOWN= 0x0004
            // RI_MOUSE_RIGHT_BUTTON_UP  = 0x0008

            if ((flags & 0x0001) != 0) _leftPressed = true;
            if ((flags & 0x0002) != 0) _leftPressed = false;
            if ((flags & 0x0004) != 0) _rightPressed = true;
            if ((flags & 0x0008) != 0) _rightPressed = false;

            if (_leftPressed && _rightPressed)
            {
                if (!_timer.Enabled)
                {
                    _timer.Start();
                }
            }
            else
            {
                _timer.Stop();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPanicTriggered?.Invoke(this, EventArgs.Empty);
        }
    }
}
