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
        private Profile _targetProfile;
        
        private class SetupStep
        {
            public string Instruction { get; set; }
            public ActionType ActionType { get; set; }
            public int TargetArg { get; set; }
            public bool IsAxis { get; set; }
            public bool IsPositive { get; set; }
        }

        private List<SetupStep> _steps;
        private int _currentStepIndex = 0;
        private bool _waitingForNeutral = false;
        private Dictionary<int, int> _axisNeutrals = new Dictionary<int, int>();

        public ControllerSetupForm(Profile profile, DirectInputManager diManager)
        {
            InitializeComponent();
            _targetProfile = profile;
            _diManager = diManager;

            // XInputの標準的な全ボタン・軸を設定するリスト
            _steps = new List<SetupStep>
            {
                new SetupStep { Instruction = "【左スティック】を『上』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 2, IsAxis = true, IsPositive = false },
                new SetupStep { Instruction = "【左スティック】を『右』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 1, IsAxis = true, IsPositive = true },
                new SetupStep { Instruction = "【右スティック】を『上』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 4, IsAxis = true, IsPositive = false },
                new SetupStep { Instruction = "【右スティック】を『右』に倒してください", ActionType = ActionType.XboxAxis, TargetArg = 3, IsAxis = true, IsPositive = true },
                new SetupStep { Instruction = "【左トリガー(LT)】を深く押し込んでください", ActionType = ActionType.XboxTrigger, TargetArg = 1, IsAxis = true, IsPositive = true },
                new SetupStep { Instruction = "【右トリガー(RT)】を深く押し込んでください", ActionType = ActionType.XboxTrigger, TargetArg = 2, IsAxis = true, IsPositive = true },
                new SetupStep { Instruction = "【Aボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 1, IsAxis = false },
                new SetupStep { Instruction = "【Bボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 2, IsAxis = false },
                new SetupStep { Instruction = "【Xボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 3, IsAxis = false },
                new SetupStep { Instruction = "【Yボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 4, IsAxis = false },
                new SetupStep { Instruction = "【LB(L1)ボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 5, IsAxis = false },
                new SetupStep { Instruction = "【RB(R1)ボタン】を押してください", ActionType = ActionType.XboxController, TargetArg = 6, IsAxis = false },
                new SetupStep { Instruction = "【十字キー】の『上』を押してください", ActionType = ActionType.XboxController, TargetArg = 11, IsAxis = false },
                new SetupStep { Instruction = "【十字キー】の『下』を押してください", ActionType = ActionType.XboxController, TargetArg = 12, IsAxis = false },
                new SetupStep { Instruction = "【十字キー】の『左』を押してください", ActionType = ActionType.XboxController, TargetArg = 13, IsAxis = false },
                new SetupStep { Instruction = "【十字キー】の『右』を押してください", ActionType = ActionType.XboxController, TargetArg = 14, IsAxis = false },
                new SetupStep { Instruction = "【Start(Menu)】を押してください", ActionType = ActionType.XboxController, TargetArg = 8, IsAxis = false },
                new SetupStep { Instruction = "【Select(View)】を押してください", ActionType = ActionType.XboxController, TargetArg = 7, IsAxis = false },
                new SetupStep { Instruction = "【左スティック押込(L3)】を押してください", ActionType = ActionType.XboxController, TargetArg = 9, IsAxis = false },
                new SetupStep { Instruction = "【右スティック押込(R3)】を押してください", ActionType = ActionType.XboxController, TargetArg = 10, IsAxis = false }
            };

            UpdateUI();
            _diManager.OnInputEvent += DiManager_OnInputEvent;
        }

        private void DiManager_OnInputEvent(object sender, DirectInputEvent e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => DiManager_OnInputEvent(sender, e)));
                return;
            }

            if (_currentStepIndex >= _steps.Count) return;
            var step = _steps[_currentStepIndex];

            // ニュートラル待ち（すべてのボタンが離され、軸が中央に戻るのを待つ）
            if (_waitingForNeutral)
            {
                if (e.Type == 10 && e.Value > 0) return; // ボタンが押されている
                if (e.Type == 11)
                {
                    if (!_axisNeutrals.ContainsKey(e.Code)) _axisNeutrals[e.Code] = e.Value;
                    int diff = Math.Abs(e.Value - _axisNeutrals[e.Code]);
                    if (diff > 10000) return; // 軸がまだ倒れている
                }
                
                // 完全なニュートラルを確認
                _waitingForNeutral = false;
                _currentStepIndex++;
                if (_currentStepIndex >= _steps.Count) FinishSetup();
                else UpdateUI();
                return;
            }

            // --- 入力検知 ---
            // ★ノイズ除去フィルター
            if (e.Type == 11)
            {
                if (!_axisNeutrals.ContainsKey(e.Code)) _axisNeutrals[e.Code] = e.Value;
                int diff = e.Value - _axisNeutrals[e.Code];
                if (Math.Abs(diff) < 20000) return; // しきい値（大きく倒さないと反応しない）

                if (step.IsAxis)
                {
                    bool inputIsPositive = diff > 0;
                    
                    // スティックの上と左はマイナス値として入ってくるコントローラーが多い
                    bool invert = step.IsPositive != inputIsPositive; 

                    var b = new UsbInputMapper.Profiles.Binding
                    {
                        Name = step.ActionType.ToString() + "_" + step.TargetArg,
                        DeviceIdentifier = e.DeviceIdentifier,
                        InputType = e.Type,
                        InputCode = e.Code,
                        InvertAxis = invert,
                        DeadZone = 15,
                        Action = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.TargetArg }
                    };
                    
                    // すでに同じ軸の登録があるか確認（Y軸なら上と下で共通のBindingにする処理が必要だが、今回は軸まるごと登録とする）
                    var existing = _targetProfile.Bindings.FirstOrDefault(x => x.InputType == 11 && x.InputCode == e.Code);
                    if (existing == null) _targetProfile.Bindings.Add(b);

                    _waitingForNeutral = true;
                    lblInstruction.Text = "スティック/トリガーを離してニュートラルに戻してください...";
                }
            }
            else if (e.Type == 10 && e.Value > 0)
            {
                if (!step.IsAxis)
                {
                    var b = new UsbInputMapper.Profiles.Binding
                    {
                        Name = step.ActionType.ToString() + "_" + step.TargetArg,
                        DeviceIdentifier = e.DeviceIdentifier,
                        InputType = e.Type,
                        InputCode = e.Code,
                        Action = new ActionDef { ActionType = step.ActionType, ArgumentNum = step.TargetArg }
                    };
                    _targetProfile.Bindings.Add(b);

                    _waitingForNeutral = true;
                    lblInstruction.Text = "ボタンを離してください...";
                }
            }
        }

        private void UpdateUI()
        {
            lblProgress.Text = $"ステップ: {_currentStepIndex + 1} / {_steps.Count}";
            lblInstruction.Text = _steps[_currentStepIndex].Instruction;
        }

        private void FinishSetup()
        {
            _diManager.OnInputEvent -= DiManager_OnInputEvent;
            // XInput出力を自動的にONにする
            _targetProfile.EnableXInput = true;
            MessageBox.Show("コントローラーのセットアップが完了しました！\r\nこのプロファイルではXInput出力が有効になります。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            _waitingForNeutral = false;
            _currentStepIndex++;
            if (_currentStepIndex >= _steps.Count) FinishSetup();
            else UpdateUI();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _diManager.OnInputEvent -= DiManager_OnInputEvent;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void ControllerSetupForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _diManager.OnInputEvent -= DiManager_OnInputEvent;
        }
    }
}
