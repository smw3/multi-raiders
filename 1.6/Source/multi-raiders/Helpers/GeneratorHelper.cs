using MultiRaiders.Hediff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MultiRaiders.Helpers
{
    public class GeneratorHelper
    {
        public static List<List<T>> SplitListEvenly<T>(List<T> source, int n)
        {
            var result = new List<List<T>>(n);
            int total = source.Count;
            int minSize = total / n;
            int remainder = total % n;
            int start = 0;

            for (int i = 0; i < n; i++)
            {
                int size = minSize + (i < remainder ? 1 : 0);
                result.Add(source.GetRange(start, size));
                start += size;
            }
            return result;
        }

        public static List<Pawn> SwarmifySpawnedPawns(List<Pawn> unsortedPawns)
        {
            if (unsortedPawns.Count < RaiderSwarmCompressionSettings.Settings.MaxRealRaiders) { return unsortedPawns; }
            Dictionary<int, List<Pawn>> sortedPawns = [];
            sortedPawns.Add(0, unsortedPawns);
            return SwarmifySpawnedPawns(sortedPawns);
        }

        public static List<Pawn> SwarmifySpawnedPawns<T>(Dictionary<T, List<Pawn>> sortedPawns)
        {
            List<Pawn> outPawns = [];
            int totalRequestedPawns = sortedPawns.Values.Sum(e => e.Count);
            if (totalRequestedPawns < RaiderSwarmCompressionSettings.Settings.MaxRealRaiders && RaiderSwarmCompressionSettings.Settings.ReplaceFractionWithFakes <= 0.0f)
            {
                return [.. sortedPawns.Values.SelectMany(_ => _)];
            }

            foreach (var sp in sortedPawns)
            {
                List<Pawn> pawnsForConfig = sp.Value;

                int raidersToGenerate = sp.Value.Count;
                float thisConfigFraction = raidersToGenerate / (float)totalRequestedPawns;

                int maxRealRaiders = Math.Max(1, (int)(RaiderSwarmCompressionSettings.Settings.MaxRealRaiders * thisConfigFraction));
                int realRaiders = Math.Max(1, Math.Min(maxRealRaiders, (int)(raidersToGenerate * (1.0f - RaiderSwarmCompressionSettings.Settings.ReplaceFractionWithFakes))));
                int fakeRaiders = Math.Max(0, raidersToGenerate - realRaiders);

                //Log.Message("Raid gen option: " + sp.Key.ToStringSafe());
                //Log.Message($"Raid gen option: {MultiRaidersSettings.Settings.MaxRealRaiders} {MultiRaidersSettings.Settings.ReplaceFractionWithFakes}");
                //Log.Message($"Fraction {thisConfigFraction} realRaiders {realRaiders} fakeRaiders {fakeRaiders}");

                List<Pawn> ToGenerate = pawnsForConfig.GetRange(0, realRaiders);
                List<List<Pawn>> ListOfFakes = SplitListEvenly(pawnsForConfig.GetRange(realRaiders, fakeRaiders), realRaiders);

                for (int pawnIdx = 0; pawnIdx < realRaiders; pawnIdx++)
                {
                    Pawn pawn = ToGenerate[pawnIdx];

                    if (ListOfFakes[pawnIdx].Count > 0)
                    {
                        HediffMirrorImage mirrorHediff = (HediffMirrorImage)pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("MirrorImage"));
                        HediffComp_MirrorImage comp = mirrorHediff.TryGetComp<HediffComp_MirrorImage>();
                        comp.fakePawns = ListOfFakes[pawnIdx];
                        mirrorHediff.UpdateSeverity();
                        //Log.Message($"Adding {comp.fakePawns.Count} to real pawn");
                    }

                    outPawns.Add(pawn);
                }
            }

            return outPawns;
        }
    }
}
