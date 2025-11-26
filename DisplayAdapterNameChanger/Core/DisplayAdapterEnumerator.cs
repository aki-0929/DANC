using DisplayAdapterNameChanger.Models;
using Microsoft.Win32;
using Spectre.Console;

namespace DisplayAdapterNameChanger.Core;

class DisplayAdapterEnumerator
{
    public List<DisplayAdapter> EnumerateDisplayAdapters()
    {
        var adapters = new List<DisplayAdapter>();
        
        try
        {
            // Enumerate PCI devices from registry
            var baseKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Enum\PCI", false);

            if (baseKey == null)
                return adapters;

            // Iterate through all PCI devices
            foreach (var deviceId in baseKey.GetSubKeyNames())
            {
                var deviceKey = baseKey.OpenSubKey(deviceId, false);
                if (deviceKey == null) continue;

                // Check for subkeys (instances)
                foreach (var instanceId in deviceKey.GetSubKeyNames())
                {
                    var instanceKey = deviceKey.OpenSubKey(instanceId, false);
                    if (instanceKey == null) continue;

                    // Read DeviceDesc
                    var deviceDesc = instanceKey.GetValue("DeviceDesc") as string;
                    
                    // Check if it's a display adapter (usually contains keywords like "Display" or "VGA")
                    if (deviceDesc != null && IsDisplayAdapter(deviceDesc))
                    {
                        var adapter = new DisplayAdapter
                        {
                            DeviceId = deviceId,
                            InstanceId = instanceId,
                            CurrentName = deviceDesc,
                            RegistryPath = $@"SYSTEM\CurrentControlSet\Enum\PCI\{deviceId}\{instanceId}"
                        };
                        adapters.Add(adapter);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return adapters;
    }

    private bool IsDisplayAdapter(string deviceDesc)
    {
        // Check if device description contains display adapter related keywords
        var keywords = new[] { "Display", "VGA", "Graphics", "Video", "NVIDIA", "AMD", "Intel", "Radeon", "GeForce" };
        var upperDesc = deviceDesc.ToUpperInvariant();
        return keywords.Any(keyword => upperDesc.Contains(keyword.ToUpperInvariant()));
    }
}

