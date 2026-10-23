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
using Xunit;

namespace EmuVora.Tests;

public class CollectionTests
{
    private static AppSettings Settings()=>new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false};
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Click(Window window,string label)=>window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string==label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    [Fact] public void NamesAndMembersRemainUniqueAcrossRenameAndReload()
    {
        using var dir=new TestDirectory();var settings=Settings();GameCollections.Create(settings,"  Couch  ");settings.Collections["Couch"].Add("D:/Game.sfc");
        Assert.Throws<InvalidDataException>(()=>GameCollections.Create(settings,"couch"));Assert.Throws<InvalidDataException>(()=>GameCollections.Create(settings,"\n"));Assert.Throws<InvalidDataException>(()=>GameCollections.Rename(settings,"Couch",new string('x',61)));
        GameCollections.Create(settings,"Other");Assert.Throws<InvalidDataException>(()=>GameCollections.Rename(settings,"Couch","Other"));GameCollections.Rename(settings,"Couch","COUCH");
        var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(settings);var loaded=saved.Load();Assert.Contains("d:/GAME.sfc",loaded.Collections["couch"]);
        var copy=GameCollections.Copy(loaded);copy["Couch"].Clear();Assert.Single(loaded.Collections["couch"]);
    }
    [Fact] public void OldNullAndCaseVariantCollectionSettingsLoadWithoutLosingMemberships()
    {
        using var dir=new TestDirectory();var file=dir.File("settings.json","{\"Collections\":null}");Assert.Empty(new SettingsStore(file).Load().Collections);
        File.WriteAllText(file,"{\"Collections\":{\"Couch\":[\"a\"],\"couch\":[\"b\"],\"Empty\":null}}");var settings=new SettingsStore(file).Load();Assert.Equal(2,settings.Collections["Couch"].Count);Assert.Empty(settings.Collections["Empty"]);
    }
    [Fact] public void MigrationDoesNotInventLegacyDatesAndRescanPreservesFirstImport()
    {
        using var dir=new TestDirectory();var file=Path.Combine(dir.Path,"library.db");using(var db=new SqliteConnection($"Data Source={file}")){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="CREATE TABLE games(path TEXT PRIMARY KEY,title TEXT NOT NULL,system TEXT NOT NULL);INSERT INTO games VALUES('legacy','Old','NES');";cmd.ExecuteNonQuery();}
        var store=new LibraryStore(file);Assert.Empty(store.GetAddedTimes());store.Upsert([new("New","SNES","new"),new("Old","NES","legacy")]);var first=Assert.Single(store.GetAddedTimes());Assert.Equal("new",first.Key);
        store.Upsert([new("Changed","NES","NEW")]);store.Update(new("Edited","SNES","new",true));store.MarkPlayed(new("Edited","SNES","new"));Assert.Equal(first.Value,store.GetAddedTimes()["new"]);
        Assert.Equal(2,store.GetAll().Count);Assert.Equal("Edited",store.GetAll().Single(g=>g.Path=="new").Title);
    }
    [AvaloniaFact] public async Task CollectionsFilterWithSearchAndCancelOrSaveGameMembership()
    {
        using var dir=new TestDirectory();var settings=Settings();GameCollections.Create(settings,"Couch");GameCollections.Create(settings,"Later");var games=new[]{new Game("Alpha","SNES","alpha.sfc"),new Game("Beta","NES","beta.nes")};settings.Collections["Couch"].Add(games[0].Path);
        var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(settings);new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert(games);
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            var choice=window.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.PlaceholderText as string=="Sammlung auswählen");choice.SelectedItem="Couch";Render(window);var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Single(list.Items);list.SelectedIndex=0;
            var task=(Task)typeof(MainWindow).GetMethod("ChooseCollectionsAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,null)!;var dialog=Assert.Single(window.OwnedWindows);Render(dialog);
            dialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Content as string=="Later").IsChecked=true;Click(dialog,"Abbrechen");await task;Assert.Empty(saved.Load().Collections["Later"]);
            task=(Task)typeof(MainWindow).GetMethod("ChooseCollectionsAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,null)!;dialog=Assert.Single(window.OwnedWindows);Render(dialog);
            dialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Content as string=="Couch").IsChecked=false;dialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Content as string=="Later").IsChecked=true;Click(dialog,"Zuordnung speichern");await task;Render(window);Assert.Empty(list.Items);
            choice.SelectedItem="Later";Render(window);Assert.Single(list.Items);Assert.Contains(games[0].Path,saved.Load().Collections["Later"]);
            window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Watermark as string=="Spiele suchen …").Text="Beta";Render(window);Assert.Empty(list.Items);
        }
        finally{window.Close();}
    }
    [AvaloniaFact] public async Task ManageCollectionsRenamesAndDeletesOnlyTheCollection()
    {
        using var dir=new TestDirectory();var rom=dir.File("game.nes","owned");var settings=Settings();GameCollections.Create(settings,"Old");settings.Collections["Old"].Add(rom);var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(settings);var library=new LibraryStore(Path.Combine(dir.Path,"library.db"));library.Upsert([new("Game","NES",rom)]);library.Update(new("Game","NES",rom,true));
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            var task=(Task)typeof(MainWindow).GetMethod("ManageCollectionsAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,null)!;var dialog=Assert.Single(window.OwnedWindows);Render(dialog);
            dialog.GetVisualDescendants().OfType<ListBox>().Single().SelectedItem="Old";dialog.GetVisualDescendants().OfType<TextBox>().Single().Text="New";Click(dialog,"Sammlung umbenennen");Assert.Contains(rom,saved.Load().Collections["New"]);
            Render(dialog);using(var frame=dialog.CaptureRenderedFrame()){Assert.NotNull(frame);var output=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(output!=null){Directory.CreateDirectory(output);frame.Save(Path.Combine(output,"Collections-management.png"));}}
            Click(dialog,"Sammlung löschen · Spiele behalten");Assert.Empty(saved.Load().Collections);Assert.Equal("owned",File.ReadAllText(rom));Assert.True(Assert.Single(new LibraryStore(Path.Combine(dir.Path,"library.db")).GetAll()).Favorite);Click(dialog,"Fertig");await task;
        }
        finally{window.Close();}
    }
    [AvaloniaFact] public void RecentlyAddedIsOrderedAndRetainsThePageLimit()
    {
        using var dir=new TestDirectory();new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(Settings());var file=Path.Combine(dir.Path,"library.db");new LibraryStore(file).Upsert(Enumerable.Range(0,60).Select(i=>new Game($"Fixture {i:00}","SNES",$"fixture{i}.sfc")));
        using(var db=new SqliteConnection($"Data Source={file}")){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE games SET addedAt='2020-01-01T00:00:00+00:00';UPDATE games SET addedAt='2026-01-01T00:00:00+00:00' WHERE path='fixture59.sfc';";cmd.ExecuteNonQuery();}
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try{Click(window,"Zuletzt hinzugefügt");Render(window);var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(48,list.ItemCount);Assert.Equal("fixture59.sfc",Assert.IsType<Game>(list.Items[0]).Path);}
        finally{window.Close();}
    }
    [Fact] public async Task FullBackupRestoresCollectionsAndImportDates()
    {
        using var dir=new TestDirectory();var source=Path.Combine(dir.Path,"data");Directory.CreateDirectory(source);var settings=Settings();GameCollections.Create(settings,"Couch");settings.Collections["Couch"].Add("D:/game.nes");new SettingsStore(Path.Combine(source,"settings.json")).Save(settings);var library=new LibraryStore(Path.Combine(source,"library.db"));library.Upsert([new("Game","NES","D:/game.nes")]);var added=library.GetAddedTimes()["D:/game.nes"];
        var archive=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(source,archive);var restored=await Backups.RestoreAsync(archive,dir.Path);Assert.Contains("d:/GAME.nes",new SettingsStore(Path.Combine(restored,"settings.json")).Load().Collections["Couch"]);Assert.Equal(added,new LibraryStore(Path.Combine(restored,"library.db")).GetAddedTimes()["D:/game.nes"]);
    }
    [AvaloniaFact] public async Task TvModeCanOpenMembershipAndCommitUsingControllerNavigation()
    {
        using var dir=new TestDirectory();var settings=Settings();settings.TvMode=true;GameCollections.Create(settings,"Couch");var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(settings);new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert([new("Game","NES","game.nes")]);
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            window.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex=0;var button=window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Zu Sammlungen zuordnen");Assert.True(button.IsEffectivelyVisible);Assert.True(button.IsEnabled);
            var task=(Task)typeof(MainWindow).GetMethod("ChooseCollectionsAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,null)!;var dialog=Assert.Single(window.OwnedWindows);Render(dialog);
            dialog.GetVisualDescendants().OfType<CheckBox>().Single().Focus();ControllerUi.Handle(dialog,0x1000);dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Zuordnung speichern").Focus();ControllerUi.Handle(dialog,0x1000);await task;
            Assert.Contains("game.nes",saved.Load().Collections["Couch"]);
        }
        finally{window.Close();}
    }
}
