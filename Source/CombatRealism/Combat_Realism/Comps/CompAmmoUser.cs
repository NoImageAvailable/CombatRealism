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
        public CompProperties_AmmoUser Props
        {
            get
            {
                return (CompProperties_AmmoUser)this.props;
            }
        }

        private int curAmmoCountInt;
        public int curMagCount
        {
            get
            {
                return curAmmoCountInt;
            }
        }
        public CompEquippable compEquippable
        {
            get { return parent.GetComp<CompEquippable>(); }
        }
        public Pawn wielder
        {
            get { return compEquippable.PrimaryVerb.CasterPawn; }
        }
        private TargetInfo storedTarget = null;
        private JobDef storedJobDef = null;

        // Ammo consumption variables
        public bool useAmmo
        {
            get
            {
                return Props.ammoSet != null;
            }
        }
        private AmmoDef currentAmmoInt = null;
        public AmmoDef currentAmmo
        {
            get
            {
                return currentAmmoInt;
            }
        }
        public AmmoDef selectedAmmo;
        public CompInventory compInventory
        {
            get
            {
                return wielder.TryGetComp<CompInventory>();
            }
        }

        public override void Initialize(CompProperties vprops)
        {
            base.Initialize(vprops);

            curAmmoCountInt = Props.magazineSize;

            // Initialize ammo with default if none is set
            if (useAmmo)
            {
                if (Props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    Log.Error(this.parent.Label + " has no available ammo types");
                }
                else
                {
                    if(currentAmmoInt == null)
                        currentAmmoInt = (AmmoDef)Props.ammoSet.ammoTypes[0];
                    if (selectedAmmo == null)
                        selectedAmmo = currentAmmoInt;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue(ref curAmmoCountInt, "count", 1);
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
        public void ReduceAmmoCount()
        {
            if (curAmmoCountInt <= 0)
            {
                Log.Error("Tried reducing current ammo of " + this.parent.ToString() + " below zero");
                curAmmoCountInt = 0;
                return;
            }
            curAmmoCountInt--;
            if (compInventory != null)
                compInventory.UpdateInventory();
        }

        public void StartReload()
        {
            if (wielder == null)
            {
                Log.ErrorOnce("Wielder of " + parent + " is null!", 7381889);
                FinishReload();
                return;
            }

            if (useAmmo)
            {
                // Add remaining ammo back to inventory
                if (curAmmoCountInt > 0)
                {
                    Thing ammoThing = ThingMaker.MakeThing(currentAmmoInt);
                    ammoThing.stackCount = curAmmoCountInt;
                    curAmmoCountInt = 0;

                    if (compInventory != null)
                    {
                        compInventory.UpdateInventory();
                        compInventory.container.TryAdd(ammoThing, ammoThing.stackCount);
                    }
                    else
                    {
                        Thing outThing;
                        GenThing.TryDropAndSetForbidden(ammoThing, wielder.Position, ThingPlaceMode.Near, out outThing, true);
                    }
                }
                // Check for ammo
                if (!compInventory.ammoList.Any(x => Props.ammoSet.ammoTypes.Contains(x.def)))
                {
                    this.DoOutOfAmmoAction();
                    return;
                }
            }

            // Throw mote
            if (Props.throwMote)
            {
                MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "CR_ReloadingMote".Translate());
            }

            // Issue reload job
            var reloadJob = new Job(DefDatabase<JobDef>.GetNamed("ReloadWeapon"), wielder, parent)
            {
                playerForced = true
            };

            // Store the current job so we can reassign it later
            if (this.wielder.drafter != null
                && this.wielder.CurJob != null
                && (this.wielder.CurJob.def == JobDefOf.AttackStatic || this.wielder.CurJob.def == JobDefOf.Goto))
            {
                this.storedTarget = this.wielder.CurJob.targetA.HasThing ? new TargetInfo(this.wielder.CurJob.targetA.Thing) : new TargetInfo(this.wielder.CurJob.targetA.Cell);
                this.storedJobDef = this.wielder.CurJob.def;
            }
            this.AssignJobToWielder(reloadJob);
        }

        private void DoOutOfAmmoAction()
        {
            if(Props.throwMote)
                MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "CR_OutOfAmmo".Translate() + "!");
            compInventory.SwitchToNextViableWeapon();
        }

        public void FinishReload()
        {
            if (useAmmo)
            {
                // Check for inventory
                if (compInventory != null)
                {
                    Thing ammoThing;
                    this.TryFindAmmoInInventory(out ammoThing);
                    
                    if (ammoThing == null)
                    {
                        this.DoOutOfAmmoAction();
                        return;
                    }
                    currentAmmoInt = (AmmoDef)ammoThing.def;
                    if (Props.magazineSize < ammoThing.stackCount)
                    {
                        curAmmoCountInt = Props.magazineSize;
                        ammoThing.stackCount -= Props.magazineSize;
                    }
                    else
                    {
                        curAmmoCountInt = ammoThing.stackCount;
                        compInventory.container.Remove(ammoThing);
                    }
                }
            }
            else
            {
                curAmmoCountInt = Props.magazineSize;
            }
            parent.def.soundInteract.PlayOneShot(SoundInfo.InWorld(wielder.Position));
            if (Props.throwMote)
            {
                MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "CR_ReloadedMote".Translate());
            }
        }

        private bool TryFindAmmoInInventory(out Thing ammoThing)
        {
            ammoThing = null;
            if (compInventory == null)
            {
                Log.Error(this.parent.ToString() + " tried searching inventory for ammo with no CompInventory");
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

            if (this.wielder != null)
            {
                var reloadCommandGizmo = new Command_Reload
                {
                    compAmmo = this,
                    action = this.StartReload,
                    defaultLabel = "CR_ReloadLabel".Translate(),
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
    }
}
