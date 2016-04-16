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

        public string LabelCap => label.CapitalizeFirst();
        public int SlotCount => _slots.Count;
        public List<LoadoutSlot> Slots => _slots;

        #endregion Properties

        #region Methods

        public void AddSlot( LoadoutSlot slot )
        {
            _slots.Add( slot );
        }

        public void MoveSlot( LoadoutSlot slot, int toIndex )
        {
            int fromIndex = _slots.IndexOf( slot );
            MoveTo( fromIndex, toIndex );
        }

        public int OrderBottom( int index )
        {
            return MoveTo( index, SlotCount - 1 );
        }

        public int OrderDown( int index )
        {
            return MoveTo( index, index + 1 );
        }

        public int OrderTop( int index )
        {
            return MoveTo( index, 0 );
        }

        public int OrderUp( int index )
        {
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
            if ( fromIndex < 0 || fromIndex >= _slots.Count || toIndex < 0 || toIndex >= _slots.Count )
            {
                throw new Exception( "Attempted to move i " + fromIndex + " to " + toIndex + ", bounds are [0," + ( _slots.Count-1 ) + "]." );
            }

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