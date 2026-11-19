using System.Windows;
using ItungoSoft.Farmer.Helpers;
using ItungoSoft.Farmer.Services;

namespace ItungoSoft.Farmer.ViewModels;

/// <summary>
/// Root ViewModel for ITUNGOSOFT. Connects the modular navigation sidebar
/// directly with the live database-backed Animals, Farm, and Extras ViewModels.
/// </summary>
public class MainViewModel : ObservableObject
{
    private string _activeSection = "Dashboard";
    private bool _isDarkMode;

    public SliderViewModel Slider { get; } = new();

    // Live Database ViewModels
    public AnimalsViewModel Animals { get; } = new();
    public FarmViewModel Farm { get; } = new();
    public ExtrasViewModel Extras { get; } = new();

    public event Action<string>? ActiveSectionChanged;
    public event Action? RequestLogout;

    public string ActiveSection
    {
        get => _activeSection;
        set
        {
            if (SetProperty(ref _activeSection, value))
            {
                RefreshCurrentSection();
                ActiveSectionChanged?.Invoke(value);
            }
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                ThemeService.Instance.SetTheme(value);
            }
        }
    }

    // Real Logged-in Farmer User Information
    public string UserName => AuthService.Current?.FullName ?? "Farmer User";
    public string UserEmail => !string.IsNullOrEmpty(AuthService.Current?.Username)
        ? $"{AuthService.Current.Username}@itungosoft.rw"
        : "farmer@itungosoft.rw";

    public string UserInitials
    {
        get
        {
            var name = UserName.Trim();
            if (string.IsNullOrEmpty(name)) return "IT";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
        }
    }

    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand SignOutCommand { get; }
    public RelayCommand NavigateCommand { get; }

    public MainViewModel()
    {
        _isDarkMode = ThemeService.Instance.IsDarkMode;
        ThemeService.Instance.ThemeChanged += dark =>
        {
            if (_isDarkMode != dark)
            {
                _isDarkMode = dark;
                OnPropertyChanged(nameof(IsDarkMode));
            }
        };

        ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
        SignOutCommand = new RelayCommand(() =>
        {
            AuthService.Logout();
            RequestLogout?.Invoke();
        });
        NavigateCommand = new RelayCommand(param =>
        {
            if (param is string section && !string.IsNullOrWhiteSpace(section))
            {
                ActiveSection = section;
            }
        });

        // Wire Slide CTA action buttons directly to relevant views
        if (Slider.Slides.Count > 0) Slider.Slides[0].ActionCommand = new RelayCommand(() => ActiveSection = "Animals");
        if (Slider.Slides.Count > 1) Slider.Slides[1].ActionCommand = new RelayCommand(() => ActiveSection = "Health");
        if (Slider.Slides.Count > 2) Slider.Slides[2].ActionCommand = new RelayCommand(() => ActiveSection = "Breeding");
        if (Slider.Slides.Count > 3) Slider.Slides[3].ActionCommand = new RelayCommand(() => ActiveSection = "Reports");

        RefreshCurrentSection();
    }

    public void RefreshCurrentSection()
    {
        Farm.Refresh();
        Extras.Refresh();
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(UserEmail));
        OnPropertyChanged(nameof(UserInitials));
    }
}
