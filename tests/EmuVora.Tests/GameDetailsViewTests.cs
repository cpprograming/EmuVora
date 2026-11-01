using Avalonia;
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

public class GameDetailsViewTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(MainWindow w, string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(w)!;
    private static void Render(Window w) { w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); w.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static void Click(Control button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Click(Window w, string text) => Click(w.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == text));
    private static async Task Pump(Func<bool> ready) { for (var i = 0; i < 150 && !ready(); i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(10); } Assert.True(ready()); }
    private static MainWindow Open(TestDirectory dir, string theme = "Midnight", int width = 860, bool tv = false, string layout = "Grid", bool empty = false)
    {
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(new() { Theme = theme, TvMode = tv, LibraryLayout = layout, PreferGerman = false, AutomaticSetup = false, OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false });
        if (!empty) new LibraryStore(Path.Combine(dir.Path, "library.db")).Restore([
            new("Adventure Collection – A Very Long Title for Display Testing", "SNES", "D:/fixture/" + new string('a', 100) + "/a.sfc", true, DateTimeOffset.Parse("2026-10-01T12:00:00Z"), 4),
            new("Adventure Collection – A Very Long Title for Display Testing", "SNES", "D:/fixture/alternate.sfc", false, null, 8),
            new("Wii Adventure", "Wii", "D:/fixture/wii.iso", false, null, 2)]);
        var window = new MainWindow(dir.Path) { Width = width, Height = tv ? 900 : 820 }; window.Show(); Render(window);
        if (!empty) { Field<ListBox>(window, "gamesList").SelectedIndex = 0; Render(window); }
        return window;
    }
    private static async Task<Window> Details(MainWindow w)
    {
        Click(Field<Button>(w, "detailsToggle")); await Pump(() => w.OwnedWindows.Any(d => d.Title == "EmuVora · Spieldetails")); var dialog = w.OwnedWindows.Single(d => d.Title == "EmuVora · Spieldetails"); Render(dialog); return dialog;
    }
    private static void Close(MainWindow w) { foreach (var d in w.OwnedWindows.ToArray()) { foreach (var child in d.OwnedWindows.ToArray()) child.Close(); d.Close(); } w.Close(); }
    [AvaloniaTheory][InlineData("Midnight", 1320, false, "Grid")][InlineData("Classic Light", 860, false, "List")][InlineData("Studio", 1400, true, "Carousel")][InlineData("Aurora", 1320, false, "Theme")]
    public async Task SharedDetailsAreReachableAndTechnicalContentStartsCollapsed(string palette, int width, bool tv, string layout)
    {
        using var dir = new TestDirectory(); var window = Open(dir, palette, width, tv, layout); var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll();
        try
        {
            var wide = width >= 1120 && !tv; Assert.Equal(wide, Field<Border>(window, "detailBorder").IsVisible); Assert.Equal(!wide, Field<Grid>(window, "compactGameActions").IsVisible);
            var technical = Field<Expander>(window, "technicalDetails"); Assert.False(technical.IsExpanded);
            Assert.DoesNotContain(technical.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible), c => ReferenceEquals(c, Field<TextBlock>(window, "technicalPath")));
            if (!wide) typeof(MainWindow).GetField("controllerUiMode", Private)!.SetValue(window, true);
            Window target = wide ? window : await Details(window); var scroll = Field<ScrollViewer>(window, "gameDetailsScroll");
            foreach (var name in new[] { "play", "chooseVersion", "favorite", "collectionGame", "manageCover" }) Assert.Contains(scroll, Field<Button>(window, name).GetVisualAncestors());
            Assert.True(Field<Button>(window, "play").IsEffectivelyVisible); Assert.True(Field<Button>(window, "chooseVersion").IsVisible);
            var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); using var frame = target.CaptureRenderedFrame(); frame!.Save(Path.Combine(output, $"Game-details-{palette}-{width}-{layout}.png")); }
            technical.IsExpanded = true; Render(target); Assert.True(Field<TextBlock>(window, "technicalPath").IsEffectivelyVisible); Assert.Contains(new string('a', 100), Field<TextBlock>(window, "technicalPath").Text); Assert.Equal(before, store.GetAll());
            if (!wide) { var close = target.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Zur Bibliothek"); Assert.DoesNotContain(close.GetVisualAncestors(), c => c is ScrollViewer); ControllerUi.Handle(target, 0x2000); await Pump(() => !target.IsVisible); Render(window); Assert.Same(scroll, Field<Border>(window, "detailBorder").Child); Assert.False(Field<bool>(window, "controllerUiMode")); }
        }
        finally { Close(window); }
    }
    [AvaloniaFact] public async Task VersionChooserIsOwnedByDetailsAndUpdatesTheSameControls()
    {
        using var dir = new TestDirectory(); var window = Open(dir); var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll();
        try
        {
            var dialog = await Details(window); var shared = Field<ScrollViewer>(window, "gameDetailsScroll"); Click(Field<Button>(window, "chooseVersion")); await Pump(() => dialog.OwnedWindows.Any()); var versions = Assert.Single(dialog.OwnedWindows); Render(versions);
            var list = Assert.Single(versions.GetVisualDescendants().OfType<ListBox>()); list.SelectedItem = list.Items.Cast<Game>().Single(g => g.Path == "D:/fixture/alternate.sfc"); Click(versions, "Diese Version auswählen"); await Pump(() => !versions.IsVisible); Render(dialog);
            Assert.Equal("D:/fixture/alternate.sfc", Assert.IsType<Game>(Field<ListBox>(window, "gamesList").SelectedItem).Path); Assert.Equal("D:/fixture/alternate.sfc", Field<TextBlock>(window, "technicalPath").Text); Assert.Contains("Als Favorit", Field<Button>(window, "favorite").Content as string);
            Assert.Contains(shared, dialog.GetVisualDescendants()); Assert.Equal(before, store.GetAll());
        }
        finally { Close(window); }
    }
    [AvaloniaFact] public async Task FavoriteInDetailsChangesOnlyTheSelectedFile()
    {
        using var dir = new TestDirectory(); var window = Open(dir); var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); var before = store.GetAll();
        try
        {
            await Details(window); var selected = Assert.IsType<Game>(Field<ListBox>(window, "gamesList").SelectedItem); Click(Field<Button>(window, "favorite")); Render(window); var after = store.GetAll();
            Assert.Equal(before.Select(g => g.Path == selected.Path ? g with { Favorite = !g.Favorite } : g), after); Assert.Equal(selected.Path, Assert.IsType<Game>(Field<ListBox>(window, "gamesList").SelectedItem).Path);
        }
        finally { Close(window); }
    }
    [AvaloniaFact] public async Task RepeatedOpenCloseAndResizeNeverDuplicateSharedControls()
    {
        using var dir = new TestDirectory(); var window = Open(dir);
        try
        {
            var shared = Field<ScrollViewer>(window, "gameDetailsScroll"); for (var i = 0; i < 3; i++) { var dialog = await Details(window); window.Width = 1320; Render(window); Assert.False(Field<Border>(window, "detailBorder").IsVisible); Click(dialog, "Zur Bibliothek"); await Pump(() => !dialog.IsVisible); Render(window); Assert.Same(shared, Field<Border>(window, "detailBorder").Child); Assert.True(Field<Border>(window, "detailBorder").IsVisible); Assert.Single(window.GetVisualDescendants().OfType<Button>(), b => ReferenceEquals(b, Field<Button>(window, "play"))); window.Width = 860; Render(window); }
        }
        finally { Close(window); }
    }
    [AvaloniaFact] public void SelectionAndOperationStateDisableCompactActionsWithoutChangingData()
    {
        using var dir = new TestDirectory(); var window = Open(dir); using var operation = new CancellationTokenSource();
        try
        {
            Assert.True(Field<Button>(window, "quickPlay").IsEnabled); typeof(MainWindow).GetField("operation", Private)!.SetValue(window, operation); typeof(MainWindow).GetMethod("UpdateSelected", Private)!.Invoke(window, null); Assert.False(Field<Button>(window, "quickPlay").IsEnabled); Assert.False(Field<Button>(window, "detailsToggle").IsEnabled); Assert.False(Field<Button>(window, "play").IsEnabled);
            typeof(MainWindow).GetField("operation", Private)!.SetValue(window, null); typeof(MainWindow).GetMethod("UpdateSelected", Private)!.Invoke(window, null); Field<ListBox>(window, "gamesList").SelectedIndex = -1; Render(window); Assert.False(Field<Button>(window, "quickPlay").IsEnabled); Assert.False(Field<Button>(window, "detailsToggle").IsEnabled);
        }
        finally { typeof(MainWindow).GetField("operation", Private)!.SetValue(window, null); Close(window); }
    }
    [AvaloniaFact] public async Task PlayFromDetailsClosesItBeforeReportingAMissingRom()
    {
        using var dir = new TestDirectory(); var window = Open(dir);
        try
        {
            var list = Field<ListBox>(window, "gamesList"); list.SelectedItem = list.Items.Cast<Game>().Single(g => g.System == "Wii"); Render(window); var dialog = await Details(window); Click(Field<Button>(window, "play")); await Pump(() => !dialog.IsVisible); await Pump(() => window.OwnedWindows.Any());
            Assert.Empty(Field<Dictionary<string, System.Diagnostics.Process>>(window, "running")); Assert.DoesNotContain(window.OwnedWindows, w => w.Title == "EmuVora · Spieldetails"); Assert.NotNull(typeof(MainWindow).GetField("operationFailure", Private)!.GetValue(window));
        }
        finally { Close(window); }
    }
}
