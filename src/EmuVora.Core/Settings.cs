using System.Text.Json;
namespace EmuVora.Core;
public sealed class AppSettings
{
    public string Theme {get;set;}="Midnight";
    public Dictionary<string,string> EmulatorPaths {get;set;}=[];
    public Dictionary<string,string> CorePaths {get;set;}=[];
    public Dictionary<string,string> Covers {get;set;}=[];
    public Dictionary<string,string> PreferredEmulators {get;set;}=[];
    public Dictionary<string,InstalledPackage> Packages {get;set;}=[];
    public List<string> RomFolders {get;set;}=[];
    public bool Fullscreen {get;set;}=true;
    public bool CheckUpdatesOnStartup {get;set;}=true;
}
public record InstalledPackage(string Version,string ExecutablePath,string? PreviousExecutablePath=null);
public sealed class SettingsStore(string file)
{
    public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    public string? LoadWarning {get;private set;}
    public AppSettings Load()
    {
        if(!File.Exists(file))return new();
        try
        {
            var settings=JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file),JsonOptions)??new();
            settings.EmulatorPaths??=[];settings.CorePaths??=[];settings.Covers??=[];settings.PreferredEmulators??=[];settings.Packages??=[];settings.RomFolders??=[];
            if(settings.CorePaths.TryGetValue("retroarch",out var legacy))foreach(var system in EmulatorCatalog.Get("retroarch").Systems)settings.CorePaths.TryAdd(system,legacy);
            return settings;
        }
        catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException)
        {
            LoadWarning="Einstellungen konnten nicht geladen werden. Die vorhandene Datei bleibt erhalten: "+ex.Message;
            try{File.Copy(file,file+".invalid-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"),false);}catch(IOException){ }return new();
        }
    }
    public void Save(AppSettings settings)=>AtomicFile.WriteText(file,JsonSerializer.Serialize(settings,JsonOptions));
}
public static class AtomicFile
{
    public static void WriteText(string file,string content)
    {
        var full=Path.GetFullPath(file);Directory.CreateDirectory(Path.GetDirectoryName(full)!);var temporary=full+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,content);if(File.Exists(full))File.Copy(full,full+".bak",true);File.Move(temporary,full,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
