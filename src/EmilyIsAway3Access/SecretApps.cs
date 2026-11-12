using System;
using System.Collections;
using System.Collections.Generic;
using EmilyIsAway3Access.Speech;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EmilyIsAway3Access
{
    // The two secret Facenook apps. CarrotVille (a little farm) is all buttons in the game,
    // but its plots and tools have no words, so they get names and states here. "Can you pet
    // the Dog?" is petted by holding the mouse button over the dog; here it's a button.
    internal static class SecretApps
    {
        private static readonly string[] Seeds = { "Carrot", "Purple carrot", "Blue carrot", "Rainbow carrot", "Gold carrot" };

        public static Item Composite(Transform t)
        {
            FBSecretFarmCanvas farm = t.GetComponentInParent<FBSecretFarmCanvas>();
            if (farm != null) return FarmItem(farm, t);
            FBSecretPuppyBox dog = t.GetComponent<FBSecretPuppyBox>();
            if (dog != null)
                return new Item { Target = dog, Type = Item.Kind.Button, Label = "Pet the dog", Enabled = true, PressFn = () => Pet(dog) };
            return null;
        }

        private static Item FarmItem(FBSecretFarmCanvas farm, Transform t)
        {
            FBSecretFarmPlot plot = t.GetComponent<FBSecretFarmPlot>();
            if (plot != null && plot.button != null)
            {
                int n = Array.IndexOf(farm.farmPlots, plot) + 1;
                return new Item
                {
                    Target = plot.button, Type = Item.Kind.Button, Label = "Plot " + n, Role = "button", Enabled = true,
                    State = () => PlotState(plot), SayStateAfterPress = true,
                    PressFn = () =>
                    {
                        if (!plot.button.interactable) return;
                        bool ripe = plot.PlotState == FBSecretFarmPlotState.Complete;
                        farm.DidPressFarmPlot(plot);
                        if (ripe) Tell.Now("Harvested!");
                    },
                };
            }
            // The tools and seeds sit beside their words in a row: match the row or the button.
            Button b = t.GetComponent<Button>();
            if (b == null)
                foreach (Button rowB in farm.rowButtons)
                    if (rowB != null && rowB.transform.parent == t) { b = rowB; break; }
            if (b == null)
                foreach (Button seedB in farm.seedButtons)
                    if (seedB != null && seedB.transform.parent == t) { b = seedB; break; }
            if (b == null && farm.shopButtImage != null && t == farm.shopButtImage.transform.parent) b = farm.shopButtImage.GetComponentInParent<Button>();
            if (b != null && farm.shopButtImage != null && (b.transform == farm.shopButtImage.transform || b.transform == farm.shopButtImage.transform.parent || b.transform.parent == farm.shopButtImage.transform.parent))
            {
                Button shop = b;
                return new Item
                {
                    Target = shop, Type = Item.Kind.Button, Enabled = true,
                    Label = Traverse.Create(farm).Field("isShopping").GetValue<bool>() ? "Back to the farm" : "Seed shop",
                    PressFn = () => farm.DidPressShopButton(),
                };
            }
            if (b != null)
            {
                int row = Array.IndexOf(farm.rowButtons, b);
                if (row >= 0)
                    return new Item
                    {
                        Target = b, Type = Item.Kind.Button, Role = "radio button", Enabled = true,
                        Label = row == 0 ? "Watering can" : Seeds[row - 1] + " seeds",
                        State = () => (b.interactable ? "not selected" : "selected") + (row == 0 ? "" : ", " + SeedCount(farm, row - 1)),
                        SayStateAfterPress = true, PressFn = () => { if (b.interactable) farm.DidPressFarmRow(row); },
                    };
                int seed = Array.IndexOf(farm.seedButtons, b);
                if (seed >= 0)
                    return new Item
                    {
                        Target = b, Type = Item.Kind.Button, Enabled = b.interactable,
                        Label = "Buy " + Seeds[seed].ToLower() + " seeds, " + Ui.Text(farm.seedPrices[seed]) + " coins",
                        State = () => SeedCount(farm, seed) + " in your bag", SayStateAfterPress = true,
                        PressFn = () => farm.DidPressBuySeeds(seed),
                    };
            }
            if (farm.shopMoneyText != null && t == farm.shopMoneyText.transform)
                return new Item { Target = farm.shopMoneyText, Type = Item.Kind.Text, Label = "Coins: " + Ui.Text(farm.shopMoneyText) };
            if (farm.moneyText != null && t == farm.moneyText.transform)
                return new Item { Target = farm.moneyText, Type = Item.Kind.Text, Label = "Coins: " + Ui.Text(farm.moneyText) };
            if (farm.levelText != null && t == farm.levelText.transform)
                return new Item { Target = farm.levelText, Type = Item.Kind.Text, Label = "Level " + Ui.Text(farm.levelText) + ", " + Mathf.RoundToInt(farm.xpFill.fillAmount * 100) + "% to the next level" };
            return null;
        }

        // Seed counts and prices are said with their buttons.
        public static bool SkipText(Transform t)
        {
            FBSecretFarmCanvas farm = t.GetComponentInParent<FBSecretFarmCanvas>();
            if (farm == null) return false;
            foreach (var x in farm.seedCounts) if (x != null && x.transform == t) return true;
            foreach (var x in farm.seedPrices) if (x != null && x.transform == t) return true;
            return false;
        }

        private static string SeedCount(FBSecretFarmCanvas farm, int seed)
        {
            var inv = Traverse.Create(farm).Field("seedInventory").GetValue<Dictionary<FBSecretFarmSeedType, int>>();
            int n;
            if (inv == null || !inv.TryGetValue((FBSecretFarmSeedType)seed, out n)) n = 0;
            return n + (n == 1 ? " seed" : " seeds");
        }

        private static string PlotState(FBSecretFarmPlot p)
        {
            string crop = Seeds[(int)p.SeedType].ToLower();
            switch (p.PlotState)
            {
                case FBSecretFarmPlotState.Basic: return "empty";
                case FBSecretFarmPlotState.Watered: return "watered, ready for seeds";
                case FBSecretFarmPlotState.Seeded: return crop + " planted, needs water";
                case FBSecretFarmPlotState.Growing: return crop + " growing";
                case FBSecretFarmPlotState.Complete: return crop + " ready to harvest";
                default: return p.PlotState.ToString();
            }
        }

        // Says when a crop is ready.
        private static readonly Dictionary<FBSecretFarmPlot, FBSecretFarmPlotState> _last = new Dictionary<FBSecretFarmPlot, FBSecretFarmPlotState>();
        private static float _next;

        public static void Watch()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            FBSecretFarmCanvas farm = UnityEngine.Object.FindObjectOfType<FBSecretFarmCanvas>();
            if (farm == null || !Ui.Shown(farm.transform)) { _last.Clear(); return; }
            for (int i = 0; i < farm.farmPlots.Length; i++)
            {
                FBSecretFarmPlot p = farm.farmPlots[i];
                FBSecretFarmPlotState was;
                if (_last.TryGetValue(p, out was) && was == FBSecretFarmPlotState.Growing && p.PlotState == FBSecretFarmPlotState.Complete)
                    Tell.Now("Plot " + (i + 1) + ": " + PlotState(p));
                _last[p] = p.PlotState;
            }
        }

        // Petting: the game pets the dog while the mouse button is held over it.
        private static bool _petting;

        private static void Pet(FBSecretPuppyBox dog)
        {
            if (_petting) return;
            Reader.Instance.StartCoroutine(PetRunner(dog));
        }

        private static IEnumerator PetRunner(FBSecretPuppyBox dog)
        {
            _petting = true;
            var hover = Traverse.Create(dog).Field("isHovering");
            hover.SetValue(true);
            Tell.Now("Petting the dog...");
            yield return new WaitForSecondsRealtime(1.5f);
            _petting = false;
            hover.SetValue(false);
            yield return null;
            string after = dog.afterCanvas != null ? Reader.Instance.TextOf(dog.afterCanvas.transform) : "";
            Tell.Now(after.Length > 0 ? "The dog loves it! " + after : "The dog loves it!");
        }

        [HarmonyPatch(typeof(Input), nameof(Input.GetMouseButton), typeof(int))]
        private static class HeldMouse
        {
            private static void Postfix(int button, ref bool __result) { if (_petting && button == 0) __result = true; }
        }
    }
}
