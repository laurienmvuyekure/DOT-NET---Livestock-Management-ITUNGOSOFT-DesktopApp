using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ItungoSoft.Farmer.Views;

public static class ChartDrawer
{
    static Brush Hex(string h) => (Brush)new BrushConverter().ConvertFrom(h)!;
    static readonly Brush[] Palette = { Hex("#2E7D32"), Hex("#F9A825"), Hex("#1565C0") };
    const double L = 38, R = 10, T = 28, B = 26;

    static void DrawText(Canvas c, string s, double x, double y, double size = 11, bool bold = false, Brush? fg = null, double width = 0)
    {
        var t = new TextBlock { Text = s, FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Foreground = fg ?? Brushes.DimGray };
        if (width > 0) { t.Width = width; t.TextAlignment = TextAlignment.Center; }
        Canvas.SetLeft(t, x); Canvas.SetTop(t, y); c.Children.Add(t);
    }

    static void DrawLine(Canvas c, double x1, double y1, double x2, double y2, Brush stroke, double th = 1, bool dash = false)
    {
        var l = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = th };
        if (dash) l.StrokeDashArray = new DoubleCollection { 4, 3 };
        c.Children.Add(l);
    }

    static (double pw, double ph, double max) Axes(Canvas c, string title, double dataMax)
    {
        c.Children.Clear();
        double pw = c.ActualWidth - L - R, ph = c.ActualHeight - T - B;
        double max = Math.Max(4, Math.Ceiling(dataMax / 4) * 4);   // gridlines land on whole numbers
        DrawText(c, title, 6, 4, 13, true, Brushes.Black);
        for (int i = 0; i <= 4; i++)
        {
            double y = T + ph - ph * i / 4;
            DrawLine(c, L, y, L + pw, y, Brushes.LightGray);
            DrawText(c, (max * i / 4).ToString("0"), 4, y - 8, 10, width: L - 8);
        }
        return (pw, ph, max);
    }

    static void Legend(Canvas c, string[] names, double pw)
    {
        double x = L + pw - names.Length * 62;
        for (int i = 0; i < names.Length; i++)
        {
            var sq = new Rectangle { Width = 10, Height = 10, Fill = Palette[i % Palette.Length] };
            Canvas.SetLeft(sq, x + i * 62); Canvas.SetTop(sq, 7); c.Children.Add(sq);
            DrawText(c, names[i], x + i * 62 + 13, 4, 10);
        }
    }

    // Bar chart, pivot (clustered) chart, and histogram (gapless = true)
    public static void Bars(Canvas c, string title, string[] labels, double[][] series, string[] names, bool gapless = false)
    {
        if (c.ActualWidth < 120 || c.ActualHeight < 100) return;
        var (pw, ph, max) = Axes(c, title, series.SelectMany(s => s).DefaultIfEmpty(0).Max());
        double slot = pw / labels.Length, gap = gapless ? 0 : slot * 0.2, bw = (slot - gap) / series.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            for (int j = 0; j < series.Length; j++)
            {
                double v = series[j][i], bh = ph * v / max, x = L + i * slot + gap / 2 + j * bw;
                var r = new Rectangle
                {
                    Width = Math.Max(1, bw),
                    Height = bh,
                    Fill = Palette[j % Palette.Length],
                    Stroke = gapless ? Brushes.White : null,
                    StrokeThickness = gapless ? 1 : 0
                };
                Canvas.SetLeft(r, x); Canvas.SetTop(r, T + ph - bh); c.Children.Add(r);
                if (v > 0) DrawText(c, v.ToString("0"), x, T + ph - bh - 15, 10, width: bw);
            }
            DrawText(c, labels[i], L + i * slot, T + ph + 4, 10, width: slot);
        }
        if (series.Length > 1) Legend(c, names, pw);
    }

    // Line chart with a least-squares linear trend line
    public static void Trend(Canvas c, string title, string[] labels, double[] vals)
    {
        if (c.ActualWidth < 120 || c.ActualHeight < 100) return;
        var (pw, ph, max) = Axes(c, title, vals.DefaultIfEmpty(0).Max());
        int n = vals.Length; double slot = pw / n;
        double X(int i) => L + slot * (i + 0.5);
        double Y(double v) => T + ph - ph * v / max;

        var pl = new Polyline { Stroke = Palette[0], StrokeThickness = 2 };
        for (int i = 0; i < n; i++)
        {
            pl.Points.Add(new Point(X(i), Y(vals[i])));
            DrawText(c, labels[i], L + i * slot, T + ph + 4, 10, width: slot);
        }
        c.Children.Add(pl);
        for (int i = 0; i < n; i++)
        {
            var dot = new Ellipse { Width = 7, Height = 7, Fill = Palette[0] };
            Canvas.SetLeft(dot, X(i) - 3.5); Canvas.SetTop(dot, Y(vals[i]) - 3.5); c.Children.Add(dot);
        }

        double mx = (n - 1) / 2.0, my = vals.Average();
        double sxx = Enumerable.Range(0, n).Sum(i => (i - mx) * (i - mx));
        double slope = sxx == 0 ? 0 : Enumerable.Range(0, n).Sum(i => (i - mx) * (vals[i] - my)) / sxx;
        double a = my - slope * mx;
        DrawLine(c, X(0), Y(a), X(n - 1), Y(a + slope * (n - 1)), Brushes.Crimson, 2, true);
        DrawText(c, $"Trend: {slope:+0.0;-0.0;0.0} per month", L + pw - 120, 4, 10, false, Brushes.Crimson);
    }
}