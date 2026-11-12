using System.Text;
using EmilyIsAway3Access.Speech;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmilyIsAway3Access
{
    // Helpers for reading the game's Unity UI (uGUI + TextMesh Pro).
    internal static class Ui
    {
        // How see-through a thing really is: its CanvasGroups multiplied up the tree.
        public static float Alpha(Transform t)
        {
            float a = 1f;
            for (Transform p = t; p != null; p = p.parent)
            {
                CanvasGroup g = p.GetComponent<CanvasGroup>();
                if (g == null) continue;
                a *= g.alpha;
                if (g.ignoreParentGroups) break;
            }
            return a;
        }

        // Can the mouse reach it: no CanvasGroup above it turns off clicks.
        public static bool Clickable(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                CanvasGroup g = p.GetComponent<CanvasGroup>();
                if (g == null) continue;
                if (!g.blocksRaycasts) return false;
                if (g.ignoreParentGroups) break;
            }
            return true;
        }

        public static bool Shown(Transform t)
        {
            return t != null && t.gameObject.activeInHierarchy && Alpha(t) > 0.05f;
        }

        // Text boxes: Unity's InputField and TextMesh Pro's TMP_InputField (the sign-up page).
        public static bool IsBox(Component c)
        {
            if (c is TMP_InputField) return true;
            return c is InputField;
        }

        public static string BoxText(Component c)
        {
            if (c is TMP_InputField) return ((TMP_InputField)c).text;
            InputField f = c as InputField;
            return f != null ? f.text : "";
        }

        public static void SetBoxText(Component c, string text)
        {
            if (c is TMP_InputField) { ((TMP_InputField)c).text = text; return; }
            InputField f = c as InputField;
            if (f != null) f.text = text;
        }

        public static bool BoxFocused(Component c)
        {
            if (c is TMP_InputField) return ((TMP_InputField)c).isFocused;
            InputField f = c as InputField;
            return f != null && f.isFocused;
        }

        public static void ActivateBox(Component c)
        {
            UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(c.gameObject);
            if (c is TMP_InputField) { ((TMP_InputField)c).ActivateInputField(); ((TMP_InputField)c).MoveTextEnd(false); return; }
            InputField f = c as InputField;
            if (f != null) { f.ActivateInputField(); f.MoveTextEnd(false); }
        }

        public static void DeactivateBox(Component c)
        {
            if (c is TMP_InputField) { ((TMP_InputField)c).DeactivateInputField(); return; }
            InputField f = c as InputField;
            if (f != null) f.DeactivateInputField();
        }

        // A text box's hint text (the grey "Type here" shown when it's empty).
        public static string BoxPlaceholder(Component c)
        {
            Graphic g = c is InputField ? ((InputField)c).placeholder : null;
            if (c is TMP_InputField) g = ((TMP_InputField)c).placeholder;
            if (g is Text) return Tell.Plain(((Text)g).text);
            if (g is TMP_Text) return Text((TMP_Text)g);
            return "";
        }

        public static string Text(TMP_Text t) { return t == null ? "" : Tell.Plain(t.text).TrimEnd('|'); }

        // Visible text under a control, joined with commas.
        public static string TextUnder(Transform root)
        {
            var sb = new StringBuilder();
            foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>())
                Add(sb, t.transform, Text(t));
            foreach (Text t in root.GetComponentsInChildren<Text>())
                Add(sb, t.transform, Tell.Plain(t.text));
            return sb.ToString();
        }

        private static void Add(StringBuilder sb, Transform t, string text)
        {
            if (text.Length == 0 || !Shown(t)) return;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(text);
        }

        // "settingsButton" -> "settings", "Exit_Button" -> "exit".
        public static string FromName(string name)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_' || c == '-' || c == ' ') { sb.Append(' '); continue; }
                if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1])) sb.Append(' ');
                sb.Append(char.ToLower(c));
            }
            string s = " " + sb.ToString().Trim() + " ";
            foreach (string w in new[] { " button ", " btn ", " (clone) ", " clone " }) s = s.Replace(w, " ");
            return s.Trim();
        }

        public static string Path(Transform t)
        {
            string p = t.name;
            for (Transform x = t.parent; x != null; x = x.parent) p = x.name + "/" + p;
            return p;
        }

        // Screen position of a UI element's centre, for ordering controls.
        public static Vector2 ScreenPos(Transform t)
        {
            RectTransform r = t as RectTransform;
            if (r == null) return Vector2.zero;
            Vector3[] c = new Vector3[4];
            r.GetWorldCorners(c);
            Vector3 mid = (c[0] + c[2]) * 0.5f;
            Canvas canvas = t.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
                return canvas.worldCamera.WorldToScreenPoint(mid);
            return mid;
        }
    }
}
