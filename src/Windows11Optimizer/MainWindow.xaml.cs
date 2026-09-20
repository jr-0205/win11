using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
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
    private readonly VirtualizationModeService _virtualization = new();
    private readonly ThemeService _theme = new();
    private readonly OptimizationService _optimizer;
    private readonly DiagnosticReportService _diagnostics;
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

        BeginActivity("Inicializando", "Leyendo servicios, inicio y métricas del sistema…");

        AdminText.Text = $"Administrador: {(IsAdministrator() ? "Sí" : "No")}";
        RefreshBackupStatus();
        RefreshStartup();
        await RefreshServicesAsync();
        await RefreshVirtualizationAsync();
        await RefreshMetricsAsync();

        _timer.Start();

        Log("Aplicación iniciada. Perfil seguro cargado.");
        Log($"WinUtil disponible como catálogo de solo lectura: {WinUtilCatalogService.Version} / {WinUtilCatalogService.Commit[..12]}.");

        EndActivity(
            "Listo",
            "Sistema analizado. Esperando una acción.",
            ActivityKind.Success);

        ShowToast(
            $"Windows 11 Optimizer listo · Tema {(_theme.IsDark ? "oscuro" : "claro")}",
            ActivityKind.Success);
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
        await Task.Run(() =>
        {
            var rows = new List<ManagedService>();

            foreach (var name in SafeProfile.VmwareServices)
            {
                var info = _serviceManager.GetInfo(name, "VMware", "Bajo demanda");
                if (info is not null) rows.Add(info);
            }

            foreach (var name in SafeProfile.AcerOnDemandServices)
            {
                var info = _serviceManager.GetInfo(name, "Acer", "Bajo demanda");
                if (info is not null) rows.Add(info);
            }

            foreach (var name in SafeProfile.AcerProtectedServices)
            {
                var info = _serviceManager.GetInfo(name, "Acer", "Protegido / conservar");
                if (info is not null) rows.Add(info);
            }

            Dispatcher.Invoke(() => ServicesGrid.ItemsSource = rows);
        });
    }

    private void RefreshStartup()
    {
        StartupGrid.ItemsSource = _startup.GetEntries();
    }

    private void RefreshBackupStatus()
    {
        BackupText.Text = _backup.Exists
            ? $"Backup: {_backup.FilePath}"
            : "Backup: aún no creado";
    }

    private async void OptimizeSafe_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "Se creará una copia de seguridad y se deshabilitarán únicamente los servicios incluidos en el perfil seguro.\n\n¿Aplicar ahora?",
            "Optimización segura",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Optimización cancelada. No se realizó ningún cambio.", ActivityKind.Info);
            return;
        }

        await RunOperationAsync(
            "Aplicando optimización segura",
            "Deteniendo servicios bajo demanda y actualizando tareas…",
            () => _optimizer.OptimizeSafeAsync(Log));

        RefreshBackupStatus();
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

            VirtualizationBootModeText.Text =
                state.HypervisorLaunchType.Equals("Off", StringComparison.OrdinalIgnoreCase)
                    ? "VMware directo · OFF"
                    : "Normal · AUTO";

            VirtualizationCurrentText.Text =
                state.HypervisorPresentNow
                    ? "Activo en esta sesión"
                    : "No detectado en esta sesión";

            VirtualizationBackupText.Text =
                _virtualization.BackupExists
                    ? "Disponible"
                    : "Aún no creado";
        }
        catch (Exception ex)
        {
            VirtualizationBootModeText.Text = "No disponible";
            VirtualizationCurrentText.Text = "No disponible";
            VirtualizationBackupText.Text =
                _virtualization.BackupExists ? "Disponible" : "Aún no creado";

            Log($"Error leyendo virtualización: {ex.Message}");
        }
    }

    private async void SetVirtualizationNormal_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "Se configurará hypervisorlaunchtype=auto para el próximo arranque.\n\n" +
            "El cambio no se aplicará hasta reiniciar Windows. ¿Continuar?",
            "Modo Normal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Cambio de virtualización cancelado.", ActivityKind.Info);
            return;
        }

        await RunVirtualizationOperationAsync(
            "Configurando modo Normal",
            "Guardando el valor original y estableciendo hypervisorlaunchtype=auto…",
            () => _virtualization.SetNormalAsync());
    }

    private async void SetVirtualizationVmware_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "Se configurará hypervisorlaunchtype=off para el próximo arranque.\n\n" +
            "Mientras esté en OFF, Hyper-V y otras funciones que dependan del hipervisor " +
            "de Windows pueden no estar disponibles.\n\n¿Continuar?",
            "Modo VMware directo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            ShowToast("Cambio de virtualización cancelado.", ActivityKind.Info);
            return;
        }

        await RunVirtualizationOperationAsync(
            "Configurando modo VMware directo",
            "Guardando el valor original y estableciendo hypervisorlaunchtype=off…",
            () => _virtualization.SetVmwareDirectAsync());
    }

    private async void RestoreVirtualization_Click(object sender, RoutedEventArgs e)
    {
        if (!_virtualization.BackupExists)
        {
            ShowToast(
                "No existe todavía un backup de la configuración de virtualización.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "Se restaurará el valor de hypervisorlaunchtype que existía antes del primer cambio realizado por la app.\n\n¿Continuar?",
            "Restaurar virtualización",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        await RunVirtualizationOperationAsync(
            "Restaurando virtualización",
            "Restaurando el valor original guardado…",
            () => _virtualization.RestoreAsync());
    }

    private async void RefreshVirtualization_Click(object sender, RoutedEventArgs e)
    {
        BeginActivity(
            "Actualizando virtualización",
            "Consultando BCD y el estado actual del hipervisor…");

        try
        {
            await RefreshVirtualizationAsync();

            EndActivity(
                "Virtualización actualizada",
                "Se volvió a consultar la configuración de arranque.",
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
        var answer = MessageBox.Show(
            "Windows se reiniciará inmediatamente. Guarda cualquier trabajo abierto antes de continuar.\n\n¿Reiniciar ahora?",
            "Reiniciar Windows",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
            return;

        Log("Reinicio solicitado para aplicar la configuración de virtualización.");
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
        Func<Task> operation)
    {
        BeginActivity(title, detail);

        try
        {
            Log(title + "…");
            await operation();
            await RefreshVirtualizationAsync();

            EndActivity(
                title + ": preparado",
                "El cambio se aplicará después de reiniciar Windows.",
                ActivityKind.Success);

            ShowToast(
                "Configuración actualizada. Reinicia Windows para aplicarla.",
                ActivityKind.Success);

            Log(title + ": configuración guardada. Reinicio pendiente.");
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

    private async void LoadWinUtil_Click(object sender, RoutedEventArgs e) =>
        await LoadWinUtilCatalogAsync(forceRefresh: false);

    private async void RefreshWinUtil_Click(object sender, RoutedEventArgs e) =>
        await LoadWinUtilCatalogAsync(forceRefresh: true);

    private async Task LoadWinUtilCatalogAsync(bool forceRefresh)
    {
        BeginActivity(
            forceRefresh ? "Actualizando catálogo WinUtil" : "Cargando catálogo WinUtil",
            "Leyendo únicamente JSON desde la versión fijada…");

        try
        {
            WinUtilStatusText.Text = forceRefresh
                ? "Actualizando catálogo fijado…"
                : "Cargando catálogo fijado…";

            var result = await _winUtil.LoadAsync(forceRefresh);
            WinUtilGrid.ItemsSource = result.Tweaks;

            var source = result.FromCache ? "caché local" : "GitHub oficial";
            WinUtilStatusText.Text =
                $"WinUtil {result.Version} · {result.Tweaks.Count} tweaks · {source}";

            Log(
                $"WinUtil catálogo cargado: {result.Version}, commit {result.Commit[..12]}, " +
                $"{result.Tweaks.Count} tweaks, fuente={source}. Ningún tweak fue ejecutado.");

            EndActivity(
                "Catálogo WinUtil listo",
                $"{result.Tweaks.Count} tweaks cargados desde {source}.",
                ActivityKind.Success);

            ShowToast(
                $"WinUtil: {result.Tweaks.Count} tweaks cargados sin ejecutar código.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            WinUtilStatusText.Text = "Error cargando WinUtil";
            Log($"Error cargando catálogo WinUtil: {ex.Message}");

            EndActivity(
                "Error cargando WinUtil",
                ex.Message,
                ActivityKind.Error);

            ShowToast(
                "WinUtil no pudo cargarse. No se ejecutó ningún código remoto.",
                ActivityKind.Error);

            MessageBox.Show(
                "No se pudo cargar el catálogo de WinUtil. La app no ejecutó ningún código remoto.\n\n" +
                ex.Message,
                "WinUtil",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

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
