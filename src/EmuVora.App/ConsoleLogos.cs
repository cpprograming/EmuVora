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
            "DOS"=>"pc","Arcade (MAME)"=>"mame",_=>EsDeFrontend.ThemeId(system)
        };
    }
    public Control CreateContent(string system,int count)
    {
        var row=new Grid{ColumnDefinitions=new("*,Auto"),ColumnSpacing=8};
        var mark=new StackPanel{Orientation=Orientation.Horizontal,Spacing=4,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
        var id=AssetId(system);
        if(id!=null)
        {
            if(!images.TryGetValue(id,out var bitmap))
            {
                using var stream=AssetLoader.Open(new Uri($"avares://EmuVora.App/Assets/ConsoleLogos/{id}.png"));
                images[id]=bitmap=new Bitmap(stream);
            }
            var image=new Image{Source=bitmap,Stretch=Stretch.Uniform,VerticalAlignment=VerticalAlignment.Center};image.Classes.Add("console-logo");mark.Children.Add(image);
        }
        else mark.Children.Add(new TextBlock{Text="\uE7F4",FontFamily=new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),FontSize=24,Foreground=Brush.Parse("#22343F")});
        var variant=system switch{"Neo Geo CD"=>"CD","Arcade (FinalBurn Neo)"=>"FBNeo",_=>null};
        if(variant!=null)mark.Children.Add(new TextBlock{Text=variant,Foreground=Brush.Parse("#22343F"),FontSize=10,FontWeight=FontWeight.Bold,VerticalAlignment=VerticalAlignment.Center});
        row.Children.Add(new Border{Background=Brush.Parse("#F1F4F5"),CornerRadius=new(5),Padding=new Thickness(4,2),Child=mark});
        var badge=new TextBlock{Text=count.ToString(System.Globalization.CultureInfo.InvariantCulture),FontSize=12,Opacity=0.75,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(badge,1);row.Children.Add(badge);
        return row;
    }
    public void Dispose(){foreach(var image in images.Values)image.Dispose();images.Clear();}
}
