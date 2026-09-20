using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;

namespace Windows11Optimizer.Services;

public sealed class OptimizationService
{
    private readonly WindowsServiceManager _services;
    private readonly ScheduledTaskManager _tasks;
    private readonly BackupService _backup;

    public OptimizationService(WindowsServiceManager services, ScheduledTaskManager tasks, BackupService backup)
    {
        _services = services;
        _tasks = tasks;
        _backup = backup;
    }

    public async Task OptimizeSafeAsync(Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.VmwareServices)
            await ApplyStopDisableAsync(name, log);

        foreach (var name in SafeProfile.AcerOnDemandServices)
            await ApplyStopDisableAsync(name, log);

        foreach (var task in SafeProfile.AcerManagedTasks)
        {
            try
            {
                var enabled = await _tasks.IsEnabledAsync(task);
                if (enabled is not null)
                {
                    await _tasks.DisableAsync(task);
                    log($"Tarea deshabilitada: {task}");
                }
            }
            catch (Exception ex)
            {
                log($"Aviso tarea {task}: {ex.Message}");
            }
        }
    }

    public Task StartVmwareAsync(Action<string> log) =>
        PrepareVmwareAsync(enableUsb: false, enableAutostart: false, log);

    public Task StopVmwareAsync(Action<string> log) =>
        StopGroupAsync(SafeProfile.VmwareServices, log);

    public async Task PrepareVmwareAsync(
        bool enableUsb,
        bool enableAutostart,
        Action<string> log)
    {
        await CreateBackupIfNeededAsync(log);

        foreach (var name in SafeProfile.VmwareCoreServices)
        {
            try
            {
                if (_services.GetInfo(name, "", "") is null)
                {
                    log($"VMware no instalado: {name}");
                    continue;
                }

                await _services.SetManualAndStartAsync(name);
                log($"VMware preparado bajo demanda: {name}");
            }
            catch (Exception ex)
            {
                log($"Error preparando {name}: {ex.Message}");
            }
        }

        if (enableUsb)
        {
            foreach (var name in SafeProfile.VmwareUsbServices)
            {
                try
                {
                    if (_services.GetInfo(name, "", "") is null)
                    {
                        log($"VMware USB no instalado: {name}");
                        continue;
                    }

                    await _services.SetManualAndStartAsync(name);
                    log($"Soporte USB VMware activado bajo demanda: {name}");
                }
                catch (Exception ex)
                {
                    log($"Error preparando USB VMware {name}: {ex.Message}");
                }
            }
        }

        if (enableAutostart)
        {
            foreach (var name in SafeProfile.VmwareAutostartServices)
            {
                try
                {
                    if (_services.GetInfo(name, "", "") is null)
                    {
                        log($"Autoinicio VMware no instalado: {name}");
                        continue;
                    }

                    await _services.SetStartupAutomaticAsync(name);
                    await _services.StartAsync(name);
                    log($"Autoinicio de VMs habilitado: {name}");
                }
                catch (Exception ex)
                {
                    log($"Error preparando autoinicio VMware {name}: {ex.Message}");
                }
            }
        }
    }
    public Task StartAcerAsync(Action<string> log) => StartGroupAsync(SafeProfile.AcerOnDemandServices, log);
    public Task StopAcerAsync(Action<string> log) => StopGroupAsync(SafeProfile.AcerOnDemandServices, log);

    public async Task SetSafeServiceStartupAsync(string name, string mode, Action<string> log)
    {
        ValidateSafeService(name);
        await CreateBackupIfNeededAsync(log);

        switch (mode.ToLowerInvariant())
        {
            case "manual":
                await _services.SetStartupManualAsync(name);
                log($"Inicio cambiado a Manual: {name}");
                break;

            case "automatic":
            case "auto":
                await _services.SetStartupAutomaticAsync(name);
                log($"Inicio cambiado a Automático: {name}");
                break;

            case "disabled":
                if (!SafeProfile.CanDisableService(name))
                    throw new InvalidOperationException($"'{name}' no está aprobado para deshabilitarse.");

                await _services.SetStartupDisabledAsync(name);
                log($"Inicio cambiado a Deshabilitado: {name}");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo de inicio no permitido.");
        }
    }

    public async Task StartSafeServiceAsync(string name, Action<string> log)
    {
        ValidateSafeService(name);
        await CreateBackupIfNeededAsync(log);

        var info = _services.GetInfo(name, "", "");
        if (info is null)
            throw new InvalidOperationException($"El servicio '{name}' no está instalado.");

        if (string.Equals(info.StartMode, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            await _services.SetStartupManualAsync(name);
            log($"'{name}' estaba deshabilitado; se cambió a Manual para poder iniciarlo.");
        }

        await _services.StartAsync(name);
        log($"Servicio iniciado: {name}");
    }

    public async Task StopSafeServiceAsync(string name, Action<string> log)
    {
        ValidateSafeService(name);
        await CreateBackupIfNeededAsync(log);
        await _services.StopAsync(name);
        log($"Servicio detenido: {name}");
    }

    private static void ValidateSafeService(string name)
    {
        if (!SafeProfile.IsUserManageableService(name))
            throw new InvalidOperationException(
                $"El servicio '{name}' está fuera de la lista segura y no puede modificarse desde Windows11Optimizer.");
    }

    public async Task RestoreAsync(Action<string> log)
    {
        var backup = await _backup.LoadAsync();
        if (backup is null)
            throw new InvalidOperationException("No existe una copia de seguridad previa.");

        foreach (var item in backup.Services)
        {
            try
            {
                await _services.RestoreAsync(item.Name, item.StartMode, item.WasRunning);
                log($"Restaurado servicio: {item.Name} -> {item.StartMode}, running={item.WasRunning}");
            }
            catch (Exception ex)
            {
                log($"Error restaurando {item.Name}: {ex.Message}");
            }
        }

        foreach (var task in backup.Tasks)
        {
            try
            {
                if (task.WasEnabled) await _tasks.EnableAsync(task.TaskName);
                else await _tasks.DisableAsync(task.TaskName);
                log($"Restaurada tarea: {task.TaskName}");
            }
            catch (Exception ex)
            {
                log($"Error restaurando tarea {task.TaskName}: {ex.Message}");
            }
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
                    x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
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
            log($"Backup ampliado para servicio: {name}");
        }

        foreach (var task in SafeProfile.AcerManagedTasks)
        {
            if (state.Tasks.Any(
                    x => x.TaskName.Equals(task, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var enabled = await _tasks.IsEnabledAsync(task);
            if (enabled is null)
                continue;

            state.Tasks.Add(new TaskBackup
            {
                TaskName = task,
                WasEnabled = enabled.Value
            });

            changed = true;
            log($"Backup ampliado para tarea: {task}");
        }

        if (changed)
        {
            await _backup.SaveAsync(state);
            log($"Backup actualizado: {_backup.FilePath}");
        }
        else
        {
            log($"Backup existente: {_backup.FilePath}");
        }
    }

    private async Task StartGroupAsync(IEnumerable<string> names, Action<string> log)
    {
        foreach (var name in names)
        {
            try
            {
                if (_services.GetInfo(name, "", "") is null)
                {
                    log($"No instalado: {name}");
                    continue;
                }

                await _services.SetManualAndStartAsync(name);
                log($"Iniciado bajo demanda: {name}");
            }
            catch (Exception ex)
            {
                log($"Error iniciando {name}: {ex.Message}");
            }
        }
    }

    private async Task StopGroupAsync(IEnumerable<string> names, Action<string> log)
    {
        foreach (var name in names)
            await ApplyStopDisableAsync(name, log);
    }

    private async Task ApplyStopDisableAsync(string name, Action<string> log)
    {
        try
        {
            if (_services.GetInfo(name, "", "") is null)
            {
                log($"No instalado: {name}");
                return;
            }

            await _services.StopAndDisableAsync(name);
            log($"Detenido y bloqueado al arranque: {name}");
        }
        catch (Exception ex)
        {
            log($"Error en {name}: {ex.Message}");
        }
    }
}
