using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private TextBlock GameTitleText(Game game, bool compact = false)
    {
        var line = settings.TvMode ? compact ? 28 : 26 : compact ? 22 : 20;
        var title = new TextBlock { Text = (game.Favorite ? "★ " : "") + game.Title, FontSize = settings.TvMode ? compact ? 21 : 18 : compact ? 16 : 14,
            FontWeight = FontWeight.SemiBold, LineHeight = line, Height = line * (compact ? 1 : 2), MaxLines = compact ? 1 : 2,
            TextWrapping = compact ? TextWrapping.NoWrap : TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis };
        title.Classes.Add("game-title"); return title;
    }
    private TextBlock GameCaption(string text, bool secondary = false)
    {
        var caption = new TextBlock { Text = text, FontSize = settings.TvMode ? 15 : 12, Height = settings.TvMode ? 22 : 18, LineHeight = settings.TvMode ? 22 : 18,
            MaxLines = 1, TextTrimming = TextTrimming.CharacterEllipsis, Opacity = secondary ? .65 : .8 };
        caption.Classes.Add("game-caption"); return caption;
    }
    private Border GameArtworkFrame(Game game, int width, double height, bool compact = false)
    {
        Control artwork = compact && CoverLibrary.IsMissing(settings, game.Path)
            ? new TextBlock { Text = "\uE7FC", FontFamily = new("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            : Artwork(game, width);
        var frame = new Border { Height = height, Padding = new Thickness(compact ? 2 : 5), CornerRadius = new(compact ? 8 : 12),
            Background = Brush.Parse(settings.Theme == "Classic Light" ? "#E4EAEC" : "#1E2932"), Child = artwork, ClipToBounds = true };
        frame.Classes.Add("game-artwork"); return frame;
    }
}
