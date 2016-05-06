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

        private const int proximitySearchRadius = 20;
        private const int maximumSearchRadius = 80;
        private const int ticksBeforeDropRaw = 7200;

        public override float GetPriority(Pawn pawn)
        {
            if (CheckForExcessItems(pawn))
            {
                return 9.2f;
            }
            ItemPriority priority;
            Thing unused;
            int i;
            LoadoutSlot slot = GetPrioritySlot(pawn, out priority, out unused, out i);
            if (slot == null)
            {
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
                        ItemPriority curPriority = ItemPriority.None;
                        Thing curThing = null;
                        int numCarried = inventory.container.NumContained(curSlot.Def);

                        // Add currently equipped gun
                        if (pawn.equipment != null && pawn.equipment.Primary != null)
                        {
                            if (pawn.equipment.Primary.def == curSlot.Def) numCarried++;
                        }
                        if (numCarried < curSlot.Count)
                        {
                            curThing = GenClosest.ClosestThingReachable(
                                pawn.Position,
                                ThingRequest.ForDef(curSlot.Def),
                                PathEndMode.ClosestTouch,
                                TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                                proximitySearchRadius,
                                x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
                            if (curThing != null) curPriority = ItemPriority.Proximity;
                            else
                            {
                                curThing = GenClosest.ClosestThingReachable(
                                    pawn.Position,
                                    ThingRequest.ForDef(curSlot.Def),
                                    PathEndMode.ClosestTouch,
                                    TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                                    maximumSearchRadius,
                                    x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
                                if (curThing != null)
                                {
                                    if (!curSlot.Def.IsNutritionSource && numCarried / curSlot.Count <= 0.5f) curPriority = ItemPriority.LowStock;
                                    else curPriority = ItemPriority.Low;
                                }
                            }
                        }
                        if (curPriority > priority && curThing != null && inventory.CanFitInInventory(curThing, out count))
                        {
                            priority = curPriority;
                            slot = curSlot;
                            closestThing = curThing;
                        }
                        if (priority >= ItemPriority.LowStock)
                        {
                            break;
                        }
                    }
                }
            }

            return slot;
        }

        private bool CheckForExcessItems(Pawn pawn)
        {
            //if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Tame) return false;
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            Loadout loadout = pawn.GetLoadout();
            if (inventory == null || inventory.container == null || loadout == null || loadout.Slots.NullOrEmpty())
            {
                return false;
            }
            if (inventory.container.Count > loadout.SlotCount + 1)
            {
                return true;
            }
            // Check to see if there is at least one loadout slot specifying currently equipped weapon
            ThingWithComps equipment = pawn.equipment?.Primary ?? null;
            if (equipment != null && !loadout.Slots.Any(slot => slot.Def == equipment.def && slot.Count >= 1))
            {
                return true;
            }

            // Go through each item in the inventory and see if its part of our loadout
            bool allowDropRaw = Find.TickManager.TicksGame > pawn.mindState.lastInventoryRawFoodUseTick + ticksBeforeDropRaw;
            foreach (Thing thing in inventory.container)
            {
                if(allowDropRaw || !thing.def.IsNutritionSource || thing.def.ingestible.preferability > FoodPreferability.Raw)
                {
                    LoadoutSlot slot = loadout.Slots.FirstOrDefault(x => x.Def == thing.def);
                    if (slot == null)
                    {
                        return true;
                    }
                    else
                    {
                        int numContained = inventory.container.NumContained(thing.def);

                        // Add currently equipped gun
                        if (pawn.equipment != null && pawn.equipment.Primary != null)
                        {
                            if (pawn.equipment.Primary.def == slot.Def)
                            {
                                numContained++;
                            }
                        }
                        if (slot.Count < numContained)
                        {
                            return true;
                        }
                    }
                }
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
                // Find and drop excess items
                foreach (LoadoutSlot slot in loadout.Slots)
                {
                    int numContained = inventory.container.NumContained(slot.Def);

                    // Add currently equipped gun
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        if (pawn.equipment.Primary.def == slot.Def)
                        {
                            numContained++;
                        }
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

                // Try drop currently equipped weapon
                if (pawn.equipment != null && pawn.equipment.Primary != null && !loadout.Slots.Any(slot => slot.Def == pawn.equipment.Primary.def && slot.Count >= 1))
                {
                    ThingWithComps droppedEq;
                    if (pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out droppedEq, pawn.Position, false))
                    {
                        return HaulAIUtility.HaulToStorageJob(pawn, droppedEq);
                    }
                }

                // Find excess items in inventory that are not part of our loadout
                bool allowDropRaw = Find.TickManager.TicksGame > pawn.mindState.lastInventoryRawFoodUseTick + ticksBeforeDropRaw;
                Thing thingToRemove = inventory.container.FirstOrDefault(t => 
                    (allowDropRaw || !t.def.IsNutritionSource || t.def.ingestible.preferability > FoodPreferability.Raw) 
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

                // Find missing items
                ItemPriority priority;
                Thing closestThing;
                int count;
                LoadoutSlot prioritySlot = GetPrioritySlot(pawn, out priority, out closestThing, out count);
                if (closestThing != null)
                {
                    // Equip gun if unarmed or current gun is not in loadout
                    if (closestThing.TryGetComp<CompEquippable>() != null
                        && (pawn.health != null && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        && (pawn.equipment == null || pawn.equipment.Primary == null || !loadout.Slots.Any(s => s.Def == pawn.equipment.Primary.def)))
                    {
                        return new Job(JobDefOf.Equip, closestThing);
                    }
                    // Take items into inventory if needed
                    int numContained = inventory.container.NumContained(prioritySlot.Def);
                    return new Job(JobDefOf.TakeInventory, closestThing) { maxNumToCarry = Mathf.Min(closestThing.stackCount, prioritySlot.Count - numContained, count) };
                }
            }
            return null;
        }
    }
}
