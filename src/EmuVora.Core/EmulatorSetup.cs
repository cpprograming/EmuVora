namespace EmuVora.Core;

public static class EmulatorSetup
{
    public static string FirmwareDirectory(string dataDirectory)=>Path.Combine(dataDirectory,"firmware");
    private static string Quoted(string value)=>"\""+value.Replace('\\','/').Replace("\"","")+"\"";
    public static void Configure(string id,string executable,string dataDirectory,bool xbox=true,bool fullscreen=true,bool reset=false,int firstController=0)
    {
        var root=Path.GetDirectoryName(executable)!;var firmware=FirmwareDirectory(dataDirectory);Directory.CreateDirectory(firmware);
        var marker=Path.Combine(root,".emuvora-configured");var replace=reset;
        // Only call this for managed packages. Portable markers keep settings inside the package.
        if(id=="retroarch")
        {
            var values=new Dictionary<string,string>
            {
                ["system_directory"]=Quoted(firmware),["savefile_directory"]=Quoted(Path.Combine(dataDirectory,"saves")),
                ["savestate_directory"]=Quoted(Path.Combine(dataDirectory,"states")),["video_fullscreen"]=fullscreen?"true":"false",
                ["config_save_on_exit"]="true",["input_autodetect_enable"]="true",["rgui_show_start_screen"]="false"
            };
            Directory.CreateDirectory(Path.Combine(dataDirectory,"saves"));Directory.CreateDirectory(Path.Combine(dataDirectory,"states"));
            if(xbox)
            {
                values["input_joypad_driver"]=Quoted("xinput");
                values["input_enable_hotkey_btn"]=Quoted("7");values["input_exit_emulator_btn"]=Quoted("6");values["input_menu_toggle_btn"]=Quoted("5");
                var buttons=new Dictionary<string,string>{{"b","0"},{"a","1"},{"y","2"},{"x","3"},{"l","4"},{"r","5"},{"start","6"},{"select","7"},{"l3","8"},{"r3","9"},{"up","h0up"},{"down","h0down"},{"left","h0left"},{"right","h0right"}};
                for(var player=1;player<=4;player++)
                {
                    values[$"input_player{player}_joypad_index"]=Quoted(((firstController+player-1)%4).ToString());
                    values[$"input_player{player}_analog_dpad_mode"]=Quoted("1");
                    foreach(var (button,value) in buttons)values[$"input_player{player}_{button}_btn"]=Quoted(value);
                    foreach(var (axis,value) in new Dictionary<string,string>{{"l_x_minus","-0"},{"l_x_plus","+0"},{"l_y_minus","+1"},{"l_y_plus","-1"},{"r_x_minus","-2"},{"r_x_plus","+2"},{"r_y_minus","+3"},{"r_y_plus","-3"},{"l2","+4"},{"r2","+5"}})values[$"input_player{player}_{axis}_axis"]=Quoted(value);
                }
            }
            Merge(Path.Combine(root,"retroarch.cfg"),new(){{"",values}},replace);
            Merge(Path.Combine(root,"retroarch-core-options.cfg"),new(){{"",new(){{"flycast_hle_bios",Quoted(File.Exists(Path.Combine(firmware,"dc/dc_boot.bin"))?"disabled":"enabled")}}}},replace);
        }
        else if(id is "duckstation" or "pcsx2")
        {
            var duck=id=="duckstation";WriteIfAbsent(Path.Combine(root,"portable.txt"),"");
            var sections=new Dictionary<string,Dictionary<string,string>>
            {
                ["BIOS"]=new(){{duck?"SearchDirectory":"",firmware}},
                ["Folders"]=duck?new():new(){{"Bios",firmware}},
                [duck?"Main":"UI"]=duck?new(){{"SetupWizardIncomplete","false"},{"ConfirmPowerOff","false"}}:new(){{"SettingsVersion","1"},{"SetupWizardIncomplete","false"},{"ConfirmShutdown","false"}},
                ["Display"]=new(){{"StartFullscreen",fullscreen?"true":"false"}}
            };
            if(!duck)sections.Remove("BIOS");
            if(!duck&&FindPlayStationBios("PS2",firmware) is{} ps2Bios)sections["Filenames"]=new(){{"BIOS",Path.GetFileName(ps2Bios)}};
            if(xbox)
            {
                sections["InputSources"]=new(){{"XInput","true"},{"SDL","false"}};
                for(var port=1;port<=2;port++)sections[$"Pad{port}"]=PlayStationPad(duck,(firstController+port-1)%4);
                sections["Hotkeys"]=new(){{duck?"PowerOff":"ShutdownVM",$"XInput-{firstController}/Back & XInput-{firstController}/Start"},{"OpenPauseMenu",$"XInput-{firstController}/Back & XInput-{firstController}/RightShoulder"}};
            }
            Merge(Path.Combine(root,duck?"settings.ini":"inis/PCSX2.ini"),sections,replace);
        }
        else if(id=="dolphin")
        {
            WriteIfAbsent(Path.Combine(root,"portable.txt"),"");
            Merge(Path.Combine(root,"User/Config/Dolphin.ini"),new(){{"Display",new(){{"Fullscreen",fullscreen?"True":"False"}}},{"Core",new(){{"SIDevice0","6"}}},{"Interface",new(){{"ConfirmStop","False"}}}},replace);
            if(xbox)
            {
                var pads=new Dictionary<string,Dictionary<string,string>>();
                for(var port=1;port<=4;port++)
                {
                    var p=new Dictionary<string,string>{{"Device",$"XInput/{(firstController+port-1)%4}/Gamepad"},{"Main Stick/Dead Zone","15.0"},{"C-Stick/Dead Zone","15.0"},{"Rumble/Motor","`Motor L` | `Motor R`"}};
                    foreach(var (button,value) in new Dictionary<string,string>{{"Buttons/A","Button A"},{"Buttons/B","Button B"},{"Buttons/X","Button X"},{"Buttons/Y","Button Y"},{"Buttons/Z","Shoulder R"},{"Buttons/Start","Start"},{"D-Pad/Up","Pad N"},{"D-Pad/Down","Pad S"},{"D-Pad/Left","Pad W"},{"D-Pad/Right","Pad E"},{"Main Stick/Up","Left Y+"},{"Main Stick/Down","Left Y-"},{"Main Stick/Left","Left X-"},{"Main Stick/Right","Left X+"},{"C-Stick/Up","Right Y+"},{"C-Stick/Down","Right Y-"},{"C-Stick/Left","Right X-"},{"C-Stick/Right","Right X+"},{"Triggers/L","Trigger L"},{"Triggers/R","Trigger R"}})p[button]="`"+value+"`";
                    pads[$"GCPad{port}"]=p;
                    p["Triggers/L-Analog"]="`Trigger L`";p["Triggers/R-Analog"]="`Trigger R`";
                }
                Merge(Path.Combine(root,"User/Config/GCPadNew.ini"),pads,replace);
                var wii=new Dictionary<string,string>{{"Device",$"XInput/{firstController}/Gamepad"},{"Source","1"},{"Extension","Nunchuk"},{"Buttons/A","`Button A`"},{"Buttons/B","`Trigger R`"},{"Buttons/1","`Button X`"},{"Buttons/2","`Button Y`"},{"Buttons/+","`Start`"},{"Buttons/-","`Back`"},{"Buttons/Home","`Thumb R`"},{"D-Pad/Up","`Pad N`"},{"D-Pad/Down","`Pad S`"},{"D-Pad/Left","`Pad W`"},{"D-Pad/Right","`Pad E`"},{"Nunchuk/Buttons/C","`Shoulder L`"},{"Nunchuk/Buttons/Z","`Trigger L`"},{"Nunchuk/Stick/Up","`Left Y+`"},{"Nunchuk/Stick/Down","`Left Y-`"},{"Nunchuk/Stick/Left","`Left X-`"},{"Nunchuk/Stick/Right","`Left X+`"},{"Shake/X","`Shoulder R`"},{"Shake/Y","`Shoulder R`"},{"Shake/Z","`Shoulder R`"},{"IR/Up","`Right Y+`"},{"IR/Down","`Right Y-`"},{"IR/Left","`Right X-`"},{"IR/Right","`Right X+`"},{"IR/Relative Input","True"}};
                Merge(Path.Combine(root,"User/Config/WiimoteNew.ini"),new(){{"Wiimote1",wii}},replace);
                Merge(Path.Combine(root,"User/Config/Hotkeys.ini"),new(){{"Hotkeys",new(){{"Device",$"XInput/{firstController}/Gamepad"},{"General/Stop","`Back` & `Start`"},{"General/Exit","`Back` & `Thumb R`"}}}},replace);
            }
        }
        else if(id=="ppsspp")
        {
            // PPSSPP's shipped Windows defaults already map Xbox/XInput, including analog input.
            Merge(Path.Combine(root,"memstick/PSP/SYSTEM/ppsspp.ini"),new(){{"Graphics",new(){{"FullScreen",fullscreen?"True":"False"}}}},replace);
        }
        WriteIfAbsent(marker,"EmuVora automatic setup v1");
    }
    private static Dictionary<string,string> PlayStationPad(bool duck,int index)
    {
        var p=new Dictionary<string,string>{{"Type",duck?"AnalogController":"DualShock2"},{"Deadzone","0.15"}};
        foreach(var (key,value) in new Dictionary<string,string>{{"Up","DPadUp"},{"Down","DPadDown"},{"Left","DPadLeft"},{"Right","DPadRight"},{"Cross","A"},{"Circle","B"},{"Square","X"},{"Triangle","Y"},{"Start","Start"},{"Select","Back"},{"L1","LeftShoulder"},{"R1","RightShoulder"},{"L2","+LeftTrigger"},{"R2","+RightTrigger"},{"L3","LeftStick"},{"R3","RightStick"},{"LLeft","-LeftX"},{"LRight","+LeftX"},{"LUp","-LeftY"},{"LDown","+LeftY"},{"RLeft","-RightX"},{"RRight","+RightX"},{"RUp","-RightY"},{"RDown","+RightY"},{"LargeMotor","LargeMotor"},{"SmallMotor","SmallMotor"}})p[key]=$"XInput-{index}/{value}";
        return p;
    }
    private static void WriteIfAbsent(string path,string value){if(!File.Exists(path))AtomicFile.WriteText(path,value);}
    public static void Merge(string file,Dictionary<string,Dictionary<string,string>> defaults,bool replace=false)
    {
        var manifest=file+".emuvora-defaults.json";
        Dictionary<string,Dictionary<string,string>> prior=[];
        if(File.Exists(manifest))try{prior=System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(File.ReadAllText(manifest))??[];}catch(System.Text.Json.JsonException){ }
        var pending=defaults.ToDictionary(p=>p.Key,p=>new Dictionary<string,string>(p.Value,StringComparer.OrdinalIgnoreCase),StringComparer.OrdinalIgnoreCase);
        var output=new List<string>();var section="";
        foreach(var line in File.Exists(file)?File.ReadAllLines(file):[])
        {
            var text=line.Trim();if(text.StartsWith('[')&&text.EndsWith(']'))section=text[1..^1];
            var split=text.IndexOf('=');
            if(split>0&&!text.StartsWith('#')&&!text.StartsWith(';')&&pending.TryGetValue(section,out var values)&&values.Remove(text[..split].Trim(),out var value))
            {
                var key=text[..split].Trim();var managed=prior.TryGetValue(section,out var previous)&&previous.TryGetValue(key,out var old)&&text[(split+1)..].Trim()==old;
                output.Add(replace||managed?key+" = "+value:line);
            }
            else output.Add(line);
        }
        foreach(var (name,values) in pending)
        {
            if(values.Count==0)continue;
            // Insert inside the existing section; duplicate INI sections are not universally accepted.
            var start=name.Length==0?-1:output.FindIndex(l=>l.Trim().Equals("["+name+"]",StringComparison.OrdinalIgnoreCase));
            if(start<0&&name.Length>0){output.Add("");output.Add("["+name+"]");start=output.Count-1;}
            var end=output.FindIndex(start+1,l=>l.TrimStart().StartsWith('['));if(end<0)end=output.Count;
            output.InsertRange(end,values.Select(p=>p.Key+" = "+p.Value));
        }
        var content=string.Join(Environment.NewLine,output)+Environment.NewLine;
        if(!File.Exists(file)||File.ReadAllText(file)!=content)AtomicFile.WriteText(file,content);
        var json=System.Text.Json.JsonSerializer.Serialize(defaults,SettingsStore.JsonOptions);
        if(!File.Exists(manifest)||File.ReadAllText(manifest)!=json)AtomicFile.WriteText(manifest,json);
    }
    public static string? FirmwareHint(string system,string dataDirectory,string? firmwareDirectory=null)
    {
        var directory=firmwareDirectory??FirmwareDirectory(dataDirectory);
        if(system=="Dreamcast")return null; // Flycast provides HLE BIOS; a real BIOS is optional.
        if(system=="MSX")return new[]{"Machines","Databases"}.All(n=>Directory.Exists(Path.Combine(directory,n))&&Directory.EnumerateFiles(Path.Combine(directory,n),"*",SearchOption.AllDirectories).Any())?null:"MSX: eigenen blueMSX-Datenordner mit Machines und Databases importieren (gemeinsamen übergeordneten Ordner wählen).";
        if(system is "PS1" or "PS2")return FindPlayStationBios(system,directory)!=null?null:$"{system}: eigenes BIOS in den Firmware-Ordner importieren (PS1: 512 KiB, PS2: 4 MiB).";
        var definition=SystemCatalog.Find(system);if(definition?.Firmware==null)return null;
        var names=definition.Firmware.Split('|');
        if(names.Any(n=>File.Exists(Path.Combine(directory,n))&&new FileInfo(Path.Combine(directory,n)).Length>0))
        {
            var additional=system switch {"Intellivision"=>"grom.bin",_=>null};
            if(additional!=null&&!File.Exists(Path.Combine(directory,additional)))return system+": Firmware fehlt: "+additional;
            return null;
        }
        return system+": Firmware fehlt: "+string.Join(" oder ",names)+". "+definition.Note;
    }
    public static string ConfiguredFirmwareDirectory(string id,string executable,string dataDirectory)
    {
        var root=Path.GetDirectoryName(executable)!;
        var (file,expectedSection,key)=id switch
        {
            "retroarch"=>("retroarch.cfg","","system_directory"),
            "duckstation"=>("settings.ini","BIOS","SearchDirectory"),
            "pcsx2"=>("inis/PCSX2.ini","Folders","Bios"),
            _=>("","","")
        };
        var path=Path.Combine(root,file);var section="";
        if(File.Exists(path))foreach(var line in File.ReadLines(path))
        {
            var text=line.Trim();if(text.StartsWith('[')&&text.EndsWith(']'))section=text[1..^1];
            var split=text.IndexOf('=');if(split<1||section!=expectedSection||!text[..split].Trim().Equals(key,StringComparison.OrdinalIgnoreCase))continue;
            var value=text[(split+1)..].Trim().Trim('"');if(value.Length>0&&value!="default")return Path.GetFullPath(Path.Combine(root,value));
        }
        return FirmwareDirectory(dataDirectory);
    }
    public static string ImportFirmware(string source,string dataDirectory)
    {
        var name=Path.GetFileName(source);var candidates=SystemCatalog.RetroSystems.Where(s=>s.Firmware!=null).SelectMany(s=>s.Firmware!.Split('|')).Concat(["neocd/000-lo.lo","grom.bin","dc/dc_flash.bin"]);
        var relative=candidates.FirstOrDefault(n=>Path.GetFileName(n).Equals(name,StringComparison.OrdinalIgnoreCase))??name;
        var destination=Path.GetFullPath(Path.Combine(FirmwareDirectory(dataDirectory),relative));Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if(!Path.GetFullPath(source).Equals(Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase)){if(File.Exists(destination))File.Copy(destination,destination+".bak",true);File.Copy(source,destination,true);}return destination;
    }
    public static void ImportFirmwareFolder(string source,string dataDirectory,CancellationToken token=default)
    {
        var root=Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        var targetRoot=Path.GetFullPath(FirmwareDirectory(dataDirectory)).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(root.StartsWith(targetRoot,StringComparison.OrdinalIgnoreCase)||targetRoot.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Quellordner und Firmware-Zielordner müssen getrennt sein.");
        long total=0;
        foreach(var file in Directory.EnumerateFiles(source,"*",new EnumerationOptions {RecurseSubdirectories=true,AttributesToSkip=FileAttributes.ReparsePoint}))
        {
            token.ThrowIfCancellationRequested();if((total+=new FileInfo(file).Length)>512L*1024*1024)throw new InvalidDataException("Firmware-Ordner ist größer als 512 MiB. Bitte nur die Firmware auswählen.");
            var target=Path.Combine(targetRoot,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);if(File.Exists(target))File.Copy(target,target+".bak",true);File.Copy(file,target,true);
        }
    }
    private static string? FindPlayStationBios(string system,string directory)=>Directory.Exists(directory)?Directory.EnumerateFiles(directory,"*",SearchOption.TopDirectoryOnly).FirstOrDefault(f=>new FileInfo(f).Length==(system=="PS1"?512*1024:4*1024*1024)):null;
}
