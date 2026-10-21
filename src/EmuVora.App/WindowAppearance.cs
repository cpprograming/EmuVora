using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace EmuVora.App;
public static class WindowAppearance
{
    public static void Apply(Window window)
    {
        window.FontFamily=new FontFamily("Segoe UI Variable, Segoe UI");
        var icon=Path.Combine(AppContext.BaseDirectory,"EmuVora.ico");if(File.Exists(icon))window.Icon=new WindowIcon(icon);
        window.Styles.Add(new Style(s=>s.OfType<Button>()){Setters={new Setter(Button.CornerRadiusProperty,new CornerRadius(8)),new Setter(Button.PaddingProperty,new Thickness(12,9)),new Setter(Button.FontSizeProperty,13d)}});
        window.Styles.Add(new Style(s=>s.OfType<TextBox>()){Setters={new Setter(TextBox.CornerRadiusProperty,new CornerRadius(8))}});
        window.Styles.Add(new Style(s=>s.OfType<ComboBox>()){Setters={new Setter(ComboBox.CornerRadiusProperty,new CornerRadius(8))}});
        window.Styles.Add(new Style(s=>s.OfType<Button>().Class("library-navigation")){Setters={new Setter(Button.BorderThicknessProperty,new Thickness(0)),new Setter(Button.MinHeightProperty,39d),new Setter(Button.BackgroundProperty,Brushes.Transparent),new Setter(Button.PaddingProperty,new Thickness(12,10))}});
        window.Styles.Add(new Style(s=>s.OfType<Image>().Class("console-logo")){Setters={new Setter(Image.HeightProperty,24d),new Setter(Image.MaxWidthProperty,124d)}});
        window.Styles.Add(new Style(s=>s.Class("tv-mode").Descendant().OfType<Image>().Class("console-logo")){Setters={new Setter(Image.HeightProperty,32d)}});
        window.Styles.Add(new Style(s=>s.OfType<TextBlock>().Class("console-count")){Setters={new Setter(TextBlock.FontSizeProperty,12d)}});
        window.Styles.Add(new Style(s=>s.OfType<TextBlock>().Class("console-variant")){Setters={new Setter(TextBlock.FontSizeProperty,10d)}});
        window.Styles.Add(new Style(s=>s.Class("tv-mode").Descendant().OfType<TextBlock>().Class("console-count")){Setters={new Setter(TextBlock.FontSizeProperty,16d)}});
        window.Styles.Add(new Style(s=>s.Class("tv-mode").Descendant().OfType<TextBlock>().Class("console-variant")){Setters={new Setter(TextBlock.FontSizeProperty,14d)}});
        window.Styles.Add(new Style(s=>s.OfType<Button>().Class("console-filter").Class("selected-console")){Setters={new Setter(Button.BorderThicknessProperty,new Thickness(2)),new Setter(Button.BorderBrushProperty,new DynamicResourceExtension("EmuAccent"))}});
        window.Styles.Add(new Style(s=>s.OfType<Button>().Class("primary-action")){Setters={new Setter(Button.BackgroundProperty,new DynamicResourceExtension("EmuAccent")),new Setter(Button.ForegroundProperty,new DynamicResourceExtension("EmuAccentText")),new Setter(Button.FontWeightProperty,FontWeight.SemiBold)}});
        window.Styles.Add(new Style(s=>s.OfType<Button>()){Setters={new Setter(Button.ContentTemplateProperty,new FuncDataTemplate<string>((text,_)=>new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,TextAlignment=TextAlignment.Center}))}});
        window.Styles.Add(new Style(s=>s.OfType<Button>().Class(":focus")){Setters={new Setter(Button.BorderBrushProperty,Brush.Parse("#37A5FF")),new Setter(Button.BorderThicknessProperty,new Thickness(2))}});
        window.Styles.Add(new Style(s=>s.OfType<CheckBox>().Class(":focus")){Setters={new Setter(CheckBox.BorderBrushProperty,Brush.Parse("#37A5FF")),new Setter(CheckBox.BorderThicknessProperty,new Thickness(2))}});
        foreach(var style in new[]{
            new Style(s=>s.Class("tv-mode").Descendant().OfType<Button>()){Setters={new Setter(Button.FontSizeProperty,18d),new Setter(Button.MinHeightProperty,48d),new Setter(Button.PaddingProperty,new Thickness(16,12))}},
            new Style(s=>s.Class("tv-mode").Descendant().OfType<TextBox>()){Setters={new Setter(TextBox.FontSizeProperty,18d),new Setter(TextBox.MinHeightProperty,48d)}},
            new Style(s=>s.Class("tv-mode").Descendant().OfType<ComboBox>()){Setters={new Setter(ComboBox.FontSizeProperty,18d),new Setter(ComboBox.MinHeightProperty,48d)}}})window.Styles.Add(style);
        window.Opened+=(_,_)=>{if(window.Owner?.Classes.Contains("tv-mode")==true)SetTvMode(window,true);};
    }
    public static void SetTvMode(Window window,bool enabled){window.Classes.Set("tv-mode",enabled);window.FontSize=enabled?18:14;}
}

