namespace EmuVora.Core;

public static class EmulatorLanguage
{
    // Only managed portable packages: no BIOS/NVRAM or save-file patching.
    public static void Apply(string id,string executable)
    {
        var root=Path.GetDirectoryName(executable)!;
        void Set(string relative,Dictionary<string,Dictionary<string,string>> values)
        {
            var file=Path.Combine(root,relative);
            if(File.Exists(file)&&!File.Exists(file+".before-german.bak"))File.Copy(file,file+".before-german.bak");
            EmulatorSetup.Merge(file,values,true);
        }
        switch(id)
        {
            case "retroarch":
                Set("retroarch.cfg",new(){{"",new(){{"user_language","4"}}}});
                Set("retroarch-core-options.cfg",new(){{"",new(){{"flycast_language","\"German\""},{"desmume_firmware_language","\"German\""}}}});break;
            case "duckstation":Set("settings.ini",new(){{"Main",new(){{"Language","de"}}}});break;
            case "pcsx2":Set("inis/PCSX2.ini",new(){{"UI",new(){{"Language","de-DE"}}}});break;
            case "dolphin":Set("User/Config/Dolphin.ini",new(){{"Interface",new(){{"LanguageCode","de"}}},{"Core",new(){{"SelectedLanguage","1"}}}});break;
            case "ppsspp":Set("memstick/PSP/SYSTEM/ppsspp.ini",new(){{"General",new(){{"Language","de_DE"}}},{"SystemParam",new(){{"GameLanguage","4"}}}});break;
        }
    }
}
