using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism
{
    class JobGiver_HunkerDown : ThinkNode_JobGiver
    {
        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            if (!pawn.Position.Standable() && !pawn.Position.ContainsStaticFire())
            {
                return null;
            }
            return new Job(DefDatabase<JobDef>.GetNamed("HunkerDown", true), pawn) { playerForced = true };
        }
    }
}
