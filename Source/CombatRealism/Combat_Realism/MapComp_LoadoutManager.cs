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
            _loadouts.Add( new Loadout( "CR.EmptyLoadoutName".Translate() ) );
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

        public static Dictionary<Pawn, Loadout> AssignedLoadouts { get { return Instance._assignedLoadouts; } }
        public static List<Loadout> Loadouts { get { return Instance._loadouts; } }

        #endregion Properties

        #region Methods

        public static void AddLoadout( Loadout loadout )
        {
            Instance._loadouts.Add( loadout );
        }

        public static void RemoveLoadout( Loadout loadout )
        {
            // TODO: Handle logic for pawns using this loadout.
            Instance._loadouts.Remove( loadout );
        }

        public override void ExposeData()
        {
            throw new NotImplementedException( "ExposeData() not implemented" );
        }

        #endregion Methods
    }
}