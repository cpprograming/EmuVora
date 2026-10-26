using System.Text.Json;
namespace EmuVora.Core;
public sealed class AppSettings
{
    public bool OnboardingCompleted {get;set;}
    public string Theme {get;set;}="Midnight";
    public string? EmulationStationTheme {get;set;}
    public Dictionary<string,string> EmulatorPaths {get;set;}=[];
    public Dictionary<string,string> CorePaths {get;set;}=[];
    public Dictionary<string,string> Covers {get;set;}=[];
    public HashSet<string> ProtectedCovers {get;set;}=new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string,HashSet<string>> Collections {get;set;}=new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string,string> PreferredEmulators {get;set;}=[];
    public Dictionary<string,InstalledPackage> Packages {get;set;}=[];
    public List<string> RomFolders {get;set;}=[];
    public List<ExternalSaveFolder> ExternalSaveFolders {get;set;}=[];
    public int BackupReminderDays {get;set;}
    public bool IncludeFirmwareInBackups {get;set;}
    public DateTimeOffset? LastBackupAt {get;set;}
    public bool Fullscreen {get;set;}=true;
    public bool CheckAppUpdatesOnStartup {get;set;}=true;
    public bool CheckUpdatesOnStartup {get;set;}=true;
    public bool AutoInstallUpdates {get;set;}=false;
    public bool AutomaticSetup {get;set;}=true;
    public bool PreferGerman {get;set;}=true;
    public bool XboxDefaults {get;set;}=true;
    public bool AutoDownloadCovers {get;set;}=true;
    public string CoverRegion {get;set;}="Europe";
    public int CoverSize {get;set;}=164;
    public bool TvMode {get;set;}
    public bool ShowGameDetails {get;set;}=true;
    public bool GroupGameVersions {get;set;}=true;
    public bool AutomaticDisplay {get;set;}=true;
    public string DisplayMode {get;set;}="Automatic";
    public int MaximumRenderScale {get;set;}=3;
    public Dictionary<string,string> GameAspectRatios {get;set;}=[];
}
public record InstalledPackage(string Version,string ExecutablePath,string? PreviousExecutablePath=null);
public sealed class SettingsStore(string file)
{
    public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    public string? LoadWarning {get;private set;}
    public AppSettings Load()
    {
        RomRelocation.Recover(Path.GetDirectoryName(Path.GetFullPath(file))!);
        DuplicateMerge.Recover(Path.GetDirectoryName(Path.GetFullPath(file))!);
        if(!File.Exists(file))return new();
        try
        {
            var json=File.ReadAllText(file);var settings=JsonSerializer.Deserialize<AppSettings>(json,JsonOptions)??new();
            settings.EmulatorPaths??=[];settings.CorePaths??=[];settings.Covers??=[];settings.PreferredEmulators??=[];settings.Packages??=[];settings.RomFolders??=[];
            settings.GameAspectRatios??=[];settings.ExternalSaveFolders??=[];
            RemoveRetiredFrontend(settings);
            settings.ProtectedCovers=new(settings.ProtectedCovers??[],StringComparer.OrdinalIgnoreCase);
            settings.Collections=(settings.Collections??[]).Where(c=>!string.IsNullOrWhiteSpace(c.Key)).GroupBy(c=>c.Key,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.First().Key,g=>new HashSet<string>(g.SelectMany(c=>c.Value??[]),StringComparer.OrdinalIgnoreCase),StringComparer.OrdinalIgnoreCase);
            // Older releases stored manually selected images under a random GUID filename.
            using(var document=JsonDocument.Parse(json))
                if(document.RootElement.ValueKind==JsonValueKind.Object&&!document.RootElement.EnumerateObject().Any(p=>p.Name.Equals("ProtectedCovers",StringComparison.OrdinalIgnoreCase)))
                    foreach(var cover in settings.Covers)
                        if(Guid.TryParseExact(Path.GetFileNameWithoutExtension(cover.Value),"N",out _))settings.ProtectedCovers.Add(cover.Key);
            if(settings.CorePaths.TryGetValue("retroarch",out var legacy))foreach(var system in EmulatorCatalog.Get("retroarch").Systems)settings.CorePaths.TryAdd(system,legacy);
            return settings;
        }
        catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException)
        {
            LoadWarning="Einstellungen konnten nicht geladen werden. Die vorhandene Datei bleibt erhalten: "+ex.Message;
            try{File.Copy(file,file+".invalid-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"),false);}catch(Exception copyError) when(copyError is IOException or UnauthorizedAccessException){ }return new();
        }
    }
    public void Save(AppSettings settings)=>AtomicFile.WriteText(file,JsonSerializer.Serialize(settings,JsonOptions));
    internal static void RemoveRetiredFrontend(AppSettings settings)
    {
        foreach(var key in settings.EmulatorPaths.Keys.Where(IsRetiredFrontend).ToArray())settings.EmulatorPaths.Remove(key);
        foreach(var key in settings.Packages.Keys.Where(IsRetiredFrontend).ToArray())settings.Packages.Remove(key);
        foreach(var key in settings.PreferredEmulators.Where(p=>IsRetiredFrontend(p.Value)).Select(p=>p.Key).ToArray())settings.PreferredEmulators.Remove(key);
    }
    private static bool IsRetiredFrontend(string id)=>id.Equals("esde",StringComparison.OrdinalIgnoreCase)||id.Equals("alekfull-nx",StringComparison.OrdinalIgnoreCase);
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
