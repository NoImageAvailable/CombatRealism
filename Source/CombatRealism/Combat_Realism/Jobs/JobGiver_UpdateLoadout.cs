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
    public class JobGiver_UpdateLoadout : ThinkNode_JobGiver
    {
        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            // Get inventory
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory == null)
                return null;

            // Find missing items
            Loadout loadout = pawn.GetLoadout();
            if (loadout != null)
            {
                foreach (LoadoutSlot slot in loadout.Slots)
                {
                    int numContained = inventory.container.NumContained(slot.Def);
                    if (numContained < slot.Count)
                    {
                        Thing thing = GenClosest.ClosestThingReachable(pawn.Position,
                            ThingRequest.ForDef(slot.Def),
                            PathEndMode.ClosestTouch,
                            TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                            80,
                            x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
                        if (thing != null)
                        {
                            int maxFit;
                            if (inventory.CanFitInInventory(thing, out maxFit))
                            {
                                return new Job(JobDefOf.TakeInventory, thing) { maxNumToCarry = Mathf.Min(thing.stackCount, slot.Count - numContained, maxFit) };
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
