using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TazUOLauncher;

class Profile
{
    private Settings? _CUOSettings;
    public bool IsDeleted = false;
    private string name = ProfileManager.EnsureUniqueName("Blank Profile");

    public string Name { get => name; set { name = value; } }
    public string SettingsFile { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = Guid.NewGuid().ToString();
    public string LastCharacterName { get; set; } = string.Empty;
    public string AdditionalArgs { get; set; } = string.Empty;

    [JsonIgnore]
    public Settings CUOSettings
    {
        get
        {
            if (_CUOSettings == null)
            {
                LoadCUOSettings();
                return _CUOSettings ?? new Settings();
            }
            else
            {
                return _CUOSettings;
            }
        }
        private set => _CUOSettings = value;
    }

    public void OverrideSettings(Settings settings)
    {
        _CUOSettings = settings;
    }
    
    private void LoadCUOSettings()
    {
        if (File.Exists(GetSettingsFilePath()))
        {
            try
            {
                var data = JsonSerializer.Deserialize<Settings>(File.ReadAllText(GetSettingsFilePath()));
                if (data != null)
                {
                    CUOSettings = data;
                }
                else
                {
                    CUOSettings = new Settings();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                _CUOSettings = new Settings();
            }
        }
        else
        {
            _CUOSettings = new Settings();
        }
    }

    public void Save()
    {
        try
        {
            var data = JsonSerializer.Serialize(this, typeof(Profile));
            Directory.CreateDirectory(PathHelper.ProfilesPath);
            File.WriteAllText(GetProfileFilePath(), data);

            if (CUOSettings == null) return;

            var settingsData = CUOSettings.GetSaveData();
            Directory.CreateDirectory(PathHelper.SettingsPath);
            File.WriteAllText(GetSettingsFilePath(), settingsData);
        }
        catch (Exception e)
        {
            Console.WriteLine($"---- Failed to save profile [ {Name} ] ---");
            Console.WriteLine(e.ToString());
            Console.WriteLine();
        }
    }

    public string GetSettingsFilePath()
    {
        return Path.Combine(PathHelper.SettingsPath, SettingsFile + ".json");
    }

    public string GetProfileFilePath()
    {
        return Path.Combine(PathHelper.ProfilesPath, FileName + ".json");
    }
}

