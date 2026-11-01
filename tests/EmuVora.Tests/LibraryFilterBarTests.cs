using Avalonia.Controls;
using Avalonia.Automation;
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

public class LibraryFilterBarTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(MainWindow w, string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(w)!;
    private static void Render(Window w) { w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Refresh(MainWindow w) => typeof(MainWindow).GetMethod("Refresh", Private)!.Invoke(w, [null, false]);
    private static MainWindow Open(TestDirectory dir, string palette = "Midnight", int width = 1320, bool tv = false, string layout = "Grid", int count = 128)
    {
        var games = Enumerable.Range(0, count).Select(i => new Game($"Adventure {i:0000}", i % 3 == 0 ? "GBA" : "SNES", $"D:/fixture/{i}.rom", i % 2 == 0, i % 4 == 0 ? DateTimeOffset.Parse("2026-10-01T12:00:00Z").AddHours(i) : null, i)).ToArray();
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(new() { Theme = palette, TvMode = tv, LibraryLayout = layout, PreferGerman = false, OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false, Collections = new() { ["Collection Alpha"] = games.Where((_, i) => i % 5 == 0).Select(g => g.Path).ToHashSet(StringComparer.OrdinalIgnoreCase) } });
        new LibraryStore(Path.Combine(dir.Path, "library.db")).Restore(games); var window = new MainWindow(dir.Path) { Width = width, Height = tv ? 900 : 820 }; window.Show(); Render(window); return window;
    }
    private static void Filter(MainWindow w, bool collection = false)
    {
        Field<TextBox>(w, "search").Text = "Adventure"; Field<ComboBox>(w, "systems").SelectedItem = "SNES"; Field<ComboBox>(w, "sort").SelectedIndex = 1;
        if (collection) Field<ComboBox>(w, "collectionChoice").SelectedItem = "Collection Alpha"; else typeof(MainWindow).GetField("view", Private)!.SetValue(w, "Favoriten"); Refresh(w); Render(w);
    }
    private static Button Chip(MainWindow w, LibraryFilterKind kind) => Field<WrapPanel>(w, "activeFilters").Children.OfType<Button>().Single(b => b.Tag is LibraryFilterKind k && k == kind);
    private static Button Reset(MainWindow w) => Field<WrapPanel>(w, "activeFilters").Children.OfType<Button>().Single(b => b.Classes.Contains("clear-library-filters"));
    [Fact] public void FilterDescriptionsSeparateConstraintsFromSortAndLayout()
    {
        Assert.Empty(LibraryFilters.Describe(new(Search: "  ", System: "Alle Konsolen", Sort: 3)));
        var filters = LibraryFilters.Describe(new(Search: "  Adventure  ", System: "SNES", View: "Sammlung", Collection: "Couch"));
        Assert.Equal(new[] { LibraryFilterKind.Search, LibraryFilterKind.System, LibraryFilterKind.Collection }, filters.Select(f => f.Kind)); Assert.Equal("Suche: Adventure", filters[0].Label); Assert.Equal("Sammlung: Couch", filters[2].Label);
        Assert.Equal(LibraryFilterKind.View, Assert.Single(LibraryFilters.Describe(new(View: "Fehlende Dateien"))).Kind);
    }
    [AvaloniaTheory][InlineData("Midnight", 1320, false, "Grid")][InlineData("Classic Light", 860, false, "List")][InlineData("Studio", 1400, true, "Carousel")][InlineData("Aurora", 1320, false, "Theme")]
    public void ClearAllPreservesLayoutSortSelectionAndStoredData(string palette, int width, bool tv, string layout)
    {
        using var dir = new TestDirectory(); var w = Open(dir, palette, width, tv, layout); var db = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = db.GetAll(); var settings = File.ReadAllBytes(Path.Combine(dir.Path, "settings.json"));
        try
        {
            Filter(w); var list = Field<ListBox>(w, "gamesList"); list.SelectedIndex = 3; Render(w); var selected = Assert.IsType<Game>(list.SelectedItem); Assert.Equal(4, Field<WrapPanel>(w, "activeFilters").Children.OfType<Button>().Count()); Assert.Contains("1 Konsole", Field<TextBlock>(w, "summary").Text);
            var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); using var frame = w.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, $"Library-filters-{palette}-{width}-{layout}.png")); }
            Click(Reset(w)); Render(w); Assert.Equal("", Field<TextBox>(w, "search").Text); Assert.Equal("Alle Konsolen", Field<ComboBox>(w, "systems").SelectedItem); Assert.Equal("Alle Spiele", Field<string>(w, "view")); Assert.Null(Field<ComboBox>(w, "collectionChoice").SelectedItem); Assert.Equal(1, Field<ComboBox>(w, "sort").SelectedIndex);
            Assert.False(Field<WrapPanel>(w, "activeFilters").IsVisible); Assert.Equal(selected.Path, Assert.IsType<Game>(list.SelectedItem).Path); Assert.True(list.ItemCount <= 48); Assert.True(Field<TextBox>(w, "search").IsKeyboardFocusWithin); Assert.Contains("2 Konsolen", Field<TextBlock>(w, "summary").Text);
            Assert.Equal(layout, new SettingsStore(Path.Combine(dir.Path, "settings.json")).Load().LibraryLayout); Assert.Equal(settings, File.ReadAllBytes(Path.Combine(dir.Path, "settings.json"))); Assert.Equal(before, db.GetAll());
        }
        finally { w.Close(); }
    }
    [AvaloniaTheory][InlineData(LibraryFilterKind.Search)][InlineData(LibraryFilterKind.System)][InlineData(LibraryFilterKind.View)][InlineData(LibraryFilterKind.Collection)]
    public void RemovingOneConstraintKeepsTheOthers(LibraryFilterKind kind)
    {
        using var dir = new TestDirectory(); var w = Open(dir);
        try
        {
            Filter(w, kind == LibraryFilterKind.Collection); Click(Chip(w, kind)); Render(w); Assert.Equal(3, Field<WrapPanel>(w, "activeFilters").Children.OfType<Button>().Count());
            Assert.Equal(kind == LibraryFilterKind.Search ? "" : "Adventure", Field<TextBox>(w, "search").Text); Assert.Equal(kind == LibraryFilterKind.System ? "Alle Konsolen" : "SNES", Field<ComboBox>(w, "systems").SelectedItem);
            Assert.Equal(kind is LibraryFilterKind.View or LibraryFilterKind.Collection ? "Alle Spiele" : "Favoriten", Field<string>(w, "view")); Assert.Equal(1, Field<ComboBox>(w, "sort").SelectedIndex);
        }
        finally { w.Close(); }
    }
    [AvaloniaFact] public void MissingResultsUseTheSameResetAndDoNotStealNewSearchFocus()
    {
        using var dir = new TestDirectory(); var w = Open(dir);
        try
        {
            Filter(w, true); Field<TextBox>(w, "search").Text = "missing search"; Render(w); Assert.Equal(0, Field<ListBox>(w, "gamesList").ItemCount); Click(Field<Button>(w, "emptyAction"));
            Field<TextBox>(w, "search").Text = "Adventure 0002"; Field<TextBox>(w, "search").Focus(); Render(w); Assert.Single(Field<ListBox>(w, "gamesList").Items); Assert.Equal("Adventure 0002", Assert.IsType<Game>(Field<ListBox>(w, "gamesList").Items[0]).Title); Assert.True(Field<TextBox>(w, "search").IsKeyboardFocusWithin);
            Assert.Null(Field<ComboBox>(w, "collectionChoice").SelectedItem); Assert.Equal("Alle Spiele", Field<string>(w, "view"));
        }
        finally { w.Close(); }
    }
    [AvaloniaFact] public void SixThousandEntriesKeepTheSelectedFileAndBoundedPageAfterReset()
    {
        using var dir = new TestDirectory(); var w = Open(dir, count: 6000);
        try
        {
            Field<TextBox>(w, "search").Text = "5999"; Render(w); var list = Field<ListBox>(w, "gamesList"); list.SelectedIndex = 0; var selected = Assert.IsType<Game>(list.SelectedItem).Path;
            Click(Reset(w)); Render(w); Assert.Equal(48, list.ItemCount); Assert.True(Field<int>(w, "libraryPage") > 0); Assert.Equal(selected, Assert.IsType<Game>(list.SelectedItem).Path);
        }
        finally { w.Close(); }
    }
    [AvaloniaFact] public async Task ControllerCanRemoveAFilterAndHomeRestoresTheRemainingConstraints()
    {
        using var dir = new TestDirectory(); var w = Open(dir, tv: true);
        try
        {
            Filter(w, true); await w.HandleLibraryControllerAsync(0x20); Chip(w, LibraryFilterKind.System).Focus(); await w.HandleLibraryControllerAsync(0x1000); Render(w); Assert.Equal("Alle Konsolen", Field<ComboBox>(w, "systems").SelectedItem);
            typeof(MainWindow).GetMethod("ShowHome", Private)!.Invoke(w, null); Render(w); Assert.False(Field<WrapPanel>(w, "activeFilters").IsEffectivelyVisible); await w.HandleLibraryControllerAsync(0x2000); Render(w);
            Assert.Equal("Adventure", Field<TextBox>(w, "search").Text); Assert.Equal("Collection Alpha", Field<ComboBox>(w, "collectionChoice").SelectedItem); Assert.Equal("Sammlung", Field<string>(w, "view")); Assert.Equal(3, Field<WrapPanel>(w, "activeFilters").Children.OfType<Button>().Count());
        }
        finally { w.Close(); }
    }
    [AvaloniaFact] public void LongFilterLabelsStayBoundedAndKeepTheirRemovalMarkAndAccessibleName()
    {
        using var dir = new TestDirectory(); var w = Open(dir, width: 860, tv: true);
        try
        {
            var value = new string('L', 400); Field<TextBox>(w, "search").Text = value; Render(w); var button = Chip(w, LibraryFilterKind.Search);
            Assert.InRange(button.Bounds.Width, 1, 300); Assert.Contains(value, AutomationProperties.GetName(button)); var remove = button.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "×");
            Assert.True(remove.IsEffectivelyVisible); Assert.True(remove.Bounds.Width > 0); Assert.True(Field<ListBox>(w, "gamesList").Bounds.Height > 0);
            Click(button); Render(w); Assert.False(Field<WrapPanel>(w, "activeFilters").IsVisible);
        }
        finally { w.Close(); }
    }
    [AvaloniaFact] public void DeferredFilterNotificationsDoNotRebuildTheFinalPageAgain()
    {
        using var dir = new TestDirectory(); var w = Open(dir);
        try
        {
            Filter(w); var list = Field<ListBox>(w, "gamesList"); list.SelectedIndex = 2; Render(w); var selected = Assert.IsType<Game>(list.SelectedItem).Path;
            Click(Reset(w)); var page = list.ItemsSource; Render(w); Assert.Same(page, list.ItemsSource); Assert.Equal(selected, Assert.IsType<Game>(list.SelectedItem).Path);
        }
        finally { w.Close(); }
    }
}
