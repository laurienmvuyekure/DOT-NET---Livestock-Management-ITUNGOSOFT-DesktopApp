using System.Windows;
using ItungoSoft.Farmer.Services;

namespace ItungoSoft.Farmer.Views;

public partial class LoginWindow : Window
{
    public LoginWindow() => InitializeComponent();

    void Login_Click(object s, RoutedEventArgs e)
    {
        if (!AuthService.Login(UserBox.Text, PassBox.Password)) { Msg.Text = "Invalid username or password."; return; }
        new MainWindow().Show();
        Close();
    }

    void Register_Click(object s, RoutedEventArgs e)
    {
        var err = AuthService.Register(NameBox.Text, UserBox.Text, PassBox.Password);
        if (err != null) { Msg.Text = err; return; }
        Login_Click(s, e);
    }
}
