using System.IO;
using BepInEx;
using BepInEx.Logging;
using EmilyIsAway3Access.Speech;
using HarmonyLib;

namespace EmilyIsAway3Access
{
    [BepInPlugin(Id, Title, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Id = "shaysters.emilyisaway3.access";
        public const string Title = "Emily is Away 3 Access";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;

        // How the version is spoken: "1.0.0" -> "1.0", "0.1.0" -> "0.1", "1.2.3" unchanged.
        public static string SpokenVersion
        {
            get
            {
                string[] parts = Version.Split('.');
                return parts.Length == 3 && parts[2] == "0" ? parts[0] + "." + parts[1] : Version;
            }
        }

        private void Awake()
        {
            Log = Logger;
            PrismOutput.Start(Path.GetDirectoryName(typeof(Plugin).Assembly.Location));
            Settings.Load();
#if DEV_TOOLS
            // Before the game's own patches: the test sandbox must be in place first.
            if (gameObject.AddComponent<TestDriver>() == null) Log.LogError("TestDriver could not be added.");
#endif
            new Harmony(Id).PatchAll(typeof(Plugin).Assembly);
            Hooks.TextEntryActiveFn = () => Reader.FocusedBox() != null;
            gameObject.AddComponent<SettingsMenu>();
            gameObject.AddComponent<Keys>();
            gameObject.AddComponent<ChatState>();
            gameObject.AddComponent<Posts>();
            gameObject.AddComponent<Reader>();
            gameObject.AddComponent<Screens>();
            gameObject.AddComponent<UpdateCheck>();
            Log.LogInfo(Title + " " + Version + " loaded.");
            Tell.Queue(Title + " " + SpokenVersion + " loaded.");
        }
    }
}
