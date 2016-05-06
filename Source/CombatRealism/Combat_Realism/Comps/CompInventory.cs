using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace Combat_Realism
{
    public class CompInventory : ThingComp
    {
        public CompProperties_Inventory Props
        {
            get
            {
                return (CompProperties_Inventory)this.props;
            }
        }

        private float currentWeightCached;
        private float currentBulkCached;
        public float currentWeight
        {
            get
            {
                return currentWeightCached;
            }
        }
        public float currentBulk
        {
            get
            {
                return currentBulkCached;
            }
        }
        private float availableWeight
        {
            get
            {
                return this.parentPawn.GetStatValue(StatDef.Named("CarryWeight")) - currentWeight;
            }
        }
        private float availableBulk
        {
            get
            {
                return capacityBulk - currentBulk;
            }
        }
        public float capacityBulk
        {
            get
            {
                return this.parentPawn.GetStatValue(StatDef.Named("CarryBulk"));
            }
        }
        public float capacityWeight
        {
            get
            {
                return this.parentPawn.GetStatValue(StatDef.Named("CarryWeight"));
            }
        }
        private Pawn parentPawnInt = null;
        private Pawn parentPawn
        {
            get
            {
                if (parentPawnInt == null)
                {
                    parentPawnInt = this.parent as Pawn;
                }
                return parentPawnInt;
            }
        }
        public float moveSpeedFactor
        {
            get
            {
                return Mathf.Lerp(1f, 0.75f, currentWeight / this.parentPawn.GetStatValue(StatDef.Named("CarryWeight")));
            }
        }
        public float workSpeedFactor
        {
            get
            {
                return Mathf.Lerp(1f, 0.75f, currentBulk / 40f);
            }
        }
        public float encumberPenalty
        {
            get
            {
                float penalty = 0f;
                if (availableWeight < 0)
                {
                    penalty = currentWeight / this.parentPawn.GetStatValue(StatDef.Named("CarryWeight")) - 1;
                }
                return penalty;
            }
        }
        public ThingContainer container
        {
            get
            {
                if (parentPawn.inventory != null)
                {
                    return parentPawn.inventory.container;
                }
                return null;
            }
        }
        private List<Thing> ammoListCached = new List<Thing>();
        public List<Thing> ammoList
        {
            get
            {
                return ammoListCached;
            }
        }
        private List<ThingWithComps> meleeWeaponListCached = new List<ThingWithComps>();
        public List<ThingWithComps> meleeWeaponList => meleeWeaponListCached;
        private List<ThingWithComps> rangedWeaponListCached = new List<ThingWithComps>();
        public List<ThingWithComps> rangedWeaponList => rangedWeaponListCached;
        private bool initializedLoadouts = false;
        private int ticksToInitLoadout = 5;         // Generate loadouts this many ticks after spawning

        public override void PostSpawnSetup()
        {
            base.PostSpawnSetup();
            UpdateInventory();
        }

        /// <summary>
        /// Refreshes the cached bulk and weight. Call this whenever items are added/removed from inventory
        /// </summary>
        public void UpdateInventory()
        {
            if (parentPawn == null)
            {
                Log.Error("CompInventory on non-pawn " + this.parent.ToString());
                return;
            }
            float newBulk = 0f;
            float newWeight = 0f;

            // Add equipped weapon
            if (parentPawn.equipment != null && parentPawn.equipment.Primary != null)
            {
                GetEquipmentStats(parentPawn.equipment.Primary, out newWeight, out newBulk);
            }

            // Add apparel
            if (parentPawn.apparel != null && parentPawn.apparel.WornApparelCount > 0)
            {
                foreach (Thing apparel in parentPawn.apparel.WornApparel)
                {
                    float apparelBulk = apparel.GetStatValue(StatDef.Named("WornBulk"));
                    float apparelWeight = apparel.GetStatValue(StatDef.Named("WornWeight"));
                    newBulk += apparelBulk;
                    newWeight += apparelWeight;
                }
            }

            // Add inventory items
            if (parentPawn.inventory != null && parentPawn.inventory.container != null)
            {
                ammoListCached.Clear();
                meleeWeaponListCached.Clear();
                rangedWeaponListCached.Clear();
                foreach (Thing thing in parentPawn.inventory.container)
                {
                    // Check for weapons
                    ThingWithComps eq = thing as ThingWithComps;
                    CompEquippable compEq = thing.TryGetComp<CompEquippable>();
                    if (eq != null && compEq != null)
                    {
                        if (compEq.PrimaryVerb != null)
                        {
                            rangedWeaponListCached.Add(eq);
                        }
                        else
                        {
                            meleeWeaponListCached.Add(eq);
                        }
                        // Calculate equipment weight
                        float eqWeight;
                        float eqBulk;
                        GetEquipmentStats(eq, out eqWeight, out eqBulk);
                        newWeight += eqWeight * thing.stackCount;
                        newBulk += eqBulk * thing.stackCount;
                    }
                    else
                    {
                        // Update ammo list
                        if (thing.def is AmmoDef)
                        {
                            ammoListCached.Add(thing);
                        }
                        // Add item weight
                        newBulk += thing.GetStatValue(StatDef.Named("Bulk")) * thing.stackCount;
                        newWeight += thing.GetStatValue(StatDef.Named("Weight")) * thing.stackCount;
                    }
                }
            }
            this.currentBulkCached = newBulk;
            this.currentWeightCached = newWeight;
        }

        /// <summary>
        /// Determines if and how many of an item currently fit into the inventory with regards to weight/bulk constraints.
        /// </summary>
        /// <param name="thing">Thing to check</param>
        /// <param name="count">Maximum amount of that item that can fit into the inventory</param>
        /// <param name="ignoreEquipment">Whether to include currently equipped weapons when calculating current weight/bulk</param>
        /// <param name="useApparelCalculations">Whether to use calculations for worn apparel. This will factor in equipped stat offsets boosting inventory space and use the worn bulk and weight.</param>
        /// <returns>True if one or more items fit into the inventory</returns>
        public bool CanFitInInventory(Thing thing, out int count, bool ignoreEquipment = false, bool useApparelCalculations = false)
        {
            float thingWeight;
            float thingBulk;

            if (useApparelCalculations)
            {
                thingWeight = thing.GetStatValue(StatDef.Named("WornWeight"));
                thingBulk = thing.GetStatValue(StatDef.Named("WornBulk"));
                if(thingWeight <= 0 && thingBulk <= 0)
                {
                    count = 1;
                    return true;
                }
                // Subtract the stat offsets we get from wearing this
                thingWeight -= thing.def.equippedStatOffsets.GetStatOffsetFromList(StatDef.Named("CarryWeight"));
                thingBulk -= thing.def.equippedStatOffsets.GetStatOffsetFromList(StatDef.Named("CarryBulk"));
            }
            else
            {
                thingWeight = thing.GetStatValue(StatDef.Named("Weight"));
                thingBulk = thing.GetStatValue(StatDef.Named("Bulk"));
            }
            // Subtract weight of currently equipped weapon
            float eqBulk = 0f;
            float eqWeight = 0f;
            if (ignoreEquipment && this.parentPawn.equipment != null && this.parentPawn.equipment.Primary != null)
            {
                ThingWithComps eq = this.parentPawn.equipment.Primary;
                GetEquipmentStats(eq, out eqWeight, out eqBulk);
            }
            // Calculate how many items we can fit into our inventory
            float amountByWeight = thingWeight <= 0 ? thing.stackCount : (availableWeight + eqWeight) / thingWeight;
            float amountByBulk = thingBulk <= 0 ? thing.stackCount : (availableBulk + eqBulk) / thingBulk;
            count = Mathf.FloorToInt(Mathf.Min(amountByBulk, amountByWeight, thing.stackCount));
            return count > 0;
        }

        public static void GetEquipmentStats(ThingWithComps eq, out float weight, out float bulk)
        {
            weight = eq.GetStatValue(StatDef.Named("Weight"));
            bulk = eq.GetStatValue(StatDef.Named("Bulk"));
            CompAmmoUser comp = eq.TryGetComp<CompAmmoUser>();
            if (comp != null && comp.currentAmmo != null)
            {
                weight += comp.currentAmmo.GetStatValueAbstract(StatDef.Named("Weight")) * comp.curMagCount;
                bulk += comp.currentAmmo.GetStatValueAbstract(StatDef.Named("Bulk")) * comp.curMagCount;
            }
        }

        /// <summary>
        /// Attempts to equip a weapon from the inventory, puts currently equipped weapon into inventory if it exists
        /// </summary>
        /// <param name="useFists">Whether to put the currently equipped weapon away even if no replacement is found</param>
        public void SwitchToNextViableWeapon(bool useFists = true)
        {
            ThingWithComps newEq = null;

            // Stop current job
            if (parentPawn.jobs != null)
                parentPawn.jobs.StopAll();

            // Cycle through available ranged weapons
            foreach(ThingWithComps gun in rangedWeaponListCached)
            {
                if(parentPawn.equipment == null || parentPawn.equipment.Primary != gun)
                {
                    CompAmmoUser compAmmo = gun.TryGetComp<CompAmmoUser>();
                    if (compAmmo == null
                        || !compAmmo.useAmmo
                        || compAmmo.curMagCount > 0
                        || compAmmo.hasAmmo)
                    {
                        newEq = gun;
                        break;
                    }
                }
            }
            // If no ranged weapon was found, use first available melee weapons
            if(newEq == null)
                newEq = meleeWeaponListCached.FirstOrDefault();
            
            // Equip the weapon
            if(newEq != null)
            {
                this.TrySwitchToWeapon(newEq);
            }
            else if (useFists && parentPawn.equipment?.Primary != null)
            {
                ThingWithComps oldEq;
                if (!parentPawn.equipment.TryTransferEquipmentToContainer(parentPawn.equipment.Primary, container, out oldEq))
                {
                    if (parentPawn.Position.InBounds())
                    {
                        ThingWithComps unused;
                        parentPawn.equipment.TryDropEquipment(oldEq, out unused, parentPawn.Position);
                    }
                    else
                    {
#if DEBUG
                        Log.Message("CR :: CompInventory :: SwitchToNextViableWeapon :: destroying out of bounds equipment" + oldEq.ToString());
#endif
                        if (!oldEq.Destroyed)
                        {
                            oldEq.Destroy();
                        }
                    }
                }
            }
        }

        public void TrySwitchToWeapon(ThingWithComps newEq)
        {
            if (newEq == null || parentPawn.equipment == null || !this.container.Contains(newEq) )
            {
                return;
            }
            if (parentPawn.equipment.Primary != null)
            {

                ThingWithComps oldEq;
                int count;
                if (CanFitInInventory(parentPawn.equipment.Primary, out count, true))
                {
                    parentPawn.equipment.TryTransferEquipmentToContainer(parentPawn.equipment.Primary, container, out oldEq);
                }
                else
                {
#if DEBUG
                    Log.Warning("CR :: CompInventory :: TrySwitchToWeapon :: failed to add current equipment to inventory");
#endif
                    parentPawn.equipment.MakeRoomFor(newEq);
                }
            }
            parentPawn.equipment.AddEquipment((ThingWithComps)container.Get(newEq, 1));
            if (newEq.def.soundInteract != null)
                newEq.def.soundInteract.PlayOneShot(parent.Position);
        }

        public override void CompTick()
        {
            // Initialize loadouts on first tick
            if (ticksToInitLoadout > 0)
            {
                ticksToInitLoadout--;
            }
            else if (!initializedLoadouts)
            {
                // Find all loadout generators
                List<LoadoutGeneratorThing> genList = new List<LoadoutGeneratorThing>();
                foreach(Thing thing in container)
                {
                    LoadoutGeneratorThing lGenThing = thing as LoadoutGeneratorThing;
                    if (lGenThing != null && lGenThing.loadoutGenerator != null)
                        genList.Add(lGenThing);
                }

                // Sort list by execution priority
                genList.Sort(delegate (LoadoutGeneratorThing x, LoadoutGeneratorThing y)
                {
                    return x.priority.CompareTo(y.priority);
                });

                // Generate loadouts
                foreach(LoadoutGeneratorThing thing in genList)
                {
                    thing.loadoutGenerator.GenerateLoadout(this);
                    container.Remove(thing);
                }
                initializedLoadouts = true;
            }

            base.CompTick();

            // Remove items from inventory if we're over the bulk limit
            while(availableBulk < 0 && container.Count > 0)
            {
                if (this.parent.Position.InBounds())
                {
                    Thing droppedThing;
                    container.TryDrop(container.Last(), this.parent.Position, ThingPlaceMode.Near, 1, out droppedThing);
                }
                else
                {
                    container.Remove(container.Last());
                }
            }
        }
    }
}
