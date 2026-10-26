namespace EmuVora.Core;

public record SystemInputGuide(string System,string Category,string Hint,bool AdditionalInput);
public static class InputGuide
{
    public static SystemInputGuide ForSystem(string system) => system switch
    {
        "Nintendo DS"=>new(system,"Touchscreen / Maus","Der DS-Touchscreen wird mit Maus oder Touch bedient. Nicht jede Aktion ist allein mit einem Gamepad möglich. Die konkrete Belegung im DeSmuME-Core prüfen.",true),
        "Wii"=>new(system,"Wii-Steuerung / Bewegung","Das aktivierte Xbox-Grundprofil bildet eine Wiimote mit Nunchuk ab. Bewegungsspiele können Anpassungen oder geeignete Wii-Eingabegeräte benötigen. Die Belegung in Dolphin prüfen.",true),
        "DOS" or "Amiga" or "Atari ST"=>new(system,"Tastatur / Maus","Je nach Spiel sind Tastatur, Maus oder zusätzliche Eingaben erforderlich. Ein Gamepad-Profil ersetzt nicht automatisch Texteingaben und alle ursprünglichen Bedienelemente. Im jeweiligen Emulator prüfen.",true),
        "Commodore 64" or "Amstrad CPC" or "ZX Spectrum" or "MSX"=>new(system,"Tastatur / Joystick","Joystick-Belegung ist ein Ausgangspunkt; Startmenüs, Befehle oder einzelne Spiele können eine Tastatur erfordern. Core-Belegung und ursprüngliche Spielanleitung prüfen.",true),
        "Intellivision"=>new(system,"Gamepad / Zifferntasten","Einige Spiele benötigen die Zifferntasten des ursprünglichen Controllers. Dafür können eine Tastatur und eine passende Core-Belegung erforderlich sein.",true),
        _ when EmulatorCatalog.Systems.Contains(system)=>new(system,"Gamepad","Viele Spiele lassen sich mit dem eingerichteten Gamepad bedienen. Spezielle Eingabegeräte, Mehrspieler und abweichende Belegungen sind spielabhängig und müssen im Emulator geprüft werden.",false),
        _=>new(system,"Bedienung ungeklärt","Zuerst die Konsole in den Spieldetails zuordnen. Für unzugeordnete Systeme wird keine Gamepad-Kompatibilität angenommen.",false)
    };
    public static IReadOnlyList<SystemInputGuide> All()=>EmulatorCatalog.Systems.Select(ForSystem).ToArray();
    public static string Describe(string system){var guide=ForSystem(system);return "Bedienung: "+guide.Category+"\n"+guide.Hint;}
}
