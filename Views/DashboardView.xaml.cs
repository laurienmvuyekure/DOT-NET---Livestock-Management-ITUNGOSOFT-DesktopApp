using System.Windows;
using System.Windows.Controls;
using ItungoSoft.Farmer.Services;

namespace ItungoSoft.Farmer.Views;

public partial class DashboardView : UserControl
{
    private ChartData? _data;

    public DashboardView()
    {
        InitializeComponent();

        foreach (var cv in new[] { BarCanvas, PivotCanvas, TrendCanvas, HistCanvas })
        {
            cv.SizeChanged += (_, _) => DrawCharts();
        }

        Loaded += (_, _) => ReloadCharts();
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue) ReloadCharts();
        };
    }

    public void ReloadCharts()
    {
        if (AuthService.Current == null) return;
        _data = ChartService.Load();
        DrawCharts();
    }

    private void DrawCharts()
    {
        if (_data == null) return;
        var d = _data;
        ChartDrawer.Bars(BarCanvas, "Animals by type", d.TypeLabels, new[] { d.TypeCounts }, new[] { "Animals" });
        ChartDrawer.Bars(PivotCanvas, "Pivot: type x gender", d.TypeLabels, new[] { d.Male, d.Female }, new[] { "Male", "Female" });
        ChartDrawer.Trend(TrendCanvas, "Health records per month", d.MonthLabels, d.MonthCounts);
        ChartDrawer.Bars(HistCanvas, "Histogram: age (months)", d.AgeLabels, new[] { d.AgeCounts }, new[] { "Animals" }, gapless: true);
    }
}
