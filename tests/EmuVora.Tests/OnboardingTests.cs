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
public class OnboardingTests
{
    [AvaloniaFact] public async Task BiosPickerReturnsSelectedPathsAndKeepsConfirmVisible()
    {
        using var dir=new TestDirectory();var first=dir.File("first.bin","first");var second=dir.File("second.rom","second");
        var window=new Window{Width=500,Height=400};window.Show();
        var pending=new ControllerPicker(window,dir.Path).OpenFilePathsAsync("BIOS importieren");Dispatcher.UIThread.RunJobs();
        var picker=Assert.Single(window.OwnedWindows);var list=picker.GetVisualDescendants().OfType<ListBox>().Single();
        list.SelectedIndex=0;picker.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Zur Auswahl hinzufügen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        list.SelectedIndex=1;var accept=picker.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Auswahl übernehmen");
        Assert.DoesNotContain(accept.GetVisualAncestors(),a=>a is ScrollViewer);Capture(picker,"BIOS-file-picker.png");
        accept.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var paths=await pending;
        Assert.Equal(new[]{first,second},paths.OrderBy(p=>p).ToArray());window.Close();
    }
    [Fact] public void SetupPlanOnlyIncludesImportedConsolesAndDeduplicatesRetroArch()
    {
        var settings=new AppSettings();Assert.Equal(new[]{"retroarch","pcsx2"},SetupPlan.RequiredEmulators(["NES","SNES","PS2","Unknown"],settings).Select(e=>e.Id));Assert.Single(SetupPlan.RequiredEmulators([],settings));Assert.Empty(SetupPlan.RequiredEmulators(["Unknown"],settings));
    }
    [AvaloniaFact] public void WizardCanGoBackAndFinishWithoutDownloadingOrChangingExistingPaths()
    {
        using var dir=new TestDirectory();var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false,EmulatorPaths=new(){{"duckstation","C:\\Own emulator\\duckstation.exe"}}});
        var window=new MainWindow(dir.Path);window.Show();Click(window,"Einrichtungsassistent");var wizard=Assert.Single(window.OwnedWindows);Click(wizard,"Weiter");Assert.Contains(wizard.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="2 / 5 · Deine Spiele");ControllerUi.Handle(wizard,0x2000);Assert.Contains(wizard.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="1 / 5 · Dein Speicherort");
        for(var i=0;i<3;i++)Click(wizard,"Weiter");Assert.Contains(wizard.GetVisualDescendants().OfType<Button>(),b=>b.Content as string=="Eigene BIOS-Dateien importieren");Capture(wizard,"Setup-assistant.png");Click(wizard,"Weiter");Click(wizard,"Zur Bibliothek");Assert.True(saved.Load().OnboardingCompleted);Assert.Equal("C:\\Own emulator\\duckstation.exe",saved.Load().EmulatorPaths["duckstation"]);Assert.Empty(saved.Load().Packages);window.Close();
    }
    [Fact] public void FirmwareReadinessUsesTheConfiguredEmulatorDirectory()
    {
        using var dir=new TestDirectory();var exe=dir.File("PS1/duckstation.exe");var bios=Path.Combine(dir.Path,"own-bios");Directory.CreateDirectory(bios);File.WriteAllBytes(Path.Combine(bios,"scph5501.bin"),new byte[512*1024]);dir.File("PS1/settings.ini","[BIOS]\nSearchDirectory="+bios);var settings=new AppSettings{EmulatorPaths=new(){{"duckstation",exe}}};Assert.Null(SetupPlan.FirmwareHint("PS1",Path.Combine(dir.Path,"data"),settings));Assert.NotNull(SetupPlan.FirmwareHint("PS2",Path.Combine(dir.Path,"data"),settings));
    }
    [AvaloniaFact] public void FirstStartShowsWizardAndLaterChoiceIsRemembered()
    {
        using var dir=new TestDirectory();var saved=new SettingsStore(Path.Combine(dir.Path,"settings.json"));saved.Save(new(){CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false});var window=new MainWindow(dir.Path);window.Show();Dispatcher.UIThread.RunJobs();var wizard=Assert.Single(window.OwnedWindows);Click(wizard,"Später einrichten");Assert.True(saved.Load().OnboardingCompleted);window.Close();
        var again=new MainWindow(dir.Path);again.Show();Dispatcher.UIThread.RunJobs();Assert.Empty(again.OwnedWindows);again.Close();
    }
    [AvaloniaFact] public void ControllerActivatesButtonsCheckboxesTabsAndKeyboard()
    {
        var panel=new StackPanel();var button=new Button{Content="Test"};var check=new CheckBox{Content="Option"};var combo=new ComboBox{ItemsSource=new[]{"First","Second"},SelectedIndex=0};var text=new TextBox{Text="Original"};panel.Children.Add(button);panel.Children.Add(check);panel.Children.Add(combo);panel.Children.Add(text);var window=new Window{Content=panel,Width=500,Height=400};window.Show();bool clicked=false;button.Click+=(_,_)=>clicked=true;button.Focus();ControllerUi.Handle(window,0x1000);Assert.True(clicked);check.Focus();ControllerUi.Handle(window,0x1000);Assert.True(check.IsChecked);combo.Focus();ControllerUi.Handle(window,8);Assert.Equal(1,combo.SelectedIndex);
        text.Focus();ControllerUi.Handle(window,0x1000);Dispatcher.UIThread.RunJobs();var keyboard=Assert.Single(window.OwnedWindows);Click(keyboard,"A");ControllerUi.Handle(keyboard,0x2000);Assert.Equal("Original",text.Text);
        text.Focus();ControllerUi.Handle(window,0x1000);Dispatcher.UIThread.RunJobs();keyboard=Assert.Single(window.OwnedWindows);Click(keyboard,"A");Click(keyboard,"Übernehmen");Assert.Equal("Originala",text.Text);
        var tabs=new TabControl{ItemsSource=new[]{new TabItem{Header="One",Content=new Button{Content="One"}},new TabItem{Header="Two",Content=new Button{Content="Two"}}},SelectedIndex=0};window.Content=tabs;window.UpdateLayout();ControllerUi.Handle(window,0x200);Assert.Equal(1,tabs.SelectedIndex);ControllerUi.Handle(window,0x100);Assert.Equal(0,tabs.SelectedIndex);window.Close();
    }
    [AvaloniaFact] public void ControllerPickerFiltersFilesAndCanBeCancelled()
    {
        using var dir=new TestDirectory();dir.File("Cover.png");dir.File("Other.txt");Directory.CreateDirectory(Path.Combine(dir.Path,"ROMs"));var window=new Window{Width=500,Height=400};window.Show();var pending=new ControllerPicker(window,dir.Path).OpenFilePickerAsync(new(){Title="Cover",FileTypeFilter=[new("Images"){Patterns=["*.png"]}]});Dispatcher.UIThread.RunJobs();var picker=Assert.Single(window.OwnedWindows);var list=picker.GetVisualDescendants().OfType<ListBox>().Single();Assert.Equal(2,list.ItemCount);Assert.DoesNotContain(list.Items,item=>item?.ToString()?.Contains("Other.txt")==true);Capture(picker,"Xbox-file-picker.png");ControllerUi.Handle(picker,0x2000);Dispatcher.UIThread.RunJobs();Assert.True(pending.IsCompletedSuccessfully);Assert.Empty(pending.Result);window.Close();
    }
    private static void Click(Window window,string label){window.GetVisualDescendants().OfType<Button>().First(b=>b.Content as string==label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();}
    [AvaloniaFact] public async Task LibraryControllerCanCancelAnActiveOperationWithoutClosingTheLibrary()
    {
        using var dir=new TestDirectory();new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false});var window=new MainWindow(dir.Path);window.Show();
        var busy=typeof(MainWindow).GetMethod("BusyAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var pending=(Task<bool>)busy.Invoke(window,[new Func<CancellationToken,Task>(token=>Task.Delay(Timeout.Infinite,token))])!;
        await window.HandleLibraryControllerAsync(0x1000);Assert.False(pending.IsCompleted);
        await window.HandleLibraryControllerAsync(0x2000);Assert.False(await pending);Assert.True(window.IsVisible);window.Close();
    }
    private static void Capture(Window window,string name){window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);var screenshots=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(screenshots!=null){Directory.CreateDirectory(screenshots);frame.Save(Path.Combine(screenshots,name));}}
}
