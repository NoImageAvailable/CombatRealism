using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism
{
    public class JobDriver_Reload : JobDriver
    {
        private CompAmmoUser compReloader
        {
            get
            {
                return TargetThingB.TryGetComp<CompAmmoUser>();
            }
        }

        protected override IEnumerable< Toil > MakeNewToils()
        {
            this.FailOnDespawnedOrNull( TargetIndex.A );
            this.FailOnMentalState(TargetIndex.A);
            
            //Toil of do-nothing		
            var waitToil = new Toil();
            waitToil.initAction = () => waitToil.actor.pather.StopDead();
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = Mathf.CeilToInt(compReloader.Props.reloadTicks / pawn.GetStatValue(StatDef.Named("ReloadSpeed")));
            yield return waitToil;

            //Actual reloader
            var reloadToil = new Toil();
            reloadToil.AddFinishAction( compReloader.FinishReload );
            yield return reloadToil;

            //Continue previous job if possible
            var continueToil = new Toil();
            continueToil.initAction = () => compReloader.TryContinuePreviousJob();
            continueToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return continueToil;
        }
    }
}
