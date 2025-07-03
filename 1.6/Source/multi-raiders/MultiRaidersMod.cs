using HarmonyLib;
using System;
using System.Runtime;
using UnityEngine;
using Verse;

namespace MultiRaiders
{
    public class MultiRaidersMod : Mod
	{
        MultiRaidersSettings settings;

        string MaxRealRaidersBuffer = "";
        public MultiRaidersMod(ModContentPack content) : base(content)
		{
            this.settings = base.GetSettings<MultiRaidersSettings>();
            MaxRealRaidersBuffer = settings.MaxRealRaiders.ToString();

            new Harmony("MultiRaiders").PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("Raid settings");
            listingStandard.Label("Replace fraction of ALL raids with fakes. If 0, mod will have no effect until raids get larger than the maximum pawn number.");
            listingStandard.Label("If the max number of real raiders in this mod is set very high, it may never happen with vanilla raid generation.");
            listingStandard.Label("Recommended: 0%");
            listingStandard.Label("");
            listingStandard.Label($"{(int)(settings.ReplaceFractionWithFakes * 90.0)}%");
            settings.ReplaceFractionWithFakes = listingStandard.Slider(settings.ReplaceFractionWithFakes, 0f, 1f);

            listingStandard.Label("");
            listingStandard.Label("Maximum real raid pawns. Pawns beyond this number will be generated as fakes instead.");
            listingStandard.Label("Recommended: 120 (meaning mod is only active in LARGE raids)");
            listingStandard.Label("");
            listingStandard.TextFieldNumericLabeled<int>("Max Real Raiders", ref settings.MaxRealRaiders, ref MaxRealRaidersBuffer, 0, 500);
            listingStandard.Label("");
            listingStandard.Label("Should fakes drop gear/corpses. Recommended.");
            listingStandard.CheckboxLabeled("Fakes drop gear/corpses", ref settings.FakesDropGear);

            listingStandard.End();

            base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Multi Raiders";
		}
	}
}
