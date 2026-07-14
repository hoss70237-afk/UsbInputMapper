private void SetupComboBoxes()
        {
            cmbCondition.Items.Add("通常入力 (押した時)");
            cmbCondition.Items.Add("長押し (ミリ秒経過で発動)");
            cmbCondition.Items.Add("連打 (押している間ループ)");
            cmbCondition.Items.Add("離した時 (Key Up)");

            cmbActionType.Items.Add(new ComboItem { Text = "(None)", Value = (int)ActionType.None });
            cmbActionType.Items.Add(new ComboItem { Text = "キーボード入力", Value = (int)ActionType.Keyboard });
            cmbActionType.Items.Add(new ComboItem { Text = "マウスクリック", Value = (int)ActionType.MouseClick });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (相対: 現在地から指定座標分)", Value = (int)ActionType.MouseMoveRelative });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (スピード: 指定方向に移動)", Value = (int)ActionType.MouseContinuousMove });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (絶対: デスクトップ座標へ)", Value = (int)ActionType.MouseMoveAbsoluteDesk });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (絶対: ウィンドウ座標へ)", Value = (int)ActionType.MouseMoveAbsoluteWin });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス座標を保存", Value = (int)ActionType.MousePosSave });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス座標を復元", Value = (int)ActionType.MousePosRestore });
            cmbActionType.Items.Add(new ComboItem { Text = "Xboxコントローラー入力", Value = (int)ActionType.XboxController });
            cmbActionType.Items.Add(new ComboItem { Text = "アプリケーション起動", Value = (int)ActionType.AppLaunch });
            cmbActionType.Items.Add(new ComboItem { Text = "トグル維持", Value = (int)ActionType.ToggleHold });
            cmbActionType.Items.Add(new ComboItem { Text = "マクロ実行", Value = (int)ActionType.Macro });

            // ★追加: 同時押しの手動追加用リスト
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "左クリック", Value = 0x0001 }); // Type=0, Code=1を16進で表現
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "右クリック", Value = 0x0002 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "中クリック", Value = 0x0003 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "ホイール上", Value = 0x0004 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "ホイール下", Value = 0x0005 });
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
                cmbManualSubTrigger.Items.Add(new ComboItem { Text = key.ToString(), Value = 0x010000 | (int)key }); // Type=1は上位ビットに
            cmbManualSubTrigger.SelectedIndex = 0;
        }

        private void btnManualAddSub_Click(object sender, EventArgs e)
        {
            if (cmbManualSubTrigger.SelectedItem is ComboItem item)
            {
                int type = (item.Value & 0x010000) != 0 ? 1 : 0;
                int code = item.Value & 0xFFFF;
                var key = new TriggerKey { DeviceIdentifier = "Any", Type = type, Code = code };
                lstSubTriggers.Items.Add(key);
            }
        }
