using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using Xunit;

namespace EmuVora.Tests;

public class RecognitionTests
{
    private static string Sha(string value)=>Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Dat(string title="Known Game",string contents="fixture")=>$"clrmamepro (\n name \"Fixture\"\n)\ngame (\n name \"{title}\"\n rom ( name \"file.gb\" size {Encoding.UTF8.GetByteCount(contents)} crc 12345678 sha1 {Sha(contents)} serial \"TEST\" )\n)\n";
    [Fact] public void CatalogParsesTrailingSerialsAndRejectsAmbiguousHashes()
    {
        var database=RomDatabases.Parse("GB",new StringReader(Dat()));Assert.Equal("Known Game",database.Find(7,Sha("fixture")));Assert.Null(database.Find(8,Sha("fixture")));
        database=RomDatabases.Parse("GB",new StringReader(Dat()+Dat("Other title")));Assert.Null(database.Find(7,Sha("fixture")));
        Assert.Throws<InvalidDataException>(()=>RomDatabases.Parse("GB",new StringReader("game (\n name \"Incomplete\"")));
        Assert.Throws<InvalidDataException>(()=>RomDatabases.Parse("GB",new StringReader(new string('x',16385))));
    }
    [Fact] public async Task RawAndSingleRomZipAreRecognizedWithoutRenamingFiles()
    {
        using var dir=new TestDirectory();var raw=dir.File("arbitrary.gb","fixture");var zipped=Path.Combine(dir.Path,"unhelpful.zip");using(var zip=ZipFile.Open(zipped,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("folder/also-arbitrary.gb").Open());writer.Write("fixture");}
        var database=RomDatabases.Parse("GB",new StringReader(Dat()));var result=await RomRecognition.ScanAsync([new("arbitrary","GB",raw),new("unhelpful","GB",zipped)],database);
        Assert.Equal(2,result.Suggestions.Count);Assert.All(result.Suggestions,s=>Assert.Equal("Known Game",s.Title));Assert.Equal("fixture",File.ReadAllText(raw));Assert.True(File.Exists(zipped));Assert.Empty(result.Notes);
    }
    [Fact] public async Task UnknownAndMultiRomArchivesAreReportedAndOtherSystemsAreIgnored()
    {
        using var dir=new TestDirectory();var unknown=dir.File("unknown.gb","unknown");var multi=Path.Combine(dir.Path,"multi.zip");using(var zip=ZipFile.Open(multi,ZipArchiveMode.Create))foreach(var name in new[]{"one.gb","two.gb"}){using var writer=new StreamWriter(zip.CreateEntry(name).Open());writer.Write("fixture");}
        var database=RomDatabases.Parse("GB",new StringReader(Dat()));var result=await RomRecognition.ScanAsync([new("unknown","GB",unknown),new("multi","GB",multi),new("other","GBA",unknown)],database);Assert.Empty(result.Suggestions);Assert.Equal(2,result.Notes.Count);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>RomRecognition.ScanAsync([new("unknown","GB",unknown)],database,token:new CancellationToken(true)));
    }
    [Fact] public async Task OversizedRomsAreSkippedBeforeReadingTheirContents()
    {
        using var dir=new TestDirectory();var file=dir.File("large.gb");using(var output=File.OpenWrite(file))output.SetLength(64L*1024*1024+1);
        var result=await RomRecognition.ScanAsync([new("large","GB",file)],RomDatabases.Parse("GB",new StringReader(Dat())));Assert.Empty(result.Suggestions);Assert.Single(result.Notes);
    }
    [Fact] public void ApplyingSuggestionsProtectsCustomEditsAndPreservesPlayHistory()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(dir.File("library.db"));var first=new Game("raw","GB",dir.File("raw.gb","fixture"));var custom=new Game("custom","GB",dir.File("custom.gb","fixture"));store.Upsert([first,custom]);store.MarkPlayed(first);store.Update(custom with {Title="My own title",Favorite=true});
        Assert.Single(store.RecognitionCandidates("GB"));var changes=store.ApplyRecognition([new(first.Path,"GB","raw","Known Game"),new(custom.Path,"GB","custom","Wrong overwrite")]);Assert.Equal(1,changes);
        var games=store.GetAll();var recognized=games.Single(g=>g.Path==first.Path);Assert.Equal(1,recognized.PlayCount);Assert.NotNull(recognized.LastPlayed);Assert.Equal("Known Game",recognized.Title);Assert.True(games.Single(g=>g.Path==custom.Path).Favorite);
        store.Update(recognized with{Title="Later own title"});Assert.Equal(0,store.ApplyRecognition([new(first.Path,"GB","Known Game","Stale title")]));Assert.Equal("Later own title",store.GetAll().Single(g=>g.Path==first.Path).Title);
    }
    [Fact] public void InvalidSuggestionRollsBackTheEntireTitleTransaction()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(dir.File("library.db"));var game=new Game("old","GB",dir.File("file.gb"));store.Upsert([game]);
        Assert.Throws<InvalidDataException>(()=>store.ApplyRecognition([new(game.Path,"GB","old","valid"),new(game.Path,"GB","valid","bad\nname")]));Assert.Equal("old",Assert.Single(store.GetAll()).Title);
    }
    [Fact] public async Task ChangedRomFilesAreSkippedAtConfirmation()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(dir.File("library.db"));var file=dir.File("own.gb","fixture");var game=new Game("old","GB",file);store.Upsert([game]);var result=await RomRecognition.ScanAsync([game],RomDatabases.Parse("GB",new StringReader(Dat())));Assert.Single(result.Suggestions);File.WriteAllText(file,"different contents");Assert.Equal(0,store.ApplyRecognition(result.Suggestions));Assert.Equal("old",Assert.Single(store.GetAll()).Title);
    }
    private sealed class ResponseHandler(byte[] bytes):HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Requests++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){RequestMessage=request,Content=new ByteArrayContent(bytes)});}
    }
    [Fact] public async Task CatalogCacheIsVerifiedAndUsableOffline()
    {
        using var dir=new TestDirectory();var bytes=Encoding.UTF8.GetBytes(Dat());var hash=Convert.ToHexString(SHA256.HashData(bytes));var source=new RomDatabaseSource("GB","metadat/no-intro/fixture.dat",new string('a',40),bytes.Length,hash);var handler=new ResponseHandler(bytes);using var http=new HttpClient(handler);
        var first=await RomDatabases.LoadAsync(source,http,dir.Path);Assert.Equal("Known Game",first.Find(7,Sha("fixture")));Assert.Equal(1,handler.Requests);
        await RomDatabases.LoadAsync(source,http,dir.Path);Assert.Equal(1,handler.Requests);
        File.WriteAllText(Path.Combine(dir.Path,hash.ToLowerInvariant()+".dat"),"corrupt");await RomDatabases.LoadAsync(source,http,dir.Path);Assert.Equal(2,handler.Requests);
        Assert.DoesNotContain(Directory.GetFiles(dir.Path),f=>f.EndsWith(".part"));
    }
    [Fact] public async Task WrongDownloadHashNeverPublishesACatalog()
    {
        using var dir=new TestDirectory();var bytes=Encoding.UTF8.GetBytes(Dat());var source=new RomDatabaseSource("GB","metadat/no-intro/fixture.dat",new string('a',40),bytes.Length,new string('0',64));using var http=new HttpClient(new ResponseHandler(bytes));
        await Assert.ThrowsAsync<IOException>(()=>RomDatabases.LoadAsync(source,http,dir.Path));Assert.Empty(Directory.GetFiles(dir.Path));
    }
    [Fact][Trait("Category","Network")] public async Task OfficialPinnedCatalogDownloadsAndParses()
    {
        using var dir=new TestDirectory();using var http=new HttpClient{Timeout=TimeSpan.FromMinutes(2)};var database=await RomDatabases.LoadAsync(RomDatabases.Sources.Single(s=>s.System=="Atari 5200"),http,dir.Path);Assert.True(database.Count>=100);
    }
    [AvaloniaFact] public async Task CancellingPreviewKeepsTheLibraryUntouched()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(dir.File("library.db"));var game=new Game("old title","GB",dir.File("own.gb","fixture"));store.Upsert([game]);new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){OnboardingCompleted=true,AutoDownloadCovers=false,CheckUpdatesOnStartup=false,CheckAppUpdatesOnStartup=false});var window=new MainWindow(dir.Path);window.Show();
        var method=typeof(MainWindow).GetMethod("PreviewRecognitionAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;var pending=(Task)method.Invoke(window,[new RecognitionResult([new(game.Path,"GB","old title","Known Game")],[]),"fixture report",new TextBlock(),window])!;Dispatcher.UIThread.RunJobs();var dialog=Assert.Single(window.OwnedWindows);
        dialog.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();using(var frame=dialog.CaptureRenderedFrame()){Assert.NotNull(frame);var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);frame.Save(Path.Combine(folder,"ROM-recognition-preview.png"));}}
        dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Abbrechen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await pending;Assert.Equal("old title",Assert.Single(store.GetAll()).Title);window.Close();
    }
    [AvaloniaFact] public async Task PreviewOnlyAppliesTitlesThatWereNotEditedInTheMeantime()
    {
        using var dir=new TestDirectory();var store=new LibraryStore(dir.File("library.db"));var one=new Game("one","GB",dir.File("one.gb"));var two=new Game("two","GB",dir.File("two.gb"));store.Upsert([one,two]);new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){OnboardingCompleted=true,AutoDownloadCovers=false,CheckUpdatesOnStartup=false,CheckAppUpdatesOnStartup=false});var window=new MainWindow(dir.Path);window.Show();
        var result=new RecognitionResult([new(one.Path,"GB","one","Known one"),new(two.Path,"GB","two","Known two")],[]);var method=typeof(MainWindow).GetMethod("PreviewRecognitionAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;var pending=(Task)method.Invoke(window,[result,"report",new TextBlock(),window])!;Dispatcher.UIThread.RunJobs();store.Update(two with{Title="My own title"});var dialog=Assert.Single(window.OwnedWindows);dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Alle eindeutigen Vorschläge übernehmen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await pending;Assert.Equal("Known one",store.GetAll().Single(g=>g.Path==one.Path).Title);Assert.Equal("My own title",store.GetAll().Single(g=>g.Path==two.Path).Title);window.Close();
    }
}
