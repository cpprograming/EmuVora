using System.Runtime.InteropServices;
using System.Security.Cryptography;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("EmuVora.Tests")]

namespace EmuVora.App;

internal static class SdlNative
{
    internal const string DllSha="2b4b128d8d510cc2d442693cf848c050d700d43dc667afe9e5fd82b0570d667e";
    internal const string DatabaseSha="c6587cc65789539e61ec375aad5bdbc14f974508b57e2ae87730ac75c4445dba";
    private static readonly object Gate=new();
    private static IntPtr library;
    internal static void EnsureLoaded()
    {
        lock(Gate)
        {
            if(library!=IntPtr.Zero)return;
            var path=Path.Combine(AppContext.BaseDirectory,"SDL3.dll");Verify(path,DllSha);
            var handle=NativeLibrary.Load(path);
            try{NativeLibrary.SetDllImportResolver(typeof(SdlNative).Assembly,(name,_,_)=>name=="SDL3.dll"?handle:IntPtr.Zero);library=handle;}
            catch{NativeLibrary.Free(handle);throw;}
        }
    }
    internal static void Verify(string path,string sha)
    {
        using var file=File.OpenRead(path);
        if(!Convert.ToHexString(SHA256.HashData(file)).Equals(sha,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Controller-Komponente ist beschädigt: "+Path.GetFileName(path));
    }
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_SetMainReady();
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] internal static extern bool SDL_InitSubSystem(uint flags);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_QuitSubSystem(uint flags);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] internal static extern bool SDL_SetHint([MarshalAs(UnmanagedType.LPUTF8Str)]string name,[MarshalAs(UnmanagedType.LPUTF8Str)]string value);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr SDL_GetError();
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern int SDL_AddGamepadMappingsFromFile([MarshalAs(UnmanagedType.LPUTF8Str)]string path);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_SetGamepadEventsEnabled([MarshalAs(UnmanagedType.I1)]bool enabled);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_SetJoystickEventsEnabled([MarshalAs(UnmanagedType.I1)]bool enabled);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_PumpEvents();
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_UpdateGamepads();
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr SDL_GetGamepads(out int count);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_free(IntPtr memory);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr SDL_OpenGamepad(uint id);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern void SDL_CloseGamepad(IntPtr pad);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] internal static extern bool SDL_GamepadConnected(IntPtr pad);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] internal static extern bool SDL_GetGamepadButton(IntPtr pad,int button);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern short SDL_GetGamepadAxis(IntPtr pad,int axis);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr SDL_GetGamepadName(IntPtr pad);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern int SDL_GetGamepadConnectionState(IntPtr pad);
    internal static string Error()=>Marshal.PtrToStringUTF8(SDL_GetError())??"Unbekannter SDL-Fehler";
}
