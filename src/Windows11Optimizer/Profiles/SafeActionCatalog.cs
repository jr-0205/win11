using Windows11Optimizer.Models;

namespace Windows11Optimizer.Profiles;

public static class SafeActionCatalog
{
    private static readonly SafeActionDefinition[] Actions =
    [
        new()
        {
            Id = "acer.on_demand",
            Title = "Utilidades Acer bajo demanda",
            Category = "Segundo plano",
            Description =
                "Evita que utilidades Acer no esenciales tengan que iniciar siempre con Windows. " +
                "No toca los servicios Acer protegidos por la aplicación.",
            Impact =
                "Puede reducir actividad al iniciar Windows. Las utilidades siguen disponibles cuando se abren.",
            Risk = "Bajo",
            Revert = "Restaura el modo de inicio que tenían antes de aplicar este ajuste.",
            CanApply = true,
            AiSelectable = true
        },
        new()
        {
            Id = "windows.transparency_off",
            Title = "Reducir transparencias",
            Category = "Interfaz",
            Description =
                "Desactiva el efecto de transparencia del usuario actual.",
            Impact =
                "Reduce ligeramente trabajo gráfico y hace la interfaz más simple. El ahorro suele ser pequeño.",
            Risk = "Muy bajo",
            Revert = "Restaura exactamente el valor anterior de transparencias.",
            CanApply = true,
            AiSelectable = true
        },
        new()
        {
            Id = "startup.review_orphans",
            Title = "Revisar entradas antiguas de inicio",
            Category = "Inicio de Windows",
            Description =
                "Señala referencias de inicio cuyo programa ya no existe.",
            Impact =
                "Limpia ruido del arranque sin eliminar programas instalados.",
            Risk = "Muy bajo",
            Revert =
                "Cada referencia eliminada por Windows11Optimizer conserva una copia para restaurarla.",
            CanApply = false,
            AiSelectable = true
        },
        new()
        {
            Id = "explorer.show_extensions",
            Title = "Mostrar extensiones de archivo",
            Category = "Explorador",
            Description =
                "Muestra .exe, .txt, .jpg y otras extensiones en el Explorador.",
            Impact =
                "No mejora rendimiento; facilita identificar archivos con claridad.",
            Risk = "Muy bajo",
            Revert = "Recupera la preferencia anterior.",
            CanApply = true,
            AiSelectable = false
        },
        new()
        {
            Id = "explorer.show_hidden",
            Title = "Mostrar archivos ocultos",
            Category = "Explorador",
            Description =
                "Permite ver archivos y carpetas marcados como ocultos.",
            Impact = "No modifica los archivos; solo cambia su visibilidad.",
            Risk = "Muy bajo",
            Revert = "Recupera la preferencia anterior.",
            CanApply = true,
            AiSelectable = false
        },
        new()
        {
            Id = "taskbar.end_task",
            Title = "Finalizar tarea desde la barra",
            Category = "Productividad",
            Description =
                "Añade una forma directa de finalizar una aplicación desde la barra de tareas.",
            Impact = "Facilita cerrar aplicaciones que dejan de responder.",
            Risk = "Muy bajo",
            Revert = "Recupera la preferencia anterior.",
            CanApply = true,
            AiSelectable = false
        }
    ];

    public static IReadOnlyList<SafeActionDefinition> All => Actions;

    public static IReadOnlyList<SafeActionDefinition> AiSelectable =>
        Actions.Where(x => x.AiSelectable).ToList();

    public static SafeActionDefinition? Find(string id) =>
        Actions.FirstOrDefault(
            x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
