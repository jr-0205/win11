using System.Diagnostics;
using System.Text.Json;

namespace Windows11Optimizer.Core;

public sealed class FocusBoostService : IDisposable
{
    private static readonly HashSet<string> CriticalProcesses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Idle", "System", "Registry", "smss", "csrss", "wininit",
            "services", "lsass", "winlogon", "svchost", "dwm", "fontdrvhost",
            "audiodg", "Memory Compression", "Secure System",
            "Windows11Optimizer", "Windows11Optimizer.Agent"
        };

    private static readonly HashSet<string> BackgroundDeprioritizeAllowlist =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "OneDrive", "Dropbox", "GoogleDriveFS"
        };

    private readonly object _gate = new();
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Windows11Optimizer");

    private Process? _watchedTarget;
    private bool _disposed;

    private string SessionFile =>
        Path.Combine(_directory, "focus-boost-session.json");

    public FocusBoostService()
    {
        RecoverOrAttach();
    }

    public IReadOnlyList<FocusProcessInfo> GetCandidateProcesses()
    {
        var currentPid = Environment.ProcessId;
        var rows = new List<FocusProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id == currentPid || process.Id <= 4)
                        continue;

                    var name = process.ProcessName;
                    var allowed = !CriticalProcesses.Contains(name);

                    rows.Add(new FocusProcessInfo
                    {
                        Pid = process.Id,
                        Name = name,
                        WindowTitle = SafeGet(() => process.MainWindowTitle) ?? "",
                        WorkingSetMb = SafeGet(() => process.WorkingSet64) / 1024d / 1024d,
                        Priority = SafeGet(() => process.PriorityClass).ToString(),
                        IsAllowedTarget = allowed
                    });
                }
                catch
                {
                    // Procesos protegidos pueden negar acceso.
                }
            }
        }

        return rows
            .OrderByDescending(x => !string.IsNullOrWhiteSpace(x.WindowTitle))
            .ThenByDescending(x => x.WorkingSetMb)
            .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public FocusBoostStatus GetStatus()
    {
        lock (_gate)
        {
            var session = LoadSession();
            if (session is null)
                return new FocusBoostStatus();

            if (!IsSameProcess(
                    session.TargetPid,
                    session.TargetStartTimeUtc,
                    session.TargetName))
            {
                RestoreInternal(session);
                return new FocusBoostStatus();
            }

            AttachWatcher(session);

            return new FocusBoostStatus
            {
                IsActive = true,
                TargetName = session.TargetName,
                TargetPid = session.TargetPid,
                StartedAt = session.StartedAt
            };
        }
    }

    public FocusBoostStatus Start(int targetPid)
    {
        lock (_gate)
        {
            var existing = LoadSession();

            if (existing is not null &&
                IsSameProcess(
                    existing.TargetPid,
                    existing.TargetStartTimeUtc,
                    existing.TargetName))
            {
                throw new InvalidOperationException(
                    $"Focus Boost ya está activo para {existing.TargetName}.");
            }

            if (existing is not null)
                RestoreInternal(existing);

            using var target = Process.GetProcessById(targetPid);
            target.Refresh();

            if (target.Id <= 4 ||
                CriticalProcesses.Contains(target.ProcessName))
            {
                throw new InvalidOperationException(
                    "Ese proceso está protegido y no puede usarse como objetivo de Focus Boost.");
            }

            var originalPriority = target.PriorityClass;

            var session = new FocusBoostSession
            {
                StartedAt = DateTime.Now,
                TargetPid = target.Id,
                TargetName = target.ProcessName,
                TargetStartTimeUtc = target.StartTime.ToUniversalTime(),
                TargetOriginalPriority = originalPriority.ToString()
            };

            // Persiste la identidad y el estado original antes de tocar nada.
            SaveSession(session);

            try
            {
                if (originalPriority is ProcessPriorityClass.Idle or
                    ProcessPriorityClass.BelowNormal or
                    ProcessPriorityClass.Normal)
                {
                    // Marcar antes del cambio hace que incluso un fallo posterior
                    // pueda restaurar de forma conservadora el valor original.
                    session.TargetPriorityChanged = true;
                    SaveSession(session);
                    target.PriorityClass = ProcessPriorityClass.AboveNormal;
                }

                foreach (var name in BackgroundDeprioritizeAllowlist)
                {
                    foreach (var background in Process.GetProcessesByName(name))
                    {
                        using (background)
                        {
                            try
                            {
                                if (background.Id == target.Id)
                                    continue;

                                var original = background.PriorityClass;

                                if (original is not ProcessPriorityClass.Normal and
                                    not ProcessPriorityClass.AboveNormal)
                                {
                                    continue;
                                }

                                var change = new FocusPriorityChange
                                {
                                    Pid = background.Id,
                                    ProcessName = background.ProcessName,
                                    StartTimeUtc =
                                        background.StartTime.ToUniversalTime(),
                                    OriginalPriority = original.ToString()
                                };

                                session.BackgroundChanges.Add(change);

                                // Guardar antes de aplicar: si la app o el sistema
                                // se interrumpen, el agente sabe qué restaurar.
                                SaveSession(session);

                                background.PriorityClass =
                                    ProcessPriorityClass.BelowNormal;
                            }
                            catch
                            {
                                // No se fuerza acceso a procesos que no lo permiten.
                            }
                        }
                    }
                }

                SaveSession(session);
                AttachWatcher(session);

                return new FocusBoostStatus
                {
                    IsActive = true,
                    TargetName = session.TargetName,
                    TargetPid = session.TargetPid,
                    StartedAt = session.StartedAt
                };
            }
            catch
            {
                RestoreInternal(session);
                throw;
            }
        }
    }

    public void Restore()
    {
        lock (_gate)
        {
            var session = LoadSession();
            if (session is null)
                return;

            RestoreInternal(session);
        }
    }

    private void RecoverOrAttach()
    {
        lock (_gate)
        {
            var session = LoadSession();
            if (session is null)
                return;

            if (IsSameProcess(
                    session.TargetPid,
                    session.TargetStartTimeUtc,
                    session.TargetName))
            {
                AttachWatcher(session);
            }
            else
            {
                RestoreInternal(session);
            }
        }
    }

    private void AttachWatcher(FocusBoostSession session)
    {
        try
        {
            if (_watchedTarget is not null &&
                !_watchedTarget.HasExited &&
                _watchedTarget.Id == session.TargetPid)
            {
                return;
            }

            _watchedTarget?.Dispose();
            _watchedTarget = Process.GetProcessById(session.TargetPid);

            if (!IsSameProcess(
                    _watchedTarget,
                    session.TargetStartTimeUtc,
                    session.TargetName))
            {
                _watchedTarget.Dispose();
                _watchedTarget = null;
                RestoreInternal(session);
                return;
            }

            _watchedTarget.EnableRaisingEvents = true;
            _watchedTarget.Exited += (_, _) =>
            {
                try
                {
                    Restore();
                }
                catch
                {
                    // El siguiente inicio recuperará el estado persistido.
                }
            };
        }
        catch
        {
            RestoreInternal(session);
        }
    }

    private void RestoreInternal(FocusBoostSession session)
    {
        if (session.TargetPriorityChanged &&
            TryOpenSameProcess(
                session.TargetPid,
                session.TargetStartTimeUtc,
                session.TargetName,
                out var target))
        {
            using (target)
            {
                TryRestorePriority(target, session.TargetOriginalPriority);
            }
        }

        foreach (var change in session.BackgroundChanges)
        {
            if (!TryOpenSameProcess(
                    change.Pid,
                    change.StartTimeUtc,
                    change.ProcessName,
                    out var process))
            {
                continue;
            }

            using (process)
            {
                TryRestorePriority(process, change.OriginalPriority);
            }
        }

        _watchedTarget?.Dispose();
        _watchedTarget = null;

        try
        {
            if (File.Exists(SessionFile))
                File.Delete(SessionFile);
        }
        catch
        {
            // El estado ya se intentó restaurar; el archivo puede reintentarse luego.
        }
    }

    private static void TryRestorePriority(Process process, string priority)
    {
        try
        {
            if (Enum.TryParse<ProcessPriorityClass>(
                    priority,
                    ignoreCase: true,
                    out var parsed))
            {
                process.PriorityClass = parsed;
            }
        }
        catch
        {
            // Si el proceso ya está terminando, no hay nada más que restaurar.
        }
    }

    private bool IsSameProcess(int pid, DateTime startTimeUtc, string name) =>
        TryOpenSameProcess(pid, startTimeUtc, name, out var process)
            ? DisposeTrue(process)
            : false;

    private static bool IsSameProcess(
        Process process,
        DateTime startTimeUtc,
        string name)
    {
        try
        {
            return process.ProcessName.Equals(
                       name,
                       StringComparison.OrdinalIgnoreCase) &&
                   Math.Abs(
                       (process.StartTime.ToUniversalTime() - startTimeUtc)
                       .TotalSeconds) < 2;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryOpenSameProcess(
        int pid,
        DateTime startTimeUtc,
        string name,
        out Process process)
    {
        process = null!;

        try
        {
            process = Process.GetProcessById(pid);

            if (!IsSameProcess(process, startTimeUtc, name))
            {
                process.Dispose();
                process = null!;
                return false;
            }

            return true;
        }
        catch
        {
            process = null!;
            return false;
        }
    }

    private static bool DisposeTrue(Process process)
    {
        process.Dispose();
        return true;
    }

    private FocusBoostSession? LoadSession()
    {
        try
        {
            if (!File.Exists(SessionFile))
                return null;

            return JsonSerializer.Deserialize<FocusBoostSession>(
                File.ReadAllText(SessionFile));
        }
        catch
        {
            return null;
        }
    }

    private void SaveSession(FocusBoostSession session)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            SessionFile,
            JsonSerializer.Serialize(
                session,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static T SafeGet<T>(Func<T> getter)
    {
        try
        {
            return getter();
        }
        catch
        {
            return default!;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _watchedTarget?.Dispose();
        _watchedTarget = null;
    }
}
