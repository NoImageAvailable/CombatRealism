using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class Verb_MarkForArtilleryCR : Verb_LaunchProjectileCR
    {
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (ShooterPawn != null && ShooterPawn.skills != null)
            {
                ShooterPawn.skills.Learn(SkillDefOf.Shooting, 200);
            }
        }

        protected override bool TryCastShot()
        {
            ArtilleryMarker marker = ThingMaker.MakeThing(ThingDef.Named(ArtilleryMarker.MarkerDef)) as ArtilleryMarker;
            ShiftVecReport report = this.ShiftVecReportFor(this.currentTarget);
            marker.aimEfficiency = report.aimEfficiency;
            marker.aimingAccuracy = report.aimingAccuracy;
            marker.lightingShift = report.lightingShift;
            marker.weatherShift = report.weatherShift;

            GenSpawn.Spawn(marker, this.currentTarget.Cell);

            // Check for something to attach marker to
            if (this.currentTarget.HasThing)
            {
                CompAttachBase comp = this.currentTarget.Thing.TryGetComp<CompAttachBase>();
                if (comp != null)
                {
                    marker.AttachTo(this.currentTarget.Thing);
                }
            }
            return true;
        }
    }
}
