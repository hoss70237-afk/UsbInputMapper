using System;

namespace UsbInputMapper.Profiles
{
    public enum ActionType
    {
        None,
        Keyboard,
        Mouse,
        XboxController,
        DirectInput,
        AppLaunch,
        Macro
    }

    public class ActionDef
    {
        public ActionType ActionType { get; set; }
        
        // キーボードの仮想キーコードや、コントローラーのボタンIDなどを格納
        public int ArgumentNum { get; set; }
        
        // アプリ起動時の実行パスなどを格納
        public string ArgumentStr { get; set; }
        
        // 引数などを格納
        public string ArgumentExtraStr { get; set; }

        public override string ToString()
        {
            switch (ActionType)
            {
                case ActionType.Keyboard:
                    return $"KB Key: {ArgumentNum}";
                case ActionType.AppLaunch:
                    return $"Launch: {ArgumentStr}";
                case ActionType.XboxController:
                    return $"Xbox Button: {ArgumentNum}";
                default:
                    return ActionType.ToString();
            }
        }
    }
}
