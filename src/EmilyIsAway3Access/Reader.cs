using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmilyIsAway3Access.Speech;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmilyIsAway3Access
{
    // One thing on screen: a piece of text, a link, a button, a text box...
    internal class Item
    {
        public enum Kind { Text, Link, Button, Toggle, Slider, TextBox, Picture }

        public Kind Type;
        public Component Target;
        public string Label = "";
        public bool Enabled = true;
        public int Group;          // 0 = the page or window itself, 1 = browser bar, 2 = chat bar
        public string Role;        // instead of "button": "radio button", "check box"...
        public Func<string> State; // "selected", "checked"... said after the role and again after pressing
        public bool SayStateAfterPress = true;
        public Action PressFn;     // instead of clicking the target
        public GameObject Go => Target.gameObject;
        public bool Pressable => Type != Kind.Text && Type != Kind.Picture;
    }

    // Browse-mode reading of whatever is on screen, like a screen reader's virtual buffer:
    // every text, link, button, text box and described picture in reading order. Up/Down move
    // one item, Home/End go to the top/bottom, Enter or Space press, Left/Right change sliders.
    // While a reply is being typed the game takes every key, so then use Ctrl with these keys.
    // When a pop-up or menu is open, only its items are listed.
    internal class Reader : MonoBehaviour
    {
        public static Reader Instance { get; private set; }
        private readonly List<Item> _items = new List<Item>();
        private Component _focus;
        private int _index = -1;
        private Selectable _box;
        private string _boxText;

        private void Awake() { Instance = this; }

        private void Update()
        {
            try
            {
                if (SettingsMenu.HoldsKeys) return;
                EchoTextBox();
                bool down = Input.GetKeyDown(KeyCode.DownArrow), up = Input.GetKeyDown(KeyCode.UpArrow);
                bool home = Input.GetKeyDown(KeyCode.Home), end = Input.GetKeyDown(KeyCode.End);
                bool left = Input.GetKeyDown(KeyCode.LeftArrow), right = Input.GetKeyDown(KeyCode.RightArrow);
                bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                bool space = Input.GetKeyDown(KeyCode.Space);
                // Escape closes the pop-up or chat bar list in front (its close, Cancel or collapse button).
                if (Input.GetKeyDown(KeyCode.Escape) && !Hooks.TypingReply() && Screens.ModalRoot() != null && FocusedBox() == null)
                {
                    Refresh();
                    Item close = null;
                    foreach (string word in new[] { "close,", "Cancel,", "collapse," })
                        if (close == null) close = _items.FirstOrDefault(i => i.Pressable && i.Enabled && Describe(i).StartsWith(word, StringComparison.OrdinalIgnoreCase));
                    if (close != null) { Press(close); Keys.UseKey(); return; }
                }
                if (!(down || up || home || end || left || right || enter || space)) return;
                if (Hooks.TypingReply() && !Keys.Ctrl()) return;   // every key types the reply

                Selectable box = FocusedBox();
                if (box != null && (left || right || space || home || end || enter)) return;   // editing keys

                if (down || up || home || end)
                {
                    Refresh();
                    if (_items.Count == 0) { Tell.Now("Nothing here."); Keys.UseKey(); return; }
                    if (down) Step(1);
                    else if (up) Step(-1);
                    else if (home) Go(0);
                    else Go(_items.Count - 1);
                    Keys.UseKey();
                    return;
                }

                Item c = Current();
                if (c == null) return;          // nothing picked: Enter and arrows go to the game
                if ((left || right) && c.Type == Item.Kind.Slider) { Nudge((Slider)c.Target, right ? 1 : -1); Keys.UseKey(); return; }
                if ((enter || space) && c.Pressable) { Press(c); Keys.UseKey(); }
            }
            catch (Exception e) { Plugin.Log.LogError("Reader: " + e); }
        }

        // --- Building the list ---

        public List<Item> Refresh()
        {
            _items.Clear();
            Transform modal = Screens.ModalRoot();
            if (modal != null) Walk(modal, 0, false);
            else
            {
                List<Canvas> roots = new List<Canvas>();
                foreach (Canvas c in FindObjectsOfType<Canvas>()) if (c.isRootCanvas && c.enabled) roots.Add(c);
                roots.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
                foreach (Canvas c in roots) Walk(c.transform, 0, false);
            }
            // The page (or window) first, then the browser bar, then the chat bar.
            List<Item> sorted = new List<Item>();
            for (int g = 0; g <= 2; g++) foreach (Item i in _items) if (i.Group == g) sorted.Add(i);
            _items.Clear();
            _items.AddRange(sorted);
            _index = _focus == null ? -1 : _items.FindIndex(i => i.Target == _focus);
            return _items;
        }

        // Depth first, in the game's own order (which is how its layouts are built: left
        // column, then right column; top to bottom).
        private void Walk(Transform t, int depth, bool inControl)
        {
            if (!t.gameObject.activeInHierarchy) return;
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            // (The storyline's percentages are see-through until the mouse is over them.)
            if (cg != null && cg.alpha < 0.05f && t.GetComponent<StatsNodeButton>() == null) return;
            if (Screens.Skip(t) || t.GetComponent<Scrollbar>() != null) return;   // scroll bars: the reader scrolls by itself

            Item made = inControl ? null : Special.Composite(t);
            if (made != null)
            {
                made.Group = Screens.Group(t);
                if (made.Label.Length > 0 || made.Pressable) _items.Add(made);   // an empty one: leave this part out
                return;
            }
            Selectable s = t.GetComponent<Selectable>();
            Selectable held = inControl ? null : LabelHolder(t);
            if (held != null)
            {
                // A button whose words sit next to it rather than inside it (the installer's
                // Next and Back): one item, named by those words.
                made = new Item { Target = held, Enabled = held.IsInteractable() && Ui.Clickable(held.transform), Type = Item.Kind.Button, Label = Labels.TextUnder(t) };
                Special.Apply(made);
                made.Group = Screens.Group(t);
                _items.Add(made);
                return;
            }
            // A button whose own picture is switched off and that has nothing else to show (the
            // unused photo slots in a "posted 1 photo" row) isn't on screen.
            Image own = s != null ? t.GetComponent<Image>() : null;
            if (own != null && (!own.enabled || own.sprite == null && own.color.a < 0.05f)
                && t.GetComponentsInChildren<Graphic>(false).All(g => g == own || !g.enabled || g is Image && ((Image)g).sprite == null))
                return;
            if (s != null && !(s is Scrollbar) && s.IsActive() && !Screens.SkipControl(s))
            {
                made = new Item { Target = s, Enabled = s.IsInteractable() && Ui.Clickable(t) };
                made.Type = s is Toggle ? Item.Kind.Toggle : s is Slider ? Item.Kind.Slider
                    : Ui.IsBox(s) ? Item.Kind.TextBox : Item.Kind.Button;
                made.Label = Labels.ControlLabel(s);
                Special.Apply(made);
            }
            else if (!inControl)
            {
                Component link = LinkOn(t);
                if (link != null)
                {
                    made = new Item { Target = link, Type = Item.Kind.Link, Label = Links.Text(link), Enabled = Ui.Clickable(t) };
                    Special.Apply(made);
                }
                else
                {
                    TMP_Text tmp = t.GetComponent<TMP_Text>();
                    Text txt = t.GetComponent<Text>();
                    string text = tmp != null ? Ui.Text(tmp) : txt != null ? Tell.Plain(txt.text) : "";
                    if (text.Length > 0 && !Screens.SkipText(t, text))
                        made = new Item { Target = (Component)tmp ?? txt, Type = Item.Kind.Text, Label = text };
                    else
                    {
                        string pic = Pictures.Describe(t);
                        if (pic != null) made = new Item { Target = t, Type = Item.Kind.Picture, Label = pic };
                    }
                }
            }
            if (made != null && (made.Label.Length > 0 || made.Pressable))
            {
                made.Group = Screens.Group(t);
                _items.Add(made);
            }
            bool nowIn = inControl || (made != null && made.Pressable && made.Type != Item.Kind.Link);
            foreach (Transform child in InReadingOrder(t)) Walk(child, depth + 1, nowIn);
            Item after = Special.After(t);
            if (after != null) { after.Group = Screens.Group(t); _items.Add(after); }
        }

        // Children top to bottom, then left to right, by where they are on screen (the game's
        // own order puts the installer's Next button before the page text, for example).
        private static List<Transform> InReadingOrder(Transform t)
        {
            var list = new List<Transform>(t.childCount);
            for (int i = 0; i < t.childCount; i++) list.Add(t.GetChild(i));
            if (list.Count < 2) return list;
            var tops = new Dictionary<Transform, Vector2>();
            foreach (Transform c in list) tops[c] = TopLeft(c);
            // Stable: keep the game's order when two things start at the same height.
            var order = new List<KeyValuePair<int, Transform>>();
            for (int i = 0; i < list.Count; i++) order.Add(new KeyValuePair<int, Transform>(i, list[i]));
            order.Sort((a, b) =>
            {
                Vector2 pa = tops[a.Value], pb = tops[b.Value];
                if (Mathf.Abs(pa.y - pb.y) > 4f) return pb.y.CompareTo(pa.y);
                if (Mathf.Abs(pa.x - pb.x) > 4f) return pa.x.CompareTo(pb.x);
                return a.Key.CompareTo(b.Key);
            });
            list.Clear();
            foreach (var kv in order) list.Add(kv.Value);
            return list;
        }

        private static readonly Vector3[] Corners = new Vector3[4];

        private static Vector2 TopLeft(Transform t)
        {
            RectTransform r = t as RectTransform;
            if (r == null) return Vector2.zero;
            r.GetWorldCorners(Corners);   // 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right
            return new Vector2(Corners[1].x, Corners[1].y);
        }

        // t holds exactly one button among its direct children, the button has no words of its
        // own, and t's other direct children are its words or pictures.
        private static Selectable LabelHolder(Transform t)
        {
            if (t.GetComponent<Selectable>() != null || t.childCount < 2 || t.childCount > 6) return null;
            Selectable found = null;
            bool words = false;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                if (!c.gameObject.activeInHierarchy) continue;
                Selectable s = c.GetComponent<Selectable>();
                if (s != null)
                {
                    if (found != null || s is Scrollbar || Ui.IsBox(s) || !s.IsActive() || c.childCount > 0 || Screens.SkipControl(s)) return null;
                    found = s;
                }
                else if (c.childCount > 0) return null;
                else if (c.GetComponent<TMP_Text>() != null || c.GetComponent<Text>() != null) words = true;
            }
            return found != null && words && Labels.TextUnder(found.transform).Length == 0 ? found : null;
        }

        private static Component LinkOn(Transform t)
        {
            foreach (Component c in t.GetComponents<MonoBehaviour>())
                if (c != null && Links.IsLink(c) && Links.Live(c)) return c;
            return null;
        }

        // --- Moving ---

        private Item Current()
        {
            if (_focus == null || !_focus.gameObject.activeInHierarchy) return null;
            Refresh();
            return _index >= 0 ? _items[_index] : null;
        }

        private void Step(int by)
        {
            int i = _index < 0 ? (by > 0 ? 0 : _items.Count - 1) : _index + by;
            string edge = "";
            if (i < 0) { i = 0; edge = "Top. "; }
            if (i >= _items.Count) { i = _items.Count - 1; edge = "Bottom. "; }
            Go(i, edge);
        }

        private void Go(int i, string prefix = "")
        {
            _index = i;
            Item c = _items[i];
            _focus = c.Target;
            FocusBox(c);
            ScrollTo(c.Target.transform);
            Tell.Now(prefix + Describe(c));
        }

        public static string Describe(Item c)
        {
            string s = c.Label;
            if (c.Role != null)
            {
                s = s + ", " + c.Role + (c.State != null ? ", " + c.State() : "");
                return c.Enabled ? s : s + ", unavailable";
            }
            switch (c.Type)
            {
                case Item.Kind.Link: s += Links.IsExternal(c.Target) ? ", web link" : ", link"; break;
                case Item.Kind.Button: s = (s.Length == 0 ? "unnamed" : s) + ", button"; break;
                case Item.Kind.Toggle: s += ", check box, " + (((Toggle)c.Target).isOn ? "checked" : "not checked"); break;
                case Item.Kind.Slider: s += ", slider, " + Labels.SliderValue((Slider)c.Target); break;
                case Item.Kind.TextBox:
                    {
                        string v = Ui.BoxText(c.Target);
                        bool secret = c.Target is TMP_InputField && ((TMP_InputField)c.Target).contentType == TMP_InputField.ContentType.Password;
                        s += ", edit, " + (v.Length == 0 ? "blank" : secret ? v.Length + " characters" : v);
                        break;
                    }
                case Item.Kind.Picture: if (!s.StartsWith("Photo: ")) s = "Picture: " + s; break;
            }
            if (c.Pressable && !c.Enabled) s += ", unavailable";
            return s;
        }

        // Starts reading from the top of a new page or window without saying anything.
        public void Forget() { _focus = null; _index = -1; }

        public Component Focus => _focus;

        // How the focused item reads now.
        public string Said()
        {
            Item c = Current();
            return c == null ? "" : Describe(c);
        }

        // Focus a given control (for example the first field of a form) and say it.
        public bool FocusOn(Component target, bool speak)
        {
            Refresh();
            int i = _items.FindIndex(x => x.Target == target);
            if (i < 0) return false;
            _index = i; _focus = target;
            FocusBox(_items[i]);
            if (speak) Tell.Queue(Describe(_items[i]));
            return true;
        }

        // Everything under one part of the screen (a new post, a pop-up...), read in one go.
        public string TextOf(Transform root)
        {
            var saved = new List<Item>(_items);
            _items.Clear();
            Walk(root, 0, false);
            var sb = new StringBuilder();
            foreach (Item i in _items)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(Tell.Stop(Describe(i)));
            }
            _items.Clear();
            _items.AddRange(saved);
            return sb.ToString();
        }

        // Puts the reading place just before a part of the screen, so Down goes into it.
        public void PlaceBefore(Transform root)
        {
            Refresh();
            int i = _items.FindIndex(x => x.Target.transform.IsChildOf(root));
            if (i < 0) return;
            _index = i - 1;
            _focus = i > 0 ? _items[i - 1].Target : null;
        }

        // All the text of the current page or window, for reading it in one go.
        public string AllText(int maxItems, bool everything = false)
        {
            Refresh();
            var sb = new StringBuilder();
            int n = 0;
            foreach (Item i in _items)
            {
                if (i.Group != 0 && !everything) continue;
                if (++n > maxItems) break;
                if (sb.Length > 0) sb.Append(" \n");
                sb.Append(Tell.Stop(Describe(i)));
            }
            return sb.ToString();
        }

        // --- Pressing ---

        public void Press(Item c)
        {
            if (!c.Enabled) { Tell.Now("Unavailable."); return; }
            if (c.Type != Item.Kind.TextBox && c.Type != Item.Kind.Slider) StartCoroutine(ReadChanges(new HashSet<Component>(Refresh().ConvertAll(i => i.Target)), Screens.ModalRoot()));
            if (c.PressFn != null)
            {
                c.PressFn();
                if (c.State != null && c.SayStateAfterPress) StartCoroutine(SayStateLater(c));
                return;
            }
            switch (c.Type)
            {
                case Item.Kind.Link:
                    if (Links.IsExternal(c.Target)) Tell.Now("Opening in your web browser.");
                    Links.Press(c.Target);
                    break;
                case Item.Kind.TextBox:
                    FocusBox(c);
                    Tell.Now("Editing. " + Describe(c));
                    break;
                case Item.Kind.Slider:
                    Tell.Now(Describe(c) + ". Use Left and Right to change it.");
                    break;
                case Item.Kind.Toggle:
                    Click(c.Go);
                    Tell.Queue(((Toggle)c.Target).isOn ? "checked" : "not checked");
                    break;
                default:
                    Click(c.Go);
                    if (c.State != null && c.SayStateAfterPress) StartCoroutine(SayStateLater(c));
                    break;
            }
        }

        // After a press, things may appear on the same page without a new page loading (the
        // main menu's Photos and Storyline, a post box opening...): read what's new.
        private System.Collections.IEnumerator ReadChanges(HashSet<Component> before, Transform modalBefore)
        {
            float until = Time.unscaledTime + 0.6f;
            while (Time.unscaledTime < until) yield return null;
            FFApplicationManager app = FFApplicationManager.Instance;
            if (app != null && app.webpage != null && (app.webpage.IsLoading)) yield break;   // a new page: read when it loads
            if (Screens.ModalRoot() != modalBefore) yield break;                                // a pop-up: read when it opens
            if (Hooks.TypingReplyFn != null && Hooks.TypingReplyFn()) yield break;             // a post being typed: the typing is read instead
            var fresh = new List<Item>();
            foreach (Item i in Refresh()) if (!before.Contains(i.Target) && (i.Group == 0 || modalBefore != null)) fresh.Add(i);
            if (fresh.Count == 0) yield break;
            var sb = new StringBuilder();
            for (int k = 0; k < fresh.Count && k < 30; k++)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(Tell.Stop(Describe(fresh[k])));
            }
            PlaceBefore(fresh[0].Target.transform);
            Tell.Queue(sb.ToString());
        }

        private static System.Collections.IEnumerator SayStateLater(Item c)
        {
            yield return null;
            if (c.Target != null) Tell.Queue(c.State());
        }

        public static void Click(GameObject go)
        {
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
        }

        private static void Nudge(Slider s, int dir)
        {
            float step = s.wholeNumbers ? Mathf.Max(1f, Mathf.Round((s.maxValue - s.minValue) / 10f)) : (s.maxValue - s.minValue) / 10f;
            s.value = Mathf.Clamp(s.value + dir * step, s.minValue, s.maxValue);
            Tell.Now(Labels.SliderValue(s));
        }

        // Scroll the web page so a sighted helper sees the same thing.
        private static void ScrollTo(Transform t)
        {
            FFApplicationManager app = FFApplicationManager.Instance;
            if (app == null || app.webpage == null || !t.IsChildOf(app.webpage.webpageParent)) return;
            FFWebpageManager w = app.webpage;
            float pageH = w.dummyPage.sizeDelta.y;
            float viewH = w.viewport.rect.height / w.webpageParent.localScale.y;
            if (pageH <= viewH) return;
            float y = -(w.webpageParent.InverseTransformPoint(t.position).y);
            float top = Mathf.Clamp(y - viewH / 3f, 0f, pageH - viewH);
            w.webScroll.verticalNormalizedPosition = 1f - top / (pageH - viewH);
        }

        // --- Text boxes ---

        private void FocusBox(Item c)
        {
            Selectable box = c.Type == Item.Kind.TextBox ? (Selectable)c.Target : null;
            if (box != null) Ui.ActivateBox(box);
            else if (FocusedBox() != null)
            {
                Ui.DeactivateBox(FocusedBox());
                EventSystem.current?.SetSelectedGameObject(null);
            }
            _box = box;
            _boxText = box == null ? null : Ui.BoxText(box);
        }

        public static Selectable FocusedBox()
        {
            GameObject sel = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (sel == null) return null;
            foreach (Selectable s in sel.GetComponents<Selectable>())
                if (Ui.IsBox(s) && Ui.BoxFocused(s)) return s;
            return null;
        }

        // Screen readers can't see the game's text boxes, so say each typed or deleted letter.
        private void EchoTextBox()
        {
            Selectable box = FocusedBox();
            if (box != _box)
            {
                // The game moved the keyboard to another box (Tab or Enter on the sign-up page).
                if (box != null && _box != null) { _focus = box; Tell.Now(Describe(new Item { Target = box, Type = Item.Kind.TextBox, Label = Labels.ControlLabel(box) })); }
                _box = box; _boxText = box == null ? null : Ui.BoxText(box);
                return;
            }
            if (box == null || Ui.BoxText(box) == _boxText) return;
            string old = _boxText ?? "";
            string now = Ui.BoxText(box) ?? "";
            _boxText = now;
            if (!Settings.EchoLetters.On) return;
            bool secret = box is TMP_InputField && ((TMP_InputField)box).contentType == TMP_InputField.ContentType.Password;
            if (now.Length == old.Length + 1 && now.StartsWith(old)) Tell.Now(secret ? "star" : Keys.SayChar(now[now.Length - 1]));
            else if (old.Length == now.Length + 1 && old.StartsWith(now)) Tell.Now((secret ? "star" : Keys.SayChar(old[old.Length - 1])) + " deleted");
            else Tell.Now(now.Length == 0 ? "blank" : secret ? now.Length + " characters" : now);
        }
    }
}
