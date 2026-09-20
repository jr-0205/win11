using System.Text.Json;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class BackupService
{
    public string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Windows11Optimizer");

    public string FilePath => Path.Combine(DirectoryPath, "backup.json");
    public bool Exists => File.Exists(FilePath);

    public async Task SaveIfMissingAsync(BackupState backup)
    {
        if (Exists) return;
        await SaveAsync(backup);
    }

    public async Task SaveAsync(BackupState backup)
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(
            backup,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(FilePath, json);
    }

    public async Task<BackupState?> LoadAsync()
    {
        if (!Exists) return null;
        var json = await File.ReadAllTextAsync(FilePath);
        return JsonSerializer.Deserialize<BackupState>(json);
    }
}
