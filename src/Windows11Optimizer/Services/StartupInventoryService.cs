using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class StartupInventoryService
{
    private readonly string _backupDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Windows11Optimizer");

    private string BackupFile =>
        Path.Combine(_backupDirectory, "startup-entry-backup.json");

    public bool HasRestorableEntry
    {
        get
        {
            try
            {
                return LoadBackups().Any(x => !x.Restored);
            }
            catch
            {
                return false;
            }
        }
    }

    public IReadOnlyList<StartupEntry> GetEntries()
    {
        var entries = new List<StartupEntry>();

        ReadRunKey(
            entries,
            Registry.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Registro del usuario",
            "HKCU");

        ReadRunKey(
            entries,
            Registry.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Registro del equipo",
            "HKLM");

        ReadRunKey(
            entries,
            Registry.LocalMachine,
            @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
            "Registro del equipo (32 bits)",
            "HKLM");

        ReadStartupFolder(
            entries,
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "Carpeta Inicio del usuario");

        ReadStartupFolder(
            entries,
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            "Carpeta Inicio de todos los usuarios");

        return entries
            .OrderByDescending(x => x.IsOrphaned)
            .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public void RemoveOrphanedEntry(StartupEntry entry)
    {
        if (!entry.CanRemoveSafely)
            throw new InvalidOperationException(
                "Esta entrada no está confirmada como huérfana y no se eliminará automáticamente.");

        var hive = ResolveHive(entry.RegistryHive);

        using var key = hive.OpenSubKey(entry.RegistryPath, writable: true);
        if (key is null)
            throw new InvalidOperationException(
                "La ubicación de inicio ya no existe.");

        var currentValue = Convert.ToString(
            key.GetValue(entry.RegistryValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames));

        if (currentValue is null)
            throw new InvalidOperationException(
                "La entrada seleccionada ya no existe.");

        var backup = new StartupEntryBackup
        {
            RemovedAt = DateTime.Now,
            Name = entry.Name,
            Command = currentValue,
            RegistryHive = entry.RegistryHive,
            RegistryPath = entry.RegistryPath,
            RegistryValueName = entry.RegistryValueName,
            Restored = false
        };

        SaveBackup(backup);

        key.DeleteValue(entry.RegistryValueName, throwOnMissingValue: true);
    }

    public StartupEntryBackup RestoreLastRemoved()
    {
        var backups = LoadBackups();
        var backup = backups
            .Where(x => !x.Restored)
            .OrderByDescending(x => x.RemovedAt)
            .FirstOrDefault();

        if (backup is null)
            throw new InvalidOperationException(
                "No hay una entrada eliminada por Windows11Optimizer pendiente de restaurar.");

        var hive = ResolveHive(backup.RegistryHive);

        using var key = hive.CreateSubKey(backup.RegistryPath, writable: true);
        if (key is null)
            throw new InvalidOperationException(
                "No se pudo abrir la ubicación de inicio para restaurarla.");

        key.SetValue(
            backup.RegistryValueName,
            backup.Command,
            RegistryValueKind.String);

        backup.Restored = true;
        WriteBackups(backups);

        return backup;
    }

    private static void ReadRunKey(
        List<StartupEntry> entries,
        RegistryKey hive,
        string path,
        string source,
        string hiveName)
    {
        try
        {
            using var key = hive.OpenSubKey(path, false);
            if (key is null) return;

            foreach (var valueName in key.GetValueNames())
            {
                var command = Convert.ToString(
                    key.GetValue(
                        valueName,
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames)) ?? "";

                var target = TryResolveTargetPath(command);

                entries.Add(new StartupEntry
                {
                    Name = string.IsNullOrWhiteSpace(valueName)
                        ? "(Sin nombre)"
                        : valueName,
                    Command = command,
                    Source = source,
                    Kind = "Registry",
                    RegistryHive = hiveName,
                    RegistryPath = path,
                    RegistryValueName = valueName,
                    TargetPath = target ?? "",
                    TargetCanBeVerified = target is not null,
                    TargetExists = target is not null && File.Exists(target)
                });
            }
        }
        catch
        {
            // Una clave inaccesible no debe impedir mostrar el resto.
        }
    }

    private static void ReadStartupFolder(
        List<StartupEntry> entries,
        string folder,
        string source)
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
                    Source = source,
                    Kind = "StartupFolder",
                    StartupFilePath = path,
                    TargetPath = path,
                    TargetCanBeVerified = false,
                    TargetExists = true
                });
            }
        }
        catch
        {
            // Una carpeta inaccesible no debe impedir mostrar el resto.
        }
    }

    private static string? TryResolveTargetPath(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        var expanded = Environment.ExpandEnvironmentVariables(command.Trim());

        if (expanded.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
            expanded.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            expanded.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? candidate = null;

        if (expanded.StartsWith('"'))
        {
            var closingQuote = expanded.IndexOf('"', 1);
            if (closingQuote > 1)
                candidate = expanded[1..closingQuote];
        }
        else
        {
            var match = Regex.Match(
                expanded,
                @"^(?<path>.+?\.(?:exe|com|bat|cmd))(?=\s|$)",
                RegexOptions.IgnoreCase);

            if (match.Success)
                candidate = match.Groups["path"].Value.Trim();
        }

        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        candidate = candidate.Trim('"');

        try
        {
            return Path.GetFullPath(candidate);
        }
        catch
        {
            return candidate;
        }
    }

    private void SaveBackup(StartupEntryBackup backup)
    {
        Directory.CreateDirectory(_backupDirectory);

        var backups = LoadBackups();
        backups.Add(backup);
        WriteBackups(backups);
    }

    private List<StartupEntryBackup> LoadBackups()
    {
        try
        {
            if (!File.Exists(BackupFile))
                return [];

            return JsonSerializer.Deserialize<List<StartupEntryBackup>>(
                       File.ReadAllText(BackupFile))
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void WriteBackups(List<StartupEntryBackup> backups)
    {
        Directory.CreateDirectory(_backupDirectory);

        File.WriteAllText(
            BackupFile,
            JsonSerializer.Serialize(
                backups,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static RegistryKey ResolveHive(string hive) => hive switch
    {
        "HKCU" => Registry.CurrentUser,
        "HKLM" => Registry.LocalMachine,
        _ => throw new InvalidOperationException(
            $"Origen de Registro no permitido: {hive}")
    };
}
