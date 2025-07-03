using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MultiRaiders.Hediff
{
    public class HediffCompProperties_MirrorImage : HediffCompProperties
    {
        public GraphicData graphicData;

        public HediffCompProperties_MirrorImage()
        {
            this.compClass = typeof(HediffComp_MirrorImage);
        }
    }
}
