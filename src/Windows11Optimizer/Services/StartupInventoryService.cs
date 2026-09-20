using Microsoft.Win32;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class StartupInventoryService
{
    public IReadOnlyList<StartupEntry> GetEntries()
    {
        var entries = new List<StartupEntry>();

        ReadRunKey(entries, Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU Run");
        ReadRunKey(entries, Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKLM Run");
        ReadRunKey(entries, Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM Run (32-bit)");

        ReadStartupFolder(entries, Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Startup (usuario)");
        ReadStartupFolder(entries, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Startup (todos)");

        return entries
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void ReadRunKey(List<StartupEntry> entries, RegistryKey hive, string path, string source)
    {
        try
        {
            using var key = hive.OpenSubKey(path, false);
            if (key is null) return;

            foreach (var name in key.GetValueNames())
            {
                entries.Add(new StartupEntry
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "(Predeterminado)" : name,
                    Command = Convert.ToString(key.GetValue(name)) ?? "",
                    Source = source
                });
            }
        }
        catch { }
    }

    private static void ReadStartupFolder(List<StartupEntry> entries, string folder, string source)
    {
        try
        {
            if (!Directory.Exists(folder)) return;
            foreach (var path in Directory.EnumerateFiles(folder))
            {
                entries.Add(new StartupEntry
                {
                    Name = Path.GetFileName(path),
                    Command = path,
                    Source = source
                });
            }
        }
        catch { }
    }
}
