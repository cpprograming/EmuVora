namespace EmuVora.Core;

public record SystemReadinessInfo(string System,int Files,int Titles,int MissingFiles,int MissingCovers,string? EmulatorId,bool EmulatorMissing,bool CoreMissing,string? FirmwareHint,string? Note)
{
    public bool Known=>EmulatorId!=null;
    public bool NeedsSetup=>Known&&(EmulatorMissing||CoreMissing);
    public bool NeedsAttention=>!Known||NeedsSetup||FirmwareHint!=null||MissingFiles>0||MissingCovers>0;
    public string Status=>!Known?"Konsole zuordnen":NeedsSetup?"Einrichtung fehlt":FirmwareHint!=null?"Firmware prüfen":Files==0?"Noch keine Spiele":MissingFiles>0?"Spieldateien prüfen":MissingCovers>0?"Cover ergänzen":"Voraussetzungen erkannt";
}
public static class SystemReadiness
{
    // Read-only inventory; a present file is not proof of executable content or game compatibility.
    public static IReadOnlyList<SystemReadinessInfo> Inspect(string data,AppSettings settings,IReadOnlyList<Game> games,CancellationToken token=default)
    {
        var groups=games.GroupBy(g=>g.System).ToDictionary(g=>g.Key,g=>g.ToArray());var result=new List<SystemReadinessInfo>();
        foreach(var system in EmulatorCatalog.Systems.Concat(groups.Keys).Distinct().OrderBy(s=>s,StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();var entries=groups.GetValueOrDefault(system)??[];var missing=0;var covers=0;
            foreach(var game in entries){token.ThrowIfCancellationRequested();if(!File.Exists(game.Path))missing++;if(CoverLibrary.IsMissing(settings,game.Path))covers++;}
            var definition=EmulatorCatalog.Systems.Contains(system)?SetupPlan.RequiredEmulators([system],settings).FirstOrDefault():null;
            var executable=definition==null?null:settings.EmulatorPaths.GetValueOrDefault(definition.Id);var emulatorMissing=definition!=null&&(!File.Exists(executable)||!Path.GetExtension(executable).Equals(".exe",StringComparison.OrdinalIgnoreCase));
            var core=definition?.Id=="retroarch"?settings.CorePaths.GetValueOrDefault(system):null;var coreMissing=definition?.Id=="retroarch"&&(!File.Exists(core)||!Path.GetExtension(core).Equals(".dll",StringComparison.OrdinalIgnoreCase));
            string? hint=null;if(definition!=null)try{hint=SetupPlan.FirmwareHint(system,data,settings);}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){hint="Firmwareprüfung nicht möglich. Pfade und Zugriffsrechte in den Emulator-Einstellungen prüfen.";}
            result.Add(new(system,entries.Length,GameVariants.Group(entries).Count,missing,covers,definition?.Id,emulatorMissing,coreMissing,hint,SystemCatalog.Find(system)?.Note));
        }
        return result;
    }
    public static string[] MissingPackages(IEnumerable<SystemReadinessInfo> systems)
    {
        var selected=systems.ToArray();var missing=selected.Where(s=>s.Known&&s.EmulatorMissing).Select(s=>s.EmulatorId).ToHashSet();
        return EmulatorCatalog.Known.Where(e=>missing.Contains(e.Id)).Select(e=>e.Id).Concat(selected.Any(s=>s.CoreMissing)?new[]{"retroarch-cores"}:[]).ToArray();
    }
}
