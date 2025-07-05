using HarmonyLib;
using MultiRaiders.Hediff;
using MultiRaiders.Helpers;
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

namespace MultiRaiders.Patches
{
    public class PawnPatches
    {
        [HarmonyPatch(typeof(Thing), "TakeDamage")]
        public class TakeDamagePatch
        {
            public static bool Prefix(Thing __instance, ref DamageInfo dinfo)
            {
                if (__instance == null || __instance is not Pawn pawn) return true;
                if (dinfo.Def == DamageDefOf.Bomb || dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.ToxGas)
                {
                    if (pawn.health == null) return true;
                    HediffMirrorImage mirrorImage = pawn.health.hediffSet.GetFirstHediff<HediffMirrorImage>();
                    if (mirrorImage == null) return true;
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

        [HarmonyPatch(typeof(Hediff_MissingPart), "PostAdd")]
        public static class Hediff_MissingPart_PostAdd_Patch
        {
            public static bool Prefix(Hediff_MissingPart __instance, DamageInfo? dinfo)
            {
                // This is bad. I don't know why this happens, so this is an ugly workaround
                if (__instance.Part == null) return false;
                return false;
            }
        }
    }
}
