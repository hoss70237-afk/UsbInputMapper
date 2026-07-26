using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UsbInputMapper.Core;

namespace UsbInputMapper.Profiles
{
    public class GlobalSettings
    {
        public bool EnableChatteringCanceler { get; set; } = false;
        public int ChatteringThresholdMs { get; set; } = 20;
        public int DoubleClickTimeMs { get; set; } = 300;
        public int TripleClickTimeMs { get; set; } = 300;
    }

    public class ProfileManager
    {
        private readonly string _settingsFilePath;
        private readonly string _controllerBaseFilePath;
        private readonly string _globalSettingsFilePath;
        private readonly string _baseFolder;
        private readonly object _saveLock = new object();
        
        public List<Profile> Profiles { get; private set; }
        public List<Binding> ControllerBaseBindings { get; private set; } 
        public GlobalSettings GlobalConfig { get; private set; }
        
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

            if (File.Exists(portableMarker) || File.Exists(exeProfilePath))
            {
                _baseFolder = exeFolder;
            }
            else
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _baseFolder = Path.Combine(appData, "UsbInputMapper");
                if (!Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);
            }

            _settingsFilePath = Path.Combine(_baseFolder, "profiles.json");
            _controllerBaseFilePath = Path.Combine(_baseFolder, "controller_base.json");
            _globalSettingsFilePath = Path.Combine(_baseFolder, "global_settings.json");
            
            Profiles = new List<Profile>();
            ControllerBaseBindings = new List<Binding>();
            GlobalConfig = new GlobalSettings();
        }

        public void Load()
        {
            lock (_saveLock)
            {
                if (File.Exists(_globalSettingsFilePath))
                {
                    try { GlobalConfig = JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText(_globalSettingsFilePath)) ?? new GlobalSettings(); }
                    catch { GlobalConfig = new GlobalSettings(); }
                }

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
        }

        public void Save()
        {
            lock (_saveLock)
            {
                try
                {
                    ManageBackups(_settingsFilePath);
                    ManageBackups(_controllerBaseFilePath);
                    ManageBackups(_globalSettingsFilePath);

                    SaveToFileAtomic(_settingsFilePath, Profiles);
                    SaveToFileAtomic(_controllerBaseFilePath, ControllerBaseBindings);
                    SaveToFileAtomic(_globalSettingsFilePath, GlobalConfig);
                    
                    OnSettingsChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    InputLogger.LogError("設定の保存に失敗しました", ex);
                }
            }
        }

        private void SaveToFileAtomic(string filePath, object data)
        {
            string tempPath = filePath + ".tmp";
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(tempPath, json);
            try
            {
                if (File.Exists(filePath))
                {
                    string backupPath = filePath + ".bak";
                    File.Replace(tempPath, filePath, backupPath, true);
                }
                else File.Move(tempPath, filePath);
            }
            catch
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempPath, filePath);
            }
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
                var backups = Directory.GetFiles(dir, $"{name}_*{ext}").Select(f => new FileInfo(f)).OrderByDescending(f => f.CreationTime).ToList();
                if (backups.Count > 5) for (int i = 5; i < backups.Count; i++) backups[i].Delete();
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

        public void SwitchToProfile(string profileName)
        {
            TemporaryProfile = null;
            var target = Profiles.Find(p => p.Name == profileName);
            if (target != null && CurrentProfile != target) ChangeProfileInternal(target);
        }

        public void SetTemporaryProfile(string profileName, bool enable)
        {
            if (enable)
            {
                var target = Profiles.Find(p => p.Name == profileName);
                if (target != null && TemporaryProfile != target)
                {
                    TemporaryProfile = target;
                    ApplyProfileState(TemporaryProfile);
                    OnProfileChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                if (TemporaryProfile != null)
                {
                    TemporaryProfile = null;
                    ApplyProfileState(CurrentProfile);
                    OnProfileChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ChangeProfileInternal(Profile newProfile)
        {
            CurrentProfile = newProfile;
            ApplyProfileState(newProfile);
            OnProfileChanged?.Invoke(this, EventArgs.Empty);
        }
        
        private void ApplyProfileState(Profile profile)
        {
            SystemMouseManager.RestoreAllSafely();
            
            if (profile.EnableXInput)
            {
                HidHideManager.WhitelistCurrentProcess();
                HidHideManager.EnableHiding(true);
            }
            else
            {
                HidHideManager.EnableHiding(false);
            }

            if (profile.NotifyProfileChangeBeep)
            {
                Task.Run(() => System.Media.SystemSounds.Beep.Play());
            }
            if (profile.NotifyProfileChangeTTS)
            {
                PlayTTS($"Profile {profile.Name}");
            }
        }

        private void PlayTTS(string text)
        {
            Task.Run(() => {
                try
                {
                    Type synthType = Type.GetType("System.Speech.Synthesis.SpeechSynthesizer, System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                    if (synthType != null)
                    {
                        object synth = Activator.CreateInstance(synthType);
                        var speakMethod = synthType.GetMethod("Speak", new[] { typeof(string) });
                        speakMethod?.Invoke(synth, new object[] { text });
                    }
                }
                catch (Exception ex)
                {
                    InputLogger.LogError("TTS Playback Failed", ex);
                }
            });
        }

        public void NotifyProfileSwitchedManually() 
        { 
            ApplyProfileState(CurrentActiveProfile);
            OnProfileChanged?.Invoke(this, EventArgs.Empty); 
        }
    }
}
