using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EmilyIsAway3Access.Speech
{
    // Talks to Prism (https://github.com/ethindp/prism), which sends text to the running
    // screen reader (NVDA, JAWS, Narrator, ...) and its braille display, or to Windows
    // speech when no screen reader is running.
    //
    // The game is 32-bit Unity 2018 on the old Mono runtime. That Mono can't [DllImport]
    // a DLL from a plugin folder by name, so the 32-bit prism.dll is loaded by its full
    // path with LoadLibrary and each C function is looked up with GetProcAddress.
    internal static class PrismOutput
    {
        private const int Ok = 0;
        private const ulong FeatureOutput = 1UL << 5;   // speech + braille in one call
        private const ulong FeatureStop = 1UL << 7;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string fileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);

        // Shapes of the Prism functions the mod uses (all cdecl).
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Fn_Ptr_Ptr(IntPtr a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong Fn_U64_Ptr(IntPtr a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Fn_Int_Ptr(IntPtr a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Fn_Text(IntPtr backend, byte[] utf8, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        private sealed class Api
        {
            public Fn_Ptr_Ptr Init, AcquireBest, BackendName;
            public Fn_U64_Ptr Features;
            public Fn_Text Speak, Output;
            public Fn_Int_Ptr Stop;
        }

        private static Api _api;
        private static IntPtr _backend;
        private static ulong _features;

        public static bool Ready => _api != null && _backend != IntPtr.Zero;
        public static string Backend { get; private set; } = "none";

        public static void Start(string pluginFolder)
        {
            string dll = Path.Combine(pluginFolder, "prism.dll");
            try
            {
                IntPtr lib = LoadLibraryW(dll);
                if (lib == IntPtr.Zero)
                {
                    Plugin.Log.LogError("Prism: can't load " + dll + " (Windows error " + Marshal.GetLastWin32Error() + ").");
                    return;
                }
                var api = new Api
                {
                    Init = Get<Fn_Ptr_Ptr>(lib, "prism_init"),
                    AcquireBest = Get<Fn_Ptr_Ptr>(lib, "prism_registry_acquire_best"),
                    BackendName = Get<Fn_Ptr_Ptr>(lib, "prism_backend_name"),
                    Features = Get<Fn_U64_Ptr>(lib, "prism_backend_get_features"),
                    Speak = Get<Fn_Text>(lib, "prism_backend_speak"),
                    Output = Get<Fn_Text>(lib, "prism_backend_output"),
                    Stop = Get<Fn_Int_Ptr>(lib, "prism_backend_stop"),
                };
                IntPtr context = api.Init(IntPtr.Zero);
                if (context == IntPtr.Zero) { Plugin.Log.LogError("Prism: prism_init returned nothing."); return; }
                IntPtr backend = api.AcquireBest(context);
                if (backend == IntPtr.Zero) { Plugin.Log.LogError("Prism: no screen reader or speech engine found."); return; }
                _features = api.Features(backend);
                Backend = ReadUtf8(api.BackendName(backend)) ?? "unknown";
                _api = api;
                _backend = backend;
                Plugin.Log.LogInfo("Prism: speaking through " + Backend + ".");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Prism: " + e.GetType().Name + ": " + e.Message);
            }
        }

        public static void Send(string text, bool interrupt)
        {
            if (!Ready || string.IsNullOrEmpty(text)) return;
            try
            {
                byte[] utf8 = WriteUtf8(text);
                int result = (_features & FeatureOutput) != 0
                    ? _api.Output(_backend, utf8, interrupt)
                    : _api.Speak(_backend, utf8, interrupt);
                if (result != Ok) Plugin.Log.LogWarning("Prism: output error " + result + ".");
            }
            catch (Exception e) { Plugin.Log.LogWarning("Prism: output failed: " + e.Message); }
        }

        public static void Silence()
        {
            if (!Ready || (_features & FeatureStop) == 0) return;
            try { _api.Stop(_backend); } catch { }
        }

        private static T Get<T>(IntPtr lib, string name) where T : class
        {
            IntPtr fn = GetProcAddress(lib, name);
            if (fn == IntPtr.Zero) throw new EntryPointNotFoundException("prism.dll has no " + name);
            return Marshal.GetDelegateForFunctionPointer(fn, typeof(T)) as T;
        }

        // Null-terminated UTF-8, as Prism expects.
        private static byte[] WriteUtf8(string text)
        {
            int n = Encoding.UTF8.GetByteCount(text);
            byte[] buffer = new byte[n + 1];
            Encoding.UTF8.GetBytes(text, 0, text.Length, buffer, 0);
            return buffer;
        }

        private static string ReadUtf8(IntPtr p)
        {
            if (p == IntPtr.Zero) return null;
            int len = 0;
            while (Marshal.ReadByte(p, len) != 0) len++;
            byte[] bytes = new byte[len];
            Marshal.Copy(p, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
