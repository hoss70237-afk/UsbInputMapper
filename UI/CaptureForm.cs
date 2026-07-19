using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public enum CaptureMode { SingleAny, MultiKeyboard }

    public partial class CaptureForm : Form
    {
        public static bool IsCapturing { get; private set; }
        public static CaptureForm CurrentInstance { get; private set; }
        
        public CaptureMode Mode { get; set; }
        public InputEvent CapturedEvent { get; private set; }
        public List<int> CapturedKeys { get; private set; } = new List<int>();

        private int _downCount = 0;
        private bool _ignoreInput = false;

        private long _lastStandardInputTime = 0;
        private List<InputEvent> _pendingHidEvents = new List<InputEvent>();

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

        private void CaptureForm_Load(object sender, EventArgs e) { IsCapturing = true; CurrentInstance = this; }
        private void CaptureForm_FormClosed(object sender, FormClosedEventArgs e) { IsCapturing = false; if (CurrentInstance == this) CurrentInstance = null; }

        public void ProcessInput(InputEvent e)
        {
            if (_ignoreInput) return;
            
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ProcessInput(e)));
                return;
            }

            long now = Environment.TickCount;
            if (e.Type == 0 || e.Type == 1)
            {
                _lastStandardInputTime = now;
                _pendingHidEvents.Clear(); 
            }

            if (e.Type == 2)
            {
                if (now - _lastStandardInputTime < 50) return;
                
                _pendingHidEvents.Add(e);
                Task.Run(async () => {
                    await Task.Delay(30);
                    this.BeginInvoke(new Action(() => {
                        if (_pendingHidEvents.Contains(e))
                        {
                            _pendingHidEvents.Remove(e);
                            ProcessFinalInput(e);
                        }
                    }));
                });
                return;
            }

            ProcessFinalInput(e);
        }

        private void ProcessFinalInput(InputEvent e)
        {
            if (Mode == CaptureMode.SingleAny)
            {
                if (e.IsKeyDown) { CapturedEvent = e; this.DialogResult = DialogResult.OK; this.Close(); }
            }
            else if (Mode == CaptureMode.MultiKeyboard)
            {
                if (e.Type == 1)
                {
                    if (e.IsKeyDown)
                    {
                        if (!CapturedKeys.Contains(e.VKey)) CapturedKeys.Add(e.VKey);
                        _downCount++;
                        string keysStr = string.Join(" + ", CapturedKeys.Select(k => ((Keys)k).ToString()));
                        label1.Text = $"取得中: {keysStr}";
                    }
                    else
                    {
                        _downCount--;
                        if (_downCount <= 0 && CapturedKeys.Count > 0) { this.DialogResult = DialogResult.OK; this.Close(); }
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
        
        private void btnRadialMenuEdge_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Retry;
            this.Close();
        }
    }
}
