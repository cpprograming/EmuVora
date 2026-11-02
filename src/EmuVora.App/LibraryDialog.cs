using Avalonia.Controls;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private static void DialogSaveError(TextBlock feedback,string title,Exception error)
    {
        var text=error is UnauthorizedAccessException or IOException
            ? "Die Datei konnte nicht gespeichert werden. Prüfe Laufwerk, Schreibrechte und ob eine andere Anwendung sie verwendet."
            : error.Message;
        feedback.Text=title+": "+text;ToolTip.SetTip(feedback,error.GetType().Name+": "+error.Message);
    }
    // Keep decisions reachable while long paths, notes and previews scroll independently.
    private Window LibraryDialog(string title, Control body, StackPanel actions, bool cancelBeforeClosing = true)
    {
        body.Margin = new(0);
        var layout = new Grid { RowDefinitions = new("*,Auto"), Margin = new(24) };
        layout.Children.Add(new ScrollViewer { Content = body });
        actions.Margin = new(0, 16, 0, 0);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        var dialog = FixedDialog(title, layout);
        dialog.Width = 760;
        dialog.Height = 720;
        dialog.Closing += (_, e) =>
        {
            if (!cancelBeforeClosing || operation == null) return;
            e.Cancel = true;
            RequestOperationCancel();
        };
        // Decision previews can be awaited by a download operation after the file is complete.
        if (!cancelBeforeClosing) ControllerUi.SetBack(dialog,dialog.Close);
        return dialog;
    }
}
