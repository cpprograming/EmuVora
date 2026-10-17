using System.Text.Json;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;
public class DataDirectoryTests
{
    [Fact] public void SwitchAndReturnRetainLibrariesAndRememberTheActualCurrentDirectory()
    {
        using var dir=new TestDirectory();var a=Data(dir,"a");var b=Data(dir,"b");var locator=dir.File("App/emuvora-location.json","{\"DataDirectory\":\"C:\\\\unused\"}");
        var aDatabase=File.ReadAllBytes(Path.Combine(a,"library.db"));var bDatabase=File.ReadAllBytes(Path.Combine(b,"library.db"));
        string? launched=null;var service=new DataDirectorySwitch(locator);var result=service.Switch(a,b,path=>launched=path);
        Assert.Equal(b,launched);Assert.Equal(1,result.Games);Assert.Equal(b,new DataLocation(locator).Read());Assert.Equal(a,DataDirectorySwitch.Previous(b));
        Assert.Equal(aDatabase,File.ReadAllBytes(Path.Combine(a,"library.db")));Assert.Equal(bDatabase,File.ReadAllBytes(Path.Combine(b,"library.db")));
        service.Switch(b,a,path=>launched=path);Assert.Equal(a,launched);Assert.Equal(b,DataDirectorySwitch.Previous(a));Assert.Equal("progress",File.ReadAllText(Path.Combine(a,"saves/game.sav")));
    }
    [Fact] public void FailedLaunchRestoresLocatorHistoryAndTheirBackupsExactly()
    {
        using var dir=new TestDirectory();var a=Data(dir,"a");var b=Data(dir,"b");var locator=dir.File("App/location.json","original locator");dir.File("App/location.json.bak","original locator backup");
        var history=dir.File("b/data-location-history.json","old history");dir.File("b/data-location-history.json.bak","old history backup");
        Assert.Throws<IOException>(()=>new DataDirectorySwitch(locator).Switch(a,b,_=>throw new IOException("Launch failed")));
        Assert.Equal("original locator",File.ReadAllText(locator));Assert.Equal("original locator backup",File.ReadAllText(locator+".bak"));
        Assert.Equal("old history",File.ReadAllText(history));Assert.Equal("old history backup",File.ReadAllText(history+".bak"));
        Assert.Equal("progress",File.ReadAllText(Path.Combine(b,"saves/game.sav")));
    }
    [Fact] public void MissingEmptyAndBrokenTargetsDoNotCreateReplacementLibraries()
    {
        using var dir=new TestDirectory();var a=Data(dir,"a");var locator=dir.File("App/location.json","unchanged");var missing=Path.Combine(dir.Path,"missing");var empty=Path.Combine(dir.Path,"empty");Directory.CreateDirectory(empty);
        Assert.Throws<IOException>(()=>DataDirectorySwitch.Inspect(missing));Assert.False(Directory.Exists(missing));
        Assert.Throws<IOException>(()=>DataDirectorySwitch.Inspect(empty));Assert.Empty(Directory.GetFiles(empty));
        var broken=Data(dir,"broken");File.WriteAllText(Path.Combine(broken,"settings.json"),"invalid");
        Assert.Throws<JsonException>(()=>new DataDirectorySwitch(locator).Switch(a,broken,_=>Assert.Fail("Must not launch")));
        Assert.Equal("unchanged",File.ReadAllText(locator));Assert.False(File.Exists(Path.Combine(broken,"data-location-history.json")));
        Assert.Throws<IOException>(()=>new DataDirectorySwitch(locator).Switch(a,a,_=>Assert.Fail("Must not launch")));
    }
    [Fact] public void CancellationAndUnavailablePreviousDirectoryKeepTheSavedLocation()
    {
        using var dir=new TestDirectory();var a=Data(dir,"a");var b=Data(dir,"b");var locator=dir.File("App/location.json",JsonSerializer.Serialize(new {DataDirectory=a}));
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(()=>new DataDirectorySwitch(locator).Switch(a,b,_=>Assert.Fail("Must not launch"),cancellation.Token));Assert.Equal(a,new DataLocation(locator).Read());
        new DataDirectorySwitch(locator).Switch(a,b,_=>{});var renamed=Path.Combine(dir.Path,"disconnected");Directory.Move(a,renamed);
        Assert.Equal(a,DataDirectorySwitch.Previous(b));Assert.Throws<IOException>(()=>new DataDirectorySwitch(locator).Switch(b,a,_=>Assert.Fail("Must not launch")));Assert.False(Directory.Exists(a));Assert.Equal(b,new DataLocation(locator).Read());
    }
    [Fact] public void InspectionRejectsAnUnrelatedDatabaseWithoutWritingSettingsBackups()
    {
        using var dir=new TestDirectory();var a=Data(dir,"a");File.Delete(Path.Combine(a,"settings.json.bak"));
        using(var database=new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder{DataSource=Path.Combine(a,"library.db"),Pooling=false}.ToString())){database.Open();using var command=database.CreateCommand();command.CommandText="DROP TABLE games";command.ExecuteNonQuery();}
        Assert.Throws<IOException>(()=>DataDirectorySwitch.Inspect(a));Assert.False(File.Exists(Path.Combine(a,"settings.json.bak")));
    }
    [Fact] public void BackupReminderIsOptInAndBecomesDueAtItsSelectedInterval()
    {
        var now=new DateTimeOffset(2026,10,17,12,0,0,TimeSpan.Zero);var settings=new AppSettings();Assert.False(BackupReminders.IsDue(settings,now));
        settings.BackupReminderDays=7;Assert.True(BackupReminders.IsDue(settings,now));settings.LastBackupAt=now.AddDays(-7).AddSeconds(1);Assert.False(BackupReminders.IsDue(settings,now));
        settings.LastBackupAt=now.AddDays(-7);Assert.True(BackupReminders.IsDue(settings,now));settings.BackupReminderDays=30;Assert.False(BackupReminders.IsDue(settings,now));settings.BackupReminderDays=0;Assert.False(BackupReminders.IsDue(settings,now));
    }
    private static string Data(TestDirectory dir,string name)
    {
        var path=Path.Combine(dir.Path,name);Directory.CreateDirectory(path);new SettingsStore(Path.Combine(path,"settings.json")).Save(new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false});
        var library=new LibraryStore(Path.Combine(path,"library.db"));library.Upsert([new("Game","NES",dir.File(name+"/game.nes","rom"))]);dir.File(name+"/saves/game.sav","progress");
        // Release fixture pools before byte comparisons or simulating a disconnected directory.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();return path;
    }
}
