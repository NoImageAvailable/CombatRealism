using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism
{
    public class JobDriver_Reload : JobDriver
    {
        private CompAmmoUser _compReloader;
        private CompAmmoUser compReloader
        {
            get
            {
                if (_compReloader == null) _compReloader = TargetThingB.TryGetComp<CompAmmoUser>();
                return _compReloader;
            }
        }

        private bool HasNoGunOrAmmo()
        {
            if (TargetThingB.DestroyedOrNull() || pawn.equipment == null || pawn.equipment.Primary == null || pawn.equipment.Primary != TargetThingB)
                return true;

            CompAmmoUser comp = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
            return comp != null && comp.useAmmo && !comp.hasAmmo;
        }

        protected override IEnumerable< Toil > MakeNewToils()
        {
            if (compReloader == null)
            {
                Log.Error(pawn.ToString() + " tried to do reload job without compReloader");
                yield return null;
            }

            this.FailOnDespawnedOrNull( TargetIndex.A );
            this.FailOnMentalState(TargetIndex.A);
            this.FailOn(HasNoGunOrAmmo);
            
            //Toil of do-nothing		
            var waitToil = new Toil();
            waitToil.initAction = () => waitToil.actor.pather.StopDead();
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = Mathf.CeilToInt(compReloader.Props.reloadTicks / pawn.GetStatValue(StatDef.Named("ReloadSpeed")));
            yield return waitToil.WithProgressBarToilDelay(TargetIndex.A);

            //Actual reloader
            var reloadToil = new Toil();
            reloadToil.AddFinishAction( () => compReloader.LoadAmmo() );
            yield return reloadToil;

            //Continue previous job if possible
            var continueToil = new Toil();
            continueToil.initAction = () => compReloader.TryContinuePreviousJob();
            continueToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return continueToil;
        }
    }
}
