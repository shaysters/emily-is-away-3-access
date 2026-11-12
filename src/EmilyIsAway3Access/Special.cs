using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EmilyIsAway3Access
{
    // Controls that look like one thing but are built as another: the installer's radio
    // buttons and license check box are plain buttons in the game.
    internal static class Special
    {
        private static readonly string[] Systems = { "Minnows XP", "Granny Smith OSX" };

        // The four desktop pictures the installer can save to your desktop.
        private static readonly string[] Desktops =
        {
            "Minnows XP desktop: a green grassy hill rolling under a bright blue sky with white clouds",
            "Granny Smith OSX desktop: glowing pink and purple light spreading like an aurora across a starry night sky",
            "Newer Minnows desktop: sweeping ribbons of green, teal and yellow light on a dark blue-green background",
            "Newer Granny Smith desktop: soft crossing swooshes of light blue on a deep blue background",
        };

        // The reaction buttons on the 2020 home page, in the game's order.
        private static readonly string[] Reactions = { "Like", "Love", "Haha", "Wow", "Sad", "Angry", "Skip" };

        public static void Apply(Item item)
        {
            Transform t = item.Target.transform;
            FBIntroReactionEmoji emoji = t.GetComponent<FBIntroReactionEmoji>();
            if (emoji != null)
            {
                // Pictures are named cfb_react1_0 (Like) to cfb_react7_0 (Skip).
                string pic = emoji.frames != null && emoji.frames.Length > 0 && emoji.frames[0] != null ? emoji.frames[0].name : "";
                int i = pic.StartsWith("cfb_react") && pic.Length > 9 ? pic[9] - '1' : -1;
                item.Label = emoji.isSkip ? "Skip" : i >= 0 && i < Reactions.Length ? Reactions[i] : "reaction";
                item.Role = "reaction button";
                return;
            }
            if (SignUp(item, t)) return;
            FBMainMenuSecrets secrets = t.GetComponentInParent<FBMainMenuSecrets>();
            if (secrets != null && item.Target == (Component)secrets.cheatCodeInput) { item.Label = "Secret code"; return; }
            if (item.Type == Item.Kind.Button && t.name == "TabButton" && t.parent != null)
            {
                // The chat bar's three tabs; their words are beside the button.
                string what = t.parent.name == "FriendsTab" ? "Friends list" : t.parent.name == "NotifiactionsTab" ? "Notifications" : t.parent.name == "AccountTab" ? "Account" : null;
                if (t.parent.name.StartsWith("ChatTab"))
                {
                    FCChatBoxBehavior box = t.parent.GetComponent<FCChatBoxBehavior>();
                    if (box != null && box.Character != FBCharacter.None)
                    {
                        Chat chat = ChatState.For(box);
                        item.Label = "Chat with " + chat.Name;
                        item.Role = "chat tab";
                        item.State = () => box.IsOpened ? "open" : chat.Choices != null ? "waiting for your reply" : box.IsNotifying ? "new messages" : "closed";
                        item.SayStateAfterPress = false;
                        return;
                    }
                }
                if (what != null) { string extra = Labels.TextUnder(t.parent); item.Label = extra.Length > 0 && !extra.StartsWith(what) ? what + ", " + extra : extra.Length > 0 ? extra : what; return; }
            }
            if (item.Type == Item.Kind.Button && t.name == "FacebookTitle") { item.Label = "Facenook home"; return; }
            InstallScreenManager inst = Screens.Installer();
            if (inst == null) return;

            InstallRadioBehavior radio = t.GetComponentInParent<InstallRadioBehavior>();
            if (radio != null)
            {
                int i = System.Array.IndexOf(inst.systemRadios, radio);
                int d = System.Array.IndexOf(inst.desktopRadios, radio);
                if (i >= 0) item.Label = Systems[i];
                else if (d >= 0) item.Label = Desktops[d];
                item.Role = "radio button";
                item.State = () => radio.IsSelected ? "selected" : "not selected";
                item.Enabled = true;   // the selected one is made unclickable, but it's not "unavailable"
                return;
            }
            if (t.parent != null && t.parent.name == "AcceptButton")
            {
                item.Role = "check box";
                item.State = () => inst.checkmarkImage.enabled ? "checked" : "not checked";
            }
        }

        // The sign-up and log-in forms: each row has its label ("Username:") beside the text
        // box and a Randomize link; the birthday is two drop-down lists.
        private static bool SignUp(Item item, Transform t)
        {
            FBSignUpWebpage page = t.GetComponentInParent<FBSignUpWebpage>();
            if (page == null) return false;
            string field = RowLabel(t.parent);
            if (item.Type == Item.Kind.TextBox && field != null) { item.Label = field; return true; }
            if (item.Type == Item.Kind.Link && t.name == "RandomButton" && field != null) { item.Label = "Randomize " + field.ToLowerInvariant(); return true; }
            FBSignUpDropdownBox box = null;
            if (t.parent != null && (t.parent.name == "Dropdown (1)" || t.parent.name == "Dropdown (2)"))
            {
                bool month = t.parent.name.Contains("1");
                box = month ? page.monthDropdown : page.dayDropdown;
                TMPro.TextMeshProUGUI shown = month ? page.monthExample : page.dayExample;
                item.Label = month ? "Birthday month" : "Birthday day";
                item.Role = "drop-down list";
                item.State = () => box.ChosenIndex < 0 ? "not chosen" : Ui.Text(shown);
                item.SayStateAfterPress = false;   // it opens the list instead
                return true;
            }
            return false;
        }

        // --- Things built from several parts that the reader shows as one ---

        // Names given to the sign-up page's picture choices (favorite movies, music...).
        public static readonly Dictionary<FBSignUpNode, string> NodeNames = new Dictionary<FBSignUpNode, string>();

        // Something the mod adds after a part of the screen (the secret hints after the
        // Secrets page's list).
        public static Item After(Transform t)
        {
            FBMainMenuSecrets secrets = t.GetComponent<FBMainMenuSecrets>();
            if (secrets == null || secrets.secretGrid == null || !Ui.Shown(secrets.secretGrid.transform)) return null;
            string hints = SecretHints.For(secrets);
            return hints == null ? null : new Item { Target = secrets, Type = Item.Kind.Text, Label = hints };
        }

        public static Item Composite(Transform t)
        {
            FBSignUpNode node = t.GetComponent<FBSignUpNode>();
            if (node != null) return SignUpNodeItem(node);
            Item app = SecretApps.Composite(t);
            if (app != null) return app;
            FBProfileTabButton tab = t.GetComponent<FBProfileTabButton>();
            if (tab != null)
            {
                bool on = tab.focusBack != null && tab.focusBack.enabled;
                return new Item { Target = tab, Type = Item.Kind.Button, Label = Ui.Text(tab.tabText), Role = "tab", State = () => tab.focusBack.enabled ? "selected" : "not selected", SayStateAfterPress = false, PressFn = () => Reader.Click(tab.gameObject) };
            }
            // A friend's picture in a friends box: the game shortens long names to fit.
            FBProfileFriendNode friend = t.GetComponent<FBProfileFriendNode>();
            if (friend != null && friend.friendImage != null)
            {
                Button fb = friend.friendImage.GetComponent<Button>() ?? friend.friendButton;
                FBCharacter who = Traverse.Create(friend).Field("friend").GetValue<FBCharacter>();
                string name = who != FBCharacter.None ? FacebookValues.GetFullName(who) : (friend.friendLink != null ? Labels.TextUnder(friend.friendLink.transform) : "");
                if (fb != null && name.Length > 0)
                    return new Item { Target = fb, Type = Item.Kind.Button, Label = name, Enabled = fb.interactable, PressFn = () => friend.DidPressAvatar() };
            }
            // The main menu's timeline filters: the chosen one is switched off in the game.
            FBMainMenuWebpage menu = t.GetComponentInParent<FBMainMenuWebpage>();
            if (menu != null && menu.filterButtons != null)
            {
                // The row holds the button and, beside it, its words.
                int fi = -1;
                Button fbtn = null;
                for (int k = 0; k < menu.filterButtons.Length && fi < 0; k++)
                {
                    Button b = menu.filterButtons[k];
                    if (b != null && (b.transform == t || b.transform.parent == t)) { fi = k; fbtn = b; }
                }
                if (fi >= 0)
                    return new Item
                    {
                        Target = fbtn, Type = Item.Kind.Button, Label = Labels.TextUnder(fbtn.transform.parent), Role = "radio button", Enabled = true,
                        State = () => fbtn.interactable ? "not selected" : "selected", SayStateAfterPress = true,
                        PressFn = () => { if (fbtn.interactable) menu.DidPressFilterStories(fi); },
                    };
            }
            // The storyline's choice graph: other players' percentages show when the mouse is
            // over a choice; here they're read as text.
            StatsNodeButton stat = t.GetComponent<StatsNodeButton>();
            if (stat != null && (stat.percent == null || Ui.Text(stat.percent).Length == 0 || Traverse.Create(stat).Field("isHidden").GetValue<bool>())) return new Item { Target = stat, Type = Item.Kind.Text, Label = "" };
            if (stat != null)
            {
                FBStatisticsChoiceNode choice = t.GetComponentInParent<FBStatisticsChoiceNode>();
                bool mine = choice != null && choice.NodeState == StatisticsChoiceNodeState.Chosen;
                return new Item { Target = stat.percent, Type = Item.Kind.Text, Label = (mine ? "Your choice: " : "Another choice: ") + Ui.Text(stat.percent) + " of players" };
            }
            FBProfileGiftNode gift = t.GetComponent<FBProfileGiftNode>();
            if (gift != null && gift.unfocusButton != null)
                return new Item
                {
                    Target = gift.unfocusButton, Type = Item.Kind.Button, Label = GiftName(gift.GiftType), Role = "radio button",
                    State = () => gift.IsSelected ? "selected" : "not selected", Enabled = gift.unfocusButton.interactable || gift.IsSelected,
                    SayStateAfterPress = true, PressFn = () => { if (!gift.IsSelected) gift.DidPressSelectButton(); },
                };
            // A quiz question's four answers: radio buttons named by their answer text.
            FBQuizQuestionBox quiz = t.GetComponentInParent<FBQuizQuestionBox>();
            if (quiz != null && quiz.radioButtons != null)
            {
                // The answer's button may be this, or sit beside the answer's words in this row.
                int i = -1;
                Button rb = null;
                for (int k = 0; k < quiz.radioButtons.Length && i < 0; k++)
                {
                    Button q = quiz.radioButtons[k];
                    if (q != null && (q.transform == t || q.transform.parent == t && t != quiz.transform)) { i = k; rb = q; }
                }
                if (i >= 0)
                    return new Item
                    {
                        Target = rb, Type = Item.Kind.Button, Label = quiz.answerTexts != null && i < quiz.answerTexts.Length ? Ui.Text(quiz.answerTexts[i]) : "answer " + (i + 1),
                        Role = "radio button", State = () => quiz.ChosenNode == i ? "selected" : "not selected", Enabled = true,
                        SayStateAfterPress = true, PressFn = () => { if (quiz.ChosenNode != i) quiz.DidPressNode(i); },
                    };
            }
            // Other things the game makes clickable without a button (notification rows, refresh).
            if (t.GetComponent<FBNewsFeedRefreshButton>() != null || t.GetComponent<FCNotificationRowBehavior>() != null)
                return new Item { Target = t, Type = Item.Kind.Button, Label = Labels.TextUnder(t), PressFn = () => Reader.Click(t.gameObject) };
            return null;
        }

        // A picture you pick on the sign-up pages: your profile picture (one of eight) or your
        // favorite movies, music, shows and books (pick a few). Two buttons in the game, one
        // check box or radio button here.
        private static Item SignUpNodeItem(FBSignUpNode node)
        {
            Transform off = node.transform.Find("OffButton");
            Button select = (off != null ? off.GetComponent<Button>() : null) ?? node.unfocusButton;
            if (select == null) return null;
            bool radio = Traverse.Create(node).Field("isRadio").GetValue<bool>();
            string name;
            if (!NodeNames.TryGetValue(node, out name) || string.IsNullOrEmpty(name))
                name = (node.content != null ? Pictures.ForSprite(node.content.sprite) : null) ?? "picture";
            return new Item
            {
                Target = select,
                Type = Item.Kind.Button,
                Label = name,
                Role = radio ? "radio button" : "check box",
                State = () => radio ? (node.IsSelected ? "selected" : "not selected") : (node.IsSelected ? "checked" : "not checked"),
                Enabled = select.interactable || node.IsSelected,
                SayStateAfterPress = true,
                PressFn = () =>
                {
                    if (node.IsSelected) { if (!radio) node.DidPressDeselectButton(); }
                    else node.DidPressSelectButton();
                },
            };
        }

        // The virtual gifts you can send on someone's wall.
        public static string GiftName(FBProfileGiftType g)
        {
            switch (g)
            {
                case FBProfileGiftType.Kiss: return "Gift: a kiss (red lips)";
                case FBProfileGiftType.Balloon: return "Gift: a balloon";
                case FBProfileGiftType.Rose: return "Gift: a rose";
                case FBProfileGiftType.Champagne: return "Gift: champagne";
                case FBProfileGiftType.Cake: return "Gift: a birthday cake";
                case FBProfileGiftType.Thong: return "Gift: a thong";
                default: return "Gift: " + g;
            }
        }

        // The words in a form row's "FieldText", without the colon.
        public static string RowLabel(Transform row)
        {
            if (row == null) return null;
            Transform f = row.Find("FieldText");
            TMPro.TMP_Text text = f != null ? f.GetComponent<TMPro.TMP_Text>() : null;
            if (text == null) return null;
            string s = Ui.Text(text).TrimEnd(':', ' ');
            return s.Length > 0 ? s : null;
        }
    }

    [HarmonyPatch(typeof(FBSignUpNode), nameof(FBSignUpNode.SetUp))]
    internal static class SignUpNodeName
    {
        private static void Postfix(FBSignUpNode __instance, string name) { Special.NodeNames[__instance] = name; }
    }
}
namespace EmilyIsAway3Access
{
    // The quiz shows each question in the same place, so a new one is read when it appears.
    [HarmonyLib.HarmonyPatch(typeof(FBQuizWebpage), "SetUpQuestion")]
    internal static class QuizQuestion
    {
        private static void Postfix(FBQuizWebpage __instance)
        {
            FBQuizQuestionBox q = __instance.questionBox;
            if (q == null) return;
            var sb = new System.Text.StringBuilder();
            sb.Append(Ui.Text(__instance.numberText)).Append(". ").Append(Speech.Tell.Stop(Ui.Text(q.questionText)));
            for (int i = 0; i < q.answerTexts.Length; i++)
                sb.Append(' ').Append(Speech.Tell.Stop(Ui.Text(q.answerTexts[i]) + (q.ChosenNode == i ? ", selected" : "")));
            Speech.Tell.Queue(sb.ToString());
        }
    }
}

