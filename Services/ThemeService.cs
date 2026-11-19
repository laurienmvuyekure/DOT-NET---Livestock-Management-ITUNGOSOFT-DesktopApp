using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace ItungoSoft.Farmer.Services;

/// <summary>
/// Manages Dark/Light theming, runtime dictionary swapping,
/// registry-based default detection, and persistence in %AppData%\ItungoSoft\settings.json.
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private readonly string _settingsPath;
    public event Action<bool>? ThemeChanged;

    public bool IsDarkMode { get; private set; } = true;

    private ThemeService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "ItungoSoft");
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    /// <summary>
    /// Initializes theme state from saved settings or system registry.
    /// </summary>
    public void Initialize()
    {
        bool isDark = LoadSavedPreference() ?? DetectWindowsAppThemeIsDark();
        ApplyTheme(isDark);
    }

    /// <summary>
    /// Toggles between Dark and Light mode, applies changes, and persists the choice.
    /// </summary>
    public void ToggleTheme() => SetTheme(!IsDarkMode);

    /// <summary>
    /// Explicitly sets Dark (true) or Light (false) theme.
    /// </summary>
    public void SetTheme(bool isDark)
    {
        if (IsDarkMode == isDark && Application.Current.Resources.MergedDictionaries.Count > 0)
            return;

        ApplyTheme(isDark);
        SavePreference(isDark);
        ThemeChanged?.Invoke(isDark);
    }

    private void ApplyTheme(bool isDark)
    {
        IsDarkMode = isDark;
        var themeName = isDark ? "Dark" : "Light";
        var uri = new Uri($"/ItungoSoft.Farmer;component/Themes/{themeName}.xaml", UriKind.Relative);

        var appResources = Application.Current.Resources;
        var newDict = new ResourceDictionary { Source = uri };

        // Replace theme dictionary at index 0 by convention
        if (appResources.MergedDictionaries.Count > 0)
        {
            appResources.MergedDictionaries[0] = newDict;
        }
        else
        {
            appResources.MergedDictionaries.Add(newDict);
        }
    }

    private bool? LoadSavedPreference()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("IsDarkMode", out var prop))
                {
                    return prop.GetBoolean();
                }
            }
        }
        catch
        {
            // Fall back to registry on parse failure
        }
        return null;
    }

    private void SavePreference(bool isDark)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(new { IsDarkMode = isDark });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Non-critical persistence failure
        }
    }

    /// <summary>
    /// Reads Windows registry: AppsUseLightTheme (0 = Dark, 1 = Light).
    /// </summary>
    public static bool DetectWindowsAppThemeIsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intVal)
                {
                    return intVal == 0;
                }
            }
        }
        catch
        {
            // Default to dark mode on restricted permissions
        }
        return true;
    }
}
