using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Combat_Realism
{
    public class CompAmmoUser : CommunityCoreLibrary.CompRangedGizmoGiver
    {
        #region Fields
        
        private int curMagCountInt;
        private TargetInfo storedTarget = null;
        private JobDef storedJobDef = null;
        private AmmoDef currentAmmoInt = null;
        public AmmoDef selectedAmmo;

        public Building_TurretGunCR turret;         // Cross-linked from CR turret

        #endregion

        #region Properties

        public CompProperties_AmmoUser Props
        {
            get
            {
                return (CompProperties_AmmoUser)this.props;
            }
        }

        public int curMagCount
        {
            get
            {
                return curMagCountInt;
            }
        }
        public CompEquippable compEquippable
        {
            get { return parent.GetComp<CompEquippable>(); }
        }
        public Pawn wielder
        {
            get
            {
                if (compEquippable == null || compEquippable.PrimaryVerb == null)
                {
                    return null;
                }
                return compEquippable.PrimaryVerb.CasterPawn;
            }
        }
        public bool useAmmo
        {
            get
            {
                return Props.ammoSet != null;
            }
        }
        public bool hasAmmo
        {
            get
            {
                if (compInventory == null)
                    return false;
                return compInventory.ammoList.Any(x => Props.ammoSet.ammoTypes.Contains(x.def));
            }
        }
        public bool hasMagazine => Props.magazineSize > 0;
        public AmmoDef currentAmmo
        {
            get
            {
                return currentAmmoInt;
            }
        }
        public CompInventory compInventory
        {
            get
            {
                return wielder.TryGetComp<CompInventory>();
            }
        }
        private IntVec3 position
        {
            get
            {
                if (wielder != null) return wielder.Position;
                else if (turret != null) return turret.Position;
                else return parent.Position;
            }
        }

        #endregion

        #region Methods

        public override void Initialize(CompProperties vprops)
        {
            base.Initialize(vprops);

            curMagCountInt = Props.spawnUnloaded ? 0 : Props.magazineSize;

            // Initialize ammo with default if none is set
            if (useAmmo)
            {
                if (Props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    Log.Error(this.parent.Label + " has no available ammo types");
                }
                else
                {
                    if (currentAmmoInt == null)
                        currentAmmoInt = (AmmoDef)Props.ammoSet.ammoTypes[0];
                    if (selectedAmmo == null)
                        selectedAmmo = currentAmmoInt;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue(ref curMagCountInt, "count", 0);
            Scribe_Defs.LookDef(ref currentAmmoInt, "currentAmmo");
            Scribe_Defs.LookDef(ref selectedAmmo, "selectedAmmo");
        }

        private void AssignJobToWielder(Job job)
        {
            if (wielder.drafter != null)
            {
                wielder.drafter.TakeOrderedJob(job);
            }
            else
            {
                ExternalPawnDrafter.TakeOrderedJob(wielder, job);
            }
        }

        /// <summary>
        /// Reduces ammo count and updates inventory if necessary, call this whenever ammo is consumed by the gun (e.g. firing a shot, clearing a jam)
        /// </summary>
        public bool TryReduceAmmoCount()
        {
            if (wielder == null && turret == null)
            {
                Log.Error(parent.ToString() + " tried reducing its ammo count without a wielder");
            }

            // Mag-less weapons feed directly from inventory
            if (!hasMagazine)
            {
                if (useAmmo)
                {
                    Thing ammo;

                    if (!TryFindAmmoInInventory(out ammo))
                    {
                        return false;
                    }

                    if (ammo.stackCount > 1)
                        ammo = ammo.SplitOff(1);

                    ammo.Destroy();
                    compInventory.UpdateInventory();
                    if (!hasAmmo) DoOutOfAmmoAction();
                }
                return true;
            }
            // If magazine is empty, return false
            else if (curMagCountInt <= 0)
            {
                curMagCountInt = 0;
                return false;
            }
            // Reduce ammo count and update inventory
            curMagCountInt--;
            if (compInventory != null)
            {
                compInventory.UpdateInventory();
            }
            if (curMagCountInt <= 0) TryStartReload();
            return true;
        }

        public void TryStartReload()
        {
            if (!hasMagazine)
            {
                return;
            }
            IntVec3 position;
            if (wielder == null)
            {
                if (turret == null) return;
                turret.isReloading = true;
                position = turret.Position;
            }
            else
            {
                position = wielder.Position;
            }

            if (useAmmo)
            {
                // Add remaining ammo back to inventory
                if (curMagCountInt > 0)
                {
                    Thing ammoThing = ThingMaker.MakeThing(currentAmmoInt);
                    ammoThing.stackCount = curMagCountInt;
                    curMagCountInt = 0;

                    if (compInventory != null)
                    {
                        compInventory.container.TryAdd(ammoThing, ammoThing.stackCount);
                    }
                    else
                    {
                        Thing outThing;
                        GenThing.TryDropAndSetForbidden(ammoThing, position, ThingPlaceMode.Near, out outThing, turret.Faction != Faction.OfColony);
                    }
                }
                // Check for ammo
                if (wielder != null && !hasAmmo)
                {
                    this.DoOutOfAmmoAction();
                    return;
                }
            }

            // Throw mote
            if (Props.throwMote)
            {
                MoteThrower.ThrowText(position.ToVector3Shifted(), "CR_ReloadingMote".Translate());
            }

            // Issue reload job
            if (wielder != null)
            {
                var reloadJob = new Job(DefDatabase<JobDef>.GetNamed("ReloadWeapon"), wielder, parent)
                {
                    playerForced = true
                };

                // Store the current job so we can reassign it later
                if (this.wielder.Faction == Faction.OfColony
                    && this.wielder.CurJob != null
                    && (this.wielder.CurJob.def == JobDefOf.AttackStatic || this.wielder.CurJob.def == JobDefOf.Goto || wielder.CurJob.def == JobDefOf.Hunt))
                {
                    this.storedTarget = this.wielder.CurJob.targetA.HasThing ? new TargetInfo(this.wielder.CurJob.targetA.Thing) : new TargetInfo(this.wielder.CurJob.targetA.Cell);
                    this.storedJobDef = this.wielder.CurJob.def;
                }
                else
                {
                    storedTarget = null;
                    storedJobDef = null;
                }
                this.AssignJobToWielder(reloadJob);
            }
        }

        private void DoOutOfAmmoAction()
        {
            if (Props.throwMote)
            {
                MoteThrower.ThrowText(position.ToVector3Shifted(), "CR_OutOfAmmo".Translate() + "!");
            }
            if (wielder != null && compInventory != null && (wielder.jobs == null || wielder.CurJob.def != JobDefOf.Hunt)) compInventory.SwitchToNextViableWeapon();
        }

        public void LoadAmmo(Thing ammo = null)
        {
            if (wielder == null && turret == null)
            {
                Log.Error(parent.ToString() + " tried loading ammo with no owner");
                return;
            }

            int newMagCount;
            if (useAmmo)
            {
                Thing ammoThing;
                bool ammoFromInventory = false;
                if (ammo == null)
                {
                    if (!TryFindAmmoInInventory(out ammoThing))
                    {
                        this.DoOutOfAmmoAction();
                        return;
                    }
                    ammoFromInventory = true;
                }
                else
                {
                    ammoThing = ammo;
                }
                currentAmmoInt = (AmmoDef)ammoThing.def;
                if (Props.magazineSize < ammoThing.stackCount)
                {
                    newMagCount = Props.magazineSize;
                    ammoThing.stackCount -= Props.magazineSize;
                    if(compInventory != null) compInventory.UpdateInventory();
                }
                else
                {
                    newMagCount = ammoThing.stackCount;
                    if (ammoFromInventory)
                    {
                        compInventory.container.Remove(ammoThing);
                    }
                    else if (!ammoThing.Destroyed)
                    {
                        ammoThing.Destroy();
                    }
                }
            }
            else
            {
                newMagCount = Props.magazineSize;
            }
            curMagCountInt = newMagCount;
            if (turret != null) turret.isReloading = false;
            if (parent.def.soundInteract != null) parent.def.soundInteract.PlayOneShot(SoundInfo.InWorld(position));
            if (Props.throwMote) MoteThrower.ThrowText(position.ToVector3Shifted(), "CR_ReloadedMote".Translate());
        }

        private bool TryFindAmmoInInventory(out Thing ammoThing)
        {
            ammoThing = null;
            if (compInventory == null)
            {
                return false;
            }

            // Try finding suitable ammoThing for currently set ammo first
            ammoThing = compInventory.ammoList.Find(thing => thing.def == selectedAmmo);
            if (ammoThing != null)
            {
                return true;
            }
            
            // Try finding ammo from different type
            foreach (AmmoDef ammoDef in Props.ammoSet.ammoTypes)
            {
                ammoThing = compInventory.ammoList.Find(thing => thing.def == ammoDef);
                if (ammoThing != null)
                {
                    selectedAmmo = ammoDef;
                    return true;
                }
            }
            return false;
        }

        public void TryContinuePreviousJob()
        {
            //If a job is stored, assign it
            if (this.storedTarget != null && this.storedJobDef != null)
            {
                this.AssignJobToWielder(new Job(this.storedJobDef, this.storedTarget));

                //Clear out stored job after assignment
                this.storedTarget = null;
                this.storedJobDef = null;
            }
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            var ammoStatusGizmo = new GizmoAmmoStatus { compAmmo = this };
            yield return ammoStatusGizmo;

            if ((this.wielder != null && wielder.Faction == Faction.OfColony) || (turret != null && turret.Faction == Faction.OfColony))
            {
                Action action = null;
                if (wielder != null) action = TryStartReload;
                else if (turret != null && turret.GetMannableComp() != null) action = turret.OrderReload;

                var reloadCommandGizmo = new Command_Reload
                {
                    compAmmo = this,
                    action = action,
                    defaultLabel = hasMagazine ? "CR_ReloadLabel".Translate() : "",
                    defaultDesc = "CR_ReloadDesc".Translate(),
                    icon = this.currentAmmo == null ? ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true) : CommunityCoreLibrary.Def_Extensions.IconTexture(this.selectedAmmo)
                };
                yield return reloadCommandGizmo;
            }
        }

        public override string GetDescriptionPart()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("CR_MagazineSize".Translate() + ": " + GenText.ToStringByStyle(this.Props.magazineSize, ToStringStyle.Integer));
            stringBuilder.AppendLine("CR_ReloadTime".Translate() + ": " + GenText.ToStringByStyle((this.Props.reloadTicks / 60), ToStringStyle.Integer) + " s");
            if (Props.ammoSet != null)
                stringBuilder.AppendLine("CR_AmmoSet".Translate() + ": " + Props.ammoSet.LabelCap);
            return stringBuilder.ToString();
        }

        #endregion
    }
}
