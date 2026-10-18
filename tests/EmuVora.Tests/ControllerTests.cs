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

public class ControllerTests
{
    private static Gamepad.Snapshot Pad(int index,ushort buttons=0)=>new(index,buttons,0,0);
    [Fact] public void ConnectingHeldButtonsRequiresReleaseBeforeActivation()
    {
        var navigation=new GamepadNavigation();Assert.Equal(0,navigation.Poll([Pad(0,0x1000)],true,0));Assert.Equal(0,navigation.Poll([Pad(0,0x1000)],true,1000));
        Assert.Equal(0,navigation.Poll([Pad(0)],true,1100));Assert.Equal(0x1000,navigation.Poll([Pad(0,0x1000)],true,1200));Assert.Equal(0,navigation.Poll([Pad(0,0x1000)],true,1300));
    }
    [Fact] public void ReconnectionDoesNotLaunchFromAHeldButton()
    {
        var navigation=new GamepadNavigation();navigation.Poll([Pad(2)],true,0);Assert.Equal(0x1000,navigation.Poll([Pad(2,0x1000)],true,100));
        navigation.Poll([],true,200);Assert.Equal(-1,navigation.ActiveIndex);Assert.Equal(0,navigation.Poll([Pad(2,0x1000)],true,300));navigation.Poll([Pad(2)],true,400);Assert.Equal(0x1000,navigation.Poll([Pad(2,0x1000)],true,500));
    }
    [Fact] public void FocusReturnWaitsForNeutralAndDoesNotRepeatHeldDirections()
    {
        var navigation=new GamepadNavigation();navigation.Poll([Pad(0)],true,0);Assert.Equal(2,navigation.Poll([Pad(0,2)],true,100));navigation.Poll([Pad(0,2)],false,200);
        Assert.Equal(0,navigation.Poll([Pad(0,2)],true,1000));Assert.Equal(0,navigation.Poll([Pad(0,2)],true,2000));navigation.Poll([Pad(0)],true,2100);Assert.Equal(2,navigation.Poll([Pad(0,2)],true,2200));
    }
    [Fact] public void DirectionRepeatsAfterDelayButActionButtonsNeverRepeat()
    {
        var navigation=new GamepadNavigation();navigation.Poll([Pad(0)],true,0);Assert.Equal(0x1002,navigation.Poll([Pad(0,0x1002)],true,100));Assert.Equal(0,navigation.Poll([Pad(0,0x1002)],true,499));Assert.Equal(2,navigation.Poll([Pad(0,0x1002)],true,500));Assert.Equal(0,navigation.Poll([Pad(0,0x1002)],true,639));Assert.Equal(2,navigation.Poll([Pad(0,0x1002)],true,640));
    }
    [Fact] public void FourControllersCanTakeOverOnlyWithAFreshPressWhenOwnerIsIdle()
    {
        var navigation=new GamepadNavigation();navigation.Poll([Pad(0),Pad(1),Pad(2),Pad(3)],true,0);
        Assert.Equal(0x1000,navigation.Poll([Pad(0),Pad(1),Pad(2),Pad(3,0x1000)],true,100));Assert.Equal(3,navigation.ActiveIndex);
        Assert.Equal(0,navigation.Poll([Pad(0,0x2000),Pad(1),Pad(2),Pad(3,0x1000)],true,200));Assert.Equal(3,navigation.ActiveIndex);
        Assert.Equal(0,navigation.Poll([Pad(0,0x2000),Pad(1),Pad(2),Pad(3)],true,300));
        navigation.Poll([Pad(0),Pad(1),Pad(2),Pad(3)],true,400);Assert.Equal(0x2000,navigation.Poll([Pad(0,0x2000),Pad(1),Pad(2),Pad(3)],true,500));Assert.Equal(0,navigation.ActiveIndex);
    }
    [Fact] public void StickNoiseAndTriggersDoNotNavigateTheInterface()
    {
        var navigation=new GamepadNavigation();Assert.Equal(0,navigation.Poll([new(1,0,8000,-8000,255,255,32767,-32768)],true,0));
        Assert.Equal(0,navigation.Poll([new(1,0,8000,-8000,255,255,32767,-32768)],true,100));Assert.Equal(8,navigation.Poll([new(1,0,20000,0)],true,200));
    }
    [Fact] public void UnarmedHeldControllerDoesNotBlockAnotherController()
    {
        var navigation=new GamepadNavigation();navigation.Poll([Pad(0,0x1000),Pad(1)],true,0);Assert.Equal(0x1000,navigation.Poll([Pad(0,0x1000),Pad(1,0x1000)],true,100));Assert.Equal(1,navigation.ActiveIndex);
    }
    [AvaloniaFact] public void DiagnosticsRequiresAHeldExitChordOnTheSameController()
    {
        var active=true;var window=new ControllerDiagnosticsWindow(()=>[],()=>active);window.Show();
        window.UpdateSnapshots([Pad(0,0x30)],0);window.UpdateSnapshots([Pad(0,0x30)],999);Assert.True(window.IsVisible);
        window.UpdateSnapshots([Pad(1,0x30)],1000);window.UpdateSnapshots([Pad(1,0x30)],1999);Assert.True(window.IsVisible);
        active=false;window.UpdateSnapshots([Pad(1,0x30)],2000);active=true;window.UpdateSnapshots([Pad(1,0x30)],3000);Assert.True(window.IsVisible);
        window.UpdateSnapshots([Pad(1,0x30)],4000);Assert.False(window.IsVisible);
    }
    [AvaloniaFact] public void DiagnosticsShowsRawInputAndConnectionChangesWithoutActivatingButtons()
    {
        var owner=new Window();owner.Show();var window=new ControllerDiagnosticsWindow(()=>[]);window.Show(owner);
        window.UpdateSnapshots([new(2,0x3000,123,-456,17,255,789,-123)],0);
        var text=string.Join("\n",window.GetVisualDescendants().OfType<TextBlock>().Select(t=>t.Text));Assert.Contains("Slot 3 · verbunden",text);Assert.Contains("A · B",text);Assert.Contains("LT 17/255",text);Assert.Contains("X 789",text);Assert.True(window.IsVisible);
        window.UpdateSnapshots([],100);window.UpdateSnapshots([Pad(2)],200);text=string.Join("\n",window.GetVisualDescendants().OfType<TextBlock>().Select(t=>t.Text));Assert.Contains("Slot 3: getrennt",text);Assert.Contains("Slot 3: verbunden",text);
        window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using(var frame=window.CaptureRenderedFrame()){Assert.NotNull(frame);var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);frame.Save(System.IO.Path.Combine(folder,"Xbox-diagnostics.png"));}}
        window.Close();owner.Close();
    }
    [AvaloniaFact] public void DiagnosticsIsAvailableInEmulatorSettings()
    {
        using var dir=new TestDirectory();new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json")).Save(new(){OnboardingCompleted=true,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false,AutoDownloadCovers=false});
        var window=new MainWindow(dir.Path);window.Show();window.GetVisualDescendants().OfType<Button>().First(b=>b.Content as string=="Einstellungen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();var settings=Assert.Single(window.OwnedWindows);settings.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex=3;Dispatcher.UIThread.RunJobs();
        settings.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Xbox-Controller prüfen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();var diagnostics=Assert.Single(settings.OwnedWindows);Assert.IsType<ControllerDiagnosticsWindow>(diagnostics);diagnostics.Close();settings.Close();window.Close();
    }
}
