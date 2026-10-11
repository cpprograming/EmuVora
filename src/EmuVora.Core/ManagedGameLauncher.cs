using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record EsDeLaunchRequest(string GameId,string DataDirectory)
{
    public static EsDeLaunchRequest Parse(string[] args)
    {
        if(args.Length!=4||args[0]!="--esde-launch"||args[2]!="--data-dir"||!Regex.IsMatch(args[1],"^[a-f0-9]{64}$")||!Path.IsPathFullyQualified(args[3]))throw new IOException("Ungültiger EmuVora-Spielverweis.");
        return new(args[1],Path.GetFullPath(args[3]));
    }
}
public sealed class ManagedGameLauncher
{
    public static Game FindGame(EsDeLaunchRequest request,LibraryStore store)
        =>store.GetAll().FirstOrDefault(g=>EsDeFrontend.GameId(g)==request.GameId)??throw new IOException("Das Spiel ist nicht mehr in der EmuVora-Bibliothek. Bitte den ES-DE-Modus neu öffnen, um die Liste abzugleichen.");
    public static async Task RunAsync(EsDeLaunchRequest request,int firstController,Action started,IProgress<DownloadProgress>? progress=null,CancellationToken token=default)
    {
        DataLocation.CheckAvailable(request.DataDirectory);
        // Serialize launches across ES-DE processes. Never execute commands from the stub.
        using var launchLock=new FileStream(Path.Combine(request.DataDirectory,".game-launch.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose);
        var store=new LibraryStore(Path.Combine(request.DataDirectory,"library.db"));var game=FindGame(request,store);
        if(!File.Exists(game.Path))throw new IOException("Die ROM-Datei ist nicht erreichbar: "+game.Path);
        var settingsStore=new SettingsStore(Path.Combine(request.DataDirectory,"settings.json"));var settings=settingsStore.Load();
        if(settingsStore.LoadWarning!=null)throw new IOException(settingsStore.LoadWarning);
        var preferred=settings.PreferredEmulators.GetValueOrDefault(game.System);
        var definition=EmulatorCatalog.Known.FirstOrDefault(e=>e.Id==preferred&&e.Systems.Contains(game.System))??EmulatorCatalog.Known.FirstOrDefault(e=>e.Systems.Contains(game.System))??throw new IOException("Konsole wird nicht unterstützt.");
        using var packages=new PackageManager();PackageCatalog? catalog=null;
        async Task Install(string id)
        {
            catalog??=await packages.GetCatalogAsync(token);var package=catalog.Packages.First(p=>p.Id==id);
            var path=settings.Packages.TryGetValue(id,out var current)&&current.Version==package.Version&&File.Exists(current.ExecutablePath)?current.ExecutablePath:await packages.InstallAsync(package,Path.Combine(request.DataDirectory,"emulators"),progress,token);
            settings.Packages[id]=new(package.Version,path,current?.ExecutablePath);
            if(id=="retroarch-cores")
            {
                var root=Path.GetDirectoryName(path)!;
                foreach(var (system,core) in PackageManager.CoreIds)
                {
                    if(settings.CorePaths.TryGetValue(system,out var existing)&&File.Exists(existing))continue;
                    var file=Directory.EnumerateFiles(root,core+"_libretro.dll",SearchOption.AllDirectories).FirstOrDefault();if(file!=null)settings.CorePaths[system]=file;
                }
            }
            else settings.EmulatorPaths[id]=path;
            settingsStore.Save(settings);
        }
        if(!settings.EmulatorPaths.TryGetValue(definition.Id,out var executable)||!File.Exists(executable))
        {if(!settings.AutomaticSetup)throw new IOException("Den Emulator zuerst in EmuVora einrichten oder die automatische Einrichtung aktivieren.");await Install(definition.Id);executable=settings.EmulatorPaths[definition.Id];}
        if(definition.Id=="retroarch"&&(!settings.CorePaths.TryGetValue(game.System,out var core)||!File.Exists(core)))
        {if(!settings.AutomaticSetup)throw new IOException("Den RetroArch-Core zuerst in EmuVora einrichten.");await Install("retroarch-cores");}
        if(settings.Packages.TryGetValue(definition.Id,out var managed)&&managed.ExecutablePath==executable)
        {
            if(settings.AutomaticSetup)EmulatorSetup.Configure(definition.Id,executable,request.DataDirectory,settings.XboxDefaults,settings.Fullscreen,firstController:firstController);
            DisplayProfiles.Apply(definition.Id,executable,game.System,DisplayDetection.Current(),settings);
            var hint=EmulatorSetup.FirmwareHint(game.System,request.DataDirectory,EmulatorSetup.ConfiguredFirmwareDirectory(definition.Id,executable,request.DataDirectory));if(hint!=null)throw new IOException(hint);
        }
        progress?.Report(new("Spiel wird gestartet …"));
        token.ThrowIfCancellationRequested();
        using var process=new ExternalEmulatorAdapter(definition).Launch(game,executable,settings.CorePaths.GetValueOrDefault(game.System),settings.Fullscreen);
        store.MarkPlayed(game);started();await process.WaitForExitAsync(token);
        if(process.ExitCode!=0)throw new IOException($"{definition.Name} wurde mit Fehlercode {process.ExitCode} beendet. BIOS, ROM und Emulator-Protokoll prüfen.");
    }
}
