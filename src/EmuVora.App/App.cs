using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.Styling;
using Avalonia.Controls;
using Avalonia.Media;
using EmuVora.Core;
namespace EmuVora.App;
public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentAvaloniaTheme {PreferSystemTheme=false,PreferUserAccentColor=false});
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit+=(_,_)=>Gamepad.Shutdown();
            if(desktop.Args?.Contains("--esde-launch")==true)
            {
                try{desktop.MainWindow=new GameLaunchWindow(EsDeLaunchRequest.Parse(desktop.Args));}
                catch(Exception ex){desktop.MainWindow=new Window{Title="EmuVora – Spielverweis ungültig",Width=620,Height=280,Content=new TextBlock{Margin=new Thickness(24),TextWrapping=TextWrapping.Wrap,Text=ex.Message}};}
                base.OnFrameworkInitializationCompleted();return;
            }
            var localLocator=Path.Combine(AppContext.BaseDirectory,"emuvora-location.json");
            var location=new DataLocation(File.Exists(localLocator)?localLocator:DataLocation.BootstrapFile);
            string? saved=null;
            void Start(Window window){desktop.MainWindow=window;window.Show();}
            try
            {
                var overrideIndex=Array.IndexOf(desktop.Args??[],"--data-dir");
                saved=overrideIndex>=0&&desktop.Args!.Length>overrideIndex+1?Path.GetFullPath(desktop.Args[overrideIndex+1]):location.Read();
                if(saved==null)desktop.MainWindow=new LocationWindow(location,Start);
                else {DataLocation.CheckAvailable(saved);desktop.MainWindow=new MainWindow(saved,desktop.Args?.Contains("--settings")==true);}
            }
            catch(Exception ex)
            {
                desktop.MainWindow=new LocationWindow(location,Start,saved,ex.Message+"\nVorhandene Daten wurden nicht gelöscht.");
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}

