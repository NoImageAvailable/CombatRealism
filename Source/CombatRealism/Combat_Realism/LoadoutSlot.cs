using CommunityCoreLibrary;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public class LoadoutSlot
    {
        #region Constructors

        public LoadoutSlot( ThingDef def, int count = 1 )
        {
            Count = count;
            Def = def;

            // TODO: uncomment
            // increase default ammo count
            // if ( def is AmmoDef )
            // Count = ( (AmmoDef)def ).defaultAmmoCount;
        }

        #endregion Constructors

        #region Properties

        public int Count { get; set; }
        public ThingDef Def { get; set; }

        #endregion Properties
    }
}