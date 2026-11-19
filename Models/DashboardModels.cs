using System.Windows.Input;

namespace ItungoSoft.Farmer.Models;

/// <summary>
/// Data model for carousel slides in the Hero banner.
/// </summary>
public class SlideItem
{
    public int Index { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public ICommand? ActionCommand { get; set; }
}

/// <summary>
/// Data model for top Quick Stats pastel cards.
/// </summary>
public class StatCardItem
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string IconData { get; set; } = string.Empty;
    public string CardBrushKey { get; set; } = string.Empty;
    public string TextBrushKey { get; set; } = string.Empty;
}
