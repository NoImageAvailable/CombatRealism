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

        #region Methods

        protected override void FillTab()
        {
            // get the inventory comp
            CompInventory comp = SelPawn.TryGetComp<CompInventory>();

            // set up rects
            Rect listRect = new Rect(
                _margin,
                _topPadding,
                size.x - 2 * _margin,
                size.y - _topPadding - _margin );

            if ( comp != null )
            {
                // adjust rects if comp found
                listRect.height -= ( _margin + _barHeight ) * 2;
                Rect weightRect = new Rect( _margin, listRect.yMax + _margin, listRect.width, _barHeight );
                Rect bulkRect = new Rect( _margin, weightRect.yMax + _margin, listRect.width, _barHeight );

                Utility_Loadouts.DrawBar( bulkRect, comp.currentBulk, comp.capacityBulk, "CR.Bulk".Translate(), SelPawn.GetBulkTip() );
                Utility_Loadouts.DrawBar( weightRect, comp.currentWeight, comp.capacityWeight, "CR.Weight".Translate(), SelPawn.GetWeightTip() );
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
            TooltipHandler.TipRegion( rect, thing.GetWeightAndBulkTip() );
            if ( Mouse.IsOver( rect ) )
            {
                GUI.color = _highlightColor;
                GUI.DrawTexture( rect, TexUI.HighlightTex );
            }
            if ( Widgets.InvisibleButton( rect ) && Event.current.button == 1 )
            {
                List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                floatOptionList.Add( new FloatMenuOption( "ThingInfo".Translate(), delegate
                {
                    Find.WindowStack.Add( new Dialog_InfoCard( thing ) );
                }, MenuOptionPriority.Medium, null, null ) );
                if ( this.CanEdit )
                {
                    // Equip option
                    ThingWithComps eq = thing as ThingWithComps;
                    if ( eq != null && eq.TryGetComp<CompEquippable>() != null )
                    {
                        CompInventory compInventory = SelPawnForGear.TryGetComp<CompInventory>();
                        if ( compInventory != null )
                        {
                            FloatMenuOption equipOption;
                            string eqLabel = GenLabel.ThingLabel( eq.def, eq.Stuff, 1 );
                            if ( SelPawnForGear.equipment.AllEquipment.Contains( eq ) && SelPawnForGear.inventory != null )
                            {
                                equipOption = new FloatMenuOption( "CR_PutAway".Translate( new object[] { eqLabel } ),
                                    new Action( delegate
                                     {
                                         ThingWithComps oldEq;
                                         SelPawnForGear.equipment.TryTransferEquipmentToContainer( SelPawnForGear.equipment.Primary, SelPawnForGear.inventory.container, out oldEq );
                                     } ) );
                            }
                            else if ( !SelPawnForGear.health.capacities.CapableOf( PawnCapacityDefOf.Manipulation ) )
                            {
                                equipOption = new FloatMenuOption( "CannotEquip".Translate( new object[] { eqLabel } ), null );
                            }
                            else
                            {
                                string equipOptionLabel = "Equip".Translate( new object[] { eqLabel } );
                                if ( eq.def.IsRangedWeapon && SelPawnForGear.story != null && SelPawnForGear.story.traits.HasTrait( TraitDefOf.Brawler ) )
                                {
                                    equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
                                }
                                equipOption = new FloatMenuOption( equipOptionLabel, new Action( delegate
                                  {
                                      compInventory.TrySwitchToWeapon( eq );
                                  } ) );
                            }
                            floatOptionList.Add( equipOption );
                        }
                    }

                    // Drop option
                    Action action = null;
                    Apparel ap = thing as Apparel;
                    if ( ap != null && SelPawnForGear.apparel.WornApparel.Contains( ap ) )
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
                    floatOptionList.Add( new FloatMenuOption( "DropThing".Translate(), action, MenuOptionPriority.Medium, null, null ) );
                }
                FloatMenu window = new FloatMenu( floatOptionList, thing.LabelCap, false, false );
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

        #endregion Methods
    }
}