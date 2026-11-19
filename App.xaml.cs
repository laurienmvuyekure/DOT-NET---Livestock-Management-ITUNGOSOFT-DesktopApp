using System;
using System.Diagnostics;
using System.Windows;
using ItungoSoft.Farmer.Data;
using ItungoSoft.Farmer.Services;
using ItungoSoft.Farmer.Views;

namespace ItungoSoft.Farmer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Database Connection Notice] {ex.Message}");
        }

        try
        {
            ThemeService.Instance.Initialize();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Theme Initialization Notice] {ex.Message}");
        }

        new LoginWindow().Show();
    }
}
