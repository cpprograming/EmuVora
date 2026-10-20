using System.Runtime.InteropServices;
namespace EmuVora.App;
public static class Gamepad
{
    public readonly record struct Snapshot(int Index,ushort Buttons,short X,short Y,byte LeftTrigger=0,byte RightTrigger=0,short RightX=0,short RightY=0,string? Name=null,string? Connection=null,string Backend="XInput");
    private static readonly Lazy<GamepadRouter> Input=new(()=>new(()=>OperatingSystem.IsWindows()?new SdlGamepads():new XInputGamepads(),()=>new XInputGamepads()));
    public static string BackendName=>Input.Value.Name;
    public static string? BackendWarning=>Input.Value.Warning;
    public static Snapshot[] Read()=>Input.Value.Read();
    public static void Shutdown(){if(Input.IsValueCreated)Input.Value.Dispose();}
    // SDL instance IDs are frontend identities, never emulator XInput player indices.
    public static int FirstXInputIndex=>EmulatorIndex(XInputGamepads.ReadConnected());
    public static int EmulatorIndex(IEnumerable<Snapshot> pads)=>pads.FirstOrDefault(p=>p.Backend=="XInput"&&p.Index is >=0 and <4).Index;
    public static string Label(Snapshot pad)=>pad.Backend=="SDL 3"?(pad.Name??"Gamepad"):"Slot "+(pad.Index+1);
    public static ushort ReadButtons()=>Read().FirstOrDefault().Buttons;
    public static ushort NavigationButtons(Snapshot state)
    {
        var buttons=state.Buttons;
        if(state.X>16000)buttons|=8;if(state.X< -16000)buttons|=4;
        if(state.Y>16000)buttons|=1;if(state.Y< -16000)buttons|=2;
        return buttons;
    }
}
public sealed class XInputGamepads : IGamepadSource
{
    public string Name=>"XInput";
    public void Dispose(){ }
    public Gamepad.Snapshot[] Read()=>ReadConnected();
    [StructLayout(LayoutKind.Sequential)] private struct Pad {public ushort Buttons;public byte LeftTrigger,RightTrigger;public short LX,LY,RX,RY;}
    [StructLayout(LayoutKind.Sequential)] private struct State {public uint Packet;public Pad Pad;}
    [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")] private static extern uint GetState(uint index,out State state);
    public static Gamepad.Snapshot[] ReadConnected()
    {
        if(!OperatingSystem.IsWindows())return [];
        var connected=new List<Gamepad.Snapshot>();
        try{for(uint i=0;i<4;i++)if(GetState(i,out var state)==0)connected.Add(new((int)i,state.Pad.Buttons,state.Pad.LX,state.Pad.LY,state.Pad.LeftTrigger,state.Pad.RightTrigger,state.Pad.RX,state.Pad.RY));}
        catch(DllNotFoundException){ }catch(EntryPointNotFoundException){ }return connected.ToArray();
    }
}
