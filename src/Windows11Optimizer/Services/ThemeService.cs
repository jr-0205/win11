using System.Text.Json;
using System.Windows;
using System.Windows.Media;

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

        if (dark)
        {
            resources["AppBackgroundBrush"] = Brush("#111318");
            resources["SurfaceBrush"] = Brush("#191C22");
            resources["SurfaceAltBrush"] = Brush("#20242C");
            resources["ForegroundBrush"] = Brush("#F3F5F7");
            resources["MutedForegroundBrush"] = Brush("#AAB1BC");
            resources["BorderBrush"] = Brush("#343A45");
            resources["AccentBrush"] = Brush("#5EA1FF");
            resources["ButtonBrush"] = Brush("#252A33");
            resources["ButtonHoverBrush"] = Brush("#303744");
            resources["SuccessBrush"] = Brush("#1F7A4D");
            resources["WarningBrush"] = Brush("#9A6A17");
            resources["ErrorBrush"] = Brush("#9B3B42");
        }
        else
        {
            resources["AppBackgroundBrush"] = Brush("#F3F5F8");
            resources["SurfaceBrush"] = Brush("#FFFFFF");
            resources["SurfaceAltBrush"] = Brush("#F7F8FA");
            resources["ForegroundBrush"] = Brush("#1A1D23");
            resources["MutedForegroundBrush"] = Brush("#626A76");
            resources["BorderBrush"] = Brush("#D7DBE2");
            resources["AccentBrush"] = Brush("#2563EB");
            resources["ButtonBrush"] = Brush("#F3F4F6");
            resources["ButtonHoverBrush"] = Brush("#E8ECF2");
            resources["SuccessBrush"] = Brush("#1E7A46");
            resources["WarningBrush"] = Brush("#A26300");
            resources["ErrorBrush"] = Brush("#B23A42");
        }

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
