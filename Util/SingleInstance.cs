using System;
using System.Threading;

namespace UsbInputMapper.Util
{
    public static class SingleInstance
    {
        private static Mutex _mutex;

        public static bool Initialize(string mutexName)
        {
            try
            {
                _mutex = new Mutex(true, mutexName, out bool createdNew);
                return createdNew;
            }
            catch
            {
                return true; // 例外時はそのまま起動を許可
            }
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
                }
                catch (Exception)
                {
                }
                finally
                {
                    _mutex.Dispose();
                    _mutex = null;
                }
            }
        }
    }
}
