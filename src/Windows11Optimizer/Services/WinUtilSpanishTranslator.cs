namespace Windows11Optimizer.Services;

/// <summary>
/// Capa de presentación en español para el catálogo de WinUtil.
/// Conserva los identificadores y textos originales fuera de la interfaz.
/// </summary>
public static class WinUtilSpanishTranslator
{
    private static readonly Dictionary<string, string> Titles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["WPFTweaksRestorePoint"] = "Crear un punto de restauración",
            ["WPFTweaksActivity"] = "Desactivar el historial de actividad",
            ["WPFTweaksConsumerFeatures"] = "Reducir contenido promocional de Windows",
            ["WPFTweaksDisableExplorerAutoDiscovery"] = "Evitar búsquedas automáticas de red en el Explorador",
            ["WPFTweaksWPBT"] = "Bloquear software añadido por el fabricante al arrancar",
            ["WPFTweaksLocation"] = "Reducir el uso de ubicación",
            ["WPFTweaksServices"] = "Optimizar servicios de Windows",
            ["WPFTweaksTelemetry"] = "Reducir telemetría de Windows",
            ["WPFTweaksDeliveryOptimization"] = "Limitar la optimización de entrega de actualizaciones",
            ["WPFTweaksDiskCleanup"] = "Limpiar archivos innecesarios del disco",
            ["WPFTweaksDeleteTempFiles"] = "Eliminar archivos temporales",
            ["WPFTweaksEndTaskOnTaskbar"] = "Añadir “Finalizar tarea” a la barra de tareas",
            ["WPFTweaksDisableStoreSearch"] = "Evitar resultados de Microsoft Store en la búsqueda",
            ["WPFTweaksRevertStartMenu"] = "Cambiar el comportamiento del menú Inicio",
            ["WPFTweaksWidget"] = "Desactivar o quitar Widgets",
            ["WPFTweaksRemoveOneDrive"] = "Quitar OneDrive",
            ["WPFTweaksWindowsAI"] = "Reducir funciones de IA integradas en Windows",
            ["WPFTweaksRightClickMenu"] = "Usar el menú contextual clásico",
            ["WPFTweaksHiber"] = "Desactivar hibernación",
            ["WPFTweaksDisableSearchHistory"] = "Desactivar historial de búsqueda",
            ["WPFTweaksDisableGameDVR"] = "Desactivar grabación de juegos en segundo plano",
            ["WPFTweaksDisableBackgroundApps"] = "Reducir aplicaciones en segundo plano",
            ["WPFTweaksDisableNotifications"] = "Reducir notificaciones de Windows",
            ["WPFTweaksDisableEdgeTelemetry"] = "Reducir telemetría de Microsoft Edge",
            ["WPFTweaksDisableCopilot"] = "Desactivar Copilot",
            ["WPFTweaksRemoveEdge"] = "Quitar Microsoft Edge",
            ["WPFTweaksDisableBitlocker"] = "Desactivar BitLocker",
            ["WPFTweaksSetClassicContextMenu"] = "Usar menú contextual clásico",
            ["WPFTweaksDisableMouseAcceleration"] = "Desactivar aceleración del mouse",
            ["WPFTweaksDarkMode"] = "Usar tema oscuro",
            ["WPFTweaksShowKnownFileExt"] = "Mostrar extensiones de archivo",
            ["WPFTweaksShowHiddenFiles"] = "Mostrar archivos ocultos",
            ["WPFTweaksTaskbarAlignLeft"] = "Alinear la barra de tareas a la izquierda"
        };

    private static readonly Dictionary<string, string> Descriptions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["WPFTweaksRestorePoint"] = "Crea una copia de seguridad de la configuración del sistema antes de realizar cambios.",
            ["WPFTweaksActivity"] = "Reduce el registro de actividad que Windows guarda para funciones de historial y sincronización.",
            ["WPFTweaksConsumerFeatures"] = "Reduce recomendaciones, sugerencias y contenido promocional que Windows puede mostrar o instalar.",
            ["WPFTweaksDisableExplorerAutoDiscovery"] = "Evita que el Explorador busque automáticamente dispositivos y recursos de red cuando no se necesitan.",
            ["WPFTweaksWPBT"] = "Impide que ciertos fabricantes carguen software adicional desde el firmware durante el arranque de Windows.",
            ["WPFTweaksLocation"] = "Reduce servicios y permisos relacionados con la ubicación. Algunas apps pueden perder funciones basadas en tu ubicación.",
            ["WPFTweaksServices"] = "Cambia el modo de inicio de varios servicios para reducir actividad en segundo plano. Requiere revisión antes de aplicarse.",
            ["WPFTweaksTelemetry"] = "Reduce el envío de datos de diagnóstico y telemetría de Windows.",
            ["WPFTweaksDeliveryOptimization"] = "Reduce el uso de red de Windows Update para compartir o descargar actualizaciones desde otros equipos.",
            ["WPFTweaksDiskCleanup"] = "Ejecuta tareas de limpieza para recuperar espacio ocupado por archivos que Windows ya no necesita.",
            ["WPFTweaksDeleteTempFiles"] = "Elimina archivos temporales que normalmente pueden volver a generarse cuando sean necesarios.",
            ["WPFTweaksEndTaskOnTaskbar"] = "Añade una opción rápida para cerrar una aplicación desde su icono en la barra de tareas.",
            ["WPFTweaksDisableStoreSearch"] = "Evita que la búsqueda de Windows mezcle resultados de Microsoft Store con tus resultados locales.",
            ["WPFTweaksRevertStartMenu"] = "Modifica opciones del menú Inicio. Puede cambiar cómo se muestran recomendaciones y accesos.",
            ["WPFTweaksWidget"] = "Reduce o elimina los Widgets y sus procesos en segundo plano.",
            ["WPFTweaksRemoveOneDrive"] = "Elimina la integración de OneDrive. No conviene si sincronizas archivos con OneDrive.",
            ["WPFTweaksWindowsAI"] = "Reduce algunas funciones de inteligencia artificial integradas en Windows. Puede afectar características nuevas del sistema.",
            ["WPFTweaksRightClickMenu"] = "Cambia el menú del clic derecho para mostrar más opciones directamente.",
            ["WPFTweaksHiber"] = "Desactiva la hibernación para ahorrar espacio en disco. También puede afectar Inicio rápido.",
            ["WPFTweaksDisableGameDVR"] = "Reduce la grabación y captura automática de juegos en segundo plano.",
            ["WPFTweaksDisableBackgroundApps"] = "Limita aplicaciones que continúan trabajando cuando no están abiertas.",
            ["WPFTweaksDisableCopilot"] = "Oculta o desactiva la integración de Copilot en Windows.",
            ["WPFTweaksRemoveEdge"] = "Intenta eliminar Microsoft Edge. Es un cambio avanzado y puede afectar componentes que dependen de él.",
            ["WPFTweaksDisableBitlocker"] = "Desactiva el cifrado BitLocker. Puede reducir la protección de tus datos y requiere especial cuidado."
        };

    private static readonly Dictionary<string, string> Categories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Essential Tweaks"] = "Ajustes esenciales",
            ["Customize Preferences"] = "Personalización",
            ["Advanced Tweaks - CAUTION"] = "Ajustes avanzados",
            ["Advanced Tweaks"] = "Ajustes avanzados",
            ["Privacy"] = "Privacidad",
            ["Performance"] = "Rendimiento",
            ["Security"] = "Seguridad",
            ["Updates"] = "Actualizaciones",
            ["Explorer"] = "Explorador de archivos",
            ["Taskbar"] = "Barra de tareas",
            ["Start Menu"] = "Menú Inicio",
            ["Network"] = "Red",
            ["Gaming"] = "Juegos",
            ["Applications"] = "Aplicaciones"
        };

    private static readonly Dictionary<string, string> Presets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Standard"] = "Estándar",
            ["Minimal"] = "Mínimo",
            ["Advanced"] = "Avanzado",
            ["AppxDefault"] = "Apps predeterminadas"
        };

    public static string Title(string id, string original)
    {
        if (Titles.TryGetValue(id, out var value))
            return value;

        if (!string.IsNullOrWhiteSpace(original))
            return TranslateCommonTitle(original);

        return HumanizeId(id);
    }

    public static string Description(string id, string original, string translatedTitle)
    {
        if (Descriptions.TryGetValue(id, out var value))
            return value;

        // Evitamos mostrar párrafos en inglés al usuario general.
        return $"Opción de WinUtil relacionada con “{translatedTitle}”. " +
               "Windows11Optimizer la muestra para consulta, pero no la aplica automáticamente.";
    }

    public static string Category(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return "General";

        if (Categories.TryGetValue(original, out var value))
            return value;

        return TranslateCommonCategory(original);
    }

    public static string PresetList(IEnumerable<string> originals)
    {
        return string.Join(", ",
            originals
                .Select(x => Presets.TryGetValue(x, out var value) ? value : x)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase));
    }

    private static string TranslateCommonTitle(string input)
    {
        var replacements = new (string English, string Spanish)[]
        {
            ("Disable", "Desactivar"),
            ("Enable", "Activar"),
            ("Remove", "Quitar"),
            ("Restore", "Restaurar"),
            ("Delete", "Eliminar"),
            ("Telemetry", "telemetría"),
            ("Widgets", "Widgets"),
            ("Widget", "Widgets"),
            ("Activity History", "historial de actividad"),
            ("Location", "ubicación"),
            ("Services", "servicios"),
            ("Search", "búsqueda"),
            ("Background Apps", "aplicaciones en segundo plano"),
            ("Notifications", "notificaciones"),
            ("Taskbar", "barra de tareas"),
            ("Start Menu", "menú Inicio"),
            ("Right Click Menu", "menú del clic derecho"),
            ("Delivery Optimization", "optimización de entrega"),
            ("Disk Cleanup", "limpieza de disco"),
            ("Temporary Files", "archivos temporales"),
            ("Temp Files", "archivos temporales")
        };

        var result = input;
        foreach (var pair in replacements)
            result = result.Replace(pair.English, pair.Spanish, StringComparison.OrdinalIgnoreCase);

        return result;
    }

    private static string TranslateCommonCategory(string input)
    {
        var result = input
            .Replace("Tweaks", "Ajustes", StringComparison.OrdinalIgnoreCase)
            .Replace("Essential", "esenciales", StringComparison.OrdinalIgnoreCase)
            .Replace("Advanced", "avanzados", StringComparison.OrdinalIgnoreCase)
            .Replace("Customize", "Personalización", StringComparison.OrdinalIgnoreCase)
            .Replace("CAUTION", "requiere cuidado", StringComparison.OrdinalIgnoreCase);

        return result;
    }

    private static string HumanizeId(string id)
    {
        var text = id
            .Replace("WPFTweaks", "", StringComparison.OrdinalIgnoreCase)
            .Replace("WPF", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(text))
            return "Opción de WinUtil";

        var chars = new List<char>();
        for (var i = 0; i < text.Length; i++)
        {
            if (i > 0 && char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                chars.Add(' ');

            chars.Add(text[i]);
        }

        return "Opción: " + new string(chars.ToArray());
    }
}
