using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Controls;
using Avalonia.Media;
namespace EmuVora.App;
public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try{desktop.MainWindow=new MainWindow();}
            catch(Exception ex)
            {
                desktop.MainWindow=new Window {Title="EmuVora – Startproblem",Width=650,Height=300,Content=new TextBlock {Margin=new Thickness(24),TextWrapping=TextWrapping.Wrap,Text="EmuVora konnte die Bibliothek nicht öffnen.\n\n"+ex.Message+"\n\nBitte die Dateien in %LOCALAPPDATA%\\EmuVora sichern und Berechtigungen bzw. library.db prüfen. Vorhandene Daten wurden nicht gelöscht."}};
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
