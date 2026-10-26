using Avalonia.Controls;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    // Keep decisions reachable while long paths, notes and previews scroll independently.
    private Window LibraryDialog(string title, Control body, StackPanel actions)
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
            if (operation == null) return;
            e.Cancel = true;
            RequestOperationCancel();
        };
        return dialog;
    }
}
