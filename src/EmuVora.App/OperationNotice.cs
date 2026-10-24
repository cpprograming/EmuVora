using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace EmuVora.App;

internal record OperationFailure(string Summary, string Details)
{
    internal static OperationFailure From(Exception error) => new(error switch
    {
        OperationCanceledException or TimeoutException => "Die Verbindung hat zu lange gebraucht. Prüfe deine Verbindung und versuche es erneut.",
        HttpRequestException { StatusCode: System.Net.HttpStatusCode.TooManyRequests } => "Die Download-Quelle begrenzt gerade die Anfragen. Versuche es später erneut.",
        HttpRequestException { StatusCode: System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized } => "Die Download-Quelle verweigert den Zugriff. Versuche es später erneut; Details siehe unten.",
        HttpRequestException => "Die Download-Quelle konnte nicht erreicht werden. Prüfe deine Internetverbindung und versuche es später erneut.",
        UnauthorizedAccessException => "Zugriff verweigert. Prüfe die Berechtigungen und ob eine andere Anwendung die Datei verwendet.",
        FileNotFoundException => "Datei nicht erreichbar. " + error.Message,
        DirectoryNotFoundException => "Ordner nicht erreichbar. Prüfe Laufwerk und Pfad.",
        InvalidDataException => "Die Daten konnten nicht geprüft werden. Bitte die Details beachten.",
        _ => error.Message
    }, error.GetType().Name + ": " + error.Message);
}

internal sealed class OperationNotice : Border
{
    private readonly TextBlock message = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar bar = new() { Height = 5, Minimum = 0, Maximum = 100 };
    private readonly Button cancel = new() { Content = "Vorgang abbrechen", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel running = new() { Spacing = 10 };
    private readonly InfoBar error = new() { Title = "Vorgang fehlgeschlagen", Severity = InfoBarSeverity.Error, IsClosable = false, IsOpen = true };
    private readonly TextBlock details = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Expander detailToggle;
    private readonly StackPanel failed = new() { Spacing = 8 };
    internal OperationNotice(Action cancelAction, Action dismiss, bool showProgress)
    {
        Padding = new Thickness(12); CornerRadius = new CornerRadius(12); Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)); IsVisible = false;
        cancel.Click += (_, _) => cancelAction(); running.Children.Add(message); running.Children.Add(bar); running.Children.Add(cancel);
        detailToggle = new Expander { Header = "Technische Details", Content = new ScrollViewer { MaxHeight = 120, Content = details } };
        var close = new Button { Content = "Fehlermeldung schließen", HorizontalAlignment = HorizontalAlignment.Stretch }; close.Click += (_, _) => dismiss();
        failed.Children.Add(error); failed.Children.Add(detailToggle); failed.Children.Add(close);
        var content = new StackPanel { Spacing = 12 }; if (showProgress) content.Children.Add(running); content.Children.Add(new ScrollViewer { MaxHeight = 260, Content = failed }); Child = content;
        ShowProgress = showProgress;
    }
    private bool ShowProgress { get; }
    internal void Update(bool busy, bool cancelling, string text, double? fraction, OperationFailure? failure)
    {
        IsVisible = ShowProgress && busy || failure != null;
        running.IsVisible = busy; failed.IsVisible = failure != null;
        message.Text = cancelling ? "Vorgang wird abgebrochen …" : string.IsNullOrWhiteSpace(text) ? "Vorgang läuft …" : text;
        bar.IsIndeterminate = fraction == null; bar.Value = fraction is { } value && double.IsFinite(value) ? Math.Clamp(value * 100, 0, 100) : 0;
        cancel.IsEnabled = busy && !cancelling; cancel.Content = cancelling ? "Abbruch läuft …" : "Vorgang abbrechen";
        if (failure?.Details != details.Text) detailToggle.IsExpanded = false;
        error.Message = failure?.Summary ?? ""; details.Text = failure?.Details;
    }
}

public sealed partial class MainWindow
{
    private event Action? FeedbackChanged;
    private OperationFailure? operationFailure;
    private int openedDialogs;
    private void NotifyFeedback() { if (!isClosed) FeedbackChanged?.Invoke(); }
    private OperationNotice CreateOperationNotice(bool showProgress)
    {
        var notice = new OperationNotice(RequestOperationCancel, () => { operationFailure = null; NotifyFeedback(); }, showProgress);
        void Update() => notice.Update(operation != null, operation?.IsCancellationRequested == true, status.Text ?? "", progress.IsIndeterminate ? null : progress.Value / 100, operationFailure);
        FeedbackChanged += Update; notice.DetachedFromVisualTree += (_, _) => FeedbackChanged -= Update;
        Update(); return notice;
    }
    private void RequestOperationCancel()
    {
        var source = operation; if (source == null) return;
        source.Cancel();
        if (ReferenceEquals(source, operation)) { status.Text = "Vorgang wird abgebrochen …"; NotifyFeedback(); }
    }
    private bool CanReport(CancellationTokenSource? source) => !isClosed && source != null && ReferenceEquals(source, operation) && !source.IsCancellationRequested;
    private IProgress<double> OperationPercentage(Action<double> report)
    {
        var source = operation; return new Progress<double>(value => { if (CanReport(source)) report(double.IsFinite(value) ? Math.Clamp(value, 0, 100) : 0); });
    }
}
