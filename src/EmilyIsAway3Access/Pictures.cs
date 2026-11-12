using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EmilyIsAway3Access
{
    // Descriptions of the game's pictures, by the picture's name in the game: photos (with
    // their captions), profile pictures, and the few other pictures that matter. Pictures
    // not known here are decoration and left out.
    internal static class Pictures
    {
        private static readonly Dictionary<string, string> Known = new Dictionary<string, string>
        {
            // The 2020 Facenook home page at the start.
            { "cfb_img1", "A memory card in old 2008 Facenook blue, with the date August 29 2008" },
            { "cfb_img2", "A smiley face meme. Its text says: I want change my name to \"NOBODY\" on Facenook so that when I see someone post something stupid, I can like it and then Facenook will say \"NOBODY likes this.\"" },
            { "cfb_img3", "A slice of salmon steak on a plate with a big red no symbol over it" },
        };

        // Your profile picture choices: blue silhouettes, head and shoulders.
        private static readonly Dictionary<PlayerAvatarType, string> PlayerLooks = new Dictionary<PlayerAvatarType, string>
        {
            { PlayerAvatarType.DefaultA, "Blue silhouette with short, neat hair" },
            { PlayerAvatarType.DefaultB, "Blue silhouette with a chin-length bob" },
            { PlayerAvatarType.DefaultC, "Blue silhouette with messy, spiky hair" },
            { PlayerAvatarType.DefaultD, "Blue silhouette with short, shaggy hair and long side-swept bangs" },
            { PlayerAvatarType.DefaultE, "Blue silhouette with long, wavy hair past the shoulders" },
            { PlayerAvatarType.DefaultF, "Blue silhouette with long, straight hair and straight-cut bangs" },
            { PlayerAvatarType.DefaultG, "Blue silhouette with short hair swept up at the front" },
            { PlayerAvatarType.DefaultH, "Blue silhouette with hair pinned up in a bun" },
            { PlayerAvatarType.UniqueGreg, "Blue picture of a bearded man with glasses in a Superman shirt" },
            { PlayerAvatarType.UniqueJack, "Blue picture of a bearded man with a green quiff and an earring" },
            { PlayerAvatarType.UniqueMark, "Blue picture of a man with swept-back hair and a pink moustache" },
        };

        private static Dictionary<string, string> _avatars;          // sprite name -> whose picture and what it shows
        private static Dictionary<string, FBPhotoType> _photos;      // sprite name -> photo
        private static Dictionary<string, string> _keys;             // sprite name -> PhotoText key

        public static string Describe(Transform t)
        {
            Image img = t.GetComponent<Image>();
            if (img == null || !img.enabled || img.sprite == null) return null;
            FBPhotoType? shown = PhotoShownBy(img);
            if (shown.HasValue) return "Photo: " + Photo(shown.Value, img.sprite.name);
            return ForSprite(img.sprite);
        }

        public static string ForSprite(Sprite s)
        {
            if (s == null) return null;
            string d;
            if (Known.TryGetValue(s.name, out d)) return d;
            if (PhotoText.Others.TryGetValue(s.name, out d)) return d;
            Build();
            if (_avatars.TryGetValue(s.name, out d)) return d;
            FBPhotoType p;
            if (_photos.TryGetValue(s.name, out p)) return "Photo: " + Photo(p, s.name);
            return GiftPicture(s.name);
        }

        // The virtual gifts' pictures (in gift posts on walls).
        private static Dictionary<string, string> _gifts;

        private static string GiftPicture(string name)
        {
            if (_gifts == null || _gifts.Count == 0)
            {
                _gifts = new Dictionary<string, string>();
                foreach (FBProfileGiftObject g in Resources.FindObjectsOfTypeAll<FBProfileGiftObject>())
                    if (g != null && g.sprite != null) _gifts[g.sprite.name] = Special.GiftName(g.giftType);
            }
            string d;
            return _gifts.TryGetValue(name, out d) ? d : null;
        }

        // Which photo an image shows, from the game part that put it there (thumbnail rows,
        // the photo viewer, the ending's photo boxes). Several photos share one picture with
        // different captions, so the picture alone isn't enough.
        private static FBPhotoType? PhotoShownBy(Image img)
        {
            try
            {
                Transform t = img.transform;
                for (int up = 0; up < 5 && t != null; up++, t = t.parent)
                {
                    foreach (MonoBehaviour mb in t.GetComponents<MonoBehaviour>())
                    {
                        if (mb == null) continue;
                        var tr = Traverse.Create(mb);
                        if (mb is FBPopupPhoto)
                        {
                            var pp = (FBPopupPhoto)mb;
                            if (pp.photoImage != img) continue;
                            var set = tr.Field("photoSet").GetValue<List<FBPhotoType>>();
                            int i = tr.Field("curIndex").GetValue<int>();
                            if (set != null && i >= 0 && i < set.Count) return set[i];
                            continue;
                        }
                        if (mb is FBOutroPhotoBox)
                        {
                            if (((FBOutroPhotoBox)mb).photoBase != img) continue;
                            object p = tr.Field("photo").GetValue();
                            if (p is FBPhotoType) return (FBPhotoType)p;
                            continue;
                        }
                        Image[] thumbs = tr.Field("thumbs").GetValue<Image[]>();
                        if (thumbs == null) continue;
                        int idx = Array.IndexOf(thumbs, img);
                        if (idx < 0) continue;
                        List<FBPhotoType> list = tr.Field("photos").GetValue<List<FBPhotoType>>()
                            ?? tr.Field("rowPhotos").GetValue<List<FBPhotoType>>();
                        if (list == null)
                        {
                            object data = tr.Field("postData").GetValue();
                            if (data != null) list = Traverse.Create(data).Field("photos").GetValue<List<FBPhotoType>>();
                        }
                        if (list != null && idx < list.Count) return list[idx];
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogDebug("PhotoShownBy: " + e.Message); }
            return null;
        }

        // What a photo shows, its caption, and whether you're in it. spriteName says which
        // version of the photo is on screen (some have a second version).
        public static string Photo(FBPhotoType type, string spriteName = null)
        {
            FBPhotoObject o = Get(type);
            Build();
            string key = null, what = null;
            if (spriteName != null) _keys.TryGetValue(spriteName, out key);
            if (key == null && o != null && o.photoSprite != null) key = o.photoSprite.name;
            if (key != null) PhotoText.Descriptions.TryGetValue(key, out what);
            string caption = o == null || string.IsNullOrEmpty(o.caption) ? "" : string.Format(o.caption, FacebookValues.GetFirstName(FBCharacter.Player).ToLower());
            var parts = new List<string>();
            if (what != null) parts.Add(what);
            if (o != null && o.HasOverlay) parts.Add("you're in this photo");
            if (caption.Length > 0) parts.Add("caption: " + caption);
            return parts.Count == 0 ? "a photo" : string.Join(". ", parts.ToArray());
        }

        private static FBPhotoObject Get(FBPhotoType t)
        {
            try { return FacebookSpriteManager.Instance == null ? null : FacebookSpriteManager.Instance.GetPhotoObject(t); }
            catch { return null; }
        }

        private static string Face(string spriteName)
        {
            foreach (var kv in PhotoText.Faces)
                if (spriteName.StartsWith(kv.Key)) return kv.Value;
            return null;
        }

        private static void Build()
        {
            if (_avatars != null && _photos != null && _photos.Count > 0) return;
            _avatars = new Dictionary<string, string>();
            _photos = new Dictionary<string, FBPhotoType>();
            _keys = new Dictionary<string, string>();
            if (FacebookSpriteManager.Instance == null) return;
            foreach (ProfilePictureType size in Enum.GetValues(typeof(ProfilePictureType)))
            {
                foreach (FBCharacter c in Enum.GetValues(typeof(FBCharacter)))
                {
                    if (c == FBCharacter.None || c == FBCharacter.Player) continue;
                    string name;
                    try { name = SpriteValues.GetAvatarString(c, size); } catch { continue; }
                    if (string.IsNullOrEmpty(name)) continue;
                    string who = FacebookValues.GetFullName(c) + "'s profile picture";
                    string face = Face(name);
                    if (face != null) who += ": " + face;
                    // _static: the character's pictures are greyed out in this story.
                    _avatars[name] = who;
                    _avatars[name + "_static"] = who + ", in grey";
                    _avatars[name + "_moustache"] = who + ", with a drawn-on moustache";
                    _avatars[name + "_static_moustache"] = who + ", in grey, with a drawn-on moustache";
                }
                foreach (RandoCharacter r in Enum.GetValues(typeof(RandoCharacter)))
                {
                    string name;
                    try { name = SpriteValues.GetAvatarString(r, size); } catch { continue; }
                    if (!string.IsNullOrEmpty(name)) _avatars[name] = FacebookValues.GetFullName(r) + "'s profile picture";
                }
                foreach (PlayerAvatarType a in Enum.GetValues(typeof(PlayerAvatarType)))
                {
                    string name;
                    try { name = SpriteValues.GetPlayerString(a, size); } catch { continue; }
                    string look;
                    if (!string.IsNullOrEmpty(name)) _avatars[name] = PlayerLooks.TryGetValue(a, out look) ? look : "Profile picture";
                }
            }
            foreach (WebpageType w in new[] { WebpageType.FacebookEventCh1Emily, WebpageType.FacebookEventCh1Evelyn })
            {
                string name;
                try { name = SpriteValues.GetEventString(w); } catch { continue; }
                string face;
                if (!string.IsNullOrEmpty(name) && PhotoText.Faces.TryGetValue(name, out face)) _avatars[name] = "Event picture: " + face;
            }
            foreach (FBPhotoType t in Enum.GetValues(typeof(FBPhotoType)))
            {
                FBPhotoObject o = Get(t);
                if (o == null) continue;
                string main = o.photoSprite != null ? o.photoSprite.name : null;
                string alt = o.alternateSprite != null ? o.alternateSprite.name : main;
                foreach (Sprite s in new[] { o.photoSprite, o.thumbSprite, o.moustacheSprite, o.moustacheThumb })
                    if (s != null) { if (!_photos.ContainsKey(s.name)) _photos[s.name] = t; if (main != null && !_keys.ContainsKey(s.name)) _keys[s.name] = main; }
                foreach (Sprite s in new[] { o.alternateSprite, o.alternateThumb, o.altMoustacheSpr, o.altMoustacheThumb })
                    if (s != null) { if (!_photos.ContainsKey(s.name)) _photos[s.name] = t; if (alt != null && !_keys.ContainsKey(s.name)) _keys[s.name] = alt; }
            }
        }
    }
}
