using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MultiRaiders
{
    public class MultiRaidersSettings : ModSettings
    {
        public bool FakesDropGear = true;

        public float ReplaceFractionWithFakes = 0f;
        public int MaxRealRaiders = 120;

        public static MultiRaidersSettings Settings => LoadedModManager.GetMod<MultiRaidersMod>().GetSettings<MultiRaidersSettings>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref FakesDropGear, "FakesDropGear");
            Scribe_Values.Look(ref ReplaceFractionWithFakes, "ReplaceFractionWithFakes");
            Scribe_Values.Look(ref MaxRealRaiders, "MaxRealRaiders");
            base.ExposeData();
        }
    }
}
