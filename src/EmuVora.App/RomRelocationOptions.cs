using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private Expander RomRelocationOptions()
    {
        var panel=new StackPanel{Spacing=12};var message=new TextBlock{TextWrapping=TextWrapping.Wrap};
        panel.Children.Add(new TextBlock{Text="Nach einem selbst durchgeführten Ordnerumzug fehlende ROM-Pfade neu zuordnen. Unterordner und Dateinamen müssen gleich geblieben sein. Titel, Favoriten, Verlauf, Cover, Bildprofile und Sammlungen bleiben erhalten. ROM-Dateien und Spielstände werden nicht verschoben oder umbenannt.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text="Bisheriger ROM-Ordner (darf inzwischen fehlen)"});var oldRoot=new TextBox{Text=settings.RomFolders.FirstOrDefault(),Watermark="Zum Beispiel D:\\ROMs"};panel.Children.Add(oldRoot);
        panel.Children.Add(new TextBlock{Text="Neuer ROM-Ordner"});var newRoot=new TextBox{Watermark="Zum Beispiel E:\\Spiele\\ROMs"};panel.Children.Add(newRoot);
        panel.Children.Add(AsyncButton("Neuen ROM-Ordner auswählen",async()=>{var folders=await new ControllerPicker(this).OpenFolderPickerAsync(new(){Title="Neuen ROM-Ordner auswählen"});if(folders.Count>0)newRoot.Text=folders[0].TryGetLocalPath();}));
        panel.Children.Add(AsyncButton("Neue Pfade prüfen",async()=>await PreviewRomRelocationAsync(oldRoot.Text??"",newRoot.Text??"",message)));
        panel.Children.Add(message);return new Expander{Header="Verschobenen ROM-Ordner neu zuordnen",Content=panel};
    }
    private async Task PreviewRomRelocationAsync(string oldRoot,string newRoot,TextBlock message)
    {
        if(operation!=null||!CanBackUp()){message.Text="Bitte laufende Spiele und Vorgänge zuerst beenden.";return;}
        var owner=OwnedWindows.LastOrDefault(w=>w.IsActive)??this;RomRelocationPlan? plan=null;
        if(!await BusyAsync(async token=>{var snapshot=allGames.ToArray();plan=await Task.Run(()=>RomRelocation.Preview(snapshot,oldRoot,newRoot,token),token);})||plan==null){message.Text=status.Text;return;}
        if(isClosed||!owner.IsVisible)return;await ReviewRomRelocationAsync(plan,message,owner);
    }
    private async Task ReviewRomRelocationAsync(RomRelocationPlan plan,TextBlock message,Window owner)
    {
        var panel=new StackPanel{Spacing=14,Margin=new(24)};
        panel.Children.Add(new TextBlock{Text=$"{plan.Suggestions.Count} passende Pfade · {plan.Notes.Count} Hinweise\n\nBisher: {plan.OldRoot}\nNeu: {plan.NewRoot}",TextWrapping=TextWrapping.Wrap,FontWeight=FontWeight.SemiBold});
        panel.Children.Add(new TextBlock{Text="Bitte die Zielpfade prüfen: Gleicher relativer Pfad bedeutet keinen Nachweis identischer ROM-Inhalte. Noch erreichbare Originale und bereits importierte Ziele werden ausgelassen. Vor dem Übernehmen werden Bibliothek und Einstellungen im Datenordner gesichert. Die Zuordnung ändert keine ROMs oder Spielstandsdateien.",TextWrapping=TextWrapping.Wrap});
        var list=new ListBox{Height=230,ItemsSource=plan.Suggestions,ItemTemplate=new FuncDataTemplate<RomPathSuggestion>((entry,_)=>new TextBlock{Text=entry==null?"":entry.Game.Title+"\n"+entry.Game.Path+"\n→ "+entry.NewPath,TextWrapping=TextWrapping.Wrap,Margin=new(6)})};panel.Children.Add(list);
        if(plan.Notes.Count>0)panel.Children.Add(new Expander{Header="Übersprungene Einträge / Hinweise",Content=new ListBox{Height=160,ItemsSource=plan.Notes,ItemTemplate=new FuncDataTemplate<string>((text,_)=>new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Margin=new(6)})}});
        var actions=new StackPanel{Spacing=8};var dialog=LibraryDialog("Neue ROM-Pfade prüfen",panel,actions);var accept=ActionButton("Geprüfte Pfade übernehmen",()=>dialog.Close(true));accept.IsEnabled=plan.Suggestions.Count>0;actions.Children.Add(accept);actions.Children.Add(ActionButton("Abbrechen",()=>dialog.Close(false)));
        if(!await dialog.ShowDialog<bool>(owner)){message.Text="Vorschau verworfen. Bibliothek und Einstellungen unverändert.";return;}
        if(isClosed||operation!=null||!CanBackUp())return;
        RomRelocationResult? result=null;
        var enabled=owner.IsEnabled;owner.IsEnabled=false;message.Text="Bibliothek und Einstellungen werden gesichert und zugeordnet …";bool completed;
        try{completed=await BusyAsync(async token=>
        {
            try{result=await Task.Run(()=>RomRelocation.Apply(dataDir,plan,token),token);}
            finally
            {
                try{settings=settingsStore.Load();if(!isClosed){UpdateCollections();Reload();}}
                catch(Exception ex)
                {
                    if(isClosed)throw;
                    // Block further editing if a pending journal cannot be recovered.
                    foreach(var window in OwnedWindows.ToArray())window.Close();var blocked=new StackPanel{Spacing=16,Margin=new(24)};blocked.Children.Add(new TextBlock{Text="ROM-Zuordnung konnte nicht abgeschlossen werden. Bitte EmuVora schließen und den Datenordner prüfen. Bibliothek und Sicherungen bleiben erhalten.\n"+ex.Message,TextWrapping=TextWrapping.Wrap});blocked.Children.Add(ActionButton("EmuVora schließen",Close));Content=blocked;throw;
                }
            }
        });}finally{owner.IsEnabled=enabled;}
        if(!completed){message.Text=status.Text;return;}
        message.Text=status.Text=$"{result!.Changed} ROM-Pfade neu zugeordnet. Sicherung: {result.BackupDirectory}. ES-DE beim nächsten Öffnen neu abgleichen; eigene Emulator-Spielstandsordner bei Bedarf prüfen.";
    }
}
