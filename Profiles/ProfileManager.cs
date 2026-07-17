using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace UsbInputMapper.Profiles
{
    public class ProfileManager
    {
        private readonly string _settingsFilePath;
        private readonly string _controllerBaseFilePath; // ★追加: ベースプロファイル保存先
        
        public List<Profile> Profiles { get; private set; }
        public List<Binding> ControllerBaseBindings { get; private set; } // ★追加
        
        public Profile CurrentProfile { get; private set; }
        public Profile TemporaryProfile { get; set; }
        public Profile CurrentActiveProfile => TemporaryProfile ?? CurrentProfile;

        public event EventHandler OnProfileChanged;

        public ProfileManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "UsbInputMapper");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            
            _settingsFilePath = Path.Combine(appFolder, "profiles.json");
            _controllerBaseFilePath = Path.Combine(appFolder, "controller_base.json");
            
            Profiles = new List<Profile>();
            ControllerBaseBindings = new List<Binding>();
        }

        public void Load()
        {
            if (File.Exists(_settingsFilePath))
            {
                try { Profiles = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(_settingsFilePath)) ?? new List<Profile>(); }
                catch { Profiles = new List<Profile>(); }
            }
            if (Profiles.Count == 0) Profiles.Add(new Profile { Name = "Default", IsDefault = true });
            CurrentProfile = Profiles.Find(p => p.IsDefault) ?? Profiles[0];

            // ベースプロファイル読み込み
            if (File.Exists(_controllerBaseFilePath))
            {
                try { ControllerBaseBindings = JsonConvert.DeserializeObject<List<Binding>>(File.ReadAllText(_controllerBaseFilePath)) ?? new List<Binding>(); }
                catch { ControllerBaseBindings = new List<Binding>(); }
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(Profiles, Formatting.Indented));
                File.WriteAllText(_controllerBaseFilePath, JsonConvert.SerializeObject(ControllerBaseBindings, Formatting.Indented));
            }
            catch { }
        }

        // 既存処理は省略せずそのまま
        public void DuplicateProfile(Profile source) { var c = JsonConvert.DeserializeObject<Profile>(JsonConvert.SerializeObject(source)); c.Name += " のコピー"; c.IsDefault = false; Profiles.Add(c); Save(); }
        public void MoveProfile(int index, int direction) { int n = index + direction; if (n >= 0 && n < Profiles.Count) { var item = Profiles[index]; Profiles.RemoveAt(index); Profiles.Insert(n, item); Save(); } }
        public void MoveBinding(List<Binding> list, int index, int direction) { int n = index + direction; if (n >= 0 && n < list.Count) { var item = list[index]; list.RemoveAt(index); list.Insert(n, item); Save(); } }
        public void SwitchToAppProfile(string appPath)
        {
            TemporaryProfile = null;
            if (string.IsNullOrEmpty(appPath)) { SwitchToDefault(); return; }
            string exeName = Path.GetFileName(appPath).ToLower();
            var matched = Profiles.Find(p => !p.IsDefault && p.TargetApplicationPaths != null && p.TargetApplicationPaths.Exists(t => t.ToLower() == exeName));
            if (matched != null && CurrentProfile != matched) { CurrentProfile = matched; OnProfileChanged?.Invoke(this, EventArgs.Empty); }
            else if (matched == null) SwitchToDefault();
        }
        public void SwitchToDefault() { TemporaryProfile = null; var def = Profiles.Find(p => p.IsDefault) ?? Profiles[0]; if (CurrentProfile != def) { CurrentProfile = def; OnProfileChanged?.Invoke(this, EventArgs.Empty); } }
        public void NotifyProfileSwitchedManually() { OnProfileChanged?.Invoke(this, EventArgs.Empty); }
    }
}
