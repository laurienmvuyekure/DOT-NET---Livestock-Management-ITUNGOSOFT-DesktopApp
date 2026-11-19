using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ItungoSoft.Farmer.Data;
using ItungoSoft.Farmer.Models;
using ItungoSoft.Farmer.Services;

namespace ItungoSoft.Farmer.ViewModels;

public partial class ExtrasViewModel : ObservableObject
{
    public ObservableCollection<Animal> AnimalList { get; } = new();
    public ObservableCollection<Consultation> Consultations { get; } = new();
    public ObservableCollection<Listing> Listings { get; } = new();

    [ObservableProperty] private Animal? _consAnimal;
    [ObservableProperty] private string _subject = "";
    [ObservableProperty] private string _consDescription = "";
    [ObservableProperty] private Consultation? _selectedConsultation;
    [ObservableProperty] private string _response = "";
    [ObservableProperty] private Animal? _listAnimal;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private string _listDescription = "";
    [ObservableProperty] private Listing? _selectedListing;
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _message = "";

    public ExtrasViewModel() => Refresh();

    public void Refresh()
    {
        using var db = new AppDbContext();
        var fid = AuthService.Current!.Id;
        var animals = db.Animals.Where(a => a.FarmerId == fid).OrderBy(a => a.EarTag).ToList();
        var tags = animals.ToDictionary(a => a.Id, a => a.EarTag);
        AnimalList.Clear(); foreach (var a in animals) AnimalList.Add(a);
        Consultations.Clear();
        foreach (var c in db.Consultations.Where(c => c.FarmerId == fid).OrderByDescending(c => c.CreatedAt).ToList())
        { c.AnimalTag = c.AnimalId.HasValue && tags.TryGetValue(c.AnimalId.Value, out var t) ? t : "-"; Consultations.Add(c); }
        Listings.Clear();
        foreach (var l in db.Listings.Where(l => l.FarmerId == fid).OrderByDescending(l => l.Id).ToList())
        { l.AnimalTag = tags.TryGetValue(l.AnimalId, out var t) ? t : "-"; Listings.Add(l); }
        FullName = AuthService.Current.FullName; Username = AuthService.Current.Username;
    }

    // ---- Consultations ----
    [RelayCommand]
    void AddConsultation()
    {
        if (ConsAnimal == null || string.IsNullOrWhiteSpace(Subject)) { Message = "Select an animal and enter a subject."; return; }
        using (var db = new AppDbContext())
        {
            db.Consultations.Add(new Consultation { FarmerId = AuthService.Current!.Id, AnimalId = ConsAnimal.Id, Subject = Subject.Trim(), Description = ConsDescription.Trim() });
            db.SaveChanges();
        }
        Subject = ""; ConsDescription = ""; Message = "Consultation request saved."; Refresh();
    }

    [RelayCommand]
    void SaveResponse()
    {
        if (SelectedConsultation == null || string.IsNullOrWhiteSpace(Response)) { Message = "Select a request and type the response."; return; }
        using (var db = new AppDbContext())
        {
            var fid = AuthService.Current!.Id;
            var c = db.Consultations.Find(SelectedConsultation.Id);
            if (c != null)
            {
                c.Response = Response.Trim();
                var key = "C" + c.Id;
                if (!db.Notifications.Any(n => n.FarmerId == fid && n.Key == key))
                    db.Notifications.Add(new Notification { FarmerId = fid, Key = key, Message = $"Response received for consultation: {c.Subject}" });
                db.SaveChanges();
            }
        }
        Response = ""; Message = "Response saved."; Refresh();
    }

    // ---- Listings ----
    [RelayCommand]
    void AddListing()
    {
        if (ListAnimal == null) { Message = "Select an animal."; return; }
        if (Price <= 0) { Message = "Enter a price above zero."; return; }
        using (var db = new AppDbContext())
        {
            var aid = ListAnimal.Id;
            if (db.Listings.Any(l => l.AnimalId == aid && l.Active)) { Message = "That animal already has an active listing."; return; }
            db.Listings.Add(new Listing { FarmerId = AuthService.Current!.Id, AnimalId = aid, Price = Price, Description = ListDescription.Trim() });
            db.SaveChanges();
        }
        Price = 0; ListDescription = ""; Message = "Listing created."; Refresh();
    }

    [RelayCommand]
    void ToggleListing()
    {
        if (SelectedListing == null) { Message = "Select a listing first."; return; }
        using (var db = new AppDbContext())
        {
            var l = db.Listings.Find(SelectedListing.Id);
            if (l != null) { l.Active = !l.Active; db.SaveChanges(); }
        }
        Refresh();
    }

    [RelayCommand]
    void DeleteListing()
    {
        if (SelectedListing == null) { Message = "Select a listing first."; return; }
        using (var db = new AppDbContext())
        {
            var l = db.Listings.Find(SelectedListing.Id);
            if (l != null) { db.Listings.Remove(l); db.SaveChanges(); }
        }
        Message = "Listing deleted."; Refresh();
    }

    // ---- Profile ----
    [RelayCommand] void SaveProfile() => Message = AuthService.UpdateProfile(FullName, Username) ?? "Profile updated.";

    // ---- Reports, export, backup ----
    void SaveFile(string filter, string name, Action<string> action)
    {
        var d = new SaveFileDialog { Filter = filter, FileName = name };
        if (d.ShowDialog() != true) return;
        try { action(d.FileName); Message = "Saved: " + d.FileName; } catch (Exception ex) { Message = "Failed: " + ex.Message; }
    }

    [RelayCommand] void PopulationPdf() => SaveFile("PDF|*.pdf", "animal_population.pdf", ExportService.PopulationPdf);
    [RelayCommand] void HealthPdf() => SaveFile("PDF|*.pdf", "health_summary.pdf", ExportService.HealthPdf);
    [RelayCommand] void BreedingPdf() => SaveFile("PDF|*.pdf", "breeding_performance.pdf", ExportService.BreedingPdf);
    [RelayCommand] void ExportCsv() => SaveFile("CSV|*.csv", "itungosoft_export.csv", ExportService.ExportCsv);

    [RelayCommand]
    void Backup()
    {
        var d = new SaveFileDialog { Filter = "Backup|*.backup", FileName = $"itungosoft_{DateTime.Now:yyyyMMdd}.backup" };
        if (d.ShowDialog() != true) return;
        Message = ExportService.Backup(d.FileName) ?? "Backup saved: " + d.FileName;
    }

    [RelayCommand]
    void Restore()
    {
        var d = new OpenFileDialog { Filter = "Backup|*.backup" };
        if (d.ShowDialog() != true) return;
        if (MessageBox.Show("Restore replaces ALL current data. Continue?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Message = ExportService.Restore(d.FileName) ?? "Restore complete. Please restart the application.";
    }
}
