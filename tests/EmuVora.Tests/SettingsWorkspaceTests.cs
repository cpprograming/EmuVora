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

public class SettingsWorkspaceTests
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static void Render(Window w){w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Click(Window w,string text)=>w.GetVisualDescendants().OfType<Button>().Single(b=>b.IsEffectivelyVisible&&b.Content as string==text).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static (MainWindow Main,Window Dialog,SettingsWorkspace Workspace,Task Task) Open(TestDirectory dir,string theme="Midnight",bool tv=false,int width=1040,int height=820)
    {
        new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){Theme=theme,TvMode=tv,OnboardingCompleted=true,PreferGerman=false,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false,Packages=new(){["retroarch"]=new("fixture","D:/fixture/retroarch.exe",null)}});
        new LibraryStore(Path.Combine(dir.Path,"library.db")).Restore([new("Fixture","SNES","D:/fixture/game.sfc")]);
        var main=new MainWindow(dir.Path);main.Show();Render(main);var task=(Task)typeof(MainWindow).GetMethod("ShowSettingsAsync",Private)!.Invoke(main,[null,null])!;var dialog=Assert.Single(main.OwnedWindows);dialog.Width=width;dialog.Height=height;Render(dialog);return(main,dialog,dialog.GetVisualDescendants().OfType<SettingsWorkspace>().Single(),task);
    }
    private static void Capture(Window w,string name){var path=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(path==null)return;Directory.CreateDirectory(path);using var image=w.CaptureRenderedFrame();image!.Save(Path.Combine(path,name+".png"));}
    [AvaloniaTheory][InlineData("Midnight",false,1040,820)][InlineData("Classic Light",false,760,600)][InlineData("Studio",true,1040,900)]
    public async Task CategoriesSearchAdvancedSectionsAndFixedActionsPreserveData(string theme,bool tv,int width,int height)
    {
        using var dir=new TestDirectory();var (main,dialog,workspace,task)=Open(dir,theme,tv,width,height);var settings=File.ReadAllBytes(Path.Combine(dir.Path,"settings.json"));var db=new LibraryStore(Path.Combine(dir.Path,"library.db"));var before=db.GetAll();
        try
        {
            Assert.Equal(6,workspace.Tabs.ItemCount);foreach(var item in workspace.Tabs.Items.Cast<TabItem>()){var position=item.TranslatePoint(default,workspace.Tabs)!.Value;Assert.InRange(position.Y+item.Bounds.Height,1,workspace.Tabs.Bounds.Height+1);}
            workspace.Search.Text="retroarch-cores";Render(dialog);Assert.Equal(3,workspace.Tabs.SelectedIndex);
            var advanced=dialog.GetVisualDescendants().OfType<Expander>().Single(e=>e.Header as string=="Erweitert · RetroArch-Cores");Assert.True(advanced.IsExpanded);Assert.Contains(dialog.GetVisualDescendants().OfType<TextBox>(),t=>t.Watermark as string=="Pfad zum RetroArch-Core"&&t.IsEffectivelyVisible);
            var close=dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Schließen");Assert.DoesNotContain(close.GetVisualAncestors(),a=>a is ScrollViewer);var point=close.TranslatePoint(default,dialog)!.Value;Assert.True(point.Y+close.Bounds.Height<=dialog.ClientSize.Height+1);Capture(dialog,$"Settings-search-{theme}-{width}");
            workspace.Search.Text="no matching settings xyz";Render(dialog);Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Classes.Contains("settings-no-results")&&t.IsEffectivelyVisible);
            workspace.Search.Text="";Render(dialog);Assert.False(advanced.IsExpanded);workspace.Search.Text="monitor auflösung";Render(dialog);Assert.Equal(1,workspace.Tabs.SelectedIndex);workspace.Search.Text="";Render(dialog);
            ControllerUi.Handle(dialog,0x200);Render(dialog);Assert.Equal(2,workspace.Tabs.SelectedIndex);Assert.Equal(settings,File.ReadAllBytes(Path.Combine(dir.Path,"settings.json")));Assert.Equal(before,db.GetAll());
            Capture(dialog,$"Settings-categories-{theme}-{width}");Click(dialog,"Schließen");await task;
        }
        finally{foreach(var child in main.OwnedWindows.ToArray())child.Close();main.Close();}
    }
    [AvaloniaFact] public async Task SavedFeedbackFollowsDurableSaveAndStopsAfterClosing()
    {
        using var dir=new TestDirectory();var(main,dialog,workspace,task)=Open(dir);var stored=new SettingsStore(Path.Combine(dir.Path,"settings.json"));
        try
        {
            var feedback=dialog.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Classes.Contains("settings-save-feedback"));Assert.DoesNotContain("Gespeichert",feedback.Text);
            var cover=dialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Content as string=="Fehlende Cover beim Start und nach einem Import automatisch laden");cover.IsChecked=true;Render(dialog);Assert.True(stored.Load().AutoDownloadCovers);Assert.StartsWith("Gespeichert",feedback.Text);
            Click(dialog,"Schließen");await task;var final=feedback.Text;var mainStore=(SettingsStore)typeof(MainWindow).GetField("settingsStore",Private)!.GetValue(main)!;mainStore.Save(stored.Load());Dispatcher.UIThread.RunJobs();Assert.Equal(final,feedback.Text);
        }
        finally{main.Close();}
    }
    [AvaloniaFact] public async Task ThemeAndTvChangesUpdateTheOpenWorkspaceImmediately()
    {
        using var dir=new TestDirectory();var(main,dialog,workspace,task)=Open(dir);
        try
        {
            workspace.Tabs.SelectedIndex=2;Render(dialog);var theme=dialog.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Items.Contains("Classic Light"));theme.SelectedItem="Classic Light";Render(dialog);Assert.Equal(main.Background,dialog.Background);Assert.Equal(main.Foreground,dialog.Foreground);Assert.Equal(main.RequestedThemeVariant,dialog.RequestedThemeVariant);
            var tv=dialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Content as string=="TV-Modus · größere Cover und Bedienelemente");tv.IsChecked=true;Render(dialog);Assert.Contains("tv-mode",dialog.Classes);Assert.All(workspace.Tabs.Items.Cast<TabItem>(),t=>Assert.Equal(18,Assert.IsType<TextBlock>(t.Header).FontSize));tv.IsChecked=false;Render(dialog);Assert.All(workspace.Tabs.Items.Cast<TabItem>(),t=>Assert.Equal(14,Assert.IsType<TextBlock>(t.Header).FontSize));Click(dialog,"Schließen");await task;
        }
        finally{dialog.Close();main.Close();}
    }
    [AvaloniaFact] public async Task ClosingWarnsAboutPathDraftsAndDiscardKeepsSavedOptions()
    {
        using var dir=new TestDirectory();var(main,dialog,workspace,task)=Open(dir);var stored=new SettingsStore(Path.Combine(dir.Path,"settings.json"));
        try
        {
            dialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Content as string=="Fehlende Cover beim Start und nach einem Import automatisch laden").IsChecked=true;
            workspace.Search.Text="eigene Emulator-EXE";Render(dialog);var path=dialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Watermark as string=="Pfad zur Emulator-EXE");path.Text="D:/draft/not-yet.exe";
            ControllerUi.Handle(dialog,0x2000);Render(dialog);Assert.True(dialog.IsVisible);Assert.False(task.IsCompleted);Assert.Contains(dialog.GetVisualDescendants().OfType<Button>(),b=>b.IsEffectivelyVisible&&b.Content as string=="Pfadentwürfe verwerfen und schließen");Capture(dialog,"Settings-pending-close");
            Click(dialog,"Weiter bearbeiten");Render(dialog);Assert.True(dialog.IsVisible);Assert.Equal("D:/draft/not-yet.exe",path.Text);dialog.Close();Render(dialog);Click(dialog,"Pfadentwürfe verwerfen und schließen");await task;
            Assert.True(stored.Load().AutoDownloadCovers);Assert.Empty(stored.Load().EmulatorPaths);
        }
        finally{workspace.PendingCount=()=>0;dialog.Close();main.Close();}
    }
    [AvaloniaTheory][InlineData(".exe")][InlineData(".dll")]
    public void PathDraftsSurviveSwitchesValidateAndAcceptUppercaseExtensions(string extension)
    {
        using var dir=new TestDirectory();var stored=new Dictionary<string,string>();var saves=0;var editor=new SettingsPathEditor(stored,()=>saves++,extension,"Path");editor.Select("a");editor.Input.Text="missing"+extension;editor.Select("b");editor.Input.Text="other"+extension;editor.Select("a");Assert.Equal("missing"+extension,editor.Input.Text);Assert.Equal(2,editor.PendingCount);
        Assert.False(editor.Save());Assert.Empty(stored);Assert.Equal(0,saves);Dispatcher.UIThread.RunJobs();Assert.StartsWith("Nicht übernommen",editor.Feedback.Text);
        var valid=dir.File("chosen"+extension.ToUpperInvariant());editor.Input.Text="\""+valid+"\"";Assert.True(editor.Save());Dispatcher.UIThread.RunJobs();Assert.Equal(valid,stored["a"]);Assert.Equal(1,saves);Assert.Equal(1,editor.PendingCount);Assert.Equal("Pfad geprüft und gespeichert.",editor.Feedback.Text);editor.Select("b");Assert.Equal("other"+extension,editor.Input.Text);
    }
    [AvaloniaFact] public void FailedPathSaveRestoresMemoryAndKeepsDraftAndError()
    {
        using var dir=new TestDirectory();var original=dir.File("old.exe");var chosen=dir.File("new.exe");var stored=new Dictionary<string,string>{{"a",original}};var editor=new SettingsPathEditor(stored,()=>throw new IOException("Test write failure"),".exe","Path");editor.Select("a");editor.Input.Text=chosen;Assert.False(editor.Save());Dispatcher.UIThread.RunJobs();Assert.Equal(original,stored["a"]);Assert.Equal(chosen,editor.Input.Text);Assert.Equal(1,editor.PendingCount);Assert.Contains("Test write failure",editor.Feedback.Text);
    }
    [Fact] public void SaveEventIsRaisedOnlyAfterSuccessfulAtomicWrite()
    {
        using var dir=new TestDirectory();var file=Path.Combine(dir.Path,"settings.json");var store=new SettingsStore(file);var count=0;store.Saved+=(_,_)=>{Assert.True(File.Exists(file));count++;};store.Save(new());Assert.Equal(1,count);
        using(var locked=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Assert.ThrowsAny<IOException>(()=>store.Save(new(){Theme="Studio"}));Assert.Equal(1,count);Assert.Equal("Midnight",store.Load().Theme);Assert.Empty(Directory.GetFiles(dir.Path,"*.tmp"));
    }
}
