using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class ControllerSetupForm : Form
    {
        private DirectInputManager _diManager;
        private ProfileManager _profileManager;
        
        enum ReqType { Button, Axis, Trigger, POV }
        private class SetupStep
        {
            public string Instruction { get; set; }
            public ActionType ActionType { get; set; }
            public int TargetArg { get; set; }
            public ReqType Req { get; set; }
            public bool IsPositive { get; set; } 
        }

        private List<SetupStep> _steps;
        private int _currentStepIndex = 0;
        private bool _waitingForNeutral = false;
        private Dictionary<int, int> _axisNeutrals = new Dictionary<int, int>();

        public ControllerSetupForm(ProfileManager profileManager, DirectInputManager diManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _diManager = diManager;

            // ★追加: セットアップ中は割り当てがなくても一時的にスティックの監視を強制オンにする
            if (_diManager != null)
            {
                _diManager.ForceEnableAxisEvents = true;
            }

            _steps = new List<SetupStep>
            {
                new SetupStep { Instruction = "【左スティック】を『上』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 2, Req = ReqType.Axis, IsPositive = false },
                new SetupStep { Instruction = "【左スティック】を『右』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 1, Req = ReqType.Axis, IsPositive = true },
                new SetupStep { Instruction = "【右スティック】を『上』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 4, Req = ReqType.Axis, IsPositive = false },
                new SetupStep { Instruction = "【右スティック】を『右』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 3, Req = ReqType.Axis, IsPositive = true },
                new SetupStep { Instruction = "【左トリガー(LT)】を深く押し込んでください", ActionType = ActionType.XboxTrigger, TargetArg = 1, Req = ReqType.Trigger },
                new SetupStep { Instruction = "【右トリガー(RT)】を深く押し込んでください", ActionType = ActionType.XboxTrigger, TargetArg = 2, Req = ReqType.Trigger },
                new SetupStep { Instruction = "【十字キー】の『上』を押してください", ActionType = ActionType.XboxController, TargetArg = 11, Req = ReqType.POV },
                new SetupStep { Instruction = "【十字キー】の『下』を押してください", ActionType = ActionType.XboxController, TargetArg = 12, Req = ReqType.POV },
                new SetupStep { Instruction = "【十字キー】の『左』を押してください", ActionType = ActionType.XboxController, TargetArg = 13, Req = ReqType.POV },
                new SetupStep { Instruction = "【十字キー】の『右』を押してください", ActionType = ActionType.XboxController, TargetArg = 14, Req = ReqType.POV },
                new SetupStep { Instruction = "【Aボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 1, Req = ReqType.Button },
                new SetupStep { Instruction = "【Bボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 2, Req = ReqType.Button },
                new SetupStep { Instruction = "【Xボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 3, Req = ReqType.Button },
                new SetupStep { Instruction = "【Yボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 4, Req = ReqType.Button },
                new SetupStep { Instruction = "【LB(L1)ボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 5, Req = ReqType.Button },
                new SetupStep { Instruction = "【RB(R1)ボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 6, Req = ReqType.Button },
                new SetupStep { Instruction = "【Start(Menu)】を押してください", ActionType = ActionType.XboxController, TargetArg = 8, Req = ReqType.Button },
                new SetupStep { Instruction = "【Select(View)】を押してください", ActionType = ActionType.XboxController, TargetArg = 7, Req = ReqType.Button },
                new SetupStep { Instruction = "【左スティック押込(L3)】を押してください", ActionType = ActionType.XboxController, TargetArg = 9, Req = ReqType.Button },
                new SetupStep { Instruction = "【右スティック押込(R3)】を押してください", ActionType = ActionType.XboxController, TargetArg = 10, Req = ReqType.Button }
            };

            UpdateUI();
            _diManager.OnInputEvent += DiManager_OnInputEvent;
        }

        private void DiManager_OnInputEvent(object sender, DirectInputEvent e)
        {
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => DiManager_OnInputEvent(sender, e))); return; }
            if (_currentStepIndex >= _steps.Count) return;
            var step = _steps[_currentStepIndex];

            if (_waitingForNeutral)
            {
                if (e.Type == 10 && e.Value > 0) return; 
                if (e.Type == 12 && e.Value != -1) return; 
                if (e.Type == 11)
                {
                    if (!_axisNeutrals.ContainsKey(e.Code)) _axisNeutrals[e.Code] = e.Value;
                    if (Math.Abs(e.Value - _axisNeutrals[e.Code]) > 10000) return;
                }
                
                _waitingForNeutral = false;
                _currentStepIndex++;
                if (_currentStepIndex >= _steps.Count) FinishSetup();
                else UpdateUI();
                return;
            }

            if (step.Req == ReqType.Axis)
            {
                if (e.Type == 11)
                {
                    if (!_axisNeutrals.ContainsKey(e.Code)) _axisNeutrals[e.Code] = e.Value;
                    int diff = e.Value - _axisNeutrals[e.Code];
                    if (Math.Abs(diff) < 20000) return; 

                    bool inputIsPositive = diff > 0;
                    bool invert = step.IsPositive != inputIsPositive;

                    var b = new UsbInputMapper.Profiles.Binding
                    {
                        Name = step.ActionType.ToString() + "_" + step.TargetArg, DeviceIdentifier = e.DeviceIdentifier,
                        InputType = e.Type, InputCode = e.Code, InvertAxis = invert, AxisRange = 0, DeadZone = 15,
                        Action = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.TargetArg }
                    };
                    _profileManager.ControllerBaseBindings.RemoveAll(x => x.InputType == e.Type && x.InputCode == e.Code && x.AxisRange == 0);
                    _profileManager.ControllerBaseBindings.Add(b);
                    _waitingForNeutral = true; lblInstruction.Text = "スティックを離してください...";
                }
            }
            else if (step.Req == ReqType.Trigger)
            {
                if (e.Type == 11) 
                {
                    if (!_axisNeutrals.ContainsKey(e.Code)) _axisNeutrals[e.Code] = e.Value;
                    int diff = e.Value - _axisNeutrals[e.Code];
                    if (Math.Abs(diff) < 15000) return; 

                    bool inputIsPositive = diff > 0;
                    var b = new UsbInputMapper.Profiles.Binding
                    {
                        Name = step.ActionType.ToString() + "_" + step.TargetArg, DeviceIdentifier = e.DeviceIdentifier,
                        InputType = e.Type, InputCode = e.Code, InvertAxis = false, AxisRange = inputIsPositive ? 1 : 2, DeadZone = 15,
                        Action = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.TargetArg }
                    };
                    _profileManager.ControllerBaseBindings.RemoveAll(x => x.InputType == e.Type && x.InputCode == e.Code && x.AxisRange == b.AxisRange);
                    _profileManager.ControllerBaseBindings.Add(b);
                    _waitingForNeutral = true; lblInstruction.Text = "トリガーを離してください...";
                }
                else if (e.Type == 10 && e.Value > 0) 
                {
                    var b = new UsbInputMapper.Profiles.Binding
                    {
                        Name = step.ActionType.ToString() + "_" + step.TargetArg, DeviceIdentifier = e.DeviceIdentifier,
                        InputType = e.Type, InputCode = e.Code,
                        Action = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.TargetArg }
                    };
                    _profileManager.ControllerBaseBindings.Add(b);
                    _waitingForNeutral = true; lblInstruction.Text = "トリガーを離してください...";
                }
            }
            else if (step.Req == ReqType.POV)
            {
                if (e.Type == 12 && e.Value != -1)
                {
                    var b = new UsbInputMapper.Profiles.Binding
                    {
                        Name = step.ActionType.ToString() + "_" + step.TargetArg, DeviceIdentifier = e.DeviceIdentifier,
                        InputType = e.Type, InputCode = e.Value, 
                        Action = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.TargetArg }
                    };
                    _profileManager.ControllerBaseBindings.Add(b);
                    _waitingForNeutral = true; lblInstruction.Text = "十字キーを離してください...";
                }
            }
            else if (step.Req == ReqType.Button)
            {
                if (e.Type == 10 && e.Value > 0)
                {
                    var b = new UsbInputMapper.Profiles.Binding
                    {
                        Name = step.ActionType.ToString() + "_" + step.TargetArg, DeviceIdentifier = e.DeviceIdentifier,
                        InputType = e.Type, InputCode = e.Code,
                        Action = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.TargetArg }
                    };
                    _profileManager.ControllerBaseBindings.Add(b);
                    _waitingForNeutral = true; lblInstruction.Text = "ボタンを離してください...";
                }
            }
        }

        private void UpdateUI() { lblProgress.Text = $"ステップ: {_currentStepIndex + 1} / {_steps.Count}"; lblInstruction.Text = _steps[_currentStepIndex].Instruction; }

        private void FinishSetup()
        {
            _diManager.OnInputEvent -= DiManager_OnInputEvent;
            
            // ★追加: セットアップが終わったらスティックの強制監視を解除
            if (_diManager != null) _diManager.ForceEnableAxisEvents = false;

            MessageBox.Show("コントローラーのベース設定が完了しました！\r\nXInput出力を有効にしたプロファイルで適用されます。", "完了");
            this.DialogResult = DialogResult.OK; this.Close();
        }

        private void btnSkip_Click(object sender, EventArgs e) { _waitingForNeutral = false; _currentStepIndex++; if (_currentStepIndex >= _steps.Count) FinishSetup(); else UpdateUI(); }
        
        private void btnCancel_Click(object sender, EventArgs e) 
        { 
            this.DialogResult = DialogResult.Cancel; 
            this.Close(); 
        }
        
        private void ControllerSetupForm_FormClosed(object sender, FormClosedEventArgs e) 
        { 
            _diManager.OnInputEvent -= DiManager_OnInputEvent; 
            
            // ★追加: キャンセルや×ボタンで閉じられた場合も強制監視を解除
            if (_diManager != null) _diManager.ForceEnableAxisEvents = false;
        }
    }
}
