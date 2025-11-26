using System.Globalization;
using System.Text;
using DisplayAdapterNameChanger.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DisplayAdapterNameChanger.Core;

class ConfigManager
{
    private readonly string _configFilePath;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private AppConfig? _config;

    public ConfigManager()
    {
        var configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DisplayAdapterNameChanger");

        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }

        _configFilePath = Path.Combine(configDirectory, "config.yaml");

        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    private AppConfig LoadConfig()
    {
        if (_config != null)
            return _config;

        try
        {
            if (File.Exists(_configFilePath))
            {
                var yaml = File.ReadAllText(_configFilePath, Encoding.UTF8);
                _config = _deserializer.Deserialize<AppConfig>(yaml) ?? new AppConfig();
            }
            else
            {
                _config = new AppConfig();
                // Auto-detect system language
                var systemLanguage = GetSystemLanguage();
                _config.Language = systemLanguage;
            }
        }
        catch
        {
            _config = new AppConfig();
            _config.Language = GetSystemLanguage();
        }

        return _config;
    }

    private void SaveConfig()
    {
        try
        {
            var yaml = _serializer.Serialize(_config);
            File.WriteAllText(_configFilePath, yaml, Encoding.UTF8);
        }
        catch
        {
            // Ignore errors
        }
    }

    private string GetSystemLanguage()
    {
        try
        {
            var culture = CultureInfo.CurrentUICulture;
            var languageCode = culture.Name;
            
            // Check if we support this language
            var supportedLanguages = GetSupportedLanguages();
            if (supportedLanguages.Contains(languageCode))
            {
                return languageCode;
            }
            
            // Try to match by language (e.g., zh-TW -> zh-CN)
            var language = culture.TwoLetterISOLanguageName;
            if (language == "zh")
            {
                return "zh-CN";
            }
        }
        catch
        {
            // Ignore errors
        }

        return "en";
    }

    public string GetLanguage()
    {
        var config = LoadConfig();
        return config.Language;
    }

    public void SetLanguage(string languageCode)
    {
        var config = LoadConfig();
        config.Language = languageCode;
        _config = config; // Update cached config
        SaveConfig();
    }

    public Dictionary<string, BackupData> GetDeviceBackups()
    {
        var config = LoadConfig();
        return config.DeviceBackups;
    }

    public void SaveDeviceBackups(Dictionary<string, BackupData> backups)
    {
        var config = LoadConfig();
        config.DeviceBackups = backups;
        _config = config; // Update cached config
        SaveConfig();
    }

    public void UpdateDeviceBackup(string deviceKey, BackupData backupData)
    {
        var config = LoadConfig();
        config.DeviceBackups[deviceKey] = backupData;
        _config = config; // Update cached config
        SaveConfig();
    }

    public void RemoveDeviceBackup(string deviceKey)
    {
        var config = LoadConfig();
        config.DeviceBackups.Remove(deviceKey);
        _config = config; // Update cached config
        SaveConfig();
    }

    public List<string> GetSupportedLanguages()
    {
        var languages = new List<string> { "en" }; // English is always supported (hardcoded)
        
        try
        {
            var languagesDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Languages");

            if (Directory.Exists(languagesDir))
            {
                var files = Directory.GetFiles(languagesDir, "*.yaml");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName != "en" && !languages.Contains(fileName))
                    {
                        languages.Add(fileName);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return languages.OrderBy(l => l).ToList();
    }
}

