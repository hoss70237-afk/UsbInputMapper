using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UsbInputMapper.Core
{
    public class RawInputManager : NativeWindow, IDisposable
    {
        public event EventHandler<InputEvent> OnInputEvent;
        private readonly Dictionary<IntPtr, DeviceInfo> _devices = new Dictionary<IntPtr, DeviceInfo>();
        private readonly Dictionary<IntPtr, byte[]> _lastHidData = new Dictionary<IntPtr, byte[]>();

        public RawInputManager()
        {
            CreateHandle(new CreateParams { Caption = "UsbInputMapper_RawInputMessageWindow", Parent = (IntPtr)(-3) });
            RegisterInputDevices();
        }

        private void RegisterInputDevices()
        {
            // 1種類でも登録に失敗すると他のデバイスの監視まで巻き添えで失敗するのを防ぐため、
            // 1つずつ個別に安全に登録する
            void TryRegister(ushort page, ushort usage)
            {
                var rid = new RawInputNative.RAWINPUTDEVICE[1];
                rid[0].usUsagePage = page;
                rid[0].usUsage = usage;
                rid[0].dwFlags = RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY;
                rid[0].hwndTarget = this.Handle;

                // 失敗した場合は無視して次へ進む
                RawInputNative.RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTDEVICE)));
            }

            TryRegister(0x01, 0x02); // マウス
            TryRegister(0x01, 0x06); // キーボード
            TryRegister(0x0C, 0x01); // コンシューマーコントロール (メディアキー、多ボタンマウス等)
            TryRegister(0x01, 0x05); // ゲームパッド
            TryRegister(0x01, 0x04); // ジョイスティック
            TryRegister(0x01, 0x00); // その他の特殊HID
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == RawInputNative.WM_INPUT) ProcessRawInput(m.LParam);
            base.WndProc(ref m);
        }

        private void ProcessRawInput(IntPtr hRawInput)
        {
            uint dataSize = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTHEADER));
            RawInputNative.GetRawInputData(hRawInput, RawInputNative.RID_INPUT, IntPtr.Zero, ref dataSize, headerSize);
            if (dataSize == 0) return;

            IntPtr pData = Marshal.AllocHGlobal((int)dataSize);
            try
            {
                if (RawInputNative.GetRawInputData(hRawInput, RawInputNative.RID_INPUT, pData, ref dataSize, headerSize) == dataSize)
                {
                    var header = (RawInputNative.RAWINPUTHEADER)Marshal.PtrToStructure(pData, typeof(RawInputNative.RAWINPUTHEADER));
                    var devInfo = GetOrAddDeviceInfo(header.hDevice);

                    InputEvent evt = new InputEvent { DeviceIdentifier = devInfo.GetIdentifier(), Type = (int)header.dwType };
                    IntPtr pRawData = new IntPtr(pData.ToInt64() + (IntPtr.Size == 8 ? 24 : 16));

                    if (header.dwType == RawInputNative.RIM_TYPEKEYBOARD)
                    {
                        var kb = (RawInputNative.RAWKEYBOARD)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWKEYBOARD));
                        evt.VKey = kb.VKey;
                        evt.IsKeyDown = (kb.Message == 0x0100 || kb.Message == 0x0104);
                        if (evt.VKey == 255) return;
                        OnInputEvent?.Invoke(this, evt);
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEMOUSE)
                    {
                        var ms = (RawInputNative.RAWMOUSE)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWMOUSE));
                        
                        // 正しいフラグ領域からマウスの全ボタンのクリックを抽出
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0001, 0x0002, 1); // 左クリック
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0004, 0x0008, 2); // 右クリック
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0010, 0x0020, 3); // 中クリック
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0040, 0x0080, 6); // サイド(進む)
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0100, 0x0200, 7); // サイド(戻る)

                        if ((ms.usButtonFlags & 0x0400) != 0) // ホイール回転
                        {
                            evt.MouseButtonFlags = (uint)(ms.usButtonData > 0 ? 4 : 5);
                            evt.IsKeyDown = true;
                            OnInputEvent?.Invoke(this, evt);
                            
                            // ホイールは押された直後に離したことにしてイベントを完了させる
                            InputEvent upEvt = new InputEvent { DeviceIdentifier = evt.DeviceIdentifier, Type = evt.Type, MouseButtonFlags = evt.MouseButtonFlags, IsKeyDown = false };
                            OnInputEvent?.Invoke(this, upEvt);
                        }
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEHID)
                    {
                        var hid = (RawInputNative.RAWHID)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWHID));
                        int size = (int)(hid.dwSizeHid * hid.dwCount);
                        
                        if (size > 0)
                        {
                            byte[] rawData = new byte[size];
                            IntPtr pHidData = new IntPtr(pRawData.ToInt64() + Marshal.SizeOf(typeof(RawInputNative.RAWHID)));
                            Marshal.Copy(pHidData, rawData, 0, size);

                            if (_lastHidData.TryGetValue(header.hDevice, out byte[] lastData) && lastData.Length == size)
                            {
                                for (int i = 0; i < size; i++)
                                {
                                    if (rawData[i] != lastData[i])
                                    {
                                        byte diff = (byte)(rawData[i] ^ lastData[i]);
                                        for (int b = 0; b < 8; b++)
                                        {
                                            if ((diff & (1 << b)) != 0)
                                            {
                                                int customCode = (i << 8) | b;
                                                bool isDown = (rawData[i] & (1 << b)) != 0;
                                                InputEvent hidEvt = new InputEvent 
                                                {
                                                    DeviceIdentifier = evt.DeviceIdentifier, Type = 2,
                                                    MouseButtonFlags = (uint)customCode, IsKeyDown = isDown, HidData = rawData
                                                };
                                                OnInputEvent?.Invoke(this, hidEvt);
                                            }
                                        }
                                    }
                                }
                            }
                            _lastHidData[header.hDevice] = (byte[])rawData.Clone();
                        }
                    }
                }
            }
            finally { Marshal.FreeHGlobal(pData); }
        }

        private void EmitMouseEvent(InputEvent baseEvt, uint currentFlags, uint downFlag, uint upFlag, uint mappedCode)
        {
            if ((currentFlags & downFlag) != 0)
            {
                InputEvent evt = new InputEvent { DeviceIdentifier = baseEvt.DeviceIdentifier, Type = baseEvt.Type, MouseButtonFlags = mappedCode, IsKeyDown = true };
                OnInputEvent?.Invoke(this, evt);
            }
            else if ((currentFlags & upFlag) != 0)
            {
                InputEvent evt = new InputEvent { DeviceIdentifier = baseEvt.DeviceIdentifier, Type = baseEvt.Type, MouseButtonFlags = mappedCode, IsKeyDown = false };
                OnInputEvent?.Invoke(this, evt);
            }
        }

        private DeviceInfo GetOrAddDeviceInfo(IntPtr hDevice)
        {
            if (!_devices.TryGetValue(hDevice, out var info))
            {
                info = new DeviceInfo { Handle = hDevice };
                _devices[hDevice] = info;
            }
            return info;
        }

        public void Dispose() { DestroyHandle(); }
    }
}
