using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private static void StyleNavigation(Button button)
    {
        button.Classes.Add("library-navigation");button.HorizontalContentAlignment=HorizontalAlignment.Stretch;
        button.ContentTemplate=new FuncDataTemplate<string>((text,_)=>
        {
            var row=new Grid {ColumnDefinitions=new("24,*"),ColumnSpacing=9};
            // Segoe Fluent Icons keeps navigation consistent with Windows and avoids emoji rendering.
            var glyph=text switch {"Alle Spiele"=>"\uE80A","Favoriten"=>"\uEB51","Zuletzt gespielt"=>"\uE81C","Einstellungen"=>"\uE713",_=>"\uE7F4"};
            row.Children.Add(new TextBlock{Text=glyph,FontFamily=new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),FontSize=16,VerticalAlignment=VerticalAlignment.Center,Opacity=0.8});
            var label=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(label,1);row.Children.Add(label);return row;
        });
    }
    private static Control MissingArtwork(EmuVora.Core.Game game)
    {
        var colors=game.System switch
        {
            "NES" or "SNES" or "N64" =>("#533B4D","#272431","#F3B5BB"),
            "GB" or "GBC" or "GBA" =>("#354E44","#1D3030","#C6E7A9"),
            "PS1" or "PS2" or "PSP" =>("#344764","#212C40","#AECFF5"),
            "Mega Drive" or "Master System" or "Game Gear" =>("#314C56","#202F38","#A9E3EC"),
            _=>("#425658","#253337","#A8E5D4")
        };
        var panel=new Grid{RowDefinitions=new("Auto,*,Auto"),Margin=new Thickness(15)};
        panel.Children.Add(new TextBlock{Text="EMUVORA / COLLECTION",FontSize=8,LetterSpacing=1.4,Foreground=Brush.Parse(colors.Item3),Opacity=0.8,TextTrimming=TextTrimming.CharacterEllipsis});
        var center=new StackPanel{Spacing=14,VerticalAlignment=VerticalAlignment.Center};
        center.Children.Add(new TextBlock{Text="\uE7FC",FontFamily=new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),FontSize=38,HorizontalAlignment=HorizontalAlignment.Center,Foreground=Brush.Parse(colors.Item3),Opacity=0.7});
        center.Children.Add(new TextBlock{Text=game.System,FontSize=25,FontWeight=FontWeight.Bold,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=Brush.Parse("#F3F7F7")});Grid.SetRow(center,1);panel.Children.Add(center);
        var caption=new TextBlock{Text="COVER AUSSTEHEND",FontSize=8,LetterSpacing=1.5,Foreground=Brush.Parse(colors.Item3),TextTrimming=TextTrimming.CharacterEllipsis};Grid.SetRow(caption,2);panel.Children.Add(caption);
        return new Border{CornerRadius=new(9),Background=new LinearGradientBrush{StartPoint=new RelativePoint(0,0,RelativeUnit.Relative),EndPoint=new RelativePoint(1,1,RelativeUnit.Relative),GradientStops=new GradientStops{new(Color.Parse(colors.Item1),0),new(Color.Parse(colors.Item2),1)}},Child=panel};
    }
}
