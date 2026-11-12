using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EmilyIsAway3Access.Speech
{
    // The one way the mod speaks. Every line is also written to BepInEx\LogOutput.log as
    // "SAY:" (or "SAY!:" when it interrupted), which the test runs read back.
    internal static class Tell
    {
        public static string Last { get; private set; } = "";
        public static int Count { get; private set; }
        public static readonly List<string> Recent = new List<string>();

        // Cuts off whatever the screen reader is saying. For things the player just did.
        public static void Now(string text) { Out(text, true); }

        // Waits its turn. For things that happen by themselves (messages, statuses).
        public static void Queue(string text) { Out(text, false); }

        private static float _lastAt = -10f;

        private static void Out(string text, bool interrupt)
        {
            text = Plain(text);
            if (text.Length == 0) return;
            // The same longer line twice in a row (two watchers noticing one change): once.
            if (!interrupt && text.Length > 15 && text == Last && UnityEngine.Time.unscaledTime - _lastAt < 1.5f) return;
            _lastAt = UnityEngine.Time.unscaledTime;
            Last = text;
            Count++;
            Recent.Add(text);
            if (Recent.Count > 300) Recent.RemoveAt(0);
            Plugin.Log.LogInfo((interrupt ? "SAY!: " : "SAY: ") + text);
            if (!Settings.Muted) PrismOutput.Send(text, interrupt);
        }

        // TextMesh Pro tags the game uses (<color=#..>, <link>, <u>, ...). Listed by name so
        // chat text like "i <3 you" is left alone.
        private static readonly Regex Tags = new Regex(
            @"</?(color|link|u|b|i|s|size|sprite|font|mark|sup|sub|alpha|align|nobr|noparse|indent|line-height|voffset|cspace|mspace|pos|style|lowercase|uppercase|smallcaps|width|margin|rotate|br)(=[^<>]*)?\s*/?>|<#[0-9a-fA-F]{3,8}>",
            RegexOptions.IgnoreCase);
        private static readonly Regex Space = new Regex(@"\s+");

        // Emotes in TextMesh Pro text (<sprite=3>): the game that uses them names them here.
        public static Func<int, string> SpriteName;
        private static readonly Regex Sprite = new Regex(@"<sprite=(\d+)\s*/?>", RegexOptions.IgnoreCase);

        public static string Plain(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (SpriteName != null)
                text = Sprite.Replace(text, m => " " + SpriteName(int.Parse(m.Groups[1].Value)) + " ");
            text = Tags.Replace(text, "");
            return Space.Replace(text, " ").Trim();
        }

        // "hello" -> "hello.", leaves "hello?" alone.
        public static string Stop(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return ".!?:;)".IndexOf(text[text.Length - 1]) >= 0 ? text : text + ".";
        }
    }
}
