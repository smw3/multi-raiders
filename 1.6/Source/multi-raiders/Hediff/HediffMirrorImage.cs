using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using static UnityEngine.Scripting.GarbageCollector;

namespace MultiRaiders.Hediff
{
    public class HediffMirrorImage : HediffWithComps
    {
        internal bool wasDowned;

        public List<Pawn> FakePawns
        {
            get
            {
                return GetComp<HediffComp_MirrorImage>().fakePawns;
            }
        }

        public void UpdateSeverity()
        {
            this.Severity = Math.Min(1.0f, FakePawns.Count * 0.25f);
        }

        private void KillFake()
        {
            int idx = FakePawns.Count - 1;
            Pawn fakePawn = FakePawns.Pop();
            UpdateSeverity();

            BloodEffect(idx);
            MaybeDropItems(fakePawn);        

            if (FakePawns.Count <= 0)
            {
                this.Severity = 0f;
            }
        }

        private void MaybeDropItems(Pawn fakePawn)
        {
            if (MultiRaidersSettings.Settings.FakesDropGear)
            {
                GenSpawn.Spawn(fakePawn, pawn.Position, pawn.Map, WipeMode.VanishOrMoveAside);
                fakePawn.Kill(null);
            }
        }

        private void BloodEffect(int value)
        {
            float tickOffset = MirrorImageHelper.GetTickOffsetForPawn(pawn, value);            
            IntVec3 pos = pawn.Position + IntVec3.FromVector3(MirrorImageHelper.GetSwirlOffset(tickOffset, 1.0f));

            ThingDef thingDef = (this.pawn.IsMutant ? (this.pawn.mutant.Def.bloodDef ?? this.pawn.RaceProps.BloodDef) : this.pawn.RaceProps.BloodDef);
            GenSpawn.SpawnIrregularLump(thingDef, pos, pawn.Map, new IntRange(1 * Math.Max(1, (int)pawn.BodySize), 5 * Math.Max(1, (int)pawn.BodySize)), new IntRange(2, 4), WipeMode.Vanish, null, null, null, null, null);
        }

        public bool ShouldDown()
        {
            if (FakePawns.Count > 0) {
                return false;
            }
            return true;
        }
        public void ApplyFakeDownConsequences()
        {
            if (wasDowned && !ShouldDown())
            {
                HealToFull();
                wasDowned = false;
            }
        }

        private void HealToFull()
        {
            KillFake();
            while (HealthUtility.FixWorstHealthCondition(pawn, []) != null);            
            
            EnsureWeaponEquipped();
        }

        private void EnsureWeaponEquipped()
        {
            // Not entirely sure how to handle yet, if it needs to be handled
            /*if (pawn.equipment.Primary == null)
            {
                Log.Message($"Pawn {pawn.Name} has no primary");
                ThingDef newWep = primaryWeapon.def;

                ThingRequest thingRequest = ThingRequest.ForGroup(ThingRequestGroup.Weapon);
                Thing newWeapon = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                    thingRequest, PathEndMode.Touch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false, true),
                    1.9f, (Thing t) => EquipmentUtility.CanEquip(t, pawn), null, 0, -1, false, RegionType.Set_Passable, false, false);
                pawn.equipment.AddEquipment((ThingWithComps)newWeapon);

                Log.Message($"  new primary {pawn.equipment.Primary}");
            }*/
        }

    }
}
