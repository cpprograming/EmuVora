using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private async Task ManageCoverAsync()
    {
        if(Selected is not{} game||operation!=null)return;
        var panel=new StackPanel{Spacing=14,Margin=new(24)};
        panel.Children.Add(new TextBlock{Text=game.Title,FontSize=22,TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text=settings.ProtectedCovers.Contains(game.Path)?"Cover geschützt. Der automatische Abruf ersetzt dieses Bild nicht.":"Wähle ein eigenes Bild oder suche mit einem anderen Titel. Vor dem Austausch erscheint eine Vorschau.",TextWrapping=TextWrapping.Wrap});
        var dialog=Dialog("Cover bearbeiten",panel);
        panel.Children.Add(ActionButton("Eigenes Bild auswählen",()=>dialog.Close("local")));
        panel.Children.Add(ActionButton("Cover online suchen",()=>dialog.Close("online")));
        panel.Children.Add(ActionButton(settings.ProtectedCovers.Contains(game.Path)?"Cover-Schutz aufheben":"Cover schützen",()=>dialog.Close("protect")));
        panel.Children.Add(ActionButton("Abbrechen",()=>dialog.Close()));
        var action=await dialog.ShowDialog<string?>(this);if(isClosed)return;
        if(action=="local")await ChooseCoverAsync();else if(action=="online")await FetchCoverAsync();
        else if(action=="protect")Guard(()=>{if(!settings.ProtectedCovers.Remove(game.Path))settings.ProtectedCovers.Add(game.Path);settingsStore.Save(settings);UpdateSelected();});
    }

    private async Task<bool> PreviewCoverAsync(Game game,string source,string origin)
    {
        // Snapshot the selection so changing the original file cannot change what is accepted.
        var candidate=CoverLibrary.CopyImage(source,Path.Combine(dataDir,"covers"));
        var adopted=false;
        try
        {
            using(var codec=SkiaSharp.SKCodec.Create(candidate))
            {
                if(codec==null||codec.Info.Width>8192||codec.Info.Height>8192||codec.Info.Width*(long)codec.Info.Height>32_000_000)
                    throw new IOException("Das Bild ist ungültig oder zu groß (höchstens 8192 Pixel pro Seite und 32 Megapixel).");
            }
            using var stream=File.OpenRead(candidate);using var bitmap=Bitmap.DecodeToWidth(stream,480);
            var panel=new StackPanel{Spacing=14,Margin=new(24)};
            panel.Children.Add(new TextBlock{Text=game.Title,FontSize=22,TextWrapping=TextWrapping.Wrap});
            panel.Children.Add(new TextBlock{Text=origin,TextWrapping=TextWrapping.Wrap});
            var comparison=new Grid{ColumnDefinitions=new("*,*"),ColumnSpacing=16};
            var old=new StackPanel{Spacing=8};old.Children.Add(new TextBlock{Text="Bisher"});
            old.Children.Add(new ContentControl{Height=230,Content=Artwork(game,180)});comparison.Children.Add(old);
            var proposed=new StackPanel{Spacing=8};proposed.Children.Add(new TextBlock{Text="Vorschlag"});
            var preview=new Image{Height=230,Source=bitmap,Stretch=Stretch.Uniform};proposed.Children.Add(preview);Grid.SetColumn(proposed,1);comparison.Children.Add(proposed);panel.Children.Add(comparison);
            panel.Children.Add(new TextBlock{Text="Dieses Cover wird nach dem Übernehmen geschützt. Die Sammelsuche ersetzt es nicht. Den Schutz kannst du in den Spieldetails aufheben. Das bisherige Bild bleibt im Datenordner erhalten.",TextWrapping=TextWrapping.Wrap});
            var dialog=Dialog("Cover prüfen",panel);
            panel.Children.Add(ActionButton("Dieses Cover übernehmen",()=>dialog.Close(true)));
            panel.Children.Add(ActionButton("Abbrechen",()=>dialog.Close(false)));
            bool accepted;
            try{accepted=await dialog.ShowDialog<bool>(this);}
            finally{preview.Source=null;}
            if(!accepted||isClosed)return false;
            var previous=settings.Covers.GetValueOrDefault(game.Path);var wasProtected=settings.ProtectedCovers.Contains(game.Path);
            settings.Covers[game.Path]=candidate;settings.ProtectedCovers.Add(game.Path);
            try{settingsStore.Save(settings);}
            catch
            {
                if(previous==null)settings.Covers.Remove(game.Path);else settings.Covers[game.Path]=previous;
                if(!wasProtected)settings.ProtectedCovers.Remove(game.Path);throw;
            }
            adopted=true;Refresh(game.Path);status.Text="Cover übernommen und geschützt.";return true;
        }
        finally{if(!adopted&&File.Exists(candidate))File.Delete(candidate);}
    }
}
