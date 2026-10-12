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
        var icon=Path.Combine(AppContext.BaseDirectory,"EmuVora.ico");if(File.Exists(icon))window.Icon=new WindowIcon(icon);
        window.Styles.Add(new Style(s=>s.OfType<Button>()){Setters={new Setter(Button.CornerRadiusProperty,new CornerRadius(8)),new Setter(Button.PaddingProperty,new Thickness(12,9)),new Setter(Button.FontSizeProperty,13d)}});
        window.Styles.Add(new Style(s=>s.OfType<TextBox>()){Setters={new Setter(TextBox.CornerRadiusProperty,new CornerRadius(8))}});
        window.Styles.Add(new Style(s=>s.OfType<Button>().Class(":focus")){Setters={new Setter(Button.BorderBrushProperty,Brush.Parse("#37A5FF")),new Setter(Button.BorderThicknessProperty,new Thickness(2))}});
    }
}
