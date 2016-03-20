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
    class JobGiver_RunForCover : ThinkNode_JobGiver
    {
        private const float maxCoverDist = 10f; //Maximum distance to run for cover to

        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            //Calculate cover position
            CompSuppressable comp = pawn.TryGetComp<CompSuppressable>();
            if (comp == null)
            {
                return null;
            }
            float distToSuppressor = (pawn.Position - comp.suppressorLoc).LengthHorizontal;
            Verb verb = pawn.TryGetAttackVerb(!pawn.IsColonist);
            IntVec3 coverPosition;

            //Try to find cover position to move up to
            if (!this.GetCoverPositionFrom(pawn, comp.suppressorLoc, maxCoverDist, out coverPosition))
            {
                return null;
            }

            //Sanity check
            if (pawn.Position == coverPosition)
            {
                return null;
            }

            //Tell pawn to move to position
            Find.PawnDestinationManager.ReserveDestinationFor(pawn, coverPosition);
            return new Job(DefDatabase<JobDef>.GetNamed("RunForCover", true), coverPosition)
            {
                locomotionUrgency = LocomotionUrgency.Sprint,
                playerForced = true
            };
        }

        private bool GetCoverPositionFrom(Pawn pawn, IntVec3 fromPosition, float maxDist, out IntVec3 coverPosition)
        {
            //First check if we have cover already
            Vector3 coverVec = (fromPosition - pawn.Position).ToVector3().normalized;
            IntVec3 coverCell = (pawn.Position.ToVector3Shifted() + coverVec).ToIntVec3();
            Thing cover = coverCell.GetCover();
            if (pawn.Position.Standable() && cover != null && !pawn.Position.ContainsStaticFire())
            {
                coverPosition = pawn.Position;
                return true;
            }

            List<IntVec3> cellList = new List<IntVec3>(GenRadial.RadialCellsAround(pawn.Position, maxDist, true));
            IntVec3 nearestPosWithPlantCover = IntVec3.Invalid; //Store the nearest position with plant cover here as a fallback in case we find no hard cover

            //Go through each cell in radius around the pawn
            foreach (IntVec3 cell in cellList)
            {
                //Sanity checks
                if (cell.IsValid 
                    && cell.Standable() 
                    && !Find.PawnDestinationManager.DestinationIsReserved(cell)
                    && pawn.CanReach(cell, PathEndMode.ClosestTouch, Danger.Deadly, false)
                    && !cell.FireNearby())
                {
                    coverVec = (fromPosition - cell).ToVector3().normalized;    //The direction in which we want to have cover
                    coverCell = (cell.ToVector3Shifted() + coverVec).ToIntVec3();   //The cell we check for cover
                    cover = coverCell.GetCover();
                    if (cover != null)
                    {
                        //If the cover is a plant we store the location for later
                        if (cover.def.category == ThingCategory.Plant && nearestPosWithPlantCover == IntVec3.Invalid)
                        {
                            nearestPosWithPlantCover = cell;
                        }
                        //The cell has hard cover in the direction we want, so we return the cell and report success
                        else
                        {
                            coverPosition = cell;
                            return true;
                        }
                    }
                }
            }
            //No hard cover to move up to, use nearest plant cover instead and report success
            if (nearestPosWithPlantCover.IsValid)
            {
                coverPosition = nearestPosWithPlantCover;
                return true;
            }

            //No hard nor plant cover in the radius, report failure
            coverPosition = IntVec3.Invalid;
            return false;
        }
    }
}
