using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Controls;
using Avalonia.Media;
using EmuVora.Core;
namespace EmuVora.App;
public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var localLocator=Path.Combine(AppContext.BaseDirectory,"emuvora-location.json");
            var location=new DataLocation(File.Exists(localLocator)?localLocator:DataLocation.BootstrapFile);
            string? saved=null;
            void Start(Window window){desktop.MainWindow=window;window.Show();}
            try
            {
                saved=location.Read();
                if(saved==null)desktop.MainWindow=new LocationWindow(location,Start);
                else {DataLocation.CheckAvailable(saved);desktop.MainWindow=new MainWindow(saved);}
            }
            catch(Exception ex)
            {
                desktop.MainWindow=new LocationWindow(location,Start,saved,ex.Message+"\nVorhandene Daten wurden nicht gelöscht.");
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
