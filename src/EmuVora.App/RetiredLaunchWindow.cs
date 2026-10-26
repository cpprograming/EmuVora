using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace EmuVora.App;

// A retired command must never execute a ROM, follow its data path, or start a download.
public sealed class RetiredLaunchWindow : Window
{
    public RetiredLaunchWindow()
    {
        Title = "EmuVora · Veraltete Verknüpfung";
        Width = 620; Height = 300; MinWidth = 420; MinHeight = 240;
        var content = new StackPanel { Spacing = 16, Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = "Bitte EmuVora direkt öffnen", FontSize = 24, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = "Diese Verknüpfung gehört zum entfernten ES-DE-Startmodus. Öffne EmuVora über die normale Desktop-Verknüpfung oder EmuVora.App.exe und wähle dein Spiel dort. Bibliothek, Cover und Spielstände bleiben erhalten. EmulationStation-/ES-DE-Themes kannst du weiterhin in EmuVora importieren.", TextWrapping = TextWrapping.Wrap });
        var close = new Button { Content = "Schließen", HorizontalAlignment = HorizontalAlignment.Stretch };
        close.Click += (_, _) => Close(); content.Children.Add(close);
        Content = new ScrollViewer { Content = content }; WindowAppearance.Apply(this); ControllerUi.Attach(this);
    }
}
