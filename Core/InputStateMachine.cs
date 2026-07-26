using System;
using System.Collections.Generic;

namespace UsbInputMapper.Core
{
    public class InputStateMachine
    {
        private readonly List<InputEvent> _history = new List<InputEvent>();
        private DateTime _lastInputTime = DateTime.MinValue;
        private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(300);
        private readonly object _lockObj = new object();

        public void Process(InputEvent evt, Action<InputEvent> onActionDetected)
        {
            if (evt == null) return;

            lock (_lockObj)
            {
                DateTime now = DateTime.Now;
                if (now - _lastInputTime > _timeout)
                {
                    _history.Clear();
                }

                _history.Add(evt);
                _lastInputTime = now;
            }

            onActionDetected?.Invoke(evt);
        }

        public void Reset()
        {
            lock (_lockObj)
            {
                _history.Clear();
                _lastInputTime = DateTime.MinValue;
            }
        }
    }
}
