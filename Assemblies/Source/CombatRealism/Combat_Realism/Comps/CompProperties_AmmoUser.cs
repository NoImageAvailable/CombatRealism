using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompProperties_AmmoUser : CompProperties
    {
        public int magazineSize = 1;
        public int reloadTicks = 300;
        public bool throwMote = true;
        public AmmoSetDef ammoSet = null;

        public CompProperties_AmmoUser()
        {
            this.compClass = typeof(CompAmmoUser);
        }
    }
}
