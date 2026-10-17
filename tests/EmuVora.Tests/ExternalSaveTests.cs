using System.IO.Compression;
using System.Text.Json;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;
public class ExternalSaveTests
{
    private const string Id="1234567890abcdef1234567890abcdef";
    [Fact] public async Task ExternalSavesRestoreSeparatelyAndCanBeBackedUpAgain()
    {
        using var dir=new TestDirectory();var data=CreateData(dir);
        var original=dir.File("outside/custom/game.sav","saved progress");
        var settings=new SettingsStore(Path.Combine(data,"settings.json"));settings.Save(new(){ExternalSaveFolders=[new(Id,Path.GetDirectoryName(original)!)]});
        var archive=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(data,archive);
        Assert.Equal(2,Backups.Inspect(archive).Format);Assert.Equal(Path.GetDirectoryName(original),Assert.Single(Backups.Inspect(archive).ExternalFolders!).Path);
        File.WriteAllText(original,"new progress");var restored=await Backups.RestoreAsync(archive,dir.Path);
        var folder=Assert.Single(new SettingsStore(Path.Combine(restored,"settings.json")).Load().ExternalSaveFolders);
        Assert.StartsWith(restored,folder.Path);Assert.Equal(Id,folder.Id);
        Assert.Equal("saved progress",File.ReadAllText(Path.Combine(folder.Path,"game.sav")));Assert.Equal("new progress",File.ReadAllText(original));
        Assert.Contains(Path.GetDirectoryName(original)!.Replace("\\","\\\\"),File.ReadAllText(Path.Combine(restored,"external-save-folders.json")));
        var second=Path.Combine(dir.Path,"second.zip");await Backups.CreateAsync(restored,second);var again=await Backups.RestoreAsync(second,dir.Path);
        var againFolder=Assert.Single(new SettingsStore(Path.Combine(again,"settings.json")).Load().ExternalSaveFolders);
        Assert.Equal("saved progress",File.ReadAllText(Path.Combine(againFolder.Path,"game.sav")));
    }
    [Fact] public async Task MissingExternalFolderAndBackupInsideSelectedFolderAreRejected()
    {
        using var dir=new TestDirectory();var data=CreateData(dir);var store=new SettingsStore(Path.Combine(data,"settings.json"));
        var missing=Path.Combine(dir.Path,"missing");store.Save(new(){ExternalSaveFolders=[new(Id,missing)]});
        var archive=Path.Combine(dir.Path,"backup.zip");await Assert.ThrowsAsync<DirectoryNotFoundException>(()=>Backups.CreateAsync(data,archive));Assert.False(File.Exists(archive));
        Directory.CreateDirectory(missing);await Assert.ThrowsAsync<IOException>(()=>Backups.CreateAsync(data,Path.Combine(missing,"backup.zip")));
        Assert.Empty(Directory.GetFiles(missing));
    }
    [Fact] public void OverlapsDuplicatesAncestorsAndDriveRootsCannotBeSelected()
    {
        using var dir=new TestDirectory();var data=CreateData(dir);Directory.CreateDirectory(Path.Combine(dir.Path,"outside/child"));
        var outside=Path.Combine(dir.Path,"outside");var other="abcdef1234567890abcdef1234567890";
        Assert.Throws<IOException>(()=>ExternalSaves.ValidateSelection(data,[new(Id,outside),new(other,Path.Combine(outside,"child"))]));
        Assert.Throws<IOException>(()=>ExternalSaves.ValidateSelection(data,[new(Id,outside),new(other,outside)]));
        Assert.Throws<IOException>(()=>ExternalSaves.ValidateSelection(data,[new(Id,dir.Path)]));
        Assert.Throws<IOException>(()=>ExternalSaves.ValidateSelection(data,[new(Id,Path.GetPathRoot(dir.Path)!)]));
        Assert.Throws<IOException>(()=>ExternalSaves.ValidateSelection(data,[new("../bad",outside)]));
        var custom=Path.Combine(data,"custom-saves");Directory.CreateDirectory(custom);Assert.Single(ExternalSaves.ValidateSelection(data,[new(Id,custom)]));
    }
    [Fact] public async Task LegacyArchivesStillRestoreAndEmptyExternalFoldersAreRetained()
    {
        using var dir=new TestDirectory();var data=CreateData(dir);var archive=Path.Combine(dir.Path,"legacy.zip");await Backups.CreateAsync(data,archive);
        RewriteManifest(archive,m=>m with {Format=1,ExternalFolders=null});var restored=await Backups.RestoreAsync(archive,dir.Path);
        Assert.Empty(new SettingsStore(Path.Combine(restored,"settings.json")).Load().ExternalSaveFolders);
        var empty=Path.Combine(dir.Path,"empty");Directory.CreateDirectory(empty);new SettingsStore(Path.Combine(data,"settings.json")).Save(new(){ExternalSaveFolders=[new(Id,empty)]});
        var emptyArchive=Path.Combine(dir.Path,"empty.zip");await Backups.CreateAsync(data,emptyArchive);var emptyRestored=await Backups.RestoreAsync(emptyArchive,dir.Path);
        Assert.True(Directory.Exists(Assert.Single(new SettingsStore(Path.Combine(emptyRestored,"settings.json")).Load().ExternalSaveFolders).Path));
    }
    [Fact] public async Task UndeclaredExternalFilesCannotBeRestored()
    {
        using var dir=new TestDirectory();var data=CreateData(dir);var file=dir.File("outside/game.sav","progress");new SettingsStore(Path.Combine(data,"settings.json")).Save(new(){ExternalSaveFolders=[new(Id,Path.GetDirectoryName(file)!)]});
        var archive=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(data,archive);RewriteManifest(archive,m=>m with {ExternalFolders=[]});
        await Assert.ThrowsAsync<IOException>(()=>Backups.RestoreAsync(archive,dir.Path));Assert.Equal("progress",File.ReadAllText(file));
        Assert.DoesNotContain(Directory.GetDirectories(dir.Path),p=>Path.GetFileName(p).StartsWith(".emuvora-restore"));
    }
    private static string CreateData(TestDirectory dir)
    {
        var path=Path.Combine(dir.Path,"data");Directory.CreateDirectory(path);new SettingsStore(Path.Combine(path,"settings.json")).Save(new());_=new LibraryStore(Path.Combine(path,"library.db"));return path;
    }
    private static void RewriteManifest(string path,Func<BackupManifest,BackupManifest> change)
    {
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("manifest.json")!;BackupManifest manifest;
        using(var stream=entry.Open())manifest=JsonSerializer.Deserialize<BackupManifest>(stream,SettingsStore.JsonOptions)!;
        entry.Delete();using var output=zip.CreateEntry("manifest.json").Open();JsonSerializer.Serialize(output,change(manifest),SettingsStore.JsonOptions);
    }
}
