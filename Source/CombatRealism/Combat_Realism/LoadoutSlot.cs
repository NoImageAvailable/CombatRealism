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
        #region Fields

        private Dictionary<AmmoCategoryDef, int> _ammoList = new Dictionary<AmmoCategoryDef, int>();

        #endregion Fields

        #region Constructors

        public LoadoutSlot( ThingDef def, int count = 1 )
        {
            Count = count;
            Def = def;
            Active = false;
        }

        #endregion Constructors

        #region Properties

        public bool Active { get; set; }
        public int Count { get; set; }
        public ThingDef Def { get; set; }

        // TODO: IsAmmo logic
        public bool IsAmmo => true;

        // TODO: Better IsWeapon logic (vanilla ThingDef IsWeapon returns true for pawns?!)
        public bool IsWeapon => true;

        #endregion Properties
    }
}