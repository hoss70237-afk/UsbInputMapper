using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace UsbInputMapper.Core
{
    public class ViGEmOutput : IDisposable
    {
        private ViGEmClient _client;
        private IXbox360Controller _controller;
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            try
            {
                _client = new ViGEmClient();
                _controller = _client.CreateXbox360Controller();
                _controller.Connect();
                IsInitialized = true;
            }
            catch { IsInitialized = false; }
        }

        public void Reset()
        {
            if (!IsInitialized) return;
            foreach(Xbox360Button b in Enum.GetValues(typeof(Xbox360Button))) _controller.SetButtonState(b, false);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);
            _controller.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
            _controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);
        }

        public void SetButton(Xbox360Button button, bool isPressed)
        {
            if (!IsInitialized) return;
            _controller.SetButtonState(button, isPressed);
        }

        public void SetAxis(Xbox360Axis axis, short value)
        {
            if (!IsInitialized) return;
            _controller.SetAxisValue(axis, value);
        }

        public void SetSlider(Xbox360Slider slider, byte value)
        {
            if (!IsInitialized) return;
            _controller.SetSliderValue(slider, value);
        }

        public void Dispose()
        {
            if (IsInitialized)
            {
                _controller?.Disconnect();
                _client?.Dispose();
            }
        }
    }
}
