using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public enum OutfitWindow
    {
        Outfits,
        Loadouts
    }

    public class MainTabWindow_OutfitsAndLoadouts : MainTabWindow_PawnList
    {
        #region Fields

        private static Texture2D _iconClearForced = ContentFinder<Texture2D>.Get( "UI/Icons/clear" );
        private static Texture2D _iconEdit        = ContentFinder<Texture2D>.Get( "UI/Icons/edit" );
        private float _buttonSize                 = 16f;
        private float _margin                     = 6f;
        private float _rowHeight                  = 30f;
        private float _topArea                    = 45f;

        #endregion Fields

        #region Properties

        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2( 1010f, 45f + (float)base.PawnsCount * _rowHeight + 65f );
            }
        }

        #endregion Properties

        #region Methods

        public override void DoWindowContents( Rect canvas )
        {
            // fix weird zooming bug
            Text.Font = GameFont.Small;

            base.DoWindowContents( canvas );

            // available space
            Rect header = new Rect( 175f + 24f + _margin, _topArea - _rowHeight, canvas.width - 175f - 24f - _margin - 16f, _rowHeight );

            // label + buttons for outfit
            Rect outfitRect = new Rect( header.xMin,
                                        header.yMin,
                                        header.width * ( 1f/3f ) + ( _margin + _buttonSize ) / 2f,
                                        header.height );
            Rect labelOutfitRect = new Rect( outfitRect.xMin,
                                             outfitRect.yMin,
                                             outfitRect.width - _margin * 3 - _buttonSize * 2,
                                             outfitRect.height )
                                             .ContractedBy( _margin / 2f );
            Rect editOutfitRect = new Rect( labelOutfitRect.xMax + _margin,
                                            outfitRect.yMin + ( ( outfitRect.height - _buttonSize ) / 2 ),
                                            _buttonSize,
                                            _buttonSize );
            Rect forcedOutfitRect = new Rect( labelOutfitRect.xMax + _buttonSize + _margin * 2,
                                              outfitRect.yMin + ( ( outfitRect.height - _buttonSize ) / 2 ),
                                              _buttonSize,
                                              _buttonSize );

            // label + button for loadout
            Rect loadoutRect = new Rect( outfitRect.xMax,
                                         header.yMin,
                                         header.width * ( 1f/3f ) - ( _margin + _buttonSize ) / 2f,
                                         header.height );
            Rect labelLoadoutRect = new Rect( loadoutRect.xMin,
                                              loadoutRect.yMin,
                                              loadoutRect.width - _margin * 2 - _buttonSize,
                                              loadoutRect.height )
                                              .ContractedBy( _margin / 2f );
            Rect editLoadoutRect = new Rect( labelLoadoutRect.xMax + _margin,
                                             loadoutRect.yMin + ( ( loadoutRect.height - _buttonSize ) / 2 ),
                                             _buttonSize,
                                             _buttonSize );

            // weight + bulk indicators
            Rect weightRect = new Rect( loadoutRect.xMax, header.yMin, header.width * ( 1f/6f ) - _margin, header.height ).ContractedBy( _margin / 2f );
            Rect bulkRect = new Rect( weightRect.xMax, header.yMin, header.width * ( 1f/6f ) - _margin, header.height ).ContractedBy( _margin / 2f );

            // draw headers
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label( labelOutfitRect, "CurrentOutfit".Translate() );
            TooltipHandler.TipRegion( editOutfitRect, "CR.EditX".Translate( "CR.Outfits".Translate() ) );
            if ( Widgets.ImageButton( editOutfitRect, _iconEdit ) )
            {
                Find.WindowStack.Add( new Dialog_ManageOutfits( null ) );
            }
            Widgets.Label( labelLoadoutRect, "CR.CurrentLoadout".Translate() );
            TooltipHandler.TipRegion( editLoadoutRect, "CR.EditX".Translate( "CR.Loadouts".Translate() ) );
            if ( Widgets.ImageButton( editLoadoutRect, _iconEdit ) )
            {
                Find.WindowStack.Add( new Dialog_ManageLoadouts( null ) );
            }
            Widgets.Label( weightRect, "CR.Weight".Translate() );
            Widgets.Label( bulkRect, "CR.Bulk".Translate() );
            Text.Anchor = TextAnchor.UpperLeft;

            // draw the rows
            canvas.yMin += 45f;
            base.DrawRows( canvas );
        }

        protected override void DrawPawnRow( Rect rect, Pawn p )
        {
            // available space for row
            Rect rowRect = new Rect( rect.x + 175f, rect.y, rect.width - 175f, rect.height );

            // response button rect
            Vector2 responsePos = new Vector2( rowRect.xMin, rowRect.yMin + ( rowRect.height - 24f ) /2f );

            // offset rest of row for that button, so we don't have to mess with all the other rect calculations
            rowRect.xMin += 24f + _margin;

            // label + buttons for outfit
            Rect outfitRect = new Rect( rowRect.xMin,
                                        rowRect.yMin,
                                        rowRect.width * ( 1f/3f ) + ( _margin + _buttonSize ) / 2f,
                                        rowRect.height );

            Rect labelOutfitRect = new Rect( outfitRect.xMin,
                                             outfitRect.yMin,
                                             outfitRect.width - _margin * 3 - _buttonSize * 2,
                                             outfitRect.height )
                                             .ContractedBy( _margin / 2f );
            Rect editOutfitRect = new Rect( labelOutfitRect.xMax + _margin,
                                            outfitRect.yMin + ( ( outfitRect.height - _buttonSize ) / 2 ),
                                            _buttonSize,
                                            _buttonSize );
            Rect forcedOutfitRect = new Rect( labelOutfitRect.xMax + _buttonSize + _margin * 2,
                                              outfitRect.yMin + ( ( outfitRect.height - _buttonSize ) / 2 ),
                                              _buttonSize,
                                              _buttonSize );

            // label + button for loadout
            Rect loadoutRect = new Rect( outfitRect.xMax,
                                         rowRect.yMin,
                                         rowRect.width * ( 1f/3f ) - ( _margin + _buttonSize ) / 2f,
                                         rowRect.height );
            Rect labelLoadoutRect = new Rect( loadoutRect.xMin,
                                              loadoutRect.yMin,
                                              loadoutRect.width - _margin * 2 - _buttonSize,
                                              loadoutRect.height )
                                              .ContractedBy( _margin / 2f );
            Rect editLoadoutRect = new Rect( labelLoadoutRect.xMax + _margin,
                                             loadoutRect.yMin + ( ( loadoutRect.height - _buttonSize ) / 2 ),
                                             _buttonSize,
                                             _buttonSize );

            // fight or flight button
            HostilityResponseModeUtility.DrawResponseButton( responsePos, p );

            // weight + bulk indicators
            Rect weightRect = new Rect( loadoutRect.xMax, rowRect.yMin, rowRect.width * ( 1f/6f ) - _margin, rowRect.height ).ContractedBy( _margin / 2f );
            Rect bulkRect = new Rect( weightRect.xMax, rowRect.yMin, rowRect.width * ( 1f/6f ) - _margin, rowRect.height ).ContractedBy( _margin / 2f );

            // OUTFITS
            // main button
            if ( Widgets.TextButton( labelOutfitRect, p.outfits.CurrentOutfit.label, true, false ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach ( Outfit outfit in Find.Map.outfitDatabase.AllOutfits )
                {
                    // need to create a local copy for delegate
                    Outfit localOutfit = outfit;
                    options.Add( new FloatMenuOption( localOutfit.label, delegate
                    {
                        p.outfits.CurrentOutfit = localOutfit;
                    }, MenuOptionPriority.Medium, null, null ) );
                }
                Find.WindowStack.Add( new FloatMenu( options, false ) );
            }

            // edit button
            TooltipHandler.TipRegion( editOutfitRect, "CR.EditX".Translate( "CR.outfit".Translate() + " " + p.outfits.CurrentOutfit.label ) );
            if ( Widgets.ImageButton( editOutfitRect, _iconEdit ) )
            {
                Find.WindowStack.Add( new Dialog_ManageOutfits( p.outfits.CurrentOutfit ) );
            }

            // clear forced button
            if ( p.outfits.forcedHandler.SomethingIsForced )
            {
                TooltipHandler.TipRegion( forcedOutfitRect, "CR.ClearForced".Translate() );
                if ( Widgets.ImageButton( forcedOutfitRect, _iconClearForced ) )
                {
                    p.outfits.forcedHandler.Reset();
                }
                TooltipHandler.TipRegion( forcedOutfitRect, new TipSignal( delegate
                {
                    string text = "ForcedApparel".Translate() + ":\n";
                    foreach ( Apparel current2 in p.outfits.forcedHandler.ForcedApparel )
                    {
                        text = text + "\n   " + current2.LabelCap;
                    }
                    return text;
                }, p.GetHashCode() * 612 ) );
            }

            // LOADOUTS
            // main button
            if ( Widgets.TextButton( labelLoadoutRect, p.outfits.CurrentOutfit.label, true, false ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach ( Outfit outfit in Find.Map.outfitDatabase.AllOutfits )
                {
                    // need to create a local copy for delegate
                    Outfit localOutfit = outfit;
                    options.Add( new FloatMenuOption( localOutfit.label, delegate
                    {
                        p.outfits.CurrentOutfit = localOutfit;
                    }, MenuOptionPriority.Medium, null, null ) );
                }
                Find.WindowStack.Add( new FloatMenu( options, false ) );
            }

            // edit button
            TooltipHandler.TipRegion( editLoadoutRect, "CR.EditX".Translate( "CR.loadout".Translate() + " " +  p.outfits.CurrentOutfit.label ) );
            if ( Widgets.ImageButton( editLoadoutRect, _iconEdit ) )
            {
                Find.WindowStack.Add( new Dialog_ManageLoadouts( p.GetLoadout() ) );
            }

            // STATUS BARS
            // fetch the comp
            CompInventory comp = p.TryGetComp<CompInventory>();

            if ( comp != null )
            {
                // weight
                bool overweight = comp.currentWeight > comp.capacityWeight;
                float weightFillPercentage = overweight ? 1f : comp.currentWeight / comp.capacityWeight;
                Widgets.FillableBar( weightRect, weightFillPercentage );
                if ( overweight )
                    Utility_UI.DrawBarThreshold( weightRect, comp.capacityWeight / comp.currentWeight, 1f );
                TooltipHandler.TipRegion( weightRect, "CR.ITabWeightTip".Translate( comp.capacityWeight, comp.currentWeight, comp.moveSpeedFactor, comp.encumberPenalty ) );

                // bulk
                bool overbulk = comp.currentBulk > comp.capacityBulk;
                float bulkFillPercentage = overbulk ? 1f : comp.currentBulk / comp.capacityBulk;
                Widgets.FillableBar( bulkRect, bulkFillPercentage );
                if ( overbulk )
                    Utility_UI.DrawBarThreshold( bulkRect, comp.capacityBulk / comp.currentBulk, 1f );
                TooltipHandler.TipRegion( bulkRect, "CR.ITabBulkTip".Translate( comp.capacityBulk, comp.currentBulk, comp.workSpeedFactor ) );
            }
        }

        #endregion Methods
    }
}