using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace EmilyIsAway3Access
{
    // Applies the mod's game hooks one class at a time, so a hook that can't apply on some
    // copy of the game (a DRM-free copy without Steam's parts, a different game version) is
    // skipped and logged, and the rest of the mod still works.
    internal static class SafePatch
    {
        public static void All(Harmony harmony, Assembly assembly, ManualLogSource log)
        {
            int failed = 0;
            foreach (Type t in AccessTools.GetTypesFromAssembly(assembly))
            {
                try { harmony.CreateClassProcessor(t).Patch(); }
                catch (Exception e)
                {
                    failed++;
                    log.LogWarning("Skipped the hooks in " + t.FullName + ": " + (e.InnerException ?? e).Message);
                }
            }
            if (failed > 0) log.LogWarning(failed + " group(s) of hooks couldn't be applied; the rest of the mod works without them.");
        }
    }
}
