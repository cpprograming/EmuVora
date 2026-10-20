using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EmuVora.App;
using Xunit;

namespace EmuVora.Tests;

public class SdlControllerTests
{
    private sealed class Source(string name,Func<Gamepad.Snapshot[]> read) : IGamepadSource
    {
        public string Name=>name;public int Reads,Disposals;
        public Gamepad.Snapshot[] Read(){Reads++;return read();}public void Dispose()=>Disposals++;
    }
    [Fact] public void HealthySdlIsTheOnlyInputSourceEvenWithoutConnectedPads()
    {
        var fallbackCreated=0;var primary=new Source("SDL 3",()=>[]);using var router=new GamepadRouter(()=>primary,()=>{fallbackCreated++;return new Source("XInput",()=>[new(0,0x1000,0,0)]);});
        Assert.Empty(router.Read());Assert.Empty(router.Read());Assert.Equal("SDL 3",router.Name);Assert.Equal(0,fallbackCreated);Assert.Null(router.Warning);
    }
    [Fact] public void MissingRuntimeFallsBackOnceAndKeepsXboxInput()
    {
        var attempts=0;var fallback=new Source("XInput",()=>[new(2,0x1000,0,0)]);using var router=new GamepadRouter(()=>{attempts++;throw new DllNotFoundException("missing SDL");},()=>fallback);
        Assert.Equal(2,Assert.Single(router.Read()).Index);Assert.Equal(2,Assert.Single(router.Read()).Index);Assert.Equal(1,attempts);Assert.Equal("XInput",router.Name);Assert.Contains("missing SDL",router.Warning);
    }
    [Fact] public void RuntimeFailureDisposesSdlAndNeverMergesItWithXInput()
    {
        var primary=new Source("SDL 3",()=>throw new InvalidOperationException("device read failed"));var fallback=new Source("XInput",()=>[new(1,0,0,0)]);
        using var router=new GamepadRouter(()=>primary,()=>fallback);Assert.Single(router.Read());Assert.Single(router.Read());Assert.Equal(1,primary.Reads);Assert.Equal(1,primary.Disposals);Assert.Equal(2,fallback.Reads);Assert.Equal("XInput",router.Name);
    }
    [Fact] public void SdlIdentifiersNeverBecomeEmulatorXInputPlayerIndices()
    {
        Assert.Equal(0,Gamepad.EmulatorIndex([new(650,0,0,0,Backend:"SDL 3")]));Assert.Equal(3,Gamepad.EmulatorIndex([new(650,0,0,0,Backend:"SDL 3"),new(3,0,0,0)]));Assert.Equal(0,Gamepad.EmulatorIndex([new(-1,0,0,0),new(9,0,0,0)]));
    }
    [Theory][InlineData(-32768,32767)][InlineData(0,0)][InlineData(32767,-32767)]
    public void SdlVerticalAxisPreservesDirectionWithoutOverflow(short input,short expected)=>Assert.Equal(expected,SdlGamepads.InvertY(input));
    [Theory][InlineData(-32768,0)][InlineData(0,0)][InlineData(16384,128)][InlineData(32767,255)]
    public void TriggerConversionClampsAndPreservesFullRange(short input,byte expected)=>Assert.Equal(expected,SdlGamepads.Trigger(input));
    [Fact] public void AlteredControllerComponentsAreRejectedBeforeLoading()
    {
        using var dir=new TestDirectory();var file=dir.File("gamecontrollerdb.txt","altered database");Assert.Throws<InvalidOperationException>(()=>SdlNative.Verify(file,SdlNative.DatabaseSha));
    }
    [StructLayout(LayoutKind.Sequential)] private struct VirtualDescription
    {
        public uint Version;public ushort Type,Padding,Vendor,Product,Axes,Buttons,Balls,Hats,Touchpads,Sensors,Padding1,Padding2;public uint ButtonMask,AxisMask;
        public IntPtr Name,TouchpadDescriptions,SensorDescriptions,Userdata,Update,SetPlayerIndex,Rumble,RumbleTriggers,SetLed,SendEffect,SetSensorsEnabled,Cleanup;
    }
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] private static extern uint SDL_AttachVirtualJoystick(ref VirtualDescription description);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] private static extern bool SDL_DetachVirtualJoystick(uint id);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] private static extern IntPtr SDL_OpenJoystick(uint id);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] private static extern void SDL_CloseJoystick(IntPtr joystick);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] private static extern bool SDL_SetJoystickVirtualButton(IntPtr joystick,int button,[MarshalAs(UnmanagedType.I1)]bool down);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] private static extern bool SDL_SetJoystickVirtualAxis(IntPtr joystick,int axis,short value);
    [AvaloniaFact] public void RealSdlRuntimeReadsVirtualGamepadAndReconnectsWithoutPhantomActions()
    {
        using var source=new SdlGamepads();var name=Marshal.StringToCoTaskMemUTF8("EmuVora virtual controller");uint id=0;IntPtr joystick=IntPtr.Zero;
        try
        {
            Assert.Equal(136,Marshal.SizeOf<VirtualDescription>());
            var desc=new VirtualDescription{Version=136,Type=1,Axes=6,Buttons=15,ButtonMask=0x7fff,AxisMask=0x3f,Name=name};
            id=SDL_AttachVirtualJoystick(ref desc);Assert.NotEqual(0u,id);joystick=SDL_OpenJoystick(id);Assert.NotEqual(IntPtr.Zero,joystick);
            Assert.True(SDL_SetJoystickVirtualAxis(joystick,4,short.MinValue));Assert.True(SDL_SetJoystickVirtualAxis(joystick,5,short.MinValue));
            var pad=Assert.Single(source.Read(),p=>p.Name=="EmuVora virtual controller");Assert.Equal("SDL 3",pad.Backend);Assert.Equal(0,pad.Buttons);var navigation=new GamepadNavigation();navigation.Poll([pad],true,0);
            Assert.True(SDL_SetJoystickVirtualButton(joystick,0,true));Assert.True(SDL_SetJoystickVirtualAxis(joystick,1,short.MinValue));Assert.True(SDL_SetJoystickVirtualAxis(joystick,4,short.MaxValue));
            pad=Assert.Single(source.Read(),p=>p.Name=="EmuVora virtual controller");Assert.Equal(0x1000,pad.Buttons);Assert.Equal(short.MaxValue,pad.Y);Assert.Equal(255,pad.LeftTrigger);Assert.Equal(0,pad.RightTrigger);Assert.Equal(0x1001,navigation.Poll([pad],true,100));
            var oldIndex=pad.Index;SDL_CloseJoystick(joystick);joystick=IntPtr.Zero;Assert.True(SDL_DetachVirtualJoystick(id));id=0;
            Assert.DoesNotContain(source.Read(),p=>p.Index==oldIndex);navigation.Poll([],true,200);
            id=SDL_AttachVirtualJoystick(ref desc);Assert.NotEqual(0u,id);joystick=SDL_OpenJoystick(id);Assert.True(SDL_SetJoystickVirtualButton(joystick,0,true));
            pad=Assert.Single(source.Read(),p=>p.Name=="EmuVora virtual controller");Assert.NotEqual(oldIndex,pad.Index);Assert.Equal(0,navigation.Poll([pad],true,300));
            Assert.True(SDL_SetJoystickVirtualButton(joystick,0,false));navigation.Poll([Assert.Single(source.Read(),p=>p.Name=="EmuVora virtual controller")],true,400);
            Assert.True(SDL_SetJoystickVirtualButton(joystick,0,true));Assert.Equal(0x1000,navigation.Poll([Assert.Single(source.Read(),p=>p.Name=="EmuVora virtual controller")],true,500));
        }
        finally{if(joystick!=IntPtr.Zero)SDL_CloseJoystick(joystick);if(id!=0)SDL_DetachVirtualJoystick(id);Marshal.FreeCoTaskMem(name);}
    }
    [AvaloniaFact] public void SdlDiagnosticsShowsNameConnectionAndNormalizedButtons()
    {
        Gamepad.Snapshot[] pads=[new(7,0x1000,123,456,255,0,Name:"Demo DualSense",Connection:"Drahtlos",Backend:"SDL 3")];var window=new ControllerDiagnosticsWindow(()=>pads);window.Show();window.UpdateSnapshots(pads,0);
        window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var text=string.Join("\n",window.GetVisualDescendants().OfType<TextBlock>().Where(t=>t.IsEffectivelyVisible).Select(t=>t.Text));Assert.Contains("Demo DualSense · Drahtlos",text);Assert.Contains("LT 255/255",text);Assert.DoesNotContain("Slot 1 · nicht verbunden",text);
        using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);var folder=Environment.GetEnvironmentVariable("EMUVORA_SCREENSHOTS");if(folder!=null){Directory.CreateDirectory(folder);frame.Save(Path.Combine(folder,"SDL-controller-diagnostics.png"));}window.Close();
    }
}
