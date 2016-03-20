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
    }

    public class CompReloader : CommunityCoreLibrary.CompRangedGizmoGiver
    {
        public int count;
        public bool needReload;
        public CompProperties_Reloader reloaderProp;

        public CompEquippable compEquippable
        {
            get { return parent.GetComp< CompEquippable >(); }
        }

        public Pawn wielder
        {
            get { return compEquippable.PrimaryVerb.CasterPawn; }
        }

        private TargetInfo storedTarget = null;
        private JobDef storedJobDef = null;

        public override void Initialize( CompProperties vprops )
        {
            base.Initialize( vprops );

            reloaderProp = vprops as CompProperties_Reloader;
            if ( reloaderProp != null )
            {
                count = reloaderProp.roundPerMag;
            }
            else
            {
                Log.Warning( "Could not find a CompProperties_Reloader for CompReloader." );
                count = 9876;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue( ref count, "count", 1 );
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
            count = 0;
            needReload = true;
#if DEBUG
            if ( wielder == null )
            {
                Log.ErrorOnce( "Wielder of " + parent + " is null!", 7381889 );
                FinishReload();
                return;
            }
#endif
            if ( reloaderProp.throwMote )
            {
                MoteThrower.ThrowText( wielder.Position.ToVector3Shifted(), "CR_ReloadingMote".Translate() );
            }

            var reloadJob = new Job( DefDatabase< JobDef >.GetNamed( "ReloadWeapon" ), wielder, parent )
            {
                playerForced = true
            };

            //Store the current job so we can reassign it later
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
            parent.def.soundInteract.PlayOneShot(SoundInfo.InWorld(wielder.Position));
            if ( reloaderProp.throwMote )
            {
                MoteThrower.ThrowText(wielder.Position.ToVector3Shifted(), "CR_ReloadedMote".Translate());
            }
            count = reloaderProp.roundPerMag;
            needReload = false;
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

        public override IEnumerable< Command > CompGetGizmosExtra()
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
            return "Magazine size: " + GenText.ToStringByStyle(this.reloaderProp.roundPerMag, ToStringStyle.Integer) + 
                "\nReload time: " + GenText.ToStringByStyle((this.reloaderProp.reloadTick / 60), ToStringStyle.Integer) + " s";
        }
    }
}
