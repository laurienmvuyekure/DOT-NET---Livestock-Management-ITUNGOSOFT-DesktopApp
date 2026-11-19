using System.Windows.Controls;

namespace ItungoSoft.Farmer.Views;

public partial class TopBarView : UserControl
{
    public TopBarView()
    {
        InitializeComponent();
    }

    public void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }
}
