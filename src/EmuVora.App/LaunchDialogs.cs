using Avalonia.Controls;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;
public sealed partial class MainWindow
{
    private Window? errorDialog;
    private TextBlock? errorSummary,errorDetails;
    private Window Alert(string title,StackPanel panel)
    {
        var dialog=new Window{Title=title,Width=620,Height=430,MinWidth=400,MinHeight=240,Content=new ScrollViewer{Content=panel},Background=Background,Foreground=Foreground,RequestedThemeVariant=RequestedThemeVariant,WindowStartupLocation=WindowStartupLocation.CenterOwner,FontSize=FontSize};
        WindowAppearance.Apply(dialog);ControllerUi.Attach(dialog);ControllerUi.SetBack(dialog,dialog.Close);
        dialog.Opened+=(_,_)=>{openedDialogs++;dialogOpen=true;};dialog.Closed+=(_,_)=>{openedDialogs=Math.Max(0,openedDialogs-1);dialogOpen=openedDialogs>0;};return dialog;
    }
    private void ShowErrorPopup()
    {
        if(isClosed||!IsVisible||operationFailure==null)return;
        if(errorDialog!=null){errorSummary!.Text=operationFailure.Summary;errorDetails!.Text=operationFailure.Details;return;}
        // An existing settings/action dialog already contains the visible error panel.
        if(openedDialogs>0)return;
        var panel=new StackPanel{Margin=new(24),Spacing=16};
        panel.Children.Add(new TextBlock{Text="Vorgang fehlgeschlagen",FontSize=24,FontWeight=FontWeight.Bold});
        errorSummary=new TextBlock{Text=operationFailure.Summary,TextWrapping=TextWrapping.Wrap};panel.Children.Add(errorSummary);
        errorDetails=new TextBlock{Text=operationFailure.Details,TextWrapping=TextWrapping.Wrap};panel.Children.Add(new Expander{Header="Technische Details",Content=errorDetails});
        var dialog=Alert("EmuVora · Fehler",panel);errorDialog=dialog;panel.Children.Add(AsyncButton("Einstellungen öffnen",async()=>{dialog.Close();await ShowSettingsAsync();}));panel.Children.Add(ActionButton("Schließen",()=>dialog.Close()));
        dialog.Closed+=(_,_)=>{errorDialog=null;errorSummary=errorDetails=null;};
        _=dialog.ShowDialog(this);
    }
    private async Task ShowFirmwareRequiredAsync(string system,string hint,string emulator)
    {
        status.Text=hint;
        var panel=new StackPanel{Margin=new(24),Spacing=16};panel.Children.Add(new TextBlock{Text=system+" · BIOS / Firmware fehlt",FontSize=24,FontWeight=FontWeight.Bold,TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text=hint+"\n\nDas Spiel wurde nicht gestartet. Importiere deine eigenen BIOS-Dateien und starte es anschließend erneut.",TextWrapping=TextWrapping.Wrap});
        var dialog=Alert("EmuVora · BIOS erforderlich",panel);var action="";
        panel.Children.Add(ActionButton("BIOS-Dateien importieren …",()=>{action="import";dialog.Close();}));panel.Children.Add(ActionButton("Emulator-Einstellungen öffnen",()=>{action="settings";dialog.Close();}));panel.Children.Add(ActionButton("Schließen",dialog.Close));
        await dialog.ShowDialog(this);
        if(isClosed)return;
        if(action=="import"){var files=await new ControllerPicker(this).OpenFilePathsAsync("Eigene BIOS-/Firmware-Dateien");if(files.Length>0)await ImportFirmwareFilesAsync(files);}
        else if(action=="settings")await ShowSettingsAsync(emulator);
    }
    private void ApplyManagedLanguages()
    {
        if(!settings.PreferGerman)return;
        foreach(var(id,package)in settings.Packages)
        {
            if(settings.EmulatorPaths.GetValueOrDefault(id)!=package.ExecutablePath||!File.Exists(package.ExecutablePath)||runningEmulators.Contains(id))continue;
            var active=false;foreach(var process in System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(package.ExecutablePath)))using(process){try{if(process.MainModule?.FileName.Equals(package.ExecutablePath,StringComparison.OrdinalIgnoreCase)==true)active=true;}catch(System.ComponentModel.Win32Exception){active=true;}}
            if(!active)EmulatorLanguage.Apply(id,package.ExecutablePath);
        }
    }
}
