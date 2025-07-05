using MultiRaiders.Graphics;
using MultiRaiders.Helpers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static System.Net.Mime.MediaTypeNames;

namespace MultiRaiders.Hediff
{
    public class HediffComp_MirrorImage : HediffComp
    {
        private HediffCompProperties_MirrorImage Props => props as HediffCompProperties_MirrorImage;

        public List<Pawn> fakePawns = [];

        public override bool CompShouldRemove
        {
            get { return base.CompShouldRemove || Pawn.Faction == Faction.OfPlayer; }
        }
        public override string CompLabelInBracketsExtra
        {
            get
            {
                return "Squad size : " + fakePawns.Count();
            }
        }

        public virtual void DrawAt(Vector3 drawPos)
        {
            int idx = 0;
            Pawn realPawn = this.parent.pawn;
            foreach (Pawn fakePawn in fakePawns)
            {
                float tickOffset = MirrorImageHelper.GetTickOffsetForPawn(fakePawn, idx);
                Vector3 offset = MirrorImageHelper.GetSwirlOffset(tickOffset, 1.0f);
                Rot4 rot = MirrorImageHelper.GetSwirlInfluencedRot4(realPawn.Rotation, MirrorImageHelper.GetSwirlDirection(tickOffset, 1.0f), 0.15f);

                if (Props.graphicData?.Graphic is MirrorImageGraphic gfx)
                {
                    gfx.SetMaterial(fakePawn, rot);
                }

                Props.graphicData?.Graphic.Draw(new Vector3(drawPos.x, AltitudeLayer.Pawn.AltitudeFor(), drawPos.z) + offset, rot, fakePawn);
                idx++;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look<Pawn>(ref this.fakePawns, "FakePawns", LookMode.Deep, []);
        }
    }
}
