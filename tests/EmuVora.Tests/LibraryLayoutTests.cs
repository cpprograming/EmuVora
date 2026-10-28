using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;

public class LibraryLayoutTests
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(MainWindow window, string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(window)!;
    private static void Render(Window window) { window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static void Click(Window window, string label) => window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Screenshot(Window window, string name)
    {
        var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output == null) return;
        Directory.CreateDirectory(output); using var frame = window.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, name + ".png"));
    }
    private static MainWindow Open(TestDirectory dir, string theme, int width, bool tv, int count = 128, string layout = "Grid")
    {
        var games = Enumerable.Range(0, count).Select(i => new Game($"Game {i:0000}", "SNES", $"D:/fixture/{i:0000}.sfc", i % 2 == 0, i % 3 == 0 ? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero) : null, i % 12)).ToArray();
        var settings = new AppSettings { Theme = theme, TvMode = tv, LibraryLayout = layout, OnboardingCompleted = true, AutoDownloadCovers = false, CheckUpdatesOnStartup = false, CheckAppUpdatesOnStartup = false, PreferGerman = false };
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(settings);
        new LibraryStore(Path.Combine(dir.Path, "library.db")).Restore(games);
        var window = new MainWindow(dir.Path) { Width = width, Height = tv ? 900 : 820 }; window.Show(); Render(window); return window;
    }
    [AvaloniaTheory]
    [InlineData("Midnight", 1320, false)]
    [InlineData("Classic Light", 860, false)]
    [InlineData("Studio", 1400, true)]
    public async Task SwitchingPreservesPageSelectionQueryAndControllerNavigation(string theme, int width, bool tv)
    {
        using var dir = new TestDirectory(); var window = Open(dir, theme, width, tv);
        var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll();
        try
        {
            var list = Field<ListBox>(window, "gamesList"); var selector = Field<ComboBox>(window, "libraryLayoutChoice");
            Field<TextBox>(window, "search").Text = "Game"; Field<ComboBox>(window, "systems").SelectedItem = "SNES"; Render(window);
            Click(window, "Nächste Seite"); list.SelectedIndex = 5; Render(window);
            var selected = Assert.IsType<Game>(list.SelectedItem); var items = list.Items.Cast<Game>().ToArray(); var page = Field<TextBlock>(window, "pageLabel").Text;
            selector.SelectedIndex = 1; Render(window);
            Assert.Equal("Bibliothekslayout", AutomationProperties.GetName(selector)); Assert.Equal(selected, list.SelectedItem);
            Assert.Equal(items, list.Items.Cast<Game>()); Assert.Equal(page, Field<TextBlock>(window, "pageLabel").Text);
            Assert.Equal("Game", Field<TextBox>(window, "search").Text); Assert.Equal("SNES", Field<ComboBox>(window, "systems").SelectedItem);
            Assert.Equal("List", new SettingsStore(Path.Combine(dir.Path, "settings.json")).Load().LibraryLayout);
            var rows = list.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("library-list-row")).ToArray(); Assert.NotEmpty(rows);
            Assert.All(rows, row => Assert.True(row.Bounds.Width > list.Bounds.Width * .75, $"Row {row.Bounds.Width}, list {list.Bounds.Width}"));
            Screenshot(window, $"Library-list-{theme}-{width}");
            await window.HandleLibraryControllerAsync(2); Render(window); Assert.Equal(6, list.SelectedIndex);
            list.SelectedIndex = 47; await window.HandleLibraryControllerAsync(2); Render(window); Assert.Equal("Game 0096", Assert.IsType<Game>(list.SelectedItem).Title);
            await window.HandleLibraryControllerAsync(1); Render(window); Assert.Equal("Game 0095", Assert.IsType<Game>(list.SelectedItem).Title);
            selector.SelectedIndex = 0; Render(window); Assert.Equal("Game 0095", Assert.IsType<Game>(list.SelectedItem).Title);
            Click(window, "Favoriten"); var favoriteItems = list.Items.Cast<Game>().ToArray(); Assert.All(favoriteItems, g => Assert.True(g.Favorite));
            selector.SelectedIndex = 1; Render(window); Assert.Equal(favoriteItems, list.Items.Cast<Game>());
            Assert.Equal(before, store.GetAll());
        }
        finally { window.Close(); }
    }
    [AvaloniaFact] public void EachLayoutRemembersScrollAndRestoresAfterReopening()
    {
        using var dir = new TestDirectory(); var window = Open(dir, "Aurora", 1320, false);
        try
        {
            var list = Field<ListBox>(window, "gamesList"); var selector = Field<ComboBox>(window, "libraryLayoutChoice");
            list.SelectedIndex = 12; Render(window);
            var scroll = list.GetVisualDescendants().OfType<ScrollViewer>().First(); scroll.Offset = new(0, 280); Render(window); var gridOffset = scroll.Offset;
            selector.SelectedIndex = 1; Render(window);
            Assert.Equal(gridOffset, Field<Dictionary<string, (int Page, string? Selected, Vector Offset)>>(window, "layoutPositions")["Grid"].Offset);
            scroll = list.GetVisualDescendants().OfType<ScrollViewer>().First(); scroll.Offset = new(0, 700); Render(window); var listOffset = scroll.Offset;
            selector.SelectedIndex = 0; Render(window); Assert.Equal(gridOffset, list.GetVisualDescendants().OfType<ScrollViewer>().First().Offset);
            selector.SelectedIndex = 1; Render(window); Assert.Equal(listOffset, list.GetVisualDescendants().OfType<ScrollViewer>().First().Offset);
        }
        finally { window.Close(); }
        window = new MainWindow(dir.Path); window.Show(); Render(window);
        try
        {
            Assert.Equal(1, Field<ComboBox>(window, "libraryLayoutChoice").SelectedIndex);
            var list = Field<ListBox>(window, "gamesList"); Assert.Contains("list-layout", list.Classes);
            list.SelectedIndex = 0; Render(window); list.GetVisualDescendants().OfType<ListBoxItem>().Single(i => i.IsSelected).Focus();
            window.KeyPressQwerty(Avalonia.Input.PhysicalKey.ArrowDown, Avalonia.Input.RawInputModifiers.None); window.KeyReleaseQwerty(Avalonia.Input.PhysicalKey.ArrowDown, Avalonia.Input.RawInputModifiers.None); Render(window);
            Assert.Equal(1, list.SelectedIndex);
            window.KeyPressQwerty(Avalonia.Input.PhysicalKey.End, Avalonia.Input.RawInputModifiers.None); window.KeyReleaseQwerty(Avalonia.Input.PhysicalKey.End, Avalonia.Input.RawInputModifiers.None); Render(window);
            Assert.Equal(47, list.SelectedIndex);
            var selected = list.GetVisualDescendants().OfType<ListBoxItem>().Single(i => i.IsSelected);
            var point = selected.TranslatePoint(new(0, 0), list)!.Value;
            Assert.InRange(point.Y, 0, list.Bounds.Height - selected.Bounds.Height + 1);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory][InlineData("List")][InlineData("Carousel")][InlineData("Theme")] public async Task SixThousandListEntriesStayBoundedAndSearchable(string libraryLayout)
    {
        using var dir = new TestDirectory(); var window = Open(dir, "Midnight", 1320, false, 6000, libraryLayout);
        try
        {
            var list = Field<ListBox>(window, "gamesList"); Assert.Equal(48, list.ItemCount);
            Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(), 1, 48);
            list.SelectedIndex = 47; await window.HandleLibraryControllerAsync(2); Render(window); Assert.Equal("Game 0048", Assert.IsType<Game>(list.SelectedItem).Title);
            Field<TextBox>(window, "search").Text = "Game 5999"; Render(window); Assert.Equal("Game 5999", Assert.IsType<Game>(Assert.Single(list.Items)).Title);
            Field<ComboBox>(window, "libraryLayoutChoice").SelectedIndex = 0; Render(window); Assert.Single(list.Items);
            Field<TextBox>(window, "search").Text = "No such game"; Render(window); Assert.Empty(list.Items);
            Field<ComboBox>(window, "libraryLayoutChoice").SelectedIndex = 1; Render(window); Assert.Empty(list.Items);
        }
        finally { window.Close(); }
    }
    [Fact] public void SharedPresentationFiltersVersionsBeforeGroupingAndFindsSelectedPage()
    {
        var games = Enumerable.Range(0, 130).Select(i => new Game($"Game {i:0000}", "GBA", $"D:/fixture/{i}.gba")).ToArray();
        var other = games[100] with { Path = "D:/alternate.zip", Favorite = true, PlayCount = 20 };
        var all = games.Append(other).ToArray(); var settings = new AppSettings(); var dates = new Dictionary<string, DateTimeOffset>();
        var result = LibraryPresentation.Create(all, settings, dates, new(SelectedPath: other.Path, FindSelectedPage: true));
        Assert.Equal(2, result.Index); Assert.Equal(130, result.TitleCount); Assert.Equal(131, result.FileCount); Assert.Contains(other, result.Games);
        var family = Assert.Single(result.Families, f => f.Representative.Path == other.Path); Assert.Equal(2, family.Versions.Count);
        result = LibraryPresentation.Create(all, settings, dates, new(View: "Favoriten")); Assert.Equal(other, Assert.Single(result.Games)); Assert.Single(Assert.Single(result.Families).Versions);
        settings.Collections["Couch"] = [other.Path]; result = LibraryPresentation.Create(all, settings, dates, new(View: "Sammlung", Collection: "Couch", Page: 100));
        Assert.Equal(0, result.Index); Assert.Equal(other, Assert.Single(result.Games)); Assert.Equal(20, other.PlayCount);
        settings.GroupGameVersions = false; result = LibraryPresentation.Create(all, settings, dates, new()); Assert.Equal(131, result.TitleCount);
    }
}
