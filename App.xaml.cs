using System.Windows;
using ItungoSoft.Farmer.Data;
using ItungoSoft.Farmer.Views;

namespace ItungoSoft.Farmer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        using (var db = new AppDbContext()) db.Database.EnsureCreated();
        new LoginWindow().Show();
    }
}
