using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class MacroEditorForm : Form
    {
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point p);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        private readonly ActionDef _action;
        private List<string> _profileNames;
        private long _lastRecordTime = 0;
        private bool _isUpdatingUI = false;
        private bool _isTimelineMode = false;

        public MacroEditorForm(ActionDef action, List<string> profileNames = null)
        {
            InitializeComponent();
            _action = action;
            _profileNames = profileNames ?? new List<string>();

            cmbPlaybackMode.Items.Clear();
            cmbPlaybackMode.Items.Add("一括再生 (離しても最後まで)");
            cmbPlaybackMode.Items.Add("順次再生 (離すと中断)");
            cmbPlaybackMode.Items.Add("リピート再生 (押している間ループ)");
            cmbPlaybackMode.Items.Add("ステップ再生 (押す度に1つ進む)");

            cmbPressState.Items.Clear();
            cmbPressState.Items.Add("タップ (押してすぐ離す)");
            cmbPressState.Items.Add("押す (Down)");
            cmbPressState.Items.Add("離す (Up)");

            cmbRecordMode.Items.Clear();
            cmbRecordMode.Items.Add("入力のみ記録 (固定50msディレイ)");
            cmbRecordMode.Items.Add("実際の経過時間を記録");

            cmbPressState.SelectedIndex = 0;
            cmbRecordMode.SelectedIndex = 0;
            numTimeout.Value = _action.StepTimeoutMs;

            cmbPlaybackMode.SelectedIndex = (int)_action.PlaybackMode;

            lstSteps.SelectedIndexChanged += LstSteps_SelectedIndexChanged;
            
            // ★追加: タイムライン用
            pnlTimeline.Paint += PnlTimeline_Paint;
            pnlTimeline.MouseDown += PnlTimeline_MouseDown;
            pnlTimeline.MouseMove += PnlTimeline_MouseMove;
            pnlTimeline.MouseUp += PnlTimeline_MouseUp;

            AttachPropertyEvents();

            RefreshMacroList();
            UpdateControlsByMode();
            UpdateDetailPanelState();
        }

        private void AttachPropertyEvents()
        {
            chkUseDelay.CheckedChanged += SyncToStep;
            numDelay.ValueChanged += SyncToStep;
            chkUseFluctuation.CheckedChanged += SyncToStep;
            numFluctuation.ValueChanged += SyncToStep;
            cmbPressState.SelectedIndexChanged += SyncToStep;
            txtWavStart.TextChanged += SyncToStep;
            txtWavEnd.TextChanged += SyncToStep;
            chkWaitForExit.CheckedChanged += SyncToStep;
        }

        private void SyncToStep(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;
            int idx = lstSteps.SelectedIndex;
            if (idx >= 0 && idx < _action.MacroSteps.Count)
            {
                var step = _action.MacroSteps[idx];
                step.UseDelay = chkUseDelay.Checked;
                step.DelayMs = (int)numDelay.Value;
                step.UseFluctuation = chkUseFluctuation.Checked;
                step.FluctuationMs = (int)numFluctuation.Value;
                step.PressState = (StepPressState)cmbPressState.SelectedIndex;
                step.PlayWavPathStart = txtWavStart.Text;
                step.PlayWavPathEnd = txtWavEnd.Text;
                step.WaitForExit = chkWaitForExit.Checked;

                RefreshMacroList(idx);
            }
        }

        private void LstSteps_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDetailPanelState();
        }

        private void UpdateDetailPanelState()
        {
            int idx = lstSteps.SelectedIndex;
            if (idx >= 0 && idx < _action.MacroSteps.Count)
            {
                pnlStepDetails.Enabled = true;
                _isUpdatingUI = true;
                var step = _action.MacroSteps[idx];
                
                chkUseDelay.Checked = step.UseDelay;
                numDelay.Value = step.DelayMs;
                chkUseFluctuation.Checked = step.UseFluctuation;
                numFluctuation.Value = step.FluctuationMs;
                cmbPressState.SelectedIndex = (int)step.PressState;
                txtWavStart.Text = step.PlayWavPathStart;
                txtWavEnd.Text = step.PlayWavPathEnd;
                chkWaitForExit.Checked = step.WaitForExit;
                
                bool isAppAction = (step.ActionType == ActionType.AhkRun || step.ActionType == ActionType.AppLaunch || step.ActionType == ActionType.FileOpen);
                chkWaitForExit.Visible = isAppAction;
                
                _isUpdatingUI = false;
            }
            else
            {
                pnlStepDetails.Enabled = false;
                _isUpdatingUI = true;
                txtWavStart.Text = ""; txtWavEnd.Text = ""; chkWaitForExit.Checked = false; chkWaitForExit.Visible = false;
                _isUpdatingUI = false;
            }
            if (_isTimelineMode) pnlTimeline.Invalidate();
        }

        private void RefreshMacroList(int selectIndex = -1)
        {
            _isUpdatingUI = true;
            if (selectIndex == -1) selectIndex = lstSteps.SelectedIndex;
            
            lstSteps.Items.Clear();
            foreach (var step in _action.MacroSteps)
            {
                string stateStr = step.PressState == StepPressState.Down ? "[押す]" : (step.PressState == StepPressState.Up ? "[離す]" : "[タップ]");
                string delayStr = step.UseDelay ? $"[{step.DelayMs}ms{(step.UseFluctuation ? "±"+step.FluctuationMs : "")}]" : "[待機無]";
                
                ActionDef dummyAct = new ActionDef { 
                    ActionType = step.ActionType, ArgumentNum = step.ArgumentNum, MultipleKeys = step.MultipleKeys, ArgumentStr = step.ArgumentStr, ArgumentExtraStr = step.ArgumentExtraStr, MouseX = step.MouseX, MouseY = step.MouseY, 
                    BgActionMode = step.BgActionMode, BgClassName = step.BgClassName, BgControlId = step.BgControlId, BgWindowName = step.BgWindowName 
                };
                lstSteps.Items.Add($"{delayStr} {stateStr} {dummyAct.ToString()}");
            }
            if (selectIndex >= 0 && selectIndex < lstSteps.Items.Count) lstSteps.SelectedIndex = selectIndex;
            _isUpdatingUI = false;
            
            if (_isTimelineMode) pnlTimeline.Invalidate();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var dummyBinding = new UsbInputMapper.Profiles.Binding();
            using (var editor = new BindingEditorForm(dummyBinding, _profileNames))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    var a = editor.ResultBinding.Action;
                    var step = new MacroStep {
                        ActionType = a.ActionType, ArgumentNum = a.ArgumentNum, MultipleKeys = a.MultipleKeys, ArgumentStr = a.ArgumentStr, ArgumentExtraStr = a.ArgumentExtraStr, MouseX = a.MouseX, MouseY = a.MouseY, 
                        BgActionMode = a.BgActionMode, BgClassName = a.BgClassName, BgControlId = a.BgControlId, BgWindowName = a.BgWindowName,
                        UseDelay = true, DelayMs = 50, UseFluctuation = false, FluctuationMs = 0,
                        PressState = StepPressState.Tap
                    };
                    _action.MacroSteps.Add(step);
                    RefreshMacroList(_action.MacroSteps.Count - 1);
                }
            }
        }

        private void btnEditStep_Click(object sender, EventArgs e)
        {
            int idx = lstSteps.SelectedIndex;
            if (idx >= 0)
            {
                var step = _action.MacroSteps[idx];
                var dummyBinding = new UsbInputMapper.Profiles.Binding();
                
                dummyBinding.Action.ActionType = step.ActionType; dummyBinding.Action.ArgumentNum = step.ArgumentNum; dummyBinding.Action.MultipleKeys = step.MultipleKeys; dummyBinding.Action.ArgumentStr = step.ArgumentStr; dummyBinding.Action.ArgumentExtraStr = step.ArgumentExtraStr; dummyBinding.Action.MouseX = step.MouseX; dummyBinding.Action.MouseY = step.MouseY;
                dummyBinding.Action.BgActionMode = step.BgActionMode; dummyBinding.Action.BgClassName = step.BgClassName; dummyBinding.Action.BgControlId = step.BgControlId; dummyBinding.Action.BgWindowName = step.BgWindowName;
                
                using (var editor = new BindingEditorForm(dummyBinding, _profileNames))
                {
                    if (editor.ShowDialog(this) == DialogResult.OK)
                    {
                        var a = editor.ResultBinding.Action;
                        step.ActionType = a.ActionType; step.ArgumentNum = a.ArgumentNum; step.MultipleKeys = a.MultipleKeys; step.ArgumentStr = a.ArgumentStr; step.ArgumentExtraStr = a.ArgumentExtraStr; step.MouseX = a.MouseX; step.MouseY = a.MouseY;
                        step.BgActionMode = a.BgActionMode; step.BgClassName = a.BgClassName; step.BgControlId = a.BgControlId; step.BgWindowName = a.BgWindowName;
                        RefreshMacroList(idx);
                        UpdateDetailPanelState();
                    }
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e) { int idx = lstSteps.SelectedIndex; if (idx >= 0) { _action.MacroSteps.RemoveAt(idx); RefreshMacroList(-1); UpdateDetailPanelState(); } }
        private void btnUpStep_Click(object sender, EventArgs e) { int idx = lstSteps.SelectedIndex; if (idx > 0) { var item = _action.MacroSteps[idx]; _action.MacroSteps.RemoveAt(idx); _action.MacroSteps.Insert(idx - 1, item); RefreshMacroList(idx - 1); } }
        private void btnDownStep_Click(object sender, EventArgs e) { int idx = lstSteps.SelectedIndex; if (idx >= 0 && idx < _action.MacroSteps.Count - 1) { var item = _action.MacroSteps[idx]; _action.MacroSteps.RemoveAt(idx); _action.MacroSteps.Insert(idx + 1, item); RefreshMacroList(idx + 1); } }

        private void cmbPlaybackMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlsByMode();
            bool isStepMode = (cmbPlaybackMode.SelectedIndex == 3);
            if (!isStepMode)
            {
                bool changed = false;
                foreach (var step in _action.MacroSteps) if (step.PressState != StepPressState.Tap) { step.PressState = StepPressState.Tap; changed = true; }
                if (changed) RefreshMacroList(lstSteps.SelectedIndex);
            }
        }

        private void UpdateControlsByMode()
        {
            bool isStepMode = (cmbPlaybackMode.SelectedIndex == 3);
            lblTimeout.Visible = numTimeout.Visible = isStepMode;
            if (cmbPressState.Items.Count > 0) cmbPressState.Enabled = isStepMode;
            if (!isStepMode && cmbPressState.Items.Count > 0) { _isUpdatingUI = true; cmbPressState.SelectedIndex = 0; _isUpdatingUI = false; }
        }

        private void btnBrowseWavStart_Click(object sender, EventArgs e) { using (var ofd = new OpenFileDialog { Filter = "WAVファイル|*.wav|全て|*.*" }) { if (ofd.ShowDialog() == DialogResult.OK) { txtWavStart.Text = ofd.FileName; } } }
        private void btnBrowseWavEnd_Click(object sender, EventArgs e) { using (var ofd = new OpenFileDialog { Filter = "WAVファイル|*.wav|全て|*.*" }) { if (ofd.ShowDialog() == DialogResult.OK) { txtWavEnd.Text = ofd.FileName; } } }

        private void chkRecord_CheckedChanged(object sender, EventArgs e)
        {
            if (GlobalHookManager.Instance == null) return;
            if (chkRecord.Checked) { chkRecord.Text = "レコーディング停止"; _lastRecordTime = Environment.TickCount; GlobalHookManager.Instance.OnRecordedInput += Hook_OnRecordedInput; GlobalHookManager.Instance.IsRecording = true; }
            else { chkRecord.Text = "レコーディング開始"; GlobalHookManager.Instance.IsRecording = false; GlobalHookManager.Instance.OnRecordedInput -= Hook_OnRecordedInput; RefreshMacroList(_action.MacroSteps.Count - 1); }
        }

        private void Hook_OnRecordedInput(object sender, GlobalHookManager.HookInputEvent e)
        {
            if (InvokeRequired) { Invoke(new Action(() => Hook_OnRecordedInput(sender, e))); return; }
            long now = e.Timestamp; int delay = 50;
            if (cmbRecordMode.SelectedIndex == 1) { delay = (int)(now - _lastRecordTime); if (delay < 0) delay = 0; }

            if (e.Type == 1)
            {
                if (cmbPlaybackMode.SelectedIndex != 3 && !e.IsDown) { _lastRecordTime = now; return; }
                var step = new MacroStep { UseDelay = true, DelayMs = delay, PressState = e.IsDown ? StepPressState.Down : StepPressState.Up, ActionType = ActionType.Keyboard, ArgumentNum = e.Code };
                if (cmbPlaybackMode.SelectedIndex != 3) step.PressState = StepPressState.Tap;
                _action.MacroSteps.Add(step);
            }
            else
            {
                if (e.IsDown)
                {
                    int targetX = e.X; int targetY = e.Y;
                    var moveStep = new MacroStep { UseDelay = true, DelayMs = delay, PressState = StepPressState.Tap, ActionType = ActionType.MouseMoveAbsoluteHoverWin, MouseX = targetX, MouseY = targetY };
                    _action.MacroSteps.Add(moveStep);
                    var clickStep = new MacroStep { UseDelay = false, DelayMs = 0, PressState = StepPressState.Down, ActionType = ActionType.MouseClick, ArgumentNum = e.Code };
                    if (cmbPlaybackMode.SelectedIndex != 3) clickStep.PressState = StepPressState.Tap;
                    _action.MacroSteps.Add(clickStep);
                }
                else if (cmbPlaybackMode.SelectedIndex == 3)
                {
                    var clickStep = new MacroStep { UseDelay = true, DelayMs = delay, PressState = StepPressState.Up, ActionType = ActionType.MouseClick, ArgumentNum = e.Code };
                    _action.MacroSteps.Add(clickStep);
                }
            }
            _lastRecordTime = now; RefreshMacroList(_action.MacroSteps.Count - 1);
        }
        
        // ★追加: タイムライン用描画ロジック
        private void btnToggleTimeline_Click(object sender, EventArgs e)
        {
            _isTimelineMode = !_isTimelineMode;
            lstSteps.Visible = !_isTimelineMode;
            pnlTimeline.Visible = _isTimelineMode;
            btnToggleTimeline.Text = _isTimelineMode ? "リスト編集へ戻る" : "タイムライン編集 (絶対時間)";
            if (_isTimelineMode) pnlTimeline.Invalidate();
        }

        private const int ROW_HEIGHT = 20;
        private const float MS_PER_PIXEL = 5.0f; // 1ピクセルあたり5ms
        private int _draggingStepIndex = -1;
        private int _dragStartX = 0;
        private int _dragStartDelay = 0;

        private void PnlTimeline_Paint(object sender, PaintEventArgs e)
        {
            if (!_isTimelineMode) return;
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            
            // 背景のグリッド描画
            int w = pnlTimeline.Width; int h = pnlTimeline.Height;
            using (Pen gridPen = new Pen(Color.LightGray))
            {
                for (int x = 0; x < w; x += 20) g.DrawLine(gridPen, x, 0, x, h); // 100ms単位
                for (int y = 0; y < h; y += ROW_HEIGHT) g.DrawLine(gridPen, 0, y, w, y);
            }

            int currentAbsTime = 0;
            using (Font f = new Font("MS UI Gothic", 8))
            {
                for (int i = 0; i < _action.MacroSteps.Count; i++)
                {
                    var step = _action.MacroSteps[i];
                    currentAbsTime += step.DelayMs;
                    
                    int px = (int)(currentAbsTime / MS_PER_PIXEL);
                    int py = i * ROW_HEIGHT;
                    
                    Rectangle rect = new Rectangle(px, py, 15, ROW_HEIGHT - 2);
                    
                    bool isSelected = (lstSteps.SelectedIndex == i);
                    using (Brush b = new SolidBrush(isSelected ? Color.DodgerBlue : Color.LightGreen))
                    {
                        g.FillRectangle(b, rect);
                    }
                    g.DrawRectangle(Pens.Black, rect);
                    
                    ActionDef dummyAct = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.ArgumentNum, MultipleKeys = step.MultipleKeys, ArgumentStr = step.ArgumentStr };
                    g.DrawString(dummyAct.ToString(), f, Brushes.Black, px + 20, py + 3);
                }
            }
        }

        private void PnlTimeline_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int currentAbsTime = 0;
                for (int i = 0; i < _action.MacroSteps.Count; i++)
                {
                    var step = _action.MacroSteps[i];
                    currentAbsTime += step.DelayMs;
                    
                    int px = (int)(currentAbsTime / MS_PER_PIXEL);
                    int py = i * ROW_HEIGHT;
                    
                    Rectangle rect = new Rectangle(px, py, 15, ROW_HEIGHT - 2);
                    if (rect.Contains(e.Location))
                    {
                        lstSteps.SelectedIndex = i;
                        _draggingStepIndex = i;
                        _dragStartX = e.X;
                        _dragStartDelay = step.DelayMs;
                        return;
                    }
                }
            }
        }

        private void PnlTimeline_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingStepIndex != -1 && e.Button == MouseButtons.Left)
            {
                int dx = e.X - _dragStartX;
                int dDelay = (int)(dx * MS_PER_PIXEL);
                int newDelay = _dragStartDelay + dDelay;
                if (newDelay < 0) newDelay = 0;
                
                _action.MacroSteps[_draggingStepIndex].DelayMs = newDelay;
                
                if (lstSteps.SelectedIndex == _draggingStepIndex)
                {
                    _isUpdatingUI = true;
                    numDelay.Value = newDelay;
                    _isUpdatingUI = false;
                }
                
                pnlTimeline.Invalidate();
            }
        }

        private void PnlTimeline_MouseUp(object sender, MouseEventArgs e)
        {
            if (_draggingStepIndex != -1)
            {
                _draggingStepIndex = -1;
                RefreshMacroList(lstSteps.SelectedIndex);
            }
        }

        private void MacroEditorForm_FormClosed(object sender, FormClosedEventArgs e) { if (GlobalHookManager.Instance != null) { GlobalHookManager.Instance.IsRecording = false; GlobalHookManager.Instance.OnRecordedInput -= Hook_OnRecordedInput; } }
        private void btnOK_Click(object sender, EventArgs e) { _action.PlaybackMode = (MacroPlaybackMode)cmbPlaybackMode.SelectedIndex; _action.StepTimeoutMs = (int)numTimeout.Value; this.DialogResult = DialogResult.OK; this.Close(); }
    }
}
