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

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.UpdateInventory();
        }

        public void UpdateInventory()
        {
            if (parentPawn == null)
            {
                Log.Error("CompInventory on non-pawn " + this.parent.ToString());
                return;
            }
            float newBulk = 0f;
            float newWeight = 0f;
            if (parentPawn.equipment != null && parentPawn.equipment.Primary != null)
            {
                newBulk += parentPawn.equipment.Primary.GetStatValue(StatDef.Named("Bulk"));
                newWeight += parentPawn.equipment.Primary.GetStatValue(StatDef.Named("Weight"));
            }
            if (parentPawn.inventory != null && parentPawn.inventory.container != null)
            {
                foreach (Thing thing in parentPawn.inventory.container)
                {
                    newBulk += thing.GetStatValue(StatDef.Named("Bulk"));
                    newWeight += thing.GetStatValue(StatDef.Named("Weight"));
                }
            }
            this.currentBulkCached = newBulk;
            this.currentWeightCached = newWeight;
            Log.Message("Updated bulk: " + currentBulk.ToString() + ", " + this.parent.ToString());
            Log.Message("Updated weight: " + currentWeight.ToString() + ", " + this.parent.ToString());
        }

        public bool CanPickUpItem(Thing thing)
        {
            float availableWeight = currentWeight - this.parentPawn.GetStatValue(StatDef.Named("Weight"));
            float availableBulk = currentBulk - this.parentPawn.GetStatValue(StatDef.Named("Bulk"));
            return thing.GetStatValue(StatDef.Named("Weight")) <= availableWeight && thing.GetStatValue(StatDef.Named("Bulk")) <= availableBulk;
        }

        public float GetMoveSpeedFactor()
        {
            return Mathf.Lerp(0.5f, 1f, 1 - currentWeight / this.parentPawn.GetStatValue(StatDef.Named("Weight")));
        }

        public override void CompTick()
        {
            base.CompTick();
            this.UpdateInventory();
        }
    }
}
