using System;
using System.Threading;

namespace UsbInputMapper.Util
{
    public static class SingleInstance
    {
        private static Mutex _mutex;

        public static bool Initialize(string mutexName)
        {
            _mutex = new Mutex(true, mutexName, out bool createdNew);
            return createdNew;
        }

        public static void Release()
        {
            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // 別のスレッドから解放された場合など
                }
                _mutex.Dispose();
                _mutex = null;
            }
        }
    }
}
