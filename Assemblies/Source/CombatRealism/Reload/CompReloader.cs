using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Combat_Realism
{
    public class CompProperties_Reloader : CompProperties
    {
        public int roundPerMag = 1;
        public int reloadTick = 300;
        public bool throwMote = true;
        public AmmoSetDef ammoSet = null;
    }

    public class CompReloader : CommunityCoreLibrary.CompRangedGizmoGiver
    {
        public int curMagCount;
        public CompProperties_Reloader rProps;
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
                return rProps.ammoSet != null;
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

            rProps = vprops as CompProperties_Reloader;
            if (rProps != null)
            {
                curMagCount = rProps.roundPerMag;
            }
            else
            {
                Log.Warning("Could not find a CompProperties_Reloader for CompReloader.");
                curMagCount = 9876;
            }

            // Initialize ammo with default if none is set
            Log.Message(this.parent.ToString() + " uses ammo " + useAmmo.ToString());
            Log.Message(this.parent.ToString() + " has current ammo " + (currentAmmoInt != null).ToString());
            Log.Message((useAmmo && currentAmmoInt == null).ToString());
            if (useAmmo && currentAmmoInt == null)
            {
                if (rProps.ammoSet.ammoTypes.NullOrEmpty())
                {
                    Log.Error(this.parent.Label + " failed to initialize with default ammo");
                }
                else
                {
                    currentAmmoInt = (AmmoDef)rProps.ammoSet.ammoTypes[0];
                    Log.Message("Initialize :: set currentAmmoInt to " + currentAmmoInt.ToString());
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue(ref curMagCount, "count", 1);
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
                    if (rProps.throwMote)
                    {
                        MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "Out of ammo");
                    }
                    return;
                }

                // Add remaining ammo back to inventory
                if (curMagCount > 0)
                {
                    Thing ammoThing = ThingMaker.MakeThing(currentAmmoInt);
                    //GenSpawn.Spawn(ammoThing, this.parent.Position);
                    ammoThing.stackCount = curMagCount;
                    curMagCount = 0;

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
            if (rProps.throwMote)
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
                            ammoThing = compInventory.ammoList.Find(ammo => rProps.ammoSet.ammoTypes.Contains(ammo.def));
                        }
                        if (ammoThing != null)
                        {
                            Log.Message("FinishReload setting currentAmmo to " + ammoThing.def.ToString());
                            currentAmmoInt = (AmmoDef)ammoThing.def;
                        }
                        else
                        {
                            if (rProps.throwMote)
                            {
                                MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "Out of ammo");
                            }
                            compInventory.SwitchToNextViableWeapon(true);
                            return;
                        }
                    }
                    Log.Message("FinishReload :: ammoThing " + ammoThing.ToString());
                    if (rProps.roundPerMag < ammoThing.stackCount)
                    {
                        Log.Message("FinishReload :: setting ammo to full mag");
                        curMagCount = rProps.roundPerMag;
                        ammoThing.stackCount -= rProps.roundPerMag;
                    }
                    else
                    {
                        Log.Message("FinishReload :: setting ammo to stack count");
                        curMagCount = ammoThing.stackCount;
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
                curMagCount = rProps.roundPerMag;
            }
            parent.def.soundInteract.PlayOneShot(SoundInfo.InWorld(wielder.Position));
            if (rProps.throwMote)
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
            foreach (AmmoDef ammoDef in rProps.ammoSet.ammoTypes)
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
            var ammoStat = new GizmoAmmoStatus
            {
                compAmmo = this
            };

            yield return ammoStat;

            if (this.wielder != null)
            {
                var com = new Command_Action
                {
                    action = StartReload,
                    defaultLabel = "CR_ReloadLabel".Translate(),
                    defaultDesc = "CR_ReloadDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true)
                };

                yield return com;
            }
        }

        public override string GetDescriptionPart()
        {
            return "Magazine size: " + GenText.ToStringByStyle(this.rProps.roundPerMag, ToStringStyle.Integer) +
                "\nReload time: " + GenText.ToStringByStyle((this.rProps.reloadTick / 60), ToStringStyle.Integer) + " s";
        }
    }
}
