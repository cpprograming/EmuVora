using EmuVora.Core;
namespace EmuVora.App;
public sealed partial class MainWindow
{
    private void ReportEmulatorFailure(string emulator,int code)
    {
        var name=EmulatorCatalog.Known.FirstOrDefault(e=>e.Id==emulator)?.Name??emulator;
        var root=Path.GetDirectoryName(settings.EmulatorPaths.GetValueOrDefault(emulator)??"");
        var log=emulator=="pcsx2"&&root!=null?Path.Combine(root,"logs","emulog.txt"):null;
        var message=$"{name} wurde unerwartet beendet (0x{unchecked((uint)code):X8}).";
        if(emulator=="pcsx2")message+=" BIOS-Auswahl und PCSX2-Protokoll prüfen; eine DIFF-/ROM1-Datei ist kein Haupt-BIOS.";
        if(log!=null&&File.Exists(log))message+=" Protokoll: "+log;
        ShowError(new IOException(message));
    }
}
