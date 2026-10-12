using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Media;

namespace EmuVora.App;

public sealed class ControllerPicker(Window owner,string? initialDirectory=null)
{
    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        var paths=await PickAsync(options.Title??"Ordner wählen",false,options.AllowMultiple);
        var result=new List<IStorageFolder>();foreach(var path in paths)if(await owner.StorageProvider.TryGetFolderFromPathAsync(path) is{} folder)result.Add(folder);return result;
    }
    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        var patterns=options.FileTypeFilter?.SelectMany(f=>f.Patterns??[]).ToArray();
        var paths=await PickAsync(options.Title??"Datei wählen",true,options.AllowMultiple,patterns);
        var result=new List<IStorageFile>();foreach(var path in paths)if(await owner.StorageProvider.TryGetFileFromPathAsync(path) is{} file)result.Add(file);return result;
    }
    public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        var folders=await OpenFolderPickerAsync(new(){Title=options.Title??"Zielordner wählen"});if(folders.Count==0)return null;
        var dialog=new Window{Title="Dateiname für die Sicherung",Width=620,Height=320,Background=owner.Background,Foreground=owner.Foreground,RequestedThemeVariant=owner.RequestedThemeVariant,WindowStartupLocation=WindowStartupLocation.CenterOwner};WindowAppearance.Apply(dialog);ControllerUi.Attach(dialog);
        var panel=new StackPanel{Margin=new Avalonia.Thickness(24),Spacing=16};var name=new TextBox{Text=options.SuggestedFileName??"EmuVora-Bibliothek.json"};var message=new TextBlock{Text="Eine vorhandene Datei mit diesem Namen wird ersetzt.",TextWrapping=TextWrapping.Wrap};var save=new Button{Content="Sicherung speichern"};panel.Children.Add(name);panel.Children.Add(message);panel.Children.Add(save);
        save.Click+=async(_,_)=>{try{var value=name.Text?.Trim()??"";if(value.Length==0||Path.GetFileName(value)!=value||value.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new IOException("Bitte einen gültigen Dateinamen eingeben.");dialog.Close(await folders[0].CreateFileAsync(value));}catch(Exception ex){message.Text=ex.Message;}};dialog.Content=panel;return await dialog.ShowDialog<IStorageFile?>(owner.OwnedWindows.LastOrDefault(w=>w.IsActive)??owner);
    }
    private async Task<string[]> PickAsync(string title,bool files,bool multiple,IReadOnlyList<string>? patterns=null)
    {
        var dialog=new Window{Title=title,Width=720,Height=650,Background=owner.Background,Foreground=owner.Foreground,RequestedThemeVariant=owner.RequestedThemeVariant,WindowStartupLocation=WindowStartupLocation.CenterOwner};WindowAppearance.Apply(dialog);ControllerUi.Attach(dialog);
        var panel=new StackPanel{Spacing=12,Margin=new Avalonia.Thickness(22)};var message=new TextBlock{TextWrapping=TextWrapping.Wrap};var path=new TextBox{Text=initialDirectory??Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)};var list=new ListBox{Height=300};var selected=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string directory=path.Text!;
        void Load(string folder)
        {
            try{var full=Path.GetFullPath(folder);if(!Directory.Exists(full))throw new IOException("Ordner nicht verfügbar.");var entries=Directory.GetDirectories(full).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Select(p=>new Entry(p,true)).Concat(files?Directory.GetFiles(full).Where(p=>patterns==null||patterns.Count==0||patterns.Any(pattern=>System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern,Path.GetFileName(p),true))).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Select(p=>new Entry(p,false)):[]).ToArray();directory=full;path.Text=full;list.ItemsSource=entries;list.SelectedIndex=entries.Length>0?0:-1;message.Text="Auswahl: "+selected.Count;}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){message.Text=ex.Message;}
        }
        void Add(string label,Action action){var button=new Button{Content=label};button.Click+=(_,_)=>action();panel.Children.Add(button);}
        panel.Children.Add(new TextBlock{Text=title,FontSize=24});var drives=new ComboBox{ItemsSource=DriveInfo.GetDrives().Where(d=>d.IsReady).Select(d=>d.RootDirectory.FullName).ToArray(),PlaceholderText="Festplatte auswählen"};drives.SelectionChanged+=(_,_)=>{if(drives.SelectedItem is string drive)Load(drive);};panel.Children.Add(drives);panel.Children.Add(path);Add("Pfad öffnen",()=>Load(path.Text??""));panel.Children.Add(list);
        Add("Gewählten Ordner öffnen",()=>{if(list.SelectedItem is Entry{Directory:true} entry)Load(entry.Path);});Add("Eine Ebene zurück",()=>{var parent=Directory.GetParent(directory);if(parent!=null)Load(parent.FullName);});
        if(multiple)Add("Zur Auswahl hinzufügen",()=>{var entry=list.SelectedItem as Entry;var chosen=files?entry is{Directory:false}?entry.Path:null:entry is{Directory:true}?entry.Path:directory;if(chosen!=null){selected.Add(chosen);message.Text="Auswahl: "+selected.Count;}});
        Add("Auswahl übernehmen",()=>{var entry=list.SelectedItem as Entry;if(selected.Count==0){var chosen=files?entry is{Directory:false}?entry.Path:null:directory;if(chosen==null){message.Text="Bitte eine Datei auswählen.";return;}selected.Add(chosen);}dialog.Close(selected.ToArray());});
        panel.Children.Add(message);panel.Children.Add(new TextBlock{Text="D-Pad ↑/↓: Dateien · ←/→: Bedienelement · A: auswählen · B: abbrechen",TextWrapping=TextWrapping.Wrap});dialog.Content=new ScrollViewer{Content=panel};Load(directory);return await dialog.ShowDialog<string[]?>(owner.OwnedWindows.LastOrDefault(w=>w.IsActive)??owner)??[];
    }
    private record Entry(string Path,bool Directory)
    {public override string ToString()=>(Directory?"▸ ":"  ")+System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar));}
}
