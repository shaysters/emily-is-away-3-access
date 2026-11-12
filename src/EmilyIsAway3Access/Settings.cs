using System;
using System.Collections.Generic;
using System.IO;
using EmilyIsAway3Access.Speech;
using UnityEngine;

namespace EmilyIsAway3Access
{
    // One mod setting: a name and a list of values to step through.
    internal class Setting
    {
        public readonly string Key;
        public readonly string Name;
        public readonly string[] Values;
        public int Index;

        public Setting(string key, string name, int start, params string[] values)
        {
            Key = key; Name = name; Index = start; Values = values;
        }

        public bool On => Index == 1;   // for on/off settings ("off", "on")
        public string Value => Values[Index];
        public void Step(int by) { Index = (Index + by + Values.Length) % Values.Length; }
    }

    internal static class Settings
    {
        private static readonly string[] OffOn = { "off", "on" };

        public static readonly Setting OtherChats = new Setting("OtherChats", "Read messages in chats you're not looking at", 1, OffOn);
        public static readonly Setting ReadWords = new Setting("ReadWords", "Read each word while typing a reply", 1, OffOn);
        public static readonly Setting ReadErased = new Setting("ReadErased", "Read erased text", 1, OffOn);
        public static readonly Setting ReadReady = new Setting("ReadReady", "Read the finished message before sending", 1, OffOn);
        public static readonly Setting BuddyTyping = new Setting("BuddyTyping", "Read when someone is typing", 1, OffOn);
        public static readonly Setting EchoLetters = new Setting("EchoLetters", "Read each letter you type in text boxes", 1, OffOn);
        public static readonly Setting ReadPages = new Setting("ReadPages", "Read each new page when it finishes loading", 1, OffOn);
        public static readonly Setting Statuses = new Setting("Statuses", "Read when friends come online or go offline", 1, OffOn);
        public static readonly Setting OwnSent = new Setting("OwnSent", "Read your own messages when sent", 0, OffOn);
        public static readonly Setting SecretHints = new Setting("SecretHints", "Secret hints, spoilers", 0, OffOn);
        public static readonly Setting UpdateCheck = new Setting("UpdateCheck", "Announce available updates", 1, OffOn);

        public static readonly List<Setting> All = new List<Setting>
        {
            OtherChats, ReadWords, ReadErased, ReadReady, BuddyTyping, OwnSent, Statuses, EchoLetters, ReadPages, SecretHints, UpdateCheck,
        };

        // Speech off (test runs only; not a menu option).
        public static bool Muted;

        // BepInEx\config\EmilyIsAway3Access-settings.txt: outside the plugin folder, so
        // reinstalling keeps it, and apart from the game's saves, so the game's reset keeps it.
        private static string File_ => Path.Combine(BepInEx.Paths.ConfigPath, "EmilyIsAway3Access-settings.txt");

        public static void Load()
        {
            try
            {
                if (!File.Exists(File_)) return;
                foreach (string line in File.ReadAllLines(File_))
                {
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    Setting s = All.Find(x => x.Key == key);
                    int v;
                    if (s != null && int.TryParse(line.Substring(eq + 1).Trim(), out v) && v >= 0 && v < s.Values.Length) s.Index = v;
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("Settings: can't read " + File_ + ": " + e.Message); }
        }

        public static void Save()
        {
            try
            {
                var lines = new List<string>();
                foreach (Setting s in All) lines.Add(s.Key + "=" + s.Index);
                File.WriteAllLines(File_, lines.ToArray());
            }
            catch (Exception e) { Plugin.Log.LogWarning("Settings: can't write " + File_ + ": " + e.Message); }
        }
    }

    // The settings list, opened with M (Ctrl+M while typing). Up/Down move, Left/Right/Enter/
    // Space change the value, Escape or M closes. The game is paused while it's open (time stops and the
    // game's key listeners skip their turn), so keys only go to the list.
    internal class SettingsMenu : MonoBehaviour
    {
        public static bool Open { get; private set; }
        private static int _closedOn = -1;
        private int _at;
        private float _timeScale = 1f;

        // Also true on the frame it closed, so the closing key does nothing else.
        public static bool HoldsKeys => Open || _closedOn == Time.frameCount;

        private void Update()
        {
            try
            {
                if (!Open)
                {
                    if (Input.GetKeyDown(KeyCode.M) && (Keys.Ctrl() || !Keys.Typing()) && !HoldsKeys) { Keys.UseKey(); Show(); }
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M)) Close();
                else if (Input.GetKeyDown(KeyCode.DownArrow)) Move(1);
                else if (Input.GetKeyDown(KeyCode.UpArrow)) Move(-1);
                else if (Input.GetKeyDown(KeyCode.Home)) { _at = 0; Read(); }
                else if (Input.GetKeyDown(KeyCode.End)) { _at = Settings.All.Count; Read(); }
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) Change(-1);
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Return)
                         || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) Change(1);
            }
            catch (Exception e) { Plugin.Log.LogError("SettingsMenu: " + e); }
        }

        public void Show()
        {
            Open = true;
            _at = 0;
            _timeScale = Time.timeScale;
            Time.timeScale = 0f;
            Tell.Now("Mod settings, game paused. " + Describe(0));
        }

        public void Close()
        {
            Open = false;
            _closedOn = Time.frameCount;
            Time.timeScale = _timeScale;
            Settings.Save();
            Tell.Now("Settings closed.");
        }

        private void Move(int by)
        {
            int n = Settings.All.Count + 1;   // the last item is "Close"
            _at = (_at + by + n) % n;
            Read();
        }

        public void Change(int by)
        {
            if (_at >= Settings.All.Count) { Close(); return; }
            Setting s = Settings.All[_at];
            s.Step(by);
            Settings.Save();
            Tell.Now(s.Value);
        }

        private void Read() { Tell.Now(Describe(_at)); }

        private static string Describe(int i)
        {
            if (i >= Settings.All.Count) return "Close";
            Setting s = Settings.All[i];
            return s.Name + ", " + s.Value;
        }

        // For the test driver.
        public void Pick(int i) { _at = Mathf.Clamp(i, 0, Settings.All.Count); Read(); }
    }
}
