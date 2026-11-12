using System.Collections.Generic;
using System.Text;
using EmilyIsAway3Access.Speech;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmilyIsAway3Access
{
    // Names for the game's buttons, text boxes and sliders.
    internal static class Labels
    {
        // Buttons that show only a picture, by their object name.
        private static readonly Dictionary<string, string> ByName = new Dictionary<string, string>
        {
            { "BackButton", "Back" },
            { "ForwardButton", "Forward" },
            { "RefreshButton", "Reload" },
            { "HomeButton", "Home" },
            { "ZoomOutButton", "Zoom out" },
            { "ZoomInButton", "Zoom in" },
            { "SettingsButton", "Settings" },
            { "BackTitle", "Back to save files" },
        };

        public static string ControlLabel(Selectable s)
        {
            if (Ui.IsBox(s)) return BoxLabel(s);
            string text = TextUnder(s.transform);
            if (text.Length > 0) return text;
            // A slider named by the row's title beside it (the Options pop-up's volumes).
            if (s is Slider && s.transform.parent != null)
            {
                Transform title = s.transform.parent.Find("Title");
                if (title != null && title != s.transform)
                {
                    string words = TextUnder(title);
                    if (words.Length > 0) return words;
                }
            }
            string named;
            if (ByName.TryGetValue(s.name, out named)) return named;
            string pic = Pictures.Describe(s.transform);
            if (pic != null) return pic;
            return Ui.FromName(s.name);
        }

        // A text box is named by its hint text, or the label just before it on the page.
        private static string BoxLabel(Selectable s)
        {
            string hint = Ui.BoxPlaceholder(s);
            string name = Ui.FromName(s.name).Replace(" input", "").Replace("input", "").Trim();
            if (name.Length == 0) name = "text";
            return hint.Length > 0 && hint != Ui.BoxText(s) ? name + ", " + hint : name;
        }

        // Visible text under a control, leaving out other controls inside it and the text
        // box's own contents.
        public static string TextUnder(Transform root)
        {
            var sb = new StringBuilder();
            Add(sb, root, root);
            return sb.ToString();
        }

        private static void Add(StringBuilder sb, Transform t, Transform root)
        {
            if (!t.gameObject.activeInHierarchy) return;
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha < 0.05f) return;
            if (t != root && t.GetComponent<Selectable>() != null) return;
            TMP_Text tmp = t.GetComponent<TMP_Text>();
            Text txt = t.GetComponent<Text>();
            string s = tmp != null ? Ui.Text(tmp) : txt != null ? Tell.Plain(txt.text) : "";
            if (s.Length > 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(s);
            }
            for (int i = 0; i < t.childCount; i++) Add(sb, t.GetChild(i), root);
        }

        public static string SliderValue(Slider s)
        {
            float range = s.maxValue - s.minValue;
            if (range <= 0f) return s.value.ToString();
            return Mathf.RoundToInt(100f * (s.value - s.minValue) / range) + " percent";
        }
    }
}
