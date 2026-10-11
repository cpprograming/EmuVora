namespace EmuVora.Core;

public record SystemDefinition(string Name,string Core,string Extensions,string Aliases,string? Firmware=null,string? Note=null,bool ArchiveSet=false);
public static class SystemCatalog
{
    // Shared by scanning, automatic core setup and the setup assistant.
    public static readonly SystemDefinition[] RetroSystems =
    [
        new("NES","fceumm","nes","nes|famicom"),
        new("SNES","snes9x","sfc smc","snes|super nintendo|super famicom"),
        new("GB","gambatte","gb","gb|game boy"),new("GBC","gambatte","gbc","gbc|game boy color"),
        new("GBA","mgba","gba","gba|game boy advance"),new("N64","mupen64plus_next","n64 z64 v64","n64|nintendo 64"),
        new("Mega Drive","genesis_plus_gx","gen md","megadrive|mega drive|genesis"),new("Master System","genesis_plus_gx","sms","mastersystem|master system|sms"),
        new("Nintendo DS","desmume","nds","nds|ds|nintendo ds",Note:"Touchscreen: Maus oder Touch; nicht jede DS-Aktion ist mit einem Gamepad bedienbar."),
        new("Virtual Boy","mednafen_vb","vb","virtualboy|virtual boy"),
        new("Game Gear","genesis_plus_gx","gg","gamegear|game gear"),
        new("Mega-CD","genesis_plus_gx","cue chd m3u","segacd|mega-cd|mega cd|sega cd","bios_CD_U.bin|bios_CD_E.bin|bios_CD_J.bin","Die BIOS-Region muss zum Spiel passen."),
        new("32X","picodrive","32x","32x|sega32x|sega 32x"),
        new("Saturn","mednafen_saturn","cue chd m3u","saturn|sega saturn","sega_101.bin|mpr-17933.bin","Die BIOS-Region muss zum Spiel passen."),
        new("Dreamcast","flycast","gdi cdi chd m3u","dreamcast|dc","dc/dc_boot.bin","Ein echtes BIOS ist optional; ohne BIOS wird Flycast HLE automatisch aktiviert."),
        new("Atari 2600","stella","a26","atari2600|atari 2600"),new("Atari 5200","a5200","a52","atari5200|atari 5200","5200.rom"),
        new("Atari 7800","prosystem","a78","atari7800|atari 7800"),new("Atari Lynx","handy","lnx","lynx|atari lynx","lynxboot.img"),
        new("Atari Jaguar","virtualjaguar","j64 jag","jaguar|atari jaguar"),
        new("PC Engine","mednafen_pce_fast","pce","pcengine|pc engine|turbografx16|turbografx-16"),
        new("PC Engine CD","mednafen_pce_fast","cue chd m3u","pcenginecd|pc engine cd|turbografx-cd","syscard3.pce"),
        new("SuperGrafx","mednafen_supergrafx","sgx","supergrafx"),new("PC-FX","mednafen_pcfx","cue chd m3u","pcfx|pc-fx","pcfx.rom"),
        new("Neo Geo","fbneo","zip","neogeo|neo geo","neogeo.zip","Passendes FinalBurn-Neo-ROM-Set verwenden.",true),
        new("Neo Geo CD","neocd","cue chd","neogeocd|neo geo cd","neocd/neocd_f.rom|neocd/neocd_sf.rom|neocd/neocd_t.rom|neocd/neocd_st.rom|neocd/neocd_z.rom|neocd/neocd_sz.rom|neocd/neocd.bin|neocd/uni-bioscd.rom|neocd/front-sp1.bin|neocd/top-sp1.bin"),
        new("Neo Geo Pocket","mednafen_ngp","ngp","ngp|neo geo pocket"),new("Neo Geo Pocket Color","mednafen_ngp","ngc","ngpc|neo geo pocket color"),
        new("3DO","opera","iso cue chd m3u","3do","panafz1.bin|panafz10.bin|goldstar.bin|sanyotry.bin"),
        new("ColecoVision","gearcoleco","col","coleco|colecovision","colecovision.rom"),
        new("Intellivision","freeintv","int","intellivision","exec.bin","Zusätzlich grom.bin benötigt; Zifferntasten können eine Tastatur erfordern."),
        new("WonderSwan","mednafen_wswan","ws","ws|wonderswan"),new("WonderSwan Color","mednafen_wswan","wsc","wsc|wonderswan color"),
        new("Commodore 64","vice_x64","d64 t64 prg crt","c64|commodore 64",Note:"Joystick vorbelegt; manche Spiele benötigen zusätzlich eine Tastatur."),
        new("Amiga","puae","adf hdf lha","amiga","kick40063.A600|kick34005.A500|kick40068.A1200","Kickstart hängt vom gewählten Amiga-Modell ab; Tastatur/Maus können nötig sein."),
        new("Atari ST","hatari","st msa stx","atarist|atari st","tos.img","Manche Spiele benötigen Tastatur/Maus."),
        new("Amstrad CPC","cap32","dsk cdt","amstradcpc|amstrad cpc|cpc",Note:"Manche Spiele benötigen eine Tastatur."),
        new("ZX Spectrum","fuse","tzx tap z80 sna","zxspectrum|zx spectrum",Note:"Joystick vorbelegt; Tastatur kann nötig sein."),
        new("MSX","bluemsx","rom mx1 mx2 dsk cas","msx",Note:"blueMSX-Datenordner Machines und Databases erforderlich; Tastatur kann nötig sein."),
        new("DOS","dosbox_pure","zip dosz","dos|dosbox",Note:"DOSBox Pure bietet automatische Gamepad-Belegung; manche Spiele benötigen Tastatur/Maus.",ArchiveSet:true),
        new("Arcade (FinalBurn Neo)","fbneo","zip 7z","fbneo|finalburn neo|arcade",Note:"ROM-Set muss zur installierten FinalBurn-Neo-Version passen.",ArchiveSet:true),
        new("Arcade (MAME)","mame","zip 7z","mame",Note:"ROM-Set muss zur installierten MAME-Version passen; Zusatzdateien/CHDs können nötig sein.",ArchiveSet:true)
    ];
    public static SystemDefinition? Find(string name)=>RetroSystems.FirstOrDefault(s=>s.Name==name);
    public static string? FromFolder(string name)=>RetroSystems.FirstOrDefault(s=>s.Name.Equals(name,StringComparison.OrdinalIgnoreCase)||s.Aliases.Split('|').Contains(name,StringComparer.OrdinalIgnoreCase))?.Name;
    public static string? FromExtension(string extension)
    {
        var candidates=RetroSystems.Where(s=>s.Extensions.Split(' ').Contains(extension.TrimStart('.'),StringComparer.OrdinalIgnoreCase)&&!s.ArchiveSet).ToArray();
        return candidates.Length==1&&!extension.Equals(".rom",StringComparison.OrdinalIgnoreCase)?candidates[0].Name:null;
    }
}
