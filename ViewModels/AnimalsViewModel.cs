using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ItungoSoft.Farmer.Data;
using ItungoSoft.Farmer.Models;
using ItungoSoft.Farmer.Services;

namespace ItungoSoft.Farmer.ViewModels;

public partial class AnimalsViewModel : ObservableObject
{
    List<Animal> _all = new();
    public ObservableCollection<Animal> Animals { get; } = new();
    public Array AnimalTypes => Enum.GetValues(typeof(AnimalType));
    public string[] Genders { get; } = { "Female", "Male" };

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _earTag = "";
    [ObservableProperty] private AnimalType _selectedType = AnimalType.Cattle;
    [ObservableProperty] private string _breed = "";
    [ObservableProperty] private string _gender = "Female";
    [ObservableProperty] private DateTime _dateOfBirth = DateTime.Today;
    [ObservableProperty] private Animal? _selected;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private int _total;
    [ObservableProperty] private string? _photoPath;
    [ObservableProperty] private string _saveLabel = "Add animal";
    int? _editingId;

    public AnimalsViewModel() => Load();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    void Load()
    {
        using var db = new AppDbContext();
        var id = AuthService.Current!.Id;
        _all = db.Animals.Where(a => a.FarmerId == id).OrderBy(a => a.EarTag).ToList();
        ApplyFilter();
    }

    void ApplyFilter()
    {
        var q = SearchText.Trim().ToLower();
        Animals.Clear();
        foreach (var a in _all.Where(a => q == "" || a.EarTag.ToLower().Contains(q)
                     || a.Breed.ToLower().Contains(q) || a.Type.ToString().ToLower().Contains(q)))
            Animals.Add(a);
        Total = _all.Count;
    }

    [RelayCommand]
    void Add()
    {
        var tag = EarTag.Trim();
        if (tag == "") { Message = "Ear tag is required."; return; }
        if (DateOfBirth > DateTime.Today) { Message = "Date of birth cannot be in the future."; return; }
        using var db = new AppDbContext();
        var id = AuthService.Current!.Id;
        var eid = _editingId ?? 0;
        if (db.Animals.Any(a => a.FarmerId == id && a.EarTag == tag && a.Id != eid)) { Message = "That ear tag already exists."; return; }
        if (_editingId == null)
            db.Animals.Add(new Animal { FarmerId = id, EarTag = tag, Type = SelectedType, Breed = Breed.Trim(), Gender = Gender, DateOfBirth = DateOfBirth, PhotoPath = PhotoPath });
        else
        {
            var a = db.Animals.Find(eid)!;
            a.EarTag = tag; a.Type = SelectedType; a.Breed = Breed.Trim(); a.Gender = Gender; a.DateOfBirth = DateOfBirth; a.PhotoPath = PhotoPath;
        }
        db.SaveChanges();
        ResetForm(); Message = "Animal saved.";
        Load();
    }

    [RelayCommand]
    void Delete()
    {
        if (Selected == null) { Message = "Select an animal first."; return; }
        if (MessageBox.Show($"Delete animal {Selected.EarTag} and all its records?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        using var db = new AppDbContext();
        var a = db.Animals.Find(Selected.Id);
        if (a != null) { db.Animals.Remove(a); db.SaveChanges(); }
        Message = "Animal deleted.";
        Load();
    }

    [RelayCommand]
    void ChoosePhoto()
    {
        var d = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png" };
        if (d.ShowDialog() != true) return;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ItungoSoft", "photos");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, Guid.NewGuid() + Path.GetExtension(d.FileName));
        File.Copy(d.FileName, dest);
        PhotoPath = dest;
    }

    [RelayCommand]
    void Edit()
    {
        if (Selected == null) { Message = "Select an animal first."; return; }
        _editingId = Selected.Id; EarTag = Selected.EarTag; SelectedType = Selected.Type; Breed = Selected.Breed;
        Gender = Selected.Gender; DateOfBirth = Selected.DateOfBirth; PhotoPath = Selected.PhotoPath;
        SaveLabel = "Save changes"; Message = "Editing: change the fields, then click Save changes.";
    }

    [RelayCommand] void CancelEdit() { ResetForm(); Message = ""; }

    void ResetForm() { _editingId = null; EarTag = ""; Breed = ""; PhotoPath = null; SaveLabel = "Add animal"; }
}
