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

public class LibraryContentUiTests
{
    private static void Render(Window window) { window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static void Click(Window window, string label) => window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task<Window> NextDialog(Window owner)
    {
        for (var i = 0; i < 200 && !owner.OwnedWindows.Any(); i++) { Dispatcher.UIThread.RunJobs(); await Task.Delay(10); }
        var dialog = Assert.Single(owner.OwnedWindows); Render(dialog); return dialog;
    }
    private static MainWindow WindowFor(TestDirectory dir)
    {
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(new() { OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false });
        var window = new MainWindow(dir.Path); window.Show(); Render(window); return window;
    }
    [AvaloniaFact] public async Task PlaylistRequiresReviewAndPreservesIndividualMetadata()
    {
        using var dir = new TestDirectory();
        var games = new[] { new Game("Game (Disc 1)", "PS1", Path.GetFullPath(dir.File("PS1/Game (Disc 1).chd", "one"))), new Game("Game (Disc 2)", "PS1", Path.GetFullPath(dir.File("PS1/Game (Disc 2).chd", "two"))) };
        var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); store.Restore(games.Select(g => g with { Favorite = true, PlayCount = 7, LastPlayed = DateTimeOffset.UtcNow })); var before = store.GetAll().ToArray();
        var group = Assert.Single(LibraryAudit.Inspect(before)); var message = new TextBlock(); var window = WindowFor(dir);
        Task Prepare() => (Task)typeof(MainWindow).GetMethod("PrepareDiscPlaylistAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [group, window, message])!;
        try
        {
            var task = Prepare(); var dialog = await NextDialog(window); var target = Path.Combine(dir.Path, "PS1/Game.m3u"); Assert.False(File.Exists(target));
            Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(), t => t.Text?.Contains("Spielstands-/Memory-Card-Namen") == true);
            Click(dialog, "Abbrechen"); await task; Assert.False(File.Exists(target)); Assert.Equal(before, store.GetAll());
            task = Prepare(); dialog = await NextDialog(window);
            using (var frame = dialog.CaptureRenderedFrame()) { Assert.NotNull(frame); var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null) { Directory.CreateDirectory(output); frame.Save(Path.Combine(output, "PS1-playlist-review.png")); } }
            Click(dialog, "Playlist erstellen und hinzufügen"); await task;
            Assert.True(File.Exists(target)); var after = store.GetAll(); Assert.Equal(3, after.Count); Assert.All(before, game => Assert.Contains(game, after)); Assert.All(games, game => Assert.True(File.Exists(game.Path)));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact] public async Task HashResultIsShownWithoutRemovingDuplicateRows()
    {
        using var dir = new TestDirectory(); var games = new[] { new Game("Game", "PS1", Path.GetFullPath(dir.File("a.chd", "same"))), new Game("Game", "PS1", Path.GetFullPath(dir.File("b.chd", "same"))) };
        var store = new LibraryStore(Path.Combine(dir.Path, "library.db")); store.Upsert(games); var window = WindowFor(dir);
        try
        {
            var options = (Expander)typeof(MainWindow).GetMethod("LibraryAuditOptions", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null)!; window.Content = options; options.IsExpanded = true; Render(window);
            Click(window, "Duplikate und Mehrdisc-Spiele prüfen"); var dialog = await NextDialog(window); dialog.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex = 0;
            Click(dialog, "Ausgewählte Dateien per SHA-256 vergleichen"); var result = await NextDialog(dialog);
            var match = Assert.Single(result.GetVisualDescendants().OfType<ListBox>().Single().Items.Cast<ContentMatch>()); Assert.Equal(2, match.Games.Count); Assert.Equal(2, store.GetAll().Count);
            Click(result, "Schließen"); Click(dialog, "Schließen");
        }
        finally { window.Close(); }
    }
}
