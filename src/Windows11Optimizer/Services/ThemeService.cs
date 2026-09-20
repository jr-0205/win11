using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Windows11Optimizer.Services;

public sealed class ThemeService
{
    private readonly string _settingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Windows11Optimizer");

    private string SettingsFile => Path.Combine(_settingsDirectory, "ui-settings.json");

    public bool IsDark { get; private set; }

    public ThemeService()
    {
        IsDark = LoadPreference();
    }

    public void ApplySavedTheme() => ApplyTheme(IsDark, persist: false);

    public void ApplyTheme(bool dark, bool persist = true)
    {
        IsDark = dark;

        var resources = Application.Current.Resources;

        ApplicationThemeManager.Apply(
            dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
            WindowBackdropType.Mica,
            updateAccent: true);

        if (dark)
        {
            resources["AppBackgroundBrush"] = Brush("#0C0E12");
            resources["SurfaceBrush"] = Brush("#15181E");
            resources["SurfaceAltBrush"] = Brush("#1C2027");
            resources["ForegroundBrush"] = Brush("#F6F7F9");
            resources["MutedForegroundBrush"] = Brush("#A7AFBB");
            resources["BorderBrush"] = Brush("#2C323C");
            resources["ButtonBrush"] = Brush("#20252D");
            resources["ButtonHoverBrush"] = Brush("#2A303A");
            resources["SuccessBrush"] = Brush("#2EA66A");
            resources["WarningBrush"] = Brush("#C28A24");
            resources["ErrorBrush"] = Brush("#D95C66");
            resources["SelectionBrush"] = Brush("#263A59");
            resources["SelectionForegroundBrush"] = Brush("#F8FAFC");
        }
        else
        {
            resources["AppBackgroundBrush"] = Brush("#F3F5F7");
            resources["SurfaceBrush"] = Brush("#FFFFFF");
            resources["SurfaceAltBrush"] = Brush("#F7F8FA");
            resources["ForegroundBrush"] = Brush("#171A20");
            resources["MutedForegroundBrush"] = Brush("#66707D");
            resources["BorderBrush"] = Brush("#D9DEE6");
            resources["ButtonBrush"] = Brush("#F5F6F8");
            resources["ButtonHoverBrush"] = Brush("#EAEDF2");
            resources["SuccessBrush"] = Brush("#218653");
            resources["WarningBrush"] = Brush("#A86D00");
            resources["ErrorBrush"] = Brush("#B83E49");
            resources["SelectionBrush"] = Brush("#E8F0FE");
            resources["SelectionForegroundBrush"] = Brush("#111827");
        }

        var accent = ApplicationAccentColorManager.SystemAccent;
        var accentBrush = new SolidColorBrush(accent);
        accentBrush.Freeze();
        resources["AccentBrush"] = accentBrush;

        if (persist)
            SavePreference();
    }

    private bool LoadPreference()
    {
        try
        {
            if (!File.Exists(SettingsFile))
                return false;

            var json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<UiSettings>(json);
            return settings?.DarkMode ?? false;
        }
        catch
        {
            return false;
        }
    }

    private void SavePreference()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            var json = JsonSerializer.Serialize(
                new UiSettings { DarkMode = IsDark },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // El tema se aplica aunque no sea posible persistir la preferencia.
        }
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private sealed class UiSettings
    {
        public bool DarkMode { get; set; }
    }
}
