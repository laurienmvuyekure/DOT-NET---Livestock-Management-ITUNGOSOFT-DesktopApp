using System.Windows;
using System.Windows.Controls;
using ItungoSoft.Farmer.Services;

namespace ItungoSoft.Farmer.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var err = AuthService.ChangePassword(OldPw.Password, NewPw.Password);
        PwMsg.Text = err ?? "Password successfully changed.";
        if (err == null)
        {
            OldPw.Clear();
            NewPw.Clear();
        }
    }
}
