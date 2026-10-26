using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using EmuVora.Core;
using System.Reflection;
using Xunit;

namespace EmuVora.Tests;

public class CarouselTests
{
    private static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    private static T Field<T>(MainWindow w,string name)=>(T)typeof(MainWindow).GetField(name,Private)!.GetValue(w)!;
    private static void Render(Window w){w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
    private static void Click(Window w,string label)=>w.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string==label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Screenshot(Window w,string name){var path=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(path==null)return;Directory.CreateDirectory(path);using var image=w.CaptureRenderedFrame();image!.Save(Path.Combine(path,name+".png"));}
    private static MainWindow Open(TestDirectory dir,string theme,int width,bool tv,bool empty=false)
    {
        new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){LibraryLayout="Carousel",Theme=theme,TvMode=tv,OnboardingCompleted=true,PreferGerman=false,AutoDownloadCovers=false,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false});
        if(!empty)new LibraryStore(Path.Combine(dir.Path,"library.db")).Restore(Enumerable.Range(0,125).Select(i=>new Game($"Game {i:0000}",i<100?"SNES":"GBA",$"D:/fixture/{i}.rom",i%2==0)));
        var window=new MainWindow(dir.Path){Width=width,Height=tv?900:820};window.Show();Render(window);return window;
    }
    [AvaloniaTheory][InlineData("Midnight",1320,false)][InlineData("Classic Light",860,false)][InlineData("Studio",1400,true)]
    public async Task CarouselCentersSelectionKeepsPagesAndUsesHorizontalNavigation(string theme,int width,bool tv)
    {
        using var dir=new TestDirectory();var window=Open(dir,theme,width,tv);var store=new LibraryStore(Path.Combine(dir.Path,"library.db"));var before=store.GetAll();
        try
        {
            var list=Field<ListBox>(window,"gamesList");var choice=Field<ComboBox>(window,"libraryLayoutChoice");Assert.Equal(2,choice.SelectedIndex);Assert.Equal(48,list.ItemCount);Assert.NotNull(list.SelectedItem);
            list.SelectedIndex=12;Render(window);var item=list.GetVisualDescendants().OfType<ListBoxItem>().Single(i=>i.IsSelected);var scroll=list.GetVisualDescendants().OfType<ScrollViewer>().First();
            var center=item.TranslatePoint(new(item.Bounds.Width/2,0),scroll)!.Value;Assert.InRange(Math.Abs(center.X-scroll.Viewport.Width/2),0,5);
            Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(),1,48);Screenshot(window,$"Carousel-{theme}-{width}");
            var resizeSelection=list.SelectedItem;window.Height-=100;Render(window);Assert.Equal(resizeSelection,list.SelectedItem);
            var cards=list.GetVisualDescendants().OfType<ListBoxItem>().SelectMany(i=>i.GetVisualDescendants().OfType<StackPanel>().Where(p=>p.Width>0&&!double.IsNaN(p.Width))).ToArray();Assert.NotEmpty(cards);Assert.Single(cards.Select(c=>c.Width).Distinct());window.Height+=100;Render(window);
            item=list.GetVisualDescendants().OfType<ListBoxItem>().Single(i=>i.IsSelected);
            item.Focus();window.KeyPressQwerty(PhysicalKey.ArrowRight,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.ArrowRight,RawInputModifiers.None);Render(window);Assert.Equal(13,list.SelectedIndex);
            list.SelectedIndex=47;await window.HandleLibraryControllerAsync(8);Render(window);Assert.Equal("Game 0048",Assert.IsType<Game>(list.SelectedItem).Title);
            await window.HandleLibraryControllerAsync(4);Render(window);Assert.Equal("Game 0047",Assert.IsType<Game>(list.SelectedItem).Title);
            var selected=list.SelectedItem;var page=Field<TextBlock>(window,"pageLabel").Text;choice.SelectedIndex=1;Render(window);choice.SelectedIndex=2;Render(window);Assert.Equal(selected,list.SelectedItem);Assert.Equal(page,Field<TextBlock>(window,"pageLabel").Text);
            Assert.Equal("Carousel",new SettingsStore(Path.Combine(dir.Path,"settings.json")).Load().LibraryLayout);Assert.Equal(before,store.GetAll());
        }
        finally{window.Close();}
    }
    [AvaloniaTheory][InlineData("Midnight",1000,false)][InlineData("Classic Light",640,false)][InlineData("Studio",1100,true)]
    public async Task ConsolePickerSearchesCancelsAndOpensSystemWithoutChangingLayout(string theme,int width,bool tv)
    {
        using var dir=new TestDirectory();var window=Open(dir,theme,1320,tv);var store=new LibraryStore(Path.Combine(dir.Path,"library.db"));var before=store.GetAll();
        Task Show()=>(Task)typeof(MainWindow).GetMethod("ShowConsolePickerAsync",Private)!.Invoke(window,null)!;
        try
        {
            var task=Show();var dialog=Assert.Single(window.OwnedWindows);dialog.Width=width;Render(dialog);
            var list=dialog.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(2,list.ItemCount);list.Focus();ControllerUi.Handle(dialog,2);ControllerUi.Handle(dialog,0x2000);await task;Assert.Equal("Alle Konsolen",Field<ComboBox>(window,"systems").SelectedItem);
            task=Show();dialog=Assert.Single(window.OwnedWindows);dialog.Width=width;Render(dialog);
            var query=dialog.GetVisualDescendants().OfType<TextBox>().Single();query.Text="No such console";Render(dialog);Assert.Empty(dialog.GetVisualDescendants().OfType<ListBox>().Single().Items);Assert.False(dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Spiele dieser Konsole anzeigen").IsEnabled);
            query.Text="GBA";Render(dialog);Assert.Single(dialog.GetVisualDescendants().OfType<ListBox>().Single().Items);Screenshot(dialog,$"Console-picker-{theme}-{width}");
            var accept=dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Spiele dieser Konsole anzeigen");Assert.DoesNotContain(accept.GetVisualAncestors(),a=>a is ScrollViewer);var point=accept.TranslatePoint(new(0,0),dialog)!.Value;Assert.True(point.Y+accept.Bounds.Height<=dialog.ClientSize.Height);
            Click(dialog,"Spiele dieser Konsole anzeigen");await task;Render(window);Assert.Equal("GBA",Field<ComboBox>(window,"systems").SelectedItem);Assert.All(Field<ListBox>(window,"gamesList").Items.Cast<Game>(),g=>Assert.Equal("GBA",g.System));Assert.Equal(2,Field<ComboBox>(window,"libraryLayoutChoice").SelectedIndex);Assert.Equal(before,store.GetAll());
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }
    [AvaloniaFact]public async Task EmptyConsolePickerCannotApplyAnInventedSystem()
    {
        using var dir=new TestDirectory();var window=Open(dir,"Midnight",1320,false,true);
        try{var task=(Task)typeof(MainWindow).GetMethod("ShowConsolePickerAsync",Private)!.Invoke(window,null)!;var dialog=Assert.Single(window.OwnedWindows);Render(dialog);Assert.Empty(dialog.GetVisualDescendants().OfType<ListBox>().Single().Items);Assert.False(dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Spiele dieser Konsole anzeigen").IsEnabled);ControllerUi.Handle(dialog,0x2000);await task;Assert.Empty(new LibraryStore(Path.Combine(dir.Path,"library.db")).GetAll());}
        finally{window.Close();}
    }
}
