using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private void RefreshSettingsWindows()
    {
        foreach(var dialog in OwnedWindows)
        {
            dialog.Background=Background;dialog.Foreground=Foreground;dialog.RequestedThemeVariant=RequestedThemeVariant;WindowAppearance.SetTvMode(dialog,settings.TvMode);
            foreach(var workspace in dialog.GetVisualDescendants().OfType<SettingsWorkspace>())workspace.UpdateScale(settings.TvMode);
        }
    }
    private static SettingsCategory SettingsPage(string name, StackPanel source, params (string Title,string Anchor,bool Advanced,string Keywords)[] definitions)
    {
        var controls=source.Children.ToArray();source.Children.Clear();var first=controls.FirstOrDefault() is TextBlock heading&&heading.FontSize>=24?1:0;
        string? Label(Control control)=>control switch{TextBlock text=>text.Text,Expander expander=>expander.Header as string,ContentControl content=>content.Content as string,_=>null};
        var starts=definitions.Select(d=>string.IsNullOrEmpty(d.Anchor)?first:Array.FindIndex(controls,c=>Label(c)==d.Anchor)).ToArray();
        if(starts.Any(i=>i<0)||!starts.SequenceEqual(starts.Order()))throw new InvalidOperationException("Einstellungsbereiche sind nicht eindeutig zugeordnet: "+name);
        var sections=new List<SettingsSection>();
        for(var i=0;i<definitions.Length;i++)
        {
            var body=new StackPanel{Spacing=12};foreach(var child in controls.Skip(starts[i]).Take((i+1<starts.Length?starts[i+1]:controls.Length)-starts[i]))body.Children.Add(child);
            var d=definitions[i];sections.Add(new(d.Title,d.Keywords,body,d.Advanced));
        }
        return new(name,sections);
    }
}
