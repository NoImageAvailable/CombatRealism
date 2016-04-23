﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Combat_Realism
{
    public class LoadoutAssignment : IExposable
    {
        #region Fields

        internal Loadout loadout;
        internal Pawn pawn;

        #endregion Fields

        #region Methods

        public void ExposeData()
        {
            Scribe_References.LookReference( ref pawn, "pawn" );
            Scribe_References.LookReference( ref loadout, "loadout" );

#if DEBUG
            Log.Message( Scribe.mode + ", pawn: " + ( pawn == null ? "NULL" : pawn.NameStringShort ) + ", loadout: " + ( loadout == null ? "NULL" : loadout.label ) );
#endif
        }

        #endregion Methods
    }
}