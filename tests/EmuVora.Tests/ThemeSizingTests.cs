using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Reflection;
using EmuVora.App;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class ThemeSizingTests
{
    private static void Render(Window window) { window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static EmulationStationTheme Source(string size, string alignment = "left", string vertical = "top")
    {
        var theme = new EmulationStationTheme();
        theme.Elements.Add(new("textlist", "gamelist", new() { ["pos"] = ".02 .02", ["size"] = ".3 .9" }));
        theme.Elements.Add(new("image", "md_image", new() { ["pos"] = ".8 .8", ["maxSize"] = size, ["origin"] = "1 1" }));
        theme.Elements.Add(new("text", "md_name", new() { ["pos"] = ".4 .02", ["size"] = ".5 .2", ["alignment"] = alignment, ["verticalAlignment"] = vertical }));
        return theme;
    }
    [Theory]
    [InlineData("0 .5")][InlineData(".5 0")][InlineData(".5 .5")]
    public void ImageAxesKeepOriginalSizeWithoutFallback(string size)
    {
        var theme = Source(size); var layout = ThemeLayout.From(theme); Assert.True(layout.Usable);
        Assert.True(layout.Elements["md_image"].ProportionalImage); Assert.DoesNotContain(layout.Notes, n => n.Contains("Ersatzgröße: md_image"));
        Assert.Equal(size, theme.Elements[1].Properties["maxSize"]);
    }
    [Theory]
    [InlineData("0 0")][InlineData("-1 .5")][InlineData("NaN .5")][InlineData(".5 Infinity")][InlineData("3 .5")]
    public void InvalidImageDimensionsRemainBounded(string size)
    {
        var layout = ThemeLayout.From(Source(size)); Assert.True(layout.Usable);
        Assert.Equal(.36, layout.Elements["md_image"].Width); Assert.Equal(.55, layout.Elements["md_image"].Height);
        Assert.Contains(layout.Notes, n => n.Contains("Ersatzgröße: md_image"));
    }
    [AvaloniaTheory]
    [InlineData(".4 0", 400, 200)]
    [InlineData("0 .4", 200, 400)]
    [InlineData(".4 .4", 400, 200)]
    [InlineData(".4 .4", 200, 400)]
    public void ImagesFitTheirIntrinsicRatioAndOriginInPixelSpace(string size, int pixelWidth, int pixelHeight)
    {
        using var bitmap = new WriteableBitmap(new(pixelWidth, pixelHeight), new(96, 96));
        var image = new Image { Source = bitmap, Stretch = Stretch.Uniform }; var slot = new ContentControl { Content = image };
        var stage = new ThemeLayoutPanel { Model = ThemeLayout.From(Source(size)) }; stage.AddSlot("gamelist", new ListBox()); stage.AddSlot("md_image", slot);
        var window = new Window { Width = 960, Height = 540, Content = stage }; window.Show(); Render(window);
        try
        {
            Assert.False(stage.UsingFallback);
            var frameWidth = Math.Min(stage.Bounds.Width, stage.Bounds.Height * stage.Model.AspectRatio); var frameHeight = frameWidth / stage.Model.AspectRatio;
            // Normalized X and Y have different pixel scales. Compute from the constrained physical axis.
            var width = size.StartsWith("0 ") ? frameHeight * .4 * pixelWidth / pixelHeight : frameWidth * .4;
            var height = width * pixelHeight / pixelWidth;
            if (!size.EndsWith(" 0") && height > frameHeight * .4) { height = frameHeight * .4; width = height * pixelWidth / pixelHeight; }
            Assert.Equal(width, slot.Bounds.Width, 4); Assert.Equal(height, slot.Bounds.Height, 4);
            Assert.Equal(pixelWidth / (double)pixelHeight, slot.Bounds.Width / slot.Bounds.Height, 5);
            Assert.Equal((stage.Bounds.Width - frameWidth) / 2 + frameWidth * .8, slot.Bounds.Right, 4);
            Assert.Equal((stage.Bounds.Height - frameHeight) / 2 + frameHeight * .8, slot.Bounds.Bottom, 4);
            Assert.False(slot.IsHitTestVisible);
            // Changing the selected cover must recalculate the automatic axis.
            using var other = new WriteableBitmap(new(pixelHeight, pixelWidth), new(96, 96)); image.Source = other; Render(window);
            Assert.Equal(pixelHeight / (double)pixelWidth, slot.Bounds.Width / slot.Bounds.Height, 5);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData("left", "top", TextAlignment.Left, VerticalAlignment.Top)]
    [InlineData("center", "center", TextAlignment.Center, VerticalAlignment.Center)]
    [InlineData("right", "bottom", TextAlignment.Right, VerticalAlignment.Bottom)]
    [InlineData("unknown", "NaN", TextAlignment.Left, VerticalAlignment.Top)]
    public void TextUsesValidatedAlignmentAndResetsWhenThemeChanges(string horizontal, string vertical, TextAlignment expected, VerticalAlignment expectedVertical)
    {
        var stage = new ThemeLayoutPanel { Model = ThemeLayout.From(Source(".4 .4", horizontal, vertical)) };
        var title = new TextBlock { Text = "A title with multiple words", TextWrapping = TextWrapping.Wrap }; stage.AddSlot("gamelist", new ListBox()); stage.AddSlot("md_name", title);
        var window = new Window { Width = 960, Height = 540, Content = stage }; window.Show(); Render(window);
        try
        {
            Assert.Equal(expected, title.TextAlignment); Assert.Equal(expectedVertical, title.VerticalAlignment); Assert.True(title.ClipToBounds); Assert.False(title.IsHitTestVisible);
            if (horizontal == "unknown") Assert.Contains(stage.Model.Notes, n => n.Contains("Ungültige Textausrichtung"));
            stage.Model = ThemeLayout.From(Source(".4 .4")); Render(window); Assert.Equal(TextAlignment.Left, title.TextAlignment); Assert.Equal(VerticalAlignment.Top, title.VerticalAlignment);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory][InlineData(860, false)][InlineData(1400, true)]
    public void ActiveLibraryResizesCoverAndKeepsSelectionAndMetadata(int width, bool tv)
    {
        using var dir = new TestDirectory(); var root = ThemeLayoutTests.Fixture(dir); var xml = Path.Combine(root, "theme.xml");
        File.WriteAllText(xml, File.ReadAllText(xml).Replace("<size>0.3 0.45</size>", "<size>0.3 0</size>").Replace("<text name='game-name'>", "<text name='game-name'><alignment>right</alignment><verticalAlignment>bottom</verticalAlignment>"));
        var cover = Path.Combine(dir.Path, "cover.png");
        var artwork = new Border { Width = 400, Height = 200, Background = Brushes.SteelBlue, Child = new TextBlock { Text = "LANDSCAPE COVER / 2:1", FontSize = 22, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        artwork.Measure(new(400, 200)); artwork.Arrange(new(0, 0, 400, 200)); using (var bitmap = new RenderTargetBitmap(new(400, 200), new(96, 96))) { bitmap.Render(artwork); bitmap.Save(cover); }
        var data = Path.Combine(dir.Path, "data"); var settings = new SettingsStore(Path.Combine(data, "settings.json"));
        var a = new Game("Covered", "SNES", "D:/fixture/a.sfc"); var b = new Game("Missing", "SNES", "D:/fixture/b.sfc");
        settings.Save(new() { Theme = "Midnight", LibraryLayout = "Theme", TvMode = tv, EmulationStationTheme = root, PreferGerman = false, OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false, Covers = new() { [a.Path] = cover } });
        var store = new LibraryStore(Path.Combine(data, "library.db")); store.Upsert([a, b]); var before = store.GetAll();
        var window = new MainWindow(data) { Width = width, Height = tv ? 900 : 820 }; window.Show(); Render(window);
        T Field<T>(string name) => (T)typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        try
        {
            var list = Field<ListBox>("gamesList"); var slot = Field<ContentControl>("stageCover"); list.SelectedIndex = 0; Render(window);
            Assert.False(Field<ThemeLayoutPanel>("themeStage").UsingFallback); Assert.Equal(2, slot.Bounds.Width / slot.Bounds.Height, 4);
            Assert.Equal(TextAlignment.Right, Field<TextBlock>("stageTitle").TextAlignment); Assert.Equal(VerticalAlignment.Bottom, Field<TextBlock>("stageTitle").VerticalAlignment);
            var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); using var frame = window.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, $"Theme-sizing-{width}.png")); }
            list.SelectedIndex = 1; Render(window); Assert.Equal(2d / 3, slot.Bounds.Width / slot.Bounds.Height, 4); Assert.Equal("Missing", Field<TextBlock>("stageTitle").Text);
            var choice = Field<ComboBox>("libraryLayoutChoice"); choice.SelectedIndex = 0; Render(window); choice.SelectedIndex = 3; Render(window);
            Assert.Equal("Missing", Assert.IsType<Game>(list.SelectedItem).Title); Assert.Equal(before, store.GetAll()); Assert.Equal(cover, settings.Load().Covers[a.Path]);
        }
        finally { window.Close(); }
    }
}
