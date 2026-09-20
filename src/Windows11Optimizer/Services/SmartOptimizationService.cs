using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;

namespace Windows11Optimizer.Services;

public sealed class SmartOptimizationService
{
    private readonly WindowsServiceManager _services;
    private readonly ScheduledTaskManager _tasks;
    private readonly StartupInventoryService _startup;

    public SmartOptimizationService(
        WindowsServiceManager services,
        ScheduledTaskManager tasks,
        StartupInventoryService startup)
    {
        _services = services;
        _tasks = tasks;
        _startup = startup;
    }

    public async Task<SmartOptimizationPlan> AnalyzeAsync()
    {
        var items = new List<OptimizationOpportunity>();

        AddServiceGroup(
            items,
            id: "acer",
            title: "Utilidades Acer",
            category: "Aplicaciones del fabricante",
            names: SafeProfile.AcerOnDemandServices,
            readyText: "Ya están disponibles solo cuando una aplicación las necesita.");

        AddServiceGroup(
            items,
            id: "vmware-background",
            title: "Componentes VMware en segundo plano",
            category: "Virtualización",
            names: SafeProfile.VmwareServices,
            readyText: "Ya están preparados para funcionar solo cuando VMware los necesite.");

        AddServiceGroup(
            items,
            id: "docker-background",
            title: "Docker en segundo plano",
            category: "Virtualización",
            names: SafeProfile.DockerOnDemandServices,
            readyText: "Docker ya está disponible sin tener que iniciarse siempre con Windows.");

        var startup = _startup.GetEntries();
        var orphaned = startup.Count(x => x.IsOrphaned);

        items.Add(new OptimizationOpportunity
        {
            Id = "startup-orphans",
            Title = "Entradas antiguas de inicio",
            Category = "Inicio de Windows",
            Status = orphaned == 0
                ? "No encontramos referencias antiguas."
                : orphaned == 1
                    ? "Encontramos 1 referencia a un programa que ya no existe."
                    : $"Encontramos {orphaned} referencias a programas que ya no existen.",
            Action = orphaned == 0
                ? "No hace falta hacer nada."
                : "Revísalas en Inicio de Windows antes de quitarlas.",
            CanApply = false
        });

        var enabledTaskCount = 0;

        foreach (var task in SafeProfile.AcerManagedTasks)
        {
            var enabled = await _tasks.IsEnabledAsync(task);
            if (enabled == true)
                enabledTaskCount++;
        }

        items.Add(new OptimizationOpportunity
        {
            Id = "scheduled-activities",
            Title = "Actividades programadas",
            Category = "Mantenimiento",
            Status = enabledTaskCount == 0
                ? "No encontramos actividades Acer activas de este grupo."
                : $"{enabledTaskCount} actividades Acer siguen disponibles.",
            Action =
                "Se conservan sin cambios para no quitar funciones. " +
                "La optimización inteligente no las desactiva.",
            CanApply = false
        });

        return new SmartOptimizationPlan
        {
            GeneratedAt = DateTime.Now,
            Opportunities = items
        };
    }

    private void AddServiceGroup(
        List<OptimizationOpportunity> items,
        string id,
        string title,
        string category,
        IEnumerable<string> names,
        string readyText)
    {
        var installed = 0;
        var automatic = 0;

        foreach (var name in names)
        {
            var info = _services.GetInfo(name, "", "");
            if (info is null)
                continue;

            installed++;

            if (string.Equals(
                    info.StartMode,
                    "Auto",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    info.StartMode,
                    "Automatic",
                    StringComparison.OrdinalIgnoreCase))
            {
                automatic++;
            }
        }

        if (installed == 0)
        {
            items.Add(new OptimizationOpportunity
            {
                Id = id,
                Title = title,
                Category = category,
                Status = "No está instalado en este equipo.",
                Action = "No hace falta hacer nada.",
                CanApply = false
            });
            return;
        }

        items.Add(new OptimizationOpportunity
        {
            Id = id,
            Title = title,
            Category = category,
            Status = automatic > 0
                ? automatic == 1
                    ? "1 componente se inicia siempre con Windows."
                    : $"{automatic} componentes se inician siempre con Windows."
                : readyText,
            Action = automatic > 0
                ? "Dejarlo disponible para que se inicie cuando una aplicación lo necesite."
                : "No hace falta cambiarlo.",
            CanApply = automatic > 0
        });
    }
}
