using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DisplayAdapterNameChanger.Core;

class Localization
{
    private readonly string _resourcesDirectory;
    private readonly IDeserializer _deserializer;
    private Dictionary<string, object>? _currentLanguage;
    private string _currentLanguageCode = "en";

    // English default strings (hardcoded)
    private static readonly Dictionary<string, Dictionary<string, string>> _englishStrings = new()
    {
        ["errors"] = new()
        {
            ["adminRequired"] = "Error: This program requires administrator privileges to modify the registry.",
            ["runAsAdmin"] = "Please run this program as administrator.",
            ["noAdaptersFound"] = "No display adapter devices found.",
            ["noAdaptersAvailable"] = "No display adapters available.",
            ["noBackupsFound"] = "No backups found.",
            ["noBackupsAvailable"] = "No backups available.",
            ["backupNotFound"] = "Backup not found.",
            ["unableToOpenRegistry"] = "Unable to open registry key.",
            ["deviceRemoved"] = "Unable to open registry key. The device may have been removed.",
            ["modificationFailed"] = "Modification failed: Unable to open registry key.",
            ["restoreFailed"] = "Failed to restore backup.",
            ["deleteFailed"] = "Failed to delete backup.",
            ["nameEmpty"] = "Name cannot be empty. Operation cancelled.",
            ["restartingAsAdmin"] = "Restarting with administrator privileges...",
            ["restartFailed"] = "Failed to restart with administrator privileges."
        },
        ["menu"] = new()
        {
            ["selectOperation"] = "Please select an operation:",
            ["viewAdapterList"] = "View Display Adapter List",
            ["modifyAdapterName"] = "Modify Display Adapter Name",
            ["backupManagement"] = "Backup Management",
            ["languageSettings"] = "Language Settings",
            ["exit"] = "Exit",
            ["back"] = "Back"
        },
        ["backup"] = new()
        {
            ["management"] = "Backup Management:",
            ["viewAll"] = "View All Backups",
            ["restore"] = "Restore from Backup",
            ["delete"] = "Delete Backup",
            ["selectToRestore"] = "Please select a backup to restore:",
            ["selectToDelete"] = "Please select a backup to delete:"
        },
        ["adapter"] = new()
        {
            ["selectToModify"] = "Please select the display adapter to modify:",
            ["currentName"] = "Current Name:",
            ["enterNewName"] = "Please enter the new name:",
            ["confirmChange"] = "Are you sure you want to change the name to '{0}'?",
            ["operationCancelled"] = "Operation cancelled.",
            ["backupCreated"] = "Backup created: {0}",
            ["usingExistingBackup"] = "Using existing backup (original value preserved)",
            ["backupFailed"] = "Warning: Failed to create backup. Continue anyway?",
            ["continueWithoutBackup"] = "Continue without backup?",
            ["nameModified"] = "Name modified successfully!",
            ["noteRestart"] = "Note: You may need to restart your computer or reinstall the driver to see the changes."
        },
        ["table"] = new()
        {
            ["no"] = "No.",
            ["deviceId"] = "Device ID",
            ["currentName"] = "Current Name",
            ["backupId"] = "Backup ID",
            ["originalName"] = "Original Name",
            ["createdTime"] = "Created Time"
        },
        ["confirm"] = new()
        {
            ["restoreBackup"] = "Are you sure you want to restore backup '{0}'?",
            ["deleteBackup"] = "Are you sure you want to delete backup '{0}'?"
        },
        ["success"] = new()
        {
            ["backupRestored"] = "Backup restored successfully!",
            ["backupDeleted"] = "Backup deleted successfully!",
            ["goodbye"] = "Goodbye!",
            ["languageChanged"] = "Language changed to {0}"
        },
        ["common"] = new()
        {
            ["pressAnyKey"] = "Press any key to continue..."
        }
    };

    public Localization()
    {
        _resourcesDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Resources",
            "Languages");

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public List<string> GetAvailableLanguages()
    {
        var languages = new List<string> { "en" }; // English is always available
        
        try
        {
            if (Directory.Exists(_resourcesDirectory))
            {
                var files = Directory.GetFiles(_resourcesDirectory, "*.yaml");
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

    public bool LoadLanguage(string languageCode)
    {
        try
        {
            if (languageCode == "en")
            {
                // Use hardcoded English strings
                _currentLanguage = _englishStrings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value.ToDictionary(
                        ikvp => ikvp.Key,
                        ikvp => (object)ikvp.Value));
                _currentLanguageCode = "en";
                return true;
            }

            var filePath = Path.Combine(_resourcesDirectory, $"{languageCode}.yaml");
            
            if (!File.Exists(filePath))
            {
                // Fallback to English
                return LoadLanguage("en");
            }

            var yaml = File.ReadAllText(filePath, Encoding.UTF8);
            _currentLanguage = _deserializer.Deserialize<Dictionary<string, object>>(yaml);
            _currentLanguageCode = languageCode;
            return true;
        }
        catch
        {
            // Fallback to English
            return LoadLanguage("en");
        }
    }

    public string GetString(string key, params object[] args)
    {
        try
        {
            if (_currentLanguage == null)
            {
                LoadLanguage("en");
            }

            var keys = key.Split('.');
            if (keys.Length < 2)
            {
                return key;
            }

            var module = keys[0];
            var property = keys[1];

            if (_currentLanguage != null && _currentLanguage.ContainsKey(module))
            {
                var moduleData = _currentLanguage[module];
                if (moduleData is Dictionary<object, object> dict)
                {
                    if (dict.ContainsKey(property))
                    {
                        var result = dict[property]?.ToString() ?? key;
                        if (args.Length > 0 && result.Contains("{0}"))
                        {
                            return string.Format(result, args);
                        }
                        return result;
                    }
                }
                else if (moduleData is Dictionary<string, object> stringDict)
                {
                    if (stringDict.ContainsKey(property))
                    {
                        var result = stringDict[property]?.ToString() ?? key;
                        if (args.Length > 0 && result.Contains("{0}"))
                        {
                            return string.Format(result, args);
                        }
                        return result;
                    }
                }
            }

            return key;
        }
        catch
        {
            return key;
        }
    }

    public string CurrentLanguage => _currentLanguageCode;

    public string GetLanguageDisplayName(string languageCode)
    {
        try
        {
            // Try to get display name from CultureInfo
            var culture = new System.Globalization.CultureInfo(languageCode);
            var displayName = culture.NativeName;
            
            // If native name is the same as language code, try English name
            if (displayName == languageCode || string.IsNullOrEmpty(displayName))
            {
                displayName = culture.EnglishName;
            }
            
            return displayName;
        }
        catch
        {
            // Fallback: return language code if culture not found
            return languageCode;
        }
    }
}
