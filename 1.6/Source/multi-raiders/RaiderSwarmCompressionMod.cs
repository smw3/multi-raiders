using HarmonyLib;
using System;
using System.Reflection;
using System.Runtime;
using UnityEngine;
using Verse;

namespace MultiRaiders
{
    public class RaiderSwarmCompressionMod : Mod
	{
        RaiderSwarmCompressionSettings settings;

        string MaxRealRaidersBuffer = "";
        public RaiderSwarmCompressionMod(ModContentPack content) : base(content)
		{
            this.settings = base.GetSettings<RaiderSwarmCompressionSettings>();
            MaxRealRaidersBuffer = settings.MaxRealRaiders.ToString();

            new Harmony("Ingendum.RaiderSwarmCompression").PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard listingStandard = new();
            listingStandard.Begin(inRect);

            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Label("ReplaceAlwaysFractionDesc1".Translate());
            listingStandard.Label("ReplaceAlwaysFractionDesc2".Translate());
            listingStandard.Label("ReplaceAlwaysFractionRec".Translate());

            float replaceWithFakes = settings.ReplaceFractionWithFakes;
            float perc = listingStandard.SliderLabeled("ReplaceAlwaysFraction".Translate((int)(replaceWithFakes * 100.0f)), replaceWithFakes, 0f, 0.9f, 0.6f, null);
            settings.ReplaceFractionWithFakes = perc;
            listingStandard.GapLine();

            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Label("MaxRealRaidersDesc1".Translate());
            listingStandard.Label("MaxRealRaidersRec".Translate());

            int maxRealRaiders = settings.MaxRealRaiders;
            int num = Mathf.RoundToInt(listingStandard.SliderLabeled("MaxRealRaiders".Translate(maxRealRaiders), (float)maxRealRaiders, 1f, 500f, 0.6f, null));
            settings.MaxRealRaiders = num;
            listingStandard.GapLine();

            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("FakesDropGear".Translate(), ref settings.FakesDropGear);

            listingStandard.End();

            base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Raider Swarm Compression";
		}
	}
}
