using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;

public class ThemeLayoutTests
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(MainWindow w, string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(w)!;
    private static void Render(Window w) { w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    public static string Fixture(TestDirectory dir)
    {
        var root = ThemeSelectionTests.Fixture(dir); var file = Path.Combine(root, "theme.xml");
        var xml = File.ReadAllText(file).Replace("<textlist name='game-list'>", "<textlist name='game-list'><pos>0.04 0.2</pos><size>0.42 0.74</size><zIndex>2</zIndex>")
            .Replace("<image name='system-logo'>", "<image name='system-logo'><pos>0.6 0.03</pos><size>0.3 0.12</size><zIndex>3</zIndex>")
            .Replace("<text name='game-name'>", "<text name='game-name'><pos>0.55 0.18</pos><size>0.4 0.08</size><zIndex>8</zIndex>");
        xml = xml.Replace("</theme>", "<view name='gamelist'><video name='game-media'><pos>0.6 0.3</pos><size>0.3 0.45</size><zIndex>4</zIndex></video></view></theme>"); File.WriteAllText(file, xml);
        foreach (var name in new[] { "wide.xml", "standard.xml" }) { var path = Path.Combine(root, name); File.WriteAllText(path, File.ReadAllText(path).Replace("<text name='description'>", "<text name='description'><pos>0.55 0.8</pos><size>0.4 0.17</size>")); }
        return root;
    }
    [Fact] public void AdapterReadsNormalizedGeometryAndStaticVideoCoverWithoutChangingSource()
    {
        using var dir = new TestDirectory(); var root = Fixture(dir); var before = File.ReadAllBytes(Path.Combine(root, "theme.xml")); var theme = EmulationStationTheme.Load(root, "SNES"); var model = ThemeLayout.From(theme);
        Assert.True(model.Usable); Assert.Equal(.04, model.Elements["gamelist"].Left); Assert.Equal(.42, model.Elements["gamelist"].Width); Assert.Equal(10000, model.Elements["gamelist"].Layer);
        Assert.Equal("image", theme.Elements.Single(e => e.Name == "md_image").Type); Assert.Contains(theme.Warnings, w => w.Contains("statisches Spielcover"));
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(root, "theme.xml")));
        var p = theme.Elements.Single(e => e.Name == "logo").Properties; p["pos"] = "0.9 0.2"; p["size"] = "0.2 0.1"; p["origin"] = "1 0.5"; p["opacity"] = ".5"; p["zIndex"] = "7"; p["visible"] = "false";
        var logo = ThemeLayout.From(theme).Elements["logo"]; Assert.Equal(.7, logo.Left, 8); Assert.Equal(.15, logo.Top, 8); Assert.Equal(.5, logo.Opacity); Assert.Equal(7, logo.Layer); Assert.False(logo.Visible);
    }
    [Theory]
    [InlineData("NaN 0", ".4 .8", "true", "1")][InlineData(".1 .1", "Infinity .8", "true", "1")]
    [InlineData(".9 .1", ".4 .8", "true", "1")][InlineData(".1 .1", ".1 .8", "true", "1")]
    [InlineData(".1 .1", ".4 0", "true", "1")][InlineData(".1 .1", ".4 .8", "false", "1")][InlineData(".1 .1", ".4 .8", "true", ".1")]
    public void UnusableListGeometryUsesNativeFallback(string pos, string size, string visible, string opacity)
    {
        var theme = new EmulationStationTheme(); theme.Elements.Add(new("textlist", "gamelist", new() { ["pos"] = pos, ["size"] = size, ["visible"] = visible, ["opacity"] = opacity }));
        var model = ThemeLayout.From(theme); Assert.False(model.Usable); Assert.Contains(model.Notes, n => n.Contains("native Liste"));
    }
    [AvaloniaFact] public void PanelAppliesOriginLayersAndLetterboxingAndProtectsInteractiveList()
    {
        using var dir = new TestDirectory(); var theme = EmulationStationTheme.Load(Fixture(dir), "SNES"); var model = ThemeLayout.From(theme);
        var panel = new ThemeLayoutPanel { Model = model }; var list = new ListBox { ItemsSource = new[] { "One", "Two" } }; var logo = new Border(); panel.AddSlot("gamelist", list); panel.AddSlot("logo", logo);
        var window = new Window { Width = 800, Height = 500, Content = panel }; window.Show(); Render(window);
        try
        {
            Assert.False(panel.UsingFallback); var width = Math.Min(panel.Bounds.Width, panel.Bounds.Height * model.AspectRatio); var height = width / model.AspectRatio;
            Assert.Equal((panel.Bounds.Width - width) / 2 + .04 * width, list.Bounds.X, 5); Assert.Equal((panel.Bounds.Height - height) / 2 + .2 * height, list.Bounds.Y, 5);
            Assert.Equal(.42 * width, list.Bounds.Width, 5); Assert.Equal(.74 * height, list.Bounds.Height, 5); Assert.True(list.IsHitTestVisible); Assert.False(logo.IsHitTestVisible); Assert.True(list.ZIndex > logo.ZIndex);
            window.Width = 320; window.Height = 180; Render(window); Assert.True(panel.UsingFallback); Assert.True(list.IsVisible); Assert.False(logo.IsVisible); Assert.True(list.Bounds.Right <= panel.Bounds.Width + 1);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory][InlineData("Midnight", 1320, false)][InlineData("Classic Light", 860, false)][InlineData("Studio", 1400, true)]
    public async Task ThemeViewKeepsRealSelectionMetadataAndNavigationAcrossNativeLayouts(string palette, int width, bool tv)
    {
        using var dir = new TestDirectory(); var theme = Fixture(dir); var data = Path.Combine(dir.Path, "data"); Directory.CreateDirectory(data);
        var games = Enumerable.Range(0, 64).Select(i => new Game($"Game {i:0000}", "SNES", $"D:/fixture/{i}.sfc", i % 2 == 0, null, i)).ToArray();
        var saved = new SettingsStore(Path.Combine(data, "settings.json")); saved.Save(new() { LibraryLayout = "Theme", Theme = palette, TvMode = tv, EmulationStationTheme = theme, PreferGerman = false, OnboardingCompleted = true, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false, AutoDownloadCovers = false });
        var store = new LibraryStore(Path.Combine(data, "library.db")); store.Restore(games); var before = store.GetAll(); var window = new MainWindow(data) { Width = width, Height = tv ? 900 : 820 }; window.Show(); Render(window);
        try
        {
            var list = Field<ListBox>(window, "gamesList"); var stage = Field<ThemeLayoutPanel>(window, "themeStage"); Assert.False(stage.UsingFallback); Assert.Same(stage, list.Parent); Assert.Equal(48, list.ItemCount);
            list.SelectedIndex = 5; Render(window); Assert.Equal("Game 0005", Field<TextBlock>(window, "stageTitle").Text); Assert.Contains("5 Starts", Field<TextBlock>(window, "stageDescription").Text);
            var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); using var frame = window.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, $"Theme-layout-{palette}-{width}.png")); }
            list.GetVisualDescendants().OfType<ListBoxItem>().Single(i => i.IsSelected).Focus(); window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); Render(window); Assert.Equal(6, list.SelectedIndex);
            list.SelectedIndex = 47; await window.HandleLibraryControllerAsync(2); Render(window); Assert.Equal("Game 0048", Assert.IsType<Game>(list.SelectedItem).Title);
            var selected = list.SelectedItem; var chooser = Field<ComboBox>(window, "libraryLayoutChoice"); foreach (var layout in new[] { 0, 1, 2, 3 }) { chooser.SelectedIndex = layout; Render(window); Assert.Equal(selected, list.SelectedItem); }
            Field<TextBox>(window, "search").Text = "Game 0063"; Render(window); Assert.Single(list.Items); Assert.Equal("Game 0063", Assert.IsType<Game>(list.Items[0]).Title); Assert.Equal(before, store.GetAll()); Assert.Equal("Theme", saved.Load().LibraryLayout);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory][InlineData("none")][InlineData("missing")][InlineData("malformed")][InlineData("broken-image")]
    public void MissingOrBrokenThemesKeepTheNativeLibraryUsable(string state)
    {
        using var dir = new TestDirectory(); string? theme = null;
        if (state != "none") theme = Path.Combine(dir.Path, "theme");
        if (state == "malformed") dir.File("theme/theme.xml", "<theme><broken>");
        if (state == "broken-image") { theme = Fixture(dir); File.WriteAllText(Path.Combine(theme, "logo.svg"), "<svg><broken>"); }
        var settings = new SettingsStore(Path.Combine(dir.Path, "settings.json")); settings.Save(new() { LibraryLayout = "Theme", EmulationStationTheme = theme, PreferGerman = false, OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false });
        var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); store.Upsert([new("Fixture", "GBA", "D:/fixture.gba", true, null, 9)]); var before = store.GetAll(); var window = new MainWindow(dir.Path); window.Show(); Render(window);
        try
        {
            var list = Field<ListBox>(window, "gamesList"); Assert.Single(list.Items); Assert.True(Field<ThemeLayoutPanel>(window, "themeStage").UsingFallback); Assert.True(list.IsEffectivelyVisible);
            Field<ComboBox>(window, "libraryLayoutChoice").SelectedIndex = 0; Render(window); Assert.Single(list.Items); Assert.Equal(before, store.GetAll());
        }
        finally { window.Close(); }
    }
}
