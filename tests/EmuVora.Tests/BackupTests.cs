using System.IO.Compression;
using System.Text.Json;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class BackupTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task FirmwareBackupIsOptInAndRestoresOnlyIntoANewLibrary(bool include)
    {
        using var dir=new TestDirectory();var original=dir.File("data/firmware/dc/dc_boot.bin","synthetic firmware");dir.File("data/firmware/own.bin.bak","old firmware");
        var source=Path.Combine(dir.Path,"data");var settings=new SettingsStore(Path.Combine(source,"settings.json"));settings.Save(new(){IncludeFirmwareInBackups=include});_=new LibraryStore(Path.Combine(source,"library.db"));
        var archive=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(source,archive);var manifest=Backups.Inspect(archive);
        Assert.Equal(include?2:0,manifest.Files.Count(f=>f.Path.StartsWith("firmware/")));
        var restored=await Backups.RestoreAsync(archive,dir.Path);Assert.NotEqual(source,restored);Assert.Equal("synthetic firmware",File.ReadAllText(original));
        if(include){Assert.Equal("synthetic firmware",File.ReadAllText(Path.Combine(restored,"firmware/dc/dc_boot.bin")));Assert.Equal("old firmware",File.ReadAllText(Path.Combine(restored,"firmware/own.bin.bak")));}
        else Assert.False(Directory.Exists(Path.Combine(restored,"firmware")));
        Assert.Equal(include,new SettingsStore(Path.Combine(restored,"settings.json")).Load().IncludeFirmwareInBackups);
    }
    [Fact] public async Task CorruptedFirmwareFailsIntegrityWithoutChangingOriginals()
    {
        using var dir=new TestDirectory();var original=dir.File("data/firmware/own.bin","synthetic");var source=Path.Combine(dir.Path,"data");
        new SettingsStore(Path.Combine(source,"settings.json")).Save(new(){IncludeFirmwareInBackups=true});_=new LibraryStore(Path.Combine(source,"library.db"));var file=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(source,file);
        using(var zip=ZipFile.Open(file,ZipArchiveMode.Update)){zip.GetEntry("data/firmware/own.bin")!.Delete();using var entry=zip.CreateEntry("data/firmware/own.bin").Open();entry.Write("Synthetic"u8);}
        await Assert.ThrowsAsync<IOException>(()=>Backups.RestoreAsync(file,dir.Path));Assert.Equal("synthetic",File.ReadAllText(original));
        Assert.DoesNotContain(Directory.GetDirectories(dir.Path),p=>Path.GetFileName(p).StartsWith("EmuVora-Wiederhergestellt")||Path.GetFileName(p).StartsWith(".emuvora-restore"));
    }
    [Theory]
    [InlineData("duckstation","memcards")][InlineData("pcsx2","sstates")]
    [InlineData("dolphin","User/Wii/title")][InlineData("ppsspp","memstick/PSP/SAVEDATA/game")]
    public async Task ReinstallAutomaticallyCopiesRestoredSavesAndKeepsConflicts(string id,string folder)
    {
        using var dir=new TestDirectory();var source=Path.Combine(dir.Path,"data");Directory.CreateDirectory(source);
        var executable=dir.File($"data/emulators/{id}/old/emulator.exe","binary");
        dir.File($"data/emulators/{id}/old/{folder}/new.sav","recovered");dir.File($"data/emulators/{id}/old/{folder}/existing.sav","old save");
        var settings=new AppSettings();settings.EmulatorPaths[id]=executable;new SettingsStore(Path.Combine(source,"settings.json")).Save(settings);_=new LibraryStore(Path.Combine(source,"library.db"));
        var archive=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(source,archive);var restored=await Backups.RestoreAsync(archive,dir.Path);
        var target=Path.Combine(restored,"emulators",id,"new");Directory.CreateDirectory(Path.Combine(target,folder));var newExecutable=Path.Combine(target,"emulator.exe");File.WriteAllText(newExecutable,"new binary");
        File.WriteAllText(Path.Combine(target,folder,"existing.sav"),"new save");
        EmulatorSetup.Configure(id,newExecutable,restored);
        Assert.Equal("recovered",File.ReadAllText(Path.Combine(target,folder,"new.sav")));
        Assert.Equal("new save",File.ReadAllText(Path.Combine(target,folder,"existing.sav")));
        Assert.Equal("old save",File.ReadAllText(Path.Combine(restored,"emulators",id,"old",folder,"existing.sav")));
        Assert.Equal(new SaveTransferResult(0,0),SaveData.RestorePending(id,newExecutable,restored));
        Assert.Contains("1 vorhandene",File.ReadAllText(Path.Combine(restored,"save-restore-report.txt")));
    }
    [Fact] public void PendingSaveTransferCannotUseAnExternalFolder()
    {
        using var dir=new TestDirectory();dir.File("data/pending-save-restores.json","{\"duckstation\":\"../../outside\"}");
        var executable=dir.File("data/emulators/duckstation/new/duck.exe","binary");
        Assert.Throws<IOException>(()=>SaveData.RestorePending("duckstation",executable,Path.Combine(dir.Path,"data")));
        Assert.True(File.Exists(Path.Combine(dir.Path,"data/pending-save-restores.json")));
    }
    [Fact] public async Task RoundTripPreservesFavoritesAndSavesExcludesRomsAndRebasesCovers()
    {
        using var dir=new TestDirectory();var source=Path.Combine(dir.Path,"data");
        var cover=dir.File("data/covers/game.png","cover");dir.File("data/saves/game.srm","save");dir.File("data/states/game.state","state");
        dir.File("data/firmware/bios.bin","bios");dir.File("data/roms/game.nes","rom");
        var emulator=dir.File("data/emulators/duckstation/version/duck.exe","binary");dir.File("data/emulators/duckstation/version/memcards/card.mcd","card");
        var settings=new AppSettings();settings.Covers["D:/roms/game.nes"]=cover;settings.EmulatorPaths["duckstation"]=emulator;
        new SettingsStore(Path.Combine(source,"settings.json")).Save(settings);
        var library=new LibraryStore(Path.Combine(source,"library.db"));var game=new Game("Test","NES","D:/roms/game.nes",true);library.Upsert([game]);library.Update(game);
        var archive=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(source,archive);
        var manifest=Backups.Inspect(archive);Assert.DoesNotContain(manifest.Files,f=>f.Path.Contains("roms/")||f.Path.Contains("firmware/")||f.Path.EndsWith(".exe"));
        var restored=await Backups.RestoreAsync(archive,dir.Path);
        Assert.Equal("save",File.ReadAllText(Path.Combine(restored,"saves/game.srm")));
        Assert.Equal("card",File.ReadAllText(Path.Combine(restored,"emulators/duckstation/version/memcards/card.mcd")));
        Assert.True(Assert.Single(new LibraryStore(Path.Combine(restored,"library.db")).GetAll()).Favorite);
        var recovered=new SettingsStore(Path.Combine(restored,"settings.json")).Load();Assert.Equal(Path.Combine(restored,"covers","game.png"),recovered.Covers[game.Path]);
        Assert.Equal(cover,new SettingsStore(Path.Combine(source,"settings.json")).Load().Covers[game.Path]);
    }
    [Fact] public async Task TamperedArchiveDoesNotPublishRestoredDirectory()
    {
        using var dir=new TestDirectory();var archive=await MakeBackup(dir);
        using(var zip=ZipFile.Open(archive,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("data/settings.json")!;byte[] bytes;
            using(var input=entry.Open()){using var memory=new MemoryStream();input.CopyTo(memory);bytes=memory.ToArray();}
            entry.Delete();bytes[0]^=1;using var output=zip.CreateEntry("data/settings.json").Open();output.Write(bytes);
        }
        await Assert.ThrowsAsync<IOException>(()=>Backups.RestoreAsync(archive,dir.Path));
        Assert.DoesNotContain(Directory.GetDirectories(dir.Path),p=>Path.GetFileName(p).StartsWith("EmuVora-Wiederhergestellt")||Path.GetFileName(p).StartsWith(".emuvora-restore"));
    }
    [Theory][InlineData("../escape")][InlineData("C:/escape")][InlineData("folder\\escape")][InlineData("folder/file:stream")]
    public void TraversalPathsAreRejected(string path)
    {
        using var dir=new TestDirectory();var file=Path.Combine(dir.Path,"bad.zip");
        using(var zip=ZipFile.Open(file,ZipArchiveMode.Create)){using var stream=zip.CreateEntry("manifest.json").Open();JsonSerializer.Serialize(stream,new BackupManifest(1,dir.Path,DateTimeOffset.UtcNow,[new(path,0,new string('0',64))]));}
        Assert.Throws<IOException>(()=>Backups.Inspect(file));
    }
    [Fact] public async Task CancelAndExistingBackupLeaveOriginalsUntouched()
    {
        using var dir=new TestDirectory();var file=await MakeBackup(dir);var original=await File.ReadAllBytesAsync(file);
        await Assert.ThrowsAsync<IOException>(()=>Backups.CreateAsync(Path.Combine(dir.Path,"data"),file));Assert.Equal(original,await File.ReadAllBytesAsync(file));
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Backups.RestoreAsync(file,dir.Path,cancellation.Token));
        Assert.DoesNotContain(Directory.GetDirectories(dir.Path),p=>Path.GetFileName(p).StartsWith(".emuvora-restore"));
        await Assert.ThrowsAsync<IOException>(()=>Backups.CreateAsync(Path.Combine(dir.Path,"data"),Path.Combine(dir.Path,"data/backup.zip")));
    }
    private static async Task<string> MakeBackup(TestDirectory dir)
    {
        var source=Path.Combine(dir.Path,"data");Directory.CreateDirectory(source);new SettingsStore(Path.Combine(source,"settings.json")).Save(new());
        _=new LibraryStore(Path.Combine(source,"library.db"));var file=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(source,file);return file;
    }
}
