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
        private enum ItemPriority : byte
        {
            None,
            Low,
            LowStock,
            Proximity
        }

        public override float GetPriority(Pawn pawn)
        {
            ItemPriority priority;
            Thing unused;
            int i;
            LoadoutSlot slot = GetPrioritySlot(pawn, out priority, out unused, out i);
            if (slot == null)
            {
                if (CheckForExcessItems(pawn)) return 3f;
                return 0f;
            }
            if (priority == ItemPriority.Low) return 3f;

            TimeAssignmentDef assignment = (pawn.timetable != null) ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;
            if (assignment == TimeAssignmentDefOf.Sleep) return 3f;

            return 9.2f;
        }

        private LoadoutSlot GetPrioritySlot(Pawn pawn, out ItemPriority priority, out Thing closestThing, out int count)
        {
            priority = ItemPriority.None;
            LoadoutSlot slot = null;
            closestThing = null;
            count = 0;

            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory != null && inventory.container != null)
            {
                Loadout loadout = pawn.GetLoadout();
                if (loadout != null && !loadout.Slots.NullOrEmpty())
                {
                    foreach(LoadoutSlot curSlot in loadout.Slots)
                    {
                        Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: checking slot " + curSlot.ToString());
                        ItemPriority curPriority = ItemPriority.None;
                        Thing curThing = null;
                        int numCarried = inventory.container.NumContained(curSlot.Def);

                        // Add currently equipped gun
                        Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: checking gun");
                        if (pawn.equipment != null && pawn.equipment.Primary != null)
                        {
                            if (pawn.equipment.Primary.def == curSlot.Def) numCarried++;
                        }

                        Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: calculating priority");
                        if (numCarried < curSlot.Count)
                        {
                            Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: checking for proximity");
                            curThing = GenClosest.ClosestThingReachable(
                                pawn.Position,
                                ThingRequest.ForDef(curSlot.Def),
                                PathEndMode.ClosestTouch,
                                TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                                15,
                                x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
                            if (curThing != null) curPriority = ItemPriority.Proximity;
                            else
                            {
                                Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: checking for availability");
                                curThing = GenClosest.ClosestThingReachable(
                                    pawn.Position,
                                    ThingRequest.ForDef(curSlot.Def),
                                    PathEndMode.ClosestTouch,
                                    TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                                    80,
                                    x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
                                if (curThing != null)
                                {
                                    Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: checking for meal");
                                    if (!curSlot.Def.IsNutritionSource && numCarried / curSlot.Count <= 0.5f) curPriority = ItemPriority.LowStock;
                                    else curPriority = ItemPriority.Low;
                                }
                            }
                        }
                        Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: checking for update");
                        if (curPriority > priority && curThing != null && inventory.CanFitInInventory(curThing, out count))
                        {
                            Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: updating current slot");
                            priority = curPriority;
                            slot = curSlot;
                            closestThing = curThing;
                        }
                        if (priority >= ItemPriority.LowStock)
                        {
                            Log.Message("JobGiver_UpdateLoadout :: GetPrioritySlot :: breaking");
                            break;
                        }
                    }
                }
            }

            return slot;
        }

        private bool CheckForExcessItems(Pawn pawn)
        {
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            Loadout loadout = pawn.GetLoadout();
            if (inventory == null || inventory.container == null || loadout == null || loadout.Slots.NullOrEmpty()) return false;
            if (inventory.container.Count > loadout.SlotCount) return true;
            foreach(Thing thing in inventory.container)
            {
                LoadoutSlot slot = loadout.Slots.FirstOrDefault(x => x.Def == thing.def);
                if (slot == null || slot.Count > inventory.container.NumContained(thing.def)) return true;
            }
            return false;
        }

        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            // Get inventory
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory == null) return null;

            Loadout loadout = pawn.GetLoadout();
            if (loadout != null)
            {
                // Find missing items
                ItemPriority priority;
                Thing closestThing;
                int count;
                LoadoutSlot prioritySlot = GetPrioritySlot(pawn, out priority, out closestThing, out count);
                if (closestThing != null)
                {
                    int numContained = inventory.container.NumContained(prioritySlot.Def);
                    return new Job(JobDefOf.TakeInventory, closestThing) { maxNumToCarry = Mathf.Min(closestThing.stackCount, prioritySlot.Count - numContained, count) };
                }

                foreach (LoadoutSlot slot in loadout.Slots)
                {
                    int numContained = inventory.container.NumContained(slot.Def);

                    // Add currently equipped gun
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        if (pawn.equipment.Primary.def == slot.Def)
                            numContained++;
                    }
                    // Drop excess items
                    if(numContained > slot.Count)
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
            return null;
        }
    }
}
