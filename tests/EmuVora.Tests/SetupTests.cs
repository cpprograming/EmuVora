using EmuVora.Core;
using EmuVora.App;
using Xunit;
namespace EmuVora.Tests;
public class SetupTests
{
    [Fact] public void EveryAdditionalSystemScansFromItsDocumentedFolder()
    {
        foreach(var system in SystemCatalog.RetroSystems)
        {
            using var dir=new TestDirectory();var extension=system.Extensions.Split(' ')[0];var file=dir.File(system.Aliases.Split('|')[0]+"/Game/game."+extension);
            if(extension=="zip")System.IO.File.WriteAllBytes(file,[1,2,3]); // Arcade/DOS archives stay intact; scanner never interprets their contents.
            var game=Assert.Single(RomScanner.Scan(dir.Path));Assert.Equal(system.Name,game.System);Assert.Equal(System.IO.Path.GetFullPath(file),game.Path);
        }
    }
    [Fact] public void SharedDiskAndFirmwareExtensionsNeedAnExplicitSystemFolder()
    {
        using var dir=new TestDirectory();dir.File("disk.dsk");dir.File("bios.rom");dir.File("Amstrad CPC/game.dsk");dir.File("MSX/game.rom");
        var games=RomScanner.Scan(dir.Path);Assert.Equal(2,games.Count);Assert.Contains(games,g=>g.System=="MSX");Assert.Contains(games,g=>g.System=="Amstrad CPC");
    }
    [Fact] public void ArcadeChdsRemainDependenciesAndAllSystemsHaveCoverSources()
    {
        using var dir=new TestDirectory();dir.File("MAME/game.zip","ROM set");dir.File("MAME/game/game.chd","disk");Assert.Equal("Arcade (MAME)",Assert.Single(RomScanner.Scan(dir.Path)).System);
        foreach(var system in EmulatorCatalog.Systems)Assert.NotNull(CoverLookup.GetUrl(new("Game",system,"path")));
    }
    [Theory][InlineData("retroarch","retroarch.cfg")][InlineData("duckstation","settings.ini")][InlineData("pcsx2","inis/PCSX2.ini")][InlineData("dolphin","User/Config/GCPadNew.ini")][InlineData("ppsspp","memstick/PSP/SYSTEM/ppsspp.ini")]
    public void ManagedSetupCreatesPortableConfigurationAndPreservesUserChanges(string id,string relative)
    {
        using var dir=new TestDirectory();var executable=dir.File("emulator/program.exe");var data=System.IO.Path.Combine(dir.Path,"data");
        EmulatorSetup.Configure(id,executable,data,firstController:2);var file=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(executable)!,relative);Assert.True(System.IO.File.Exists(file));
        var custom=System.IO.File.ReadAllText(file).Replace("XInput-2/A","Keyboard/K").Replace("input_player1_a_btn = \"1\"","input_player1_a_btn = \"custom\"")+"\n; custom settings\n";System.IO.File.WriteAllText(file,custom);
        EmulatorSetup.Configure(id,executable,data,firstController:0);var result=System.IO.File.ReadAllText(file);Assert.Contains("; custom settings",result);
        if(id=="retroarch"){Assert.Contains("input_joypad_driver = \"xinput\"",result);Assert.Contains("input_player1_a_btn = \"custom\"",result);Assert.Contains("input_player1_joypad_index = \"0\"",result);Assert.Contains("input_player4_r2_axis = \"+5\"",result);Assert.Contains("input_player1_l_y_minus_axis = \"+1\"",result);}
        if(id is "duckstation" or "pcsx2"){Assert.Contains("Cross = Keyboard/K",result);Assert.Contains("L2 = XInput-0/+LeftTrigger",result);Assert.Contains("SmallMotor = XInput-0/SmallMotor",result);Assert.True(System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(executable)!,"portable.txt")));}
        if(id=="dolphin"){Assert.Contains("Device = XInput/0/Gamepad",result);Assert.Contains("Triggers/R = `Trigger R`",result);Assert.True(System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(executable)!,"User/Config/WiimoteNew.ini")));}
    }
    [Fact] public void IniMergeAddsKeysInTheExistingSectionAndSavesBackup()
    {
        using var dir=new TestDirectory();var file=dir.File("settings.ini","[Main]\nCustom = yes\n[Other]\nValue = 1\n");
        EmulatorSetup.Merge(file,new(){{"Main",new(){{"New","2"}}}});var text=System.IO.File.ReadAllText(file);Assert.Equal(1,text.Split("[Main]").Length-1);Assert.True(text.IndexOf("New = 2")<text.IndexOf("[Other]"));Assert.True(System.IO.File.Exists(file+".bak"));
    }
    [Fact] public void ExistingControllerAndFirmwarePathsAreRetainedWithoutExplicitReset()
    {
        using var dir=new TestDirectory();var exe=dir.File("retro/program.exe");var config=dir.File("retro/retroarch.cfg","input_joypad_driver = \"sdl2\"\nsystem_directory = \"../my-bios\"\n");
        EmulatorSetup.Configure("retroarch",exe,dir.Path);Assert.Contains("input_joypad_driver = \"sdl2\"",System.IO.File.ReadAllText(config));Assert.Equal(System.IO.Path.GetFullPath(System.IO.Path.Combine(dir.Path,"my-bios")),EmulatorSetup.ConfiguredFirmwareDirectory("retroarch",exe,dir.Path));
    }
    [Fact] public void Pcsx2SelectsImportedBiosUsingTheOfficialFilenamesSetting()
    {
        using var dir=new TestDirectory();var exe=dir.File("pcsx2/program.exe");var firmware=EmulatorSetup.FirmwareDirectory(dir.Path);Directory.CreateDirectory(firmware);System.IO.File.WriteAllBytes(System.IO.Path.Combine(firmware,"ps2.bin"),new byte[4*1024*1024]);
        EmulatorSetup.Configure("pcsx2",exe,dir.Path);var config=System.IO.File.ReadAllText(System.IO.Path.Combine(dir.Path,"pcsx2/inis/PCSX2.ini"));Assert.Contains("[Filenames]",config);Assert.Contains("BIOS = ps2.bin",config);Assert.Null(EmulatorSetup.FirmwareHint("PS2",dir.Path));
    }
    [Fact] public void FirmwareImportRoutesSubdirectoriesAndChecksAdditionalFiles()
    {
        using var dir=new TestDirectory();var data=System.IO.Path.Combine(dir.Path,"data");var file=dir.File("sources/dc_boot.bin","firmware");
        Assert.EndsWith(System.IO.Path.Combine("dc","dc_boot.bin"),EmulatorSetup.ImportFirmware(file,data));Assert.Null(EmulatorSetup.FirmwareHint("Dreamcast",data));
        EmulatorSetup.ImportFirmware(dir.File("sources/exec.bin","firmware"),data);Assert.Contains("grom.bin",EmulatorSetup.FirmwareHint("Intellivision",data));
        Assert.Throws<InvalidOperationException>(()=>EmulatorSetup.ImportFirmwareFolder(dir.Path,data));
    }
    [Fact] public void XboxNavigationUsesStickDeadzoneAndKeepsActionButtons()
    {
        Assert.Equal((ushort)0x1000,Gamepad.NavigationButtons(new(0,0x1000,8000,-8000)));
        Assert.Equal((ushort)9,Gamepad.NavigationButtons(new(1,0,24000,24000)));
        Assert.Equal((ushort)6,Gamepad.NavigationButtons(new(3,0,-24000,-24000)));
    }
    [Fact] public void AutomaticSetupIsEnabledByDefaultAndCanBeDisabledPersistently()
    {
        using var dir=new TestDirectory();var store=new SettingsStore(System.IO.Path.Combine(dir.Path,"settings.json"));Assert.True(store.Load().AutomaticSetup);Assert.True(store.Load().XboxDefaults);store.Save(new(){AutomaticSetup=false,XboxDefaults=false});Assert.False(store.Load().AutomaticSetup);Assert.False(store.Load().XboxDefaults);
        Assert.Equal(47,EmulatorCatalog.Systems.Length);
    }
}
