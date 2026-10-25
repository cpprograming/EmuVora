using System.Text;
namespace EmuVora.Core;

public static class Diagnostics
{
    // Allowlisted aggregate data only. Never serialize settings, exception text, game names or paths.
    public static string Create(string data,AppSettings settings,IReadOnlyList<Game> games,string version,DisplayInfo? display,bool hasError=false,CancellationToken token=default)
    {
        var report=new StringBuilder();void Line(string value="")=>report.AppendLine(value);
        Line("EmuVora · Diagnosebericht");Line("Version: "+(Version.TryParse(version,out var parsed)?parsed.ToString():"unbekannt"));
        Line("Erstellt (UTC): "+DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));Line("Laufzeit: .NET "+Environment.Version);Line("Betriebssystem: "+(OperatingSystem.IsWindows()?"Windows":OperatingSystem.IsLinux()?"Linux":OperatingSystem.IsMacOS()?"macOS":"anderes"));Line();
        Line("Datenschutz: Keine Spielnamen, Pfade, Benutzernamen, Gerätekennungen oder Protokollinhalte enthalten. Kein automatischer Upload.");
        var missing=0;var covers=0;foreach(var game in games){token.ThrowIfCancellationRequested();if(!File.Exists(game.Path))missing++;if(!CoverLibrary.IsMissing(settings,game.Path))covers++;}
        Line();Line("Bibliothek");Line($"Dateieinträge: {games.Count}; Titelgruppen: {GameVariants.Group(games).Count}");Line($"Nicht erreichbare Spieldateien: {missing}; Cover vorhanden: {covers}; Cover fehlen: {games.Count-covers}");
        Line();Line("Emulatoren");foreach(var emulator in EmulatorCatalog.Known)
        {
            token.ThrowIfCancellationRequested();var path=settings.EmulatorPaths.GetValueOrDefault(emulator.Id);
            Line(emulator.Name+": "+(string.IsNullOrEmpty(path)?"nicht eingerichtet":File.Exists(path)?"Programmdatei erreichbar":"Programmdatei nicht erreichbar")+(settings.Packages.TryGetValue(emulator.Id,out var package)&&package.ExecutablePath==path?" · verwaltet":""));
        }
        Line();Line("Systemprüfung (erreichbare Datei bedeutet noch keinen erfolgreichen Spielstart)");
        foreach(var group in games.GroupBy(g=>g.System).OrderBy(g=>g.Key,StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();var known=EmulatorCatalog.Systems.Contains(group.Key);var name=known?group.Key:"Nicht zugeordnetes System";
            if(!known){Line(name+": "+group.Count()+" Einträge; Konsole zuordnen.");continue;}
            var emulator=SetupPlan.RequiredEmulators([group.Key],settings).FirstOrDefault();var core=emulator?.Id=="retroarch"?(settings.CorePaths.TryGetValue(group.Key,out var path)&&File.Exists(path)?"; Core erreichbar":"; Core fehlt"):"";
            string firmware;try{firmware=SetupPlan.FirmwareHint(group.Key,data,settings)==null?"keine fehlende Pflicht-Firmware erkannt":"Firmware prüfen / eigene Dateien importieren";}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){firmware="Firmwareprüfung nicht möglich";}
            Line($"{name}: {group.Count()} Einträge{core}; {firmware}.");
        }
        token.ThrowIfCancellationRequested();Line();Line("Bild und Vorgaben");Line(display==null?"Monitor: nicht erkannt":$"Monitor: {Math.Max(0,display.Width)} × {Math.Max(0,display.Height)} Pixel (Gerätekennung ausgelassen)");
        var mode=DisplayProfiles.Modes.Contains(settings.DisplayMode)?settings.DisplayMode:"unbekannt";
        Line($"Bildmodus: {mode}; automatische Bildprofile: {settings.AutomaticDisplay}; maximale 3D-Skalierung: {Math.Clamp(settings.MaximumRenderScale,1,6)}×");
        Line($"Deutsch bevorzugt: {settings.PreferGerman}; automatische Einrichtung: {settings.AutomaticSetup}; Xbox-Standardprofile: {settings.XboxDefaults}; TV-Modus: {settings.TvMode}");
        Line("Aktuell gemeldeter Fehler: "+(hasError?"ja; technische Details ausschließlich im lokalen Fehlerdialog":"nein"));
        Line();Line("Nächste Schritte");if(missing>0)Line("- Fehlende Laufwerke verbinden oder ROM-Ordner in Einstellungen → Bibliothek neu zuordnen.");
        if(covers<games.Count)Line("- Ansicht Fehlende Cover öffnen; eigene Bilder oder passende Online-Treffer nach Vorschau übernehmen.");
        Line("- Fehlende Emulatoren/Cores unter Einstellungen → Emulatoren einrichten; erforderliche eigene Firmware importieren.");
        Line("- Controller-Hardware in der Controller-Diagnose und Spielsprache/Bildformat im echten Spiel prüfen.");Line("- Dieser Bericht ersetzt keine Hardwaretests und bescheinigt keine BIOS-Echtheit oder Spielekompatibilität.");return report.ToString();
    }
}
