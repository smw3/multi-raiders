using HarmonyLib;
using MultiRaiders.Hediff;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace MultiRaiders
{
    public class HarmonyPatches
    {
        [HarmonyPatch(typeof(RaidStrategyWorker), nameof(RaidStrategyWorker.SpawnThreats))]
        public static class RaidStrategyWorker_SpawnThreats_Patch
        {
            public static bool Prefix(ref List<Pawn> __result, RaidStrategyWorker __instance, IncidentParms parms)
            {
                if (parms.pawnKind == null) return true;
                if (parms.pawnCount == 0) return true;

                int raidersToGenerate = parms.pawnCount;
                int maxRaiders = MultiRaidersSettings.Settings.MaxRealRaiders;

                int realRaiders = Math.Min(maxRaiders, (int)(parms.pawnCount * (1.0f - MultiRaidersSettings.Settings.ReplaceFractionWithFakes)));
                int fakeRaiders = Math.Max(0, raidersToGenerate - realRaiders);

                List<Pawn> realPawns = [];
                for (int i = 0; i < realRaiders; i++)
                {
                    PawnKindDef pawnKind = parms.pawnKind;
                    Faction faction = parms.faction;
                    PawnGenerationContext pawnGenerationContext = PawnGenerationContext.NonPlayer;
                    float biocodeWeaponsChance = parms.biocodeWeaponsChance;
                    float biocodeApparelChance = parms.biocodeApparelChance;
                    bool pawnsCanBringFood = __instance.def.pawnsCanBringFood;
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, pawnGenerationContext, null, false, false, false, true, true, 1f, false, true, false, pawnsCanBringFood, true, false, false, false, false, biocodeWeaponsChance, biocodeApparelChance, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false)
                    {
                        BiocodeApparelChance = 1f
                    });
                    if (pawn != null)
                    {
                        realPawns.Add(pawn);
                    }
                }

                Dictionary<Pawn, List<Pawn>> fakePawns = [];
                for (int i = 0; i < fakeRaiders; i++)
                {
                    Pawn parentPawn = realPawns[i % realPawns.Count];

                    PawnKindDef pawnKind = parms.pawnKind;
                    Faction faction = parms.faction;
                    PawnGenerationContext pawnGenerationContext = PawnGenerationContext.NonPlayer;
                    float biocodeWeaponsChance = parms.biocodeWeaponsChance;
                    float biocodeApparelChance = parms.biocodeApparelChance;
                    bool pawnsCanBringFood = __instance.def.pawnsCanBringFood;
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, pawnGenerationContext, null, false, false, false, true, true, 1f, false, true, false, pawnsCanBringFood, true, false, false, false, false, biocodeWeaponsChance, biocodeApparelChance, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false)
                    {
                        BiocodeApparelChance = 1f
                    });
                    if (pawn != null)
                    {
                        if (!fakePawns.ContainsKey(parentPawn)) fakePawns.Add(parentPawn, []);
                        fakePawns[parentPawn].Add(pawn);
                    }
                }

                foreach (Pawn pawn in realPawns)
                {
                    if (!fakePawns.ContainsKey(pawn)) continue;
                    HediffMirrorImage mirrorHediff = (HediffMirrorImage)pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("MirrorImage"));
                    HediffComp_MirrorImage comp = mirrorHediff.TryGetComp<HediffComp_MirrorImage>();
                    comp.fakePawns = fakePawns[pawn];
                    mirrorHediff.UpdateSeverity();
                }

                if (realPawns.Any<Pawn>())
                {
                    parms.raidArrivalMode.Worker.Arrive(realPawns, parms);
                    __result = realPawns;
                    return false;
                }

                __result = null;
                return false;
            }
        }
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

        [HarmonyPatch(typeof(PawnGroupKindWorker_Normal), "GeneratePawns")]
        public static class PawnGroupKindWorker_GeneratePawns_Patch
        {
            public static bool Prefix(PawnGroupKindWorker_Normal __instance, PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
            {
                if (!__instance.CanGenerateFrom(parms, groupMaker))
                {
                    if (errorOnZeroResults)
                    {
                        string[] array = new string[5];
                        array[0] = "Cannot generate pawns for ";
                        int num = 1;
                        Faction faction = parms.faction;
                        array[num] = ((faction != null) ? faction.ToString() : null);
                        array[2] = " with ";
                        array[3] = parms.points.ToString();
                        array[4] = ". Defaulting to a single random cheap group.";
                        Log.Error(string.Concat(array));
                    }
                    return false;
                }

                bool allowFood = parms.raidStrategy == null || parms.raidStrategy.pawnsCanBringFood || (parms.faction != null && !parms.faction.HostileTo(Faction.OfPlayer));
                bool firstPawnWasGenerated = false;

                bool forceGenerateNewPawn = false;
                bool allowDead = false;

                Predicate<Pawn> validatorPreGear = ((parms.raidStrategy != null) ? ((Pawn p) => parms.raidStrategy.Worker.CanUsePawn(parms.points, p, outPawns)) : null);
                Dictionary<PawnGenOptionWithXenotype, List<Pawn>> sortedPawns = [];

                foreach (PawnGenOptionWithXenotype pawnGenOptionWithXenotype in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(parms.points, groupMaker.options, parms))
                {
                    PawnKindDef kind = pawnGenOptionWithXenotype.Option.kind;
                    Faction faction2 = parms.faction;
                    PawnGenerationContext pawnGenerationContext = PawnGenerationContext.NonPlayer;
                    Ideo ideo = parms.ideo;
                    XenotypeDef xenotype = pawnGenOptionWithXenotype.Xenotype;
                    PlanetTile? planetTile = new PlanetTile?(parms.tile);
                    
                    bool inhabitants = parms.inhabitants;
                    PawnGenerationRequest pawnGenerationRequest = new PawnGenerationRequest(kind, faction2, pawnGenerationContext, planetTile, forceGenerateNewPawn, allowDead, parms.faction.deactivated, true, true, 1f, false, true, true, allowFood, true, inhabitants, false, false, false, 0f, 0f, null, 1f, null, validatorPreGear, null, null, null, null, null, null, null, null, null, ideo, false, false, false, false, null, null, xenotype, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false);
                    if (parms.raidAgeRestriction != null && parms.raidAgeRestriction.Worker.ShouldApplyToKind(pawnGenOptionWithXenotype.Option.kind))
                    {
                        pawnGenerationRequest.BiologicalAgeRange = parms.raidAgeRestriction.ageRange;
                        pawnGenerationRequest.AllowedDevelopmentalStages = parms.raidAgeRestriction.developmentStage;
                    }
                    if (pawnGenOptionWithXenotype.Option.kind.pawnGroupDevelopmentStage != null)
                    {
                        pawnGenerationRequest.AllowedDevelopmentalStages = pawnGenOptionWithXenotype.Option.kind.pawnGroupDevelopmentStage.Value;
                    }
                    if (!Find.Storyteller.difficulty.ChildRaidersAllowed && parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer))
                    {
                        pawnGenerationRequest.AllowedDevelopmentalStages = DevelopmentalStage.Adult;
                    }
                    Pawn pawn = PawnGenerator.GeneratePawn(pawnGenerationRequest);
                    if (parms.forceOneDowned && !firstPawnWasGenerated)
                    {
                        pawn.health.forceDowned = true;
                        if (pawn.guest != null)
                        {
                            pawn.guest.Recruitable = true;
                        }
                        pawn.mindState.canFleeIndividual = false;
                        firstPawnWasGenerated = true;
                    }
                    if (!sortedPawns.ContainsKey(pawnGenOptionWithXenotype)) sortedPawns.Add(pawnGenOptionWithXenotype, []);

                    sortedPawns[pawnGenOptionWithXenotype].Add(pawn);
                }

                int totalRequestedPawns = sortedPawns.Values.Sum(e => e.Count);
                //Log.Message($"Total raid pawns {totalRequestedPawns}");

                foreach (var sp in sortedPawns)
                {
                    List<Pawn> pawnsForConfig = sp.Value;

                    int raidersToGenerate = sp.Value.Count;
                    float thisConfigFraction = (float)raidersToGenerate / (float)totalRequestedPawns;

                    int maxRealRaiders = Math.Max(1, (int)(MultiRaidersSettings.Settings.MaxRealRaiders * thisConfigFraction));
                    int realRaiders = Math.Min(maxRealRaiders, (int)(raidersToGenerate * (1.0f - MultiRaidersSettings.Settings.ReplaceFractionWithFakes)));
                    int fakeRaiders = Math.Max(0, raidersToGenerate - realRaiders);

                    //Log.Message("Raid gen option: " + sp.Key.ToStringSafe());
                    //Log.Message($"Fraction {thisConfigFraction} realRaiders {realRaiders} fakeRaiders {fakeRaiders}");

                    List<Pawn> ToGenerate = pawnsForConfig.GetRange(0, realRaiders);
                    List<List<Pawn>> ListOfFakes = SplitListEvenly(pawnsForConfig.GetRange(realRaiders, fakeRaiders), realRaiders);

                    for (int pawnIdx = 0; pawnIdx < realRaiders; pawnIdx++)
                    {
                        Pawn pawn = ToGenerate[pawnIdx];

                        HediffMirrorImage mirrorHediff = (HediffMirrorImage)pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("MirrorImage"));
                        HediffComp_MirrorImage comp = mirrorHediff.TryGetComp<HediffComp_MirrorImage>();
                        comp.fakePawns = ListOfFakes[pawnIdx];
                        mirrorHediff.UpdateSeverity();
                        //Log.Message($"Adding {comp.fakePawns.Count} to real pawn");

                        outPawns.Add(pawn);
                    }
                }
                //Log.Message($"Generated {outPawns.Count} real pawns");
                
                return false;
            }
        }

        [HarmonyPatch(typeof(Thing), "TakeDamage")]
        public class TakeDamagePatch
        {
            public static bool Prefix(Thing __instance, ref DamageInfo dinfo)
            {
                if (__instance is not Pawn pawn) return true;
                if (dinfo.Def == DamageDefOf.Bomb || dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.ToxGas)
                {
                    if (pawn.health == null) return true;
                    HediffMirrorImage mirrorImage = pawn.health.hediffSet.GetFirstHediff<HediffMirrorImage>();
                    dinfo.SetAmount(dinfo.Amount * (1.0f + mirrorImage.FakePawns.Count));
                }
                return true;
            }
        }

                [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
        public static class Pawn_HealthTracker_ShouldBeDowned_Patch
        {
            private static Lazy<FieldInfo> _effectivePawn = new(() => AccessTools.Field(typeof(Pawn_HealthTracker), "pawn"));
            public static void Postfix(ref bool __result, Pawn_HealthTracker __instance)
            {
                Pawn pawn = (Pawn)_effectivePawn.Value.GetValue(__instance);
                HediffMirrorImage mirrorImage = pawn.health.hediffSet.GetFirstHediff<HediffMirrorImage>();
                if (mirrorImage == null) return;

                mirrorImage.wasDowned = __result;
                if (__result)
                {
                    if (!mirrorImage.ShouldDown())
                    {
                        __result = false;
                    }
                }
                return;
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "CheckForStateChange")]
        public static class Pawn_HealthTracker_CheckForStateChange_Patch
        {
            private static Lazy<FieldInfo> _effectivePawn = new(() => AccessTools.Field(typeof(Pawn_HealthTracker), "pawn"));
            public static void Postfix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Verse.Hediff hediff)
            {
                Pawn pawn = (Pawn)_effectivePawn.Value.GetValue(__instance);
                HediffMirrorImage mirrorImage = pawn.health.hediffSet.GetFirstHediff<HediffMirrorImage>();
                if (mirrorImage == null) return;

                mirrorImage.ApplyFakeDownConsequences();
            }
        }
    }
}
