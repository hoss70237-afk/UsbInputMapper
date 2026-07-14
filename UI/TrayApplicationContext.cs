private void RawInputManager_OnInputEvent(object sender, InputEvent e)
        {
            if (CaptureForm.IsCapturing) { CaptureForm.CurrentInstance?.ProcessInput(e); return; }

            int inputCode = (e.Type == 1) ? e.VKey : (int)e.MouseButtonFlags;
            string keyId = $"{e.Type}_{inputCode}";

            if (e.IsKeyDown) _physicalKeysDown[keyId] = true;
            else _physicalKeysDown.TryRemove(keyId, out _);

            // ★修正: 該当するすべてのBindingを取得してループで全実行する
            var bindings = FindAllMatchingBindings(e.DeviceIdentifier, e.Type, inputCode);
            if (bindings.Count == 0) return;

            foreach (var binding in bindings)
            {
                // Bindingごとに一意のループキーを作成（かぶり実行対応）
                string loopKey = $"{e.DeviceIdentifier}_{e.Type}_{inputCode}_{binding.GetHashCode()}";

                // ホイール(4,5)は長押しやループの概念がないため、即時ワンショット実行
                if (e.Type == 0 && (inputCode == 4 || inputCode == 5))
                {
                    if (e.IsKeyDown)
                    {
                        ExecuteAction(binding.Action, true, CancellationToken.None, loopKey);
                        ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                    }
                    continue; // ホイールはここまで
                }

                if (e.IsKeyDown)
                {
                    if (binding.Condition == TriggerCondition.Release) continue;
                    if (_activeLoops.ContainsKey(loopKey)) continue;

                    var cts = new CancellationTokenSource();
                    _activeLoops[loopKey] = cts;

                    Task.Run(async () =>
                    {
                        try
                        {
                            if (binding.Condition == TriggerCondition.Hold)
                            {
                                await Task.Delay(binding.ConditionParam, cts.Token);
                                ExecuteAction(binding.Action, true, cts.Token, loopKey);
                            }
                            else if (binding.Condition == TriggerCondition.RapidFire)
                            {
                                while (!cts.Token.IsCancellationRequested)
                                {
                                    ExecuteAction(binding.Action, true, cts.Token, loopKey);
                                    await Task.Delay(20);
                                    ExecuteAction(binding.Action, false, cts.Token, loopKey);
                                    await Task.Delay(Math.Max(10, binding.ConditionParam), cts.Token);
                                }
                            }
                            else
                            {
                                if (binding.Action.ActionType == ActionType.ToggleHold)
                                {
                                    bool currentState = _toggleStates.GetOrAdd(loopKey, false);
                                    _toggleStates[loopKey] = !currentState;
                                    _dispatcher.Dispatch(binding.Action, !currentState);
                                }
                                else ExecuteAction(binding.Action, true, cts.Token, loopKey);
                            }
                        }
                        catch (TaskCanceledException) { }
                    }, cts.Token);
                }
                else
                {
                    if (_activeLoops.TryRemove(loopKey, out var cts)) { cts.Cancel(); cts.Dispose(); }

                    if (binding.Condition == TriggerCondition.Release)
                    {
                        ExecuteAction(binding.Action, true, CancellationToken.None, loopKey);
                        Thread.Sleep(20);
                        ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                    }
                    else if (binding.Action.ActionType != ActionType.ToggleHold)
                    {
                        ExecuteAction(binding.Action, false, CancellationToken.None, loopKey);
                    }
                }
            }
        }

        private List<UsbInputMapper.Profiles.Binding> FindAllMatchingBindings(string deviceId, int inputType, int inputCode)
        {
            if (_profileManager.CurrentProfile == null) return new List<UsbInputMapper.Profiles.Binding>();
            return _profileManager.CurrentProfile.Bindings
                .Where(b => b.DeviceIdentifier == deviceId && b.InputType == inputType && b.InputCode == inputCode)
                .Where(b => b.SubTriggers == null || b.SubTriggers.All(st => _physicalKeysDown.ContainsKey($"{st.Type}_{st.Code}")))
                .ToList();
        }

        private void PlayMacroStep(MacroStep step, MacroPlaybackMode currentMode)
        {
            Thread.Sleep(step.DelayMs);
            var tempDef = new ActionDef { 
                ActionType = step.ActionType, ArgumentNum = step.ArgumentNum, MultipleKeys = step.MultipleKeys, ArgumentStr = step.ArgumentStr,
                MouseX = step.MouseX, MouseY = step.MouseY
            };

            // 競合状態（ステップ再生以外でDown/Upが指定された場合）は強制的にTap扱いにする
            StepPressState state = step.PressState;
            if (currentMode != MacroPlaybackMode.StepByStep && (state == StepPressState.Down || state == StepPressState.Up))
            {
                state = StepPressState.Tap;
            }

            if (state == StepPressState.Down)
            {
                _dispatcher.Dispatch(tempDef, true);
            }
            else if (state == StepPressState.Up)
            {
                _dispatcher.Dispatch(tempDef, false);
            }
            else // Tap
            {
                _dispatcher.Dispatch(tempDef, true);
                Thread.Sleep(20); 
                _dispatcher.Dispatch(tempDef, false);
            }
        }
