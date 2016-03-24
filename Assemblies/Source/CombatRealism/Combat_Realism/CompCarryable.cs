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
    class CompCarryable : ThingComp
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            CompInventory inventoryComp = selPawn.TryGetComp<CompInventory>();
            if (inventoryComp != null)
            {
                String failReason = "";
                if (selPawn.CanReserve(this.parent))
                {
                    if (selPawn.CanReach(this.parent, PathEndMode.Touch, Danger.Deadly))
                    {
                        int count;
                        if (inventoryComp.CanFitInInventory(this.parent, out count))
                        {
                            FloatMenuOption takeSingle = new FloatMenuOption(("Pick up " + this.parent.Label),
                                delegate
                                {
                                    Job newJob = new Job(JobDefOf.TakeInventory, this.parent) { maxNumToCarry = 1 };
                                    newJob.playerForced = true;
                                    selPawn.drafter.TakeOrderedJob(newJob);
                                });
                            yield return takeSingle;

                            if (count > 1)
                            {
                                FloatMenuOption takeStack = new FloatMenuOption(("Pick up " + this.parent.Label + " x" + this.parent.stackCount.ToString()),
                                    delegate
                                    {
                                        Job newJob = new Job(JobDefOf.TakeInventory, this.parent) { maxNumToCarry = count };
                                        newJob.playerForced = true;
                                        selPawn.drafter.TakeOrderedJob(newJob);
                                    });
                                yield return takeStack;
                            }
                        }
                        else
                        {
                            failReason = "(Inventory is full)";
                        }
                    }
                    else
                    {
                        failReason = "(" + "NoPath".Translate() + ")";
                    }
                }
                else
                {
                    failReason = "(" + "ReservedBy".Translate() + Find.Reservations.FirstReserverOf(this.parent, selPawn.Faction) + ")";
                }
                if (!failReason.NullOrEmpty())
                {
                    FloatMenuOption failOption = new FloatMenuOption();
                    failOption.label = "Cannot pick up " + this.parent.Label + " " + failReason;
                    yield return failOption;
                }
            }
        }
    }
}
