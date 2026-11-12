using System;
using System.Collections.Generic;
using EmilyIsAway3Access.Speech;
using HarmonyLib;
using UnityEngine;

namespace EmilyIsAway3Access
{
    // What the mod knows about one chat window in the chat bar.
    internal class Chat : Typer
    {
        public FCChatBoxBehavior Box;
        public readonly List<string> Lines = new List<string>();
        public int ReviewAt = -1;
        public string[] Choices;              // the reply choices on screen, or null
        public readonly List<FCMessageBehavior> LinkLines = new List<FCMessageBehavior>();
        public int LinkPick = -1;

        // Typing watch
        public string TypingLabel = "";
        public float TypingSaid = -10f;
        public string Instructions = "";

        public string Name => Box.Character == FBCharacter.None ? "chat" : FacebookValues.GetFullName(Box.Character);
        public string First => FacebookValues.GetFirstName(Box.Character);
        public bool Open => Box.IsOpened;
        public bool Writing => Box.IsWriting;
        public bool ReadyToSend => Box.IsWriting && Box.chatText.chatIcon.sprite == Box.chatText.chatOn;
    }

    // The chat bar's chat windows: new messages, reply choices, your reply as it's typed,
    // "is typing...", who's online. Keys:
    //   Page Up / Page Down   previous / next line of the open chat (Ctrl+Home / Ctrl+End: first / last)
    //   C                     repeat the reply choices
    //   W                     which chats are open, and which are waiting for a reply
    //   L / Shift+L           open the latest link in the chat / pick an older one
    //   I                     open the profile of the person you're chatting with
    // Letter keys need Ctrl while a reply is being typed.
    internal class ChatState : MonoBehaviour
    {
        private static readonly Dictionary<FCChatBoxBehavior, Chat> _chats = new Dictionary<FCChatBoxBehavior, Chat>();

        public static Chat For(FCChatBoxBehavior box)
        {
            Chat c;
            if (!_chats.TryGetValue(box, out c)) { c = new Chat { Box = box }; _chats[box] = c; }
            return c;
        }

        public static IEnumerable<Chat> All()
        {
            FCChatBarManager bar = FCChatBarManager.Instance;
            if (bar == null) yield break;
            foreach (FCChatBoxBehavior b in new[] { bar.chatTab1, bar.chatTab2, bar.chatTab3 })
                if (b != null && b.Character != FBCharacter.None) yield return For(b);
        }

        // The chat window that's open, if any.
        public static Chat Showing()
        {
            foreach (Chat c in All()) if (c.Open) return c;
            return null;
        }

        // Every key types the next letter: a reply is being typed in the open chat, or a post on the page.
        public static bool TypingReply()
        {
            Chat c = Showing();
            return (c != null && c.Writing && FBPopupManager.Instance != null && !FBPopupManager.Instance.IsShowing) || Posts.Writing();
        }

        private static Chat _lastViewed;
        public static Chat Viewed()
        {
            Chat c = Showing();
            if (c != null) _lastViewed = c;
            if (_lastViewed != null && _lastViewed.Box == null) _lastViewed = null;
            return _lastViewed;
        }

        private void Awake()
        {
            Hooks.TypingReplyFn = TypingReply;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { _chats.Clear(); _lastViewed = null; };
        }

        private void Update()
        {
            try
            {
                foreach (Chat c in All())
                {
                    WatchReply(c);
                    WatchLabel(c);
                    WatchInstructions(c);
                }
                if (!SettingsMenu.HoldsKeys) DoKeys();
            }
            catch (Exception e) { Plugin.Log.LogError("ChatState: " + e); }
        }

        // --- Keys ---

