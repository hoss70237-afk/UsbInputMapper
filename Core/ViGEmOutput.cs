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
