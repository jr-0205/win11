namespace Windows11Optimizer.Services;

/// <summary>
/// Traducciones de presentación para el catálogo fijado de WinUtil 26.08.19.
/// Los IDs originales se conservan intactos para compatibilidad y auditoría.
/// </summary>
public static class WinUtilSpanishTranslator
{
    private static readonly Dictionary<string, string> Titles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["WPFTweaksActivity"] = "Desactivar el historial de actividad",
            ["WPFTweaksHiber"] = "Desactivar la hibernación",
            ["WPFTweaksWidget"] = "Quitar Widgets",
            ["WPFTweaksRevertStartMenu"] = "Usar el diseño anterior del menú Inicio",
            ["WPFTweaksDisableStoreSearch"] = "Ocultar recomendaciones de Microsoft Store en la búsqueda",
            ["WPFTweaksLocation"] = "Desactivar el seguimiento de ubicación",
            ["WPFTweaksServices"] = "Poner servicios seleccionados en modo manual",
            ["WPFTweaksBraveDebloat"] = "Reducir funciones adicionales de Brave",
            ["WPFTweaksDisableWarningForUnsignedRdp"] = "Ocultar advertencias de archivos RDP sin firma",
            ["WPFTweaksEdgeDebloat"] = "Reducir funciones adicionales de Microsoft Edge",
            ["WPFTweaksConsumerFeatures"] = "Desactivar contenido promocional de Windows",
            ["WPFTweaksTelemetry"] = "Reducir telemetría de Windows",
            ["WPFTweaksDeliveryOptimization"] = "Desactivar la optimización de entrega",
            ["WPFTweaksRemoveEdge"] = "Quitar Microsoft Edge",
            ["WPFTweaksDisableBitLocker"] = "Desactivar BitLocker",
            ["WPFTweaksUTC"] = "Usar UTC para el reloj del sistema",
            ["WPFTweaksRemoveOneDrive"] = "Quitar OneDrive",
            ["WPFTweaksRemoveHomeAndGallery"] = "Ocultar Inicio y Galería del Explorador de archivos",
            ["WPFTweaksDisplay"] = "Priorizar rendimiento sobre efectos visuales",
            ["WPFTweaksReservedStorage"] = "Desactivar el almacenamiento reservado de Windows",
            ["WPFTweaksRestorePoint"] = "Crear un punto de restauración",
            ["WPFTweaksEndTaskOnTaskbar"] = "Añadir “Finalizar tarea” al clic derecho",
            ["WPFTweaksStorage"] = "Desactivar el Sensor de almacenamiento",
            ["WPFTweaksWindowsAI"] = "Desactivar funciones de IA integradas en Windows",
            ["WPFTweaksWPBT"] = "Bloquear software del fabricante cargado desde el firmware",
            ["WPFTweaksPreventDeviceMetadataFromNetwork"] = "Evitar instalación automática de apps para dispositivos",
            ["WPFTweaksRazerBlock"] = "Evitar instalación automática del software de Razer",
            ["WPFTweaksDisableNotifications"] = "Desactivar notificaciones y calendario de la bandeja",
            ["WPFTweaksBlockAdobeNet"] = "Bloquear dominios de Adobe definidos por WinUtil",
            ["WPFTweaksRightClickMenu"] = "Usar el menú del clic derecho clásico",
            ["WPFTweaksDiskCleanup"] = "Ejecutar limpieza de disco",
            ["WPFTweaksDeleteTempFiles"] = "Eliminar archivos temporales",
            ["WPFTweaksIPv46"] = "Preferir IPv4 sin desactivar IPv6",
            ["WPFTweaksTeredo"] = "Desactivar Teredo",
            ["WPFTweaksDisableIPv6"] = "Desactivar IPv6",
            ["WPFTweaksDisableBGapps"] = "Limitar aplicaciones en segundo plano",
            ["WPFTweaksDisableExplorerAutoDiscovery"] = "Desactivar detección automática de carpetas en el Explorador",
            ["WPFToggleDetailedBSoD"] = "Mostrar más detalles cuando Windows falla",
            ["WPFToggleBatteryPercentage"] = "Mostrar porcentaje de batería en la bandeja",
            ["WPFToggleDarkMode"] = "Usar tema oscuro de Windows",
            ["WPFToggleShowExt"] = "Mostrar extensiones de archivo",
            ["WPFToggleHiddenFiles"] = "Mostrar archivos ocultos",
            ["WPFToggleVerboseLogon"] = "Mostrar detalles durante el inicio y cierre de sesión",
            ["WPFToggleNewOutlook"] = "Usar la versión nueva de Outlook",
            ["WPFToggleScrollbars"] = "Mantener visibles las barras de desplazamiento",
            ["WPFMultiplaneOverlay"] = "Configurar composición gráfica de ventanas (MPO)",
            ["WPFToggleMouseAcceleration"] = "Cambiar la aceleración del mouse",
            ["WPFToggleNumLock"] = "Activar Bloq Num al iniciar",
            ["WPFToggleWindowSnapping"] = "Activar ajuste automático de ventanas",
            ["WPFToggleStandbyFix"] = "Configurar red durante la suspensión moderna",
            ["WPFToggleS3Sleep"] = "Usar suspensión clásica S3",
            ["WPFToggleHideSettingsHome"] = "Mostrar u ocultar la página principal de Configuración",
            ["WPFToggleBingSearch"] = "Mostrar u ocultar resultados web de Bing en Inicio",
            ["WPFToggleLoginBlur"] = "Cambiar el desenfoque de la pantalla de inicio de sesión",
            ["WPFTweaksDisableLockscreen"] = "Desactivar la pantalla de bloqueo",
            ["WPFToggleStartMenuRecommendations"] = "Mostrar u ocultar recomendaciones del menú Inicio",
            ["WPFToggleStickyKeys"] = "Activar o desactivar Teclas especiales",
            ["WPFToggleTaskbarAlignment"] = "Centrar o alinear a la izquierda los iconos de la barra",
            ["WPFToggleTaskbarSearch"] = "Mostrar u ocultar el icono de búsqueda",
            ["WPFToggleTaskView"] = "Mostrar u ocultar Vista de tareas",
            ["WPFToggleGameMode"] = "Activar o desactivar Modo Juego",
            ["WPFToggleLongPaths"] = "Permitir rutas de archivos largas",
            ["WPFOOSUbutton"] = "Abrir O&O ShutUp10++",
            ["WPFchangedns"] = "Cambiar los servidores DNS",
            ["WPFAddUltPerf"] = "Activar el plan Máximo rendimiento",
            ["WPFRemoveUltPerf"] = "Desactivar el plan Máximo rendimiento"
        };

    private static readonly Dictionary<string, string> Descriptions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["WPFTweaksActivity"] = "Reduce el registro de actividades recientes de Windows.",
            ["WPFTweaksHiber"] = "Desactiva la hibernación. Puede liberar espacio en disco, pero también puede afectar Inicio rápido.",
            ["WPFTweaksWidget"] = "Quita Widgets y reduce sus procesos en segundo plano.",
            ["WPFTweaksRevertStartMenu"] = "Cambia el aspecto o comportamiento del menú Inicio para acercarlo a un diseño anterior.",
            ["WPFTweaksDisableStoreSearch"] = "Evita que la búsqueda de Windows muestre recomendaciones de Microsoft Store.",
            ["WPFTweaksLocation"] = "Reduce funciones que usan la ubicación. Algunas aplicaciones pueden perder funciones basadas en tu ubicación.",
            ["WPFTweaksServices"] = "Cambia varios servicios de Windows para que solo se inicien cuando sean necesarios. Debe revisarse antes de aplicarse.",
            ["WPFTweaksBraveDebloat"] = "Reduce funciones opcionales, promociones y componentes adicionales de Brave.",
            ["WPFTweaksDisableWarningForUnsignedRdp"] = "Oculta avisos relacionados con archivos de conexión remota sin firma. Es una opción avanzada y puede reducir advertencias de seguridad.",
            ["WPFTweaksEdgeDebloat"] = "Reduce funciones opcionales y componentes adicionales de Microsoft Edge sin necesariamente eliminarlo.",
            ["WPFTweaksConsumerFeatures"] = "Reduce sugerencias, promociones y contenido que Windows puede instalar o recomendar.",
            ["WPFTweaksTelemetry"] = "Reduce el envío de datos de diagnóstico y uso a Microsoft.",
            ["WPFTweaksDeliveryOptimization"] = "Evita que Windows comparta o descargue actualizaciones desde otros equipos mediante Optimización de entrega.",
            ["WPFTweaksRemoveEdge"] = "Intenta eliminar Microsoft Edge. Puede afectar componentes de Windows y aplicaciones que usan su motor.",
            ["WPFTweaksDisableBitLocker"] = "Desactiva el cifrado BitLocker. Puede reducir la protección de tus datos.",
            ["WPFTweaksUTC"] = "Hace que Windows interprete el reloj de hardware usando UTC. Es útil sobre todo en equipos con varios sistemas operativos.",
            ["WPFTweaksRemoveOneDrive"] = "Quita la integración de OneDrive. No conviene si sincronizas archivos con OneDrive.",
            ["WPFTweaksRemoveHomeAndGallery"] = "Oculta las secciones Inicio y Galería del Explorador de archivos.",
            ["WPFTweaksDisplay"] = "Reduce animaciones y efectos visuales para favorecer el rendimiento.",
            ["WPFTweaksReservedStorage"] = "Desactiva el espacio que Windows reserva para actualizaciones y mantenimiento.",
            ["WPFTweaksRestorePoint"] = "Crea un punto de restauración antes de realizar cambios importantes.",
            ["WPFTweaksEndTaskOnTaskbar"] = "Permite cerrar una aplicación desde el menú del clic derecho de su icono en la barra de tareas.",
            ["WPFTweaksStorage"] = "Desactiva la limpieza automática de archivos mediante Sensor de almacenamiento.",
            ["WPFTweaksWindowsAI"] = "Reduce o elimina algunas funciones de inteligencia artificial integradas en Windows. Puede afectar funciones nuevas del sistema.",
            ["WPFTweaksWPBT"] = "Bloquea un mecanismo que algunos fabricantes pueden usar para cargar software desde el firmware durante el arranque.",
            ["WPFTweaksPreventDeviceMetadataFromNetwork"] = "Evita que Windows descargue automáticamente algunas aplicaciones complementarias para dispositivos conectados.",
            ["WPFTweaksRazerBlock"] = "Evita que el software de Razer se instale automáticamente al conectar hardware compatible.",
            ["WPFTweaksDisableNotifications"] = "Oculta notificaciones y elementos del calendario de la bandeja. Puede hacer que pierdas avisos útiles.",
            ["WPFTweaksBlockAdobeNet"] = "Añade bloqueos de red relacionados con servicios de Adobe. Puede afectar activación, actualizaciones o funciones en línea.",
            ["WPFTweaksRightClickMenu"] = "Hace que el menú clásico del clic derecho aparezca directamente.",
            ["WPFTweaksDiskCleanup"] = "Ejecuta herramientas de limpieza para recuperar espacio en disco.",
            ["WPFTweaksDeleteTempFiles"] = "Elimina archivos temporales que normalmente pueden regenerarse cuando sean necesarios.",
            ["WPFTweaksIPv46"] = "Mantiene IPv6 disponible, pero hace que Windows prefiera IPv4 cuando ambos están disponibles.",
            ["WPFTweaksTeredo"] = "Desactiva una tecnología de túnel de red usada por algunas funciones y juegos. Puede afectar conectividad específica.",
            ["WPFTweaksDisableIPv6"] = "Desactiva IPv6. No es recomendable salvo que tengas un motivo concreto de red.",
            ["WPFTweaksDisableBGapps"] = "Reduce aplicaciones que pueden seguir ejecutándose cuando no están abiertas.",
            ["WPFTweaksDisableExplorerAutoDiscovery"] = "Evita que el Explorador intente adivinar automáticamente el tipo de contenido de algunas carpetas.",
            ["WPFToggleDetailedBSoD"] = "Muestra información técnica adicional cuando Windows encuentra un error grave.",
            ["WPFToggleBatteryPercentage"] = "Muestra el porcentaje de batería junto al icono de batería.",
            ["WPFToggleDarkMode"] = "Cambia entre el tema claro y oscuro de Windows.",
            ["WPFToggleShowExt"] = "Muestra extensiones como .exe, .jpg o .txt en los nombres de archivo.",
            ["WPFToggleHiddenFiles"] = "Permite ver archivos y carpetas marcados como ocultos.",
            ["WPFToggleVerboseLogon"] = "Muestra mensajes más detallados mientras Windows inicia, cierra sesión o apaga.",
            ["WPFToggleNewOutlook"] = "Cambia la preferencia relacionada con la nueva aplicación Outlook.",
            ["WPFToggleScrollbars"] = "Hace que las barras de desplazamiento permanezcan visibles en lugar de ocultarse automáticamente.",
            ["WPFMultiplaneOverlay"] = "MPO es una función gráfica de Windows. Cambiarla puede ayudar en algunos problemas de parpadeo, vídeo o drivers, pero no conviene modificarla sin necesidad.",
            ["WPFToggleMouseAcceleration"] = "Cambia si Windows ajusta la velocidad del puntero según la rapidez con la que mueves el mouse.",
            ["WPFToggleNumLock"] = "Decide si el teclado numérico empieza activado al iniciar Windows.",
            ["WPFToggleWindowSnapping"] = "Controla las funciones para acomodar ventanas automáticamente en zonas de la pantalla.",
            ["WPFToggleStandbyFix"] = "Controla si la red puede permanecer activa durante la suspensión moderna S0.",
            ["WPFToggleS3Sleep"] = "Intenta usar el modo de suspensión clásico S3 cuando el hardware lo admite.",
            ["WPFToggleHideSettingsHome"] = "Muestra u oculta la página principal de la aplicación Configuración.",
            ["WPFToggleBingSearch"] = "Controla si el menú Inicio puede mostrar resultados de búsqueda web de Bing.",
            ["WPFToggleLoginBlur"] = "Activa o desactiva el efecto de desenfoque de la pantalla de inicio de sesión.",
            ["WPFTweaksDisableLockscreen"] = "Evita mostrar la pantalla de bloqueo antes del inicio de sesión.",
            ["WPFToggleStartMenuRecommendations"] = "Controla la sección de recomendaciones del menú Inicio.",
            ["WPFToggleStickyKeys"] = "Controla Teclas especiales, una función de accesibilidad para combinaciones de teclado.",
            ["WPFToggleTaskbarAlignment"] = "Cambia la posición de los iconos principales de la barra de tareas.",
            ["WPFToggleTaskbarSearch"] = "Muestra u oculta el acceso a Búsqueda en la barra de tareas.",
            ["WPFToggleTaskView"] = "Muestra u oculta el botón Vista de tareas de la barra.",
            ["WPFToggleGameMode"] = "Controla el Modo Juego de Windows, que prioriza recursos durante algunos juegos.",
            ["WPFToggleLongPaths"] = "Permite a aplicaciones compatibles usar rutas de archivo más largas que el límite clásico.",
            ["WPFOOSUbutton"] = "Abre una herramienta externa de privacidad. Windows11Optimizer no la ejecuta automáticamente.",
            ["WPFchangedns"] = "Permite elegir otros servidores DNS. Esto puede cambiar cómo tu equipo resuelve nombres de Internet.",
            ["WPFAddUltPerf"] = "Activa un plan de energía que prioriza rendimiento y aumenta consumo. No está pensado para portátiles.",
            ["WPFRemoveUltPerf"] = "Quita el plan de energía Máximo rendimiento si estaba habilitado."
        };

    private static readonly Dictionary<string, string> Categories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Essential Tweaks"] = "Ajustes comunes",
            ["Customize Preferences"] = "Personalización",
            ["z__Advanced Tweaks - CAUTION"] = "Opciones avanzadas",
            ["Advanced Tweaks - CAUTION"] = "Opciones avanzadas",
            ["Advanced Tweaks"] = "Opciones avanzadas",
            ["Performance Plans - NOT FOR LAPTOPS"] = "Planes de energía (no recomendados en portátiles)"
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

        return "Opción de WinUtil";
    }

    public static string Description(string id, string original, string translatedTitle)
    {
        if (Descriptions.TryGetValue(id, out var value))
            return value;

        return $"Opción de WinUtil relacionada con “{translatedTitle}”. " +
               "Se muestra solo para consulta y Windows11Optimizer no la aplica automáticamente.";
    }

    public static string Category(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return "General";

        return Categories.TryGetValue(original, out var value)
            ? value
            : "General";
    }

    public static string PresetList(IEnumerable<string> originals)
    {
        return string.Join(", ",
            originals
                .Select(x => Presets.TryGetValue(x, out var value) ? value : x)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase));
    }
}
