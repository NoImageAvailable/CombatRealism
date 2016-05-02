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

            Loadout loadout = pawn.GetLoadout();
            if (loadout != null)
            {
                // Find missing items
                foreach (LoadoutSlot slot in loadout.Slots)
                {
                    int numContained = inventory.container.NumContained(slot.Def);

                    // Add currently equipped gun
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        if (pawn.equipment.Primary.def == slot.Def)
                            numContained++;
                    }

                    // Find closest thing to pick up
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
                    // Drop excess items
                    else if(numContained > slot.Count)
                    {
                        Thing thing = inventory.container.FirstOrDefault(x => x.def == slot.Def);
                        if (thing != null)
                        {
                            Thing droppedThing;
                            if (inventory.container.TryDrop(thing, pawn.Position, ThingPlaceMode.Near, numContained - slot.Count, out droppedThing))
                            {
                                if (droppedThing != null)
                                {
                                    return HaulAIUtility.HaulToStorageJob(pawn, droppedThing);
                                }
                                else
                                {
                                    Log.Error(pawn.ToString() + " tried dropping " + thing.ToString() + " from loadout but resulting thing is null");
                                }
                            }
                        }
                    }
                }
                /*
                // Remove items not in the loadout
                if(pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Tame)
                {
                    Thing thingToRemove = inventory.container.FirstOrDefault(t => 
                    (t.def.ingestible == null || t.def.ingestible.preferability > FoodPreferability.Raw) 
                    && !loadout.Slots.Any(s => s.Def == t.def));
                    if (thingToRemove != null)
                    {
                        Thing droppedThing;
                        if (inventory.container.TryDrop(thingToRemove, pawn.Position, ThingPlaceMode.Near, thingToRemove.stackCount, out droppedThing))
                        {
                            return HaulAIUtility.HaulToStorageJob(pawn, droppedThing);
                        }
                        else
                        {
                            Log.Error(pawn.ToString() + " tried dropping " + thingToRemove.ToString() + " from inventory but resulting thing is null");
                        }
                    }
                }
                */
            }
            return null;
        }
    }
}
