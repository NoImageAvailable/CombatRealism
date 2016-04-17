using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public class LoadoutManager : MapComponent
    {
        #region Fields

        private static LoadoutManager _instance;
        private Dictionary<Pawn, Loadout> _assignedLoadouts = new Dictionary<Pawn, Loadout>();
        private List<Loadout> _loadouts = new List<Loadout>();

        #endregion Fields

        #region Constructors

        public LoadoutManager()
        {
            // create a default empty loadout
            // there needs to be at least one default tagged loadout at all times
            _loadouts.Add( new Loadout( "CR.EmptyLoadoutName".Translate() ) { canBeDeleted = false, defaultLoadout = true } );
        }

        #endregion Constructors

        #region Properties

        public static LoadoutManager Instance
        {
            get
            {
                if ( _instance == null )
                    _instance = new LoadoutManager();
                return _instance;
            }
        }

        public static Dictionary<Pawn, Loadout> AssignedLoadouts => Instance._assignedLoadouts;
        public static Loadout DefaultLoadout => Instance._loadouts.First( l => l.defaultLoadout );
        public static List<Loadout> Loadouts => Instance._loadouts;

        #endregion Properties

        #region Methods

        public static void AddLoadout( Loadout loadout )
        {
            Instance._loadouts.Add( loadout );
        }

        public static void RemoveLoadout( Loadout loadout )
        {
            Instance._loadouts.Remove( loadout );

            // assign default loadout to pawns that used to use this loadout
            var obsolete = AssignedLoadouts.Where( pair => pair.Value == loadout ).Select( pair => pair.Key );
            foreach ( var pawn in obsolete )
            {
                AssignedLoadouts[pawn] = DefaultLoadout;
            }
        }

        public override void ExposeData()
        {
            throw new NotImplementedException( "ExposeData() not implemented" );
        }

        #endregion Methods
    }
}