namespace EmilyIsAway3Access
{
    // The quiz results count up for a few seconds ("Calculating Results..."): say that, and
    // read the final results when they're done.
    [HarmonyLib.HarmonyPatch(typeof(FBQuizWebpage), "SetUpResults")]
    internal static class QuizResults
    {
        public static bool Calculating(Transform t)
        {
            FBQuizWebpage q = t.GetComponentInParent<FBQuizWebpage>();
            return q != null && q.resultsText != null && Ui.Text(q.resultsText).StartsWith("Calculating")
                && t.GetComponentInParent<FBQuizResultRow>() != null;
        }

        private static void Postfix(FBQuizWebpage __instance)
        {
            var sb = new System.Text.StringBuilder("Results: " + Speech.Tell.Stop(Ui.Text(__instance.resultsText)));
            foreach (FBQuizResultRow row in __instance.resultRows)
                if (row != null && Ui.Shown(row.transform)) sb.Append(' ').Append(Speech.Tell.Stop(Reader.Instance.TextOf(row.transform)));
            Speech.Tell.Queue(sb.ToString());
        }
    }
}

namespace EmilyIsAway3Access
{
    // After a chapter, the main menu adds that chapter's posts to the timeline one by one:
    // read each as it arrives.
    [HarmonyLib.HarmonyPatch(typeof(FBMenuMiniFeed), nameof(FBMenuMiniFeed.SetUp))]
    internal static class MenuNewPost
    {
        private static object _top;

        private static void Postfix(FBMenuMiniFeed __instance, System.Collections.Generic.List<MiniFeedData> menuFeed)
        {
            object top = menuFeed != null && menuFeed.Count > 0 ? menuFeed[0] : null;
            bool added = _top != null && top != null && top != _top && FFBlockingPanel.Instance != null && FFBlockingPanel.Instance.blockCanvas != null && FFBlockingPanel.Instance.blockCanvas.blocksRaycasts;
            _top = top;
            if (added) __instance.StartCoroutine(Read(__instance));
        }

        private static System.Collections.IEnumerator Read(FBMenuMiniFeed feed)
        {
            yield return null;   // the old rows are gone after this frame
            Transform list = feed.minifeedParent != null ? feed.minifeedParent.transform : null;
            if (list == null) yield break;
            for (int i = 0; i < list.childCount; i++)
            {
                string text = Reader.Instance.TextOf(list.GetChild(i));
                if (text.Length == 0) continue;
                Reader.Instance.PlaceBefore(list.GetChild(i));
                Speech.Tell.Queue("New: " + text);
                yield break;
            }
        }
    }
}
