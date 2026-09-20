using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;

namespace Windows11Optimizer.Services;

public sealed class OptimizationService
{
    private readonly WindowsServiceManager _services;
    private readonly ScheduledTaskManager _tasks;
    private readonly BackupService _backup;

    public OptimizationService(
        WindowsServiceManager services,
        ScheduledTaskManager tasks,
        BackupService backup)
    {
        _services = services;
        _tasks = tasks;
        _backup = backup;
    }

    /// <summary>
    /// Optimización conservadora: no deshabilita servicios ni tareas.
    /// Los componentes aprobados pasan a inicio bajo demanda (Manual).
    /// </summary>
    public async Task OptimizeSafeAsync(Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.IntelligentOnDemandServices)
            await SetOnDemandIfInstalledAsync(name, log);

        foreach (var task in SafeProfile.AcerManagedTasks)
        {
            var enabled = await _tasks.IsEnabledAsync(task);

            if (enabled is not null)
            {
                log(
                    $"Actividad programada conservada sin cambios: {task} " +
                    $"(habilitada={enabled.Value}).");
            }
        }
    }

    /// <summary>
    /// Prepara el entorno para Docker/WSL2/Sandbox sin iniciar VMware.
    /// Los componentes de terceros quedan disponibles cuando una app los solicite.
    /// </summary>
    public async Task PrepareWindowsVirtualizationAsync(Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.DockerOnDemandServices)
            await SetOnDemandIfInstalledAsync(name, log);

        foreach (var name in SafeProfile.VmwareServices)
            await SetOnDemandIfInstalledAsync(name, log);
    }

    /// <summary>
    /// Prepara VMware sin dejar servicios permanentemente en Automático.
    /// Los componentes principales se inician ahora; los opcionales quedan en Manual.
    /// </summary>
    public async Task PrepareVmwareAsync(Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.VmwareServices)
            await SetOnDemandIfInstalledAsync(name, log);

        foreach (var name in SafeProfile.VmwareCoreServices)
        {
            try
            {
                if (_services.GetInfo(name, "", "") is null)
                    continue;

                await _services.StartAsync(name);
                log($"Componente VMware disponible ahora: {name}");
            }
            catch (Exception ex)
            {
                log($"No se pudo iniciar {name}: {ex.Message}");
            }
        }
    }

    public Task StartVmwareAsync(Action<string> log) =>
        PrepareVmwareAsync(log);

    public async Task StopVmwareAsync(Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.VmwareServices)
            await SetOnDemandIfInstalledAsync(name, log);
    }

    public async Task StartAcerAsync(Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.AcerOnDemandServices)
        {
            await SetOnDemandIfInstalledAsync(name, log);

            try
            {
                if (_services.GetInfo(name, "", "") is not null)
                    await _services.StartAsync(name);
            }
            catch (Exception ex)
            {
                log($"No se pudo iniciar {name}: {ex.Message}");
            }
        }
    }

    public async Task StopAcerAsync(Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.AcerOnDemandServices)
            await SetOnDemandIfInstalledAsync(name, log);
    }

    public async Task SetSafeServiceStartupAsync(
        string name,
        string mode,
        Action<string> log)
    {
        ValidateSafeService(name);
        await CreateBackupIfNeededAsync(log);

        switch (mode.ToLowerInvariant())
        {
            case "manual":
                await _services.SetStartupManualAsync(name);
                log($"Disponible cuando se necesite: {name}");
                break;

            case "automatic":
            case "auto":
                await _services.SetStartupAutomaticAsync(name);
                log($"Disponible desde el inicio de Windows: {name}");
                break;

            case "disabled":
                throw new InvalidOperationException(
                    "Windows11Optimizer ya no deshabilita estos componentes. " +
                    "Puede dejarlos disponibles cuando una aplicación los necesite.");

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "Modo de inicio no permitido.");
        }
    }

    public async Task StartSafeServiceAsync(string name, Action<string> log)
    {
        ValidateSafeService(name);
        await CreateBackupIfNeededAsync(log);

        var info = _services.GetInfo(name, "", "");
        if (info is null)
            throw new InvalidOperationException(
                $"El componente '{name}' no está instalado.");

        if (string.Equals(
                info.StartMode,
                "Disabled",
                StringComparison.OrdinalIgnoreCase))
        {
            await _services.SetStartupManualAsync(name);
            log($"Se volvió a dejar disponible: {name}");
        }

        await _services.StartAsync(name);
        log($"Componente iniciado: {name}");
    }

    public async Task StopSafeServiceAsync(string name, Action<string> log)
    {
        ValidateSafeService(name);
        await CreateBackupIfNeededAsync(log);
        await _services.SetStartupManualAsync(name);
        log($"Disponible cuando se necesite: {name}");
    }

    public async Task RestoreAsync(Action<string> log)
    {
        var backup = await _backup.LoadAsync();
        if (backup is null)
            throw new InvalidOperationException(
                "No existe una copia de seguridad previa.");

        foreach (var item in backup.Services)
        {
            try
            {
                await _services.RestoreAsync(
                    item.Name,
                    item.StartMode,
                    item.WasRunning);

                log(
                    $"Restaurado componente: {item.Name} -> " +
                    $"{item.StartMode}, activo={item.WasRunning}");
            }
            catch (Exception ex)
            {
                log($"Error restaurando {item.Name}: {ex.Message}");
            }
        }

        // Compatibilidad con backups de versiones anteriores que sí
        // administraban estas tareas.
        foreach (var task in backup.Tasks)
        {
            try
            {
                if (task.WasEnabled)
                    await _tasks.EnableAsync(task.TaskName);
                else
                    await _tasks.DisableAsync(task.TaskName);

                log($"Restaurada actividad programada: {task.TaskName}");
            }
            catch (Exception ex)
            {
                log(
                    $"Error restaurando actividad programada " +
                    $"{task.TaskName}: {ex.Message}");
            }
        }
    }

    private static void ValidateSafeService(string name)
    {
        if (!SafeProfile.IsUserManageableService(name))
        {
            throw new InvalidOperationException(
                $"El componente '{name}' está fuera de la lista segura.");
        }
    }

    private async Task SetOnDemandIfInstalledAsync(
        string name,
        Action<string> log)
    {
        try
        {
            var info = _services.GetInfo(name, "", "");

            if (info is null)
            {
                log($"No instalado: {name}");
                return;
            }

            if (string.Equals(
                    info.StartMode,
                    "Manual",
                    StringComparison.OrdinalIgnoreCase))
            {
                log($"Ya está disponible cuando se necesite: {name}");
                return;
            }

            await _services.SetStartupManualAsync(name);
            log($"Disponible cuando se necesite: {name}");
        }
        catch (Exception ex)
        {
            log($"No se pudo ajustar {name}: {ex.Message}");
        }
    }

    private async Task CreateBackupIfNeededAsync(Action<string> log)
    {
        var state = await _backup.LoadAsync()
            ?? new BackupState { CreatedAt = DateTime.Now };

        var changed = !_backup.Exists;

        foreach (var name in SafeProfile.AllModifiedServices)
        {
            if (state.Services.Any(
                    x => x.Name.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var info = _services.GetInfo(name, "Backup", "");
            if (info is null)
                continue;

            state.Services.Add(new ServiceBackup
            {
                Name = name,
                StartMode = info.StartMode,
                WasRunning = string.Equals(
                    info.State,
                    "Running",
                    StringComparison.OrdinalIgnoreCase)
            });

            changed = true;
        }

        if (changed)
        {
            await _backup.SaveAsync(state);
            log($"Copia de seguridad actualizada: {_backup.FilePath}");
        }
    }
}
