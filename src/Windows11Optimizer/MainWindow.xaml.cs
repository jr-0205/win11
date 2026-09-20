using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;
using Windows11Optimizer.Services;

namespace Windows11Optimizer;

public partial class MainWindow : Window
{
    private readonly SystemMetricsService _metrics = new();
    private readonly WindowsServiceManager _serviceManager = new();
    private readonly ScheduledTaskManager _taskManager = new();
    private readonly BackupService _backup = new();
    private readonly StartupInventoryService _startup = new();
    private readonly WinUtilCatalogService _winUtil = new();
    private readonly WinUtilNativeService _winUtilNative = new();
    private readonly AutorunsService _autoruns = new();
    private readonly VirtualizationModeService _virtualization = new();
    private readonly ThemeService _theme = new();
    private readonly OptimizationService _optimizer;
    private readonly DiagnosticReportService _diagnostics;
    private readonly SmartAnalysisService _smartAnalysis;
    private readonly SmartOptimizationService _smartOptimizer;
    private SmartOptimizationPlan? _lastOptimizationPlan;
    private IReadOnlyList<WinUtilTweak> _winUtilAllTweaks = Array.Empty<WinUtilTweak>();
    private AutorunsAnalysisResult? _lastAutorunsAnalysis;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _toastTimer;
    private bool _refreshingMetrics;
    private bool _suppressThemeEvent;

    public MainWindow()
    {
        _theme.ApplySavedTheme();
        InitializeComponent();

        _optimizer = new OptimizationService(_serviceManager, _taskManager, _backup);
        _diagnostics = new DiagnosticReportService(_metrics, _serviceManager, _startup);
        _smartAnalysis = new SmartAnalysisService(_metrics, _startup, _autoruns);
        _smartOptimizer = new SmartOptimizationService(
            _serviceManager,
            _taskManager,
            _startup);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await RefreshMetricsAsync();

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            ToastBorder.Visibility = Visibility.Collapsed;
        };

        Loaded += MainWindow_Loaded;
        Closed += (_, _) =>
        {
            _timer.Stop();
            _toastTimer.Stop();
        };
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _suppressThemeEvent = true;
        DarkModeToggle.IsChecked = _theme.IsDark;
        _suppressThemeEvent = false;
        ApplyResponsiveLayout();

        BeginActivity("Inicializando", "Leyendo servicios, inicio y métricas del sistema…");

        AdminText.Text = $"Permisos de administrador: {(IsAdministrator() ? "Sí" : "No")}";
        RefreshBackupStatus();
        RefreshStartup();
        await RefreshServicesAsync();
        await RefreshVirtualizationAsync();
        await RefreshMetricsAsync();
        UpdateAutorunsAvailability();

        _timer.Start();
        _ = LoadWinUtilCatalogAsync(forceRefresh: false, showFeedback: false);

        Log("Aplicación iniciada. Perfil seguro cargado.");
        Log($"WinUtil: catálogo fijado {WinUtilCatalogService.Version} / {WinUtilCatalogService.Commit[..12]} con acciones nativas limitadas y reversibles.");

        EndActivity(
            "Listo",
            "Sistema analizado. Esperando una acción.",
            ActivityKind.Success);

        ShowToast(
            $"Windows 11 Optimizer listo · Tema {(_theme.IsDark ? "oscuro" : "claro")}",
            ActivityKind.Success);
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (!IsInitialized)
            return;

        var compact = ActualWidth < 940;

