using System;
using System.Collections.Generic;

namespace UsbInputMapper.Core
{
    public class InputStateMachine
    {
        // 簡易的な状態遷移・入力バッファ管理
        private readonly List<InputEvent> _history = new List<InputEvent>();
        private DateTime _lastInputTime = DateTime.MinValue;
        private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(300);

        public void Process(InputEvent evt, Action<InputEvent> onActionDetected)
        {
            DateTime now = DateTime.Now;
            if (now - _lastInputTime > _timeout)
            {
                _history.Clear();
            }

            _history.Add(evt);
            _lastInputTime = now;

            // 長押し、ダブルクリック、N連打などの判定をここで行う
            // 現実装ではシンプルにそのまま渡す（必要に応じて後続処理でパターンマッチする）
            onActionDetected?.Invoke(evt);
        }

        public void Reset()
        {
            _history.Clear();
        }
    }
}
