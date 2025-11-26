using System.Diagnostics;
using System.Security.Principal;

namespace DisplayAdapterNameChanger.Core;

class PrivilegeManager
{
    public static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void RestartAsAdministrator()
    {
        try
        {
            string? exePath = null;

            // Method 1: Use Environment.ProcessPath (most reliable in .NET 6+)
            exePath = Environment.ProcessPath;

            // Method 2: Get from command line arguments (safe fallback)
            if (string.IsNullOrEmpty(exePath))
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    exePath = args[0];
                }
            }

            // Method 3: Use AppContext.BaseDirectory as last resort
            if (string.IsNullOrEmpty(exePath))
            {
                var baseDir = AppContext.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDir))
                {
                    // Try to find the executable in the base directory
                    var exeName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
                    var possiblePaths = new[]
                    {
                        Path.Combine(baseDir, $"{exeName}.exe"),
                        Path.Combine(baseDir, $"{exeName}.dll")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            exePath = path;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(exePath))
            {
                throw new InvalidOperationException("Unable to determine executable path");
            }

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = exePath,
                Verb = "runas" // This will request administrator privileges
            };

            Process.Start(startInfo);
        }
        catch
        {
            // Ignore errors
        }
    }
}

