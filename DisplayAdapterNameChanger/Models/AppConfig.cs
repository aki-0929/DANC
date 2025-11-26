namespace DisplayAdapterNameChanger.Models;

class AppConfig
{
    public string Language { get; set; } = "en";
    public Dictionary<string, BackupData> DeviceBackups { get; set; } = new();
}

