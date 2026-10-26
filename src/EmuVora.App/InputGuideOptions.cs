using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using EmuVora.Core;
namespace EmuVora.App;
public sealed partial class MainWindow
{
    private readonly Button inputGuideButton=new(){IsVisible=false};
    private async Task ShowInputGuideAsync()
    {
        var owner=OwnedWindows.LastOrDefault(w=>w.IsActive)??this;var selected=Selected?.System;var panel=new StackPanel{Margin=new(24),Spacing=14};panel.Children.Add(new TextBlock{Text="Bedienung deiner Systeme",FontSize=25,FontWeight=FontWeight.Bold,TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text="Grundhinweise je System. Die tatsächlichen Anforderungen hängen vom Spiel und seiner Emulator-Belegung ab; es werden keine Geräte eingerichtet oder getestet.",TextWrapping=TextWrapping.Wrap});
        var search=new TextBox{Watermark="System oder Bedienung suchen …"};var special=new CheckBox{Content="Nur Systeme mit möglichen Zusatzeingaben",IsChecked=selected==null||InputGuide.ForSystem(selected).AdditionalInput};panel.Children.Add(search);panel.Children.Add(special);
        var list=new ListBox{Height=200,ItemTemplate=new FuncDataTemplate<SystemInputGuide>((g,_)=>new TextBlock{Text=g==null?"":g.System+" · "+g.Category,TextWrapping=TextWrapping.Wrap,Margin=new(8)})};panel.Children.Add(list);var detail=new TextBlock{TextWrapping=TextWrapping.Wrap};panel.Children.Add(detail);
        void Detail()=>detail.Text=list.SelectedItem is SystemInputGuide guide?InputGuide.Describe(guide.System):"Kein passendes System. Suche oder Filter ändern.";
        void Filter(){var current=(list.SelectedItem as SystemInputGuide)?.System??selected;var query=search.Text?.Trim()??"";var rows=InputGuide.All().Where(g=>(special.IsChecked!=true||g.AdditionalInput)&&(g.System.Contains(query,StringComparison.OrdinalIgnoreCase)||g.Category.Contains(query,StringComparison.OrdinalIgnoreCase))).ToArray();list.ItemsSource=rows;list.SelectedItem=rows.FirstOrDefault(g=>g.System==current)??rows.FirstOrDefault();Detail();}
        var dialog=Dialog("EmuVora · Bedienhinweise",panel);dialog.Height=720;panel.Children.Add(ActionButton("Schließen",dialog.Close));list.SelectionChanged+=(_,_)=>Detail();search.TextChanged+=(_,_)=>Filter();special.IsCheckedChanged+=(_,_)=>Filter();Filter();await dialog.ShowDialog(owner);
    }
}
