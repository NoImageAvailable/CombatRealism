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
        public int curAmmo;
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
        private ThingDef currentAmmo = null;
        private CompInventory inventoryComp
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
                curAmmo = rProps.roundPerMag;
            }
            else
            {
                Log.Warning("Could not find a CompProperties_Reloader for CompReloader.");
                curAmmo = 9876;
            }

            // Initialize ammo with default if none is set
            if (useAmmo && currentAmmo == null)
            {
                if (rProps.ammoSet.ammoTypes.NullOrEmpty())
                {
                    Log.Error(this.parent.Label + " failed to initialize with default ammo");
                }
                else
                {
                    currentAmmo = rProps.ammoSet.ammoTypes[0];
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue(ref curAmmo, "count", 1);
            Scribe_Defs.LookDef(ref currentAmmo, "currentAmmo");
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

            // Add remaining ammo back to inventory
            if (curAmmo > 0 && useAmmo)
            {
                Thing ammoThing = ThingMaker.MakeThing(currentAmmo);
                ammoThing.stackCount = curAmmo;

                if (inventoryComp != null)
                {
                    Log.Message("Adding ammo " + ammoThing.Label + " x" + curAmmo.ToString() + " to " + wielder.ToString());
                    inventoryComp.container.TryAdd(ammoThing, curAmmo);
                }
                else
                {
                    Thing outThing;
                    GenThing.TryDropAndSetForbidden(ammoThing, wielder.Position, ThingPlaceMode.Near, out outThing, true);
                }
            }
            curAmmo = 0;

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
                if (inventoryComp != null)
                {
                    // -TODO- Add handling for getting ammo from inventory
                }
                else
                {
                    // -TODO- Tell turret operator to fetch ammo
                }
            }
            parent.def.soundInteract.PlayOneShot(SoundInfo.InWorld(wielder.Position));
            if (rProps.throwMote)
            {
                MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "CR_ReloadedMote".Translate());
            }
            curAmmo = rProps.roundPerMag;
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