        MetricsGrid.Columns = compact ? 2 : 4;
        VirtualizationStatusGrid.Columns = compact ? 1 : 2;
        ActivityProgress.Width = compact ? 120 : 180;
    }

    private void UpdateAutorunsAvailability()
    {
        var tool = _autoruns.FindTool();

        if (tool is null)
        {
            AutorunsStatusText.Text =
                "Autoruns no está instalado. El análisis propio de la app sigue funcionando; puedes añadir Autoruns para revisar más ubicaciones de inicio.";
            return;
        }

        AutorunsStatusText.Text =
            $"Autoruns detectado: {Path.GetFileName(tool)}. Listo para análisis avanzado.";
    }

    private async void AnalyzeComputer_Click(object sender, RoutedEventArgs e)
    {
        BeginActivity(
            "Analizando el equipo",
            "Revisando recursos, inicio de Windows y componentes conocidos…");

        try
        {
            var snapshot = await _smartAnalysis.CaptureAsync(includeAutoruns: false);
            var plan = await _smartOptimizer.AnalyzeAsync();
            _lastOptimizationPlan = plan;

            OptimizationPlanGrid.ItemsSource = plan.Opportunities;
            SmartOptimizationSummaryText.Text = plan.Summary;
            ApplySmartOptimizationButton.IsEnabled = plan.ApplicableCount > 0;

            var autoruns = await _autoruns.AnalyzeAsync();
            _lastAutorunsAnalysis = autoruns;
            AutorunsFindingsGrid.ItemsSource = autoruns.Entries;
            UpdateAutorunsResult(autoruns);

            SmartAnalysisText.Text =
                $"RAM: {snapshot.UsedRamGb:N2} de {snapshot.TotalRamGb:N2} GB ({snapshot.RamPercent:N1} %). " +
                $"Procesos: {snapshot.ProcessCount}. " +
                $"Inicio: {snapshot.StartupEntryCount} elementos, {snapshot.OrphanedStartupCount} antiguos. " +
                $"{plan.Summary}";

            EndActivity(
                "Análisis completado",
                "No se realizaron cambios.",
                ActivityKind.Success);

            ShowToast(
                plan.ApplicableCount > 0
                    ? "Análisis listo. Hay recomendaciones disponibles."
                    : "Análisis listo. No encontramos cambios necesarios.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error en análisis inteligente: {ex.Message}");
            EndActivity(
                "No se pudo completar el análisis",
                ex.Message,
                ActivityKind.Error);
            ShowToast(
                "No se pudo completar el análisis.",
                ActivityKind.Error);
        }
    }

    private async void AnalyzeAutoruns_Click(object sender, RoutedEventArgs e)
    {
        await AnalyzeAutorunsInternalAsync(showToast: true);
    }

    private async Task AnalyzeAutorunsInternalAsync(bool showToast)
    {
        BeginActivity(
            "Analizando inicio con Autoruns",
            "Revisando entradas de terceros, tareas, servicios y otras ubicaciones de autoarranque…");

        var result = await _autoruns.AnalyzeAsync();
        _lastAutorunsAnalysis = result;
        AutorunsFindingsGrid.ItemsSource = result.Entries;
        UpdateAutorunsResult(result);

        if (!result.Available)
        {
            EndActivity(
                "Autoruns no disponible",
                "Puedes instalarlo desde la página oficial de Microsoft Sysinternals.",
                ActivityKind.Warning);

            if (showToast)
                ShowToast("Autoruns no está instalado.", ActivityKind.Warning);

            return;
        }

        EndActivity(
            "Análisis de Autoruns completado",
            $"{result.ThirdPartyCount} entradas de terceros; {result.MissingCount} con archivo no encontrado.",
            ActivityKind.Success);

        if (showToast)
        {
            ShowToast(
                $"Autoruns encontró {result.MissingCount} entradas con archivo no encontrado.",
                result.MissingCount > 0 ? ActivityKind.Warning : ActivityKind.Success);
        }
    }

    private void UpdateAutorunsResult(AutorunsAnalysisResult result)
    {
        if (!result.Available)
        {
            AutorunsStatusText.Text =
                "Autoruns no está instalado. Pulsa “Abrir / obtener Autoruns” para ir a la página oficial.";
            return;
        }

        if (result.Entries.Count == 0)
        {
            AutorunsStatusText.Text = result.Message;
            return;
        }

        AutorunsStatusText.Text =
            $"Análisis avanzado: {result.ThirdPartyCount} entradas de terceros. " +
            $"{result.MissingCount} apuntan a archivos que ya no se encontraron.";
    }

    private void OpenAutorunsDownload_Click(object sender, RoutedEventArgs e) =>
        OpenShell(AutorunsService.OfficialPage);

    private void OpenAutoruns_Click(object sender, RoutedEventArgs e)
    {
        var gui = _autoruns.FindGui();
        if (!string.IsNullOrWhiteSpace(gui))
        {
            OpenShell(gui);
            return;
        }

        OpenShell(AutorunsService.OfficialPage);
    }

    private int CountRunningManageableServices()
    {
        var count = 0;

        foreach (var name in SafeProfile.UserManageableServices)
        {
            var info = _serviceManager.GetInfo(name, "", "");
            if (info is not null &&
                string.Equals(info.State, "Running", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private async Task RefreshMetricsAsync()
    {
        if (_refreshingMetrics) return;
        _refreshingMetrics = true;
        try
        {
            var m = await _metrics.GetAsync();
            CpuText.Text = $"{m.CpuPercent:N1} %";
            RamText.Text = $"{m.RamPercent:N1} %";
            RamGbText.Text = $"{m.UsedRamGb:N2} / {m.TotalRamGb:N2} GB";
            ProcessText.Text = m.ProcessCount.ToString();
        }
        catch (Exception ex)
        {
            Log($"Error leyendo métricas: {ex.Message}");
        }
        finally
        {
            _refreshingMetrics = false;
        }
    }

    private async Task RefreshServicesAsync()
    {
        var plan = await _smartOptimizer.AnalyzeAsync();
        _lastOptimizationPlan = plan;

        OptimizationPlanGrid.ItemsSource = plan.Opportunities;
        SmartOptimizationSummaryText.Text = plan.Summary;
        ApplySmartOptimizationButton.IsEnabled = plan.ApplicableCount > 0;

        RefreshVmwareServiceProfile();
    }

    private void RefreshStartup()
    {
        var entries = _startup.GetEntries();
        StartupGrid.ItemsSource = entries;

        var orphanedCount = entries.Count(x => x.IsOrphaned);

        StartupSummaryText.Text = orphanedCount switch
        {
            0 => $"Se detectaron {entries.Count} entradas de inicio. No encontramos referencias huérfanas confirmadas.",
            1 => $"Se detectaron {entries.Count} entradas de inicio. Hay 1 entrada huérfana que puede limpiarse de forma segura.",
            _ => $"Se detectaron {entries.Count} entradas de inicio. Hay {orphanedCount} entradas huérfanas que pueden revisarse."
        };

        RestoreStartupEntryButton.IsEnabled = _startup.HasRestorableEntry;
    }

    private void RefreshStartup_Click(object sender, RoutedEventArgs e)
    {
        BeginActivity(
            "Actualizando programas de inicio",
            "Comprobando si los programas registrados todavía existen…");

        try
        {
            RefreshStartup();

            EndActivity(
                "Programas de inicio actualizados",
                StartupSummaryText.Text,
                ActivityKind.Success);

            ShowToast(
                "Lista de inicio actualizada.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error actualizando inicio de Windows: {ex.Message}");

            EndActivity(
                "No se pudo actualizar el inicio",
                ex.Message,
                ActivityKind.Error);

            ShowToast(
                "No se pudo actualizar la lista de inicio.",
                ActivityKind.Error);
        }
    }

    private void StartupGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is null)
            return;

        StartupGrid.SelectedItem = row.Item;
        row.Focus();
    }

    private void OpenStartupEntryLocation_Click(object sender, RoutedEventArgs e)
    {
        if (StartupGrid.SelectedItem is not StartupEntry entry)
        {
            ShowToast(
                "Selecciona primero una entrada de inicio.",
                ActivityKind.Warning);
            return;
        }

        var candidate = GetStartupEntryPath(entry);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            ShowToast(
                "Esta entrada no tiene una ubicación de archivo que podamos abrir.",
                ActivityKind.Warning);
            return;
        }

        try
        {
            candidate = Environment.ExpandEnvironmentVariables(candidate);

            if (File.Exists(candidate))
            {
                Process.Start(new ProcessStartInfo(
                    "explorer.exe",
                    $"/select,\"{candidate}\"")
                {
                    UseShellExecute = true
                });

                Log($"Ubicación de inicio abierta: {entry.Name} | {candidate}");
                return;
            }

            if (Directory.Exists(candidate))
            {
                Process.Start(new ProcessStartInfo(candidate)
                {
                    UseShellExecute = true
                });

                Log($"Directorio de inicio abierto: {entry.Name} | {candidate}");
                return;
            }

            var parent = Path.GetDirectoryName(candidate);
            while (!string.IsNullOrWhiteSpace(parent) && !Directory.Exists(parent))
            {
                var next = Path.GetDirectoryName(parent);
                if (string.Equals(next, parent, StringComparison.OrdinalIgnoreCase))
                    break;

                parent = next;
            }

            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
            {
                Process.Start(new ProcessStartInfo(parent)
                {
                    UseShellExecute = true
                });

                ShowToast(
                    "La carpeta exacta ya no existe. Abrimos la ubicación disponible más cercana.",
                    ActivityKind.Info);

                Log($"Ubicación aproximada abierta para entrada huérfana: {entry.Name} | {parent}");
                return;
            }

            ShowToast(
                "La ubicación de esta entrada ya no existe en el equipo.",
                ActivityKind.Warning);
        }
        catch (Exception ex)
        {
            Log($"Error abriendo ubicación de inicio: {ex.Message}");

            ShowToast(
                "No se pudo abrir la ubicación de esta entrada.",
                ActivityKind.Error);
        }
    }

    private static string GetStartupEntryPath(StartupEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.StartupFilePath))
            return entry.StartupFilePath;

        if (!string.IsNullOrWhiteSpace(entry.TargetPath))
            return entry.TargetPath;

        return "";
    }

    private static T? FindVisualParent<T>(DependencyObject? child)
        where T : DependencyObject
    {
        var current = child;

        while (current is not null)
        {
            if (current is T typed)
                return typed;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void RemoveOrphanedStartup_Click(object sender, RoutedEventArgs e)
    {
        if (StartupGrid.SelectedItem is not StartupEntry entry)
        {
            ShowToast(
                "Selecciona primero una entrada de inicio.",
                ActivityKind.Warning);
            return;
        }

        if (!entry.CanRemoveSafely)
        {
            var message = entry.IsOrphaned
                ? "La entrada parece huérfana, pero su origen no permite una limpieza automática segura."
                : "Esta entrada no está confirmada como huérfana. No se eliminará.";

            ShowToast(message, ActivityKind.Warning);
            return;
        }

        var target = string.IsNullOrWhiteSpace(entry.TargetPath)
            ? entry.Command
            : entry.TargetPath;

        var answer = MessageBox.Show(
            $"'{entry.Name}' apunta a un programa que ya no existe:\n\n{target}\n\n" +
            "Se quitará únicamente su referencia de inicio de Windows. " +
            "Antes se guardará una copia para poder restaurarla.\n\n¿Continuar?",
            "Quitar entrada huérfana",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            _startup.RemoveOrphanedEntry(entry);

            Log($"Entrada de inicio huérfana eliminada: {entry.Name} | {entry.Source}");
            RefreshStartup();

            ShowToast(
                $"Se quitó '{entry.Name}' del inicio de Windows.",
                ActivityKind.Success);

            EndActivity(
                "Entrada huérfana eliminada",
                "La referencia antigua fue retirada y se guardó una copia.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error eliminando entrada de inicio: {ex.Message}");

            ShowToast(
                "No se pudo quitar la entrada seleccionada.",
                ActivityKind.Error);

            MessageBox.Show(
                ex.Message,
                "Quitar entrada huérfana",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RestoreStartupEntry_Click(object sender, RoutedEventArgs e)
    {
        if (!_startup.HasRestorableEntry)
        {
            RestoreStartupEntryButton.IsEnabled = false;
            ShowToast(
                "No hay entradas eliminadas por la app pendientes de restaurar.",
                ActivityKind.Info);
            return;
        }

        var answer = MessageBox.Show(
            "Se restaurará la última entrada de inicio eliminada por Windows11Optimizer.\n\n¿Continuar?",
            "Restaurar entrada de inicio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            var restored = _startup.RestoreLastRemoved();
            RefreshStartup();

            Log($"Entrada de inicio restaurada: {restored.Name}");

            ShowToast(
                $"Se restauró '{restored.Name}'.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error restaurando entrada de inicio: {ex.Message}");

            ShowToast(
                "No se pudo restaurar la entrada.",
                ActivityKind.Error);
        }
    }

    private void RefreshBackupStatus()
    {
        BackupText.Text = _backup.Exists
            ? "Copia de seguridad: disponible"
            : "Copia de seguridad: aún no creada";
    }

    private async void OptimizeSafe_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "La app medirá el estado actual, aplicará únicamente el perfil seguro y volverá a medir para mostrarte el resultado.\n\n" +
            "Se creará una copia de seguridad antes de cambiar servicios o tareas. ¿Continuar?",
            "Optimizar ahora",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Optimización cancelada. No se realizó ningún cambio.", ActivityKind.Info);
            return;
        }

        try
        {
            var before = await _smartAnalysis.CaptureAsync(includeAutoruns: false);

            await RunOperationAsync(
                "Aplicando optimización segura",
                "Deteniendo componentes bajo demanda y actualizando tareas aprobadas…",
                () => _optimizer.OptimizeSafeAsync(Log));

            await Task.Delay(1200);
            var after = await _smartAnalysis.CaptureAsync(includeAutoruns: false);

            var ramSavedMb = Math.Max(
                0,
                (before.UsedRamGb - after.UsedRamGb) * 1024d);

            var processReduction = Math.Max(
                0,
                before.ProcessCount - after.ProcessCount);

            OptimizationResultText.Text =
                $"Resultado de esta sesión: RAM {before.UsedRamGb:N2} → {after.UsedRamGb:N2} GB " +
                $"(≈ {ramSavedMb:N0} MB menos). Procesos {before.ProcessCount} → {after.ProcessCount} " +
                $"({processReduction} menos). " +
                $"Las cifras pueden variar unos minutos después por procesos normales de Windows.";

            RefreshBackupStatus();
            RefreshStartup();
        }
        catch (Exception ex)
        {
            Log($"Error midiendo optimización: {ex.Message}");
            OptimizationResultText.Text =
                "La optimización terminó, pero no fue posible calcular la comparación antes/después.";
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (!_backup.Exists)
        {
            ShowToast("No existe todavía una copia de seguridad para restaurar.", ActivityKind.Warning);
            MessageBox.Show(
                "No existe una copia de seguridad creada por la aplicación.",
                "Restaurar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var answer = MessageBox.Show(
            "Se restaurarán los estados guardados antes de la primera optimización. ¿Continuar?",
            "Restaurar configuración",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Restauración cancelada.", ActivityKind.Info);
            return;
        }

        await RunOperationAsync(
            "Restaurando configuración",
            "Reponiendo servicios y tareas guardados en el backup…",
            () => _optimizer.RestoreAsync(Log));
    }

    private async void StartVmware_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync(
            "Iniciando VMware",
            "Activando servicios de red VMware bajo demanda…",
            () => _optimizer.StartVmwareAsync(Log));

    private async void StopVmware_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync(
            "Deteniendo VMware",
            "Deteniendo servicios VMware y bloqueando su próximo arranque…",
            () => _optimizer.StopVmwareAsync(Log));

    private async void StartAcer_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync(
            "Iniciando utilidades Acer",
            "Activando temporalmente los componentes Acer administrados…",
            () => _optimizer.StartAcerAsync(Log));

    private async void StopAcer_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync(
            "Deteniendo utilidades Acer",
            "Deteniendo los componentes Acer administrados…",
            () => _optimizer.StopAcerAsync(Log));

    private async void RefreshServices_Click(object sender, RoutedEventArgs e)
    {
        BeginActivity("Actualizando servicios", "Leyendo el estado actual desde Windows…");

        try
        {
            await RefreshServicesAsync();
            EndActivity("Servicios actualizados", "La tabla refleja el estado actual.", ActivityKind.Success);
            ShowToast("Servicios actualizados.", ActivityKind.Success);
        }
        catch (Exception ex)
        {
            EndActivity("Error actualizando servicios", ex.Message, ActivityKind.Error);
            ShowToast("No se pudo actualizar la lista de servicios.", ActivityKind.Error);
            Log($"Error actualizando servicios: {ex.Message}");
        }
    }

    private void ServicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ServicesGrid.SelectedItem is not ManagedService service)
        {
            SelectedServiceText.Text = "Ninguno";
            return;
        }

        var suffix = SafeProfile.IsUserManageableService(service.Name)
            ? " · editable"
            : " · protegido";

        SelectedServiceText.Text =
            $"{service.DisplayName} ({service.Name}){suffix}";
    }

    private async void SetServiceManual_Click(object sender, RoutedEventArgs e)
    {
        var service = GetSelectedManageableServiceOrNotify();
        if (service is null) return;

        await RunOperationAsync(
            $"Cambiando {service.DisplayName} a Manual",
            "Guardando backup y cambiando únicamente el tipo de inicio…",
            () => _optimizer.SetSafeServiceStartupAsync(service.Name, "manual", Log));
    }

    private async void SetServiceAutomatic_Click(object sender, RoutedEventArgs e)
    {
        var service = GetSelectedManageableServiceOrNotify();
        if (service is null) return;

        await RunOperationAsync(
            $"Cambiando {service.DisplayName} a Automático",
            "Guardando backup y habilitando el arranque automático…",
            () => _optimizer.SetSafeServiceStartupAsync(service.Name, "automatic", Log));
    }

    private async void StartSelectedService_Click(object sender, RoutedEventArgs e)
    {
        var service = GetSelectedManageableServiceOrNotify();
        if (service is null) return;

        await RunOperationAsync(
            $"Iniciando {service.DisplayName}",
            "Iniciando el servicio seguro seleccionado…",
            () => _optimizer.StartSafeServiceAsync(service.Name, Log));
    }

    private async void StopSelectedService_Click(object sender, RoutedEventArgs e)
    {
        var service = GetSelectedManageableServiceOrNotify();
        if (service is null) return;

        await RunOperationAsync(
            $"Deteniendo {service.DisplayName}",
            "Deteniendo el servicio seguro seleccionado…",
            () => _optimizer.StopSafeServiceAsync(service.Name, Log));
    }

    private async void DisableSelectedService_Click(object sender, RoutedEventArgs e)
    {
        var service = GetSelectedManageableServiceOrNotify();
        if (service is null) return;

        if (!SafeProfile.CanDisableService(service.Name))
        {
            ShowToast(
                "Este servicio no está aprobado para quedar deshabilitado.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            $"Se impedirá que '{service.DisplayName}' arranque automáticamente.\n\n" +
            "Podrás volver a Manual o Automático desde esta misma pantalla. ¿Continuar?",
            "Deshabilitar arranque",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Cambio cancelado.", ActivityKind.Info);
            return;
        }

        await RunOperationAsync(
            $"Deshabilitando arranque de {service.DisplayName}",
            "Guardando backup y cambiando el inicio a Deshabilitado…",
            () => _optimizer.SetSafeServiceStartupAsync(service.Name, "disabled", Log));
    }

    private ManagedService? GetSelectedManageableServiceOrNotify()
    {
        if (ServicesGrid.SelectedItem is not ManagedService service)
        {
            ShowToast(
                "Selecciona primero un servicio de la tabla.",
                ActivityKind.Warning);
            return null;
        }

        if (!SafeProfile.IsUserManageableService(service.Name))
        {
            ShowToast(
                $"'{service.DisplayName}' está protegido y Windows11Optimizer no permitirá modificarlo.",
                ActivityKind.Warning);

            Log($"Cambio bloqueado por política segura: {service.Name}");
            return null;
        }

        return service;
    }

    private async Task RefreshVirtualizationAsync()
    {
        try
        {
            var state = await _virtualization.GetStateAsync();

            VirtualizationBootModeText.Text = state.ConfiguredModeLabel;
            VirtualizationCurrentText.Text = state.CurrentHypervisorLabel;
            VirtualizationVbsText.Text = state.VbsLabel;
            VirtualizationMemoryIntegrityText.Text = state.MemoryIntegrityLabel;
            VirtualizationRestartText.Text = state.RestartLabel;

            VirtualizationBackupText.Text =
                _virtualization.BackupExists
                    ? "Disponible"
                    : "Aún no creada";

            RestartForVirtualizationButton.IsEnabled = state.PendingRestart;
            UseNormalVirtualizationButton.IsEnabled = !state.IsConfiguredForNormal;

            // Aunque el BCD ya esté en modo VMware, el botón sigue disponible
            // porque también prepara los servicios VMware seleccionados.
            UseVmwareVirtualizationButton.IsEnabled = true;
            UseVmwareVirtualizationButton.Content = state.IsConfiguredForVmware
                ? "Preparar servicios VMware"
                : "Preparar para VMware";

            RefreshVmwareServiceProfile();

            if (state.PendingRestart)
            {
                VirtualizationHelpText.Text =
                    "Hay un cambio de virtualización preparado que todavía no está aplicado en esta sesión. " +
                    "Reinicia únicamente si quieres usar ese nuevo modo ahora.";

                VirtualizationRestartHintText.Text =
                    "Hay un cambio pendiente. Reinicia cuando te convenga para aplicarlo.";
            }
            else if (state.IsConfiguredForVmware && !state.HypervisorPresentNow)
            {
                VirtualizationHelpText.Text =
                    "Modo VMware activo: el hipervisor de Windows no está en ejecución. " +
                    "Las políticas de VBS e Integridad de memoria no se eliminaron, pero las funciones que dependan " +
                    "del hipervisor no estarán activas durante este arranque.";

                VirtualizationRestartHintText.Text =
                    "Modo VMware ya activo para esta sesión. No necesitas reiniciar.";
            }
            else if (state.IsConfiguredForNormal && state.HypervisorPresentNow)
            {
                VirtualizationHelpText.Text =
                    "Modo normal activo: el hipervisor de Windows está disponible para Docker/WSL2, Hyper-V " +
                    "y funciones de seguridad basadas en virtualización.";

                VirtualizationRestartHintText.Text =
                    "Modo normal ya activo. No necesitas reiniciar.";
            }
            else
            {
                VirtualizationHelpText.Text =
                    "El modo normal está configurado, pero el hipervisor no aparece activo en esta sesión. " +
                    "Puede ser normal si ninguna función lo está usando; no recomendamos reiniciar solo por este estado.";

                VirtualizationRestartHintText.Text =
                    "No hay un cambio pendiente creado por la app.";
            }
        }
        catch (Exception ex)
        {
            VirtualizationBootModeText.Text = "No disponible";
            VirtualizationCurrentText.Text = "No disponible";
            VirtualizationVbsText.Text = "No disponible";
            VirtualizationMemoryIntegrityText.Text = "No disponible";
            VirtualizationRestartText.Text = "No disponible";
            VirtualizationBackupText.Text =
                _virtualization.BackupExists ? "Disponible" : "Aún no creada";
            VirtualizationHelpText.Text =
                "No se pudo comprobar el estado de virtualización.";
            VirtualizationRestartHintText.Text =
                "No reinicies desde la app hasta poder comprobar el estado.";
            RestartForVirtualizationButton.IsEnabled = false;
            UseNormalVirtualizationButton.IsEnabled = true;
            UseVmwareVirtualizationButton.IsEnabled = true;

            RefreshVmwareServiceProfile();
            Log($"Error leyendo virtualización: {ex.Message}");
        }
    }

    private void RefreshVmwareServiceProfile()
    {
        var coreInstalled = 0;
        var coreRunning = 0;

        foreach (var name in SafeProfile.VmwareCoreServices)
        {
            var info = _serviceManager.GetInfo(name, "", "");
            if (info is null)
                continue;

            coreInstalled++;

            if (string.Equals(
                    info.State,
                    "Running",
                    StringComparison.OrdinalIgnoreCase))
            {
                coreRunning++;
            }
        }

        var usb = GetVmwareOptionalServiceState(
            SafeProfile.VmwareUsbServices.FirstOrDefault());

        var autostart = GetVmwareOptionalServiceState(
            SafeProfile.VmwareAutostartServices.FirstOrDefault());

        VmwareServiceProfileText.Text =
            $"Servicios principales: {coreRunning}/{coreInstalled} en ejecución. " +
            $"USB: {usb}. Autoinicio: {autostart}.";
    }

    private string GetVmwareOptionalServiceState(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "no configurado";

        var info = _serviceManager.GetInfo(name, "", "");

        if (info is null)
            return "no instalado";

        return string.Equals(
                info.State,
                "Running",
                StringComparison.OrdinalIgnoreCase)
            ? "activo"
            : info.StartModeDisplay;
    }

    private async void SetVirtualizationNormal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var state = await _virtualization.GetStateAsync();

            if (state.IsConfiguredForNormal)
            {
                await RefreshVirtualizationAsync();

                if (state.PendingRestart)
                {
                    ShowToast(
                        "El modo normal ya está preparado. Solo falta reiniciar para aplicarlo.",
                        ActivityKind.Warning);
                }
                else
                {
                    ShowToast(
                        "El equipo ya está configurado en modo normal. No necesitas reiniciar.",
                        ActivityKind.Success);
                }

                return;
            }
        }
        catch (Exception ex)
        {
            Log($"No se pudo comprobar el modo actual: {ex.Message}");
        }

        var answer = MessageBox.Show(
            "Se preparará Windows para volver al modo normal.\n\n" +
            "El hipervisor de Windows quedará disponible en el próximo arranque para Docker/WSL2, Hyper-V " +
            "y funciones de seguridad basadas en virtualización. La app no cambia tus políticas de VBS o Integridad de memoria.\n\n" +
            "¿Continuar?",
            "Usar modo normal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Cambio de virtualización cancelado.", ActivityKind.Info);
            return;
        }

        await RunVirtualizationOperationAsync(
            "Preparando modo normal",
            "Conservando las políticas de seguridad y habilitando el arranque del hipervisor de Windows…",
            () => _virtualization.SetNormalAsync());
    }

    private async void SetVirtualizationVmware_Click(object sender, RoutedEventArgs e)
    {
        var enableUsb = VmwareUsbToggle.IsChecked == true;
        var enableAutostart = VmwareAutostartToggle.IsChecked == true;

        var optionalSummary =
            $"USB: {(enableUsb ? "sí" : "no")} · Autoinicio de VMs: {(enableAutostart ? "sí" : "no")}";

        var answer = MessageBox.Show(
            "Se preparará VMware y se configurará Windows para que el hipervisor de Windows no arranque " +
            "en el próximo inicio. Esto puede mejorar compatibilidad con VMware y virtualización anidada " +
            "cuando Hyper-V/VBS interfieren.\n\n" +
            "No se borrarán ni desactivarán permanentemente las políticas de VBS o Integridad de memoria. " +
            "Mientras el hipervisor esté fuera de ejecución, las funciones que dependan de él no podrán estar activas.\n\n" +
            $"Opciones: {optionalSummary}\n\n¿Continuar?",
            "Preparar para VMware",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Preparación de VMware cancelada.", ActivityKind.Info);
            return;
        }

        await RunVirtualizationOperationAsync(
            "Preparando modo VMware",
            "Preparando servicios VMware y configurando el próximo arranque sin el hipervisor de Windows…",
            async () =>
            {
                await _optimizer.PrepareVmwareAsync(
                    enableUsb,
                    enableAutostart,
                    Log);

                return await _virtualization.SetVmwareDirectAsync();
            });

        await RefreshServicesAsync();
        RefreshVmwareServiceProfile();
    }

    private async void RestoreVirtualization_Click(object sender, RoutedEventArgs e)
    {
        if (!_virtualization.BackupExists)
        {
            ShowToast(
                "No existe todavía una copia de seguridad de la configuración de virtualización.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "Se restaurará el valor de arranque del hipervisor que existía antes del primer cambio.\n\n" +
            "Las políticas de VBS e Integridad de memoria nunca fueron eliminadas por esta función. " +
            "Después se comprobará automáticamente si hace falta reiniciar. ¿Continuar?",
            "Volver al estado original",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        await RunVirtualizationOperationAsync(
            "Restaurando virtualización",
            "Restaurando el valor original del arranque del hipervisor…",
            () => _virtualization.RestoreAsync());
    }

    private async void RefreshVirtualization_Click(object sender, RoutedEventArgs e)
    {
        BeginActivity(
            "Actualizando virtualización",
            "Comprobando hipervisor, VBS, Integridad de memoria y servicios VMware…");

        try
        {
            await RefreshVirtualizationAsync();

            EndActivity(
                "Virtualización actualizada",
                "El estado mostrado corresponde a la configuración actual del equipo.",
                ActivityKind.Success);

            ShowToast(
                "Estado de virtualización actualizado.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            EndActivity(
                "Error actualizando virtualización",
                ex.Message,
                ActivityKind.Error);

            ShowToast(
                "No se pudo actualizar la virtualización.",
                ActivityKind.Error);
        }
    }

    private async void RestartForVirtualization_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var state = await _virtualization.GetStateAsync();

            if (!state.PendingRestart)
            {
                RestartForVirtualizationButton.IsEnabled = false;
                ShowToast(
                    "No hay ningún cambio pendiente que necesite reinicio.",
                    ActivityKind.Success);
                await RefreshVirtualizationAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            Log($"No se pudo validar el reinicio pendiente: {ex.Message}");
            ShowToast(
                "No se pudo comprobar si el reinicio es necesario.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "Hay un cambio de virtualización pendiente. Windows se reiniciará inmediatamente para aplicarlo.\n\n" +
            "Guarda cualquier trabajo abierto antes de continuar. ¿Reiniciar ahora?",
            "Reiniciar para aplicar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
            return;

        Log("Reinicio solicitado para aplicar un cambio pendiente de virtualización.");
        ShowToast("Reiniciando Windows…", ActivityKind.Warning);

        try
        {
            await VirtualizationModeService.RestartWindowsAsync();
        }
        catch (Exception ex)
        {
            Log($"Error solicitando reinicio: {ex.Message}");
            ShowToast("Windows no aceptó el reinicio.", ActivityKind.Error);
        }
    }

    private async Task RunVirtualizationOperationAsync(
        string title,
        string detail,
        Func<Task<bool>> operation)
    {
        BeginActivity(title, detail);

        try
        {
            Log(title + "…");
            var requiresRestart = await operation();

            await RefreshServicesAsync();
            await RefreshVirtualizationAsync();

            if (requiresRestart)
            {
                EndActivity(
                    title + ": cambio preparado",
                    "El nuevo modo necesita un reinicio para aplicarse en esta sesión.",
                    ActivityKind.Warning);

                ShowToast(
                    "Cambio preparado. Reinicia cuando quieras aplicarlo.",
                    ActivityKind.Warning);

                Log(title + ": configuración guardada. Reinicio necesario para aplicar el cambio.");
            }
            else
            {
                EndActivity(
                    title + ": listo",
                    "El estado actual ya es compatible. No necesitas reiniciar.",
                    ActivityKind.Success);

                ShowToast(
                    "Listo. No necesitas reiniciar el equipo.",
                    ActivityKind.Success);

                Log(title + ": configuración guardada sin reinicio necesario.");
            }
        }
        catch (Exception ex)
        {
            Log($"ERROR virtualización: {ex.Message}");

            EndActivity(
                title + ": error",
                ex.Message,
                ActivityKind.Error);

            ShowToast(
                "No se pudo cambiar la configuración de virtualización.",
                ActivityKind.Error);

            MessageBox.Show(
                ex.Message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RunOperationAsync(
        string title,
        string detail,
        Func<Task> operation)
    {
        BeginActivity(title, detail);

        try
        {
            Log(title + "…");
            await operation();
            await RefreshServicesAsync();
            await RefreshMetricsAsync();
            RefreshVmwareServiceProfile();

            var completed = title + ": completado";
            Log(completed + ".");

            EndActivity(
                completed,
                "Estado del sistema actualizado.",
                ActivityKind.Success);

            ShowToast(completed + ".", ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");

            EndActivity(
                title + ": error",
                ex.Message,
                ActivityKind.Error);

            ShowToast(
                $"{title}: ocurrió un error. Revisa el registro.",
                ActivityKind.Error);

            MessageBox.Show(
                ex.Message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ExportDiagnostic_Click(object sender, RoutedEventArgs e)
    {
        BeginActivity(
            "Generando diagnóstico",
            "Recolectando métricas, procesos, inicio y servicios…");

        try
        {
            var path = await _diagnostics.ExportToDesktopAsync();
            Log($"Diagnóstico exportado: {path}");

            EndActivity(
                "Diagnóstico generado",
                path,
                ActivityKind.Success);

            ShowToast("Reporte de diagnóstico creado en el Escritorio.", ActivityKind.Success);

            MessageBox.Show(
                $"Reporte generado en:\n{path}",
                "Diagnóstico",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"Error exportando diagnóstico: {ex.Message}");
            EndActivity("Error generando diagnóstico", ex.Message, ActivityKind.Error);
            ShowToast("No se pudo generar el diagnóstico.", ActivityKind.Error);
        }
    }

    private async void RefreshWinUtil_Click(object sender, RoutedEventArgs e) =>
        await LoadWinUtilCatalogAsync(forceRefresh: true, showFeedback: true);

    private async Task LoadWinUtilCatalogAsync(
        bool forceRefresh,
        bool showFeedback = true)
    {
        if (showFeedback)
        {
            BeginActivity(
                forceRefresh ? "Actualizando WinUtil" : "Cargando WinUtil",
                "Leyendo el catálogo fijado y preparando las opciones compatibles…");
        }

        try
        {
            WinUtilStatusText.Text = forceRefresh
                ? "Actualizando opciones…"
                : "Cargando opciones…";

            var result = await _winUtil.LoadAsync(forceRefresh);
            _winUtilAllTweaks = result.Tweaks;

            ApplyWinUtilFilter();

            var applicable = result.Tweaks.Count(x => _winUtilNative.Supports(x.Id));
            var source = result.FromCache ? "caché local" : "GitHub oficial";

            WinUtilStatusText.Text =
                $"{applicable} aplicables · {result.Tweaks.Count} totales · {source}";

            Log(
                $"WinUtil cargado: {result.Version}, commit {result.Commit[..12]}, " +
                $"{applicable} opciones portadas nativamente de {result.Tweaks.Count}.");

            if (showFeedback)
            {
                EndActivity(
                    "WinUtil listo",
                    $"{applicable} opciones se pueden aplicar de forma nativa y reversible.",
                    ActivityKind.Success);

                ShowToast(
                    $"WinUtil: {applicable} opciones listas para aplicar.",
                    ActivityKind.Success);
            }
        }
        catch (Exception ex)
        {
            WinUtilStatusText.Text = "No se pudo cargar WinUtil";
            Log($"Error cargando catálogo WinUtil: {ex.Message}");

            if (showFeedback)
            {
                EndActivity(
                    "Error cargando WinUtil",
                    ex.Message,
                    ActivityKind.Error);

                ShowToast(
                    "No se pudo cargar el catálogo. No se ejecutó código remoto.",
                    ActivityKind.Error);
            }
        }
    }

    private void WinUtilShowAllToggle_Changed(object sender, RoutedEventArgs e)
    {
        ApplyWinUtilFilter();
    }

    private void ApplyWinUtilFilter()
    {
        if (WinUtilGrid is null)
            return;

        var showAll = WinUtilShowAllToggle?.IsChecked == true;

        var items = _winUtilAllTweaks
            .Where(x => showAll || _winUtilNative.Supports(x.Id))
            .OrderByDescending(x => _winUtilNative.Supports(x.Id))
            .ThenBy(x => x.Category, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.Content, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        WinUtilGrid.ItemsSource = items;

        if (items.Count > 0)
            WinUtilGrid.SelectedIndex = 0;
        else
            ResetWinUtilDetails();
    }

    private void WinUtilGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateWinUtilSelection();
    }

    private void UpdateWinUtilSelection()
    {
        if (WinUtilGrid.SelectedItem is not WinUtilTweak tweak)
        {
            ResetWinUtilDetails();
            return;
        }

        var supported = _winUtilNative.Supports(tweak.Id);
        var state = supported
            ? _winUtilNative.GetState(tweak.Id)
            : "Solo consulta";

        WinUtilSelectedTitleText.Text = tweak.Content;
        WinUtilSelectedStateText.Text = supported
            ? $"{state} · Aplicación nativa"
            : $"{tweak.Risk} · Solo consulta";

        WinUtilSelectedDescriptionText.Text = tweak.Description;
        WinUtilSelectedCategoryText.Text =
            $"Categoría: {tweak.Category}" +
            (string.IsNullOrWhiteSpace(tweak.Presets)
                ? ""
                : $" · Perfiles WinUtil: {tweak.Presets}");

        WinUtilSelectedImpactText.Text = supported
            ? GetWinUtilImpactText(tweak.Id)
            : "Esta opción se muestra para que entiendas qué propone WinUtil. " +
              "Windows11Optimizer no la ejecutará hasta que exista una implementación nativa, revisada y reversible.";

        WinUtilTechnicalText.Text =
            $"ID: {tweak.Id}{Environment.NewLine}" +
            $"Nivel: {tweak.Risk}{Environment.NewLine}" +
            $"Acciones declaradas: {tweak.Actions}{Environment.NewLine}" +
            $"Nombre original: {tweak.OriginalContent}";

        WinUtilApplyButton.IsEnabled =
            supported &&
            !string.Equals(state, "Aplicado", StringComparison.OrdinalIgnoreCase);

        WinUtilRestoreButton.IsEnabled =
            supported &&
            _winUtilNative.HasBackup(tweak.Id);
    }

    private void ResetWinUtilDetails()
    {
        WinUtilSelectedTitleText.Text = "Selecciona una opción";
        WinUtilSelectedStateText.Text = "Sin seleccionar";
        WinUtilSelectedDescriptionText.Text =
            "Selecciona una opción de la lista para ver una explicación sencilla.";
        WinUtilSelectedImpactText.Text =
            "No se realizará ningún cambio hasta que pulses Aplicar.";
        WinUtilSelectedCategoryText.Text = "";
        WinUtilTechnicalText.Text = "ID, acciones y perfil aparecerán aquí.";
        WinUtilApplyButton.IsEnabled = false;
        WinUtilRestoreButton.IsEnabled = false;
    }

    private void ApplySelectedWinUtil_Click(object sender, RoutedEventArgs e)
    {
        if (WinUtilGrid.SelectedItem is not WinUtilTweak tweak ||
            !_winUtilNative.Supports(tweak.Id))
        {
            ShowToast(
                "Esta opción todavía es solo informativa.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            $"Se aplicará “{tweak.Content}”.\n\n" +
            "La app guardará primero el valor actual para que puedas deshacer el cambio. ¿Continuar?",
            "Aplicar ajuste",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            _winUtilNative.Apply(tweak.Id);
            UpdateWinUtilSelection();

            Log($"WinUtil nativo aplicado: {tweak.Id}");
            ShowToast(
                $"Aplicado: {tweak.Content}.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error aplicando WinUtil nativo {tweak.Id}: {ex.Message}");
            ShowToast(
                "No se pudo aplicar este ajuste.",
                ActivityKind.Error);
        }
    }

    private void RestoreSelectedWinUtil_Click(object sender, RoutedEventArgs e)
    {
        if (WinUtilGrid.SelectedItem is not WinUtilTweak tweak ||
            !_winUtilNative.Supports(tweak.Id))
        {
            return;
        }

        try
        {
            _winUtilNative.Restore(tweak.Id);
            UpdateWinUtilSelection();

            Log($"WinUtil nativo restaurado: {tweak.Id}");
            ShowToast(
                $"Cambio deshecho: {tweak.Content}.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error restaurando WinUtil nativo {tweak.Id}: {ex.Message}");
            ShowToast(
                "No se pudo deshacer este ajuste.",
                ActivityKind.Error);
        }
    }

    private static string GetWinUtilImpactText(string id) => id switch
    {
        "WPFToggleShowExt" =>
            "Hace visibles extensiones como .exe, .jpg o .txt en el Explorador de archivos. " +
            "Solo cambia una preferencia del usuario actual.",

        "WPFToggleHiddenFiles" =>
            "Permite ver archivos y carpetas marcados como ocultos. " +
            "No elimina ni modifica esos archivos.",

        "WPFTweaksEndTaskOnTaskbar" =>
            "Añade “Finalizar tarea” al menú del clic derecho de las aplicaciones en la barra de tareas.",

        "WPFToggleTaskbarSearch" =>
            "Muestra el acceso a Búsqueda en la barra de tareas. Puedes deshacerlo para recuperar tu estado anterior.",

        "WPFToggleDarkMode" =>
            "Activa el tema oscuro de Windows y de aplicaciones compatibles para el usuario actual.",

        _ =>
            "Cambia una preferencia del usuario actual y conserva una copia del valor anterior."
    };

    private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressThemeEvent) return;

        _theme.ApplyTheme(true);
        ShowToast("Tema oscuro activado.", ActivityKind.Info);
        Log("Tema oscuro activado.");
    }

    private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_suppressThemeEvent) return;

        _theme.ApplyTheme(false);
        ShowToast("Tema claro activado.", ActivityKind.Info);
        Log("Tema claro activado.");
    }

    private void BeginActivity(string title, string detail)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = title;
            LastActionText.Text = detail;
            ActivityProgress.Visibility = Visibility.Visible;
            ActivityDot.SetResourceReference(Border.BackgroundProperty, "WarningBrush");
        });
    }

    private void EndActivity(string title, string detail, ActivityKind kind)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = title;
            LastActionText.Text = detail;
            ActivityProgress.Visibility = Visibility.Collapsed;
            ActivityDot.SetResourceReference(
                Border.BackgroundProperty,
                ResourceFor(kind));
        });
    }

    private void ShowToast(string message, ActivityKind kind)
    {
        Dispatcher.Invoke(() =>
        {
            ToastText.Text = message;
            ToastIcon.Text = kind switch
            {
                ActivityKind.Success => "✓",
                ActivityKind.Warning => "!",
                ActivityKind.Error => "×",
                _ => "●"
            };

            ToastBorder.SetResourceReference(
                Border.BackgroundProperty,
                ResourceFor(kind));

            ToastBorder.Visibility = Visibility.Visible;

            _toastTimer.Stop();
            _toastTimer.Start();
        });
    }

    private static string ResourceFor(ActivityKind kind) => kind switch
    {
        ActivityKind.Success => "SuccessBrush",
        ActivityKind.Warning => "WarningBrush",
        ActivityKind.Error => "ErrorBrush",
        _ => "AccentBrush"
    };

    private void OpenWinUtilRepo_Click(object sender, RoutedEventArgs e) =>
        OpenShell(WinUtilCatalogService.RepositoryUrl);

    private void OpenStartupSettings_Click(object sender, RoutedEventArgs e) =>
        OpenShell("ms-settings:startupapps");

    private void OpenTaskManager_Click(object sender, RoutedEventArgs e) =>
        OpenShell("taskmgr.exe");

    private void OpenServices_Click(object sender, RoutedEventArgs e) =>
        OpenShell("services.msc");

    private static void OpenShell(string target)
    {
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
        ShowToast("Registro limpiado.", ActivityKind.Info);
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        });
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private enum ActivityKind
    {
        Info,
        Success,
        Warning,
        Error
    }
}
