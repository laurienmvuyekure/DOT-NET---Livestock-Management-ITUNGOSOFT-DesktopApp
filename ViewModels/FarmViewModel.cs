using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using ItungoSoft.Farmer.Data;
using ItungoSoft.Farmer.Models;
using ItungoSoft.Farmer.Services;

namespace ItungoSoft.Farmer.ViewModels;

public class MainViewModel
{
    public AnimalsViewModel Animals { get; } = new();
    public FarmViewModel Farm { get; } = new();
    public ExtrasViewModel Extras { get; } = new();
}

public partial class FarmViewModel : ObservableObject
{
    public ObservableCollection<Animal> AnimalList { get; } = new();
    public ObservableCollection<HealthRecord> Health { get; } = new();
    public ObservableCollection<BreedingRecord> Breeding { get; } = new();
    public ObservableCollection<Notification> Notifications { get; } = new();
    public ObservableCollection<string> Activity { get; } = new();
    public Array HealthKinds => Enum.GetValues(typeof(HealthKind));
    public string[] Methods { get; } = { "Natural", "Artificial insemination" };

    // Health form + filter
    [ObservableProperty] private Animal? _healthAnimal;
    [ObservableProperty] private HealthKind _kind = HealthKind.Vaccination;
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private DateTime _healthDate = DateTime.Today;
    [ObservableProperty] private bool _done = true;
    [ObservableProperty] private HealthRecord? _selectedHealth;
    // Breeding form
    [ObservableProperty] private Animal? _dam;
    [ObservableProperty] private string _sire = "";
    [ObservableProperty] private string _method = "Natural";
    [ObservableProperty] private DateTime _matingDate = DateTime.Today;
    [ObservableProperty] private BreedingRecord? _selectedBreeding;
    [ObservableProperty] private string _outcome = "";
    // Dashboard
    [ObservableProperty] private int _totalAnimals;
    [ObservableProperty] private int _upcomingVaccinations;
    [ObservableProperty] private int _pendingConsultations;
    [ObservableProperty] private int _activeListings;
    [ObservableProperty] private int _unread;
    [ObservableProperty] private string _message = "";

    public FarmViewModel() => Refresh();

    // Gestation in days; poultry = 21-day incubation.
    static int Gestation(AnimalType t) => t switch
    {
        AnimalType.Cattle => 283, AnimalType.Goat => 150, AnimalType.Sheep => 147, AnimalType.Pig => 114, _ => 21
    };

    partial void OnHealthAnimalChanged(Animal? value) { using var db = new AppDbContext(); LoadHealth(db); }

    void LoadHealth(AppDbContext db)
    {
        var fid = AuthService.Current!.Id;
        var q = db.HealthRecords.Include(h => h.Animal).Where(h => h.Animal!.FarmerId == fid);
        if (HealthAnimal != null) { var aid = HealthAnimal.Id; q = q.Where(h => h.AnimalId == aid); }
        Health.Clear();
        foreach (var h in q.OrderBy(h => h.Date).ToList()) Health.Add(h);
    }

    public void Refresh()
    {
        var hid = HealthAnimal?.Id; var did = Dam?.Id;
        using var db = new AppDbContext();
        var fid = AuthService.Current!.Id; var today = DateTime.Today;

        AnimalList.Clear();
        foreach (var a in db.Animals.Where(a => a.FarmerId == fid).OrderBy(a => a.EarTag).ToList()) AnimalList.Add(a);
        HealthAnimal = AnimalList.FirstOrDefault(a => a.Id == hid);
        Dam = AnimalList.FirstOrDefault(a => a.Id == did);

        // Reminders: pending health tasks due within 7 days (or overdue), deliveries due within 14 days
        var keys = db.Notifications.Where(n => n.FarmerId == fid).Select(n => n.Key).ToHashSet();
        var soon = today.AddDays(7); var delivery = today.AddDays(14);
        foreach (var h in db.HealthRecords.Include(h => h.Animal).Where(h => h.Animal!.FarmerId == fid && !h.Completed && h.Date <= soon).ToList())
            if (keys.Add("H" + h.Id))
                db.Notifications.Add(new Notification { FarmerId = fid, Key = "H" + h.Id,
                    Message = $"{h.Kind} for {h.Animal!.EarTag} {(h.Date < today ? "was due" : "is due")} on {h.Date:dd/MM/yyyy}" });
        foreach (var b in db.BreedingRecords.Include(b => b.Animal).Where(b => b.Animal!.FarmerId == fid && b.Outcome == null && b.ExpectedDelivery <= delivery).ToList())
            if (keys.Add("B" + b.Id))
                db.Notifications.Add(new Notification { FarmerId = fid, Key = "B" + b.Id,
                    Message = $"Delivery expected for {b.Animal!.EarTag} on {b.ExpectedDelivery:dd/MM/yyyy}" });
        db.SaveChanges();

        LoadHealth(db);
        Breeding.Clear();
        foreach (var b in db.BreedingRecords.Include(b => b.Animal).Where(b => b.Animal!.FarmerId == fid).OrderByDescending(b => b.MatingDate).ToList()) Breeding.Add(b);
        Notifications.Clear();
        foreach (var n in db.Notifications.Where(n => n.FarmerId == fid).OrderByDescending(n => n.CreatedAt).ToList()) Notifications.Add(n);

        TotalAnimals = AnimalList.Count;
        UpcomingVaccinations = db.HealthRecords.Count(h => h.Animal!.FarmerId == fid && !h.Completed && h.Kind == HealthKind.Vaccination && h.Date >= today);
        PendingConsultations = db.Consultations.Count(c => c.FarmerId == fid && c.Response == null);
        ActiveListings = db.Listings.Count(l => l.FarmerId == fid && l.Active);
        Unread = Notifications.Count(n => !n.IsRead);
        Activity.Clear();
        foreach (var h in db.HealthRecords.Include(h => h.Animal).Where(h => h.Animal!.FarmerId == fid).OrderByDescending(h => h.Id).Take(5).ToList())
            Activity.Add($"{h.Kind}: {h.Animal!.EarTag} - {h.Description} ({h.Status})");
    }

