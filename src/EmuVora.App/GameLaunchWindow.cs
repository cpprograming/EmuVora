using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using EmuVora.Core;
using System.Diagnostics;

namespace EmuVora.App;

public sealed class GameLaunchWindow : Window
{
    public GameLaunchWindow(EsDeLaunchRequest request)
    {
        WindowAppearance.Apply(this);ControllerUi.Attach(this);Background=Brush.Parse("#101722");Foreground=Brush.Parse("#EAF0FA");
        var monitor=DisplayDetection.Current();
        Title="EmuVora – Spiel starten";Width=620;Height=420;MinWidth=500;MinHeight=280;WindowStartupLocation=WindowStartupLocation.CenterScreen;RequestedThemeVariant=ThemeVariant.Dark;
        var token=new CancellationTokenSource();var panel=new StackPanel{Margin=new Thickness(26),Spacing=18};
        var status=new TextBlock{Text="Emulator und Spiel werden vorbereitet …",TextWrapping=TextWrapping.Wrap,FontSize=18};var progress=new ProgressBar{IsIndeterminate=true};
        var cancel=new Button{Content="Abbrechen"};cancel.Click+=(_,_)=>token.Cancel();
        var setup=new Button{Content="EmuVora-Einrichtung öffnen",IsVisible=false};
        setup.Click+=(_,_)=>{var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};foreach(var value in new[]{"--data-dir",request.DataDirectory,"--settings"})start.ArgumentList.Add(value);Process.Start(start)?.Dispose();Close();};
        panel.Children.Add(new TextBlock{Text="◈ EmuVora",FontSize=26});panel.Children.Add(status);panel.Children.Add(progress);panel.Children.Add(cancel);panel.Children.Add(setup);Content=new ScrollViewer{Content=panel};
        Opened+=async(_,_)=>
        {
            try
            {
                var updates=new Progress<DownloadProgress>(value=>{status.Text=value.Message;progress.IsIndeterminate=value.Fraction==null;if(value.Fraction is{} fraction)progress.Value=fraction*100;});
                await ManagedGameLauncher.RunAsync(request,Gamepad.Read().FirstOrDefault().Index,()=>Hide(),updates,token.Token,monitor);Close();
            }
            catch(OperationCanceledException){Close();}
            catch(Exception ex)
            {
                progress.IsVisible=false;cancel.Content="Zurück zu ES-DE";cancel.Click+=(_,_)=>Close();setup.IsVisible=true;
                status.Text="Das Spiel konnte nicht gestartet werden.\n\n"+ex.Message;Show();Activate();
                try{File.AppendAllText(Path.Combine(request.DataDirectory,"errors.log"),$"{DateTimeOffset.Now:O} {ex}\n");}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        };
        Closed+=(_,_)=>token.Cancel();
    }
}
