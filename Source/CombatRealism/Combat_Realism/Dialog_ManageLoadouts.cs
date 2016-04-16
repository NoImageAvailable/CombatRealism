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
    public enum SourceSelection
    {
        Ranged,
        Melee,
        Ammo,
        All
    }

    public class Dialog_ManageLoadouts : Window
    {
        #region Fields

        private static Texture2D
            _arrowBottom        = ContentFinder<Texture2D>.Get( "UI/Icons/arrowBottom" ),
            _arrowDown          = ContentFinder<Texture2D>.Get( "UI/Icons/arrowDown" ),
            _arrowTop           = ContentFinder<Texture2D>.Get( "UI/Icons/arrowTop" ),
            _arrowUp            = ContentFinder<Texture2D>.Get( "UI/Icons/arrowUp" ),
            _darkBackground     = SolidColorMaterials.NewSolidColorTexture(0f, 0f, 0f, .2f),
            _iconEdit           = ContentFinder<Texture2D>.Get( "UI/Icons/edit" ),
            _iconClear          = ContentFinder<Texture2D>.Get( "UI/Icons/clear" ),
            _iconAmmo           = ContentFinder<Texture2D>.Get( "UI/Icons/ammo" ),
            _iconRanged         = ContentFinder<Texture2D>.Get( "UI/Icons/ranged" ),
            _iconMelee          = ContentFinder<Texture2D>.Get( "UI/Icons/melee" ),
            _iconAll            = ContentFinder<Texture2D>.Get( "UI/Icons/all" );

        private static Regex validNameRegex          = new Regex("^[a-zA-Z0-9 '\\-]*$");
        private float _availableListHeight           = 0f;
        private Vector2 _availableScrollPosition     = Vector2.zero;
        private Vector2 _countFieldSize              = new Vector2( 40f, 24f );
        private Loadout _currentLoadout;
        private int _currentSlotIndex                = -1;
        private LoadoutSlot _draggedSlot;
        private bool _dragging;
        private float _iconSize                      = 16f;
        private float _margin                        = 6f;
        private float _rowHeight                     = 30f;
        private float _slotListHeight                = 0f;
        private Vector2 _slotScrollPosition          = Vector2.zero;
        private List<ThingDef> _source;
        private SourceSelection _sourceType          = SourceSelection.Ranged;
        private int _ticks                           = 0;
        private float _topAreaHeight                 = 30f;

        #endregion Fields

        #region Constructors

        public Dialog_ManageLoadouts( Loadout loadout )
        {
            CurrentLoadout = loadout;
            SetSource( SourceSelection.Ranged );
        }

        #endregion Constructors

        #region Properties

        public Loadout CurrentLoadout
        {
            get
            {
                return _currentLoadout;
            }
            set
            {
                _currentLoadout = value;
            }
        }

        public LoadoutSlot Dragging
        {
            get
            {
                if ( _dragging )
                    return _draggedSlot;
                return null;
            }
            set
            {
                if ( value == null )
                    _dragging = false;
                else
                    _dragging = true;
                _draggedSlot = value;
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
                ( canvas.width - _margin ) / 2f,
                24f );

            Rect slotListRect = new Rect(
                0f,
                nameRect.yMax + _margin,
                ( canvas.width - _margin ) / 2f,
                canvas.height - _topAreaHeight - nameRect.height - _margin * 3 );

            Rect sourceButtonRect = new Rect(
                slotListRect.xMax + _margin,
                _topAreaHeight + _margin * 2,
                ( canvas.width - _margin ) /2f,
                24f );

            Rect selectionRect = new Rect(
                slotListRect.xMax + _margin,
                sourceButtonRect.yMax + _margin,
                ( canvas.width - _margin ) / 2f,
                canvas.height - 24f - _topAreaHeight - _margin * 3 );

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

            // name
            DrawNameField( nameRect );

            // source selection
            DrawSourceSelection( sourceButtonRect );

            // selection area
            DrawSlotSelection( selectionRect );

            // current slots
            DrawSlotList( slotListRect );

            // done!
        }

        public void DrawSourceSelection( Rect canvas )
        {
            Rect button = new Rect( canvas.xMin, canvas.yMin + ( canvas.height - 24f ) / 2f, 24f, 24f );

            // Ranged weapons
            GUI.color = _sourceType == SourceSelection.Ranged ? GenUI.MouseoverColor : Color.white;
            if ( Widgets.ImageButton( button, _iconRanged ) )
                SetSource( SourceSelection.Ranged );
            button.x += 24f + _margin;

            // Melee weapons
            GUI.color = _sourceType == SourceSelection.Melee ? GenUI.MouseoverColor : Color.white;
            if ( Widgets.ImageButton( button, _iconMelee ) )
                SetSource( SourceSelection.Melee );
            button.x += 24f + _margin;

            // Ammo
            GUI.color = _sourceType == SourceSelection.Ammo ? GenUI.MouseoverColor : Color.white;
            if ( Widgets.ImageButton( button, _iconAmmo ) )
                SetSource( SourceSelection.Ammo );
            button.x += 24f + _margin;

            // All
            GUI.color = _sourceType == SourceSelection.All ? GenUI.MouseoverColor : Color.white;
            if ( Widgets.ImageButton( button, _iconAll ) )
                SetSource( SourceSelection.All );
            button.x += 24f + _margin;

            // reset color
            GUI.color = Color.white;
        }

        public void SetSource( SourceSelection source )
        {
            _source = DefDatabase<ThingDef>.AllDefsListForReading;

            switch ( source )
            {
                case SourceSelection.Ranged:
                    _source = _source.Where( td => td.IsRangedWeapon ).ToList();
                    _sourceType = SourceSelection.Ranged;
                    break;

                case SourceSelection.Melee:
                    _source = _source.Where( td => td.IsMeleeWeapon ).ToList();
                    _sourceType = SourceSelection.Melee;
                    break;

                case SourceSelection.Ammo:
                    _source = _source.Where( td => td is AmmoDef ).ToList();
                    _sourceType = SourceSelection.Ammo;
                    break;

                case SourceSelection.All:
                default:
                    _sourceType = SourceSelection.All;
                    break;
            }

            if ( !_source.NullOrEmpty() )
                _source = _source.OrderBy( td => td.label ).ToList();
        }

        private void DrawCountField( Rect canvas, LoadoutSlot slot )
        {
            if ( slot == null )
                return;
            string count = GUI.TextField( canvas, slot.Count.ToString() );
            TooltipHandler.TipRegion( canvas, "CR.CountFieldTip".Translate( slot.Count ) );
            int countInt;
            if ( int.TryParse( count, out countInt ) )
            {
                slot.Count = countInt;
            }
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
                if ( _currentSlotIndex < CurrentLoadout.SlotCount - 1 )
                {
                    if ( Widgets.ImageButton( down, _arrowDown ) )
                        _currentSlotIndex = CurrentLoadout.OrderDown( _currentSlotIndex );
                    if ( Widgets.ImageButton( bottom, _arrowBottom ) )
                        _currentSlotIndex = CurrentLoadout.OrderBottom( _currentSlotIndex );
                }
                GUI.EndGroup();
            }
        }

        private void DrawSlot( Rect row, LoadoutSlot slot )
        {
            // set up rects
            // label (fill) || count (50px) || delete (25px)
            Rect labelRect = new Rect( row );
            labelRect.xMin += _margin;
            labelRect.width -= _countFieldSize.x - _iconSize - 2 * _margin;

            Rect countRect = new Rect(
                row.xMax - _countFieldSize.x - _iconSize - 2 * _margin,
                row.yMin + ( row.height - _countFieldSize.y ) / 2f,
                _countFieldSize.x,
                _countFieldSize.y );

            Rect deleteRect = new Rect( row );
            deleteRect.xMin = row.xMax - _iconSize - _margin;

            // interactions (main row rect)
            if ( !Mouse.IsOver( deleteRect ) )
            {
                Widgets.DrawHighlightIfMouseover( row );
                TooltipHandler.TipRegion( row, slot.Def.LabelCap );
            }

            // label
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label( labelRect, slot.Def.LabelCap );
            Text.Anchor = TextAnchor.UpperLeft;

            // count
            DrawCountField( countRect, slot );

            // delete
            if ( Mouse.IsOver( deleteRect ) )
                GUI.DrawTexture( row, TexUI.HighlightTex );
            if ( Widgets.ImageButton( deleteRect, TexUI.UnknownThing ) )
            {
                CurrentLoadout.RemoveSlot( slot );
            }
            TooltipHandler.TipRegion( deleteRect, "CR.DeleteFilter".Translate() );
        }

        private void DrawSlotList( Rect canvas )
        {
            // set up content canvas
            Rect viewRect = new Rect( 0f, 0f, canvas.width, _rowHeight * CurrentLoadout.SlotCount + 1 );

            // create some extra height if we're dragging
            if ( Dragging != null )
                viewRect.height += _rowHeight;

            // leave room for scrollbar if necessary
            if ( viewRect.height > canvas.height )
                viewRect.width -= 16f;

            // darken whole area
            GUI.DrawTexture( canvas, _darkBackground );

            Widgets.BeginScrollView( canvas, ref _slotScrollPosition, viewRect );
            int i = 0;
            float curY = 0f;
            for ( ; i < CurrentLoadout.SlotCount; i++ )
            {
                // create row rect
                Rect row = new Rect( 0f, curY, viewRect.width, _rowHeight );
                curY += _rowHeight;

                // if we're dragging, and currently on this row, and this row is not the row being dragged - draw a ghost of the slot here
                if ( Dragging != null && Mouse.IsOver( row ) && Dragging != CurrentLoadout.Slots[i] )
                {
                    // draw ghost
                    GUI.color = new Color( .7f, .7f, .7f, .5f );
                    DrawSlot( row, Dragging );
                    GUI.color = Color.white;

                    // catch mouseUp
                    if ( Input.GetMouseButtonUp( 0 ) )
                    {
                        CurrentLoadout.MoveSlot( Dragging, i );
                        Dragging = null;
                    }

                    // ofset further slots down
                    row.y += _rowHeight;
                    curY += _rowHeight;
                }

                // alternate row background
                if ( i % 2 == 0 )
                    GUI.DrawTexture( row, _darkBackground );

                // draw the slot - grey out if draggin this, but only when dragged over somewhere else
                if ( Dragging == CurrentLoadout.Slots[i] && !Mouse.IsOver( row ) )
                    GUI.color = new Color( .6f, .6f, .6f, .4f );
                DrawSlot( row, CurrentLoadout.Slots[i] );
                GUI.color = Color.white;

                // check mouse down
                if ( Mouse.IsOver( row ) && Input.GetMouseButtonDown( 0 ) )
                    Dragging = CurrentLoadout.Slots[i];
            }

            // if we're dragging, create an extra invisible row to allow moving stuff to the bottom
            if ( Dragging != null )
            {
                Rect row = new Rect( 0f, curY, viewRect.width, _rowHeight );

                if ( Mouse.IsOver( row ) )
                {
                    // draw ghost
                    GUI.color = new Color( .7f, .7f, .7f, .5f );
                    DrawSlot( row, Dragging );
                    GUI.color = Color.white;

                    // catch mouseUp
                    if ( Input.GetMouseButtonUp( 0 ) )
                    {
                        CurrentLoadout.MoveSlot( Dragging, CurrentLoadout.Slots.Count - 1 );
                        Dragging = null;
                    }
                }
            }

            // cancel drag when mouse leaves the area, or on mouseup.
            if ( !Mouse.IsOver( viewRect ) || Input.GetMouseButtonUp( 0 ) )
                Dragging = null;

            Widgets.EndScrollView();
        }

        private void DrawSlotSelection( Rect canvas )
        {
            GUI.DrawTexture( canvas, _darkBackground );

            if ( _source.NullOrEmpty() )
                return;

            Rect viewRect = new Rect( canvas );
            viewRect.width -= 16f;
            viewRect.height = _source.Count * _rowHeight;

            Widgets.BeginScrollView( canvas, ref _availableScrollPosition, viewRect.AtZero() );
            for ( int i = 0; i < _source.Count; i++ )
            {
                Rect row = new Rect( 0f, i * _rowHeight, canvas.width, _rowHeight );
                Rect labelRect = new Rect( row );

                labelRect.xMin += _margin;
                if ( i % 2 == 0 )
                    GUI.DrawTexture( row, _darkBackground );

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label( labelRect, _source[i].LabelCap );
                Text.Anchor = TextAnchor.UpperLeft;

                Widgets.DrawHighlightIfMouseover( row );
                if ( Widgets.InvisibleButton( row ) )
                {
                    var slot = new LoadoutSlot( _source[i], 1 );
                    CurrentLoadout.AddSlot( slot );
                }
            }
            Widgets.EndScrollView();
        }

        #endregion Methods
    }
}