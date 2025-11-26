namespace DisplayAdapterNameChanger.Models;

class BackupData
{
    public string BackupId { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string RegistryPath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
}

