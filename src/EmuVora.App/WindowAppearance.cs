using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace EmuVora.App;
public static class WindowAppearance
{
    public static void Apply(Window window)
    {
        window.FontFamily=new FontFamily("Segoe UI");
        window.Styles.Add(new Style(s=>s.OfType<Button>()){Setters={new Setter(Button.CornerRadiusProperty,new CornerRadius(8)),new Setter(Button.PaddingProperty,new Thickness(12,9)),new Setter(Button.FontSizeProperty,13d)}});
        window.Styles.Add(new Style(s=>s.OfType<TextBox>()){Setters={new Setter(TextBox.CornerRadiusProperty,new CornerRadius(8))}});
    }
}
