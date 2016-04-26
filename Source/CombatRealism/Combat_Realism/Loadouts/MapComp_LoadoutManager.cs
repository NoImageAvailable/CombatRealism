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
        private List<LoadoutAssignment> _assignedLoadoutsScribeHelper = new List<LoadoutAssignment>();
        private List<Loadout> _loadouts = new List<Loadout>();

        #endregion Fields

        #region Constructors

        public LoadoutManager()
        {
            // create a default empty loadout
            // there needs to be at least one default tagged loadout at all times
            _loadouts.Add( new Loadout( "CR.EmptyLoadoutName".Translate(), 1 ) { canBeDeleted = false, defaultLoadout = true } );
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
            var obsolete = AssignedLoadouts.Where( a => a.Value == loadout ).Select( a => a.Key );
            foreach ( var id in obsolete )
            {
                AssignedLoadouts[id] = DefaultLoadout;
            }
        }

        public override void ExposeData()
        {
            // scribe available loadouts
            Scribe_Collections.LookList<Loadout>( ref Instance._loadouts, "loadouts", LookMode.Deep );

            //scribe loadout assignments (for some reason using the dictionary directly doesn't work -- Fluffy)
            // create list of scribe helper objects
            if ( Scribe.mode == LoadSaveMode.Saving )
                Instance._assignedLoadoutsScribeHelper = Instance._assignedLoadouts.Select( pair => new LoadoutAssignment() { pawn = pair.Key, loadout = pair.Value } ).ToList();

            //scribe that list
            Scribe_Collections.LookList( ref Instance._assignedLoadoutsScribeHelper, "assignments", LookMode.Deep );

            // convert back into useable dictionary
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
                Instance._assignedLoadouts = Instance
                    ._assignedLoadoutsScribeHelper
                    .Where( a => a.Valid ) // removes assignments that for some reason have a null value.
                    .ToDictionary( k => k.pawn, v => v.loadout );
        }

        internal static int GetUniqueID()
        {
            if ( Loadouts.Any() )
                return Loadouts.Max( l => l.uniqueID ) + 1;
            else
                return 1;
        }

        internal static string GetUniqueLabel()
        {
            string label;
            int i = 1;
            do
            {
                label = "CR.DefaultLoadoutName".Translate() + i++;
            }
            while ( Loadouts.Any( l => l.label == label ) );
            return label;
        }

        #endregion Methods
    }
}