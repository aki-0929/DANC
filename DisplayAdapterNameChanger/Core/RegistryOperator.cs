using DisplayAdapterNameChanger.Models;
using Microsoft.Win32;

namespace DisplayAdapterNameChanger.Core;

class RegistryOperator
{
    public bool ModifyDeviceName(DisplayAdapter adapter, string newName)
    {
        try
        {
            var key = Registry.LocalMachine.OpenSubKey(adapter.RegistryPath, true);
            if (key == null)
            {
                return false;
            }

            key.SetValue("DeviceDesc", newName, RegistryValueKind.String);
            key.Close();

            return true;
        }
        catch
        {
            return false;
        }
    }
}

