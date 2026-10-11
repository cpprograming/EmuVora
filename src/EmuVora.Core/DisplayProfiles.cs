using System.Runtime.InteropServices;

namespace EmuVora.Core;

public record DisplayInfo(string Device,int Width,int Height,bool Primary)
{
    public override string ToString()=>Device+" · "+Width+" × "+Height+(Primary?" · Hauptmonitor":"");
}
public static class DisplayDetection
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct MonitorInfo{public int Size;public Rect Monitor,Work;public int Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
    private delegate bool MonitorCallback(IntPtr monitor,IntPtr dc,ref Rect rect,IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorCallback callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    private static DisplayInfo? Read(IntPtr handle){var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>(),Device=""};return GetMonitorInfo(handle,ref info)?new(info.Device,info.Monitor.Right-info.Monitor.Left,info.Monitor.Bottom-info.Monitor.Top,(info.Flags&1)!=0):null;}
    public static IReadOnlyList<DisplayInfo> All()
    {
        var result=new List<DisplayInfo>();if(!OperatingSystem.IsWindows())return result;
        EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,(IntPtr monitor,IntPtr dc,ref Rect rect,IntPtr data)=>{if(Read(monitor) is{} info)result.Add(info);return true;},IntPtr.Zero);return result;
    }
    public static DisplayInfo? Current(IntPtr window=default)=>OperatingSystem.IsWindows()?Read(MonitorFromWindow(window==IntPtr.Zero?GetForegroundWindow():window,2)):null;
}
public static class DisplayProfiles
{
    public static readonly string[] Modes=["Automatic","PixelPerfect","Fit","Smooth"];
    public static int RenderScale(int monitorHeight,int nativeHeight,int maximum)=>Math.Clamp((int)Math.Ceiling(Math.Max(1,monitorHeight)/(double)nativeHeight),1,Math.Clamp(maximum,1,6));
    public static bool IntegerScaling(string mode,string system)=>mode=="PixelPerfect"||(mode=="Automatic"&&system is not ("N64" or "Dreamcast" or "Saturn" or "3DO" or "PS1" or "PS2" or "GameCube" or "Wii" or "PSP"));
    public static string Description(string system,DisplayInfo? monitor,AppSettings settings,string? gamePath=null)
    {
        if(!settings.AutomaticDisplay)return "Bildprofil: eigene Emulator-Einstellungen";
        var format=gamePath==null?null:settings.GameAspectRatios.GetValueOrDefault(gamePath);
        return (monitor==null?"Monitor nicht verfügbar":monitor.Width+" × "+monitor.Height)+" · "+(format is "4:3" or "16:9"?format:"Originalformat")+" · "+(IntegerScaling(settings.DisplayMode,system)?"ganze Pixelstufen":"proportionale Skalierung")+" · 3D maximal "+Math.Clamp(settings.MaximumRenderScale,1,6)+"×";
    }
    public static void Apply(string id,string executable,string system,DisplayInfo? monitor,AppSettings settings,bool reset=false,string? gamePath=null)
    {
        if(!settings.AutomaticDisplay)return;
        var root=Path.GetDirectoryName(executable)!;var marker=Path.Combine(root,".emuvora-video-profile");var replace=reset||!File.Exists(marker);
        var mode=Modes.Contains(settings.DisplayMode)?settings.DisplayMode:"Automatic";var integer=IntegerScaling(mode,system);var smooth=mode=="Smooth";
        var height=monitor?.Height??720;int Scale(int native)=>RenderScale(height,native,settings.MaximumRenderScale);
        var requested=gamePath==null?null:settings.GameAspectRatios.GetValueOrDefault(gamePath);var aspect=requested is "4:3" or "16:9"?requested:null;
        var relative=id switch{"retroarch"=>"retroarch.cfg","duckstation"=>"settings.ini","pcsx2"=>"inis/PCSX2.ini","dolphin"=>"User/Config/GFX.ini","ppsspp"=>"memstick/PSP/SYSTEM/ppsspp.ini",_=>null};
        if(!File.Exists(marker)&&relative!=null){var original=Path.Combine(root,relative);if(File.Exists(original)&&!File.Exists(original+".emuvora-video-original.bak"))File.Copy(original,original+".emuvora-video-original.bak");}
        if(id=="retroarch")EmulatorSetup.Merge(Path.Combine(root,"retroarch.cfg"),new(){{"",new(){{"video_windowed_fullscreen","true"},{"video_fullscreen_x","0"},{"video_fullscreen_y","0"},{"video_force_aspect","true"},{"video_aspect_ratio_auto","true"},{"aspect_ratio_index",aspect==null?"22":"20"},{"video_aspect_ratio",aspect=="16:9"?"1.7777778":aspect=="4:3"?"1.3333333":"-1"},{"video_scale_integer",integer?"true":"false"},{"video_smooth",smooth?"true":"false"}}}},replace);
        else if(id=="duckstation")EmulatorSetup.Merge(Path.Combine(root,"settings.ini"),new(){{"GPU",new(){{"ResolutionScale",mode=="PixelPerfect"?"1":Scale(240).ToString()}}},{"Display",new(){{"AspectRatio",aspect??"Auto (Game Native)"},{"Scaling",integer?"NearestInteger":smooth?"BilinearSmooth":"BilinearSharp"},{"CropMode","None"}}}},replace);
        else if(id=="pcsx2")EmulatorSetup.Merge(Path.Combine(root,"inis/PCSX2.ini"),new(){{"EmuCore/GS",new(){{"AspectRatio",aspect??"Auto 4:3/3:2"},{"upscale_multiplier",mode=="PixelPerfect"?"1":Scale(480).ToString()},{"IntegerScaling",integer?"true":"false"},{"StretchY","100"}}}},replace);
        else if(id=="dolphin")EmulatorSetup.Merge(Path.Combine(root,"User/Config/GFX.ini"),new(){{"Settings",new(){{"AspectRatio",aspect=="16:9"?"1":aspect=="4:3"?"2":"0"},{"Crop","False"},{"wideScreenHack","False"},{"InternalResolution",mode=="PixelPerfect"?"1":Scale(528).ToString()},{"BorderlessFullscreen","True"}}}},replace);
        else if(id=="ppsspp")EmulatorSetup.Merge(Path.Combine(root,"memstick/PSP/SYSTEM/ppsspp.ini"),new(){{"Graphics",new(){{"InternalResolution",mode=="PixelPerfect"?"1":Scale(272).ToString()}}},{"DisplayLayout.Landscape",new(){{"DisplayStretch","False"},{"DisplayAspectRatio",aspect=="4:3"?"0.7555556":aspect=="16:9"?"1.0074074":"1.000000"},{"DisplayScale","1.000000"},{"DisplayIntegerScale",integer?"True":"False"},{"BufferFiltering",smooth?"1":"2"}}}},replace);
        AtomicFile.WriteText(marker,"EmuVora display profile v1");
    }
}
