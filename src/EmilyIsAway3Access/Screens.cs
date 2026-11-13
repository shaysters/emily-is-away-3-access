using System;
using EmilyIsAway3Access.Speech;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmilyIsAway3Access
{
    // Knows the game's screens: which one is in front (pop-ups, the settings window, the
    // sign-up date lists, the chat bar's lists), what belongs to the page, and what's
    // decoration. Reads new pages, pop-ups and installer steps when they appear.
    internal class Screens : MonoBehaviour
    {
        // --- What the reader lists ---

        public static Transform ModalRoot()
        {
            FBPopupManager pop = FBPopupManager.Instance;
            if (pop != null && pop.IsShowing && pop.popupCanvas != null) return pop.popupCanvas.transform;
            FFSystemPanel sys = FFSystemPanel.Instance;
            if (sys != null && sys.IsShowing) return sys.canvas.transform;
            InstallScreenManager inst = Installer();
            if (inst != null && inst.alertCanvas.alpha > 0.5f) return inst.alertCanvas.transform;
            FBSignUpWebpage signUp = SignUpPage();
            if (signUp != null)
            {
                if (signUp.monthDropdown.canvas.alpha > 0.5f) return signUp.monthDropdown.transform;
                if (signUp.dayDropdown.canvas.alpha > 0.5f) return signUp.dayDropdown.transform;
            }
            FCChatBarManager bar = FCChatBarManager.Instance;
            if (bar != null)
            {
                if (bar.friendsTab.IsOpened) return bar.friendsTab.friendsWindow.transform;
                if (bar.notifactionsTab.IsOpened) return bar.notifactionsTab.notification.transform;
                if (bar.accountTab.IsOpened) return bar.accountTab.accountWindow.transform;
            }
            return null;
        }

        // Left out of the list: chat messages (read with the chat keys instead) and the big
        // date shown between chapters (said when it appears).
        public static bool Skip(Transform t)
        {
            return t.GetComponent<FCMessageWindowBehavior>() != null || t.GetComponent<FFDateTimePanel>() != null;
        }

        // Full-window "click outside to close" backgrounds.
        public static bool SkipControl(Selectable s)
        {
            string n = s.name.ToLowerInvariant();
            if (n.Contains("background") || n.Contains("overlay") || n == "blocker") return true;
            // The quiz's permission list: icons that look like buttons but do nothing.
            return n == "icon" && s.transform.parent != null && s.transform.parent.name.StartsWith("RelatedNode") && s.GetComponentInParent<FBPopupPermissions>() != null;
        }

        public static bool SkipText(Transform t, string text)
        {
            if (text == "|" || text.Trim().Length == 0) return true;
            // The installer's radio button names, already said with the radio buttons.
            if (_installer != null && t.name.StartsWith("SystemText (")) return true;
            // The chat bar's tab names, said with their tab buttons.
            if (t.parent != null && t.parent.Find("TabButton") != null && t.parent.GetComponentInParent<FCChatBarManager>() != null) return true;
            // The photo viewer's caption, said with the photo.
            FBPopupPhoto photo = t.GetComponentInParent<FBPopupPhoto>();
            if (photo != null && t == photo.captionText.transform) return true;
            if (SecretApps.SkipText(t)) return true;
            // A settings row's title, said with its slider.
            if (t.name == "Title" && t.parent != null && RowHasSlider(t.parent)) return true;
            if (QuizResults.Calculating(t)) return true;
            // Quiz answers, said with their radio buttons.
            FBQuizQuestionBox quiz = t.GetComponentInParent<FBQuizQuestionBox>();
            if (quiz != null && quiz.answerTexts != null && System.Array.Exists(quiz.answerTexts, a => a != null && a.transform == t)) return true;
            // A form row's label, already said with its text box.
            return t.name == "FieldText" && t.parent != null && t.parent.GetComponentInChildren<TMP_InputField>() != null;
        }

        private static bool RowHasSlider(Transform row)
        {
            for (int i = 0; i < row.childCount; i++) if (row.GetChild(i).GetComponent<Slider>() != null) return true;
            return false;
        }

        // 0 = the page or window, 1 = browser bar and the rest, 2 = chat bar.
        public static int Group(Transform t)
        {
            FFApplicationManager app = FFApplicationManager.Instance;
            if (app != null)
            {
                if (app.webpage != null && t.IsChildOf(app.webpage.webpageParent)) return 0;
                if (app.addressBar != null && t.IsChildOf(app.addressBar.transform)) return 1;
            }
            FCChatBarManager bar = FCChatBarManager.Instance;
            if (bar != null && t.IsChildOf(bar.transform)) return 2;
            return 0;
        }

        public static InstallScreenManager Installer() { return _installer; }

        private static FBSignUpWebpage SignUpPage()
        {
            FFApplicationManager app = FFApplicationManager.Instance;
            return app == null || app.webpage == null ? null : app.webpage.currentWebpage as FBSignUpWebpage;
        }

        // --- Watching for changes ---

        private static InstallScreenManager _installer;
        private float _nextFind;
        private FBBaseWebpage _page;
        private bool _wasLoading;
        private Transform _modal;
        private int _installStep = -1;
        private string _progress;
        private string _scene;
        private bool _blocking;
        private Component _before;   // what the reader was on when a pop-up opened

        private void Update()
        {
            try
            {
                string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (scene != _scene) { _scene = scene; _installer = null; _page = null; _installStep = -1; OnScene(scene); }
                SecretApps.Watch();
                if (_installer == null && Time.unscaledTime > _nextFind) { _nextFind = Time.unscaledTime + 0.5f; _installer = FindObjectOfType<InstallScreenManager>(); }
                if (_installer != null) WatchInstaller();
                WatchBlocking();
                WatchPage();
                WatchModal();
                WatchFormError();
            }
            catch (Exception e) { Plugin.Log.LogError("Screens: " + e); }
        }

        private static void OnScene(string scene)
        {
            Plugin.Log.LogInfo("Scene " + scene);
            if (scene == "splash") Tell.Queue("Starting up.");
        }

        private void WatchInstaller()
        {
            int step = Traverse.Create(_installer).Field("currentScreen").GetValue<int>();
            if (step != _installStep)
            {
                _installStep = step;
                Reader.Instance.Forget();
                Tell.Now(Reader.Instance.AllText(30));
            }
            string p = Ui.Text(_installer.progressText);
            if (step == 3 && p != _progress && p.Length > 0) Tell.Queue(p);
            _progress = p;
        }

        // The "loading" panel with a message between chapters.
        private void WatchBlocking()
        {
            FFBlockingPanel b = FFBlockingPanel.Instance;
            bool on = b != null && b.messageCanvas != null && b.messageCanvas.alpha > 0.5f && Ui.Text(b.messageText).Length > 0;
            if (on && !_blocking) Tell.Queue(Ui.Text(b.messageText));
            _blocking = on;
        }

        private void WatchPage()
        {
            FFApplicationManager app = FFApplicationManager.Instance;
            if (app == null || app.webpage == null) { _page = null; return; }
            FFWebpageManager w = app.webpage;
            if (w.IsLoading && !_wasLoading) Tell.Now("Loading " + Ui.Text(app.addressBar.addressText));
            _wasLoading = w.IsLoading;
            if (w.IsLoading || w.currentWebpage == null || w.currentWebpage == _page) return;
            _page = w.currentWebpage;
            Reader.Instance.Forget();
            Transform modalNow = ModalRoot();
            if (modalNow != null) { _pageUnread = true; return; }   // a pop-up opened with the page: it's read first, the page after
            ReadPage();
        }

        private bool _pageUnread;

        private System.Collections.IEnumerator AfterClose(Component before, bool pageUnread)
        {
            float until = Time.unscaledTime + 0.3f;
            while (Time.unscaledTime < until) yield return null;
            FFApplicationManager app = FFApplicationManager.Instance;
            if (app != null && app.webpage != null && app.webpage.IsLoading) yield break;   // a page is coming: it's read
            if (ModalRoot() != null) yield break;                                           // another pop-up: it's read
            if (pageUnread || _pageUnread) ReadPage();
            else if (before != null && before.gameObject.activeInHierarchy && Reader.Instance.FocusOn(before, false)) Tell.Queue(Reader.Instance.Said());
            else Tell.Queue("Closed.");
        }

        private void ReadPage()
        {
            _pageUnread = false;
            FFApplicationManager app = FFApplicationManager.Instance;
            if (app == null || app.webpage == null) return;
            string title = PageTitle(app.webpage.CurrentWebType);
            Tell.Queue(Settings.ReadPages.On ? title + ". " + Reader.Instance.AllText(60) : title);
        }

        private void WatchModal()
        {
            Transform m = ModalRoot();
            if (m == _modal) return;
            bool closed = m == null && _modal != null;
            bool opened = m != null && _modal == null;
            _modal = m;
            if (opened) _before = Reader.Instance.Focus;
            Reader.Instance.Forget();
            if (m == null)
            {
                // Back where the player was (a drop-down list's button says what was picked).
                // (Not when the pop-up opened a page: the page is read instead.)
                FFApplicationManager app = FFApplicationManager.Instance;
                bool loading = app != null && app.webpage != null && app.webpage.IsLoading;
                // Wait a moment: a pop-up's button often opens a page a frame or two after the
                // pop-up closes (Log Out), and then the page is read instead.
                if (closed && !loading) StartCoroutine(AfterClose(_before, _pageUnread));
                _before = null;
                return;
            }
            Tell.Now(Reader.Instance.AllText(40, true));
        }

        // The sign-up and log-in forms flash an error ("Please enter all fields.") for a second.
        private bool _formError;
        private void WatchFormError()
        {
            FBSignUpWebpage page = SignUpPage();
            bool on = page != null && (page.errorCanvas.alpha > 0.9f || page.logInErrorCanvas.alpha > 0.9f);
            if (on && !_formError)
                Tell.Now(Ui.Text(page.errorCanvas.alpha > 0.9f ? page.errorText : page.logInErrorText));
            _formError = on;
        }
        // A short name for each page, said when it loads.
        public static string PageTitle(WebpageType t)
        {
            switch (t)
            {
                case WebpageType.FacebookSignUp: return "Facenook sign up page";
                case WebpageType.FaceookSpecialSignUp: return "Facenook log in page";
                case WebpageType.FacebookSignUpNodesMovies: return "Sign up: favorite movies";
                case WebpageType.FacebookSignUpNodesMusic: return "Sign up: favorite music";
                case WebpageType.FacebookSignUpNodesShowsBooks: return "Sign up: favorite shows and books";
                case WebpageType.FacebookSignUpAvatar: return "Sign up: profile picture";
                case WebpageType.FacebookPlayerProfile: return "Your profile";
                case WebpageType.FacebookPlayerNewsFeed: return "News feed";
                case WebpageType.FacebookMainMenu: return "Main menu";
                case WebpageType.FacebookSaveSelect: return "Choose a save";
                case WebpageType.FacebookCurrentHome: return "Home";
                case WebpageType.FacebookOutroThanks: return "Thanks for playing";
                case WebpageType.FacebookOutroStats: return "Your story";
                case WebpageType.FacebookSecretPuppy: return "Puppy app";
                case WebpageType.FacebookSecretFarm: return "Farm app";
                case WebpageType.FacebookQuizCh3: return "Quiz";
                case WebpageType.FacebookCh2RepeatNodes:
                case WebpageType.FacebookCh3RepeatNodes:
                case WebpageType.FacebookCh4RepeatNodes:
                case WebpageType.FacebookCh5RepeatNodes: return "Update your favorites";
            }
            string n = t.ToString();
            if (n.StartsWith("FacebookProfile")) return n.Substring(15) + "'s profile";
            if (n.StartsWith("FacebookEvent")) return "Event";
            if (n.StartsWith("FacebookNote")) return "Note";
            return Ui.FromName(n.Replace("Facebook", ""));
        }
    }

    // The 2020 home page at the start: each reaction brings a new post to the top of the feed.
    [HarmonyPatch(typeof(FBIntroReactionBar), nameof(FBIntroReactionBar.DidPressReaction))]
    internal static class IntroReacted
    {
        private static void Postfix(FBIntroReactionBar __instance)
        {
            string s = Ui.Text(__instance.outcomeText);
            if (s.Length > 0) Tell.Queue(s);
        }
    }

    [HarmonyPatch(typeof(FBCurrentWebpage), "ShowPostRunner", MethodType.Enumerator)]
    internal static class IntroNewPost
    {
        private static readonly System.Collections.Generic.HashSet<GameObject> Said = new System.Collections.Generic.HashSet<GameObject>();

        private static void Postfix()
        {
            FBCurrentWebpage page = UnityEngine.Object.FindObjectOfType<FBCurrentWebpage>();
            if (page == null) return;
            foreach (GameObject post in page.postObjects)
            {
                if (post == null || !post.activeInHierarchy || Said.Contains(post)) continue;
                Said.Add(post);
                Reader.Instance.PlaceBefore(post.transform);
                Tell.Queue("New post. " + Reader.Instance.TextOf(post.transform));
            }
        }
    }

    // The photo viewer: Left/Right (and Enter) go through the photos; say the new one.
    [HarmonyPatch]
    internal static class PhotoBrowse
    {
        private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (string m in new[] { "DidPressLeft", "DidPressRight", "DidPressDefault", "DidTypeLeftArrow", "DidTypeRightArrow" })
                yield return AccessTools.Method(typeof(FBPopupPhoto), m);
        }

        private static void Postfix(FBPopupPhoto __instance)
        {
            if (__instance == null || !__instance.gameObject.activeInHierarchy || !FBPopupManager.Instance.IsShowing) return;
            Tell.Now(Say(__instance));
        }

        public static string Say(FBPopupPhoto p)
        {
            var set = Traverse.Create(p).Field("photoSet").GetValue<System.Collections.Generic.List<FBPhotoType>>();
            int i = Traverse.Create(p).Field("curIndex").GetValue<int>();
            string pos = set != null && set.Count > 1 ? " " + (i + 1) + " of " + set.Count + "." : "";
            return Tell.Stop(Pictures.Describe(p.photoImage.transform) ?? "Photo") + pos;
        }
    }

    // The big date shown when the story starts and when it jumps back in time.
    [HarmonyPatch(typeof(FFDateTimePanel), nameof(FFDateTimePanel.ShowCurrent))]
    internal static class DateNow
    {
        private static void Postfix(FFDateTimePanel __instance) { Tell.Queue(Ui.Text(__instance.dateText).Replace("\n", " ")); }
    }

    [HarmonyPatch(typeof(FFDateTimePanel), nameof(FFDateTimePanel.ShowMemory))]
    internal static class DateMemory
    {
        private static void Postfix() { Tell.Queue("The date spins back through the years, to Friday, August 29, 2008."); }
    }

    // New notifications in the chat bar (friend requests, pokes, event invites...).
    [HarmonyPatch(typeof(FCNotificationListBehavior), nameof(FCNotificationListBehavior.AddNotification))]
    internal static class NewNotification
    {
        private static void Postfix(FCNotificationListBehavior __instance)
        {
            Transform rows = __instance.notificationParent.transform;
            if (rows.childCount == 0) return;
            Tell.Queue("Notification: " + Labels.TextUnder(rows.GetChild(rows.childCount - 1)));
        }
    }

    [HarmonyPatch(typeof(FCChatBarManager), nameof(FCChatBarManager.AddSignOutAlert))]
    internal static class SignOutAlert
    {
        private static void Postfix(FCChatBarManager __instance)
        {
            Tell.Queue("Account alert: " + Labels.TextUnder(__instance.accountTab.logoutRow.transform) + ". It's in the account list in the chat bar.");
        }
    }
}