    [RelayCommand] void ClearFilter() => HealthAnimal = null;

    [RelayCommand]
    void AddHealth()
    {
        if (HealthAnimal == null) { Message = "Select an animal first."; return; }
        if (string.IsNullOrWhiteSpace(Description)) { Message = "Description is required."; return; }
        if (Done && HealthDate > DateTime.Today) { Message = "A completed record cannot be in the future. Untick 'Already done' to schedule it."; return; }
        using (var db = new AppDbContext())
        {
            db.HealthRecords.Add(new HealthRecord { AnimalId = HealthAnimal.Id, Kind = Kind, Description = Description.Trim(), Date = HealthDate, Completed = Done });
            db.SaveChanges();
        }
        Message = Done ? "Health record saved." : "Scheduled."; Description = ""; Refresh();
    }

    [RelayCommand]
    void MarkDone()
    {
        if (SelectedHealth == null) { Message = "Select a record first."; return; }
        using (var db = new AppDbContext())
        {
            var h = db.HealthRecords.Find(SelectedHealth.Id);
            if (h != null) { h.Completed = true; db.SaveChanges(); }
        }
        Message = "Marked as done."; Refresh();
    }

    [RelayCommand]
    void DeleteHealth()
    {
        if (SelectedHealth == null) { Message = "Select a record first."; return; }
        using (var db = new AppDbContext())
        {
            var h = db.HealthRecords.Find(SelectedHealth.Id);
            if (h != null) { db.HealthRecords.Remove(h); db.SaveChanges(); }
        }
        Message = "Record deleted."; Refresh();
    }

    [RelayCommand]
    void AddBreeding()
    {
        if (Dam == null) { Message = "Select the female animal first."; return; }
        if (Dam.Gender != "Female") { Message = "Mating must be recorded against a female animal."; return; }
        if (MatingDate > DateTime.Today) { Message = "Mating date cannot be in the future."; return; }
        using (var db = new AppDbContext())
        {
            db.BreedingRecords.Add(new BreedingRecord { AnimalId = Dam.Id, MatingDate = MatingDate, SireEarTag = Sire.Trim(),
                Method = Method, ExpectedDelivery = MatingDate.AddDays(Gestation(Dam.Type)) });
            db.SaveChanges();
        }
        Message = "Mating recorded; expected delivery calculated."; Sire = ""; Refresh();
    }

    [RelayCommand]
    void RecordOutcome()
    {
        if (SelectedBreeding == null || string.IsNullOrWhiteSpace(Outcome)) { Message = "Select a record and type the birth outcome."; return; }
        using (var db = new AppDbContext())
        {
            var b = db.BreedingRecords.Find(SelectedBreeding.Id);
            if (b != null) { b.Outcome = Outcome.Trim(); db.SaveChanges(); }
        }
        Message = "Birth outcome recorded."; Outcome = ""; Refresh();
    }

    [RelayCommand]
    void MarkAllRead()
    {
        using (var db = new AppDbContext())
        {
            var fid = AuthService.Current!.Id;
            foreach (var n in db.Notifications.Where(n => n.FarmerId == fid && !n.IsRead)) n.IsRead = true;
            db.SaveChanges();
        }
        Refresh();
    }
}
