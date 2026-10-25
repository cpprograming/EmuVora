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
public class DiagnosticTests
{
    [Fact] public void ReportUsesOnlyAllowlistedAggregateInformationAndPreservesInputs()
    {
        using var dir=new TestDirectory();const string secret="PRIVATE_SENTINEL_987654";var rom=dir.File(secret+"/game.gba");var exe=dir.File(secret+"/program.exe");var cover=dir.File(secret+"/cover.png");
        var settings=new AppSettings{Theme=secret,DisplayMode=secret,EmulationStationTheme=secret};settings.EmulatorPaths["retroarch"]=exe;settings.Packages["retroarch"]=new(secret,exe);settings.Covers[rom]=cover;var games=new Game[]{new(secret,"GBA",rom),new(secret,secret,Path.Combine(dir.Path,secret,"missing.gba"))};
        var files=Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories).ToDictionary(f=>f,File.ReadAllBytes);var report=Diagnostics.Create(dir.Path,settings,games,secret,new(secret,2560,1440,true),true);
        Assert.DoesNotContain(secret,report);Assert.DoesNotContain(dir.Path,report);Assert.Contains("Dateieinträge: 2",report);Assert.Contains("Nicht erreichbare Spieldateien: 1",report);Assert.Contains("Cover vorhanden: 1",report);Assert.Contains("Core fehlt",report);Assert.Contains("Nicht zugeordnetes System",report);Assert.Contains("Bildmodus: unbekannt",report);Assert.Contains("Version: unbekannt",report);Assert.Contains("2560 × 1440",report);
        foreach(var(file,bytes)in files)Assert.Equal(bytes,File.ReadAllBytes(file));Assert.Equal(files.Count,Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories).Length);Assert.Equal(secret,settings.Theme);Assert.Equal(secret,games[0].Title);
    }
    [Fact] public void MissingFirmwareAndUnconfiguredEmulatorsAreReportedWithoutFileNames()
    {
        using var dir=new TestDirectory();var report=Diagnostics.Create(dir.Path,new(),[new("Secret PS2 title","PS2","secret.iso")],"0.14.116",new("private-device",1920,1080,true));
        Assert.Contains("PCSX2: nicht eingerichtet",report);Assert.Contains("Firmware prüfen / eigene Dateien importieren",report);Assert.DoesNotContain("Secret",report);Assert.DoesNotContain("secret.iso",report);Assert.DoesNotContain("private-device",report);
    }
    [Fact] public void CancellationDoesNotWriteAnIncompleteReport()
    {
        using var dir=new TestDirectory();using var source=new CancellationTokenSource();source.Cancel();Assert.Throws<OperationCanceledException>(()=>Diagnostics.Create(dir.Path,new(),[new("Secret","NES","missing.nes")],"1.0",new("private",1,1,true),token:source.Token));Assert.Empty(Directory.GetFiles(dir.Path));
    }
    [AvaloniaFact] public async Task PreviewIsReadOnlyAndClosingDoesNotSaveOrUpload()
    {
        using var dir=new TestDirectory();new SettingsStore(Path.Combine(dir.Path,"settings.json")).Save(new(){OnboardingCompleted=true,AutoDownloadCovers=false,CheckAppUpdatesOnStartup=false,CheckUpdatesOnStartup=false});var window=new MainWindow(dir.Path);window.Show();
        void Render(Window w){w.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
        Render(window);var before=Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories).ToHashSet();var task=(Task)typeof(MainWindow).GetMethod("ShowDiagnosticsAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,null)!;
        try
        {
            for(var i=0;i<200&&!window.OwnedWindows.Any();i++){await Task.Delay(10);Dispatcher.UIThread.RunJobs();}
            var dialog=Assert.Single(window.OwnedWindows);Render(dialog);var preview=Assert.Single(dialog.GetVisualDescendants().OfType<TextBox>());Assert.True(preview.IsReadOnly);Assert.Contains("EmuVora · Diagnosebericht",preview.Text);Assert.Contains(dialog.GetVisualDescendants().OfType<Button>(),b=>b.Content as string=="Bericht lokal speichern …");
            var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);using var frame=dialog.CaptureRenderedFrame();frame!.Save(Path.Combine(folder,"Diagnostic-preview.png"));}
            dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Schließen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await task;Assert.True(before.SetEquals(Directory.GetFiles(dir.Path,"*",SearchOption.AllDirectories)));
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }
}
