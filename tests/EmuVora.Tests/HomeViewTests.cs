using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;

public class HomeViewTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(MainWindow window, string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(window)!;
    private static void Call(MainWindow window, string name) => typeof(MainWindow).GetMethod(name, Private)!.Invoke(window, null);
    private static void Render(Window w) { w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static void Click(Window w, string label) => w.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static MainWindow Open(TestDirectory dir, string palette = "Midnight", int width = 1320, bool tv = false, string layout = "Grid", bool startup = false, int count = 64)
    {
        var games = Enumerable.Range(0, count).Select(i => new Game($"Adventure {i:0000}", "SNES", $"D:/fixture/{i}.sfc", i % 2 == 0, DateTimeOffset.Parse("2026-10-01T12:00:00Z").AddHours(i), i)).ToArray();
        var settings = new AppSettings { Theme = palette, TvMode = tv, LibraryLayout = layout, HomeOnStartup = startup, PreferGerman = false, OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false };
        for (var i = 0; i < 10; i++) settings.Collections["Collection " + i] = games.Where((_, index) => index % 10 == i).Select(g => g.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 3; i++)
        {
            var artwork = new Border { Width = 200, Height = 280, Background = Brush.Parse(new[] { "#315879", "#583E67", "#356359" }[i]), Child = new TextBlock { Text = "EMU\nVORA", FontSize = 34, FontWeight = FontWeight.Bold, Foreground = Brushes.White, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center } };
            artwork.Measure(new(200, 280)); artwork.Arrange(new(0, 0, 200, 280)); var file = Path.Combine(dir.Path, $"cover-{i}.png"); using (var bitmap = new RenderTargetBitmap(new(200, 280), new(96, 96))) { bitmap.Render(artwork); bitmap.Save(file); }
            foreach (var game in games.Where((_, index) => index % 3 == i)) settings.Covers[game.Path] = file;
        }
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(settings); new LibraryStore(Path.Combine(dir.Path, "library.db")).Restore(games);
        var window = new MainWindow(dir.Path) { Width = width, Height = tv ? 900 : 820 }; window.Show(); Render(window); return window;
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void HomeModelKeepsRealHistoryFavoritesAndCollectionCounts(bool group)
    {
        var date = DateTimeOffset.Parse("2026-10-01T12:00:00Z"); var a = new Game("Same", "GBA", "a.gba", false, date, 8); var b = new Game("same", "GBA", "b.zip", true, date.AddDays(-1), 4); var c = new Game("Same (USA)", "GBA", "c.gba"); var d = new Game("Same", "GBC", "d.gbc");
        var settings = new AppSettings { GroupGameVersions = group, Collections = new() { ["Mixed"] = new() { "A.GBA", "b.zip", "c.gba", "deleted" } } };
        var data = HomePresentation.Create([a, b, c, d], settings); Assert.Equal(group ? 3 : 4, data.Titles); Assert.Equal(4, data.Files); Assert.Equal(2, data.Systems);
        Assert.Equal(group ? 1 : 2, data.Recent.Count); Assert.Equal(a, data.Recent[0]); Assert.Equal(b, Assert.Single(data.Favorites)); Assert.Equal(group ? 2 : 3, Assert.Single(data.Collections).Titles); Assert.Equal(3, data.Collections[0].Files);
        Assert.Equal(8, a.PlayCount); Assert.Contains("deleted", settings.Collections["Mixed"]);
    }
    [Fact] public void ExistingSettingsKeepLibraryStartupAndHomeOptionsRoundTrip()
    {
        using var dir = new TestDirectory(); var store = new SettingsStore(dir.File("settings.json", "{}")); var settings = store.Load(); Assert.False(settings.HomeOnStartup); Assert.True(settings.HomeShowRecent && settings.HomeShowFavorites && settings.HomeShowCollections);
        settings.HomeOnStartup = true; settings.HomeShowRecent = settings.HomeShowFavorites = settings.HomeShowCollections = false; store.Save(settings); var loaded = store.Load(); Assert.True(loaded.HomeOnStartup); Assert.False(loaded.HomeShowRecent || loaded.HomeShowFavorites || loaded.HomeShowCollections);
    }
    [AvaloniaTheory][InlineData("Midnight", 1320, false, "Grid")][InlineData("Classic Light", 860, false, "List")][InlineData("Studio", 1400, true, "Carousel")][InlineData("Aurora", 1320, false, "Theme")]
    public async Task HomePreservesLibraryFiltersPageSelectionAndLayout(string palette, int width, bool tv, string layout)
    {
        using var dir = new TestDirectory(); var window = Open(dir, palette, width, tv, layout); var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll(); var settings = new SettingsStore(Path.Combine(dir.Path, "settings.json")); var covers = settings.Load().Covers;
        try
        {
            var search = Field<TextBox>(window, "search"); var list = Field<ListBox>(window, "gamesList"); search.Text = "Adventure"; Render(window);
            typeof(MainWindow).GetMethod("ChangeLibraryPage", Private)!.Invoke(window, [1]); list.SelectedIndex = 3; Render(window); var selected = Assert.IsType<Game>(list.SelectedItem); Assert.Equal(1, Field<int>(window, "libraryPage"));
            Call(window, "ShowHome"); Render(window); Assert.True(Field<ScrollViewer>(window, "homeSurface").IsEffectivelyVisible); Assert.False(Field<Grid>(window, "libraryArea").IsEffectivelyVisible); Assert.False(Field<Border>(window, "detailBorder").IsEffectivelyVisible);
            Assert.Equal(12, window.GetVisualDescendants().OfType<Button>().Count(b => b.Classes.Contains("home-game"))); Assert.Equal(8, window.GetVisualDescendants().OfType<Button>().Count(b => b.Classes.Contains("home-collection")));
            Assert.All(window.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("home-game")), button => Assert.Contains(button.GetVisualDescendants(), control => control is Image));
            var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); using var frame = window.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, $"Home-{palette}-{width}-{layout}.png")); }
            window.Width = 860; Render(window); Assert.All(Field<List<Avalonia.Controls.Primitives.UniformGrid>>(window, "homeGameGrids"), grid => Assert.Equal(2, grid.Columns));
            await window.HandleLibraryControllerAsync(0x2000); Render(window); Assert.False(Field<ScrollViewer>(window, "homeSurface").IsVisible); Assert.Equal(selected.Path, Assert.IsType<Game>(list.SelectedItem).Path); Assert.Equal(1, Field<int>(window, "libraryPage")); Assert.Equal("Adventure", search.Text);
            Assert.Equal(layout, settings.Load().LibraryLayout); Assert.Equal(covers, settings.Load().Covers); Assert.Equal(before, store.GetAll());
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory][InlineData(0)][InlineData(6000)]
    public void HomeIsBoundedAndEmptyLibraryRemainsUsable(int count)
    {
        using var dir = new TestDirectory(); var window = Open(dir, startup: true, count: count); var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll();
        try
        {
            Assert.True(Field<ScrollViewer>(window, "homeSurface").IsEffectivelyVisible); Assert.False(Field<Grid>(window, "libraryFilters").IsEffectivelyVisible);
            Assert.Equal(count == 0 ? 0 : 12, window.GetVisualDescendants().OfType<Button>().Count(b => b.Classes.Contains("home-game"))); Assert.Equal(8, window.GetVisualDescendants().OfType<Button>().Count(b => b.Classes.Contains("home-collection")));
            Assert.Equal(0, Field<ListBox>(window, "gamesList").ItemCount); Assert.True(window.GetVisualDescendants().Count() < 3000); Assert.Equal(before, store.GetAll());
            if (count == 0) Assert.Contains(window.GetVisualDescendants().OfType<Button>(), b => b.Content as string == "Ersten ROM-Ordner importieren");
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory][InlineData(true)][InlineData(false)] public async Task HomeCardControllerActivationOpensDetailsAndCollectionsUseExistingFilter(bool startup)
    {
        using var dir = new TestDirectory(); var window = Open(dir, startup: startup); var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll();
        try
        {
            if (!startup) { Field<TextBox>(window, "search").Text = "Adventure 0001"; Render(window); Call(window, "ShowHome"); Render(window); }
            var cards = window.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("home-game")).ToArray(); Assert.Equal(12, cards.Length); Assert.True(cards[0].Focus()); await window.HandleLibraryControllerAsync(0x1000);
            Assert.True(Field<ListBox>(window, "gamesList").SelectedItem != null, "Immediate selection missing; page=" + Field<int>(window, "libraryPage") + ", items=" + Field<ListBox>(window, "gamesList").ItemCount + ", view=" + Field<string>(window, "view")); Render(window);
            Assert.False(Field<ScrollViewer>(window, "homeSurface").IsVisible); Assert.Equal("Adventure 0063", Assert.IsType<Game>(Field<ListBox>(window, "gamesList").SelectedItem).Title); Assert.Empty(Field<Dictionary<string, System.Diagnostics.Process>>(window, "running"));
            Call(window, "ShowHome"); Render(window); var collection = window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("home-collection")); collection.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Render(window);
            Assert.Equal("Collection 0", Field<ComboBox>(window, "collectionChoice").SelectedItem); Assert.All(Field<ListBox>(window, "gamesList").Items.Cast<Game>(), g => Assert.Contains(g.Path, new SettingsStore(Path.Combine(dir.Path, "settings.json")).Load().Collections["Collection 0"])); Assert.Equal(before, store.GetAll());
        }
        finally { window.Close(); }
    }
    [AvaloniaFact] public async Task ModuleDialogCancelKeepsSettingsAndSavingAllHiddenPersistsStartup()
    {
        using var dir = new TestDirectory(); var window = Open(dir, startup: true); var file = Path.Combine(dir.Path, "settings.json"); var before = File.ReadAllBytes(file);
        try
        {
            async Task<Window> Dialog()
            {
                Click(window, "Startseite anpassen"); for (var i = 0; i < 100 && !window.OwnedWindows.Any(); i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(10); } var dialog = Assert.Single(window.OwnedWindows); Render(dialog); return dialog;
            }
            var cancel = await Dialog(); foreach (var option in cancel.GetVisualDescendants().OfType<CheckBox>()) option.IsChecked = false; Click(cancel, "Abbrechen"); Render(window); Assert.Equal(before, File.ReadAllBytes(file));
            var save = await Dialog(); foreach (var option in save.GetVisualDescendants().OfType<CheckBox>()) option.IsChecked = option.Content as string == "EmuVora mit der Startseite öffnen";
            var apply = save.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Speichern"); Assert.DoesNotContain(apply.GetVisualAncestors(), c => c is ScrollViewer); Click(save, "Speichern"); for (var i = 0; i < 20; i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(5); } Render(window);
            var settings = new SettingsStore(file).Load(); Assert.True(settings.HomeOnStartup); Assert.False(settings.HomeShowRecent || settings.HomeShowFavorites || settings.HomeShowCollections); Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(), b => b.Classes.Contains("home-game") || b.Classes.Contains("home-collection")); Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text?.Contains("Alle Bereiche sind ausgeblendet") == true);
        }
        finally { foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close(); window.Close(); }
    }
    [AvaloniaFact] public void HomeVersionCardOpensItsExactLibraryEntryAndVersionChooser()
    {
        using var dir = new TestDirectory(); var window = Open(dir, count: 4); var store = new LibraryStore(Path.Combine(dir.Path, "library.db"));
        var extra = new Game("Adventure 0000", "SNES", "D:/fixture/alternate.sfc", true, DateTimeOffset.Parse("2026-10-08T12:00:00Z"), 23); store.Restore([extra]); var before = store.GetAll();
        try
        {
            typeof(MainWindow).GetMethod("Reload", Private)!.Invoke(window, [null]); Render(window); Call(window, "ShowHome"); Render(window);
            var button = window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("home-game") && b.Tag as string == extra.Path); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Render(window);
            Assert.Equal(extra.Path, Assert.IsType<Game>(Field<ListBox>(window, "gamesList").SelectedItem).Path); Assert.True(Field<Button>(window, "chooseVersion").IsVisible); Assert.Equal(before, store.GetAll());
            Assert.DoesNotContain(Field<ListBox>(window, "gamesList").Items.Cast<Game>().GroupBy(g => g.Title), group => group.Count() > 1);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact] public void SearchShortcutLeavesHomeAndKeepsKeyboardFocusAfterDeferredNavigation()
    {
        using var dir = new TestDirectory(); var window = Open(dir, startup: true);
        try
        {
            var key = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.F, KeyModifiers = KeyModifiers.Control }; window.RaiseEvent(key); Render(window);
            Assert.True(key.Handled); Assert.False(Field<ScrollViewer>(window, "homeSurface").IsVisible); Assert.True(Field<TextBox>(window, "search").IsKeyboardFocusWithin);
        }
        finally { window.Close(); }
    }
}
