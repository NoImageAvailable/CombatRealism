using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class CompInventory : ThingComp
    {
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
                return this.parentPawn.GetStatValue(StatDef.Named("CarryBulk")) - currentBulk;
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
                return Mathf.Lerp(0.75f, 1f, currentWeight / this.parentPawn.GetStatValue(StatDef.Named("CarryWeight")));
            }
        }
        public float workSpeedFactor
        {
            get
            {
                return Mathf.Lerp(0.75f, 1f, currentWeight / this.parentPawn.GetStatValue(StatDef.Named("CarryBulk")));
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
        private List<ThingCount> ammoListCached = new List<ThingCount>();

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.UpdateInventory();
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
                newBulk += parentPawn.equipment.Primary.GetStatValue(StatDef.Named("Bulk"));
                newWeight += parentPawn.equipment.Primary.GetStatValue(StatDef.Named("Weight"));
            }

            // Add apparel
            if (parentPawn.apparel != null && parentPawn.apparel.WornApparelCount > 0)
            {
                foreach (Thing apparel in parentPawn.apparel.WornApparel)
                {
                    float apparelBulk = apparel.GetStatValue(StatDef.Named("Bulk"));
                    float apparelWeight = apparel.GetStatValue(StatDef.Named("Weight"));
                    if (apparelBulk != StatDef.Named("Bulk").defaultBaseValue && apparelWeight != StatDef.Named("Weight").defaultBaseValue)
                    {
                        newBulk += apparelBulk;
                        newWeight += apparelWeight;
                    }
                }
            }

            // Add inventory items
            if (parentPawn.inventory != null && parentPawn.inventory.container != null)
            {
                foreach (Thing thing in parentPawn.inventory.container)
                {
                    newBulk += thing.GetStatValue(StatDef.Named("Bulk"));
                    newWeight += thing.GetStatValue(StatDef.Named("Weight"));

                    // Update ammo list
                    // -TODO-
                }
            }
            this.currentBulkCached = newBulk;
            this.currentWeightCached = newWeight;
        }

        public bool CanFitInInventory(Thing thing, out int count, bool ignoreEquipment = false, bool ignoreDefaultStats = false)
        {
            float thingWeight = thing.GetStatValue(StatDef.Named("Weight"));
            float thingBulk = thing.GetStatValue(StatDef.Named("Bulk"));

            if (ignoreDefaultStats && thingBulk != StatDef.Named("Bulk").defaultBaseValue && thingWeight != StatDef.Named("Weight").defaultBaseValue)
            {
                count = 1;
                return true;
            }

            // Equipment weight
            float eqBulk = 0f;
            float eqWeight = 0f;
            if (ignoreEquipment && this.parentPawn.equipment != null && this.parentPawn.equipment.Primary != null)
            {
                ThingWithComps eq = this.parentPawn.equipment.Primary;
                eqBulk = eq.GetStatValue(StatDef.Named("Bulk"));
                eqWeight = eq.GetStatValue(StatDef.Named("Weight"));
            }

            float amountByWeight = (availableWeight + eqWeight) / thingWeight;
            float amountByBulk = (availableBulk + eqBulk) / thingBulk;
            count = Mathf.FloorToInt(Mathf.Min(amountByBulk, amountByWeight, thing.stackCount));
            return count > 0;
        }

        public void SwitchToNextViableWeapon(bool useFists)
        {
            // -TODO-
        }

        // Placeholder - remove once UpdateInventory() is being called properly on adding/removing things from containers
        public override void CompTick()
        {
            base.CompTick();
            float lastWeight = this.currentWeightCached;
            float lastBulk = this.currentBulkCached;
            this.UpdateInventory();
            if (lastWeight != this.currentWeightCached || lastBulk != this.currentBulkCached)
            {
                Log.Error(this.parent.ToString() + " failed inventory validation");
            }
        }
    }
}
