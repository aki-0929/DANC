using DisplayAdapterNameChanger.Models;
using Microsoft.Win32;
using Spectre.Console;

namespace DisplayAdapterNameChanger.Core;

class BackupManager
{
    private readonly ConfigManager _ConfigManager;

    public BackupManager(ConfigManager ConfigManager)
    {
        _ConfigManager = ConfigManager;
    }

    public (string? BackupId, bool IsNew) CreateBackup(DisplayAdapter adapter)
    {
        try
        {
            var backups = _ConfigManager.GetDeviceBackups();
            var deviceKey = $"{adapter.DeviceId}_{adapter.InstanceId}";
            
            // Only create backup if it doesn't exist (preserve original value)
            if (backups.ContainsKey(deviceKey))
            {
                // Backup already exists, return existing backup ID
                return (backups[deviceKey].BackupId, false);
            }

            var backupId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            var backupData = new BackupData
            {
                BackupId = backupId,
                CreatedTime = DateTime.Now,
                DeviceId = adapter.DeviceId,
                InstanceId = adapter.InstanceId,
                RegistryPath = adapter.RegistryPath,
                OriginalName = adapter.CurrentName
            };

            _ConfigManager.UpdateDeviceBackup(deviceKey, backupData);

            return (backupId, true);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return (null, false);
        }
    }

    public List<BackupInfo> GetAllBackups()
    {
        var backups = new List<BackupInfo>();

        try
        {
            var deviceBackups = _ConfigManager.GetDeviceBackups();
            foreach (var backupData in deviceBackups.Values)
            {
                backups.Add(new BackupInfo
                {
                    BackupId = backupData.BackupId,
                    CreatedTime = backupData.CreatedTime,
                    DeviceId = backupData.DeviceId,
                    OriginalName = backupData.OriginalName
                });
            }

            backups = backups.OrderByDescending(b => b.CreatedTime).ToList();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return backups;
    }

    public BackupInfo? GetBackupByDevice(string deviceId, string instanceId)
    {
        try
        {
            var backups = _ConfigManager.GetDeviceBackups();
            var deviceKey = $"{deviceId}_{instanceId}";
            
            if (backups.ContainsKey(deviceKey))
            {
                var backupData = backups[deviceKey];
                return new BackupInfo
                {
                    BackupId = backupData.BackupId,
                    CreatedTime = backupData.CreatedTime,
                    DeviceId = backupData.DeviceId,
                    OriginalName = backupData.OriginalName
                };
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return null;
    }

    public BackupData? GetBackup(string backupId)
    {
        try
        {
            var backups = _ConfigManager.GetDeviceBackups();
            return backups.Values.FirstOrDefault(b => b.BackupId == backupId);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return null;
    }

    public bool RestoreBackup(string backupId)
    {
        try
        {
            var backup = GetBackup(backupId);
            if (backup == null)
            {
                return false;
            }

            var key = Registry.LocalMachine.OpenSubKey(backup.RegistryPath, true);
            if (key == null)
            {
                return false;
            }

            key.SetValue("DeviceDesc", backup.OriginalName, RegistryValueKind.String);
            key.Close();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteBackup(string backupId)
    {
        try
        {
            var backups = _ConfigManager.GetDeviceBackups();
            
            var deviceKeyToRemove = backups
                .FirstOrDefault(kvp => kvp.Value.BackupId == backupId)
                .Key;
            
            if (deviceKeyToRemove != null)
            {
                _ConfigManager.RemoveDeviceBackup(deviceKeyToRemove);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
