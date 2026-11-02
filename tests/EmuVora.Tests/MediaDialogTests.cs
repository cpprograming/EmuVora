using System.Reflection;
using Avalonia;
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

public class MediaDialogTests
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static void Render(Window w){w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static Button FindButton(Window w,string name)=>w.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string==name);
    private static void Click(Window w,string name)=>FindButton(w,name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static Task Invoke(MainWindow w,string method)=>(Task)typeof(MainWindow).GetMethod(method,Private)!.Invoke(w,null)!;
    private static AppSettings Live(MainWindow w)=>(AppSettings)typeof(MainWindow).GetField("settings",Private)!.GetValue(w)!;
    private static void Fixed(Window w,params string[] names)
    {
        foreach(var name in names){var button=FindButton(w,name);Assert.DoesNotContain(button.GetVisualAncestors(),a=>a is ScrollViewer);var point=button.TranslatePoint(default,w)!.Value;Assert.InRange(point.Y,0,w.ClientSize.Height);Assert.True(point.Y+button.Bounds.Height<=w.ClientSize.Height+1);Assert.True(button.Bounds.Height>0);}
    }
    private static void Capture(Window w,string name){var path=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(path==null)return;Directory.CreateDirectory(path);using var image=w.CaptureRenderedFrame();image!.Save(Path.Combine(path,name+".png"));}
    private static string Image(TestDirectory dir,string name)
    {
        var file=Path.Combine(dir.Path,name);using var surface=SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(100,140));surface.Canvas.Clear(SkiaSharp.SKColors.Teal);using var shot=surface.Snapshot();using var bytes=shot.Encode(SkiaSharp.SKEncodedImageFormat.Png,100);File.WriteAllBytes(file,bytes.ToArray());return file;
    }
    private static (MainWindow Main,Game Game) Open(TestDirectory dir,string theme="Midnight",bool tv=false)
    {
        var game=new Game("Adventure "+new string('W',180),"SNES","D:/fixture/"+new string('p',150)+"/game.sfc");var settings=new AppSettings{Theme=theme,TvMode=tv,OnboardingCompleted=true,PreferGerman=false,AutomaticSetup=false,AutoDownloadCovers=false,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false};
        for(var i=0;i<80;i++)settings.Collections[$"Collection {i:00} "+new string('N',40)]=new(StringComparer.OrdinalIgnoreCase);
        var alternate=game with{Path="D:/fixture/zz-alternate.sfc"};settings.Covers[game.Path]=settings.Covers[alternate.Path]=Image(dir,"old.png");new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(settings);new LibraryStore(Path.Combine(dir.Path,"library.db")).Restore([game,alternate]);var main=new MainWindow(dir.Path);main.Show();Render(main);var list=main.GetVisualDescendants().OfType<ListBox>().Single();list.SelectedIndex=0;Render(main);return(main,Assert.IsType<Game>(list.SelectedItem));
    }
    [AvaloniaTheory][InlineData("Midnight",false,760,600)][InlineData("Classic Light",false,760,600)][InlineData("Studio",true,900,760)]
    public async Task MembershipActionsStayVisibleWithManyCollectionsAndCancelKeepsBytes(string theme,bool tv,int width,int height)
    {
        using var dir=new TestDirectory();var(main,game)=Open(dir,theme,tv);var before=File.ReadAllBytes(Path.Combine(dir.Path,"settings.json"));
        try
        {
            var task=Invoke(main,"ChooseCollectionsAsync");var dialog=Assert.Single(main.OwnedWindows);dialog.Width=width;dialog.Height=height;Render(dialog);Fixed(dialog,"Zuordnung speichern","Abbrechen");var last=dialog.GetVisualDescendants().OfType<CheckBox>().Last();last.BringIntoView();Render(dialog);last.IsChecked=true;Fixed(dialog,"Zuordnung speichern","Abbrechen");Capture(dialog,$"Media-membership-{theme}-{tv}");ControllerUi.Handle(dialog,0x2000);await task;Assert.Equal(before,File.ReadAllBytes(Path.Combine(dir.Path,"settings.json")));
        }
        finally{main.Close();}
    }
    [AvaloniaFact] public async Task MembershipWriteFailureKeepsChoicesAndCanRetry()
    {
        using var dir=new TestDirectory();var(main,game)=Open(dir);var file=Path.Combine(dir.Path,"settings.json");
        try
        {
            var task=Invoke(main,"ChooseCollectionsAsync");var dialog=Assert.Single(main.OwnedWindows);Render(dialog);var choice=dialog.GetVisualDescendants().OfType<CheckBox>().First();choice.IsChecked=true;
            using(var locked=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Click(dialog,"Zuordnung speichern");Render(dialog);Assert.True(dialog.IsVisible);Assert.False(task.IsCompleted);Assert.True(choice.IsChecked);Assert.All(Live(main).Collections.Values,c=>Assert.Empty(c));Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.StartsWith("Nicht gespeichert:")==true);Fixed(dialog,"Zuordnung speichern","Abbrechen");Click(dialog,"Zuordnung speichern");await task;Assert.Contains(game.Path,new SettingsStore(file).Load().Collections[(string)choice.Content!]);
        }
        finally{main.Close();}
    }
    [AvaloniaTheory][InlineData(false)][InlineData(true)]
    public async Task CoverWriteFailureKeepsPreviewAndOldStateUntilRetryOrCancel(bool retry)
    {
        using var dir=new TestDirectory();var(main,game)=Open(dir,retry?"Studio":"Classic Light",retry);var file=Path.Combine(dir.Path,"settings.json");var before=File.ReadAllBytes(file);var old=Live(main).Covers[game.Path];var source=Image(dir,"new.png");
        try
        {
            var task=(Task<bool>)typeof(MainWindow).GetMethod("PreviewCoverAsync",Private)!.Invoke(main,[game,source,"Origin "+new string('O',1200)])!;var dialog=Assert.Single(main.OwnedWindows);dialog.Width=retry?900:760;dialog.Height=retry?760:600;Render(dialog);Fixed(dialog,"Dieses Cover übernehmen","Abbrechen");
            using(var locked=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Click(dialog,"Dieses Cover übernehmen");Render(dialog);Assert.True(dialog.IsVisible);Assert.False(task.IsCompleted);Assert.Equal(old,Live(main).Covers[game.Path]);Assert.DoesNotContain(game.Path,Live(main).ProtectedCovers);Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.StartsWith("Nicht übernommen:")==true);Fixed(dialog,"Dieses Cover übernehmen","Abbrechen");Capture(dialog,$"Media-cover-failure-{retry}");Click(dialog,retry?"Dieses Cover übernehmen":"Abbrechen");Assert.Equal(retry,await task);Assert.True(File.Exists(old));Assert.True(File.Exists(source));
            if(retry){Assert.NotEqual(old,new SettingsStore(file).Load().Covers[game.Path]);Assert.Contains(game.Path,new SettingsStore(file).Load().ProtectedCovers);}else{Assert.Equal(before,File.ReadAllBytes(file));Assert.Empty(Directory.GetFiles(Path.Combine(dir.Path,"covers")));}
        }
        finally{main.Close();}
    }
    [AvaloniaTheory][InlineData("accept")][InlineData("cancel")][InlineData("back")]
    public async Task DownloadOperationCanFinishAfterCoverPreviewDecision(string action)
    {
        using var dir=new TestDirectory();var(main,game)=Open(dir);var source=Image(dir,"downloaded.png");var before=File.ReadAllBytes(Path.Combine(dir.Path,"settings.json"));bool? accepted=null;
        try
        {
            Func<CancellationToken,Task> decision=async _=>{accepted=await (Task<bool>)typeof(MainWindow).GetMethod("PreviewCoverAsync",Private)!.Invoke(main,[game,source,"Downloaded image fixture"])!;};
            var outer=(Task<bool>)typeof(MainWindow).GetMethod("BusyAsync",Private)!.Invoke(main,[decision])!;var dialog=Assert.Single(main.OwnedWindows);dialog.Height=600;Render(dialog);Fixed(dialog,"Dieses Cover übernehmen","Abbrechen");
            if(action=="back")ControllerUi.Handle(dialog,0x2000);else Click(dialog,action=="accept"?"Dieses Cover übernehmen":"Abbrechen");Render(main);Assert.True(await outer.WaitAsync(TimeSpan.FromSeconds(3)));Assert.Equal(action=="accept",accepted);Assert.Empty(main.OwnedWindows);Assert.Null(typeof(MainWindow).GetField("operation",Private)!.GetValue(main));
            if(action!="accept"){Assert.Equal(before,File.ReadAllBytes(Path.Combine(dir.Path,"settings.json")));Assert.Empty(Directory.GetFiles(Path.Combine(dir.Path,"covers")));}Assert.True(File.Exists(source));
        }
        finally{main.Close();}
    }
    [AvaloniaFact] public async Task CoverProtectionFailureStaysOpenAndDoesNotChangeMemory()
    {
        using var dir=new TestDirectory();var(main,game)=Open(dir);var file=Path.Combine(dir.Path,"settings.json");
        try{var task=Invoke(main,"ManageCoverAsync");var dialog=Assert.Single(main.OwnedWindows);dialog.Height=600;Render(dialog);Fixed(dialog,"Eigenes Bild auswählen","Cover online suchen","Cover schützen","Abbrechen");using(var locked=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Click(dialog,"Cover schützen");Render(dialog);Assert.True(dialog.IsVisible);Assert.DoesNotContain(game.Path,Live(main).ProtectedCovers);Click(dialog,"Cover schützen");await task;Assert.Contains(game.Path,new SettingsStore(file).Load().ProtectedCovers);}
        finally{main.Close();}
    }
    [AvaloniaFact] public async Task VersionAndSearchDialogsKeepActionsReachableAndBlankSearchStaysLocal()
    {
        using var dir=new TestDirectory();var(main,game)=Open(dir);var file=Path.Combine(dir.Path,"settings.json");var before=File.ReadAllBytes(file);
        try
        {
            var task=(Task<Game?>)typeof(MainWindow).GetMethod("ChooseVersionAsync",Private)!.Invoke(main,[game])!;var dialog=Assert.Single(main.OwnedWindows);dialog.Height=600;Render(dialog);Fixed(dialog,"Diese Version auswählen","Abbrechen");Click(dialog,"Abbrechen");Assert.Null(await task);
            var search=Invoke(main,"FetchCoverAsync");dialog=Assert.Single(main.OwnedWindows);dialog.Height=600;Render(dialog);Fixed(dialog,"Treffer suchen","Abbrechen");var title=dialog.GetVisualDescendants().OfType<TextBox>().Single();title.Text=" ";Click(dialog,"Treffer suchen");Render(dialog);Assert.True(dialog.IsVisible);Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="Bitte einen Titel für die Cover-Suche eingeben.");Assert.False(Directory.Exists(Path.Combine(dir.Path,"cache","cover-index")));ControllerUi.Handle(dialog,0x2000);await search;Assert.Equal(before,File.ReadAllBytes(file));
        }
        finally{main.Close();}
    }
    [AvaloniaFact] public async Task CollectionValidationAndActionsRemainVisibleInCompactWindow()
    {
        using var dir=new TestDirectory();var(main,game)=Open(dir,"Classic Light");var file=Path.Combine(dir.Path,"settings.json");var before=File.ReadAllBytes(file);
        try{var task=Invoke(main,"ManageCollectionsAsync");var dialog=Assert.Single(main.OwnedWindows);dialog.Height=600;Render(dialog);Fixed(dialog,"Neue Sammlung erstellen","Sammlung umbenennen","Sammlung löschen · Spiele behalten","Fertig");dialog.GetVisualDescendants().OfType<TextBox>().Single().Text=" ";Click(dialog,"Neue Sammlung erstellen");Render(dialog);Fixed(dialog,"Fertig");Assert.Equal(before,File.ReadAllBytes(file));Capture(dialog,"Media-collection-validation");Click(dialog,"Fertig");await task;}
        finally{main.Close();}
    }
}
