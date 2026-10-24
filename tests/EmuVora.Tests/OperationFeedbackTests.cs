using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using FluentAvalonia.UI.Controls;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;

public class OperationFeedbackTests
{
    private static object? Call(MainWindow window, string method, params object?[] args) => typeof(MainWindow).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);
    private static Task<bool> Run(MainWindow window, Func<CancellationToken, Task> action) => (Task<bool>)Call(window, "BusyAsync", action)!;
    private static void Render(Window window) { window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
    private static MainWindow Open(TestDirectory dir)
    {
        new SettingsStore(Path.Combine(dir.Path, "settings.json")).Save(new() { OnboardingCompleted = true, AutoDownloadCovers = false, CheckAppUpdatesOnStartup = false, CheckUpdatesOnStartup = false });
        var window = new MainWindow(dir.Path); window.Show(); Render(window); return window;
    }
    private static void Click(Window window, string text) => window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == text).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    [AvaloniaFact] public async Task SettingsShowsProgressAcrossTabsAndControllerCanCancel()
    {
        using var dir = new TestDirectory(); var window = Open(dir); var settings = (Task)Call(window, "ShowSettingsAsync", null, 0)!; var dialog = Assert.Single(window.OwnedWindows); Render(dialog);
        try
        {
            var task = Run(window, async token => { var report = (IProgress<DownloadProgress>)Call(window, "DownloadProgress")!; report.Report(new("Archiv wird geladen", .42)); await Task.Delay(Timeout.Infinite, token); }); Render(dialog);
            var bar = dialog.GetVisualDescendants().OfType<ProgressBar>().Single(); Assert.False(bar.IsIndeterminate); Assert.Equal(42, bar.Value);
            dialog.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 5; Render(dialog); Assert.True(bar.IsEffectivelyVisible); Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(), t => t.IsEffectivelyVisible && t.Text == "Archiv wird geladen");
            ControllerUi.Handle(dialog, 0x2000); Assert.False(await task); Render(dialog); Assert.False(bar.IsEffectivelyVisible); Assert.DoesNotContain(dialog.GetVisualDescendants().OfType<InfoBar>(), i => i.IsEffectivelyVisible);
            dialog.Close(); await settings;
        }
        finally { foreach (var child in window.OwnedWindows.ToArray()) child.Close(); window.Close(); }
    }
    [AvaloniaTheory] [InlineData("timeout")] [InlineData("network")] [InlineData("permission")] [InlineData("missing")] [InlineData("invalid")] [InlineData("ratelimit")]
    public async Task FailuresStayVisibleInDialogWithDetailsAndCanBeDismissed(string kind)
    {
        using var dir = new TestDirectory(); var window = Open(dir); var dialog = (Window)Call(window, "Dialog", "Prüfung", new TextBlock { Text = "Dialoginhalt" })!; var shown = dialog.ShowDialog(window); Render(dialog);
        try
        {
            Exception failure = kind switch { "timeout" => new TaskCanceledException("HTTP timeout fixture"), "network" => new HttpRequestException("Network fixture"), "ratelimit" => new HttpRequestException("Rate limit fixture", null, System.Net.HttpStatusCode.TooManyRequests), "permission" => new UnauthorizedAccessException("Permission fixture"), "missing" => new FileNotFoundException("Missing fixture"), _ => new InvalidDataException("Invalid archive fixture") };
            Assert.False(await Run(window, _ => Task.FromException(failure))); Render(dialog); var info = Assert.Single(dialog.GetVisualDescendants().OfType<InfoBar>(), i => i.IsEffectivelyVisible); Assert.Equal(InfoBarSeverity.Error, info.Severity); Assert.DoesNotContain("abgebrochen", info.Message);
            if (kind == "timeout") Assert.Contains("lange gebraucht", info.Message);
            if (kind == "ratelimit") Assert.Contains("begrenzt", info.Message);
            var expander = dialog.GetVisualDescendants().OfType<Expander>().Single(); expander.IsExpanded = true; Render(dialog); await Task.Delay(250); Render(dialog); Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(), t => t.Text?.Contains(failure.Message) == true && t.IsEffectivelyVisible && t.Bounds.Height > 0);
            using (var frame = dialog.CaptureRenderedFrame()) { Assert.NotNull(frame); var output = Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS"); if (output != null && kind == "timeout") { Directory.CreateDirectory(output); frame.Save(Path.Combine(output, "Operation-timeout.png")); } }
            Click(dialog, "Fehlermeldung schließen"); Render(dialog); Assert.False(info.IsEffectivelyVisible); Assert.DoesNotContain(window.GetVisualDescendants().OfType<InfoBar>(), i => i.IsEffectivelyVisible && i.Severity == InfoBarSeverity.Error); dialog.Close(); await shown;
        }
        finally { dialog.Close(); window.Close(); }
    }
    [AvaloniaFact] public async Task LateDownloadReportsCannotOverwriteACompletedOrNewOperation()
    {
        using var dir = new TestDirectory(); var window = Open(dir); IProgress<DownloadProgress>? old = null;
        try
        {
            Assert.True(await Run(window, _ => { old = (IProgress<DownloadProgress>)Call(window, "DownloadProgress")!; return Task.CompletedTask; }));
            var status = (TextBlock)typeof(MainWindow).GetField("status", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!; status.Text = "Fertig"; old!.Report(new("Verspätet", .9)); Dispatcher.UIThread.RunJobs(); Assert.Equal("Fertig", status.Text);
            var completion = new TaskCompletionSource(); var current = Run(window, _ => completion.Task); status.Text = "Neue Aufgabe"; old.Report(new("Alt überschreibt neu", .8)); Dispatcher.UIThread.RunJobs(); Assert.Equal("Neue Aufgabe", status.Text); completion.SetResult(); Assert.True(await current);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact] public async Task NestedDialogKeepsParentNavigationStateUntilAllDialogsClose()
    {
        using var dir = new TestDirectory(); var window = Open(dir); var parent = (Window)Call(window, "Dialog", "Parent", new TextBlock())!; var parentTask = parent.ShowDialog(window); var child = (Window)Call(window, "Dialog", "Child", new TextBlock())!; var childTask = child.ShowDialog(parent);
        try
        {
            child.Close(); await childTask; Assert.True((bool)typeof(MainWindow).GetField("dialogOpen", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!);
            parent.Close(); await parentTask; Assert.False((bool)typeof(MainWindow).GetField("dialogOpen", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!);
        }
        finally { child.Close(); parent.Close(); window.Close(); }
    }
    [AvaloniaFact] public async Task UpdatePercentagesAreBoundedAndLateReportsIgnored()
    {
        using var dir = new TestDirectory(); var window = Open(dir); var values = new List<double>(); IProgress<double>? report = null; var completion = new TaskCompletionSource();
        try
        {
            var task = Run(window, _ => { report = (IProgress<double>)Call(window, "OperationPercentage", (Action<double>)(v => values.Add(v)))!; return completion.Task; });
            report!.Report(150); report.Report(double.NaN); Dispatcher.UIThread.RunJobs(); Assert.Equal(new double[] { 100, 0 }, values);
            completion.SetResult(); Assert.True(await task); report.Report(75); Dispatcher.UIThread.RunJobs(); Assert.Equal(2, values.Count);
        }
        finally { completion.TrySetResult(); window.Close(); }
    }
}