        private static void DoKeys()
        {
            // Tab switches chats in the game, but only while one is open. With none open, the
            // mod opens the one waiting for a reply (or the last one used).
            if (Input.GetKeyDown(KeyCode.Tab) && Showing() == null && !Keys.Ctrl() && Reader.FocusedBox() == null)
            {
                Chat pick = null;
                foreach (Chat c in All()) if (c.Choices != null) { pick = c; break; }
                if (pick == null) pick = Viewed();
                if (pick == null) foreach (Chat c in All()) { pick = c; break; }
                if (pick != null && FCChatBarManager.Instance.CanOpenChatWindow(pick.Box.Character)) { FCChatBarManager.Instance.OpenChatWindow(pick.Box); Keys.UseKey(); return; }
                if (pick == null) { Tell.Now("No chats yet."); Keys.UseKey(); return; }
            }
            bool used = true;
            if (Input.GetKeyDown(KeyCode.PageUp)) Review(-1);
            else if (Input.GetKeyDown(KeyCode.PageDown)) Review(1);
            else if (Input.GetKeyDown(KeyCode.End) && Keys.Ctrl() && Viewed() != null) Review(int.MaxValue);
            else if (Input.GetKeyDown(KeyCode.Home) && Keys.Ctrl() && Viewed() != null) Review(int.MinValue);
            else used = false;
            if (used) { Keys.UseKey(); return; }

            if (Keys.Typing() && !Keys.Ctrl()) return;
            used = true;
            if (Input.GetKeyDown(KeyCode.C)) SayChoices();
            else if (Input.GetKeyDown(KeyCode.W)) SayWhere();
            else if (Input.GetKeyDown(KeyCode.L) && Keys.Shift()) PickOlderLink();
            else if (Input.GetKeyDown(KeyCode.L)) OpenLink();
            else if (Input.GetKeyDown(KeyCode.I)) OpenProfile();
            else used = false;
            if (used) Keys.UseKey();
        }

        private static void Review(int by)
        {
            Chat chat = Viewed();
            if (chat == null || chat.Lines.Count == 0) { Tell.Now("No chat lines yet."); return; }
            int n = chat.Lines.Count;
            int i;
            if (by == int.MaxValue) i = n - 1;
            else if (by == int.MinValue) i = 0;
            else i = (chat.ReviewAt < 0 ? n : chat.ReviewAt) + by;
            string edge = "";
            if (i < 0) { i = 0; edge = "Top. "; }
            if (i >= n) { i = n - 1; edge = "Bottom. "; }
            chat.ReviewAt = i;
            Tell.Now(edge + chat.Lines[i]);
        }

        public static void SayChoices()
        {
            Chat c = Showing();
            if (c != null && c.Choices != null) { Tell.Now(ChoiceText(c)); return; }
            if (Posts.LastChoices != null && (c == null || !c.Writing)) { Tell.Now(Posts.LastChoices); return; }
            foreach (Chat o in All())
                if (o.Choices != null) { Tell.Now(o.Name + " is waiting for your reply. Press Tab to switch chats."); return; }
            Tell.Now(c != null && c.Writing ? (c.ReadyToSend ? "Ready to send: " + c.Shown + ". Press Enter." : "Typing: " + c.Shown) : "No choices right now.");
        }

        private static string ChoiceText(Chat c)
        {
            return "Choices: " + string.Join(" ", Array.ConvertAll(c.Choices, Tell.Stop));
        }

        public static void SayWhere()
        {
            var parts = new List<string>();
            Chat on = Showing();
            foreach (Chat c in All())
            {
                string s = c.Name;
                if (c == on) s += ", open";
                if (c.Choices != null) s += ", waiting for your reply";
                else if (c.Writing) s += ", typing your reply";
                if (c.Box.IsNotifying && c.Choices == null) s += ", new messages";
                parts.Add(s);
            }
            Tell.Now(parts.Count == 0 ? "No chats." : "Chats: " + string.Join(". ", parts.ToArray()) + ".");
        }

        private static void OpenLink()
        {
            Chat c = Viewed();
            if (c == null) { Tell.Now("No chat."); return; }
            c.LinkLines.RemoveAll(l => l == null);
            if (c.LinkLines.Count == 0) { Tell.Now("No links in this chat."); return; }
            int i = c.LinkPick < 0 || c.LinkPick >= c.LinkLines.Count ? c.LinkLines.Count - 1 : c.LinkPick;
            c.LinkPick = -1;
            MessageLinkButton link = c.LinkLines[i].message.GetComponent<MessageLinkButton>();
            if (link == null) return;
            Tell.Now((Links.IsExternal(link) ? "Opening in your web browser: " : "Opening: ") + Links.Text(link));
            Links.Press(link);
        }

