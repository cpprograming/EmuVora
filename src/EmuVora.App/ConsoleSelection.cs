using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private sealed record ConsoleChoice(string System, int Titles, int Files);
    private async Task ShowConsolePickerAsync()
    {
        if(operation!=null)return;
        var choices=allGames.GroupBy(g=>g.System).OrderBy(g=>g.Key).Select(g=>new ConsoleChoice(g.Key,settings.GroupGameVersions?GameVariants.Group(g).Count:g.Count(),g.Count())).ToArray();
        var owner=OwnedWindows.LastOrDefault(w=>w.IsActive)??this;
        var header=new StackPanel{Spacing=10};header.Children.Add(new TextBlock{Text="Deine Konsolen",FontSize=28,FontWeight=FontWeight.Bold});
        header.Children.Add(new TextBlock{Text="Eine Konsole auswählen und ihre Spiele öffnen. Die gewählte Bibliotheksdarstellung bleibt erhalten.",TextWrapping=TextWrapping.Wrap});
        var searchConsole=new TextBox{Watermark="Konsole suchen …"};header.Children.Add(searchConsole);
        var message=new TextBlock{TextWrapping=TextWrapping.Wrap};header.Children.Add(message);
        var list=new ListBox{Margin=new(0,14,0,14),ItemTemplate=new FuncDataTemplate<ConsoleChoice>((choice,_)=>
        {
            if(choice==null)return new Border();
            var row=new Grid{ColumnDefinitions=new("220,*"),ColumnSpacing=20,Margin=new(10)};
            row.Children.Add(consoleLogos.CreateContent(choice.System,choice.Titles,true));
            var text=new StackPanel{Spacing=5,VerticalAlignment=VerticalAlignment.Center};text.Children.Add(new TextBlock{Text=choice.System,FontSize=settings.TvMode?23:20,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap});text.Children.Add(new TextBlock{Text=$"{choice.Titles} Titel · {choice.Files} Dateien",Opacity=.75,TextWrapping=TextWrapping.Wrap});Grid.SetColumn(text,1);row.Children.Add(text);return row;
        })};
        list.Styles.Add(new Style(s=>s.OfType<ListBoxItem>()){Setters={new Setter(ListBoxItem.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch)}});
        var actions=new StackPanel{Spacing=8};var layout=new Grid{RowDefinitions=new("Auto,*,Auto"),Margin=new(24)};layout.Children.Add(header);Grid.SetRow(list,1);layout.Children.Add(list);Grid.SetRow(actions,2);layout.Children.Add(actions);
        var dialog=FixedDialog("EmuVora · Konsolen auswählen",layout);dialog.Width=850;dialog.Height=760;
        var open=ActionButton("Spiele dieser Konsole anzeigen",()=>{if(operation==null&&list.SelectedItem is ConsoleChoice choice)dialog.Close(choice.System);});actions.Children.Add(open);actions.Children.Add(ActionButton("Abbrechen",()=>dialog.Close()));
        void Filter()
        {
            var selected=(list.SelectedItem as ConsoleChoice)?.System??systems.SelectedItem as string;
            var filtered=choices.Where(c=>c.System.Contains(searchConsole.Text?.Trim()??"",StringComparison.OrdinalIgnoreCase)).ToArray();
            list.ItemsSource=filtered;list.SelectedItem=filtered.FirstOrDefault(c=>c.System==selected)??filtered.FirstOrDefault();
            message.Text=choices.Length==0?"Noch keine Konsolen importiert. Zuerst einen ROM-Ordner hinzufügen.":filtered.Length==0?"Keine passende Konsole. Suchtext ändern.":$"{filtered.Length} "+(filtered.Length==1?"Konsole":"Konsolen")+" in deiner Bibliothek";open.IsEnabled=list.SelectedItem!=null;
        }
        searchConsole.TextChanged+=(_,_)=>Filter();list.SelectionChanged+=(_,_)=>open.IsEnabled=list.SelectedItem!=null;list.DoubleTapped+=(_,_)=>{if(list.SelectedItem is ConsoleChoice choice)dialog.Close(choice.System);};Filter();
        var selectedSystem=await dialog.ShowDialog<string?>(owner);if(selectedSystem==null||isClosed||operation!=null)return;
        if(owner!=this)owner.Close();search.Text="";view="Alle Spiele";collectionChoice.SelectedItem=null;systems.SelectedItem=selectedSystem;Refresh();Show();Activate();gamesList.Focus();
    }
}
