using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace Combat_Realism
{
    // Cloned from Verse.Command_VerbTarget which is internal for no goddamn reason
    public class Command_VerbTarget : Command
    {
        public Verb verb;

        protected override Color IconDrawColor
        {
            get
            {
                if (this.verb.ownerEquipment != null)
                {
                    return this.verb.ownerEquipment.DrawColor;
                }
                return base.IconDrawColor;
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.TickTiny.PlayOneShotOnCamera();
            Targeter targeter = Find.Targeter;
            if (this.verb.CasterIsPawn && targeter.targetingVerb != null && targeter.targetingVerb.verbProps == this.verb.verbProps)
            {
                Pawn casterPawn = this.verb.CasterPawn;
                if (!targeter.IsPawnTargeting(casterPawn))
                {
                    targeter.targetingVerbAdditionalPawns.Add(casterPawn);
                }
            }
            else
            {
                Find.Targeter.BeginTargeting(this.verb);
            }
        }
    }
}
