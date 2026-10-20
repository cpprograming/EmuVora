using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace EmuVora.App;

public static class BrandIdentity
{
    private static readonly Lazy<Bitmap> Mark = new(() => new Bitmap(AssetLoader.Open(new Uri("avares://EmuVora.App/Assets/emuvora-mark.png"))));
    public static Image CreateMark(double size) => new() { Source=Mark.Value, Width=size, Height=size, Stretch=Stretch.Uniform };
    public static Control Lockup()
    {
        var text=new StackPanel { Spacing=1, VerticalAlignment=VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text="EmuVora", FontSize=25, FontWeight=FontWeight.Bold, LetterSpacing=-0.6 });
        text.Children.Add(new TextBlock { Text="DEINE SPIELE. DEIN ZUHAUSE.", FontSize=8, LetterSpacing=1.05, Opacity=0.6 });
        var row=new StackPanel { Orientation=Orientation.Horizontal, Spacing=9, Margin=new Thickness(0,8,0,24) };
        row.Children.Add(CreateMark(40));row.Children.Add(text);return row;
    }
}
