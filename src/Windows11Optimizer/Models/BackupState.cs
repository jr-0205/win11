namespace Windows11Optimizer.Models;

public sealed class BackupState
{
    public DateTime CreatedAt { get; set; }
    public List<ServiceBackup> Services { get; set; } = [];
    public List<TaskBackup> Tasks { get; set; } = [];
}

public sealed class ServiceBackup
{
    public string Name { get; set; } = "";
    public string StartMode { get; set; } = "Manual";
    public bool WasRunning { get; set; }
}

public sealed class TaskBackup
{
    public string TaskName { get; set; } = "";
    public bool WasEnabled { get; set; }
}
