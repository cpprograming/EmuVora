using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private readonly ContentControl librarySurface = new();
    private readonly ThemeLayoutPanel themeStage = new();
    private readonly Image stageBackground = new() { Stretch = Stretch.UniformToFill, IsHitTestVisible = false };
    private readonly Image stageLogo = new() { Stretch = Stretch.Uniform, IsHitTestVisible = false };
    private readonly ContentControl stageCover = new() { IsHitTestVisible = false };
    private readonly TextBlock stageTitle = new() { TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, IsHitTestVisible = false };
    private readonly TextBlock stageDescription = new() { TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, IsHitTestVisible = false };
    private readonly Dictionary<string, Bitmap> stageDecorationImages = new(StringComparer.OrdinalIgnoreCase);
    private bool IsThemeLayout => settings.LibraryLayout == "Theme";
    private void MountLibrarySurface()
    {
        librarySurface.Content = null; themeStage.RemoveSlot(gamesList);
        if (IsThemeLayout)
        {
            if (themeStage.Children.Count == 0)
            {
                themeStage.AddSlot("background", stageBackground); themeStage.AddSlot("logo", stageLogo);
                themeStage.AddSlot("md_image", stageCover); themeStage.AddSlot("md_name", stageTitle); themeStage.AddSlot("md_description", stageDescription);
            }
            themeStage.AddSlot("gamelist", gamesList); librarySurface.Content = themeStage;
        }
        else librarySurface.Content = gamesList;
    }
    private void ClearStageDecorations()
    {
        ThemeDecorationRenderer.Clear(themeStage); foreach (var bitmap in stageDecorationImages.Values) bitmap.Dispose(); stageDecorationImages.Clear();
    }
    private void ClearThemeStageImages() { ClearStageDecorations(); stageBackground.Source = stageLogo.Source = null; stageCover.Content = null; }
    private void ResetThemeStage()
    {
        ClearThemeStageImages(); themeStage.Model = ThemeLayout.From(null); themeStage.Background = Background;
        if (IsThemeLayout) { gamesList.Background = Background; themeStatus.Text = "Theme-Layout · kein aktives Layout; native Liste"; }
    }
    private void ApplyThemeStage(EmulationStationTheme theme, Func<ThemeElement?, Bitmap?> imageFor, Func<ThemeElement?, string, IBrush?> colorFor)
    {
        if (!IsThemeLayout) return;
        ThemeElement? Find(string name) => theme.Elements.LastOrDefault(e => e.Name == name);
        themeStage.Model = ThemeLayout.From(theme);
        ClearStageDecorations();
        var decorationNotes = ThemeDecorationRenderer.Replace(themeStage, element =>
        {
            var path = element.Properties.GetValueOrDefault("path"); if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            if (!stageDecorationImages.TryGetValue(path, out var bitmap)) { bitmap = ThemeArtwork.LoadDecoration(path); stageDecorationImages[path] = bitmap; } return bitmap;
        }, FontFamily, Foreground ?? Brushes.White, settings.TvMode ? 17 : 14);
        stageBackground.Source = imageFor(Find("background")); stageLogo.Source = imageFor(Find("logo"));
        stageTitle.FontSize = settings.TvMode ? 22 : 19; stageDescription.FontSize = settings.TvMode ? 17 : 14;
        stageTitle.FontFamily = stageDescription.FontFamily = FontFamily;
        stageTitle.Foreground = colorFor(Find("md_name"), "color") ?? Foreground;
        stageDescription.Foreground = colorFor(Find("md_description") ?? Find("description"), "color") ?? Foreground;
        themeStage.Background = colorFor(Find("canvas") ?? Find("background"), "color") ?? Background;
        gamesList.Background = themeStage.Background;
        themeBackground.Source = null; themeLogo.IsVisible = false;
        themeStatus.Text = "Theme-Layout · " + (themeStage.Model.Usable ? themeStage.Model.Elements.Count + " zugeordnete Elemente" : "native Ersatzliste") + "\nKleine Ansichten verwenden bei Bedarf die native Liste.";
        if (decorationNotes.Count > 0) themeStatus.Text += "\n" + string.Join("\n", decorationNotes.Take(3));
    }
    private void UpdateThemeStageSelection(Game? game)
    {
        if (!IsThemeLayout) return;
        stageTitle.Text = game?.Title ?? "Spiel auswählen";
        stageDescription.Text = game == null ? "" : $"{game.System}\n{game.PlayCount} Starts · " + (game.LastPlayed is {} last ? last.ToLocalTime().ToString("g") : "Noch nicht gespielt");
        stageCover.Content = game == null ? null : Artwork(game, 260);
    }
}
