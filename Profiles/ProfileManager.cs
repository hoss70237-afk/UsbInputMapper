using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UsbInputMapper.Core;

namespace UsbInputMapper.Profiles
{
    public class ProfileManager
    {
        private readonly string _settingsFilePath;
        private readonly string _controllerBaseFilePath;
        private readonly string _baseFolder;
        private readonly bool _isPortable;
        
        public List<Profile> Profiles { get; private set; }
        public List<Binding> ControllerBaseBindings { get; private set; } 
        
        public Profile CurrentProfile { get; private set; }
        public Profile TemporaryProfile { get; set; }
        public Profile CurrentActiveProfile => TemporaryProfile ?? CurrentProfile;

        public event EventHandler OnProfileChanged;
        public event EventHandler OnSettingsChanged;

        public ProfileManager()
        {
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string exeProfilePath = Path.Combine(exeFolder, "profiles.json");
            string portableMarker = Path.Combine(exeFolder, "portable.txt");

            // ★追加: ポータブルモード判定
            if (File.Exists(portableMarker) || File.Exists(exeProfilePath))
            {
                _isPortable = true;
                _baseFolder = exeFolder;
            }
            else
            {
                _isPortable = false;
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _baseFolder = Path.Combine(appData, "UsbInputMapper");
                if (!Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);
            }

            _settingsFilePath = Path.Combine(_baseFolder, "profiles.json");
            _controllerBaseFilePath = Path.Combine(_baseFolder, "controller_base.json");
            
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
                // ★追加: 5世代バックアップ機能
                ManageBackups(_settingsFilePath);
                ManageBackups(_controllerBaseFilePath);

                File.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(Profiles, Formatting.Indented));
                File.WriteAllText(_controllerBaseFilePath, JsonConvert.SerializeObject(ControllerBaseBindings, Formatting.Indented));
                
                OnSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        private void ManageBackups(string filePath)
        {
            if (!File.Exists(filePath)) return;
            string dir = Path.GetDirectoryName(filePath);
            string name = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);
            string backupPath = Path.Combine(dir, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
            
            try
            {
                File.Copy(filePath, backupPath, true);
                
                var backups = Directory.GetFiles(dir, $"{name}_*{ext}")
                                       .Select(f => new FileInfo(f))
                                       .OrderByDescending(f => f.CreationTime)
                                       .ToList();
                
                // 最新5つを残して削除
                if (backups.Count > 5)
                {
                    for (int i = 5; i < backups.Count; i++)
                    {
                        backups[i].Delete();
                    }
                }
            }
            catch { }
        }

        public void DuplicateProfile(Profile source) { var c = JsonConvert.DeserializeObject<Profile>(JsonConvert.SerializeObject(source)); c.Name += " のコピー"; c.IsDefault = false; Profiles.Add(c); Save(); }
        public void MoveProfile(int index, int direction) { int n = index + direction; if (n >= 0 && n < Profiles.Count) { var item = Profiles[index]; Profiles.RemoveAt(index); Profiles.Insert(n, item); Save(); } }
        public void MoveBinding(List<Binding> list, int index, int direction) { int n = index + direction; if (n >= 0 && n < list.Count) { var item = list[index]; list.RemoveAt(index); list.Insert(n, item); Save(); } }
        
        public void SwitchToAppProfile(string appPath)
        {
            TemporaryProfile = null;
            if (string.IsNullOrEmpty(appPath)) { SwitchToDefault(); return; }
            string exeName = Path.GetFileName(appPath).ToLower();
            var matched = Profiles.Find(p => !p.IsDefault && p.TargetApplicationPaths != null && p.TargetApplicationPaths.Exists(t => t.ToLower() == exeName));
            if (matched != null && CurrentProfile != matched) { ChangeProfileInternal(matched); }
            else if (matched == null) SwitchToDefault();
        }
        
        public void SwitchToDefault() 
        { 
            TemporaryProfile = null; 
            var def = Profiles.Find(p => p.IsDefault) ?? Profiles[0]; 
            if (CurrentProfile != def) { ChangeProfileInternal(def); } 
        }

        private void ChangeProfileInternal(Profile newProfile)
        {
            CurrentProfile = newProfile;
            
            // ★追加: プロファイルが切り替わった時、前回のOS変更が残っていれば強制リセット
            SystemMouseManager.RestoreAllSafely();
            
            OnProfileChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyProfileSwitchedManually() 
        { 
            SystemMouseManager.RestoreAllSafely();
            OnProfileChanged?.Invoke(this, EventArgs.Empty); 
        }
    }
}
