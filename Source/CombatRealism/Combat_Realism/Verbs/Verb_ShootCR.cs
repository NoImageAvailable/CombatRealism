using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Combat_Realism
{
	public class Verb_ShootCR : Verb_LaunchProjectileCR
    {
        protected override int ShotsPerBurst
        {
            get
            {
                if (this.compFireModes != null)
                {
                    if (this.compFireModes.currentFireMode == FireMode.SingleFire)
                    {
                        return 1;
                    }
                    if ((this.compFireModes.currentFireMode == FireMode.BurstFire || (useDefaultModes && this.compFireModes.Props.aiUseBurstMode)) 
                        && this.compFireModes.Props.aimedBurstShotCount > 0)
                    {
                        return this.compFireModes.Props.aimedBurstShotCount;
                    }
                }
                return this.verbPropsCR.burstShotCount;
            }
        }

        private CompFireModes compFireModesInt = null;
        private CompFireModes compFireModes
        {
            get
            {
                if (this.compFireModesInt == null && this.ownerEquipment != null)
                {
                    this.compFireModesInt = this.ownerEquipment.TryGetComp<CompFireModes>();
                }
                return this.compFireModesInt;
            }
        }
        private CompAmmoUser compAmmoInt = null;
        private CompAmmoUser compAmmo
        {
            get
            {
                if (compAmmoInt == null)
                {
                    compAmmoInt = this.ownerEquipment.TryGetComp<CompAmmoUser>();
                }
                return compAmmoInt;
            }
        }

        private bool shouldAim
        {
            get
            {
                if(this.CasterIsPawn)
                {
                    // Check for hunting job
                    if (CasterPawn.jobs != null && CasterPawn.jobs.curJob != null && CasterPawn.jobs.curJob.def == JobDefOf.Hunt)
                        return true;

                    // Check for suppression
                    CompSuppressable comp = this.caster.TryGetComp<CompSuppressable>();
                    if (comp != null)
                    {
                        if (comp.isSuppressed)
                        {
                            return false;
                        }
                    }
                    return this.compFireModes != null && (this.compFireModes.currentAimMode == AimMode.AimedShot || (useDefaultModes && this.compFireModes.Props.aiUseAimMode));
                }
                return false;
            }
        }
        private bool isAiming = false;
        private int xpTicks = 0;                        // Tracker to see how much xp should be awarded for time spent aiming + bursting

        // How much time to spend on aiming
        private const int aimTicksMin = 30;             
        private const int aimTicksMax = 240;

        // XP amounts
        private const float objectXP = 0.1f;
        private const float pawnXP = 0.75f;
        private const float hostileXP = 3.6f;

        protected override float swayAmplitude
        {
            get
            {
                float sway = base.swayAmplitude;
                if (this.shouldAim)
                {
                    sway *= Mathf.Max(0, 1 - aimingAccuracy);
                }
                return sway;
            }
        }

        // Whether this gun should use default AI firing modes
        private bool useDefaultModes
        {
            get
            {
                return !(CasterIsPawn && CasterPawn.Faction == Faction.OfColony);
            }
        }

        /// <summary>
        /// Handles activating aim mode at the start of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            if (xpTicks <= 0)
                xpTicks = Mathf.CeilToInt(verbProps.warmupTicks * 0.5f);

            if (this.shouldAim && !this.isAiming)
            {
                float targetDist = (this.currentTarget.Cell - this.caster.Position).LengthHorizontal;
                int aimTicks = (int)Mathf.Lerp(aimTicksMin, aimTicksMax, (targetDist / 100));
                this.CasterPawn.stances.SetStance(new Stance_Warmup(aimTicks, this.currentTarget, this));
                this.isAiming = true;
                return;
            }

            // Shooty stuff
            base.WarmupComplete();
            this.isAiming = false;
        }

        public override void VerbTickCR()
        {
            if (this.isAiming)
            {
                this.xpTicks++;
                if (!this.shouldAim)
                {
                    this.WarmupComplete();
                }
                if (this.CasterPawn.stances.curStance.GetType() != typeof(Stance_Warmup))
                {
                    this.isAiming = false;
                }
            }
            // Increase shootTicks while bursting so we can calculate XP afterwards
            else if (this.state == VerbState.Bursting)
            {
                this.xpTicks++;
            }
            else if (this.xpTicks > 0)
            {
                // Reward XP to shooter pawn
                if (this.ShooterPawn != null && this.ShooterPawn.skills != null)
                {
                    float xpPerTick = objectXP;
                    Pawn targetPawn = this.currentTarget.Thing as Pawn;
                    if (targetPawn != null)
                    {
                        if (targetPawn.HostileTo(this.caster.Faction))
                        {
                            xpPerTick = hostileXP;
                        }
                        else
                        {
                            xpPerTick = pawnXP;
                        }
                    }
                    this.ShooterPawn.skills.Learn(SkillDefOf.Shooting, xpPerTick * xpTicks);
                }
                this.xpTicks = 0;
            }
        }

        /// <summary>
        /// Reset selected fire mode back to default when gun is dropped
        /// </summary>
        public override void Notify_Dropped()
        {
            base.Notify_Dropped();
            if (this.compFireModes != null)
            {
                this.compFireModes.ResetModes();
            }
            caster = null;
        }

        /// <summary>
        /// Checks to see if fire mode is set to hold fire before doing the base check
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, TargetInfo targ)
        {
            if (this.compFireModes != null && this.compFireModes.currentAimMode == AimMode.HoldFire)
                return false;
            return base.CanHitTargetFrom(root, targ);
        }

        protected override bool TryCastShot()
        {
            if (compAmmo != null)
            {
                if (!compAmmo.TryReduceAmmoCount())
                {
                    if (compAmmo.hasMagazine)
                        compAmmo.StartReload();
                    return false;
                }
            }
            return base.TryCastShot();
        }
    }
}
