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
        private const string enableTradeTag = "CR_AutoEnableTrade";             // The trade tag which designates ammo defs for being automatically switched to Tradeability.Stockable
        private const string enableCraftingTag = "CR_AutoEnableCrafting";        // The trade tag which designates ammo defs for having their crafting recipes automatically added to the crafting table
        private static ThingDef ammoCraftingStationInt;                         // The crafting station to which ammo will be automatically added
        private static ThingDef ammoCraftingStation
        {
            get
            {
                if (ammoCraftingStationInt == null)
                    ammoCraftingStationInt = ThingDef.Named("AmmoBench");
                return ammoCraftingStationInt;
            }
        }

        public override bool Inject()
        {
            // Initialize list of all weapons so we don't have to iterate through all the defs, all the time
            Utility.allWeaponDefs.Clear();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.IsWeapon && (def.canBeSpawningInventory || def.tradeability == Tradeability.Stockable || def.weaponTags.Contains("TurretGun")))
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
                        if(ammoDef != null)
                        {
                            // Enable trading
                            if (ammoDef.tradeTags.Contains(enableTradeTag))
                                ammoDef.tradeability = Tradeability.Stockable;

                            // Enable recipe
                            if (ammoDef.tradeTags.Contains(enableCraftingTag))
                            {
                                RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed(("Make" + ammoDef.defName), false);
                                if (recipe == null)
                                {
                                    Log.Error("CR ammo injector found no recipe named Make" + ammoDef.defName);
                                }
                                else
                                {
                                    if (ammoCraftingStation == null)
                                    {
                                        Log.ErrorOnce("CR ammo injector crafting station is null", 84653201);
                                    }
                                    else
                                    {
                                        if (!recipe.recipeUsers.Contains(ammoCraftingStation))
                                        {
                                            recipe.recipeUsers.Add(ammoCraftingStation);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
