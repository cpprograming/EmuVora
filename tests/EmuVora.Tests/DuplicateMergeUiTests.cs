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

public class DuplicateMergeUiTests
{
    private static void Render(Window window) { window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static void Click(Window window, string label) => window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    [AvaloniaFact] public async Task ReviewCanCancelAndChooseTheRemainingEntry()
    {
        using var dir = new TestDirectory(); var (data, store, match) = await DuplicateMergeTests.Pair(dir); var window = new MainWindow(data); window.Show(); Render(window); var message = new TextBlock();
        Task<bool> Review() => (Task<bool>)typeof(MainWindow).GetMethod("ReviewDuplicateMergeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [match, window, message])!;
        try
        {
            var task = Review(); var dialog = Assert.Single(window.OwnedWindows); Render(dialog); Click(dialog, "Abbrechen"); Assert.False(await task); Assert.Equal(2, store.GetAll().Count);
            task = Review(); dialog = Assert.Single(window.OwnedWindows); Render(dialog); dialog.GetVisualDescendants().OfType<ComboBox>().First().SelectedItem = match.Games[1]; Render(dialog);
            using (var frame = dialog.CaptureRenderedFrame()) { Assert.NotNull(frame); var folder = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (folder != null) { Directory.CreateDirectory(folder); frame.Save(Path.Combine(folder, "Duplicate-merge-review.png")); } }
            Click(dialog, "Geprüfte Einträge zusammenführen"); Assert.True(await task); Assert.Equal(match.Games[1].Path, Assert.Single(store.GetAll()).Path); Assert.All(match.Games, game => Assert.True(File.Exists(game.Path))); Assert.Contains("Sicherung:", message.Text);
        }
        finally { window.Close(); }
    }
}
