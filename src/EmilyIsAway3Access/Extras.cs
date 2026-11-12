using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EmilyIsAway3Access.Speech;
using HarmonyLib;
using Steamworks;
using Version = System.Version;
using UnityEngine;
using UnityEngine.Networking;

namespace EmilyIsAway3Access
{
    // Once per game start, asks GitHub for the newest release and says so on the main menu
    // if it's newer than the installed mod. It reads the release manifest the installer
    // itself uses (a plain download, no account or token); nothing is sent but the request,
    // and a failed check is only logged. Turned off with "Announce available updates".
    internal class UpdateCheck : MonoBehaviour
    {
        private const string ManifestUrl = "https://github.com/shaysters/emily-is-away-3-access/releases/latest/download/loadstone-release.json";
        private static readonly Regex VersionField = new Regex(@"""version""\s*:\s*""v?([0-9.]+)""");

        private UnityWebRequest _request;
        private string _newer;
        private bool _done;
        private float _menuSince = -1;

        private void Start()
        {
            if (!Settings.UpdateCheck.On) { _done = true; return; }
            _request = UnityWebRequest.Get(ManifestUrl);
            _request.timeout = 15;
            _request.SendWebRequest();
        }

        private void Update()
        {
            if (_done) return;
            try
            {
                if (_newer != null)
                {
                    // On the main menu, once its page has been read.
                    FFApplicationManager app = FFApplicationManager.Instance;
                    bool menu = app != null && app.webpage != null && !app.webpage.IsLoading && app.webpage.CurrentWebType == WebpageType.FacebookMainMenu;
                    if (!menu) { _menuSince = -1; return; }
                    if (_menuSince < 0) _menuSince = Time.unscaledTime;
                    if (Time.unscaledTime - _menuSince < 4f) return;
                    _done = true;
                    Tell.Queue(Plugin.Title + " " + Spoken(_newer) + " is available. Run the installer again to update.");
                    return;
                }
                if (_request == null || !_request.isDone) return;
                if (_request.isNetworkError || _request.isHttpError)
                {
                    Plugin.Log.LogInfo("Update check skipped: " + _request.error);
                    _done = true;
                    return;
                }
                Match m = VersionField.Match(_request.downloadHandler.text);
                Version latest = m.Success ? Parse(m.Groups[1].Value) : null;
                Version installed = Parse(Plugin.Version);
                Plugin.Log.LogInfo("Update check: installed " + installed + ", latest " + latest);
                if (latest != null && installed != null && latest > installed) _newer = m.Groups[1].Value;
                else _done = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogInfo("Update check failed: " + e.Message);
                _done = true;
            }
            finally
            {
                if (_request != null && (_done || _newer != null)) { _request.Dispose(); _request = null; }
            }
        }

        private static string Spoken(string v) { return v.EndsWith(".0") && v.Split('.').Length == 3 ? v.Substring(0, v.Length - 2) : v; }

        private static Version Parse(string s)
        {
            try { return new Version(s); }
            catch { return null; }
        }
    }

    // Steam shows a new achievement only as a picture: say it.
    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.SetAchievement))]
    internal static class AchievementSaid
    {
        private static void Prefix(SteamManager __instance, CheevoEnum cheevo, out string __state)
        {
            __state = null;
            try
            {
                if (!SteamManager.Initialized) return;
                string id = Traverse.Create(__instance).Method("NameForAchievement", cheevo).GetValue<string>();
                bool had;
                if (string.IsNullOrEmpty(id) || !SteamUserStats.GetAchievement(id, out had) || had) return;
                __state = id;
            }
            catch (Exception e) { Plugin.Log.LogDebug("Achievement check: " + e.Message); }
        }

        private static void Postfix(string __state)
        {
            if (__state == null) return;
            string name = null;
            try { name = SteamUserStats.GetAchievementDisplayAttribute(__state, "name"); } catch { }
            Tell.Queue("Achievement unlocked: " + (string.IsNullOrEmpty(name) ? __state : name));
        }
    }

    // "Secret hints, spoilers": the Secrets page also lists the secrets you haven't found,
    // with the code that unlocks each one.
    internal static class SecretHints
    {
        public static string For(FBMainMenuSecrets page)
        {
            if (!Settings.SecretHints.On || page == null) return null;
            var objects = Traverse.Create(page).Field("secretObjects").GetValue<List<FBSecretObject>>();
            if (objects == null) return null;
            List<FBSecretsType> unlocked = PlayerValues.UnlockedSecrets ?? new List<FBSecretsType>();
            var parts = new List<string>();
            foreach (FBSecretObject o in objects.OrderBy(x => (int)x.secretsType))
            {
                if (o == null || unlocked.Contains(o.secretsType) || o.codes == null || o.codes.Length == 0) continue;
                parts.Add(Tell.Plain(o.secretName) + " (" + Tell.Plain(o.secretCaption) + "): type " + string.Join(" or ", o.codes));
            }
            if (!PlayerValues.MustacheModeUnlocked)
            {
                string code = Traverse.Create(page).Field("supersecretCode").GetValue<string>();
                if (!string.IsNullOrEmpty(code)) parts.Add("Moustache mode: type the letters " + string.Join(" ", code.ToCharArray().Select(ch => ch.ToString()).ToArray()) + " (they're hidden in the game's adverts)");
            }
            return parts.Count == 0 ? "Secret hints: you've found every secret that has a code." : "Secret hints, spoilers. Not found yet: " + string.Join(". ", parts.ToArray()) + ".";
        }
    }
}
