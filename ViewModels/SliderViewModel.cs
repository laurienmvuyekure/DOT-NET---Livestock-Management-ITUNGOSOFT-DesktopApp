using System.Collections.ObjectModel;
using System.Windows.Threading;
using ItungoSoft.Farmer.Helpers;
using ItungoSoft.Farmer.Models;

namespace ItungoSoft.Farmer.ViewModels;

/// <summary>
/// ViewModel managing the Hero Carousel slider: slide collection, auto-advance timer (6s),
/// hover/focus pause state, manual transitions, and accessibility announcements.
/// </summary>
public class SliderViewModel : ObservableObject
{
    private readonly DispatcherTimer _autoTimer;
    private int _currentIndex;
    private SlideItem _currentSlide = null!;
    private bool _isHoveredOrFocused;
    private string _announcement = string.Empty;

    public ObservableCollection<SlideItem> Slides { get; } = new();

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (SetProperty(ref _currentIndex, value))
            {
                CurrentSlide = Slides[_currentIndex];
                UpdateAnnouncement();
                OnPropertyChanged(nameof(SlideNumberDisplay));
            }
        }
    }

    public SlideItem CurrentSlide
    {
        get => _currentSlide;
        private set => SetProperty(ref _currentSlide, value);
    }

    public string Announcement
    {
        get => _announcement;
        private set => SetProperty(ref _announcement, value);
    }

    public string SlideNumberDisplay => $"Slide {CurrentIndex + 1} of {Slides.Count}";

    public RelayCommand NextCommand { get; }
    public RelayCommand PrevCommand { get; }
    public RelayCommand SelectSlideCommand { get; }

    public SliderViewModel()
    {
        InitializeSlides();

        _currentSlide = Slides[0];
        _currentIndex = 0;
        UpdateAnnouncement();

        NextCommand = new RelayCommand(NextSlide);
        PrevCommand = new RelayCommand(PrevSlide);
        SelectSlideCommand = new RelayCommand(param =>
        {
            if (param is int idx && idx >= 0 && idx < Slides.Count)
            {
                CurrentIndex = idx;
            }
        });

        // 6-second auto-advance timer
        _autoTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        _autoTimer.Tick += (s, e) =>
        {
            if (!_isHoveredOrFocused)
            {
                NextSlide();
            }
        };
        _autoTimer.Start();
    }

    private void InitializeSlides()
    {
        Slides.Add(new SlideItem
        {
            Index = 0,
            Category = "COMMAND CENTER",
            Title = "Welcome to ITUNGOSOFT — Digital Livestock Management for Rwanda",
            Description = "Replace paper records with an offline-first desktop platform. Track animal identification, health cycles, breeding schedules, and marketplace listings.",
            ButtonText = "Getting Started: Watch Now"
        });

        Slides.Add(new SlideItem
        {
            Index = 1,
            Category = "HEALTH TRACKING",
            Title = "Automated Vaccination & Deworming Reminders",
            Description = "Stay ahead of disease outbreaks with automated 7-day in-app notifications and real-time overdue alerts across your entire herd.",
            ButtonText = "View Health Schedule"
        });

        Slides.Add(new SlideItem
        {
            Index = 2,
            Category = "BREEDING MANAGEMENT",
            Title = "Accurate Gestation & Calving Timeline Calculations",
            Description = "Calculate expected delivery dates automatically based on species: Cattle (283d), Goats (150d), Sheep (147d), Pigs (114d), and Poultry (21d).",
            ButtonText = "View Breeding Registry"
        });

        Slides.Add(new SlideItem
        {
            Index = 3,
            Category = "REPORTS & DATA",
            Title = "Generate Official Population & Health PDF Audits",
            Description = "Export structured QuestPDF reports for veterinary audits, census submissions, and CSV data backups with one click.",
            ButtonText = "Export Reports"
        });
    }

    public void NextSlide() => CurrentIndex = (CurrentIndex + 1) % Slides.Count;

    public void PrevSlide() => CurrentIndex = (CurrentIndex - 1 + Slides.Count) % Slides.Count;

    public void SetHoverOrFocusState(bool isPaused) => _isHoveredOrFocused = isPaused;

    private void UpdateAnnouncement()
    {
        Announcement = $"Showing slide {CurrentIndex + 1} of {Slides.Count}: {CurrentSlide.Title}";
    }
}
