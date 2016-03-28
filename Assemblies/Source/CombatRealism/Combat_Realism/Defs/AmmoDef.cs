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
        public enum AmmoClass
        {
            // TODO - Add enum for ammo classes to use with generics in loadout manager
        }

        public ThingDef linkedProjectile;
        public AmmoClass ammoClass;
    }
}
