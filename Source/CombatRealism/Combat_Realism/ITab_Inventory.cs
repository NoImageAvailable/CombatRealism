using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public class ITab_Inventory : ITab_Pawn_Gear
    {

        #region Fields

        private const float _barHeight                 = 20f;
        private const float _margin                    = 6f;
        private const float _thingIconSize             = 28f;
        private const float _thingLeftX                = 36f;
        private const float _thingRowHeight            = 28f;
        private const float _topPadding                = 20f;
        private static readonly Color _highlightColor  = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color _thingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private Vector2 _scrollPosition                = Vector2.zero;

        private float _scrollViewHeight;

        #endregion Fields

        #region Constructors

        public ITab_Inventory() : base()
        {
        }

        #endregion Constructors

        #region Properties

        private bool CanEdit
        {
            get
            {
                return this.SelPawnForGear.IsColonistPlayerControlled;
            }
        }

        private Pawn SelPawnForGear
        {
            get
            {
                if ( base.SelPawn != null )
                {
                    return base.SelPawn;
                }
                Corpse corpse = base.SelThing as Corpse;
                if ( corpse != null )
                {
                    return corpse.innerPawn;
                }
                throw new InvalidOperationException( "Gear tab on non-pawn non-corpse " + base.SelThing );
            }
        }
        #endregion Properties

        protected override void FillTab()
        {
            // get the inventory comp
            CompInventory comp = SelPawn.TryGetComp<CompInventory>();

            // set up rects
            Rect listRect = new Rect( _margin,
                                           _topPadding,
                                           size.x - 2 * _margin,
                                           size.y - _topPadding - _margin );

            if ( comp != null )
            {
                // adjust rects if comp found
                listRect.height -= _margin * 4 + _barHeight * 2;
                Rect weightRect = new Rect( 0f, listRect.yMax + _margin, size.x, _barHeight + _margin * 2 );
                Rect bulkRect = new Rect( 0f, weightRect.yMax, size.x, _barHeight + _margin * 2 );

                // get size of label
                int labelSize = (int)( _margin + Math.Max( Text.CalcSize( "CR.Weight".Translate() ).x, Text.CalcSize( "CR.Bulk".Translate() ).x ) );

                // draw labels
                Rect weightLabelRect = new Rect( weightRect ).ContractedBy( _margin );
                Rect bulkLabelRect = new Rect( bulkRect ).ContractedBy( _margin );
                weightLabelRect.xMax = labelSize;
                bulkLabelRect.xMax = labelSize;
                Widgets.Label( weightLabelRect, "CR.Weight".Translate() );
                Widgets.Label( bulkLabelRect, "CR.Bulk".Translate() );

                // draw bars
                Rect weightBarRect = new Rect( weightRect ).ContractedBy( _margin );
                Rect bulkBarRect = new Rect( bulkRect ).ContractedBy( _margin );
                weightBarRect.xMin += labelSize;
                bulkBarRect.xMin += labelSize;

                bool overweight = comp.currentWeight > comp.capacityWeight;
                float weightFillPercentage = overweight ? 1f : comp.currentWeight / comp.capacityWeight;
                Widgets.DrawHighlightIfMouseover( weightRect );
                Widgets.FillableBar( weightBarRect, weightFillPercentage );
                if ( overweight )
                {
                    Widgets.FillableBar( weightBarRect, weightFillPercentage );
                    Utility_UI.DrawBarThreshold( weightRect, comp.capacityWeight / comp.currentWeight, 1f );
                }
                else
                    Widgets.FillableBar( weightBarRect, weightFillPercentage );

                bool overbulk = comp.currentBulk > comp.capacityBulk;
                float bulkFillPercentage = overbulk ? 1f : comp.currentBulk / comp.capacityBulk;
                Widgets.DrawHighlightIfMouseover( bulkRect );
                if ( overbulk )
                {
                    Utility_UI.DrawBarThreshold( bulkRect, comp.capacityBulk / comp.currentBulk, 1f );
                }
                else
                    Widgets.FillableBar( bulkBarRect, bulkFillPercentage );

                // tooltips
                TooltipHandler.TipRegion( bulkRect, "CR.ITabBulkTip".Translate( comp.capacityBulk, comp.currentBulk, comp.workSpeedFactor ) );
                TooltipHandler.TipRegion( weightRect, "CR.ITabWeightTip".Translate( comp.capacityWeight, comp.currentWeight, comp.moveSpeedFactor, comp.encumberPenalty ) );
            }

            // start drawing list (rip from ITab_Pawn_Gear)
            GUI.BeginGroup( listRect );
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect( 0f, 0f, listRect.width, listRect.height );
            Rect viewRect = new Rect( 0f, 0f, listRect.width - 16f, this._scrollViewHeight );
            Widgets.BeginScrollView( outRect, ref this._scrollPosition, viewRect );
            float curY = 0f;
            if ( this.SelPawnForGear.equipment != null )
            {
                Widgets.ListSeparator( ref curY, viewRect.width, "Equipment".Translate() );
                foreach ( ThingWithComps current in this.SelPawnForGear.equipment.AllEquipment )
                {
                    this.DrawThingRow( ref curY, viewRect.width, current );
                }
            }
            if ( this.SelPawnForGear.apparel != null )
            {
                Widgets.ListSeparator( ref curY, viewRect.width, "Apparel".Translate() );
                foreach ( Apparel current2 in from ap in this.SelPawnForGear.apparel.WornApparel
                                              orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                              select ap )
                {
                    this.DrawThingRow( ref curY, viewRect.width, current2 );
                }
            }
            if ( this.SelPawnForGear.inventory != null )
            {
                Widgets.ListSeparator( ref curY, viewRect.width, "Inventory".Translate() );
                foreach ( Thing current3 in this.SelPawnForGear.inventory.container )
                {
                    this.DrawThingRow( ref curY, viewRect.width, current3 );
                }
            }
            if ( Event.current.type == EventType.Layout )
            {
                this._scrollViewHeight = curY + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void DrawThingRow( ref float y, float width, Thing thing )
        {
            Rect rect = new Rect( 0f, y, width, 28f );
            if ( Mouse.IsOver( rect ) )
            {
                GUI.color = _highlightColor;
                GUI.DrawTexture( rect, TexUI.HighlightTex );
            }
            if ( Widgets.InvisibleButton( rect ) && Event.current.button == 1 )
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add( new FloatMenuOption( "ThingInfo".Translate(), delegate
                {
                    Find.WindowStack.Add( new Dialog_InfoCard( thing ) );
                }, MenuOptionPriority.Medium, null, null ) );
                if ( this.CanEdit )
                {
                    Action action = null;
                    ThingWithComps eq = thing as ThingWithComps;
                    Apparel ap = thing as Apparel;
                    if ( ap != null )
                    {
                        Apparel unused;
                        action = delegate
                        {
                            this.SelPawnForGear.apparel.TryDrop( ap, out unused, this.SelPawnForGear.Position, true );
                        };
                    }
                    else if ( eq != null && this.SelPawnForGear.equipment.AllEquipment.Contains( eq ) )
                    {
                        ThingWithComps unused;
                        action = delegate
                        {
                            this.SelPawnForGear.equipment.TryDropEquipment( eq, out unused, this.SelPawnForGear.Position, true );
                        };
                    }
                    else if ( !thing.def.destroyOnDrop )
                    {
                        Thing unused;
                        action = delegate
                        {
                            this.SelPawnForGear.inventory.container.TryDrop( thing, this.SelPawnForGear.Position, ThingPlaceMode.Near, out unused );
                        };
                    }
                    list.Add( new FloatMenuOption( "DropThing".Translate(), action, MenuOptionPriority.Medium, null, null ) );
                }
                FloatMenu window = new FloatMenu( list, thing.LabelCap, false, false );
                Find.WindowStack.Add( window );
            }
            if ( thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null )
            {
                Widgets.ThingIcon( new Rect( 4f, y, 28f, 28f ), thing );
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = _thingLabelColor;
            Rect rect2 = new Rect( 36f, y, width - 36f, 28f );
            string text = thing.LabelCap;
            if ( thing is Apparel && this.SelPawnForGear.outfits != null && this.SelPawnForGear.outfits.forcedHandler.IsForced( (Apparel)thing ) )
            {
                text = text + ", " + "ApparelForcedLower".Translate();
            }
            Widgets.Label( rect2, text );
            y += 28f;
        }

    }
}