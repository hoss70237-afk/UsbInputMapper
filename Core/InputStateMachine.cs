using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UsbInputMapper.Core
{
    public class InputStateMachine
    {
        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        private readonly List<InputEvent> _history = new List<InputEvent>();
        private long _lastInputTick = 0;
        
        // DateTime計算を排除し、高速なTickCount(ms)比較へ移行
        private readonly long _timeoutMs = 300; 
        private readonly object _lockObj = new object();

        public void Process(InputEvent evt, Action<InputEvent> onActionDetected)
        {
            if (evt == null) return;

            lock (_lockObj)
            {
                // 【最適化5】DateTime.Now は文字列処理とタイムゾーン計算が入るため、GetTickCount64 に置き換えて軽量化
                long now = (long)GetTickCount64();
                
                if (_lastInputTick > 0 && (now - _lastInputTick > _timeoutMs))
                {
                    _history.Clear();
                }

                _history.Add(evt);
                _lastInputTick = now;
            }

            onActionDetected?.Invoke(evt);
        }

        public void Reset()
        {
            lock (_lockObj)
            {
                _history.Clear();
                _lastInputTick = 0;
            }
        }
    }
}
