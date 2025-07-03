using LudeonTK;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace AnimalGear.Debug
{
    public static class MultiRaidersDebug
    {
        private static void DoRaid(IncidentParms parms)
        {
            IncidentDef incidentDef;
            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                incidentDef = IncidentDefOf.RaidEnemy;
            }
            else
            {
                incidentDef = IncidentDefOf.RaidFriendly;
            }
            incidentDef.Worker.TryExecute(parms);
        }

        [DebugAction("Test compressed raid", "Execute raid with faction...", false, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ExecuteRaidWithFaction()
        {
            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);

            parms.forced = true;
            parms.faction = Find.FactionManager.AllFactions.Where(f => f.PlayerRelationKind == FactionRelationKind.Hostile && f.def.categoryTag == "Tribal").First();
            parms.points = 10000;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;

            DoRaid(parms);
        }
    }
}
