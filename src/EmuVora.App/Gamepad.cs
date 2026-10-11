using System.Runtime.InteropServices;
namespace EmuVora.App;
public static class Gamepad
{
    public readonly record struct Snapshot(int Index,ushort Buttons,short X,short Y);
    [StructLayout(LayoutKind.Sequential)] private struct Pad {public ushort Buttons;public byte LeftTrigger,RightTrigger;public short LX,LY,RX,RY;}
    [StructLayout(LayoutKind.Sequential)] private struct State {public uint Packet;public Pad Pad;}
    [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")] private static extern uint GetState(uint index,out State state);
    public static ushort ReadButtons()
        =>Read().FirstOrDefault().Buttons;
    public static Snapshot[] Read()
    {
        if(!OperatingSystem.IsWindows())return [];
        var connected=new List<Snapshot>();
        try{for(uint i=0;i<4;i++)if(GetState(i,out var state)==0)connected.Add(new((int)i,state.Pad.Buttons,state.Pad.LX,state.Pad.LY));}
        catch(DllNotFoundException){ }catch(EntryPointNotFoundException){ }return connected.ToArray();
    }
    public static ushort NavigationButtons(Snapshot state)
    {
        var buttons=state.Buttons;
        if(state.X>16000)buttons|=8;if(state.X< -16000)buttons|=4;
        if(state.Y>16000)buttons|=1;if(state.Y< -16000)buttons|=2;
        return buttons;
    }
}
