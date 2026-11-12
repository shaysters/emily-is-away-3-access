using System;
using System.Collections.Generic;
using EmilyIsAway3Access.Speech;
using HarmonyLib;
using UnityEngine;

namespace EmilyIsAway3Access
{
    // A reply the game types out letter by letter as the player presses keys (chat replies,
    // wall posts, statuses, comments). Reads each finished word, erased text, and the whole
    // message once it's ready.
    internal class Typer
    {
        public string Shown = "";       // text in the box last frame
        public string Erased = "";      // letters erased since the last one typed
        public bool Ready;

        // verb: "send" or "post".
        public void Watch(string now, bool ready, string verb)
        {
            string old = Shown;
            if (now == old && ready == Ready) return;
            Shown = now;

            if (now.Length == 0)   // sent or cleared
            {
                Erased = ""; Ready = ready; return;
            }
            if (now.Length < old.Length && old.StartsWith(now))
            {
                Erased = old.Substring(now.Length) + Erased;   // backspaces go right to left
            }
            else if (now.Length > old.Length && now.StartsWith(old))
            {
                if (Erased.Length > 0)
                {
                    if (Settings.ReadErased.On) Tell.Queue("erased " + Erased.Trim());
                    Erased = "";
                }
                char added = now[now.Length - 1];
                char before = now.Length > 1 ? now[now.Length - 2] : ' ';
                // A word is finished when a space or punctuation follows a letter.
                if (Settings.ReadWords.On && IsBreak(added) && !IsBreak(before))
                {
                    string word = LastWord(now.Substring(0, now.Length - 1));
                    if (word.Length > 0) Tell.Queue(word);
                }
            }

            if (ready && !Ready)
            {
                if (Erased.Length > 0 && Settings.ReadErased.On) Tell.Queue("erased " + Erased.Trim());
                Erased = "";
                if (Settings.ReadWords.On && !Settings.ReadReady.On)
                {
                    string word = LastWord(now);
                    if (word.Length > 0) Tell.Queue(word);
                }
                Tell.Queue(Settings.ReadReady.On ? "Ready to " + verb + ": " + Tell.Stop(now) + " Press Enter." : "Ready to " + verb + ". Press Enter.");
            }
            Ready = ready;
        }

        private static bool IsBreak(char ch) { return ch == ' ' || ch == ',' || ch == '.' || ch == '?' || ch == '!'; }

        private static string LastWord(string s)
        {
            s = s.TrimEnd(' ', ',', '.', '?', '!');
            int sp = s.LastIndexOf(' ');
            return sp < 0 ? s : s.Substring(sp + 1);
        }
    }

    // Post boxes on pages: status updates, wall posts, event and note comments, gift messages.
    // You pick one of three things to say (1, 2, 3), then the game types it as you press keys.
    internal class Posts : MonoBehaviour
    {
        private static readonly Dictionary<FBPostBoxBehavior, Typer> _typers = new Dictionary<FBPostBoxBehavior, Typer>();
        private static readonly List<FBPostBoxBehavior> _boxes = new List<FBPostBoxBehavior>();
        private float _nextFind;

        // A post is being typed: every key types the next letter (unless a chat reply is
        // being typed or a pop-up is open, then the game waits).
        public static bool Writing()
        {
            if (FCChatBarManager.Instance != null && FCChatBarManager.Instance.IsChatWriting) return false;
            if (FBPopupManager.Instance != null && FBPopupManager.Instance.IsShowing) return false;
            foreach (FBPostBoxBehavior b in _boxes) if (b != null && IsWriting(b)) return true;
            return false;
        }

        private static bool IsWriting(FBPostBoxBehavior b)
        {
            return b.isActiveAndEnabled && b.postText.canvas.alpha > 0.5f;
        }

        private void Update()
        {
            try
            {
                if (Time.unscaledTime > _nextFind)
                {
                    _nextFind = Time.unscaledTime + 0.5f;
                    _boxes.Clear();
                    _boxes.AddRange(FindObjectsOfType<FBPostBoxBehavior>());
                }
                foreach (FBPostBoxBehavior b in _boxes)
                {
                    if (b == null) continue;
                    Typer t;
                    if (!_typers.TryGetValue(b, out t)) { t = new Typer(); _typers[b] = t; }
                    bool writing = IsWriting(b);
                    t.Watch(writing ? b.postText.ContentText : "", writing && b.postButton.interactable, "post");
                }
            }
            catch (Exception e) { Plugin.Log.LogError("Posts: " + e); }
        }

        // A post that's typed out and waiting for Enter.
        public static bool AnyReady()
        {
            foreach (FBPostBoxBehavior b in _boxes) if (b != null && IsWriting(b) && b.postButton.interactable) return true;
            return false;
        }

        public static string LastChoices;

        // On a note you're filling in, the post box answers the next question: say it.
        private static string NoteQuestion()
        {
            FBNoteRowList list = UnityEngine.Object.FindObjectOfType<FBNoteRowList>();
            if (list == null || list.responseCanvas == null || list.responseCanvas.alpha < 0.5f) return null;
            var tr = HarmonyLib.Traverse.Create(list);
            var data = tr.Field("data").GetValue<List<FBNoteRowData>>();
            int i = tr.Field("responseIndex").GetValue<int>();
            if (data == null || i < 0 || i >= data.Count) return null;
            return "Question " + (i + 1) + " of " + data.Count + ": " + Tell.Plain(data[i].question) + " Answer " + LastChoices;
        }

        public static void OnChoices(string a, string b, string c)
        {
            LastChoices = "Choices: " + Tell.Stop(Tell.Plain(a)) + " " + Tell.Stop(Tell.Plain(b)) + " " + Tell.Stop(Tell.Plain(c));
            Tell.Queue(NoteQuestion() ?? "Write a post. " + LastChoices);
        }
    }

    [HarmonyPatch(typeof(FBPostButtonsBehavior), nameof(FBPostButtonsBehavior.ShowChoices))]
    internal static class PostChoices
    {
        private static void Postfix(string str1, string str2, string str3) { Posts.OnChoices(str1, str2, str3); }
    }

    // The post box closes when the post is sent (the page may reload right after).
    [HarmonyPatch(typeof(FBPostTextBehavior), nameof(FBPostTextBehavior.HideDisplay))]
    internal static class PostSent
    {
        private static void Prefix(FBPostTextBehavior __instance)
        {
            if (__instance.canvas.alpha > 0.5f) Tell.Queue("Posted.");
        }
    }

    [HarmonyPatch(typeof(FBPostButtonsBehavior), nameof(FBPostButtonsBehavior.HideChoices))]
    internal static class PostChose
    {
        private static void Postfix() { Posts.LastChoices = null; }
    }

    // Every question answered: the note can be posted.
    [HarmonyLib.HarmonyPatch(typeof(FBNotesWebpage), nameof(FBNotesWebpage.ShowPostNote))]
    internal static class NoteReady
    {
        private static void Postfix() { Tell.Queue("Note finished. Press Enter, or the Post Note button, to post it."); }
    }
}
