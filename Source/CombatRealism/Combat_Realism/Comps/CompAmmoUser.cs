using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Combat_Realism
{
    public class CompAmmoUser : CommunityCoreLibrary.CompRangedGizmoGiver
    {
        #region Fields

        public AmmoDef selectedAmmo;

        private int curMagCountInt;

        private AmmoDef currentAmmoInt = null;

        private JobDef storedJobDef = null;

        private TargetInfo storedTarget = null;

        #endregion Fields

        #region Properties

        public CompEquippable compEquippable
        {
            get { return parent.GetComp<CompEquippable>(); }
        }

        public CompInventory compInventory
        {
            get
            {
                return wielder.TryGetComp<CompInventory>();
            }
        }

        public int curMagCount
        {
            get
            {
                return curMagCountInt;
            }
        }

        public AmmoDef currentAmmo
        {
            get
            {
                return currentAmmoInt;
            }
        }

        public bool hasAmmo
        {
            get
            {
                if ( compInventory == null )
                    return false;
                return compInventory.ammoList.Any( x => Props.ammoSet.ammoTypes.Contains( x.def ) );
            }
        }

        public bool hasMagazine => Props.magazineSize > 0;

        public CompProperties_AmmoUser Props
        {
            get
            {
                return (CompProperties_AmmoUser)this.props;
            }
        }

        // Ammo consumption variables
        public bool useAmmo
        {
            get
            {
                return Props.ammoSet != null;
            }
        }

        public Pawn wielder
        {
            get { return compEquippable.PrimaryVerb.CasterPawn; }
        }

        #endregion Properties

        #region Methods

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            var ammoStatusGizmo = new GizmoAmmoStatus { compAmmo = this };
            yield return ammoStatusGizmo;

            if ( this.wielder != null )
            {
                var reloadCommandGizmo = new Command_Reload
                {
                    compAmmo = this,
                    action = this.StartReload,
                    defaultLabel = hasMagazine ? "CR_ReloadLabel".Translate() : "",
                    defaultDesc = "CR_ReloadDesc".Translate(),
                    icon = this.currentAmmo == null ? ContentFinder<Texture2D>.Get( "UI/Buttons/Reload", true ) : CommunityCoreLibrary.Def_Extensions.IconTexture( this.selectedAmmo )
                };
                yield return reloadCommandGizmo;
            }
        }

        public void FinishReload()
        {
            if ( useAmmo )
            {
                // Check for inventory
                if ( compInventory != null )
                {
                    Thing ammoThing;
                    this.TryFindAmmoInInventory( out ammoThing );

                    if ( ammoThing == null )
                    {
                        this.DoOutOfAmmoAction();
                        return;
                    }
                    currentAmmoInt = (AmmoDef)ammoThing.def;
                    if ( Props.magazineSize < ammoThing.stackCount )
                    {
                        curMagCountInt = Props.magazineSize;
                        ammoThing.stackCount -= Props.magazineSize;
                        compInventory.UpdateInventory();
                    }
                    else
                    {
                        curMagCountInt = ammoThing.stackCount;
                        compInventory.container.Remove( ammoThing );
                    }
                }
            }
            else
            {
                curMagCountInt = Props.magazineSize;
            }
            parent.def.soundInteract.PlayOneShot( SoundInfo.InWorld( wielder.Position ) );
            if ( Props.throwMote )
            {
                MoteThrower.ThrowText( wielder.Position.ToVector3Shifted(), "CR_ReloadedMote".Translate() );
            }
        }

        public override string GetDescriptionPart()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine( "CR_MagazineSize".Translate() + ": " + GenText.ToStringByStyle( this.Props.magazineSize, ToStringStyle.Integer ) );
            stringBuilder.AppendLine( "CR_ReloadTime".Translate() + ": " + GenText.ToStringByStyle( ( this.Props.reloadTicks / 60 ), ToStringStyle.Integer ) + " s" );
            if ( Props.ammoSet != null )
                stringBuilder.AppendLine( "CR_AmmoSet".Translate() + ": " + Props.ammoSet.LabelCap );
            return stringBuilder.ToString();
        }

        public override void Initialize( CompProperties vprops )
        {
            base.Initialize( vprops );

            curMagCountInt = Props.magazineSize;

            // Initialize ammo with default if none is set
            if ( useAmmo )
            {
                if ( Props.ammoSet.ammoTypes.NullOrEmpty() )
                {
                    Log.Error( this.parent.Label + " has no available ammo types" );
                }
                else
                {
                    if ( currentAmmoInt == null )
                        currentAmmoInt = (AmmoDef)Props.ammoSet.ammoTypes[0];
                    if ( selectedAmmo == null )
                        selectedAmmo = currentAmmoInt;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue( ref curMagCountInt, "count", 0 );
            Scribe_Defs.LookDef( ref currentAmmoInt, "currentAmmo" );
            Scribe_Defs.LookDef( ref selectedAmmo, "selectedAmmo" );
        }

        public void StartReload()
        {
            if ( wielder == null )
            {
                Log.ErrorOnce( "Wielder of " + parent + " is null!", 7381889 );
                FinishReload();
                return;
            }

            if ( !hasMagazine )
            {
                return;
            }

            if ( useAmmo )
            {
                // Add remaining ammo back to inventory
                if ( curMagCountInt > 0 )
                {
                    Thing ammoThing = ThingMaker.MakeThing( currentAmmoInt );
                    ammoThing.stackCount = curMagCountInt;
                    curMagCountInt = 0;

                    if ( compInventory != null )
                    {
                        compInventory.UpdateInventory();
                        compInventory.container.TryAdd( ammoThing, ammoThing.stackCount );
                    }
                    else
                    {
                        Thing outThing;
                        GenThing.TryDropAndSetForbidden( ammoThing, wielder.Position, ThingPlaceMode.Near, out outThing, true );
                    }
                }
                // Check for ammo
                if ( !hasAmmo )
                {
                    this.DoOutOfAmmoAction();
                    return;
                }
            }

            // Throw mote
            if ( Props.throwMote )
            {
                MoteThrower.ThrowText( wielder.Position.ToVector3Shifted(), "CR_ReloadingMote".Translate() );
            }

            // Issue reload job
            var reloadJob = new Job( DefDatabase<JobDef>.GetNamed( "ReloadWeapon" ), wielder, parent )
            {
                playerForced = true
            };

            // Store the current job so we can reassign it later
            if ( this.wielder.drafter != null
                && this.wielder.CurJob != null
                && ( this.wielder.CurJob.def == JobDefOf.AttackStatic || this.wielder.CurJob.def == JobDefOf.Goto ) )
            {
                this.storedTarget = this.wielder.CurJob.targetA.HasThing ? new TargetInfo( this.wielder.CurJob.targetA.Thing ) : new TargetInfo( this.wielder.CurJob.targetA.Cell );
                this.storedJobDef = this.wielder.CurJob.def;
            }
            this.AssignJobToWielder( reloadJob );
        }

        public void TryContinuePreviousJob()
        {
            //If a job is stored, assign it
            if ( this.storedTarget != null && this.storedJobDef != null )
            {
                this.AssignJobToWielder( new Job( this.storedJobDef, this.storedTarget ) );

                //Clear out stored job after assignment
                this.storedTarget = null;
                this.storedJobDef = null;
            }
        }

        /// <summary>
        /// Reduces ammo count and updates inventory if necessary, call this whenever ammo is consumed by the gun (e.g. firing a shot, clearing a jam)
        /// </summary>
        public bool TryReduceAmmoCount()
        {
            // Mag-less weapons feed directly from inventory
            if ( !hasMagazine )
            {
                if ( useAmmo )
                {
                    Thing ammo;

                    if ( !TryFindAmmoInInventory( out ammo ) )
                    {
                        return false;
                    }

                    if ( ammo.stackCount > 1 )
                        ammo = ammo.SplitOff( 1 );

                    ammo.Destroy();
                    compInventory.UpdateInventory();
                }
                return true;
            }
            // If magazine is empty, return false
            else if ( curMagCountInt <= 0 )
            {
                curMagCountInt = 0;
                return false;
            }
            // Reduce ammo count and update inventory
            curMagCountInt--;
            if ( compInventory != null )
            {
                compInventory.UpdateInventory();
            }
            return true;
        }

        private void AssignJobToWielder( Job job )
        {
            if ( wielder.drafter != null )
            {
                wielder.drafter.TakeOrderedJob( job );
            }
            else
            {
                ExternalPawnDrafter.TakeOrderedJob( wielder, job );
            }
        }

        private void DoOutOfAmmoAction()
        {
            if ( Props.throwMote )
                MoteThrower.ThrowText( wielder.Position.ToVector3Shifted(), "CR_OutOfAmmo".Translate() + "!" );
            if ( compInventory != null )
                compInventory.SwitchToNextViableWeapon();
            if ( wielder != null && wielder.jobs != null )
                wielder.jobs.StopAll();
        }

        private bool TryFindAmmoInInventory( out Thing ammoThing )
        {
            ammoThing = null;
            if ( compInventory == null )
            {
                return false;
            }

            // Try finding suitable ammoThing for currently set ammo first
            ammoThing = compInventory.ammoList.Find( thing => thing.def == selectedAmmo );
            if ( ammoThing != null )
            {
                return true;
            }

            // Try finding ammo from different type
            foreach ( AmmoDef ammoDef in Props.ammoSet.ammoTypes )
            {
                ammoThing = compInventory.ammoList.Find( thing => thing.def == ammoDef );
                if ( ammoThing != null )
                {
                    selectedAmmo = ammoDef;
                    return true;
                }
            }
            return false;
        }

        #endregion Methods
    }
}