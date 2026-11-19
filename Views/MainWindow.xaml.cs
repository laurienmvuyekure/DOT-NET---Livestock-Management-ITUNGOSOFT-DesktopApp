using System.Windows;
using System.Windows.Input;
using ItungoSoft.Farmer.ViewModels;

namespace ItungoSoft.Farmer.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml. Handles window chrome buttons,
/// global accessibility keyboard shortcuts (Ctrl+F, Ctrl+1..9, Ctrl+T),
/// logout delegation, and chart refresh triggers.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Logout Event: returns user to LoginWindow
        _viewModel.RequestLogout += () =>
        {
            new LoginWindow().Show();
            Close();
        };

        // Redraw charts when returning to Dashboard
        _viewModel.ActiveSectionChanged += section =>
        {
            if (section == "Dashboard")
            {
                DashboardPane.ReloadCharts();
            }
        };

        PreviewKeyDown += MainWindow_PreviewKeyDown;

        if (SystemParameters.HighContrast)
        {
            ApplyHighContrastAdjustments();
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl + F focuses the global search input
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            TopBar.FocusSearch();
            e.Handled = true;
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ApplyHighContrastAdjustments()
    {
        Resources["Border"] = SystemColors.WindowFrameBrush;
        Resources["TextPrimary"] = SystemColors.WindowTextBrush;
        Resources["Background"] = SystemColors.WindowBrush;
    }
}
