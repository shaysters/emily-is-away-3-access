using HarmonyLib;
using TMPro;
using UnityEngine;

namespace EmilyIsAway3Access
{
    // The game's four kinds of text links (TextLinkButton, HeaderLinkButton, MessageLinkButton,
    // ChoiceLinkButton). They only react to a click while the mouse is over the linked words,
    // so the mod presses them by doing what the click would do.
    internal static class Links
    {
        public static bool IsLink(Component c)
        {
            return c is TextLinkButton || c is HeaderLinkButton || c is MessageLinkButton || c is ChoiceLinkButton;
        }

        // A link component that's really a link right now (the game turns them into plain text
        // with SetStatic, or disables them when the text has no <link> part).
        public static bool Live(Component c)
        {
            Behaviour b = c as Behaviour;
            if (b == null || !b.enabled) return false;
            if (c is TextLinkButton) return ((TextLinkButton)c).linkType != LinkType.None;
            if (c is HeaderLinkButton) return ((HeaderLinkButton)c).linkType != LinkType.None;
            if (c is MessageLinkButton) return ((MessageLinkButton)c).linkType != LinkType.None;
            return c is ChoiceLinkButton;
        }

        public static string Text(Component c)
        {
            TMP_Text t = c.GetComponent<TMP_Text>();
            return t == null ? "" : Ui.Text(t);
        }

        // External links open the real web page in the player's browser.
        public static bool IsExternal(Component c)
        {
            if (c is TextLinkButton) return ((TextLinkButton)c).linkType == LinkType.ExternalURL;
            if (c is HeaderLinkButton) return ((HeaderLinkButton)c).linkType == LinkType.ExternalURL;
            if (c is MessageLinkButton) return ((MessageLinkButton)c).linkType == LinkType.ExternalURL;
            return false;
        }

        public static void Press(Component c)
        {
            if (c is ChoiceLinkButton)
            {
                Traverse.Create(c).Field("onClickEvent").GetValue<UnityEngine.Events.UnityEvent>()?.Invoke();
                return;
            }
            LinkType type = LinkType.None;
            WebpageType page = WebpageType.None;
            ExternalURLType url = ExternalURLType.None;
            UnityEngine.Events.UnityEvent fn = null;
            if (c is TextLinkButton) { var l = (TextLinkButton)c; type = l.linkType; page = l.webpageType; url = l.externalURL; fn = l.onClickEvent; }
            else if (c is HeaderLinkButton) { var l = (HeaderLinkButton)c; type = l.linkType; page = l.webpageType; url = l.externalURL; fn = l.onClickEvent; }
            else if (c is MessageLinkButton) { var l = (MessageLinkButton)c; type = l.linkType; page = l.webpageType; url = l.externalURL; }
            switch (type)
            {
                case LinkType.InternalWebpage:
                    SoundManager.Instance.PlayMouseClick();
                    FFApplicationManager.Instance.NavigateToWebpage(page);
                    break;
                case LinkType.ExternalURL:
                    SoundManager.Instance.PlayMouseClick();
                    FFApplicationManager.Instance.OpenExternalURL(url);
                    break;
                case LinkType.Function:
                    fn?.Invoke();
                    break;
            }
        }
    }
}