        private static void PickOlderLink()
        {
            Chat c = Viewed();
            if (c == null) { Tell.Now("No chat."); return; }
            c.LinkLines.RemoveAll(l => l == null);
            if (c.LinkLines.Count == 0) { Tell.Now("No links in this chat."); return; }
            c.LinkPick = c.LinkPick <= 0 ? c.LinkLines.Count - 1 : c.LinkPick - 1;
            Tell.Now("Link " + (c.LinkPick + 1) + " of " + c.LinkLines.Count + ": " + Ui.Text(c.LinkLines[c.LinkPick].message) + ". Press L to open.");
        }

        private static void OpenProfile()
        {
            Chat c = Viewed();
            if (c == null) { Tell.Now("No chat."); return; }
            if (FFApplicationManager.Instance == null || FFApplicationManager.Instance.webpage.IsLoading) return;
            c.Box.DidPressFriendButton();
        }

        // --- Watching ---

        private static void WatchReply(Chat c)
        {
            string now = c.Box.chatText != null && c.Writing ? c.Box.chatText.ContentText : "";
            c.Watch(now, c.ReadyToSend, "send");
        }
        // "Emily is typing...", "Emily is deleting..."
        private static void WatchLabel(Chat c)
        {
            string now = c.Box.chatTyping.alpha > 0.5f ? Ui.Text(c.Box.typingText) : "";
            if (now == c.TypingLabel) return;
            c.TypingLabel = now;
            if (now.Length == 0 || !Settings.BuddyTyping.On) return;
            if (!c.Open && !Settings.OtherChats.On) return;
            if (now.EndsWith("typing...") && Time.unscaledTime - c.TypingSaid < 1.5f) return;   // no stutter
            c.TypingSaid = Time.unscaledTime;
            Tell.Queue(now);
        }

        // The game's tips ("Press 1, 2 or 3...", "Press Tab to switch...").
        private static void WatchInstructions(Chat c)
        {
            string now = c.Box.chatInstructs.alpha > 0.9f && c.Open ? Ui.Text(c.Box.instructText) : "";
            if (now == c.Instructions) return;
            c.Instructions = now;
            if (now.Length > 0) Tell.Queue("Tip: " + now);
        }

        // --- Called by the patches ---

        public static void OnMessage(FCChatBoxBehavior box, string text, FBCharacter who, bool quiet)
        {
            Chat c = For(box);
            string name = who == FBCharacter.Player
                ? (BaseMasterScriptBehavior.Instance != null && BaseMasterScriptBehavior.Instance.IsUpsideDown ? FacebookValues.GetFirstName(FBCharacter.NewMat) : "You")
                : FacebookValues.GetFirstName(who);
            string line = name + ": " + Tell.Plain(text);
            c.Lines.Add(line);
            c.ReviewAt = -1;
            // The new message is the last one in the window (only the spacer comes after it).
            Transform parent = box.messageWindow.chatParent;
            FCMessageBehavior last = null;
            for (int i = parent.childCount - 1; i >= 0 && last == null; i--) last = parent.GetChild(i).GetComponent<FCMessageBehavior>();
            if (last != null && last.message.GetComponent<MessageLinkButton>() != null) c.LinkLines.Add(last);
            if (quiet) return;
            if (who == FBCharacter.Player) { Tell.Queue(Settings.OwnSent.On ? "Sent: " + Tell.Plain(text) : "Sent."); return; }
            if (c.Open) Tell.Queue(line);
            else if (Settings.OtherChats.On) Tell.Queue("In " + c.Name + "'s chat, " + line);
            else Tell.Queue("New message from " + c.Name + ".");
        }

        public static void OnChoices(FCChatBoxBehavior box, string a, string b, string cc)
        {
            Chat c = For(box);
            c.Choices = new[] { Tell.Plain(a), Tell.Plain(b), Tell.Plain(cc) };
            if (c.Open) Tell.Queue(ChoiceText(c));
            else Tell.Queue(c.Name + " is waiting for your reply. Press Tab to switch chats.");
        }

