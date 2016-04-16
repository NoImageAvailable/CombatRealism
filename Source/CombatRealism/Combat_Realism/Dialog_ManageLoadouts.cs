using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public class Dialog_ManageLoadouts : Window
    {
        #region Fields

        private static Texture2D _arrowTop           = ContentFinder<Texture2D>.Get( "UI/Icons/arrowTop" );
        private static Texture2D _arrowUp            = ContentFinder<Texture2D>.Get( "UI/Icons/arrowUp" );
        private static Texture2D _arrowDown          = ContentFinder<Texture2D>.Get( "UI/Icons/arrowDown" );
        private static Texture2D _arrowBottom        = ContentFinder<Texture2D>.Get( "UI/Icons/arrowBottom" );
        private static Texture2D _iconEdit           = ContentFinder<Texture2D>.Get( "UI/Icons/edit" );
        private static Texture2D _darkBackground     = SolidColorMaterials.NewSolidColorTexture(0f, 0f, 0f, .2f);
        private static Regex validNameRegex          = new Regex("^[a-zA-Z0-9 '\\-]*$");
        private Loadout _currentLoadout;
        private LoadoutSlot _currentSlot;         
        private int _currentSlotIndex                = -1;
        private float _availableListHeight           = 0f;
        private float _slotListHeight                = 0f;
        private Vector2 _availableScrollPosition     = Vector2.zero;
        private Vector2 _slotScrollPosition          = Vector2.zero;
        private float _margin                        = 6f;
        private float _iconSize                      = 16f;
        private float _rowHeight                     = 30f;
        private int _ticks                           = 0;
        private float _topAreaHeight                 = 30f;
        private Vector2 _countFieldSize              = 

        #endregion Fields

        #region Constructors

        public Dialog_ManageLoadouts( Loadout loadout )
        {
            CurrentLoadout = loadout;
        }

        #endregion Constructors

        #region Properties

        public LoadoutSlot CurrentSlot
        {
            get
            {
                return _currentSlot;
            }
            set
            {
                _currentSlot = value;
                _currentSlotIndex = value == null ? -1 : CurrentLoadout.Slots.IndexOf( _currentSlot );
            }
        }

        public Loadout CurrentLoadout
        {
            get
            {
                return _currentLoadout;
            }
            set
            {
                _currentLoadout = value;

                // note: this must happen after loadout is set, Slot setter also sets index of the slot, which depends on loadout.
                CurrentSlot = null;
            }
        }

        public override Vector2 InitialWindowSize
        {
            get
            {
                return new Vector2( 700, 700 );
            }
        }

        #endregion Properties

        #region Methods

        public override void DoWindowContents( Rect canvas )
        {
            // fix weird zooming bug
            Text.Font = GameFont.Small;

            // SET UP RECTS
            // top buttons
            Rect selectRect = new Rect( 0f, 0f, canvas.width * .2f, _topAreaHeight );
            Rect newRect = new Rect( selectRect.xMax + _margin, 0f, canvas.width * .2f, _topAreaHeight );
            Rect deleteRect = new Rect( newRect.xMax + _margin, 0f, canvas.width * .2f, _topAreaHeight );

            // main areas
            Rect nameRect = new Rect(
                0f,
                _topAreaHeight + _margin * 2,
                ( canvas.width - _margin * 2 - _iconSize ) / 2f,
                24f );

            Rect currentFiltersRect = new Rect(
                0f,
                nameRect.yMax + _margin,
                ( canvas.width - _margin * 2 - _iconSize ) / 2f,
                canvas.height - _topAreaHeight - nameRect.height - _margin * 3 );

            Rect orderButtonRect = new Rect(
                currentFiltersRect.xMax + _margin,
                nameRect.yMax + _margin,
                _iconSize,
                canvas.height - _topAreaHeight - nameRect.height - _margin * 3 );

            Rect filterDetailRect = new Rect(
                orderButtonRect.xMax + _margin,
                _topAreaHeight + _margin * 2,
                ( canvas.width - _margin * 2 - _iconSize ) / 2f,
                canvas.height - _topAreaHeight - _margin * 2 );

            var loadouts = LoadoutManager.Loadouts;

            // DRAW CONTENTS
            // buttons
            // select loadout
            if ( Widgets.TextButton( selectRect, "CR.SelectLoadout".Translate() ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                if ( loadouts.Count == 0 )
                    options.Add( new FloatMenuOption( "CR.NoLoadouts".Translate(), null ) );
                else
                {
                    for ( int i = 0; i < loadouts.Count; i++ )
                    {
                        int local_i = i;
                        options.Add( new FloatMenuOption( loadouts[i].LabelCap, delegate
                        { CurrentLoadout = loadouts[local_i]; } ) );
                    }
                }

                Find.WindowStack.Add( new FloatMenu( options ) );
            }
            // create loadout
            if ( Widgets.TextButton( newRect, "CR.NewLoadout".Translate() ) )
            {
                var loadout = new Loadout();
                LoadoutManager.AddLoadout( loadout );
                CurrentLoadout = loadout;
            }
            // delete loadout
            if ( loadouts.Count > 0 && Widgets.TextButton( deleteRect, "CR.DeleteLoadout".Translate() ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                for ( int i = 0; i < loadouts.Count; i++ )
                {
                    int local_i = i;
                    options.Add( new FloatMenuOption( loadouts[i].LabelCap,
                        delegate
                        {
                            if ( CurrentLoadout == loadouts[local_i] )
                                CurrentLoadout = null;
                            loadouts.Remove( loadouts[local_i] );
                        } ) );
                }

                Find.WindowStack.Add( new FloatMenu( options ) );
            }

            // draw notification if no loadout selected
            if ( CurrentLoadout == null )
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.grey;
                Widgets.Label( canvas, "CR.NoLoadoutSelected".Translate() );
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                // and stop further drawing
                return;
            }

            // name field
            DrawNameField( nameRect );

            // new filter
            DrawSlotSelection( filterDetailRect );

            // list current filters
            DrawSlotList( currentFiltersRect );

            // draw order buttons
            DrawOrderButtons( orderButtonRect );

            // done!
        }

        private void DrawCountField( Rect canvas, LoadoutSlot slot )
        {
            string count = GUI.TextField( canvas, slot.Count.ToString() );
            TooltipHandler.TipRegion( canvas, "CR.CountFieldTip".Translate( _currentSlot.Count ) );
            int countInt;
            if ( int.TryParse( count, out countInt ) )
            {
                slot.Count = countInt;
            }
        }

        private void DrawSlotList( Rect canvas )
        {
            Rect viewRect = new Rect( 0f, 0f, canvas.width - 16f, _rowHeight * CurrentLoadout.FilterCount + 1 );
            
            // darken whole area
            GUI.DrawTexture( canvas, _darkBackground );

            Widgets.BeginScrollView( canvas, ref _slotScrollPosition, viewRect );
            int i = 0;
            for ( ; i < CurrentLoadout.FilterCount; i++ )
            {
                // create row rect
                Rect row = new Rect( 0f, i * _rowHeight, canvas.width, _rowHeight );

                // alternate row background
                if ( i % 2 == 0 )
                    GUI.DrawTexture( row, _darkBackground );

                // draw the slot
                DrawSlot( row, CurrentLoadout.Slots[i] );
            }

            Widgets.EndScrollView();
        }

        private void DrawSlot( Rect row, LoadoutSlot slot )
        {
            // set up rects
            // label (fill) || count (50px) || delete (25px)
            Rect labelRect = new Rect( row );
            labelRect.width -= 75f;

            Rect countRect = new Rect(
                row.xMax - _countFieldSize.x - _iconSize - 2 * _margin,
                row.yMin + _margin,
                _countFieldSize.x,
                _countFieldSize.y );


            Rect deleteRect = new Rect( row );
            deleteRect.xMin = countRect.xMax + _margin;
            
            // interactions (main row rect)
            if ( !Mouse.IsOver( deleteRect ) )
            {
                if ( Widgets.InvisibleButton( row ) )
                    CurrentSlot = CurrentLoadout.Slots[i];

                if ( _currentSlotIndex == i )
                    Widgets.DrawHighlightSelected( row );
                else
                    Widgets.DrawHighlightIfMouseover( row );
                TooltipHandler.TipRegion( row, CurrentLoadout.Slots[i].Def.LabelCap );
            }

            // label
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label( labelRect, CurrentLoadout.Slots[i].Def.LabelCap );

            // count
            DrawCountField( countRect, CurrentLoadout.Slots[i] );

            // delete
            if ( Mouse.IsOver( deleteRect ) )
                GUI.DrawTexture( row, TexUI.HighlightTex );
            if ( Widgets.ImageButton( deleteRect, TexUI.UnknownThing ) )
            {
                CurrentLoadout.RemoveSlot( i );
                if ( _currentSlotIndex == i )
                    CurrentSlot = null;
            }
            TooltipHandler.TipRegion( deleteRect, "CR.DeleteFilter".Translate() );
        }

        private void DrawSlotSelection( Rect canvas )
        {
            GUI.DrawTexture( canvas, _darkBackground );

            var weapons = DefDatabase<ThingDef>.AllDefsListForReading.Where( def => def.IsWeapon ).ToList();
            Rect viewRect = new Rect( canvas );
            viewRect.width -= 16f;
            viewRect.height = weapons.Count * _rowHeight;
 
            Widgets.BeginScrollView( canvas, ref _availableScrollPosition, viewRect.AtZero() );
            for ( int i = 0; i < weapons.Count; i++ )
            {
                Rect row = new Rect( 0f, i * _rowHeight, canvas.width, _rowHeight );
                if ( i % 2 == 0 )
                    GUI.DrawTexture( row, _darkBackground );

                Widgets.Label( row.ContractedBy( _margin ), weapons[i].LabelCap );

                Widgets.DrawHighlightIfMouseover( row );
                if (Widgets.InvisibleButton( row ) )
                {
                    var slot = new LoadoutSlot( weapons[i], 1 );
                    CurrentLoadout.AddSlot( slot );
                    CurrentSlot = slot;
                }
            }
            Widgets.EndScrollView();
        }

        private void DrawNameField( Rect canvas )
        {
            string label = GUI.TextField( canvas, CurrentLoadout.label );
            if ( validNameRegex.IsMatch( label ) )
            {
                CurrentLoadout.label = label;
            }
        }

        private void DrawOrderButtons( Rect canvas )
        {
            // only draw order buttons if a filtercount is selected
            if ( _currentSlotIndex >= 0 )
            {
                // Set up rects
                Rect top = new Rect( 0f, 0f, _iconSize, _iconSize );
                Rect up = new Rect( top );
                up.y += _iconSize + _margin;
                Rect bottom = new Rect( top );
                bottom.y = canvas.height - _iconSize;
                Rect down = new Rect( bottom );
                down.y -= _iconSize + _margin;
                
                GUI.BeginGroup( canvas );
                if ( _currentSlotIndex > 0 )
                {
                    if ( Widgets.ImageButton( top, _arrowTop ) )
                        _currentSlotIndex = CurrentLoadout.OrderTop( _currentSlotIndex );
                    if ( Widgets.ImageButton( up, _arrowUp ) )
                        _currentSlotIndex = CurrentLoadout.OrderUp( _currentSlotIndex );
                }
                if ( _currentSlotIndex < CurrentLoadout.FilterCount - 1 )
                {
                    if ( Widgets.ImageButton( down, _arrowDown ) )
                        _currentSlotIndex = CurrentLoadout.OrderDown( _currentSlotIndex );
                    if ( Widgets.ImageButton( bottom, _arrowBottom ) )
                        _currentSlotIndex = CurrentLoadout.OrderBottom( _currentSlotIndex );
                }
                GUI.EndGroup();
            }
        }

        #endregion Methods
    }
}