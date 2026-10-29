using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;

public class ThemeDecorationTests
{
    [AvaloniaTheory][InlineData(3000, 1000)][InlineData(32, 4096)]
    public void RasterDecorationsDecodeToBoundedPixelCountAndKeepRatio(int width, int height)
    {
        using var dir = new TestDirectory(); var file = Path.Combine(dir.Path, "decoration.png"); using (var bitmap = new WriteableBitmap(new(width, height), new(96, 96))) bitmap.Save(file);
        using var image = ThemeArtwork.LoadDecoration(file); Assert.True((long)image.PixelSize.Width * image.PixelSize.Height <= 1920L * 1080);
        Assert.InRange(image.Size.Width / image.Size.Height, width / (double)height * .98, width / (double)height * 1.02);
    }
    private static void Render(Window w) { w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static EmulationStationTheme Source()
    {
        var theme = new EmulationStationTheme(); theme.Elements.Add(new("textlist", "gamelist", new() { ["pos"] = ".05 .1", ["size"] = ".4 .8" })); return theme;
    }
    private static ThemeElement Text(string name, string value = "Collection") => new("text", name, new() { ["pos"] = ".5 .1", ["size"] = ".4 .1", ["text"] = value, ["alignment"] = "right", ["zIndex"] = "999", ["opacity"] = ".6", ["color"] = "33AA99" });
    [Fact] public void StaticDecorationsKeepGeometryAndSourceAndDoNotReplaceNativeRoles()
    {
        var theme = Source(); var text = Text("collection-label"); theme.Elements.Add(text); theme.Elements.Add(new("image", "border-art", new() { ["path"] = "fixture.png", ["pos"] = ".8 .6", ["maxSize"] = ".4 0", ["origin"] = "1 .5" }));
        var model = ThemeLayout.From(theme); Assert.True(model.Usable); Assert.Equal(2, model.Decorations.Count);
        var key = Assert.Single(model.Decorations, pair => pair.Value.Name == "border-art").Key; var p = model.Elements[key]; Assert.True(p.ProportionalImage); Assert.Equal(0, p.Height); Assert.Equal(1, p.OriginX);
        var caption = model.Elements[Assert.Single(model.Decorations, pair => pair.Value.Name == text.Name).Key]; Assert.Equal("right", caption.TextAlignment); Assert.Equal(.6, caption.Opacity); Assert.True(caption.Layer < model.Elements["gamelist"].Layer);
        Assert.Equal(".4 0", theme.Elements[2].Properties["maxSize"]); Assert.Contains(model.Notes, n => n.Contains("Statische Dekoration angepasst"));
    }
    [Theory][InlineData("md_genre", "Genre")][InlineData("game-developer", "Developer")][InlineData("dynamic", "${game.name}")][InlineData("empty", "")]
    public void MissingOrDynamicMetadataIsNotInvented(string name, string text)
    {
        var theme = Source(); theme.Elements.Add(Text(name, text)); Assert.Empty(ThemeLayout.From(theme).Decorations);
    }
    [Theory][InlineData("pos", "NaN 0")][InlineData("size", "0 0")][InlineData("pos", "")][InlineData("size", "-1 .2")]
    public void DecorationsRequireExplicitValidGeometry(string key, string value)
    {
        var theme = Source(); var text = Text("label"); text.Properties[key] = value; theme.Elements.Add(text); var model = ThemeLayout.From(theme);
        Assert.True(model.Usable); Assert.Empty(model.Decorations); Assert.Contains(model.Notes, n => n.Contains("ohne gültige Position/Größe"));
    }
    [Fact] public void DecorativeCountIsBoundedAndInvalidElementsDoNotConsumeSlots()
    {
        var theme = Source(); var invalid = Text("bad"); invalid.Properties.Remove("pos"); theme.Elements.Add(invalid);
        for (var i = 0; i < 80; i++) theme.Elements.Add(Text("label-" + i));
        var model = ThemeLayout.From(theme); Assert.Equal(32, model.Decorations.Count); Assert.Equal(33, model.Elements.Count); Assert.Contains(model.Notes, n => n.Contains("Dekorationslimit erreicht"));
    }
    [AvaloniaTheory][InlineData("missing")][InlineData("broken")][InlineData("hidden")]
    public void OptionalBrokenOrHiddenArtDoesNotRemoveTheListOrOtherDecorations(string state)
    {
        using var dir = new TestDirectory(); var theme = Source(); theme.Elements.Add(Text("label", new string('A', 700)));
        var image = new ThemeElement("image", "art", new() { ["path"] = dir.File("broken.svg", "<svg><broken>"), ["pos"] = ".5 .5", ["size"] = ".3 .3", ["visible"] = state == "hidden" ? "false" : "true" }); theme.Elements.Add(image);
        var stage = new ThemeLayoutPanel { Model = ThemeLayout.From(theme) }; var list = new ListBox { ItemsSource = new[] { "Game" } }; stage.AddSlot("gamelist", list); var calls = 0;
        Bitmap? Load(ThemeElement e) { calls++; return state == "missing" ? null : ThemeArtwork.Load(e.Properties["path"]); }
        var notes = ThemeDecorationRenderer.Replace(stage, Load, FontFamily.Default, Brushes.White, 14); Assert.Equal(state == "hidden" ? 0 : 1, calls); Assert.Equal(state == "hidden" ? 0 : 1, notes.Count);
        Assert.Equal(512, Assert.Single(stage.Children.OfType<TextBlock>()).Text!.Length); Assert.Contains(list, stage.Children); Assert.Equal(2, stage.Children.Count);
        ThemeDecorationRenderer.Replace(stage, Load, FontFamily.Default, Brushes.White, 14); Assert.Equal(2, stage.Children.Count);
        var window = new Window { Width = 960, Height = 540, Content = stage }; window.Show(); Render(window);
        try { Assert.False(stage.UsingFallback); Assert.True(list.IsEffectivelyVisible); Assert.All(stage.Children.Where(c => c != list), c => { Assert.False(c.IsHitTestVisible); Assert.True(c.ZIndex < list.ZIndex); }); }
        finally { window.Close(); }
        ThemeDecorationRenderer.Clear(stage); Assert.Single(stage.Children); Assert.Same(list, stage.Children[0]);
    }
    [AvaloniaFact] public void FallbackDoesNotDecodeInvisibleDecorations()
    {
        var theme = Source(); theme.Elements[0].Properties["size"] = ".1 .1"; theme.Elements.Add(Text("label"));
        var stage = new ThemeLayoutPanel { Model = ThemeLayout.From(theme) }; stage.AddSlot("gamelist", new ListBox());
        ThemeDecorationRenderer.Replace(stage, _ => throw new Exception("Should not load"), FontFamily.Default, Brushes.White, 14); Assert.Single(stage.Children);
    }
    [AvaloniaTheory][InlineData("Midnight", 1320, false)][InlineData("Classic Light", 860, false)][InlineData("Studio", 1400, true)]
    public void PreviewAndActiveLibraryShareDecorationsAndLayoutSwitchDoesNotAccumulateThem(string palette, int width, bool tv)
    {
        using var dir = new TestDirectory(); var root = ThemeLayoutTests.Fixture(dir); var xml = Path.Combine(root, "theme.xml");
        File.WriteAllText(xml, File.ReadAllText(xml).Replace("</theme>", "<view name='gamelist'><text name='collection-label'><text>MEINE SAMMLUNG</text><pos>.04 .04</pos><size>.42 .08</size><color>33AA99</color><alignment>center</alignment><zIndex>900</zIndex></text><image name='frame-art'><path>logo.svg</path><pos>.56 .92</pos><size>.36 .04</size><opacity>.4</opacity></image><image name='broken-decoration'><path>broken.svg</path><pos>.6 .4</pos><size>.2 .2</size></image></view></theme>"));
        File.WriteAllText(Path.Combine(root, "broken.svg"), "<svg><broken>"); var data = Path.Combine(dir.Path, "data");
        new SettingsStore(Path.Combine(data, "settings.json")).Save(new() { Theme = palette, TvMode = tv, LibraryLayout = "Theme", EmulationStationTheme = root, PreferGerman = false, OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false });
        var store = new LibraryStore(Path.Combine(data, "library.db")); store.Upsert([new("Game", "SNES", "D:/fixture.sfc", true, null, 7)]); var before = store.GetAll(); var images = new List<Bitmap>();
        var window = new MainWindow(data) { Width = width, Height = tv ? 900 : 820 }; window.Show(); Render(window);
        T Field<T>(string name) => (T)typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        try
        {
            var stage = Field<ThemeLayoutPanel>("themeStage"); Assert.False(stage.UsingFallback); Assert.Equal(2, stage.Children.Count(c => c.Tag as string == ThemeLayout.DecorationPrefix));
            Assert.Contains("Dekorationsbild unlesbar", Field<TextBlock>("themeStatus").Text); var label = Assert.Single(stage.Children.OfType<TextBlock>(), t => t.Text == "MEINE SAMMLUNG"); Assert.Equal(TextAlignment.Center, label.TextAlignment);
            var notes = new List<string>(); var sample = (Control)typeof(MainWindow).GetMethod("ThemeSample", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [EmulationStationTheme.Load(root, "SNES"), images, true, notes])!;
            var previewStage = Assert.Single(sample.GetVisualDescendants().OfType<ThemeLayoutPanel>()); Assert.Equal(2, previewStage.Children.Count(c => c.Tag as string == ThemeLayout.DecorationPrefix)); Assert.Single(notes); Assert.Equal(stage.Model.Elements, previewStage.Model.Elements);
            var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); using var frame = window.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, $"Theme-decoration-{palette}-{width}.png")); }
            var choice = Field<ComboBox>("libraryLayoutChoice"); for (var i = 0; i < 3; i++) { choice.SelectedIndex = 0; Render(window); Assert.Empty(Field<Dictionary<string, Bitmap>>("stageDecorationImages")); choice.SelectedIndex = 3; Render(window); Assert.Equal(2, stage.Children.Count(c => c.Tag as string == ThemeLayout.DecorationPrefix)); Assert.Single(Field<Dictionary<string, Bitmap>>("stageDecorationImages")); }
            Assert.Equal(before, store.GetAll()); Assert.Single(Field<ListBox>("gamesList").Items);
        }
        finally { window.Close(); foreach (var image in images) image.Dispose(); }
    }
}
