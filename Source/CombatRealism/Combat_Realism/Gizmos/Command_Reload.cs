﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CommunityCoreLibrary;

namespace Combat_Realism
{
    public class Command_Reload : Command_Action
    {
        public CompAmmoUser compAmmo;

        public override void ProcessInput(Event ev)
        {
            if (ev.button == 1 && compAmmo.useAmmo && compAmmo.compInventory != null)
                Find.WindowStack.Add(MakeAmmoMenu());
            else
                base.ProcessInput(ev);
        }

        private FloatMenu MakeAmmoMenu()
        {
            List<ThingDef> ammoList = new List<ThingDef>();      // List of all ammo types the gun can use and the pawn has in his inventory
            if (compAmmo.compInventory == null)
            {
                // If we have no inventory available (e.g. manned turret), add all possible ammo types to the selection
                ammoList.AddRange(compAmmo.Props.ammoSet.ammoTypes);
            }
            else
            {
                // Iterate through all suitable ammo types and check if they're in our inventory
                foreach (ThingDef curAmmoDef in compAmmo.Props.ammoSet.ammoTypes)
                {
                    if (compAmmo.compInventory.ammoList.Any(x => x.def == curAmmoDef))
                        ammoList.Add(curAmmoDef);
                }
            }

            // Append float menu options for every available ammo type
            List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
            if (ammoList.NullOrEmpty())
            {
                floatOptionList.Add(new FloatMenuOption("CR_OutOfAmmo".Translate(), null));
            }
            else
            {
                foreach (ThingDef curDef in ammoList)
                {
                    AmmoDef ammoDef = (AmmoDef)curDef;
                    floatOptionList.Add(new FloatMenuOption(ammoDef.ammoClass.LabelCap, new Action(delegate { compAmmo.selectedAmmo = ammoDef; })));
                }
            }
            return new FloatMenu(floatOptionList);
        }
    }
}
