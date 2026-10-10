using EmuVora.Core;
using Microsoft.Data.Sqlite;
using System.IO.Compression;
using System.Security.Cryptography;
using Xunit;
namespace EmuVora.Tests;
public sealed class TestDirectory:IDisposable
{
    public string Path {get;}=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"emuvora-"+Guid.NewGuid());
    public TestDirectory()=>Directory.CreateDirectory(Path);
    public string File(string name,string content=""){var file=System.IO.Path.Combine(Path,name);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);System.IO.File.WriteAllText(file,content);return file;}
    public void Dispose(){SqliteConnection.ClearAllPools();Directory.Delete(Path,true);}
}
public class LibraryTests
{
    [Fact] public void RescanPreservesCorrectionFavoritesAndHistory()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db"));var path=dir.File("game.iso");
        store.Upsert([new("game","Unknown disc",path)]);store.Update(new("My game","PS2",path,true));store.MarkPlayed(new("My game","PS2",path));store.Upsert([new("game","Unknown disc",path.ToUpperInvariant())]);
        var game=Assert.Single(store.GetAll());Assert.Equal("My game",game.Title);Assert.Equal("PS2",game.System);Assert.True(game.Favorite);Assert.Equal(1,game.PlayCount);Assert.NotNull(game.LastPlayed);
    }
    [Fact] public void MigratesOriginalDatabaseWithoutLosingGames()
    {
        using var dir=new TestDirectory();var file=System.IO.Path.Combine(dir.Path,"library.db");using(var c=new SqliteConnection($"Data Source={file}")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="CREATE TABLE games(path TEXT PRIMARY KEY,title TEXT NOT NULL,system TEXT NOT NULL);INSERT INTO games VALUES('C:\\ROMs\\game.nes','Game','NES');";cmd.ExecuteNonQuery();}
        var game=Assert.Single(new LibraryStore(file).GetAll());Assert.Equal("Game",game.Title);Assert.False(game.Favorite);Assert.Equal(0,game.PlayCount);
    }
    [Fact] public void RemoveDoesNotDeleteRomAndRestoreKeepsMetadata()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db"));var path=dir.File("test.nes");var game=new Game("Test","NES",path,true,DateTimeOffset.UtcNow,3);store.Restore([game]);store.Remove(game);Assert.Empty(store.GetAll());Assert.True(System.IO.File.Exists(path));store.Restore([game]);Assert.Equal(game,Assert.Single(store.GetAll()));
    }
    [Fact] public void RestoreRollsBackWholeTransactionOnInvalidEntry()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(System.IO.Path.Combine(dir.Path,"library.db"));Assert.Throws<InvalidDataException>(()=>store.Restore([new("Valid","NES",dir.File("a.nes")),new("Invalid","NES","relative.nes")]));Assert.Empty(store.GetAll());
    }
    [Fact] public void PlaylistSuppressesDiscEntriesAndNestedFolderClassifies()
    {
        using var dir=new TestDirectory();dir.File("PS1/Game/disc1.chd");dir.File("PS1/Game/disc2.chd");dir.File("PS1/Game/Game.m3u","# discs\ndisc1.chd\ndisc2.chd\n");var game=Assert.Single(RomScanner.Scan(dir.Path));Assert.Equal("Game",game.Title);Assert.Equal("PS1",game.System);
    }
    [Fact] public void WiiHeaderOverridesFolderAndRvzRemainsAmbiguous()
    {
        using var dir=new TestDirectory();var file=dir.File("GC/game.iso");var bytes=new byte[32];bytes[24]=0x5D;bytes[25]=0x1C;bytes[26]=0x9E;bytes[27]=0xA3;System.IO.File.WriteAllBytes(file,bytes);dir.File("ambiguous.rvz");var games=RomScanner.Scan(dir.Path);Assert.Contains(games,g=>g.System=="Wii");Assert.Contains(games,g=>g.System=="Unknown disc");
    }
    [Fact] public void ScanHonorsCancellation()
    {using var dir=new TestDirectory();using var cts=new CancellationTokenSource();cts.Cancel();Assert.Throws<OperationCanceledException>(()=>RomScanner.ScanDetailed(dir.Path,cts.Token));}
    [Fact] public void LegacyCoreAndNullCollectionsAreMigrated()
    {
        using var dir=new TestDirectory();var file=dir.File("settings.json","{\"CorePaths\":{\"retroarch\":\"core.dll\"},\"EmulatorPaths\":null}");var settings=new SettingsStore(file).Load();Assert.Empty(settings.EmulatorPaths);Assert.Equal("core.dll",settings.CorePaths["SNES"]);
    }
    [Fact] public void CorruptSettingsArePreservedAndAtomicSaveCreatesBackup()
    {
        using var dir=new TestDirectory();var file=dir.File("settings.json","invalid");var store=new SettingsStore(file);store.Load();Assert.NotNull(store.LoadWarning);Assert.Equal("invalid",System.IO.File.ReadAllText(file));store.Save(new());Assert.Equal("invalid",System.IO.File.ReadAllText(file+".bak"));Assert.Single(Directory.GetFiles(dir.Path,"*.invalid-*"));
    }
    [Fact] public void LaunchArgumentsKeepRomPathAsOneArgumentAndUsePerSystemCore()
    {
        using var dir=new TestDirectory();var exe=dir.File("retroarch.exe");var rom=dir.File("game with spaces.nes");var core=dir.File("core.dll");var adapter=new ExternalEmulatorAdapter(EmulatorCatalog.Get("retroarch"));var info=adapter.CreateStartInfo(new("Game","NES",rom),exe,core);Assert.Equal(new[]{"-L",core,"--fullscreen",rom},info.ArgumentList);Assert.False(info.UseShellExecute);Assert.Throws<InvalidOperationException>(()=>adapter.CreateStartInfo(new("Game","NES",rom),exe));
    }
}
public class PackageTests
{
    private sealed class FakeDownload(byte[] bytes):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new ByteArrayContent(bytes)});
    }
    [Fact] public async Task FailedDownloadLeavesExistingInstallationAndCleansStaging()
    {
        using var dir=new TestDirectory();var old=dir.File("duckstation/old/emulator.exe","previous");using var manager=new PackageManager(new FakeDownload([1,2,3]));
        var package=new EmulatorPackage("duckstation","1","https://github.com/stenzek/duckstation/releases/download/latest/package.zip",new string('a',64),"duckstation.exe");
        await Assert.ThrowsAsync<InvalidDataException>(()=>manager.InstallAsync(package,dir.Path));Assert.Equal("previous",System.IO.File.ReadAllText(old));Assert.Empty(Directory.GetDirectories(System.IO.Path.Combine(dir.Path,"duckstation"),"staging-*"));
    }
    [Theory][InlineData("../outside.exe")][InlineData("..\\outside.exe")][InlineData("C:\\outside.exe")][InlineData("/outside.exe")][InlineData("folder/test.exe:stream")]
    public void RejectsTraversalAndAlternateStreams(string entry)
    {using var dir=new TestDirectory();Assert.Throws<InvalidDataException>(()=>PackageManager.SafeEntryPath(dir.Path,entry));}
    [Fact] public void ExtractsValidZipAndRejectsTraversal()
    {
        using var dir=new TestDirectory();var zip=System.IO.Path.Combine(dir.Path,"test.zip");using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)){using var writer=new StreamWriter(archive.CreateEntry("folder/emulator.exe").Open());writer.Write("test");}
        var output=System.IO.Path.Combine(dir.Path,"out");Directory.CreateDirectory(output);PackageManager.ExtractSafely(zip,output);Assert.Equal("test",System.IO.File.ReadAllText(System.IO.Path.Combine(output,"folder/emulator.exe")));
        var bad=System.IO.Path.Combine(dir.Path,"bad.zip");using(var archive=ZipFile.Open(bad,ZipArchiveMode.Create))archive.CreateEntry("../outside.exe");Assert.Throws<InvalidDataException>(()=>PackageManager.ExtractSafely(bad,output));Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir.Path,"outside.exe")));
    }
    [Fact] public async Task RejectsHashMismatch()
    {using var dir=new TestDirectory();var file=dir.File("package.zip","test");await Assert.ThrowsAsync<InvalidDataException>(()=>PackageManager.VerifyFileAsync(file,new string('0',64)));await PackageManager.VerifyFileAsync(file,Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(file))));}
    [Fact] public void RejectsWrongProjectOrHttpDownload()
    {
        var package=new EmulatorPackage("duckstation","1","https://github.com/attacker/duckstation/releases/download/latest/package.zip",new string('a',64),"duckstation.exe");Assert.Throws<InvalidDataException>(()=>PackageManager.ValidatePackage(package));Assert.Throws<InvalidDataException>(()=>PackageManager.ValidatePackage(package with {Url="http://github.com/stenzek/duckstation/releases/download/latest/package.zip"}));
    }
}
