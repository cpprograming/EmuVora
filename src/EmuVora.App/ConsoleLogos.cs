using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using EmuVora.Core;

namespace EmuVora.App;

public sealed class ConsoleLogos:IDisposable
{
    private readonly Dictionary<string,Bitmap> images=new(StringComparer.Ordinal);
    public static string? AssetId(string system)
    {
        if(!EmulatorCatalog.Systems.Contains(system))return null;
        return system switch
        {
            "PC Engine CD"=>"pce-cd","Neo Geo CD"=>"neogeo","ColecoVision"=>"colecovision",
            "DOS"=>"pc","Arcade (MAME)"=>"mame",_=>SystemArtworkIds.ThemeId(system)
        };
    }
    public Control CreateContent(string system,int count,bool large=false)
    {
        var row=new Grid{ColumnDefinitions=new(large?"*":"*,56"),ColumnSpacing=8,MinHeight=large?72:36};
        var mark=new Grid{ColumnDefinitions=new("*,Auto"),ColumnSpacing=4,VerticalAlignment=VerticalAlignment.Center};
        var id=AssetId(system);
        if(id!=null)
        {
            if(!images.TryGetValue(id,out var bitmap))
            {
                using var stream=AssetLoader.Open(new Uri($"avares://EmuVora.App/Assets/ConsoleLogos/{id}.png"));
                images[id]=bitmap=new Bitmap(stream);
            }
            var image=new Image{Source=bitmap,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};image.Classes.Add("console-logo");mark.Children.Add(image);
            if(large){image.Height=56;image.MaxWidth=200;}
        }
        else mark.Children.Add(new TextBlock{Text=system,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,MaxLines=1,FontSize=large?18:12,Foreground=Brush.Parse("#22343F")});
        var variant=system switch{"Neo Geo CD"=>"CD","Arcade (FinalBurn Neo)"=>"FBNeo",_=>null};
        if(variant!=null){var label=new TextBlock{Text=variant,Foreground=Brush.Parse("#22343F"),FontWeight=FontWeight.Bold,VerticalAlignment=VerticalAlignment.Center};label.Classes.Add("console-variant");Grid.SetColumn(label,1);mark.Children.Add(label);}
        var plate=new Border{Background=Brush.Parse("#F1F4F5"),CornerRadius=new(8),Padding=new Thickness(4,2),Child=mark};plate.Classes.Add(large?"console-logo-large":"console-logo-plate");if(large)plate.Height=72;row.Children.Add(plate);
        if(!large){var badge=new TextBlock{Text=count.ToString(System.Globalization.CultureInfo.InvariantCulture),TextAlignment=TextAlignment.Right,HorizontalAlignment=HorizontalAlignment.Stretch,Opacity=0.75,VerticalAlignment=VerticalAlignment.Center};badge.Classes.Add("console-count");Grid.SetColumn(badge,1);row.Children.Add(badge);}
        return row;
    }
    public void Dispose(){foreach(var image in images.Values)image.Dispose();images.Clear();}
}
