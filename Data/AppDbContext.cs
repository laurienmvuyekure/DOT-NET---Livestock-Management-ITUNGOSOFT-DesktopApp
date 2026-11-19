using System.IO;
using Microsoft.EntityFrameworkCore;
using ItungoSoft.Farmer.Models;

namespace ItungoSoft.Farmer.Data;

public class AppDbContext : DbContext
{
    public DbSet<FarmerUser> Farmers => Set<FarmerUser>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<HealthRecord> HealthRecords => Set<HealthRecord>();
    public DbSet<BreedingRecord> BreedingRecords => Set<BreedingRecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<Listing> Listings => Set<Listing>();

    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=itungosoft_farmer;Username=postgres;Password=12345";

    protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseNpgsql(ConnectionString);

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Animal>().HasIndex(a => new { a.FarmerId, a.EarTag }).IsUnique();
        b.Entity<Animal>().Property(a => a.Type).HasConversion<string>();
        b.Entity<HealthRecord>().Property(h => h.Kind).HasConversion<string>();

        foreach (var e in b.Model.GetEntityTypes())
        {
            e.SetTableName(e.GetTableName()!.ToLower());
            foreach (var p in e.GetProperties())
                p.SetColumnName((p.GetColumnName() ?? p.Name).ToLower());
        }
    }
}
