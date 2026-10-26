using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace EmuVora.Tests;

public class RomRelocationTests
{
    private static AppSettings Settings()=>new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false};
    private static void Sql(string data,string sql){using var db=new SqliteConnection($"Data Source={Path.Combine(data,"library.db")}");db.Open();using var cmd=db.CreateCommand();cmd.CommandText=sql;cmd.ExecuteNonQuery();}
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Click(Window window,string label)=>window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string==label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static string Data(TestDirectory dir){var data=Path.Combine(dir.Path,"data");Directory.CreateDirectory(data);new SettingsStore(Path.Combine(data,"settings.json")).Save(Settings());_=new LibraryStore(Path.Combine(data,"library.db"));return data;}
    [Fact] public void PreviewUsesRelativePathsAndSkipsExistingSourcesCollisionsAndLookalikeRoots()
    {
        using var dir=new TestDirectory();var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");dir.File("new/SNES/Mario.sfc","rom");dir.File("new/NES/Same.nes","rom");dir.File("new/Exists.nes","other");var existing=dir.File("old/Exists.nes","old");
        var games=new[]{new Game("Mario","SNES",Path.Combine(old,"SNES/Mario.sfc")),new Game("Missing","NES",Path.Combine(old,"Missing.nes")),new Game("Collision","NES",Path.Combine(old,"NES/Same.nes")),new Game("Target","NES",Path.Combine(target,"NES/Same.nes")),new Game("Existing","NES",existing),new Game("Outside","NES",Path.Combine(dir.Path,"old-copy/Mario.sfc"))};
        var plan=RomRelocation.Preview(games,old,target);Assert.Equal(games[0],Assert.Single(plan.Suggestions).Game);Assert.Equal(2,plan.Notes.Count);Assert.Equal("rom",File.ReadAllText(plan.Suggestions[0].NewPath));
        Assert.Throws<IOException>(()=>RomRelocation.Preview(games,"relative",target));Assert.Throws<IOException>(()=>RomRelocation.Preview(games,target,target));using var cancelled=new CancellationTokenSource();cancelled.Cancel();Assert.Throws<OperationCanceledException>(()=>RomRelocation.Preview(games,old,target,cancelled.Token));
    }
    [Fact] public void ApplyKeepsAllMetadataAndMovesOnlyPathKeyedSettingsWithABackup()
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");var newRom=Path.GetFullPath(dir.File("new/SNES/Game.sfc","rom"));var path=Path.Combine(old,"SNES/Game.sfc");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Original","SNES",path)]);store.Update(new("My title","SNES",path,true));store.MarkPlayed(new("My title","SNES",path));var game=Assert.Single(store.GetAll());var added=store.GetAddedTimes()[path];
        var settings=Settings();var cover=dir.File("data/covers/own.png","cover");settings.Covers[path]=cover;settings.ProtectedCovers.Add(path);settings.GameAspectRatios[path]="16:9";GameCollections.Create(settings,"Couch");settings.Collections["Couch"].Add(path);settings.RomFolders.Add(old);new SettingsStore(Path.Combine(data,"settings.json")).Save(settings);
        var result=RomRelocation.Apply(data,RomRelocation.Preview([game],old,target));Assert.Equal(1,result.Changed);Assert.Equal(game with{Path=newRom},Assert.Single(store.GetAll()));Assert.Equal(added,store.GetAddedTimes()[newRom]);Assert.Empty(store.RecognitionCandidates("SNES"));
        var loaded=new SettingsStore(Path.Combine(data,"settings.json")).Load();Assert.Equal(cover,loaded.Covers[newRom]);Assert.False(loaded.Covers.ContainsKey(path));Assert.Contains(newRom,loaded.ProtectedCovers);Assert.Equal("16:9",loaded.GameAspectRatios[newRom]);Assert.Contains(newRom,loaded.Collections["Couch"]);Assert.DoesNotContain(path,loaded.Collections["Couch"]);Assert.DoesNotContain(old,loaded.RomFolders);Assert.Contains(target,loaded.RomFolders);
        Assert.Equal("rom",File.ReadAllText(newRom));Assert.Equal("cover",File.ReadAllText(cover));Assert.False(File.Exists(path));Assert.True(File.Exists(Path.Combine(result.BackupDirectory,"library.db")));Assert.Equal(game,Assert.Single(new LibraryStore(Path.Combine(result.BackupDirectory,"library.db")).GetAll()));Assert.False(File.Exists(Path.Combine(data,"rom-relocation-pending.json")));
    }
    [Theory][InlineData("target")][InlineData("source")][InlineData("metadata")][InlineData("collision")][InlineData("settings")]
    public void ChangesAfterPreviewAreRejectedWithoutPartialUpdates(string change)
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");var newRom=dir.File("new/Game.nes","rom");var game=new Game("Game","NES",Path.Combine(old,"Game.nes"));var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([game]);var plan=RomRelocation.Preview(store.GetAll(),old,target);
        if(change=="target")File.AppendAllText(newRom,"changed");if(change=="source")dir.File("old/Game.nes","returned");if(change=="metadata")store.Update(game with{Title="Edited"});if(change=="collision")store.Upsert([game with{Path=newRom}]);
        if(change=="settings"){var settings=Settings();settings.Covers[newRom]=dir.File("own.png","own");new SettingsStore(Path.Combine(data,"settings.json")).Save(settings);}
        var before=store.GetAll().ToArray();var json=File.ReadAllText(Path.Combine(data,"settings.json"));Assert.Throws<IOException>(()=>RomRelocation.Apply(data,plan));Assert.Equal(before,store.GetAll());Assert.Equal(json,File.ReadAllText(Path.Combine(data,"settings.json")));Assert.False(File.Exists(Path.Combine(data,"rom-relocation-pending.json")));
    }
    [Fact] public void FailedSecondSqlUpdateRollsBackFirstAndRecoversTheJournal()
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");dir.File("new/One.nes","one");dir.File("new/Two.nes","two");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("One","NES",Path.Combine(old,"One.nes")),new("Two","NES",Path.Combine(old,"Two.nes"))]);var before=store.GetAll().ToArray();var settings=File.ReadAllText(Path.Combine(data,"settings.json"));
        Sql(data,"CREATE TRIGGER fail_move BEFORE UPDATE OF path ON games WHEN OLD.title='Two' BEGIN SELECT RAISE(ABORT,'synthetic failure'); END;");Assert.Throws<SqliteException>(()=>RomRelocation.Apply(data,RomRelocation.Preview(before,old,target)));
        Assert.Equal(before,store.GetAll());Assert.Equal(settings,File.ReadAllText(Path.Combine(data,"settings.json")));Assert.False(File.Exists(Path.Combine(data,"rom-relocation-pending.json")));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void InterruptedPairRecoversSettingsToTheCommittedDatabaseState(bool committed)
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old/Game.nes");var target=dir.File("new/Game.nes","rom");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Game","NES",old)]);
        var before=Settings();before.Covers[old]="cover.png";var after=Settings();after.Covers[target]="cover.png";var oldJson=JsonSerializer.Serialize(before,SettingsStore.JsonOptions);var newJson=JsonSerializer.Serialize(after,SettingsStore.JsonOptions);
        if(committed){using var db=new SqliteConnection($"Data Source={Path.Combine(data,"library.db")}");db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE games SET path=$new";cmd.Parameters.AddWithValue("$new",target);cmd.ExecuteNonQuery();}
        File.WriteAllText(Path.Combine(data,"settings.json"),committed?oldJson:newJson);File.WriteAllText(Path.Combine(data,"rom-relocation-pending.json"),JsonSerializer.Serialize(new{OldSettings=oldJson,NewSettings=newJson,Changes=new[]{new{OldPath=old,NewPath=target}}}));
        var loaded=new SettingsStore(Path.Combine(data,"settings.json")).Load();Assert.Equal("cover.png",loaded.Covers[committed?target:old]);Assert.False(loaded.Covers.ContainsKey(committed?old:target));Assert.False(File.Exists(Path.Combine(data,"rom-relocation-pending.json")));RomRelocation.Recover(data);
    }
    [Fact] public void InconsistentJournalBlocksInsteadOfResettingSettings()
    {
        using var dir=new TestDirectory();var data=Data(dir);var before=File.ReadAllText(Path.Combine(data,"settings.json"));File.WriteAllText(Path.Combine(data,"rom-relocation-pending.json"),"invalid");Assert.Throws<IOException>(()=>new SettingsStore(Path.Combine(data,"settings.json")).Load());Assert.Equal(before,File.ReadAllText(Path.Combine(data,"settings.json")));Assert.True(File.Exists(Path.Combine(data,"rom-relocation-pending.json")));
    }
    [Fact] public void CueAndPlaylistReferencesMustRemainRelativeAvailableAndInsideTheNewRoot()
    {
        using var dir=new TestDirectory();var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");dir.File("new/discs/disc.bin","disc");var cue=dir.File("new/discs/disc.cue","FILE \"disc.bin\" BINARY\nTRACK 01 MODE1/2352");var list=dir.File("new/Game.m3u","discs/disc.cue");var game=new Game("Game","PS1",Path.Combine(old,"Game.m3u"));Assert.Single(RomRelocation.Preview([game],old,target).Suggestions);
        File.WriteAllText(cue,"FILE \"missing.bin\" BINARY");Assert.Empty(RomRelocation.Preview([game],old,target).Suggestions);File.WriteAllText(list,Path.GetFullPath(cue));Assert.Empty(RomRelocation.Preview([game],old,target).Suggestions);File.WriteAllText(list,"../outside.bin");dir.File("outside.bin","outside");Assert.Empty(RomRelocation.Preview([game],old,target).Suggestions);
    }
    [Fact] public void CancellationPreservesLibraryAndSettings()
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");dir.File("new/Game.nes","rom");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Game","NES",Path.Combine(old,"Game.nes"))]);var before=store.GetAll().ToArray();var json=File.ReadAllText(Path.Combine(data,"settings.json"));var plan=RomRelocation.Preview(before,old,target);using var cancelled=new CancellationTokenSource();cancelled.Cancel();Assert.Throws<OperationCanceledException>(()=>RomRelocation.Apply(data,plan,cancelled.Token));Assert.Equal(before,store.GetAll());Assert.Equal(json,File.ReadAllText(Path.Combine(data,"settings.json")));
    }
    [AvaloniaTheory][InlineData(false)][InlineData(true)]
    public async Task PreviewOnlyAppliesAfterConfirmationAndReloadsTheWindow(bool accept)
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");var newRom=dir.File("new/Game.nes","rom");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Game","NES",Path.Combine(old,"Game.nes"))]);var original=Assert.Single(store.GetAll());var plan=RomRelocation.Preview([original],old,target);var window=new MainWindow(data);window.Show();Render(window);
        try
        {
            var feedback=new TextBlock();var task=(Task)typeof(MainWindow).GetMethod("ReviewRomRelocationAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,[plan,feedback,window])!;var dialog=Assert.Single(window.OwnedWindows);Render(dialog);Assert.Equal(original,Assert.Single(store.GetAll()));
            using(var frame=dialog.CaptureRenderedFrame()){Assert.NotNull(frame);var output=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(output!=null){Directory.CreateDirectory(output);frame.Save(Path.Combine(output,"ROM-relocation-preview.png"));}}
            Click(dialog,accept?"Geprüfte Pfade übernehmen":"Abbrechen");await task;Assert.Equal(accept?Path.GetFullPath(newRom):original.Path,Assert.Single(store.GetAll()).Path);Assert.True(File.Exists(newRom));
        }
        finally{window.Close();}
    }
    [Fact] public void ADisappearedCueDependencyInvalidatesTheWholePreview()
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");dir.File("new/Game.cue","FILE \"disc.bin\" BINARY");var dependency=dir.File("new/disc.bin","disc");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Game","PS1",Path.Combine(old,"Game.cue"))]);var original=Assert.Single(store.GetAll());var plan=RomRelocation.Preview([original],old,target);File.Delete(dependency);Assert.Throws<IOException>(()=>RomRelocation.Apply(data,plan));Assert.Equal(original,Assert.Single(store.GetAll()));
    }
    [Fact] public void SettingsWriteFailureRollsBackSqlAndLeavesThePreviousConfiguration()
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");dir.File("new/Game.nes","rom");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Game","NES",Path.Combine(old,"Game.nes"))]);var original=Assert.Single(store.GetAll());var file=Path.Combine(data,"settings.json");var before=File.ReadAllText(file);var plan=RomRelocation.Preview([original],old,target);
        File.SetAttributes(file,FileAttributes.ReadOnly);
        try{Assert.ThrowsAny<Exception>(()=>RomRelocation.Apply(data,plan));Assert.Equal(original,Assert.Single(store.GetAll()));Assert.Equal(before,File.ReadAllText(file));Assert.False(File.Exists(Path.Combine(data,"rom-relocation-pending.json")));}
        finally{foreach(var path in Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories))File.SetAttributes(path,FileAttributes.Normal);}
    }
    [Fact] public void MixedDatabaseStateDoesNotOverwriteEitherSettingsOrJournal()
    {
        using var dir=new TestDirectory();var data=Data(dir);var before=File.ReadAllText(Path.Combine(data,"settings.json"));var old=Path.Combine(dir.Path,"old/Game.nes");var target=dir.File("new/Game.nes","rom");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Old","NES",old),new("New","NES",target)]);
        var journal=Path.Combine(data,"rom-relocation-pending.json");File.WriteAllText(journal,JsonSerializer.Serialize(new{OldSettings=before,NewSettings=before,Changes=new[]{new{OldPath=old,NewPath=target}}}));Assert.Throws<IOException>(()=>new SettingsStore(Path.Combine(data,"settings.json")).Load());Assert.Equal(before,File.ReadAllText(Path.Combine(data,"settings.json")));Assert.True(File.Exists(journal));Assert.Equal(2,store.GetAll().Count);
    }
    [Fact] public void HundredsOfMappingsKeepCaseInsensitiveMembershipAndSharedArtwork()
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");var games=Enumerable.Range(0,512).Select(i=>new Game($"Fixture {i:000}","NES",Path.Combine(old,$"game{i}.nes"))).ToArray();var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert(games);var settings=Settings();GameCollections.Create(settings,"Couch");var cover=dir.File("data/covers/shared.png","cover");
        foreach(var game in games){dir.File("new/"+Path.GetFileName(game.Path),"fixture");settings.Covers[game.Path.ToUpperInvariant()]=cover;settings.Collections["Couch"].Add(game.Path.ToUpperInvariant());}
        new SettingsStore(Path.Combine(data,"settings.json")).Save(settings);var plan=RomRelocation.Preview(store.GetAll(),old,target);Assert.Equal(512,plan.Suggestions.Count);Assert.Equal(512,RomRelocation.Apply(data,plan).Changed);var loaded=new SettingsStore(Path.Combine(data,"settings.json")).Load();Assert.Equal(512,loaded.Collections["Couch"].Count);Assert.All(store.GetAll(),game=>{Assert.True(File.Exists(game.Path));Assert.Equal(cover,loaded.Covers[game.Path]);Assert.Contains(game.Path,loaded.Collections["Couch"]);});Assert.Equal("cover",File.ReadAllText(cover));
    }
    [Fact] public void ConcurrentLibraryOperationPreventsRelocationWithoutChangingData()
    {
        using var dir=new TestDirectory();var data=Data(dir);var old=Path.Combine(dir.Path,"old");var target=Path.Combine(dir.Path,"new");dir.File("new/Game.nes","rom");var store=new LibraryStore(Path.Combine(data,"library.db"));store.Upsert([new("Game","NES",Path.Combine(old,"Game.nes"))]);var game=Assert.Single(store.GetAll());var before=File.ReadAllText(Path.Combine(data,"settings.json"));var plan=RomRelocation.Preview([game],old,target);
        using(var launch=new FileStream(Path.Combine(data,".game-launch.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose))Assert.Throws<IOException>(()=>RomRelocation.Apply(data,plan));
        Assert.Equal(game,Assert.Single(store.GetAll()));Assert.Equal(before,File.ReadAllText(Path.Combine(data,"settings.json")));Assert.Equal(1,RomRelocation.Apply(data,plan).Changed);
    }
}
