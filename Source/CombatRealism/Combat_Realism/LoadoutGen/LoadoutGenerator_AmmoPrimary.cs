using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public class LoadoutGenerator_AmmoPrimary : LoadoutGenerator_List
    {
        /// <summary>
        /// Initializes availableDefs and adds all available ammo types of the currently equipped weapon 
        /// </summary>
        protected override void InitAvailableDefs()
        {
            Pawn pawn = compInvInt.parent as Pawn;
            if(pawn == null)
            {
                Log.Error("Tried generating ammo loadout defs with null pawn");
                return;
            }
            if (pawn.equipment != null && pawn.equipment.Primary != null)
            {
                ThingWithComps eq = pawn.equipment.Primary;
                availableDefs = new List<ThingDef>();
                AddAvailableAmmoFor(eq);
            }
        }

        protected void AddAvailableAmmoFor(ThingWithComps eq)
        {
            if (eq == null || availableDefs == null)
            {
                return;
            }
            CompAmmoUser compAmmo = eq.TryGetComp<CompAmmoUser>();
            if (compAmmo != null && !compAmmo.Props.ammoSet.ammoTypes.NullOrEmpty())
            {
                foreach (ThingDef ammo in compAmmo.Props.ammoSet.ammoTypes)
                {
                    if (ammo.canBeSpawningInventory)
                        availableDefs.Add(ammo);
                }
            }
        }

        protected override float GetWeightForDef(ThingDef def)
        {
            float weight = 1;
            AmmoDef ammo = def as AmmoDef;
            if (ammo != null && ammo.ammoClass.advanced)
                weight *= 0.2f;
            return weight;
        }
    }
}
