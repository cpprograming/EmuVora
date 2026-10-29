using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EmuVora.Core;

namespace EmuVora.App;

public static class ThemeDecorationRenderer
{
    public static void Clear(ThemeLayoutPanel stage)
    {
        foreach (var child in stage.Children.Where(c => c.Tag as string == ThemeLayout.DecorationPrefix).ToArray()) stage.RemoveSlot(child);
    }
    public static IReadOnlyList<string> Replace(ThemeLayoutPanel stage, Func<ThemeElement, Bitmap?> imageFor, FontFamily font, IBrush foreground, double fontSize)
    {
        Clear(stage); var notes = new List<string>();
        if (!stage.Model.Usable) return notes;
        foreach (var (role, element) in stage.Model.Decorations)
        {
            if (!stage.Model.Elements[role].Visible) continue;
            Control control;
            if (element.Type == "image")
            {
                try
                {
                    var bitmap = imageFor(element);
                    if (bitmap == null) { notes.Add("Dekorationsbild fehlt; ausgelassen: " + element.Name); continue; }
                    control = new Image { Source = bitmap, Stretch = Stretch.Uniform };
                }
                catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException or System.Xml.XmlException or NotSupportedException or FormatException)
                { notes.Add("Dekorationsbild unlesbar; ausgelassen: " + element.Name); continue; }
            }
            else
            {
                var text = element.Properties.GetValueOrDefault("text") ?? "";
                var value = element.Properties.GetValueOrDefault("color")?.TrimStart('#'); if (value?.Length == 8) value = value[6..] + value[..6];
                var brush = value?.Length is 6 or 8 && Color.TryParse("#" + value, out var color) ? new SolidColorBrush(color) : foreground;
                control = new TextBlock { Text = text.Length > 512 ? text[..512] : text, FontFamily = font, FontSize = fontSize, Foreground = brush, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, ClipToBounds = true };
            }
            control.Tag = ThemeLayout.DecorationPrefix; stage.AddSlot(role, control);
        }
        return notes;
    }
}