        public static void OnChose(FCChatBoxBehavior box) { For(box).Choices = null; }

        public static void OnOpened(FCChatBoxBehavior box)
        {
            Chat c = For(box);
            _lastViewed = c;
            string s = "Chat with " + c.Name + ".";
            if (c.Choices != null) s += " " + ChoiceText(c);
            else if (c.Writing) s += c.ReadyToSend ? " Ready to send: " + c.Shown + "." : " Typing your reply.";
            else if (c.Lines.Count > 0) s += " Last: " + c.Lines[c.Lines.Count - 1];
            Tell.Now(s);
        }
    }

    [HarmonyPatch(typeof(FCChatBoxBehavior), nameof(FCChatBoxBehavior.SendChatMessage))]
    internal static class ChatMessage
    {
        private static void Postfix(FCChatBoxBehavior __instance, string text, FBCharacter fbChar)
        {
            try { ChatState.OnMessage(__instance, text, fbChar, false); } catch (Exception e) { Plugin.Log.LogError("ChatMessage: " + e); }
        }
    }

    // A saved chat put back on screen: remembered for review, not read out.
    [HarmonyPatch(typeof(FCChatBoxBehavior), nameof(FCChatBoxBehavior.RehydrateChatMessages))]
    internal static class ChatRestored
    {
        private static void Postfix(FCChatBoxBehavior __instance, JSONChatBox jsonChat)
        {
            if (jsonChat == null) return;
            Chat c = ChatState.For(__instance);
            foreach (JSONChatMessage m in jsonChat.chat)
                c.Lines.Add((m.c == FBCharacter.Player ? "You" : FacebookValues.GetFirstName(m.c)) + ": " + Tell.Plain(m.t));
        }
    }

    [HarmonyPatch(typeof(FCChatButtonsBehavior), nameof(FCChatButtonsBehavior.ShowChoices))]
    internal static class ChatChoices
    {
        private static void Postfix(FCChatButtonsBehavior __instance, string str1, string str2, string str3)
        {
            ChatState.OnChoices(__instance.chatBox, str1, str2, str3);
        }
    }

    [HarmonyPatch(typeof(FCChatButtonsBehavior), nameof(FCChatButtonsBehavior.HideChoices))]
    internal static class ChatChose
    {
        private static void Postfix(FCChatButtonsBehavior __instance) { ChatState.OnChose(__instance.chatBox); }
    }

    [HarmonyPatch(typeof(FCChatBoxBehavior), nameof(FCChatBoxBehavior.OpenChatWindow))]
    internal static class ChatOpened
    {
        private static void Prefix(FCChatBoxBehavior __instance, out bool __state) { __state = __instance.IsOpened; }

        private static void Postfix(FCChatBoxBehavior __instance, bool __state)
        {
            if (!__state) ChatState.OnOpened(__instance);
        }
    }

    // Friends coming online or going offline.
    [HarmonyPatch(typeof(FCChatBarManager), nameof(FCChatBarManager.UpdateFriend))]
    internal static class FriendStatus
    {
        private static void Prefix(FCChatBarManager __instance, FBCharacter fbChar, out FBOnline __state)
        {
            __state = __instance.friendsTab.HasFriend(fbChar) ? __instance.friendsTab.GetFriendStatus(fbChar) : (FBOnline)(-1);
        }

        private static void Postfix(FBCharacter fbChar, FBOnline status, FBOnline __state)
        {
            if ((int)__state == -1 || __state == status || !Settings.Statuses.On) return;
            Tell.Queue(FacebookValues.GetFullName(fbChar) + (status == FBOnline.Online ? " is online." : " went offline."));
        }
    }

    [HarmonyPatch(typeof(FCChatBarManager), nameof(FCChatBarManager.AddFriend))]
    internal static class FriendAdded
    {
        private static void Prefix(FCChatBarManager __instance, FBCharacter fbChar, out bool __state) { __state = __instance.friendsTab.HasFriend(fbChar); }

        private static void Postfix(FBCharacter fbChar, bool __state)
        {
            if (!__state) Tell.Queue(FacebookValues.GetFullName(fbChar) + " is now your friend.");
        }
    }
}
