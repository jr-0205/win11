using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
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
    private readonly OptimizationService _optimizer;
    private readonly DiagnosticReportService _diagnostics;
    private readonly DispatcherTimer _timer;
    private bool _refreshingMetrics;

    public MainWindow()
    {
        InitializeComponent();

        _optimizer = new OptimizationService(_serviceManager, _taskManager, _backup);
        _diagnostics = new DiagnosticReportService(_metrics, _serviceManager, _startup);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await RefreshMetricsAsync();

        Loaded += MainWindow_Loaded;
        Closed += (_, _) => _timer.Stop();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AdminText.Text = $"Administrador: {(IsAdministrator() ? "Sí" : "No")}";
        RefreshBackupStatus();
        RefreshStartup();
        await RefreshServicesAsync();
        await RefreshMetricsAsync();
        _timer.Start();
        Log("Aplicación iniciada. Perfil seguro cargado.");
        Log($"WinUtil disponible como catálogo de solo lectura: {WinUtilCatalogService.Version} / {WinUtilCatalogService.Commit[..12]}.");
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

        if (answer != MessageBoxResult.Yes) return;

        await RunOperationAsync("Aplicando perfil seguro", () => _optimizer.OptimizeSafeAsync(Log));
        RefreshBackupStatus();
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (!_backup.Exists)
        {
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

        if (answer != MessageBoxResult.Yes) return;
        await RunOperationAsync("Restaurando configuración", () => _optimizer.RestoreAsync(Log));
    }

    private async void StartVmware_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync("Iniciando VMware", () => _optimizer.StartVmwareAsync(Log));

    private async void StopVmware_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync("Deteniendo VMware", () => _optimizer.StopVmwareAsync(Log));

    private async void StartAcer_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync("Iniciando utilidades Acer", () => _optimizer.StartAcerAsync(Log));

    private async void StopAcer_Click(object sender, RoutedEventArgs e) =>
        await RunOperationAsync("Deteniendo utilidades Acer", () => _optimizer.StopAcerAsync(Log));

    private async void RefreshServices_Click(object sender, RoutedEventArgs e) =>
        await RefreshServicesAsync();

    private async Task RunOperationAsync(string title, Func<Task> operation)
    {
        try
        {
            Log(title + "…");
            await operation();
            await RefreshServicesAsync();
            await RefreshMetricsAsync();
            Log(title + ": terminado.");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportDiagnostic_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = await _diagnostics.ExportToDesktopAsync();
            Log($"Diagnóstico exportado: {path}");
            MessageBox.Show(
                $"Reporte generado en:\n{path}",
                "Diagnóstico",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"Error exportando diagnóstico: {ex.Message}");
        }
    }

    private async void LoadWinUtil_Click(object sender, RoutedEventArgs e) =>
        await LoadWinUtilCatalogAsync(forceRefresh: false);

    private async void RefreshWinUtil_Click(object sender, RoutedEventArgs e) =>
        await LoadWinUtilCatalogAsync(forceRefresh: true);

    private async Task LoadWinUtilCatalogAsync(bool forceRefresh)
    {
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
        }
        catch (Exception ex)
        {
            WinUtilStatusText.Text = "Error cargando WinUtil";
            Log($"Error cargando catálogo WinUtil: {ex.Message}");
            MessageBox.Show(
                "No se pudo cargar el catálogo de WinUtil. La app no ejecutó ningún código remoto.\n\n" +
                ex.Message,
                "WinUtil",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

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

    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogBox.Clear();

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
}
