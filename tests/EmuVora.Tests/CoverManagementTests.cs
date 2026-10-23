using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;

public class CoverManagementTests
{
    private static AppSettings Settings()=>new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false};
    private static void Render(Window window){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Click(Window window,string label)=>window.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string==label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static string Image(TestDirectory dir,string name)
    {
        var path=Path.Combine(dir.Path,name);using var surface=SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(40,60));surface.Canvas.Clear(SkiaSharp.SKColors.Teal);
        using var picture=surface.Snapshot();using var png=picture.Encode(SkiaSharp.SKEncodedImageFormat.Png,100);File.WriteAllBytes(path,png.ToArray());return path;
    }
    private static Task<bool> Preview(MainWindow window,Game game,string file)=>(Task<bool>)typeof(MainWindow).GetMethod("PreviewCoverAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,[game,file,"Eigenes Bild · Test"])!;

    [Fact] public void ProtectionSurvivesOldNullSettingsAndCaseInsensitiveReload()
    {
        using var dir=new TestDirectory();var file=dir.File("settings.json","{\"Covers\":{},\"ProtectedCovers\":null}");var store=new SettingsStore(file);var settings=store.Load();Assert.Empty(settings.ProtectedCovers);
        settings.ProtectedCovers.Add("D:/Games/Own.sfc");store.Save(settings);settings=store.Load();Assert.Contains("d:/games/own.SFC",settings.ProtectedCovers);
        Assert.True(CoverLibrary.IsMissing(settings,"d:/games/own.SFC"));Assert.False(CoverLibrary.CanDownload(settings,"d:/games/own.SFC"));Assert.True(CoverLibrary.CanDownload(settings,"another"));
    }
    [Fact] public void ImmutableCopiesKeepOriginalAndRejectUnsupportedOrOversizedImages()
    {
        using var dir=new TestDirectory();var original=Image(dir,"own.png");var folder=Path.Combine(dir.Path,"covers");
        var first=CoverLibrary.CopyImage(original,folder);var second=CoverLibrary.CopyImage(original,folder);Assert.NotEqual(first,second);Assert.Equal(File.ReadAllBytes(original),File.ReadAllBytes(first));
        Assert.Throws<IOException>(()=>CoverLibrary.CopyImage(dir.File("bad.exe","x"),folder));
        var large=dir.File("large.png");using(var stream=File.OpenWrite(large))stream.SetLength(20*1024*1024+1);
        Assert.Throws<IOException>(()=>CoverLibrary.CopyImage(large,folder));Assert.Equal(2,Directory.GetFiles(folder).Length);
    }
    [Fact] public void LegacyManualImagesAreProtectedOnceAndExplicitReleaseSurvivesReload()
    {
        using var dir=new TestDirectory();var image=dir.File(Guid.NewGuid().ToString("N")+".png","legacy");
        var file=dir.File("settings.json",System.Text.Json.JsonSerializer.Serialize(new{Covers=new Dictionary<string,string>{{"own",image},{"online",dir.File(new string('a',64)+".png","online")}}}));
        var store=new SettingsStore(file);var settings=store.Load();Assert.Equal("own",Assert.Single(settings.ProtectedCovers));
        settings.ProtectedCovers.Clear();store.Save(settings);Assert.Empty(store.Load().ProtectedCovers);
    }
    [AvaloniaFact] public async Task MissingViewFiltersAcrossSystemsAndRemovesAcceptedGame()
    {
        using var dir=new TestDirectory();var image=Image(dir,"own.png");var settings=Settings();var games=new[]{new Game("A Missing","SNES","a.sfc"),new Game("B Deleted","NES","b.nes"),new Game("C Present","SNES","c.sfc")};
        settings.Covers[games[1].Path]=Path.Combine(dir.Path,"deleted.png");settings.Covers[games[2].Path]=image;
        new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(settings);new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert(games);
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            Click(window,"Fehlende Cover");Render(window);var list=window.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(2,list.ItemCount);
            var systems=window.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Items.Contains("Alle Konsolen"));systems.SelectedItem="SNES";Render(window);Assert.Single(list.Items);
            var task=Preview(window,games[0],image);var dialog=Assert.Single(window.OwnedWindows);Render(dialog);Click(dialog,"Dieses Cover übernehmen");Assert.True(await task);Render(window);
            Assert.Empty(list.Items);var loaded=new SettingsStore(Path.Combine(dir.Path,"settings.json")).Load();Assert.Contains(games[0].Path,loaded.ProtectedCovers);Assert.True(File.Exists(loaded.Covers[games[0].Path]));
            systems.SelectedItem="Alle Konsolen";Render(window);Assert.Single(list.Items);Assert.Equal(games[1].Path,Assert.IsType<Game>(list.Items[0]).Path);
        }
        finally{window.Close();}
    }
    [AvaloniaTheory][InlineData(false)][InlineData(true)]
    public async Task PreviewCancelAndAcceptPreserveOldFileAndOnlyPersistConfirmedImage(bool accept)
    {
        using var dir=new TestDirectory();var image=Image(dir,"new.png");var old=Image(dir,"old.png");var game=new Game("Fixture","SNES","fixture.sfc");var settings=Settings();settings.Covers[game.Path]=old;
        var store=new SettingsStore(Path.Combine(dir.Path,"settings.json"));store.Save(settings);var before=File.ReadAllBytes(Path.Combine(dir.Path,"settings.json"));
        new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert([game]);var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            var task=Preview(window,game,image);var dialog=Assert.Single(window.OwnedWindows);Render(dialog);
            using(var frame=dialog.CaptureRenderedFrame()){Assert.NotNull(frame);var output=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(output!=null){Directory.CreateDirectory(output);frame.Save(Path.Combine(output,"Cover-preview.png"));}}
            Assert.Equal(old,store.Load().Covers[game.Path]);Click(dialog,accept?"Dieses Cover übernehmen":"Abbrechen");Assert.Equal(accept,await task);
            Assert.True(File.Exists(old));Assert.True(File.Exists(image));var loaded=store.Load();Assert.Equal(accept,loaded.ProtectedCovers.Contains(game.Path));
            if(accept){Assert.NotEqual(old,loaded.Covers[game.Path]);Assert.Equal(File.ReadAllBytes(image),File.ReadAllBytes(loaded.Covers[game.Path]));}
            else{Assert.Equal(before,File.ReadAllBytes(Path.Combine(dir.Path,"settings.json")));Assert.Empty(Directory.GetFiles(Path.Combine(dir.Path,"covers")));}
        }
        finally{window.Close();}
    }
    [AvaloniaFact] public async Task ProtectedMissingImageIsNotDownloadedAndProtectionCanBeReleased()
    {
        using var dir=new TestDirectory();var game=new Game("Fixture","SNES","fixture.sfc");var settings=Settings();settings.ProtectedCovers.Add(game.Path);
        var store=new SettingsStore(Path.Combine(dir.Path,"settings.json"));store.Save(settings);new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert([game]);var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            await (Task)typeof(MainWindow).GetMethod("FetchAllCoversAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,null)!;
            Assert.False(Directory.Exists(Path.Combine(dir.Path,"cache","cover-index")));Assert.Empty(store.Load().Covers);
            window.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex=0;var expander=window.GetVisualDescendants().OfType<Expander>().Single(e=>e.Header as string=="Spieldetails bearbeiten");expander.IsExpanded=true;Render(window);
            Click(window,"Cover-Schutz aufheben");Assert.Empty(store.Load().ProtectedCovers);Click(window,"Cover schützen");Assert.Contains(game.Path,store.Load().ProtectedCovers);
        }
        finally{window.Close();}
    }
    [Fact] public void EsDeDoesNotReplaceProtectedCoverButStillImportsFavorites()
    {
        using var dir=new TestDirectory();var game=new Game("Fixture","NES",dir.File("fixture.nes"));var own=Image(dir,"own.png");var settings=Settings();settings.Covers[game.Path]=own;settings.ProtectedCovers.Add(game.Path);
        var store=new LibraryStore(Path.Combine(dir.Path,"library.db"));store.Upsert([game]);EsDeFrontend.Export(dir.Path,dir.File("app.exe"),[game],settings);
        var native=Path.Combine(EsDeFrontend.AppData(dir.Path),"downloaded_media","emuvora-nes","covers",EsDeFrontend.GameId(game)+".png");File.WriteAllText(native,"changed");
        var listFile=Path.Combine(EsDeFrontend.AppData(dir.Path),"gamelists","emuvora-nes","gamelist.xml");var list=System.Xml.Linq.XDocument.Load(listFile);list.Root!.Element("game")!.SetElementValue("favorite","true");list.Save(listFile);
        EsDeFrontend.ImportChanges(dir.Path,store,settings);Assert.True(Assert.Single(store.GetAll()).Favorite);Assert.Equal(own,settings.Covers[game.Path]);Assert.True(File.Exists(own));
        settings.ProtectedCovers.Clear();EsDeFrontend.ImportChanges(dir.Path,store,settings);Assert.Equal(native,settings.Covers[game.Path]);
    }
    [AvaloniaFact] public async Task TvControllerOpensCoverActionsAndSearchCanBeCancelledWithoutEdits()
    {
        using var dir=new TestDirectory();var game=new Game("Fixture","SNES","fixture.sfc");var settings=Settings();settings.TvMode=true;
        var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(settings);new LibraryStore(Path.Combine(dir.Path,"library.db")).Upsert([game]);
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try
        {
            window.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex=0;
            var task=window.HandleLibraryControllerAsync(0x4000);var actions=Assert.Single(window.OwnedWindows);Render(actions);Assert.Equal("Cover bearbeiten",actions.Title);
            Click(actions,"Cover schützen");await task;Assert.Contains(game.Path,saved.Load().ProtectedCovers);
            var before=File.ReadAllBytes(Path.Combine(dir.Path,"settings.json"));
            var search=(Task)typeof(MainWindow).GetMethod("FetchCoverAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,null)!;
            var dialog=Assert.Single(window.OwnedWindows);Render(dialog);dialog.GetVisualDescendants().OfType<TextBox>().Single().Text="Different cover title";Click(dialog,"Abbrechen");await search;
            Assert.Equal(before,File.ReadAllBytes(Path.Combine(dir.Path,"settings.json")));Assert.Equal("Fixture",Assert.Single(new LibraryStore(Path.Combine(dir.Path,"library.db")).GetAll()).Title);
        }
        finally{window.Close();}
    }
    [AvaloniaFact] public async Task InvalidImageLeavesNoCopiedCoverOrChangedSettings()
    {
        using var dir=new TestDirectory();var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(Settings());var before=File.ReadAllBytes(Path.Combine(dir.Path,"settings.json"));
        var window=new MainWindow(dir.Path);window.Show();Render(window);
        try{await Assert.ThrowsAsync<IOException>(()=>Preview(window,new("Fixture","SNES","fixture.sfc"),dir.File("invalid.png","not an image")));Assert.Empty(Directory.GetFiles(Path.Combine(dir.Path,"covers")));Assert.Equal(before,File.ReadAllBytes(Path.Combine(dir.Path,"settings.json")));}
        finally{window.Close();}
    }
    [Fact] public async Task BackupRestoresProtectionAndRebasesTheSelectedCover()
    {
        using var dir=new TestDirectory();var source=Path.Combine(dir.Path,"data");Directory.CreateDirectory(Path.Combine(source,"covers"));
        var image=dir.File("data/covers/own.png","synthetic cover");var settings=Settings();settings.Covers["fixture.sfc"]=image;settings.ProtectedCovers.Add("fixture.sfc");
        new SettingsStore(Path.Combine(source,"settings.json")).Save(settings);_=new LibraryStore(Path.Combine(source,"library.db"));
        var archive=Path.Combine(dir.Path,"backup.zip");await Backups.CreateAsync(source,archive);var restored=await Backups.RestoreAsync(archive,dir.Path);
        var loaded=new SettingsStore(Path.Combine(restored,"settings.json")).Load();Assert.Contains("fixture.sfc",loaded.ProtectedCovers);Assert.StartsWith(restored,loaded.Covers["fixture.sfc"]);Assert.Equal("synthetic cover",File.ReadAllText(loaded.Covers["fixture.sfc"]));
    }
}
