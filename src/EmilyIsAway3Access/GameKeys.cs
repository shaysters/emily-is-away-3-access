using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EmilyIsAway3Access
{
    // When a reply or post is being typed, the game counts any key at all as "type the next
    // letter", including keys that belong to the screen reader or Windows (Insert, Caps Lock,
    // Shift, Ctrl, Alt, the Windows key...). In NVDA's sleep mode those reach the game, so
    // screen reader commands would type letters. Only real typing keys count here.
    [HarmonyPatch]
    internal static class TypingKeys
    {
        private static readonly HashSet<KeyCode> NotTyping = new HashSet<KeyCode>
        {
            KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
            KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.AltGr,
            KeyCode.LeftWindows, KeyCode.RightWindows, KeyCode.LeftCommand, KeyCode.RightCommand,
            KeyCode.Insert, KeyCode.CapsLock, KeyCode.Numlock, KeyCode.ScrollLock,
            KeyCode.Print, KeyCode.SysReq, KeyCode.Break, KeyCode.Pause, KeyCode.Menu, KeyCode.Help,
            KeyCode.Escape, KeyCode.Tab, KeyCode.PageUp, KeyCode.PageDown,
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8,
            KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14, KeyCode.F15,
        };

        // Every keyboard key that counts as typing (mouse and joystick buttons excluded).
        private static readonly List<KeyCode> Typing = Build();

        private static List<KeyCode> Build()
        {
            var list = new List<KeyCode>();
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                if (k != KeyCode.None && (int)k < (int)KeyCode.Mouse0 && !NotTyping.Contains(k) && !list.Contains(k)) list.Add(k);
            return list;
        }

        public static bool AnyTypingKey(bool down)
        {
            foreach (KeyCode k in Typing)
                if (down ? Input.GetKeyDown(k) : Input.GetKey(k)) return true;
            return false;
        }

        [HarmonyPatch(typeof(InputValues), nameof(InputValues.GetKeysDown))]
        [HarmonyPostfix]
        private static void KeysDown(ref int __result)
        {
            if (__result > 0 && (GameKeysHeld.Hold() || !AnyTypingKey(true))) __result = 0;
        }

        [HarmonyPatch(typeof(InputValues), nameof(InputValues.GetKeyDown))]
        [HarmonyPostfix]
        private static void KeyDown(ref bool __result)
        {
            if (__result && (GameKeysHeld.Hold() || !AnyTypingKey(true))) __result = false;
        }

        [HarmonyPatch(typeof(InputValues), nameof(InputValues.GetKey))]
        [HarmonyPostfix]
        private static void KeyHeld(ref bool __result)
        {
            if (__result && !AnyTypingKey(false)) __result = false;
        }
    }

    // When the mod used a key (or its settings list is open), the game's own key listeners
    // skip that frame, so one key press doesn't do two things: reply choices 1-3, Tab between
    // chats, Enter and arrows in pop-ups, Enter on forms, and typing replies and posts.
    [HarmonyPatch]
    internal static class GameKeysHeld
    {
        public static bool Hold()
        {
            return SettingsMenu.HoldsKeys || Keys.ModUsedKeyThisFrame || (Keys.Ctrl() && !Keys.Shift());
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(FCChatBoxBehavior), nameof(FCChatBoxBehavior.WriteText)));
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(FCChatButtonsBehavior), "ChoiceRunner"));
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(FCChatBarManager), "CycleChatRunner"));
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(FBPopupManager), "DefaultEnterRunner"));
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(FBSignUpWebpage), "KeyboardListener"));
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(FBPostBoxBehavior), nameof(FBPostBoxBehavior.WriteTextRunner)));
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(FBPostButtonsBehavior), "ChoiceRunner"));
        }

        private static bool Prefix(ref bool __result)
        {
            if (!Hold()) return true;
            __result = true;   // still running; it tries again next frame
            return false;
        }
    }
}
