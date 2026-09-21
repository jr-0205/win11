using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Windows11Optimizer.Core;
using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;
using Windows11Optimizer.Services;
using Wpf.Ui;
using ControlAppearance = Wpf.Ui.Controls.ControlAppearance;
using AppThemeService = Windows11Optimizer.Services.ThemeService;

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
    private readonly AppThemeService _theme = new();
    private readonly InstalledAppService _installedApps = new();
    private readonly OpenAiAnalysisService _openAi = new();
    private readonly AgentIntegrationService _agent = new();
    private readonly FocusBoostService _focusBoost = new();
    private readonly OptimizationService _optimizer;
    private readonly SafeActionEngine _safeActions;
    private readonly SystemAssessmentService _systemAssessment;
    private readonly DiagnosticReportService _diagnostics;
    private readonly SmartAnalysisService _smartAnalysis;
    private readonly SmartOptimizationService _smartOptimizer;
    private SmartOptimizationPlan? _lastOptimizationPlan;
    private IReadOnlyList<WinUtilTweak> _winUtilAllTweaks = Array.Empty<WinUtilTweak>();
    private AutorunsAnalysisResult? _lastAutorunsAnalysis;
    private readonly DispatcherTimer _timer;
    private readonly SnackbarService _snackbar = new();
    private bool _refreshingMetrics;
    private bool _suppressThemeEvent;

    public MainWindow()
    {
        InitializeComponent();
        _snackbar.SetSnackbarPresenter(SnackbarPresenter);
        _theme.ApplySavedTheme();

        _optimizer = new OptimizationService(_serviceManager, _taskManager, _backup);
        _safeActions = new SafeActionEngine(_serviceManager, _winUtilNative);
        _systemAssessment = new SystemAssessmentService(
            _metrics,
            _startup,
            _installedApps,
            _safeActions);
        _diagnostics = new DiagnosticReportService(_metrics, _serviceManager, _startup);
        _smartAnalysis = new SmartAnalysisService(_metrics, _startup, _autoruns);
        _smartOptimizer = new SmartOptimizationService(
            _serviceManager,
            _taskManager,
            _startup);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await RefreshMetricsAsync();

        Loaded += MainWindow_Loaded;
        Closed += (_, _) =>
        {
            _timer.Stop();
            _focusBoost.Dispose();
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
        await RefreshInstalledAppsAsync();
        RefreshFocusProcesses();
        RefreshFocusStatus();
        UpdateAiAvailability();

        if (_agent.IsAvailable)
        {
            try
            {
                // Inicia únicamente el agente ligero. Él mismo respeta
                // la preferencia StartWithWindows guardada por el usuario.
                _agent.StartAgent();
            }
            catch (Exception ex)
            {
                Log($"No se pudo iniciar el agente ligero: {ex.Message}");
            }
        }

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

    private void NavigationButton_Checked(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not RadioButton button ||
            button.Tag is not string rawIndex ||
            !int.TryParse(rawIndex, out var index))
        {
            return;
        }

        if (MainTabs is null)
            return;

        MainTabs.SelectedIndex = index;
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
        NavigationColumn.Width = compact
            ? new GridLength(148)
            : new GridLength(188);
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

    private void UpdateAiAvailability()
    {
        AiExamStatusText.Text = _openAi.IsConfigured
            ? $"IA opcional configurada · modelo {_openAi.Model}. La disponibilidad también depende del saldo de la API."
            : "IA opcional no configurada. El examen local funciona sin clave, Internet ni saldo de API.";
    }

    private async void RunAiExam_Click(object sender, RoutedEventArgs e)
    {
        if (!_openAi.IsConfigured)
        {
            await RunLocalExamFallbackAsync(
                "La IA opcional no está configurada. " +
                "Se ejecutó el examen local seguro en su lugar.");

            ShowToast(
                "Examen local completado. Puedes configurar la IA más adelante si quieres.",
                ActivityKind.Info);

            return;
        }

        BeginActivity(
            "Examen con IA",
            "Recopilando un resumen limitado del sistema y consultando el catálogo seguro…");

        try
        {
            var snapshot = await _systemAssessment.CaptureAsync();
            var result = await _openAi.AnalyzeAsync(snapshot);

            var rows = result.Recommendations
                .Select(recommendation =>
                {
                    var action = SafeActionCatalog.Find(recommendation.ActionId);
                    return action is null
                        ? null
                        : new AiRecommendationRow
                        {
                            ActionId = action.Id,
                            Title = action.Title,
                            Category = action.Category,
                            Reason = recommendation.Reason,
                            Confidence = recommendation.Confidence,
                            Impact = action.Impact,
                            Risk = action.Risk,
                            Revert = action.Revert,
                            CanApply = action.CanApply
                        };
                })
                .Where(x => x is not null)
                .Cast<AiRecommendationRow>()
                .ToList();

            AiRecommendationGrid.ItemsSource = rows;
            AiExamStatusText.Text =
                string.IsNullOrWhiteSpace(result.Summary)
                    ? $"Examen terminado: {rows.Count} recomendaciones válidas."
                    : result.Summary;

            AiRecommendationGrid.SelectedIndex =
                rows.Count > 0 ? 0 : -1;

            EndActivity(
                "Examen con IA completado",
                $"{rows.Count} recomendaciones válidas del catálogo local.",
                ActivityKind.Success);

            ShowToast(
                "La IA terminó de recomendar. Ningún cambio se aplicó automáticamente.",
                ActivityKind.Success);
        }
        catch (OpenAiApiException ex) when (ex.IsQuotaOrCreditsError)
        {
            Log(
                $"Examen con IA sin saldo disponible: HTTP {ex.StatusCode} | {ex.Message}");

            await RunLocalExamFallbackAsync(
                "La clave de OpenAI funciona, pero esta cuenta de API no tiene saldo disponible. " +
                "Se ejecutó el examen local seguro en su lugar.");

            ShowToast(
                "La IA no tiene saldo disponible. Usamos el examen local sin aplicar cambios.",
                ActivityKind.Warning);
        }
        catch (OpenAiApiException ex) when (ex.IsRateLimitError)
        {
            Log(
                $"Límite temporal de OpenAI: HTTP {ex.StatusCode} | {ex.Message}");

            await RunLocalExamFallbackAsync(
                "OpenAI está limitando temporalmente las solicitudes. " +
                "Se ejecutó el examen local seguro en su lugar.");

            ShowToast(
                "OpenAI está ocupado. Usamos el examen local por ahora.",
                ActivityKind.Warning);
        }
        catch (OpenAiApiException ex)
        {
            Log(
                $"Error de API en examen con IA: HTTP {ex.StatusCode} | {ex.Message}");

            AiExamStatusText.Text =
                "No se pudo usar la IA. El análisis local sigue disponible y no depende de la API.";

            EndActivity(
                "IA no disponible",
                ex.Message,
                ActivityKind.Warning);

            ShowToast(
                "La IA no está disponible. Puedes seguir usando el análisis local.",
                ActivityKind.Warning);
        }
        catch (Exception ex)
        {
            Log($"Error en examen con IA: {ex.Message}");

            AiExamStatusText.Text =
                "No se pudo completar el examen con IA. El análisis local sigue disponible.";

            EndActivity(
                "No se pudo completar el examen con IA",
                ex.Message,
                ActivityKind.Error);

            ShowToast(
                "No se pudo usar la IA. El resto de la app sigue funcionando.",
                ActivityKind.Error);
        }
    }

    private async Task RunLocalExamFallbackAsync(string reason)
    {
        var plan = await _smartOptimizer.AnalyzeAsync();

        _lastOptimizationPlan = plan;
        OptimizationPlanGrid.ItemsSource = plan.Opportunities;
        SmartOptimizationSummaryText.Text = plan.Summary;
        ApplySmartOptimizationButton.IsEnabled = plan.ApplicableCount > 0;

        AiRecommendationGrid.ItemsSource = Array.Empty<AiRecommendationRow>();
        AiRecommendationDetailText.Text =
            "El examen local no necesita API y solo usa reglas internas validadas.";

        AiExamStatusText.Text =
            reason + Environment.NewLine +
            "La IA es opcional; no se aplicó ningún cambio automáticamente.";

        EndActivity(
            "Examen local completado",
            plan.Summary,
            ActivityKind.Success);
    }

    private void AiRecommendationGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        UpdateAiRecommendationDetail();
    }

    private void UpdateAiRecommendationDetail()
    {
        if (AiRecommendationGrid.SelectedItem is not AiRecommendationRow row)
        {
            AiRecommendationDetailText.Text =
                "Selecciona una recomendación para ver impacto y cómo revertirla.";
            ApplyAiRecommendationButton.IsEnabled = false;
            RevertAiRecommendationButton.IsEnabled = false;
            return;
        }

        var state = _safeActions.GetState(row.ActionId);

        AiRecommendationDetailText.Text =
            $"Impacto: {row.Impact}{Environment.NewLine}" +
            $"Riesgo: {row.Risk} · Estado: {state}{Environment.NewLine}" +
            $"Cómo se deshace: {row.Revert}";

        ApplyAiRecommendationButton.IsEnabled =
            row.CanApply &&
            !string.Equals(
                state,
                "Aplicado",
                StringComparison.OrdinalIgnoreCase);

        RevertAiRecommendationButton.IsEnabled =
            _safeActions.HasBackup(row.ActionId);
    }

    private async void ApplyAiRecommendation_Click(object sender, RoutedEventArgs e)
    {
        if (AiRecommendationGrid.SelectedItem is not AiRecommendationRow row)
            return;

        if (!row.CanApply)
        {
            ShowToast(
                "Esta recomendación requiere revisión manual y no se ejecuta automáticamente.",
                ActivityKind.Info);
            return;
        }

        var answer = MessageBox.Show(
            $"{row.Title}{Environment.NewLine}{Environment.NewLine}" +
            $"Impacto: {row.Impact}{Environment.NewLine}" +
            $"Riesgo: {row.Risk}{Environment.NewLine}" +
            $"Deshacer: {row.Revert}{Environment.NewLine}{Environment.NewLine}" +
            "La IA no ejecutará nada; el cambio lo aplicará el motor local validado. ¿Continuar?",
            "Aplicar ajuste seguro",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            await _safeActions.ApplyAsync(row.ActionId);
            Log($"Ajuste seguro aplicado: {row.ActionId}");
            UpdateAiRecommendationDetail();
            RefreshStartup();
            ShowToast($"Aplicado: {row.Title}.", ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error aplicando {row.ActionId}: {ex.Message}");
            ShowToast(ex.Message, ActivityKind.Error);
        }
    }

    private async void RevertAiRecommendation_Click(object sender, RoutedEventArgs e)
    {
        if (AiRecommendationGrid.SelectedItem is not AiRecommendationRow row)
            return;

        try
        {
            await _safeActions.RevertAsync(row.ActionId);
            Log($"Ajuste seguro restaurado: {row.ActionId}");
            UpdateAiRecommendationDetail();
            ShowToast($"Deshecho: {row.Title}.", ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error restaurando {row.ActionId}: {ex.Message}");
            ShowToast(ex.Message, ActivityKind.Error);
        }
    }

    private void RefreshFocusProcesses()
    {
        FocusProcessGrid.ItemsSource = _focusBoost
            .GetCandidateProcesses()
            .Where(x => x.IsAllowedTarget)
            .ToList();

        RefreshFocusStatus();
    }

    private void RefreshFocusProcesses_Click(object sender, RoutedEventArgs e) =>
        RefreshFocusProcesses();

    private void RefreshFocusStatus()
    {
        try
        {
            FocusBoostStatusText.Text = _focusBoost.GetStatus().DisplayText;
        }
        catch
        {
            FocusBoostStatusText.Text = "No se pudo comprobar Focus Boost.";
        }
    }

    private void StartFocusBoost_Click(object sender, RoutedEventArgs e)
    {
        if (FocusProcessGrid.SelectedItem is not FocusProcessInfo selected)
        {
            ShowToast(
                "Selecciona primero un proceso activo.",
                ActivityKind.Warning);
            return;
        }

        try
        {
            if (_agent.IsAvailable)
                _agent.StartAgent();

            var status = _focusBoost.Start(selected.Pid);
            FocusBoostStatusText.Text = status.DisplayText;
            ShowToast(
                $"Focus Boost activo para {selected.Name}.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error iniciando Focus Boost: {ex.Message}");
            ShowToast(ex.Message, ActivityKind.Error);
        }
    }

    private void StopFocusBoost_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _focusBoost.Restore();
            RefreshFocusStatus();
            ShowToast(
                "Focus Boost detenido y prioridades restauradas.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, ActivityKind.Error);
        }
    }

    private void OpenMiniFocus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _agent.OpenMiniFocus();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, ActivityKind.Warning);
        }
    }

    private void OpenAgentSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _agent.OpenSettings();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, ActivityKind.Warning);
        }
    }

    private async Task RefreshInstalledAppsAsync()
    {
        var apps = await Task.Run(() => _installedApps.GetInstalledApps());
        InstalledAppsGrid.ItemsSource = apps;
        InstalledAppsSummaryText.Text =
            $"{apps.Count} aplicaciones registradas. La desinstalación usa únicamente el desinstalador publicado por Windows.";
    }

    private async void RefreshInstalledApps_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshInstalledAppsAsync();
            ShowToast("Aplicaciones actualizadas.", ActivityKind.Success);
        }
        catch (Exception ex)
        {
            ShowToast(
                $"No se pudo actualizar la lista: {ex.Message}",
                ActivityKind.Error);
        }
    }

    private void InstalledAppsGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (InstalledAppsGrid.SelectedItem is not InstalledApp app)
        {
            SelectedAppDetailText.Text =
                "Selecciona una aplicación para ver sus datos.";
            return;
        }

        SelectedAppDetailText.Text =
            $"{app.DisplayName} · {app.VersionDisplay}{Environment.NewLine}" +
            $"Editor: {app.PublisherDisplay}{Environment.NewLine}" +
            $"Desinstalador registrado: {(app.CanUninstall ? "sí" : "no")}";
        AppAiExplanationText.Text =
            "Pulsa “Explicar con IA” para obtener una explicación basada únicamente en nombre, versión y editor.";
    }

    private void UninstallSelectedApp_Click(object sender, RoutedEventArgs e)
    {
        if (InstalledAppsGrid.SelectedItem is not InstalledApp app)
        {
            ShowToast(
                "Selecciona primero una aplicación.",
                ActivityKind.Warning);
            return;
        }

        if (!app.CanUninstall)
        {
            ShowToast(
                "Esta aplicación no publica un desinstalador que podamos iniciar de forma segura.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            $"Se abrirá el desinstalador registrado por Windows para “{app.DisplayName}”.{Environment.NewLine}{Environment.NewLine}" +
            "Windows11Optimizer no borrará carpetas ni archivos por su cuenta. ¿Continuar?",
            "Desinstalar aplicación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            _installedApps.StartUninstall(app);
            Log($"Desinstalador iniciado: {app.DisplayName}");
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, ActivityKind.Error);
        }
    }

    private void OpenSelectedAppLocation_Click(object sender, RoutedEventArgs e)
    {
        if (InstalledAppsGrid.SelectedItem is not InstalledApp app)
            return;

        try
        {
            _installedApps.OpenInstallLocation(app);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, ActivityKind.Warning);
        }
    }

    private async void ExplainSelectedAppWithAi_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (InstalledAppsGrid.SelectedItem is not InstalledApp app)
        {
            ShowToast(
                "Selecciona primero una aplicación.",
                ActivityKind.Warning);
            return;
        }

        if (!_openAi.IsConfigured)
        {
            ShowToast(
                "Configura OPENAI_API_KEY para usar explicaciones con IA.",
                ActivityKind.Warning);
            return;
        }

        AppAiExplanationText.Text = "Consultando…";

        try
        {
            var explanation =
                await _openAi.ExplainApplicationAsync(app);

            AppAiExplanationText.Text = explanation.DisplayText;
        }
        catch (OpenAiApiException ex) when (ex.IsQuotaOrCreditsError)
        {
            AppAiExplanationText.Text =
                "La clave de OpenAI funciona, pero la cuenta de API no tiene saldo disponible." +
                Environment.NewLine + Environment.NewLine +
                "Datos locales disponibles:" + Environment.NewLine +
                $"• Aplicación: {app.DisplayName}" + Environment.NewLine +
                $"• Versión: {app.VersionDisplay}" + Environment.NewLine +
                $"• Editor: {app.PublisherDisplay}" + Environment.NewLine +
                "No se realizó ningún cambio.";
        }
        catch (OpenAiApiException ex) when (ex.IsRateLimitError)
        {
            AppAiExplanationText.Text =
                "OpenAI está limitando temporalmente las solicitudes. " +
                "Puedes volver a intentarlo más tarde. Ningún cambio fue realizado.";
        }
        catch (OpenAiApiException)
        {
            AppAiExplanationText.Text =
                "La explicación con IA no está disponible ahora. " +
                "Puedes seguir administrando esta aplicación con las funciones locales.";
        }
        catch (Exception ex)
        {
            AppAiExplanationText.Text =
                "No se pudo obtener la explicación con IA. " +
                "Las funciones locales siguen disponibles." +
                Environment.NewLine + Environment.NewLine +
                ex.Message;
        }
    }

    private void OpenAppsSettings_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("ms-settings:appsfeatures")
        {
            UseShellExecute = true
        });
    }

    private void RemoveSelectedResidue_Click(object sender, RoutedEventArgs e)
    {
        if (ResidueGrid.SelectedItem is not StartupEntry entry ||
            !entry.CanRemoveSafely)
        {
            ShowToast(
                "Selecciona una referencia huérfana confirmada.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            $"Se quitará únicamente la referencia de inicio “{entry.Name}”.{Environment.NewLine}" +
            "No se borrará ningún programa ni carpeta y se guardará una copia para restaurar. ¿Continuar?",
            "Quitar residuo confirmado",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            _startup.RemoveOrphanedEntry(entry);
            RefreshStartup();
            ShowToast(
                "Referencia huérfana eliminada de forma reversible.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, ActivityKind.Error);
        }
    }

    private void RestoreResidue_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var restored = _startup.RestoreLastRemoved();
            RefreshStartup();
            ShowToast(
                $"Referencia restaurada: {restored.Name}.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, ActivityKind.Warning);
        }
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
            RefreshFocusStatus();
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

        var residues = entries
            .Where(x => x.IsOrphaned && x.CanRemoveSafely)
            .ToList();

        ResidueGrid.ItemsSource = residues;
        ResidueSummaryText.Text = residues.Count switch
        {
            0 => "No encontramos referencias huérfanas confirmadas que puedan borrarse automáticamente.",
            1 => "Encontramos 1 referencia huérfana confirmada. El borrado usa el mismo motor seguro y reversible de Inicio de Windows.",
            _ => $"Encontramos {residues.Count} referencias huérfanas confirmadas. Solo estas pueden borrarse automáticamente."
        };
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

    private void DisableStartupEntry_Click(object sender, RoutedEventArgs e)
    {
        if (StartupGrid.SelectedItem is not StartupEntry entry)
        {
            ShowToast(
                "Selecciona primero una entrada de inicio.",
                ActivityKind.Warning);
            return;
        }

        if (!entry.CanDisableAtStartup)
        {
            ShowToast(
                "Esta entrada no puede administrarse de forma reversible desde la app. Usa la configuración de Inicio de Windows.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            $"“{entry.Name}” dejará de abrirse automáticamente con Windows.{Environment.NewLine}{Environment.NewLine}" +
            "El programa seguirá instalado y podrás restaurar esta entrada desde la misma pantalla. ¿Continuar?",
            "No iniciar con Windows",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            _startup.DisableStartupEntry(entry);
            RefreshStartup();

            Log($"Inicio desactivado de forma reversible: {entry.Name} | {entry.Source}");

            ShowToast(
                $"'{entry.Name}' ya no iniciará automáticamente.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error desactivando entrada de inicio: {ex.Message}");
            ShowToast(
                "No se pudo cambiar esta entrada de inicio.",
                ActivityKind.Error);
        }
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
            "Se restaurará el último cambio de Inicio de Windows realizado por Windows11Optimizer.\n\n¿Continuar?",
            "Restaurar entrada de inicio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            var restored = _startup.RestoreLastRemoved();
            RefreshStartup();

            Log(
                $"Entrada de inicio restaurada: {restored.Name} | motivo={restored.Reason}");

            ShowToast(
                restored.Reason == "DisabledByUser"
                    ? $"'{restored.Name}' volverá a iniciar con Windows."
                    : $"Se restauró '{restored.Name}'.",
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

    private async void AnalyzeOptimization_Click(
        object sender,
        RoutedEventArgs e)
    {
        BeginActivity(
            "Analizando optimización",
            "Buscando componentes que pueden quedar disponibles solo cuando se necesiten…");

        try
        {
            var plan = await _smartOptimizer.AnalyzeAsync();
            _lastOptimizationPlan = plan;

            OptimizationPlanGrid.ItemsSource = plan.Opportunities;
            SmartOptimizationSummaryText.Text = plan.Summary;
            ApplySmartOptimizationButton.IsEnabled = plan.ApplicableCount > 0;

            EndActivity(
                "Análisis listo",
                plan.Summary,
                ActivityKind.Success);

            ShowToast(
                plan.ApplicableCount > 0
                    ? "Hay recomendaciones listas para aplicar."
                    : "No encontramos cambios necesarios.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error analizando optimización: {ex.Message}");
            EndActivity(
                "No se pudo analizar la optimización",
                ex.Message,
                ActivityKind.Error);
            ShowToast(
                "No se pudo completar el análisis.",
                ActivityKind.Error);
        }
    }

    private async void ApplySmartOptimization_Click(
        object sender,
        RoutedEventArgs e)
    {
        var plan = _lastOptimizationPlan ?? await _smartOptimizer.AnalyzeAsync();

        if (plan.ApplicableCount == 0)
        {
            ShowToast(
                "Los componentes administrados ya están en la configuración recomendada.",
                ActivityKind.Success);
            return;
        }

        var answer = MessageBox.Show(
            "La app dejará los componentes aprobados disponibles para que se inicien cuando una aplicación los necesite.\n\n" +
            "No se deshabilitarán funciones ni actividades programadas. Se guardará una copia del estado anterior. ¿Continuar?",
            "Aplicar optimización",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            var before = await _smartAnalysis.CaptureAsync(includeAutoruns: false);

            await RunOperationAsync(
                "Aplicando optimización inteligente",
                "Dejando componentes aprobados disponibles solo cuando se necesiten…",
                () => _optimizer.OptimizeSafeAsync(Log));

            var after = await _smartAnalysis.CaptureAsync(includeAutoruns: false);
            var refreshedPlan = await _smartOptimizer.AnalyzeAsync();

            _lastOptimizationPlan = refreshedPlan;
            OptimizationPlanGrid.ItemsSource = refreshedPlan.Opportunities;
            SmartOptimizationSummaryText.Text = refreshedPlan.Summary;
            ApplySmartOptimizationButton.IsEnabled =
                refreshedPlan.ApplicableCount > 0;

            OptimizationResultText.Text =
                $"Configuración aplicada. Procesos actuales: {before.ProcessCount} → {after.ProcessCount}. " +
                "El principal beneficio se notará en próximos inicios de Windows, porque la app no fuerza el cierre de componentes que ya están funcionando.";

            RefreshBackupStatus();
            RefreshStartup();
        }
        catch (Exception ex)
        {
            Log($"Error aplicando optimización inteligente: {ex.Message}");
            OptimizationResultText.Text =
                "No se pudo completar la optimización. No se deshabilitaron componentes.";
            ShowToast(
                "No se pudo completar la optimización.",
                ActivityKind.Error);
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (!_backup.Exists)
        {
            ShowToast(
                "No existe todavía una copia de seguridad para restaurar.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "Se restaurará la forma en que estos componentes iniciaban antes de la primera optimización. ¿Continuar?",
            "Deshacer optimización",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        await RunOperationAsync(
            "Deshaciendo optimización",
            "Restaurando la configuración guardada…",
            () => _optimizer.RestoreAsync(Log));

        await RefreshServicesAsync();
    }

    private IReadOnlyList<string> GetDisabledWindowsVirtualizationServices()
    {
        var disabled = new List<string>();

        foreach (var name in SafeProfile.WindowsVirtualizationRepairServices)
        {
            var info = _serviceManager.GetInfo(name, "", "");
            if (info is null)
                continue;

            if (string.Equals(
                    info.StartMode,
                    "Disabled",
                    StringComparison.OrdinalIgnoreCase))
            {
                disabled.Add(info.DisplayName);
            }
        }

        return disabled;
    }

    private IReadOnlyList<string> GetDisabledVmwareCoreServices()
    {
        var disabled = new List<string>();

        foreach (var name in SafeProfile.VmwareCoreServices)
        {
            var info = _serviceManager.GetInfo(name, "", "");
            if (info is null)
                continue;

            if (string.Equals(
                    info.StartMode,
                    "Disabled",
                    StringComparison.OrdinalIgnoreCase))
            {
                disabled.Add(info.DisplayName);
            }
        }

        return disabled;
    }

    private async Task RunSecondaryModePreparationAsync(
        string modeName,
        Func<Task> preparation)
    {
        try
        {
            await preparation();
        }
        catch (Exception ex)
        {
            // El núcleo del modo (auto/off) ya fue aplicado antes.
            // Una preparación secundaria nunca debe deshacer ni ocultar ese cambio.
            Log(
                $"{modeName}: el modo base quedó configurado, " +
                $"pero la preparación secundaria encontró un problema: {ex.Message}");
        }
    }

    private async Task RefreshVirtualizationAsync()
    {
        try
        {
            var state = await _virtualization.GetStateAsync();

            VirtualizationBootModeText.Text = state.IsConfiguredForVmware
                ? "VMware"
                : "Windows y Docker";

            VirtualizationCurrentText.Text = state.CurrentHypervisorLabel;
            VirtualizationVbsText.Text = state.VbsLabel;
            VirtualizationMemoryIntegrityText.Text = state.MemoryIntegrityLabel;
            VirtualizationRestartText.Text = state.RestartLabel;

            VirtualizationBackupText.Text =
                _virtualization.BackupExists
                    ? "Copia de seguridad disponible"
                    : "Aún no se ha creado una copia";

            RestartForVirtualizationButton.IsEnabled = state.PendingRestart;
            UseNormalVirtualizationButton.IsEnabled = true;
            UseVmwareVirtualizationButton.IsEnabled = true;

            RefreshVmwareServiceProfile();
            var disabledWindowsVirtualization =
                GetDisabledWindowsVirtualizationServices();

            if (state.IsConfiguredForNormal &&
                disabledWindowsVirtualization.Count > 0)
            {
                VirtualizationHelpText.Text =
                    "Windows y Docker está seleccionado, pero encontramos componentes de WSL/virtualización deshabilitados: " +
                    string.Join(", ", disabledWindowsVirtualization) +
                    ". Pulsa “Usar Windows y Docker” para repararlos.";

                VirtualizationRestartHintText.Text = state.PendingRestart
                    ? "También hay un reinicio pendiente para terminar de aplicar el modo."
                    : "No reinicies todavía: primero deja que la app repare esos componentes.";

                return;
            }

            if (state.PendingRestart)
            {
                VirtualizationHelpText.Text =
                    "El modo elegido ya está preparado, pero todavía falta reiniciar para que Windows lo use.";
                VirtualizationRestartHintText.Text =
                    "Reinicia cuando te convenga para aplicar el modo elegido.";
            }
            else if (state.IsConfiguredForVmware && !state.HypervisorPresentNow)
            {
                VirtualizationHelpText.Text =
                    "VMware está priorizado para esta sesión. Las funciones de Windows que necesitan su propio modo de virtualización volverán a estar disponibles cuando regreses a Windows y Docker.";
                VirtualizationRestartHintText.Text =
                    "VMware ya está listo. No necesitas reiniciar.";
            }
            else
            {
                VirtualizationHelpText.Text =
                    "Windows y Docker están priorizados. Docker Desktop, WSL2, Windows Sandbox y las funciones de virtualización de Windows pueden usar el entorno normal del sistema.";
                VirtualizationRestartHintText.Text =
                    "Windows y Docker ya están listos. No necesitas reiniciar.";
            }
        }
        catch (Exception ex)
        {
            VirtualizationBootModeText.Text = "No disponible";
            VirtualizationCurrentText.Text = "No disponible";
            VirtualizationVbsText.Text = "No disponible";
            VirtualizationMemoryIntegrityText.Text = "No disponible";
            VirtualizationRestartText.Text = "No disponible";
            VirtualizationBackupText.Text = "No disponible";
            VirtualizationHelpText.Text =
                "No se pudo comprobar el modo de virtualización.";
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
        var installed = 0;
        var running = 0;

        foreach (var name in SafeProfile.VmwareCoreServices)
        {
            var info = _serviceManager.GetInfo(name, "", "");
            if (info is null)
                continue;

            installed++;

            if (string.Equals(
                    info.State,
                    "Running",
                    StringComparison.OrdinalIgnoreCase))
            {
                running++;
            }
        }

        VmwareServiceProfileText.Text = installed switch
        {
            0 => "No encontramos los componentes principales de VMware instalados.",
            _ when running == installed =>
                "VMware está preparado y sus componentes principales están disponibles ahora.",
            _ =>
                "VMware está instalado. Al elegir este modo, la app preparará automáticamente sus componentes principales."
        };
    }

    private async void SetVirtualizationNormal_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RunVirtualizationOperationAsync(
            "Preparando Windows y Docker",
            "Aplicando primero el modo original de virtualización y después comprobando WSL/Docker…",
            async () =>
            {
                // Núcleo heredado de VMwareMode.ps1: primero BCD -> auto.
                var requiresRestart =
                    await _virtualization.SetNormalAsync();

                // Capa secundaria: solo repara componentes conocidos si hace falta.
                await RunSecondaryModePreparationAsync(
                    "Windows y Docker",
                    () => _optimizer.PrepareWindowsVirtualizationAsync(Log));

                return requiresRestart;
            });
    }

    private async void SetVirtualizationVmware_Click(
        object sender,
        RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "Este modo prioriza VMware y las máquinas virtuales. Puede requerir reiniciar Windows.\n\n" +
            "Docker Desktop, Windows Sandbox y otras funciones que usan el modo de virtualización de Windows pueden necesitar que vuelvas a “Windows y Docker”.\n\n" +
            "¿Continuar?",
            "Usar VMware",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        await RunVirtualizationOperationAsync(
            "Preparando VMware",
            "Aplicando primero el modo original de VMware y después preparando sus componentes…",
            async () =>
            {
                // Núcleo heredado de VMwareMode.ps1: primero BCD -> off.
                var requiresRestart =
                    await _virtualization.SetVmwareDirectAsync();

                // Capa secundaria: prepara VMware sin alterar el modo BCD.
                await RunSecondaryModePreparationAsync(
                    "VMware",
                    () => _optimizer.PrepareVmwareAsync(Log));

                return requiresRestart;
            });
    }

    private async void RestoreVirtualization_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_virtualization.BackupExists)
        {
            ShowToast(
                "Todavía no existe una configuración anterior para restaurar.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "Se restaurará el modo que tenía Windows antes del primer cambio realizado por la app. ¿Continuar?",
            "Volver al estado original",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        await RunVirtualizationOperationAsync(
            "Restaurando modo anterior",
            "Restaurando la configuración guardada…",
            () => _virtualization.RestoreAsync());
    }

    private async void RefreshVirtualization_Click(
        object sender,
        RoutedEventArgs e)
    {
        BeginActivity(
            "Actualizando estado",
            "Comprobando el modo de virtualización actual…");

        try
        {
            await RefreshVirtualizationAsync();

            EndActivity(
                "Estado actualizado",
                "La información mostrada corresponde al estado actual.",
                ActivityKind.Success);

            ShowToast(
                "Estado actualizado.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            EndActivity(
                "No se pudo actualizar",
                ex.Message,
                ActivityKind.Error);

            ShowToast(
                "No se pudo actualizar el estado.",
                ActivityKind.Error);
        }
    }

    private async void RestartForVirtualization_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var state = await _virtualization.GetStateAsync();

            if (!state.PendingRestart)
            {
                RestartForVirtualizationButton.IsEnabled = false;
                ShowToast(
                    "No hay cambios pendientes que necesiten reinicio.",
                    ActivityKind.Success);
                await RefreshVirtualizationAsync();
                return;
            }
        }
        catch (Exception ex)
        {
            Log($"No se pudo comprobar el reinicio pendiente: {ex.Message}");
            ShowToast(
                "No se pudo comprobar si el reinicio es necesario.",
                ActivityKind.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "Windows se reiniciará inmediatamente para aplicar el modo elegido.\n\n" +
            "Guarda cualquier trabajo abierto antes de continuar. ¿Reiniciar ahora?",
            "Reiniciar para aplicar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            await VirtualizationModeService.RestartWindowsAsync();
        }
        catch (Exception ex)
        {
            Log($"Error solicitando reinicio: {ex.Message}");
            ShowToast(
                "Windows no aceptó el reinicio.",
                ActivityKind.Error);
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

            var stateAfter = await _virtualization.GetStateAsync();

            IReadOnlyList<string> readinessIssues =
                stateAfter.IsConfiguredForNormal
                    ? GetDisabledWindowsVirtualizationServices()
                    : GetDisabledVmwareCoreServices();

            if (readinessIssues.Count > 0)
            {
                var missingText = string.Join(
                    ", ",
                    readinessIssues);

                EndActivity(
                    title + ": modo base aplicado",
                    "El cambio de virtualización sí quedó guardado, pero todavía hay componentes que necesitan reparación: " + missingText,
                    ActivityKind.Warning);

                ShowToast(
                    stateAfter.IsConfiguredForNormal
                        ? "Modo Windows/Docker aplicado; WSL todavía necesita reparación."
                        : "Modo VMware aplicado; algunos componentes VMware todavía necesitan reparación.",
                    ActivityKind.Warning);

                return;
            }

            if (requiresRestart)
            {
                EndActivity(
                    title + ": preparado",
                    "Hace falta reiniciar para terminar de aplicar este modo.",
                    ActivityKind.Warning);

                ShowToast(
                    "Modo preparado. Reinicia cuando quieras aplicarlo.",
                    ActivityKind.Warning);
            }
            else
            {
                EndActivity(
                    title + ": listo",
                    "No necesitas reiniciar.",
                    ActivityKind.Success);

                ShowToast(
                    "Listo. No necesitas reiniciar.",
                    ActivityKind.Success);
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
                "No se pudo preparar el modo seleccionado.",
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

    private async void RefreshWinUtil_Click(
        object sender,
        RoutedEventArgs e) =>
        await LoadWinUtilCatalogAsync(
            forceRefresh: true,
            showFeedback: true);

    private async Task LoadWinUtilCatalogAsync(
        bool forceRefresh,
        bool showFeedback = true)
    {
        if (showFeedback)
        {
            BeginActivity(
                "Actualizando ajustes",
                "Cargando únicamente opciones sencillas y reversibles…");
        }

        try
        {
            WinUtilStatusText.Text = "Cargando ajustes…";

            var result = await _winUtil.LoadAsync(forceRefresh);
            _winUtilAllTweaks = result.Tweaks;

            ApplyWinUtilFilter();

            var applicable = result.Tweaks.Count(
                x => _winUtilNative.Supports(x.Id));

            WinUtilStatusText.Text =
                $"{applicable} ajustes sencillos disponibles";

            Log(
                $"Catálogo WinUtil cargado: {result.Version}, " +
                $"commit {result.Commit[..12]}. " +
                $"{applicable} opciones nativas visibles.");

            if (showFeedback)
            {
                EndActivity(
                    "Ajustes actualizados",
                    $"{applicable} opciones disponibles.",
                    ActivityKind.Success);

                ShowToast(
                    "Ajustes actualizados.",
                    ActivityKind.Success);
            }
        }
        catch (Exception ex)
        {
            WinUtilStatusText.Text = "No se pudieron cargar los ajustes";
            Log($"Error cargando catálogo WinUtil: {ex.Message}");

            if (showFeedback)
            {
                EndActivity(
                    "No se pudieron cargar los ajustes",
                    ex.Message,
                    ActivityKind.Error);

                ShowToast(
                    "No se pudieron cargar los ajustes.",
                    ActivityKind.Error);
            }
        }
    }

    private void ApplyWinUtilFilter()
    {
        if (WinUtilGrid is null)
            return;

        var items = _winUtilAllTweaks
            .Where(x => _winUtilNative.Supports(x.Id))
            .OrderBy(x => x.Content, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        WinUtilGrid.ItemsSource = items;

        if (items.Count > 0)
            WinUtilGrid.SelectedIndex = 0;
        else
            ResetWinUtilDetails();
    }

    private void WinUtilGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        UpdateWinUtilSelection();
    }

    private void UpdateWinUtilSelection()
    {
        if (WinUtilGrid.SelectedItem is not WinUtilTweak tweak ||
            !_winUtilNative.Supports(tweak.Id))
        {
            ResetWinUtilDetails();
            return;
        }

        var state = _winUtilNative.GetState(tweak.Id);

        WinUtilSelectedTitleText.Text = tweak.Content;
        WinUtilSelectedStateText.Text = state;
        WinUtilSelectedDescriptionText.Text = tweak.Description;
        WinUtilSelectedImpactText.Text = GetWinUtilImpactText(tweak.Id);
        WinUtilSelectedRiskText.Text = GetWinUtilRiskText(tweak.Id);
        WinUtilSelectedRevertText.Text = GetWinUtilRevertText(tweak.Id);

        WinUtilApplyButton.IsEnabled =
            !string.Equals(
                state,
                "Aplicado",
                StringComparison.OrdinalIgnoreCase);

        WinUtilRestoreButton.IsEnabled =
            _winUtilNative.HasBackup(tweak.Id);
    }

    private void ResetWinUtilDetails()
    {
        WinUtilSelectedTitleText.Text = "Selecciona un ajuste";
        WinUtilSelectedStateText.Text = "Sin seleccionar";
        WinUtilSelectedDescriptionText.Text =
            "Selecciona un ajuste para ver una explicación sencilla.";
        WinUtilSelectedImpactText.Text =
            "No se realizará ningún cambio hasta que pulses Aplicar.";
        WinUtilSelectedRiskText.Text = "Muy bajo";
        WinUtilSelectedRevertText.Text =
            "La app guarda el valor anterior antes de aplicar.";
        WinUtilApplyButton.IsEnabled = false;
        WinUtilRestoreButton.IsEnabled = false;
    }

    private void ApplySelectedWinUtil_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (WinUtilGrid.SelectedItem is not WinUtilTweak tweak ||
            !_winUtilNative.Supports(tweak.Id))
        {
            return;
        }

        try
        {
            _winUtilNative.Apply(tweak.Id);
            UpdateWinUtilSelection();

            Log($"Ajuste aplicado: {tweak.Id}");
            ShowToast(
                $"Aplicado: {tweak.Content}.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error aplicando ajuste {tweak.Id}: {ex.Message}");
            ShowToast(
                "No se pudo aplicar este ajuste.",
                ActivityKind.Error);
        }
    }

    private void RestoreSelectedWinUtil_Click(
        object sender,
        RoutedEventArgs e)
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

            Log($"Ajuste restaurado: {tweak.Id}");
            ShowToast(
                $"Cambio deshecho: {tweak.Content}.",
                ActivityKind.Success);
        }
        catch (Exception ex)
        {
            Log($"Error restaurando ajuste {tweak.Id}: {ex.Message}");
            ShowToast(
                "No se pudo deshacer este ajuste.",
                ActivityKind.Error);
        }
    }

    private static string GetWinUtilImpactText(string id) => id switch
    {
        "WPFToggleShowExt" =>
            "Muestra terminaciones como .exe, .jpg o .txt en el Explorador. No modifica tus archivos.",

        "WPFToggleHiddenFiles" =>
            "Permite ver archivos y carpetas ocultos. No elimina ni cambia su contenido.",

        "WPFTweaksEndTaskOnTaskbar" =>
            "Añade la opción para cerrar una aplicación desde la barra de tareas cuando deje de responder.",

        "WPFToggleTaskbarSearch" =>
            "Muestra el acceso a la búsqueda en la barra de tareas.",

        "WPFToggleDarkMode" =>
            "Activa el tema oscuro de Windows para el usuario actual.",

        _ =>
            "Cambia una preferencia sencilla y guarda el valor anterior para poder deshacerla."
    };

    private static string GetWinUtilRiskText(string id) => id switch
    {
        "WPFTweaksEndTaskOnTaskbar" =>
            "Muy bajo. Habilita un acceso para finalizar aplicaciones; usar esa acción puede cerrar trabajo no guardado.",

        _ =>
            "Muy bajo. Solo cambia una preferencia del usuario actual."
    };

    private static string GetWinUtilRevertText(string id) => id switch
    {
        "WPFToggleShowExt" =>
            "Pulsa Deshacer para recuperar exactamente la preferencia anterior de extensiones.",

        "WPFToggleHiddenFiles" =>
            "Pulsa Deshacer para recuperar la visibilidad anterior de archivos ocultos.",

        "WPFTweaksEndTaskOnTaskbar" =>
            "Pulsa Deshacer para recuperar el valor anterior del menú de la barra de tareas.",

        "WPFToggleTaskbarSearch" =>
            "Pulsa Deshacer para recuperar el estado anterior de Búsqueda en la barra.",

        "WPFToggleDarkMode" =>
            "Pulsa Deshacer para restaurar los valores de tema que existían antes de aplicar.",

        _ =>
            "Pulsa Deshacer para restaurar el valor guardado antes del cambio."
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
            var title = kind switch
            {
                ActivityKind.Success => "Listo",
                ActivityKind.Warning => "Revisa esto",
                ActivityKind.Error => "Ocurrió un problema",
                _ => "Windows 11 Optimizer"
            };

            var appearance = kind switch
            {
                ActivityKind.Success => ControlAppearance.Success,
                ActivityKind.Warning => ControlAppearance.Caution,
                ActivityKind.Error => ControlAppearance.Danger,
                _ => ControlAppearance.Info
            };

            _snackbar.Show(
                title,
                message,
                appearance,
                icon: null,
                timeout: TimeSpan.FromSeconds(3.6));
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
        message = SanitizeTechnicalText(message);

        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText(
                $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        });
    }

    private static string SanitizeTechnicalText(string message)
    {
        var sanitized = message;

        foreach (var serviceName in SafeProfile.AcerOnDemandServices)
        {
            sanitized = sanitized.Replace(
                serviceName,
                "componente del fabricante",
                StringComparison.OrdinalIgnoreCase);
        }

        foreach (var serviceName in SafeProfile.AcerProtectedServices)
        {
            sanitized = sanitized.Replace(
                serviceName,
                "componente protegido del fabricante",
                StringComparison.OrdinalIgnoreCase);
        }

        return sanitized;
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
