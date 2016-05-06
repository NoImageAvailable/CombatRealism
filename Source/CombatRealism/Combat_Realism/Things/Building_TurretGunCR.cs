using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace Combat_Realism
{
    // Class is cloned from Building_TurretGun with various changes made to support fire modes and ammo
    public class Building_TurretGunCR : Building_Turret
    {
        private const int minTicksBeforeAutoReload = 1800;              // This much time must pass before haulers will try to automatically reload an auto-turret

        #region Fields

        protected int burstCooldownTicksLeft;
        public int burstWarmupTicksLeft;                                // Need this public so aim mode can modify it
        protected TargetInfo currentTargetInt = TargetInfo.Invalid;
        public Thing gun;
        public bool loaded = true;
        protected CompMannable mannableComp;
        protected CompPowerTrader powerComp;
        protected TurretTopCR top;

        // New fields
        private CompAmmoUser _compAmmo = null;
        private CompFireModes _compFireModes = null;
        public bool isReloading = false;
        private int ticksSinceLastBurst = minTicksBeforeAutoReload;
        
        #endregion

        #region Properties

        public override Verb AttackVerb
        {
            get
            {
                if (this.gun == null)
                {
                    return null;
                }
                return this.GunCompEq.verbTracker.PrimaryVerb;
            }
        }
        public override TargetInfo CurrentTarget
        {
            get
            {
                return this.currentTargetInt;
            }
        }
        public CompEquippable GunCompEq
        {
            get
            {
                return this.gun.TryGetComp<CompEquippable>();
            }
        }
        private bool WarmingUp
        {
            get
            {
                return this.burstWarmupTicksLeft > 0;
            }
        }

        // New properties
        public CompAmmoUser compAmmo
        {
            get
            {
                if (_compAmmo == null && gun != null) _compAmmo = gun.TryGetComp<CompAmmoUser>();
                return _compAmmo;
            }
        }
        public CompFireModes compFireModes
        {
            get
            {
                if (_compFireModes == null && gun != null) _compFireModes = gun.TryGetComp<CompFireModes>();
                return _compFireModes;
            }
        }
        public bool needsReload
        {
            get
            {
                return mannableComp == null
                    && compAmmo != null
                    && (compAmmo.curMagCount < compAmmo.Props.magazineSize || compAmmo.selectedAmmo != compAmmo.currentAmmo);
            }
        }
        public bool allowAutomaticReload
        {
            get
            {
                return mannableComp == null && compAmmo != null 
                    && (ticksSinceLastBurst >= minTicksBeforeAutoReload || compAmmo.curMagCount <= Mathf.CeilToInt(compAmmo.Props.magazineSize / 6));
            }
        }

        #endregion

        #region Methods

        protected void BeginBurst()
        {
            ticksSinceLastBurst = 0;
            this.GunCompEq.PrimaryVerb.TryStartCastOn(this.CurrentTarget, false);
        }

        protected void BurstComplete()
        {
            if (this.def.building.turretBurstCooldownTicks >= 0)
            {
                this.burstCooldownTicksLeft = this.def.building.turretBurstCooldownTicks;
            }
            else
            {
                this.burstCooldownTicksLeft = this.GunCompEq.PrimaryVerb.verbProps.defaultCooldownTicks;
            }
            if (compAmmo != null && compAmmo.curMagCount <= 0)
            {
                OrderReload();
            }
        }

        public override void Draw()
        {
            this.top.DrawTurret();
            base.Draw();
        }

        public override void DrawExtraSelectionOverlays()
        {
            float range = this.GunCompEq.PrimaryVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(base.Position, range);
            }
            float minRange = this.GunCompEq.PrimaryVerb.verbProps.minRange;
            if (minRange < 90f && minRange > 0.1f)
            {
                GenDraw.DrawRadiusRing(base.Position, minRange);
            }
            if (this.burstWarmupTicksLeft > 0)
            {
                int degreesWide = (int)((float)this.burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPie(this, this.CurrentTarget, degreesWide, (float)this.def.size.x * 0.5f);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.LookValue<bool>(ref this.loaded, "loaded", false, false);

            // Look new variables
            Scribe_Values.LookValue(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_Values.LookValue(ref isReloading, "isReloading", false);
            Scribe_Deep.LookDeep(ref gun, "gun");
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            stringBuilder.AppendLine("GunInstalled".Translate() + ": " + this.gun.LabelCap);
            if (this.GunCompEq.PrimaryVerb.verbProps.minRange > 0f)
            {
                stringBuilder.AppendLine("MinimumRange".Translate() + ": " + this.GunCompEq.PrimaryVerb.verbProps.minRange.ToString("F0"));
            }

            if (isReloading)
            {
                stringBuilder.AppendLine("CR_TurretReloading".Translate());
            }
            else if (this.burstCooldownTicksLeft > 0)
            {
                stringBuilder.AppendLine("CanFireIn".Translate() + ": " + this.burstCooldownTicksLeft.TickstoSecondsString());
            }

            if (compAmmo != null && compAmmo.Props.ammoSet != null)
            {
                stringBuilder.AppendLine("CR_AmmoSet".Translate() + ": " + compAmmo.Props.ammoSet.LabelCap);
            }
            /*
            if (this.def.building.turretShellDef != null)
            {
                if (this.loaded)
                {
                    stringBuilder.AppendLine("ShellLoaded".Translate());
                }
                else
                {
                    stringBuilder.AppendLine("ShellNotLoaded".Translate());
                }
            }
            */
            return stringBuilder.ToString();
        }

        private bool IsValidTarget(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
                {
                    RoofDef roofDef = Find.RoofGrid.RoofAt(t.Position);
                    if (roofDef != null && roofDef.isThickRoof)
                    {
                        return false;
                    }
                }
                if (this.mannableComp == null)
                {
                    return !GenAI.MachinesLike(base.Faction, pawn);
                }
            }
            return true;
        }

        public override void OrderAttack(TargetInfo targ)
        {
            if ((targ.Cell - base.Position).LengthHorizontal < this.GunCompEq.PrimaryVerb.verbProps.minRange)
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            if ((targ.Cell - base.Position).LengthHorizontal > this.GunCompEq.PrimaryVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            this.forcedTarget = targ;
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            this.powerComp = base.GetComp<CompPowerTrader>();
            this.mannableComp = base.GetComp<CompMannable>();
            if (gun == null)
            {
                this.gun = ThingMaker.MakeThing(this.def.building.turretGunDef, null);
            }
            for (int i = 0; i < this.GunCompEq.AllVerbs.Count; i++)
            {
                Verb verb = this.GunCompEq.AllVerbs[i];
                verb.caster = this;
                verb.castCompleteCallback = new Action(this.BurstComplete);
            }
            this.top = new TurretTopCR(this);

            // Callback for ammo comp
            if (compAmmo != null)
            {
                compAmmo.turret = this;
                if (def.building.turretShellDef != null && def.building.turretShellDef is AmmoDef) compAmmo.selectedAmmo = (AmmoDef)def.building.turretShellDef;
            }
        }

        public override void Tick()
        {
            base.Tick();
            ticksSinceLastBurst++;
            if (this.powerComp != null && !this.powerComp.PowerOn)
            {
                return;
            }
            if (this.mannableComp != null && !this.mannableComp.MannedNow)
            {
                return;
            }
            this.GunCompEq.verbTracker.VerbsTick();
            if (this.stunner.Stunned)
            {
                return;
            }
            if (this.GunCompEq.PrimaryVerb.state == VerbState.Bursting)
            {
                return;
            }
            if (this.WarmingUp)
            {
                this.burstWarmupTicksLeft--;
                if (this.burstWarmupTicksLeft == 0)
                {
                    this.BeginBurst();
                }
            }
            else
            {
                if (this.burstCooldownTicksLeft > 0)
                {
                    this.burstCooldownTicksLeft--;
                }
                if (this.burstCooldownTicksLeft == 0)
                {
                    this.TryStartShootSomething();
                }
            }
            this.top.TurretTopTick();
        }

        protected TargetInfo TryFindNewTarget()
        {
            Thing searcher;
            Faction faction;
            if (this.mannableComp != null && this.mannableComp.MannedNow)
            {
                searcher = this.mannableComp.ManningPawn;
                faction = this.mannableComp.ManningPawn.Faction;
            }
            else
            {
                searcher = this;
                faction = base.Faction;
            }
            if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && faction.HostileTo(Faction.OfColony) && Rand.Value < 0.5f && Find.ListerBuildings.allBuildingsColonist.Count > 0)
            {
                return Find.ListerBuildings.allBuildingsColonist.RandomElement<Building>();
            }
            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
            if (!this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
            }
            if (this.GunCompEq.PrimaryVerb.verbProps.ai_IsIncendiary)
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }
            return AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, new Predicate<Thing>(this.IsValidTarget), this.GunCompEq.PrimaryVerb.verbProps.range, this.GunCompEq.PrimaryVerb.verbProps.minRange, targetScanFlags);
        }

        protected void TryStartShootSomething()
        {
            // Check for ammo first
            if (compAmmo != null && (isReloading || (mannableComp == null && compAmmo.curMagCount <= 0))) return;

            if (this.forcedTarget.ThingDestroyed)
            {
                this.forcedTarget = null;
            }
            if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && Find.RoofGrid.Roofed(base.Position))
            {
                return;
            }
            bool isValid = this.currentTargetInt.IsValid;
            if (this.forcedTarget.IsValid)
            {
                this.currentTargetInt = this.forcedTarget;
            }
            else
            {
                this.currentTargetInt = this.TryFindNewTarget();
            }
            if (!isValid && this.currentTargetInt.IsValid)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(base.Position);
            }
            if (this.currentTargetInt.IsValid)
            {
                if (AttackVerb.verbProps.warmupTicks > 0)
                {
                    this.burstWarmupTicksLeft = AttackVerb.verbProps.warmupTicks;
                }
                else
                {
                    this.BeginBurst();
                }
            }
        }

        // New methods

        public void OrderReload()
        {
            if (mannableComp == null
                || !mannableComp.MannedNow
                || (compAmmo.currentAmmo == compAmmo.selectedAmmo && compAmmo.curMagCount == compAmmo.Props.magazineSize)) return;
            Job reloadJob = null;
            CompInventory inventory = mannableComp.ManningPawn.TryGetComp<CompInventory>();
            if (inventory != null)
            {
                Thing ammo = inventory.container.FirstOrDefault(x => x.def == compAmmo.selectedAmmo);
                if (ammo != null)
                {
                    Thing droppedAmmo;
                    int amount = compAmmo.Props.magazineSize;
                    if (compAmmo.currentAmmo == compAmmo.selectedAmmo) amount -= compAmmo.curMagCount;
                    if (inventory.container.TryDrop(ammo, this.Position, ThingPlaceMode.Direct, Mathf.Min(ammo.stackCount, amount), out droppedAmmo))
                    {
                        reloadJob = new Job(DefDatabase<JobDef>.GetNamed("ReloadTurret"), this, droppedAmmo) { maxNumToCarry = droppedAmmo.stackCount };
                    }
                }
            }
            if (reloadJob == null)
            {
                reloadJob = new WorkGiver_ReloadTurret().JobOnThing(mannableComp.ManningPawn, this);
            }
            if (reloadJob != null)
            {
                mannableComp.ManningPawn.jobs.StartJob(reloadJob, JobCondition.Ongoing, null, true);
            }
        }

        public CompMannable GetMannableComp()
        {
            return mannableComp;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Ammo gizmos
            if (compAmmo != null)
            {
                foreach (Command com in compAmmo.CompGetGizmosExtra())
                {
                    yield return com;
                }
            }
            // Fire mode gizmos
            if (compFireModes != null)
            {
                foreach (Command com in compFireModes.GenerateGizmos())
                {
                    yield return com;
                }
            }
            if (Faction == Faction.OfColony)
            {
                // Stop forced attack gizmo
                Gizmo stop = new Command_Action()
                {
                    defaultLabel = "CommandStopForceAttack".Translate(),
                    defaultDesc = "CommandStopForceAttackDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true),
                    action = new Action(delegate
                    {
                        forcedTarget = TargetInfo.Invalid;
                        SoundDefOf.TickLow.PlayOneShotOnCamera();
                    }),
                    hotKey = KeyBindingDefOf.Misc5
                };
                yield return stop;
                // Set forced target gizmo
                if ((mannableComp != null && mannableComp.MannedNow && mannableComp.ManningPawn.Faction == Faction.OfColony)
                    || (mannableComp == null && Faction == Faction.OfColony))
                {
                    Gizmo attack = new Command_VerbTarget()
                    {
                        defaultLabel = "CommandSetForceAttackTarget".Translate(),
                        defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                        verb = GunCompEq.PrimaryVerb,
                        hotKey = KeyBindingDefOf.Misc4
                    };
                    yield return attack;
                }
            }

            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }

        #endregion

    }
}
