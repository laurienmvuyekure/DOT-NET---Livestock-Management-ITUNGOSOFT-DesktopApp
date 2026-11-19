using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ItungoSoft.Farmer.Behaviors;

/// <summary>
/// Attached behavior providing 60 FPS throttled cursor proximity detection (60px)
/// and magnetic pull (up to 4px) with reduced-motion respect across all dashboard buttons.
/// </summary>
public static class ProximityBehavior
{
    private const double ProximityThreshold = 60.0;
    private const double MaxPullDistance = 4.0;
    private const long ThrottleMs = 16; // ~60 FPS

    private static readonly Stopwatch Timer = Stopwatch.StartNew();
    private static long _lastRunTime;
    private static readonly DropShadowEffect ProximityGlow = new()
    {
        Color = Color.FromRgb(0x3B, 0x82, 0xF6),
        BlurRadius = 12,
        ShadowDepth = 0,
        Opacity = 0.35
    };

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ProximityBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            if ((bool)e.NewValue)
            {
                window.PreviewMouseMove += Window_PreviewMouseMove;
                window.MouseLeave += Window_MouseLeave;
            }
            else
            {
                window.PreviewMouseMove -= Window_PreviewMouseMove;
                window.MouseLeave -= Window_MouseLeave;
            }
        }
    }

    private static void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Window win)
        {
            ResetAllButtons(win);
        }
    }

    private static void Window_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var now = Timer.ElapsedMilliseconds;
        if (now - _lastRunTime < ThrottleMs) return; // 60 FPS throttle
        _lastRunTime = now;

        if (sender is not Window window) return;
        var cursor = e.GetPosition(window);
        bool allowMotion = SystemParameters.ClientAreaAnimation; // Respect reduced motion

        var buttons = FindVisualChildren<Button>(window);

        foreach (var button in buttons)
        {
            if (!button.IsVisible || !button.IsEnabled)
            {
                ResetButton(button);
                continue;
            }

            try
            {
                var transform = button.TransformToAncestor(window);
                var bounds = transform.TransformBounds(new Rect(0, 0, button.ActualWidth, button.ActualHeight));
                var center = new Point(bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0);

                double dx = cursor.X - center.X;
                double dy = cursor.Y - center.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double edgeDist = Math.Max(0.0, distance - Math.Min(bounds.Width, bounds.Height) / 2.0);

                if (edgeDist <= ProximityThreshold)
                {
                    if (!button.IsMouseOver)
                    {
                        button.Effect = ProximityGlow;

                        if (allowMotion && distance > 0.001)
                        {
                            double strength = 1.0 - (edgeDist / ProximityThreshold);
                            double pull = strength * MaxPullDistance;
                            button.RenderTransform = new TranslateTransform((dx / distance) * pull, (dy / distance) * pull);
                        }
                    }
                }
                else
                {
                    ResetButton(button);
                }
            }
            catch
            {
                ResetButton(button);
            }
        }
    }

    private static void ResetAllButtons(DependencyObject parent)
    {
        foreach (var b in FindVisualChildren<Button>(parent))
            ResetButton(b);
    }

    private static void ResetButton(Button button)
    {
        if (!button.IsMouseOver)
        {
            button.ClearValue(UIElement.EffectProperty);
            button.ClearValue(UIElement.RenderTransformProperty);
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) yield break;
        int count = VisualTreeHelper.GetChildrenCount(depObj);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T t) yield return t;
            foreach (var grandChild in FindVisualChildren<T>(child))
                yield return grandChild;
        }
    }
}
