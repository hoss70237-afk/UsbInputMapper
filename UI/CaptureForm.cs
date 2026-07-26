using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public enum CaptureMode { SingleAny, MultiKeyboard }

    public partial class CaptureForm : Form
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        public static bool IsCapturing { get; private set; }
        public static CaptureForm CurrentInstance { get; private set; }
        
        public CaptureMode Mode { get; set; }
        public InputEvent CapturedEvent { get; private set; }
        public List<int> CapturedKeys { get; private set; } = new List<int>();

        private int _downCount = 0;
        private bool _ignoreInput = false;

        private long _lastStandardInputTime = 0;
        private List<InputEvent> _pendingHidEvents = new List<InputEvent>();
        
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public CaptureForm(CaptureMode mode = CaptureMode.SingleAny)
        {
            InitializeComponent();
            Mode = mode;
            if (Mode == CaptureMode.MultiKeyboard)
            {
                label1.Text = "キーボードのキーを押してください。\r\nすべてのキーを離すと確定します。";
                btnRadialMenuEdge.Visible = false;
            }

            btnCancel.MouseEnter += (s, e) => _ignoreInput = true;
            btnCancel.MouseLeave += (s, e) => _ignoreInput = false;
            btnRadialMenuEdge.MouseEnter += (s, e) => _ignoreInput = true;
            btnRadialMenuEdge.MouseLeave += (s, e) => _ignoreInput = false;
        }

        private void CaptureForm_Load(object sender, EventArgs e) 
        { 
            IsCapturing = true; 
            CurrentInstance = this; 
        }
        
        private void CaptureForm_FormClosed(object sender, FormClosedEventArgs e) 
        { 
            IsCapturing = false; 
            if (CurrentInstance == this) CurrentInstance = null;
            
            _cts.Cancel();
            _cts.Dispose();
        }

        public void ProcessInput(InputEvent e)
        {
            if (_ignoreInput || IsDisposed || _cts.IsCancellationRequested) return;
            
            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action(() => ProcessInput(e))); } catch { }
                return;
            }

            long now = (long)GetTickCount64();
            if (e.Type == 0 || e.Type == 1)
            {
                _lastStandardInputTime = now;
                _pendingHidEvents.Clear(); 
            }

            if (e.Type == 2) // HID入力の遅延評価
            {
                if (now - _lastStandardInputTime < 50) return;
                
                _pendingHidEvents.Add(e);
                
                Task.Run(async () => {
                    try
                    {
                        await Task.Delay(30, _cts.Token);
                        if (_cts.IsCancellationRequested) return;

                        this.BeginInvoke(new Action(() => {
                            if (!IsDisposed && _pendingHidEvents.Contains(e))
                            {
                                _pendingHidEvents.Remove(e);
                                ProcessFinalInput(e);
                            }
                        }));
                    }
                    catch (TaskCanceledException) { }
                    catch (ObjectDisposedException) { }
                });
                return;
            }

            ProcessFinalInput(e);
        }

        private void ProcessFinalInput(InputEvent e)
        {
            if (IsDisposed || _cts.IsCancellationRequested) return;

            // ★ ゲームパッドの軸(Type 11)判定：ニュートラルから大きく傾いた時のみキャプチャ
            if (e.Type == 11)
            {
                int diff = Math.Abs(e.Value - 32767);
                if (diff < 15000) return; // デッドゾーン内は無視
                e.IsDown = true;
            }

            if (Mode == CaptureMode.SingleAny)
            {
                if (e.IsDown) 
                { 
                    CapturedEvent = e; 
                    this.DialogResult = DialogResult.OK; 
                    this.Close(); 
                }
            }
            else if (Mode == CaptureMode.MultiKeyboard)
            {
                if (e.Type == 1)
                {
                    if (e.IsDown)
                    {
                        if (!CapturedKeys.Contains(e.Code)) CapturedKeys.Add(e.Code);
                        _downCount++;
                        string keysStr = string.Join(" + ", CapturedKeys.Select(k => ((Keys)k).ToString()));
                        label1.Text = $"取得中: {keysStr}";
                    }
                    else
                    {
                        _downCount--;
                        if (_downCount <= 0 && CapturedKeys.Count > 0) 
                        { 
                            this.DialogResult = DialogResult.OK; 
                            this.Close(); 
                        }
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) 
        { 
            this.DialogResult = DialogResult.Cancel; 
            this.Close(); 
        }
        
        private void btnRadialMenuEdge_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Retry;
            this.Close();
        }
    }
}
