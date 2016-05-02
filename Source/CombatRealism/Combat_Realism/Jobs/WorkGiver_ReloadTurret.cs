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
    public class WorkGiver_ReloadTurret : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial); ;
            }
        }

        public override bool HasJobOnThingForced(Pawn pawn, Thing t)
        {
            Building_TurretGunCR turret = t as Building_TurretGunCR;
            if (turret == null || !turret.needsReload || !pawn.CanReserveAndReach(turret, PathEndMode.ClosestTouch, Danger.Deadly) || turret.IsForbidden(pawn.Faction)) return false;
            Thing ammo = GenClosest.ClosestThingReachable(pawn.Position,
                            ThingRequest.ForDef(turret.compAmmo.selectedAmmo),
                            PathEndMode.ClosestTouch,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn),
                            80,
                            x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
            return ammo != null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            Building_TurretGunCR turret = t as Building_TurretGunCR;
            if (turret == null || !turret.allowAutomaticReload) return false;
            return HasJobOnThingForced(pawn, t);
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Building_TurretGunCR turret = t as Building_TurretGunCR;
            if (turret == null) return null;

            Thing ammo = GenClosest.ClosestThingReachable(pawn.Position,
                            ThingRequest.ForDef(turret.compAmmo.selectedAmmo),
                            PathEndMode.ClosestTouch,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn),
                            80,
                            x => !x.IsForbidden(pawn) && pawn.CanReserve(x));

            if (ammo == null) return null;
            int amountNeeded = turret.compAmmo.Props.magazineSize;
            if (turret.compAmmo.currentAmmo == turret.compAmmo.selectedAmmo) amountNeeded -= turret.compAmmo.curMagCount;
            return new Job(DefDatabase<JobDef>.GetNamed("ReloadTurret"), t, ammo) { maxNumToCarry = Mathf.Min(amountNeeded, ammo.stackCount) };
        }


    }
}
