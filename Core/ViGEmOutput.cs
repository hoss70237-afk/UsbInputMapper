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
        private bool _isInitialized = false;

        public ViGEmOutput()
        {
        }

        public void Initialize()
        {
            try
            {
                _client = new ViGEmClient();
                _controller = _client.CreateXbox360Controller();
                _controller.Connect();
                _isInitialized = true;
            }
            catch
            {
                _isInitialized = false;
            }
        }

        public void SetButton(Xbox360Button button, bool isPressed)
        {
            if (!_isInitialized) return;
            _controller.SetButtonState(button, isPressed);
        }

        public void Dispose()
        {
            if (_isInitialized)
            {
                _controller?.Disconnect();
                _client?.Dispose();
            }
        }
    }
}
