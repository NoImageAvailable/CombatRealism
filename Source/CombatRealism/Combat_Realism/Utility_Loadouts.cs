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