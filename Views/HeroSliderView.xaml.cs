using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ItungoSoft.Farmer.ViewModels;

namespace ItungoSoft.Farmer.Views;

public partial class HeroSliderView : UserControl
{
    private Point _touchStartPoint;
    private bool _isDragging;

    public HeroSliderView()
    {
        InitializeComponent();

        MouseEnter += (s, e) => UpdatePauseState(true);
        MouseLeave += (s, e) => UpdatePauseState(false);
        IsKeyboardFocusWithinChanged += (s, e) => UpdatePauseState(IsKeyboardFocusWithin);

        PreviewKeyDown += HeroSliderView_PreviewKeyDown;
        PreviewMouseDown += HeroSliderView_PreviewMouseDown;
        PreviewMouseUp += HeroSliderView_PreviewMouseUp;

        DataContextChanged += HeroSliderView_DataContextChanged;
    }

    private void HeroSliderView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SliderViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(SliderViewModel.CurrentIndex))
                {
                    PlaySlideAnimation();
                    AnnounceToScreenReader(vm.Announcement);
                }
            };
        }
    }

    private void UpdatePauseState(bool isPaused)
    {
        if (DataContext is SliderViewModel vm)
        {
            vm.SetHoverOrFocusState(isPaused);
        }
    }

    /// <summary>
    /// Smooth slide and fade animation when slide changes (400ms, CubicEase).
    /// </summary>
    private void PlaySlideAnimation()
    {
        if (!SystemParameters.ClientAreaAnimation) return;

        var storyboard = new Storyboard();

        var slideAnim = new DoubleAnimation
        {
            From = 30,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideAnim, SlideContentPanel);
        Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

        var fadeAnim = new DoubleAnimation
        {
            From = 0.4,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeAnim, SlideContentPanel);
        Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));

        storyboard.Children.Add(slideAnim);
        storyboard.Children.Add(fadeAnim);
        storyboard.Begin();
    }

    private void HeroSliderView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not SliderViewModel vm) return;

        if (e.Key == Key.Left)
        {
            vm.PrevSlide();
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            vm.NextSlide();
            e.Handled = true;
        }
    }

    private void HeroSliderView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _touchStartPoint = e.GetPosition(this);
        _isDragging = true;
    }

    private void HeroSliderView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        var endPoint = e.GetPosition(this);
        double deltaX = endPoint.X - _touchStartPoint.X;

        if (DataContext is SliderViewModel vm)
        {
            const double swipeThreshold = 50.0;
            if (deltaX < -swipeThreshold)
            {
                vm.NextSlide();
            }
            else if (deltaX > swipeThreshold)
            {
                vm.PrevSlide();
            }
        }
    }

    private void AnnounceToScreenReader(string announcement)
    {
        if (string.IsNullOrEmpty(announcement)) return;
        var peer = UIElementAutomationPeer.FromElement(this);
        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
