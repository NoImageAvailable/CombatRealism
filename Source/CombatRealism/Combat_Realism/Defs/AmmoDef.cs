using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class AmmoDef : ThingDef
    {
        public ThingDef linkedProjectile;
        public AmmoCategoryDef ammoClass;
        public int defaultAmmoCount = 1;
    }
}
