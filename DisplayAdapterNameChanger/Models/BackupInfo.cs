namespace DisplayAdapterNameChanger.Models;

class BackupInfo
{
    public string BackupId { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
}

