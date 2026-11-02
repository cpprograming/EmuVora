using Avalonia.Controls;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private readonly ComboBox collectionChoice=new(){PlaceholderText="Sammlung auswählen",HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
    private readonly Button collectionGame=new(){Content="Zu Sammlungen zuordnen",IsEnabled=false,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
    private IReadOnlyDictionary<string,DateTimeOffset> addedTimes=new Dictionary<string,DateTimeOffset>();
    private bool updatingCollections;

    private void UpdateCollections(string? selection=null)
    {
        selection??=collectionChoice.SelectedItem as string;updatingCollections=true;
        try{collectionChoice.ItemsSource=settings.Collections.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();collectionChoice.SelectedItem=selection!=null&&settings.Collections.ContainsKey(selection)?settings.Collections.Keys.First(n=>n.Equals(selection,StringComparison.OrdinalIgnoreCase)):null;}
        finally{updatingCollections=false;}
    }
    private void SaveCollections(Action<AppSettings> change,string? selection=null)
    {
        var draft=new AppSettings{Collections=GameCollections.Copy(settings)};change(draft);
        var previous=settings.Collections;settings.Collections=draft.Collections;
        try{settingsStore.Save(settings);}catch{settings.Collections=previous;throw;}
        UpdateCollections(selection);if(view=="Sammlung"&&collectionChoice.SelectedItem==null)view="Alle Spiele";Refresh();
    }
    private async Task ManageCollectionsAsync()
    {
        if(operation!=null)return;
        var panel=new StackPanel{Spacing=12,Margin=new(24)};panel.Children.Add(new TextBlock{Text="Eigene Sammlungen",FontSize=24,FontWeight=FontWeight.Bold});
        panel.Children.Add(new TextBlock{Text="Spiele können zu mehreren Sammlungen gehören. Umbenennen oder Löschen einer Sammlung verändert keine Spiele, Cover oder ROM-Dateien.",TextWrapping=TextWrapping.Wrap});
        var list=new ListBox{Height=200};var name=new TextBox{Watermark="Name der Sammlung (max. 60 Zeichen)",MaxLength=60};var message=new TextBlock{TextWrapping=TextWrapping.Wrap};
        void ReloadNames(string? selected=null){list.ItemsSource=settings.Collections.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();list.SelectedItem=selected;}
        list.SelectionChanged+=(_,_)=>{if(list.SelectedItem is string selected)name.Text=selected;};ReloadNames(collectionChoice.SelectedItem as string);
        panel.Children.Add(list);panel.Children.Add(name);
        void Edit(Action action){try{action();message.Text="Sammlungen gespeichert.";ToolTip.SetTip(message,null);}catch(Exception ex){DialogSaveError(message,"Nicht gespeichert",ex);}}
        var actions=new StackPanel{Spacing=8};actions.Children.Add(message);
        actions.Children.Add(ActionButton("Neue Sammlung erstellen",()=>Edit(()=>{var value=GameCollections.ValidateName(name.Text);SaveCollections(s=>GameCollections.Create(s,value));ReloadNames(value);})));
        actions.Children.Add(ActionButton("Sammlung umbenennen",()=>Edit(()=>{if(list.SelectedItem is not string old)throw new InvalidOperationException("Bitte eine Sammlung wählen.");var value=GameCollections.ValidateName(name.Text);SaveCollections(s=>GameCollections.Rename(s,old,value),value);ReloadNames(value);})));
        actions.Children.Add(ActionButton("Sammlung löschen · Spiele behalten",()=>Edit(()=>{if(list.SelectedItem is not string selected)throw new InvalidOperationException("Bitte eine Sammlung wählen.");SaveCollections(s=>s.Collections.Remove(selected));ReloadNames();name.Text="";})));
        var dialog=LibraryDialog("Sammlungen verwalten",panel,actions);actions.Children.Add(ActionButton("Fertig",()=>dialog.Close()));await dialog.ShowDialog(OwnedWindows.LastOrDefault(w=>w.IsActive)??this);
    }
    private async Task ChooseCollectionsAsync()
    {
        if(Selected is not{} game||operation!=null)return;
        if(settings.Collections.Count==0){await ManageCollectionsAsync();if(settings.Collections.Count==0||isClosed)return;}
        var panel=new StackPanel{Spacing=12,Margin=new(24)};panel.Children.Add(new TextBlock{Text=game.Title,FontSize=22,TextWrapping=TextWrapping.Wrap});
        var choices=settings.Collections.OrderBy(c=>c.Key,StringComparer.OrdinalIgnoreCase).Select(c=>(Name:c.Key,Option:new CheckBox{Content=c.Key,IsChecked=c.Value.Contains(game.Path)})).ToArray();
        foreach(var entry in choices)panel.Children.Add(entry.Option);
        panel.Children.Add(new TextBlock{Text="Mehrere Sammlungen sind möglich. Abwählen entfernt nur die Zuordnung.",TextWrapping=TextWrapping.Wrap});
        var actions=new StackPanel{Spacing=8};var feedback=new TextBlock{TextWrapping=TextWrapping.Wrap};actions.Children.Add(feedback);var dialog=LibraryDialog("Spiel zu Sammlungen zuordnen",panel,actions);
        actions.Children.Add(ActionButton("Zuordnung speichern",()=>
        {
            try{SaveCollections(s=>{foreach(var entry in choices)if(s.Collections.TryGetValue(entry.Name,out var members)){if(entry.Option.IsChecked==true)members.Add(game.Path);else members.Remove(game.Path);}});status.Text="Sammlungen gespeichert.";dialog.Close();}
            catch(Exception ex){DialogSaveError(feedback,"Nicht gespeichert",ex);}
        }));actions.Children.Add(ActionButton("Abbrechen",()=>dialog.Close()));
        await dialog.ShowDialog(OwnedWindows.LastOrDefault(w=>w.IsActive)??this);
    }
}
