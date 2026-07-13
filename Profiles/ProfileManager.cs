using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace UsbInputMapper.Profiles
{
    public class ProfileManager
    {
        private readonly string _settingsFilePath;
        public List<Profile> Profiles { get; private set; }
        public Profile CurrentProfile { get; private set; }

        public event EventHandler OnProfileChanged;

        public ProfileManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "UsbInputMapper");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _settingsFilePath = Path.Combine(appFolder, "profiles.json");
            Profiles = new List<Profile>();
        }

        public void Load()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    Profiles = JsonConvert.DeserializeObject<List<Profile>>(json) ?? new List<Profile>();
                }
                catch
                {
                    Profiles = new List<Profile>();
                }
            }

            if (Profiles.Count == 0)
            {
                Profiles.Add(new Profile { Name = "Default", IsDefault = true });
            }

            CurrentProfile = Profiles.Find(p => p.IsDefault) ?? Profiles[0];
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Profiles, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch { }
        }

        // --- プロファイルの並べ替えと複製 ---

        public void DuplicateProfile(Profile source)
        {
            // ディープコピーを行うためJSONでシリアライズ・デシリアライズする
            string json = JsonConvert.SerializeObject(source);
            var cloned = JsonConvert.DeserializeObject<Profile>(json);
            
            cloned.Name = source.Name + " のコピー";
            cloned.IsDefault = false; // コピーはデフォルトにはならない

            Profiles.Add(cloned);
            Save();
        }

        public void MoveProfile(int index, int direction)
        {
            // direction: -1 (Up), 1 (Down)
            if (index < 0 || index >= Profiles.Count) return;
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= Profiles.Count) return;

            var item = Profiles[index];
            Profiles.RemoveAt(index);
            Profiles.Insert(newIndex, item);
            Save();
        }

        // --- アイテム(Binding)の並べ替え ---

        public void MoveBinding(Profile profile, int index, int direction)
        {
            if (index < 0 || index >= profile.Bindings.Count) return;
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= profile.Bindings.Count) return;

            var item = profile.Bindings[index];
            profile.Bindings.RemoveAt(index);
            profile.Bindings.Insert(newIndex, item);
            Save();
        }

        // --- アクティブ切り替えロジック ---

        public void SwitchToAppProfile(string appPath)
        {
            if (string.IsNullOrEmpty(appPath))
            {
                SwitchToDefault();
                return;
            }

            string exeName = Path.GetFileName(appPath).ToLower();

            // UWPなどの長いパスでも実行ファイル名で正確にマッチングする
            var matched = Profiles.Find(p => !p.IsDefault && 
                p.TargetApplicationPaths != null &&
                p.TargetApplicationPaths.Exists(target => target.ToLower() == exeName));

            if (matched != null)
            {
                if (CurrentProfile != matched)
                {
                    CurrentProfile = matched;
                    OnProfileChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                SwitchToDefault();
            }
        }

        public void SwitchToDefault()
        {
            var def = Profiles.Find(p => p.IsDefault) ?? Profiles[0];
            if (CurrentProfile != def)
            {
                CurrentProfile = def;
                OnProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Binding FindBinding(string deviceId, int inputType, int inputCode)
        {
            if (CurrentProfile == null) return null;

            return CurrentProfile.Bindings.Find(b => 
                b.DeviceIdentifier == deviceId && 
                b.InputType == inputType && 
                b.InputCode == inputCode);
        }
    }
}
