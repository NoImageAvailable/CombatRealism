using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CommunityCoreLibrary;

namespace Combat_Realism
{
    public class AmmoInjector : SpecialInjector
    {
        private const string enableTag = "CR_AutoEnableTrade";      // The trade tag which designates ammo defs for being automatically switched to Tradeability.Stockable

        public override bool Inject()
        {
            // Initialize list of all weapons so we don't have to iterate through all the defs, all the time
            Utility.allWeaponDefs.Clear();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.IsWeapon && (def.canBeSpawningInventory || def.tradeability == Tradeability.Stockable))
                    Utility.allWeaponDefs.Add(def);
            }
            if (Utility.allWeaponDefs.NullOrEmpty())
            {
                Log.Warning("CR Ammo Injector found no weapon defs");
                return true;
            }
            
            // Find all ammo using guns
            foreach (ThingDef weaponDef in Utility.allWeaponDefs)
            {
                CompProperties_AmmoUser props = weaponDef.GetCompProperty<CompProperties_AmmoUser>();
                if (props != null && props.ammoSet != null && !props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    foreach(ThingDef curDef in props.ammoSet.ammoTypes)
                    {
                        AmmoDef ammoDef = curDef as AmmoDef;
                        if(ammoDef != null && ammoDef.tradeTags.Contains(enableTag))
                        {
                            ammoDef.tradeability = Tradeability.Stockable;
                        }
                    }
                }
            }
            return true;
        }
    }
}
