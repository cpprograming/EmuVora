using Avalonia.Controls;
using Avalonia.Media;

namespace EmuVora.App;

internal sealed class SettingsPathEditor
{
    private readonly IDictionary<string,string> stored;
    private readonly Action save;
    private readonly string extension;
    private readonly Dictionary<string,string> drafts = new(StringComparer.OrdinalIgnoreCase);
    private string? key;
    private string loaded = "";
    private string observed = "";
    public TextBox Input { get; } = new();
    public TextBlock Feedback { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public event Action? Changed;
    public SettingsPathEditor(IDictionary<string,string> stored, Action save, string extension, string name)
    {
        this.stored=stored;this.save=save;this.extension=extension;
        Input.Watermark=name;Avalonia.Automation.AutomationProperties.SetName(Input,name);
        Input.TextChanged+=(_,_)=>{if(observed==(Input.Text??""))return;observed=Input.Text??"";Remember();Feedback.Text=PendingCount>0?"Pfadentwurf noch nicht übernommen. Mit Speichern prüfen und übernehmen.":"";Changed?.Invoke();};
    }
    private static string Clean(string? value)=>(value??"").Trim().Trim('"');
    private string? Stored(string id)=>stored.TryGetValue(id,out var value)?value:null;
    private void Remember()
    {
        if(key==null)return;var value=Input.Text??"";
        if(string.Equals(Clean(value),Clean(Stored(key)),StringComparison.OrdinalIgnoreCase)||value==loaded)drafts.Remove(key);else drafts[key]=value;
    }
    public int PendingCount { get { Remember();return drafts.Count; } }
    public void Select(string next)
    {
        Remember();key=next;loaded=Stored(next)??"";Input.Text=drafts.GetValueOrDefault(next)??loaded;observed=Input.Text;Feedback.Text=drafts.ContainsKey(next)?"Pfadentwurf noch nicht übernommen.":"";Changed?.Invoke();
    }
    public bool Save()
    {
        if(key==null)return false;string? previous=Stored(key);var had=stored.ContainsKey(key);
        try
        {
            var text=Clean(Input.Text);if(string.IsNullOrWhiteSpace(text))throw new InvalidOperationException("Bitte eine Datei auswählen.");
            var full=Path.GetFullPath(text);if(!File.Exists(full)||!Path.GetExtension(full).Equals(extension,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Eine vorhandene "+extension+"-Datei auswählen.");
            stored[key]=full;
            try { save(); } catch { if(had)stored[key]=previous!;else stored.Remove(key);throw; }
            drafts.Remove(key);loaded=full;observed=full;Input.Text=full;Feedback.Text="Pfad geprüft und gespeichert.";Changed?.Invoke();return true;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        { observed=Input.Text??"";Remember();Feedback.Text="Nicht übernommen: "+ex.Message;Changed?.Invoke();return false; }
    }
}
