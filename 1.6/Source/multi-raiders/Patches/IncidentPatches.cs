using HarmonyLib;
using MultiRaiders.Hediff;
using MultiRaiders.Helpers;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MultiRaiders.Patches
{
    public class IncidentPatches
    {
        [HarmonyPatch(typeof(RaidStrategyWorker), nameof(RaidStrategyWorker.SpawnThreats))]
        public static class RaidStrategyWorker_SpawnThreats_Patch
        {
            public static bool Prefix(ref List<Pawn> __result, RaidStrategyWorker __instance, IncidentParms parms)
            {
                if (parms.pawnKind == null) return true;
                if (parms.pawnCount == 0) return true;

                int raidersToGenerate = parms.pawnCount;
                int maxRaiders = RaiderSwarmCompressionSettings.Settings.MaxRealRaiders;

                int realRaiders = Math.Min(maxRaiders, (int)(parms.pawnCount * (1.0f - RaiderSwarmCompressionSettings.Settings.ReplaceFractionWithFakes)));
                int fakeRaiders = Math.Max(0, raidersToGenerate - realRaiders);

                List<Pawn> allPawns = [];
                for (int i = 0; i < raidersToGenerate; i++)
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
                        allPawns.Add(pawn);
                    }
                }

                if (allPawns.Any())
                {
                    List<Pawn> realPawns = GeneratorHelper.SwarmifySpawnedPawns(allPawns);
                    parms.raidArrivalMode.Worker.Arrive(realPawns, parms);
                    __result = realPawns;
                    return false;
                }

                __result = null;
                return false;
            }
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
                        array[num] = faction != null ? faction.ToString() : null;
                        array[2] = " with ";
                        array[3] = parms.points.ToString();
                        array[4] = ". Defaulting to a single random cheap group.";
                        Log.Error(string.Concat(array));
                    }
                    return false;
                }

                bool allowFood = parms.raidStrategy == null || parms.raidStrategy.pawnsCanBringFood || parms.faction != null && !parms.faction.HostileTo(Faction.OfPlayer);
                bool firstPawnWasGenerated = false;

                bool forceGenerateNewPawn = false;
                bool allowDead = false;

                Predicate<Pawn> validatorPreGear = parms.raidStrategy != null ? ((Pawn p) => parms.raidStrategy.Worker.CanUsePawn(parms.points, p, outPawns)) : null;
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

                outPawns.AddRange(GeneratorHelper.SwarmifySpawnedPawns(sortedPawns));
                return false;
            }
        }

        [HarmonyPatch(typeof(PawnGroupKindWorker_Shamblers), "GeneratePawns")]
        public static class PawnGroupKindWorker_Shamblers_GeneratePawns_Patch
        {
            public static bool Prefix(PawnGroupKindWorker_Shamblers __instance, PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
            {
                if (!__instance.CanGenerateFrom(parms, groupMaker))
                {
                    if (errorOnZeroResults)
                    {
                        string[] array = new string[5];
                        array[0] = "Cannot generate pawns for ";
                        int num = 1;
                        Faction faction = parms.faction;
                        array[num] = faction != null ? faction.ToString() : null;
                        array[2] = " with ";
                        array[3] = parms.points.ToString();
                        array[4] = ". Defaulting to a single random cheap group.";
                        Log.Error(string.Concat(array));
                    }
                    return false;
                }

                float totalRaidPoints = parms.points;
                float minPoints = groupMaker.options.Min((opt) => opt.Cost);

                Dictionary<PawnGenOption, List<Pawn>> sortedPawns = [];
                while (totalRaidPoints > minPoints)
                {
                    PawnGenOption pawnGenOption;
                    groupMaker.options.TryRandomElementByWeight((gr) => gr.selectionWeight, out pawnGenOption);
                    if (pawnGenOption.Cost <= totalRaidPoints)
                    {
                        totalRaidPoints -= pawnGenOption.Cost;
                        DevelopmentalStage developmentalStage = DevelopmentalStage.Adult;
                        if (Find.Storyteller.difficulty.ChildrenAllowed && Find.Storyteller.difficulty.childShamblersAllowed)
                        {
                            developmentalStage |= DevelopmentalStage.Child;
                        }
                        PawnKindDef kind = pawnGenOption.kind;
                        Faction faction2 = parms.faction;
                        PawnGenerationContext pawnGenerationContext = PawnGenerationContext.NonPlayer;
                        DevelopmentalStage developmentalStage2 = developmentalStage;
                        FloatRange floatRange = new(0f, 8f);
                        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction2, pawnGenerationContext, null, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, developmentalStage2, null, floatRange, null, false, false, false, -1, 0, false));

                        if (!sortedPawns.ContainsKey(pawnGenOption)) sortedPawns.Add(pawnGenOption, []);
                        sortedPawns[pawnGenOption].Add(pawn);
                    }
                }

                outPawns.AddRange(GeneratorHelper.SwarmifySpawnedPawns(sortedPawns));
                return false;
            }
        }

        [HarmonyPatch(typeof(AggressiveAnimalIncidentUtility), nameof(AggressiveAnimalIncidentUtility.GenerateAnimals), [typeof(PawnKindDef), typeof(PlanetTile), typeof(float), typeof(int)])]
        public static class AggressiveAnimalIncidentUtility_GenerateAnimals_Patch
        {
            public static void Postfix(ref List<Pawn> __result, PawnKindDef animalKind, PlanetTile tile, float points, int animalCount = 0)
            {
                __result = GeneratorHelper.SwarmifySpawnedPawns(__result);
            }
        }

        [HarmonyPatch(typeof(AggressiveAnimalIncidentUtility), nameof(AggressiveAnimalIncidentUtility.GenerateAnimals), [typeof(List<PawnKindDef>), typeof(PlanetTile)])]
        public static class AggressiveAnimalIncidentUtility_GenerateAnimals2_Patch
        {
            public static void Postfix(ref List<Pawn> __result, List<PawnKindDef> animalKinds, PlanetTile tile)
            {
                __result = GeneratorHelper.SwarmifySpawnedPawns(__result);
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff), [typeof(HediffDef), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)])]
        public static class Pawn_HealthTracker_AddHediff_Patch
        {
            public static void Postfix(ref Verse.Hediff __result, Pawn_HealthTracker __instance, HediffDef def, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
            {
                if (def == HediffDefOf.Scaria || def == HediffDefOf.ScariaInfection)
                {
                    Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
                    HediffMirrorImage mirrorImage = pawn.health.hediffSet.GetFirstHediff<HediffMirrorImage>();
                    if (mirrorImage != null && mirrorImage.FakePawns.Count > 0)
                    {
                        foreach (Pawn fakePawn in mirrorImage.FakePawns)
                        {
                            fakePawn.health.AddHediff(def, part, dinfo, result);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff), [typeof(Verse.Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)])]
        public static class Pawn_HealthTracker_AddHediff2_Patch
        {
            public static void Postfix(Pawn_HealthTracker __instance, Verse.Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
            {
                if (hediff.def == HediffDefOf.Scaria || hediff.def == HediffDefOf.ScariaInfection)
                {
                    Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
                    HediffMirrorImage mirrorImage = pawn.health.hediffSet.GetFirstHediff<HediffMirrorImage>();
                    if (mirrorImage != null && mirrorImage.FakePawns.Count > 0)
                    {
                        foreach (Pawn fakePawn in mirrorImage.FakePawns)
                        {
                            fakePawn.health.AddHediff(hediff, part, dinfo, result);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Hediff_Scaria), "get_IsBerserk")]
        public static class Hediff_Scaria_get_IsBerserk_Patch
        {
            public static bool Prefix(Hediff_Scaria __instance, ref bool __result)
            {
                //Log.Message($"Instance: {__instance}");
                //Log.Message($"Pawn: {__instance.pawn} MentalStateHandler: {__instance.pawn?.mindState?.mentalStateHandler}");

                if (__instance.pawn?.mindState?.mentalStateHandler == null) { 
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}
