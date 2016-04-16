using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using CommunityCoreLibrary;
using RimWorld;

namespace Combat_Realism
{
    public class Loadout
    {
        #region Fields

        public string label;
        private List<LoadoutSlot> _slots = new List<LoadoutSlot>();

        #endregion Fields

        #region Constructors

        public Loadout()
        {
            // create a unique default name.
            int i = 1;
            do
            {
                label = "CR.DefaultLoadoutName".Translate( i++ );
            }
            while ( LoadoutManager.Loadouts.Any( l => l.label == label ) );
        }

        public Loadout( string label )
        {
            this.label = label;
        }

        #endregion Constructors

        #region Properties

        public int FilterCount => _slots.Count;
        public string LabelCap => label.CapitalizeFirst();
        public List<LoadoutSlot> Slots => _slots;

        #endregion Properties

        #region Methods

        public void AddSlot( LoadoutSlot slot )
        {
            _slots.Add( slot );
        }

        public int OrderBottom( int index )
        {
            if ( index >= FilterCount )
                throw new ArgumentOutOfRangeException( "index < 0 or > n" );
            return MoveTo( index, FilterCount );
        }

        public int OrderDown( int index )
        {
            if ( index >= FilterCount )
                throw new ArgumentOutOfRangeException( "index < 0 or > n" );
            return MoveTo( index, index + 1 );
        }

        public int OrderTop( int index )
        {
            if ( index <= 0 )
                throw new ArgumentOutOfRangeException( "index < 0 or > n" );
            return MoveTo( index, 0 );
        }

        public int OrderUp( int index )
        {
            if ( index <= 0 )
                throw new ArgumentOutOfRangeException( "index < 0 or > n" );
            return MoveTo( index, index - 1 );
        }

        public void RemoveSlot( LoadoutSlot slot )
        {
            _slots.Remove( slot );
        }

        public void RemoveSlot( int index )
        {
            _slots.RemoveAt( index );
        }

        private int MoveTo( int fromIndex, int toIndex )
        {
            // fetch the filter we're moving
            var temp = _slots[fromIndex];

            // remove from old location
            _slots.RemoveAt( fromIndex );

            // this may have changed the toIndex
            if ( fromIndex + 1 < toIndex )
                toIndex--;

            // insert at new location
            _slots.Insert( toIndex, temp );
            return toIndex;
        }

        #endregion Methods
    }
}
