using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using CommunityCoreLibrary;
using UnityEngine;

namespace Combat_Realism
{
    public class LoadoutSlot
    {
        private Dictionary<AmmoCategoryDef, int> _ammoList = new Dictionary<AmmoCategoryDef, int>();

        public LoadoutSlot( ThingDef def, int count = 1 )
        {
            Count = count;
            Def = def;
            Active = false;
        }

        public int Count { get; set; }
        public ThingDef Def { get; set; }
        public bool Active { get; set; }

        // TODO: Better IsWeapon logic (vanilla ThingDef IsWeapon returns true for pawns?!)
        public bool IsWeapon => true;
        public bool IsAmmo => Def.IsAmmo();
    }
}
