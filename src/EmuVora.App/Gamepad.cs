using System.Runtime.InteropServices;
namespace EmuVora.App;
public static class Gamepad
{
    [StructLayout(LayoutKind.Sequential)] private struct Pad {public ushort Buttons;public byte LeftTrigger,RightTrigger;public short LX,LY,RX,RY;}
    [StructLayout(LayoutKind.Sequential)] private struct State {public uint Packet;public Pad Pad;}
    [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")] private static extern uint GetState(uint index,out State state);
    public static ushort ReadButtons()
    {
        if(!OperatingSystem.IsWindows())return 0;
        try{for(uint i=0;i<4;i++)if(GetState(i,out var state)==0)return state.Pad.Buttons;}
        catch(DllNotFoundException){ }catch(EntryPointNotFoundException){ }return 0;
    }
}
