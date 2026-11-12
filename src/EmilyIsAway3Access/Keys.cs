using System;
using EmilyIsAway3Access.Speech;
using UnityEngine;

namespace EmilyIsAway3Access
{
    // Helpers for the mod's keys and the shared key rules. While a reply is being typed (every
    // key types the next letter, that's how the game works) or a text box has the keyboard,
    // the mod's letter keys only work with Ctrl, and a key the mod uses doesn't also reach
    // the game.
    internal class Keys : MonoBehaviour
    {
        private static int _usedFrame = -1;

        // The mod used this frame's key press; the game's key listeners skip the frame.
        public static bool ModUsedKeyThisFrame => _usedFrame == Time.frameCount;
        public static void UseKey() { _usedFrame = Time.frameCount; }

        public static bool Shift() { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }

        public static bool Ctrl() { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl); }

        // A Ctrl key on its own: the game would count it as a typing key press.
        public static bool CtrlDownAlone()
        {
            return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        }

        // Letter keys are text: a text box has the keyboard, or a reply is being typed out.
        public static bool Typing()
        {
            return Hooks.TextEntryActive() || Hooks.TypingReply();
        }

        private void Update()
        {
            try
            {
                if (SettingsMenu.HoldsKeys) return;
                if (Typing() && !Ctrl()) return;
                if (Input.GetKeyDown(KeyCode.R)) { Tell.Now(Tell.Last); UseKey(); }
                else if (Input.GetKeyDown(KeyCode.H)) { Tell.Now(Help); UseKey(); }
            }
            catch (Exception e) { Plugin.Log.LogError("Keys: " + e); }
        }

        public const string Help =
            "Keys. Up and Down arrows: read the page or window one item at a time. Home and End: top and bottom. " +
            "Enter or Space: press a link or button. Left and Right: change a slider. " +
            "1, 2, 3: pick a reply. Then any key types it, and Enter sends it. While a reply or post is being typed, use Ctrl with the mod's keys. " +
            "Tab: switch chats, or open the chat that's waiting. Page Up and Page Down: read the open chat line by line. Ctrl Home and Ctrl End: first and last line. " +
            "C: repeat the reply choices. W: which chats are open and waiting. L: open the latest link in the chat, Shift L: pick an older link. I: open the profile of the person you're chatting with. " +
            "R: repeat the last thing said. M: mod settings. H: this help.";

        public static string SayChar(char c)
        {
            if (c == ' ') return "space";
            if (char.IsUpper(c)) return "cap " + c;
            return c.ToString();
        }
    }

    // Answers other parts of the mod fill in once they exist (so Keys doesn't depend on them).
    internal static class Hooks
    {
        public static Func<bool> TextEntryActiveFn = () => false;
        public static Func<bool> TypingReplyFn = () => false;
        public static bool TextEntryActive() { return TextEntryActiveFn(); }
        public static bool TypingReply() { return TypingReplyFn(); }
    }
}
