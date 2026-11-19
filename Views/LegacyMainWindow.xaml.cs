using System.Windows;
using ItungoSoft.Farmer.Services;
using ItungoSoft.Farmer.ViewModels;

namespace ItungoSoft.Farmer.Views;

public partial class LegacyMainWindow : Window
{
    ChartData? _data;

    public LegacyMainWindow()
    {
        InitializeComponent();
        Welcome.Text = $"Welcome, {AuthService.Current!.FullName}";
        var vm = new MainViewModel();
        DataContext = vm;

        Tabs.SelectionChanged += (s, e) =>
        {
            if (e.Source == Tabs)
            {
                vm.Farm.Refresh();
                vm.Extras.Refresh();
                if (Tabs.SelectedIndex == 0) ReloadCharts();
            }
        };

        foreach (var cv in new[] { BarCanvas, PivotCanvas, TrendCanvas, HistCanvas })
            cv.SizeChanged += (_, _) => DrawCharts();
        Loaded += (_, _) => ReloadCharts();
    }

    void ReloadCharts() { _data = ChartService.Load(); DrawCharts(); }

    void DrawCharts()
    {
        if (_data == null) return;
        var d = _data;
        ChartDrawer.Bars(BarCanvas, "Animals by type", d.TypeLabels, new[] { d.TypeCounts }, new[] { "Animals" });
        ChartDrawer.Bars(PivotCanvas, "Pivot: type x gender", d.TypeLabels, new[] { d.Male, d.Female }, new[] { "Male", "Female" });
        ChartDrawer.Trend(TrendCanvas, "Health records per month", d.MonthLabels, d.MonthCounts);
        ChartDrawer.Bars(HistCanvas, "Histogram: age (months)", d.AgeLabels, new[] { d.AgeCounts }, new[] { "Animals" }, gapless: true);
    }

    void ChangePassword_Click(object s, RoutedEventArgs e)
    {
        var err = AuthService.ChangePassword(OldPw.Password, NewPw.Password);
        PwMsg.Text = err ?? "Password changed.";
        if (err == null) { OldPw.Clear(); NewPw.Clear(); }
    }

    void Logout_Click(object s, RoutedEventArgs e)
    {
        AuthService.Logout();
        new LoginWindow().Show();
        Close();
    }
}
