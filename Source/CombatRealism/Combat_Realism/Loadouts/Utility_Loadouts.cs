using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public static class Utility_Loadouts
    {
        #region Fields

        public static StatDef Bulk = StatDef.Named( "Bulk" ); // for items in inventory
        public static StatDef CarryBulk = StatDef.Named( "CarryBulk" ); // pawn capacity
        public static StatDef CarryWeight = StatDef.Named( "CarryWeight" ); // pawn capacity
        public static StatDef Weight = StatDef.Named( "Weight" ); // items in inventory
        public static StatDef WornBulk = StatDef.Named( "WornBulk" ); // apparel offsets
        public static StatDef WornWeight = StatDef.Named( "WornWeight" ); // apparel offsets
        private static float _labelSize = -1f;
        private static float _margin = 6f;
        private static Texture2D _overburdenedTex;

        #endregion Fields

        #region Properties

        public static float LabelSize
        {
            get
            {
                if ( _labelSize < 0 )
                {
                    // get size of label
                    _labelSize = ( _margin + Math.Max( Text.CalcSize( "CR.Weight".Translate() ).x, Text.CalcSize( "CR.Bulk".Translate() ).x ) );
                }
                return _labelSize;
            }
        }

        public static Texture2D OverburdenedTex
        {
            get
            {
                if ( _overburdenedTex == null )
                    _overburdenedTex = SolidColorMaterials.NewSolidColorTexture( Color.red );
                return _overburdenedTex;
            }
        }

        #endregion Properties

        #region Methods

        public static void DrawBar( Rect canvas, float current, float capacity, string label = "", string tooltip = "" )
        {
            // rects
            Rect labelRect = new Rect( canvas );
            Rect barRect = new Rect( canvas );
            if ( label != "" )
                barRect.xMin += LabelSize;
            labelRect.width = LabelSize;

            // label
            if ( label != "" )
                Widgets.Label( labelRect, label );

            // bar
            bool overburdened = current > capacity;
            float fillPercentage = overburdened ? 1f : current / capacity;
            if ( overburdened )
            {
                Widgets.FillableBar( barRect, fillPercentage, OverburdenedTex );
                DrawBarThreshold( barRect, capacity / current, 1f );
            }
            else
                Widgets.FillableBar( barRect, fillPercentage );

            // tooltip
            if ( tooltip != "" )
                TooltipHandler.TipRegion( canvas, tooltip );
        }

        public static void DrawBarThreshold( Rect barRect, float pct, float curLevel = 1f )
        {
            float thresholdBarWidth = (float)( ( barRect.width <= 60f ) ? 1 : 2 );

            Rect position = new Rect( barRect.x + barRect.width * pct - ( thresholdBarWidth - 1f ), barRect.y + barRect.height / 2f, thresholdBarWidth, barRect.height / 2f );
            Texture2D image;
            if ( pct < curLevel )
            {
                image = BaseContent.BlackTex;
                GUI.color = new Color( 1f, 1f, 1f, 0.9f );
            }
            else
            {
                image = BaseContent.GreyTex;
                GUI.color = new Color( 1f, 1f, 1f, 0.5f );
            }
            GUI.DrawTexture( position, image );
            GUI.color = Color.white;
        }

        public static string GetBulkTip( this Loadout loadout )
        {
            float baseBulkCapacity = ThingDefOf.Human.GetStatValueAbstract( CarryBulk );
            float workSpeedFactor = Mathf.Lerp( 1f, 0.75f, loadout.Bulk / baseBulkCapacity );

            return "CR.DetailedBaseBulkTip".Translate(
                CarryBulk.ValueToString( baseBulkCapacity, CarryBulk.toStringNumberSense ),
                CarryBulk.ValueToString( loadout.Bulk, CarryBulk.toStringNumberSense ),
                workSpeedFactor.ToStringPercent() );
        }

        public static string GetBulkTip( this Pawn pawn )
        {
            var comp = pawn.TryGetComp<CompInventory>();
            if ( comp != null )
                return "CR.DetailedBulkTip".Translate( CarryBulk.ValueToString( comp.capacityBulk, CarryBulk.toStringNumberSense ),
                                                       CarryBulk.ValueToString( comp.currentBulk, CarryBulk.toStringNumberSense ),
                                                       comp.workSpeedFactor.ToStringPercent() );
            else
                return String.Empty;
        }

        public static string GetBulkTip( this Thing thing, int count = 1 )
        {
            return
                "CR.Bulk".Translate() + ": " +
                Bulk.ValueToString( thing.GetStatValue( Bulk ) * count, Bulk.toStringNumberSense );
        }

        public static string GetBulkTip( this ThingDef def, int count = 1 )
        {
            return
                "CR.Bulk".Translate() + ": " +
                Bulk.ValueToString( def.GetStatValueAbstract( Bulk ) * count, Bulk.toStringNumberSense );
        }

        public static Loadout GetLoadout( this Pawn pawn )
        {
            if ( pawn == null )
                throw new ArgumentNullException( "pawn" );

            Loadout loadout;
            if ( !LoadoutManager.AssignedLoadouts.TryGetValue( pawn, out loadout ) )
            {
                LoadoutManager.AssignedLoadouts.Add( pawn, LoadoutManager.DefaultLoadout );
                loadout = LoadoutManager.DefaultLoadout;
            }
            return loadout;
        }

        public static string GetWeightAndBulkTip( this Loadout loadout )
        {
            return loadout.GetWeightTip() + "\n\n" + loadout.GetBulkTip();
        }

        public static string GetWeightAndBulkTip( this Pawn pawn )
        {
            return pawn.GetWeightTip() + "\n\n" + pawn.GetBulkTip();
        }

        public static string GetWeightAndBulkTip( this Thing thing )
        {
            return thing.LabelCap + "\n" + thing.GetWeightTip( thing.stackCount ) + "\n" + thing.GetBulkTip( thing.stackCount );
        }

        public static string GetWeightAndBulkTip( this ThingDef def, int count = 1 )
        {
            return def.LabelCap +
                ( count != 1 ? " x" + count : "" ) +
                "\n" + def.GetWeightTip( count ) + "\n" + def.GetBulkTip( count );
        }

        public static string GetWeightTip( this ThingDef def, int count = 1 )
        {
            return
                "CR.Weight".Translate() + ": " +
                Weight.ValueToString( def.GetStatValueAbstract( Weight ) * count, Weight.toStringNumberSense );
        }

        public static string GetWeightTip( this Thing thing, int count = 1 )
        {
            return
                "CR.Weight".Translate() + ": " +
                Weight.ValueToString( thing.GetStatValue( Weight ) * count, Weight.toStringNumberSense );
        }

        public static string GetWeightTip( this Loadout loadout )
        {
            float baseWeightCapacity = ThingDefOf.Human.GetStatValueAbstract( CarryWeight );
            float moveSpeedFactor = Mathf.Lerp( 1f, 0.75f, loadout.Weight / baseWeightCapacity );
            float encumberPenalty = loadout.Weight > baseWeightCapacity ?
                loadout.Weight / baseWeightCapacity - 1 :
                0f;

            return "CR.DetailedBaseWeightTip".Translate( CarryWeight.ValueToString( baseWeightCapacity, CarryWeight.toStringNumberSense ),
                                                 CarryWeight.ValueToString( loadout.Weight, CarryWeight.toStringNumberSense ),
                                                 moveSpeedFactor.ToStringPercent(),
                                                 encumberPenalty.ToStringPercent() );
        }

        public static string GetWeightTip( this Pawn pawn )
        {
            var comp = pawn.TryGetComp<CompInventory>();
            if ( comp != null )
                return "CR.DetailedWeightTip".Translate( CarryWeight.ValueToString( comp.capacityWeight, CarryWeight.toStringNumberSense ),
                                                     CarryWeight.ValueToString( comp.currentWeight, CarryWeight.toStringNumberSense ),
                                                     comp.moveSpeedFactor.ToStringPercent(),
                                                     comp.encumberPenalty.ToStringPercent() );
            else
                return "";
        }

        public static void SetLoadout( this Pawn pawn, Loadout loadout )
        {
            if ( pawn == null )
                throw new ArgumentNullException( "pawn" );

            if ( LoadoutManager.AssignedLoadouts.ContainsKey( pawn ) )
                LoadoutManager.AssignedLoadouts[pawn] = loadout;
            else
                LoadoutManager.AssignedLoadouts.Add( pawn, loadout );
        }

        #endregion Methods
    }
}