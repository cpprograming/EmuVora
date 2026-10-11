using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EmuVora.Core;

namespace EmuVora.App;

public sealed class LocationWindow : Window
{
    public LocationWindow(DataLocation location, Action<Window> start, string? saved = null, string? error = null)
    {
        Title = "EmuVora – Speicherort wählen"; Width = 700; Height = 470; MinWidth = 560; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var panel = new StackPanel { Margin = new Thickness(28), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = "Wo soll deine Spielezentrale liegen?", FontSize = 26, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = "Hier speichert EmuVora Bibliothek, Downloads, Emulatoren, Cores, BIOS, Cover, Themes und verwaltete Spielstände. Wähle einen Ordner auf deiner gewünschten Festplatte. ROMs bleiben in den von dir importierten Ordnern.", TextWrapping = TextWrapping.Wrap });
        var path = new TextBox { Text = saved ?? (File.Exists(Path.Combine(DataLocation.LegacyDirectory, "library.db")) ? DataLocation.LegacyDirectory : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EmuVora")) };
        panel.Children.Add(path);
        var browse = new Button { Content = "Festplatte / Ordner auswählen …" };
        browse.Click += async (_, _) => { var folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = "EmuVora-Datenordner", AllowMultiple = false }); if (folders.Count > 0) path.Text = folders[0].TryGetLocalPath(); };
        panel.Children.Add(browse);
        var message = new TextBlock { Text = error ?? "Vorhandene Bibliothek? Wähle ihren bisherigen Datenordner. Ein neuer Ordner startet eine neue Bibliothek; bestehende Daten werden nicht gelöscht.", TextWrapping = TextWrapping.Wrap }; panel.Children.Add(message);
        var open = new Button { Content = "Diesen Speicherort verwenden", HorizontalAlignment = HorizontalAlignment.Stretch };
        open.Click += (_, _) =>
        {
            try
            {
                var full = DataLocation.Prepare(path.Text?.Trim().Trim('"') ?? "");
                // Open successfully before committing the locator. A broken database stays recoverable.
                var window = new MainWindow(full);
                try { location.Select(full); } catch { window.Close(); throw; }
                start(window); Close();
            }
            catch (Exception ex) { message.Text = "Speicherort konnte nicht geöffnet werden: " + ex.Message; }
        };
        panel.Children.Add(open); Content = new ScrollViewer { Content = panel };
    }
}
