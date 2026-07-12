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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
            }
        }

        public void SwitchToAppProfile(string appPath)
        {
            if (string.IsNullOrEmpty(appPath))
            {
                SwitchToDefault();
                return;
            }

            string exeName = Path.GetFileName(appPath);

            var matched = Profiles.Find(p => !p.IsDefault && 
                !string.IsNullOrEmpty(p.TargetApplicationPath) && 
                p.TargetApplicationPath.Equals(exeName, StringComparison.OrdinalIgnoreCase));

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
