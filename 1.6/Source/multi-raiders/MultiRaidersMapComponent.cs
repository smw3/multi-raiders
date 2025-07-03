using MultiRaiders.Hediff;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MultiRaiders
{
    public class MultiRaidersMapComponent(Verse.Map map) : MapComponent(map)
    {
        private Dictionary<Pawn, (HediffMirrorImage hediff, HediffComp_MirrorImage comp)> mirrorCache = new();

        private int lastCacheUpdateTick = -1;
        private int numEntitiesSinceLastUpdate = -1;
        private const int CacheUpdateInterval = 60;

        public override void MapComponentUpdate()
        {
            if (Find.TickManager.TicksGame - lastCacheUpdateTick > CacheUpdateInterval || numEntitiesSinceLastUpdate != map.mapPawns.AllPawnsSpawned.Count)
            {
                UpdateMirrorCache();
                lastCacheUpdateTick = Find.TickManager.TicksGame;
            }

            foreach (var kvp in mirrorCache)
            {
                Pawn pawn = kvp.Key;
                var (mirrorImage, mirrorImageComp) = kvp.Value;
                if (pawn != null && pawn.Spawned && mirrorImageComp != null)
                {
                    mirrorImageComp.DrawAt(pawn.TrueCenter());
                }
            }
        }

        private void UpdateMirrorCache()
        {
            mirrorCache.Clear();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                HediffMirrorImage mirrorImage = pawn.health.hediffSet.GetFirstHediff<HediffMirrorImage>();
                if (mirrorImage == null) continue;
                HediffComp_MirrorImage mirrorImageComp = mirrorImage.GetComp<HediffComp_MirrorImage>();
                if (mirrorImageComp != null)
                    mirrorCache[pawn] = (mirrorImage, mirrorImageComp);
            }
            numEntitiesSinceLastUpdate = map.mapPawns.AllPawnsSpawned.Count;
        }
    }
}
