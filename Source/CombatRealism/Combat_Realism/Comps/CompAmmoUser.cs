using System;
using System.Collections.Generic;
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
        private bool useAmmo
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
        private CompInventory compInventory
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
            Log.Message(this.parent.ToString() + " uses ammo " + useAmmo.ToString());
            Log.Message(this.parent.ToString() + " has current ammo " + (currentAmmoInt != null).ToString());
            Log.Message((useAmmo && currentAmmoInt == null).ToString());
            if (useAmmo && currentAmmoInt == null)
            {
                if (Props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    Log.Error(this.parent.Label + " failed to initialize with default ammo");
                }
                else
                {
                    currentAmmoInt = (AmmoDef)Props.ammoSet.ammoTypes[0];
                    Log.Message("Initialize :: set currentAmmoInt to " + currentAmmoInt.ToString());
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue(ref curAmmoCountInt, "count", 1);
            Scribe_Defs.LookDef(ref currentAmmoInt, "currentAmmo");
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
                Log.Message("StartReload :: " + this.parent.Label + " uses ammo, checking hasAmmo");
                // Check for ammo
                Thing thing;
                if (!TryFindAmmoInInventory(out thing))
                {
                    Log.Message("StartReload :: " + this.parent.Label + " has no ammo, returning");
                    if (Props.throwMote)
                    {
                        MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "Out of ammo");
                    }
                    return;
                }

                // Add remaining ammo back to inventory
                if (curAmmoCountInt > 0)
                {
                    Thing ammoThing = ThingMaker.MakeThing(currentAmmoInt);
                    //GenSpawn.Spawn(ammoThing, this.parent.Position);
                    ammoThing.stackCount = curAmmoCountInt;
                    curAmmoCountInt = 0;

                    if (compInventory != null)
                    {
                        Log.Message("StartReload :: Adding ammo " + ammoThing.Label + " to " + wielder.ToString());
                        compInventory.UpdateInventory();
                        compInventory.container.TryAdd(ammoThing, ammoThing.stackCount);
                    }
                    else
                    {
                        Thing outThing;
                        GenThing.TryDropAndSetForbidden(ammoThing, wielder.Position, ThingPlaceMode.Near, out outThing, true);
                    }
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

        public void FinishReload()
        {
            if (useAmmo)
            {
                // Check for inventory
                if (compInventory != null)
                {
                    Thing ammoThing = compInventory.ammoList.Find(ammo => ammo.def == currentAmmoInt);

                    // If we don't have the right ammo, try to switch to different type
                    if (ammoThing == null)
                    {
                        if (!compInventory.ammoList.NullOrEmpty())
                        {
                            ammoThing = compInventory.ammoList.Find(ammo => Props.ammoSet.ammoTypes.Contains(ammo.def));
                        }
                        if (ammoThing != null)
                        {
                            Log.Message("FinishReload setting currentAmmo to " + ammoThing.def.ToString());
                            currentAmmoInt = (AmmoDef)ammoThing.def;
                        }
                        else
                        {
                            if (Props.throwMote)
                            {
                                MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "Out of ammo");
                            }
                            compInventory.SwitchToNextViableWeapon(true);
                            return;
                        }
                    }
                    Log.Message("FinishReload :: ammoThing " + ammoThing.ToString());
                    if (Props.magazineSize < ammoThing.stackCount)
                    {
                        Log.Message("FinishReload :: setting ammo to full mag");
                        curAmmoCountInt = Props.magazineSize;
                        ammoThing.stackCount -= Props.magazineSize;
                    }
                    else
                    {
                        Log.Message("FinishReload :: setting ammo to stack count");
                        curAmmoCountInt = ammoThing.stackCount;
                        compInventory.container.Remove(ammoThing);
                    }
                }
                // Tell turret operator to fetch ammo
                else
                {
                    // -TODO-
                }
            }
            else
            {
                Log.Message("FinishReload :: don't use ammo, setting to full mag");
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
            ammoThing = compInventory.ammoList.Find(thing => ((AmmoDef)thing.def).Equals(currentAmmo));
            Log.Message("TryFindAmmoInInventory :: ammoThing after first pass " + (ammoThing == null ? "null" : ammoThing.ToString()));
            if (ammoThing != null)
            {
                return true;
            }
            
            // Try finding ammo from different type
            foreach (AmmoDef ammoDef in Props.ammoSet.ammoTypes)
            {
                Log.Message("TryFindAmmoInInventory :: trying to find ammo for type " + ammoDef.ToString());
                ammoThing = compInventory.ammoList.Find(thing => ((AmmoDef)thing.def).Equals(ammoDef));
                if (ammoThing != null)
                {
                    Log.Message("TryFindAmmoInInventory :: found ammoThing after second pass " + ammoThing.ToString());
                    currentAmmoInt = ammoDef;
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
                var reloadCommandGizmo = new Command_Action
                {
                    action = this.StartReload,
                    defaultLabel = "CR_ReloadLabel".Translate(),
                    defaultDesc = "CR_ReloadDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true)
                };
                yield return reloadCommandGizmo;
            }
        }

        public override string GetDescriptionPart()
        {
            return "Magazine size: " + GenText.ToStringByStyle(this.Props.magazineSize, ToStringStyle.Integer) +
                "\nReload time: " + GenText.ToStringByStyle((this.Props.reloadTicks / 60), ToStringStyle.Integer) + " s";
        }
    }
}
