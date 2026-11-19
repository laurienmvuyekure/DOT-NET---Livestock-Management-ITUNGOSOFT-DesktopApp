namespace ItungoSoft.Farmer.Models;

public enum AnimalType { Cattle, Goat, Sheep, Pig, Poultry }
public enum HealthKind { Vaccination, Treatment, Deworming, Checkup }

public class FarmerUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Salt { get; set; } = "";
}

public class Animal
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public string EarTag { get; set; } = "";
    public AnimalType Type { get; set; }
    public string Breed { get; set; } = "";
    public string Gender { get; set; } = "Female";
    public DateTime DateOfBirth { get; set; } = DateTime.Today;
    public string? PhotoPath { get; set; }
    public List<HealthRecord> HealthRecords { get; set; } = new();
    public List<BreedingRecord> BreedingRecords { get; set; } = new();
}

public class HealthRecord
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }
    public HealthKind Kind { get; set; }
    public string Description { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;   // date done, or scheduled date
    public bool Completed { get; set; } = true;
    public string Status => Completed ? "Done" : Date.Date < DateTime.Today ? "Overdue" : "Upcoming";
}

public class BreedingRecord
{
    public int Id { get; set; }
    public int AnimalId { get; set; }                       // the dam
    public Animal? Animal { get; set; }
    public DateTime MatingDate { get; set; }
    public string SireEarTag { get; set; } = "";
    public string Method { get; set; } = "Natural";
    public DateTime ExpectedDelivery { get; set; }
    public string? Outcome { get; set; }
    public string Status => Outcome == null ? "Pregnant" : "Delivered";
}

public class Notification
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public string Key { get; set; } = "";                   // prevents duplicate reminders
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
}

// Tables created now so phase 3 needs no database reset.
public class Consultation
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int? AnimalId { get; set; }
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Response { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string AnimalTag { get; set; } = "";
}

public class Listing
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int AnimalId { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = "";
    public bool Active { get; set; } = true;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string AnimalTag { get; set; } = "";
}
