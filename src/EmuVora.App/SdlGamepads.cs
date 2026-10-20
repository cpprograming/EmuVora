using System.Runtime.InteropServices;

namespace EmuVora.App;

public interface IGamepadSource : IDisposable
{
    string Name {get;}
    Gamepad.Snapshot[] Read();
}

public sealed class SdlGamepads : IGamepadSource
{
    private const uint Flags=0x2000;
    private readonly Dictionary<uint,(IntPtr Handle,int Index)> devices=[];
    private int nextIndex=4;
    private bool disposed;
    public string Name=>"SDL 3";
    public SdlGamepads()
    {
        SdlNative.EnsureLoaded();
        var mappings=Path.Combine(AppContext.BaseDirectory,"gamecontrollerdb.txt");SdlNative.Verify(mappings,SdlNative.DatabaseSha);
        SdlNative.SDL_SetMainReady();SdlNative.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS","1");
        if(!SdlNative.SDL_InitSubSystem(Flags))throw new InvalidOperationException(SdlNative.Error());
        try
        {
            if(SdlNative.SDL_AddGamepadMappingsFromFile(mappings)<0)throw new InvalidOperationException(SdlNative.Error());
            // Avalonia owns the window loop. Poll state without accumulating button events.
            SdlNative.SDL_SetGamepadEventsEnabled(false);SdlNative.SDL_SetJoystickEventsEnabled(false);
        }
        catch{SdlNative.SDL_QuitSubSystem(Flags);throw;}
    }
    public Gamepad.Snapshot[] Read()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        SdlNative.SDL_PumpEvents();SdlNative.SDL_UpdateGamepads();
        var array=SdlNative.SDL_GetGamepads(out var count);
        var ids=new int[Math.Clamp(count,0,256)];
        try
        {
            if(array==IntPtr.Zero||count<0||count>256)throw new InvalidOperationException(SdlNative.Error());
            Marshal.Copy(array,ids,0,ids.Length);
        }
        finally{if(array!=IntPtr.Zero)SdlNative.SDL_free(array);}
        var connected=ids.Select(id=>unchecked((uint)id)).Where(id=>id!=0).Order().Take(16).ToHashSet();
        foreach(var id in devices.Keys.Where(id=>!connected.Contains(id)).ToArray()){SdlNative.SDL_CloseGamepad(devices[id].Handle);devices.Remove(id);}
        var result=new List<Gamepad.Snapshot>();
        foreach(var id in connected.Order())
        {
            if(!devices.TryGetValue(id,out var device))
            {
                var handle=SdlNative.SDL_OpenGamepad(id);if(handle==IntPtr.Zero)continue;
                device=(handle,checked(nextIndex++));devices[id]=device;
            }
            if(!SdlNative.SDL_GamepadConnected(device.Handle))continue;
            ushort buttons=0;
            ushort[] masks=[0x1000,0x2000,0x4000,0x8000,0x20,0,0x10,0x40,0x80,0x100,0x200,1,2,4,8];
            for(var b=0;b<masks.Length;b++)if(masks[b]!=0&&SdlNative.SDL_GetGamepadButton(device.Handle,b))buttons|=masks[b];
            short Axis(int axis)=>SdlNative.SDL_GetGamepadAxis(device.Handle,axis);
            var name=Marshal.PtrToStringUTF8(SdlNative.SDL_GetGamepadName(device.Handle))??"Gamepad";
            name=new string(name.Where(c=>!char.IsControl(c)).Take(120).ToArray());
            var connection=SdlNative.SDL_GetGamepadConnectionState(device.Handle) switch {1=>"Kabel",2=>"Drahtlos",_=>"Unbekannt"};
            result.Add(new(device.Index,buttons,Axis(0),InvertY(Axis(1)),Trigger(Axis(4)),Trigger(Axis(5)),Axis(2),InvertY(Axis(3)),name,connection,"SDL 3"));
        }
        return result.ToArray();
    }
    internal static short InvertY(short value)=>(short)Math.Clamp(-(int)value,short.MinValue,short.MaxValue);
    internal static byte Trigger(short value)=>(byte)((Math.Clamp((int)value,0,32767)*255+16383)/32767);
    public void Dispose()
    {
        if(disposed)return;disposed=true;foreach(var pad in devices.Values)SdlNative.SDL_CloseGamepad(pad.Handle);devices.Clear();SdlNative.SDL_QuitSubSystem(Flags);
    }
}

public sealed class GamepadRouter : IGamepadSource
{
    private IGamepadSource source;
    private readonly Func<IGamepadSource> fallback;
    private bool usingFallback;
    public string Name=>source.Name;
    public string? Warning {get;private set;}
    public GamepadRouter(Func<IGamepadSource> primary,Func<IGamepadSource> fallback)
    {
        this.fallback=fallback;
        try{source=primary();}catch(Exception ex) when(IsUnavailable(ex)){Warning=ex.Message;source=fallback();usingFallback=true;}
    }
    private static bool IsUnavailable(Exception ex)=>ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException or IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException;
    public Gamepad.Snapshot[] Read()
    {
        try{return source.Read();}
        catch(Exception ex) when(!usingFallback&&IsUnavailable(ex))
        {
            Warning=ex.Message;try{source.Dispose();}catch(Exception cleanup) when(IsUnavailable(cleanup)){Warning+=" · "+cleanup.Message;}source=fallback();usingFallback=true;return source.Read();
        }
    }
    public void Dispose()=>source.Dispose();
}
