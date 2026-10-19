using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private Expander RecognitionOptions()
    {
        var panel=new StackPanel{Spacing=12};var message=new TextBlock{TextWrapping=TextWrapping.Wrap};
        panel.Children.Add(new TextBlock{Text="Spielnamen anhand lokaler SHA-1-Prüfsummen erkennen. Nur der gewählte Katalog wird von GitHub geladen; ROMs und Prüfsummen werden nicht übertragen. Bibliothekstitel erst nach Vorschau übernehmen. Eigene Änderungen und aus einem Bibliotheksexport wiederhergestellte Einträge sind geschützt. Dateinamen, Cover, Favoriten und Verlauf bleiben erhalten.",TextWrapping=TextWrapping.Wrap});
        var names=RomDatabases.Sources.Select(s=>s.System).Order().ToArray();var system=new ComboBox{ItemsSource=names,SelectedItem=names.FirstOrDefault(n=>allGames.Any(g=>g.System==n))??names[0]};panel.Children.Add(system);
        panel.Children.Add(AsyncButton("Katalog laden und Vorschläge prüfen",async()=>{if(system.SelectedItem is string selected)await RecognizeTitlesAsync(selected,message);}));
        panel.Children.Add(ActionButton("Erkennung abbrechen",()=>operation?.Cancel()));
        panel.Children.Add(ActionButton("Letzten Erkennungsbericht öffnen",()=>{var report=Path.Combine(dataDir,"recognition-report.txt");if(File.Exists(report))OpenPath(report);else message.Text="Noch kein Erkennungsbericht vorhanden.";}));
        panel.Children.Add(new TextBlock{Text="Quelle: libretro-database / No-Intro, CC-BY-SA-4.0 laut Repository. Katalogstand ist für diesen EmuVora-Release festgelegt und wird mit SHA-256 geprüft. Unterstützt: 16 Cartridge-Systeme, einzelne ROM-Dateien oder ZIP mit genau einer passenden ROM, höchstens 64 MiB. Header-Varianten, Patches und andere Datenstände können ohne Treffer bleiben. Keine Disc-, Arcade- oder Mehrdisc-Erkennung.",TextWrapping=TextWrapping.Wrap,FontSize=12});panel.Children.Add(message);
        return new Expander{Header="Spielnamen per Prüfsumme erkennen",Content=panel};
    }
    private async Task RecognizeTitlesAsync(string system,TextBlock message)
    {
        if(operation!=null){message.Text="Bitte den laufenden Vorgang abschließen oder abbrechen.";return;}
        var source=RomDatabases.Sources.Single(s=>s.System==system);var candidates=store.RecognitionCandidates(system);
        if(candidates.Count==0){message.Text="Keine ungeschützten Bibliothekseinträge für "+system+" vorhanden. Eigene Änderungen werden nicht überschrieben.";return;}
        var owner=OwnedWindows.LastOrDefault(w=>w.IsActive)??this;bool closed=false;void Closed(object? sender,EventArgs e)=>closed=true;owner.Closed+=Closed;
        try
        {
            RecognitionResult? result=null;
            var completed=await BusyAsync(async token=>
            {
                message.Text="Katalog wird geprüft …";using var http=new HttpClient{Timeout=TimeSpan.FromMinutes(2)};
                var database=await RomDatabases.LoadAsync(source,http,Path.Combine(dataDir,"metadata-cache"),token);
                var progress=new Progress<string>(title=>message.Text="Prüfsumme wird gelesen: "+title);
                result=await Task.Run(()=>RomRecognition.ScanAsync(candidates,database,progress,token),token);
            });
            if(closed||isClosed)return;if(!completed||result==null){message.Text=status.Text;return;}
            var report="Quelle: https://github.com/libretro/libretro-database/tree/"+source.Commit+"/metadat/no-intro\nLizenz: https://creativecommons.org/licenses/by-sa/4.0/\nSystem: "+system+"\n"+string.Join("\n",result.Suggestions.Select(s=>s.Path+": "+s.PreviousTitle+" → "+s.Title).Concat(result.Notes));
            AtomicFile.WriteText(Path.Combine(dataDir,"recognition-report.txt"),"Vorschau, noch nicht übernommen.\n"+report);
            if(result.Suggestions.Count==0){message.Text="Keine neuen eindeutigen Titel gefunden. Einzelheiten im Erkennungsbericht.";return;}
            await PreviewRecognitionAsync(result,report,message,owner);
        }
        finally{owner.Closed-=Closed;}
    }
    private async Task PreviewRecognitionAsync(RecognitionResult result,string report,TextBlock message,Window owner)
    {
        var panel=new StackPanel{Spacing=14,Margin=new Avalonia.Thickness(24)};
        panel.Children.Add(new TextBlock{Text=$"{result.Suggestions.Count} eindeutige Vorschläge. Nur Bibliothekstitel werden geändert; ROM-Dateinamen, Cover und Spielstände bleiben erhalten. Zwischenzeitlich geänderte oder geschützte Einträge werden beim Übernehmen ausgelassen.",TextWrapping=TextWrapping.Wrap});
        var list=new ListBox{Height=280,ItemsSource=result.Suggestions,ItemTemplate=new FuncDataTemplate<TitleSuggestion>((entry,_)=>new TextBlock{Text=entry==null?"":entry.PreviousTitle+" → "+entry.Title+"\n"+entry.Path,TextWrapping=TextWrapping.Wrap,Margin=new Avalonia.Thickness(8)})};panel.Children.Add(list);
        var dialog=Dialog("Erkannte Spielnamen prüfen",panel);panel.Children.Add(ActionButton("Alle eindeutigen Vorschläge übernehmen",()=>dialog.Close(true)));panel.Children.Add(ActionButton("Abbrechen",()=>dialog.Close(false)));
        if(!await dialog.ShowDialog<bool>(owner)){message.Text="Vorschläge verworfen. Bibliothek unverändert.";return;}
        if(isClosed)return;var changed=store.ApplyRecognition(result.Suggestions);Reload();
        message.Text=changed+" Bibliothekstitel übernommen. Vorhandene Cover bleiben erhalten. Fehlende Cover bei Bedarf erneut laden.";
        AtomicFile.WriteText(Path.Combine(dataDir,"recognition-report.txt"),changed+" Titel übernommen; weitere Vorschläge gegebenenfalls wegen zwischenzeitlicher Änderungen ausgelassen.\n"+report);
    }
}
