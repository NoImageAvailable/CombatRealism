using RimWorld;
using Verse;
using Verse.AI;

namespace Combat_Realism
{
    public static class ExternalPawnDrafter
    {
        public static bool CanTakeOrderedJob( Pawn pawn )
        {
            return !pawn.HasAttachment( ThingDefOf.Fire ) &&
                   (pawn.CurJob == null || pawn.CurJob.def.playerInterruptible);
        }

        public static void TakeOrderedJob( Pawn pawn, Job newJob )
        {
            if ( pawn.jobs.debugLog )
            {
                pawn.jobs.DebugLogEvent( "TakeOrderedJob " + newJob );
            }
            if ( !CanTakeOrderedJob( pawn ) )
            {
                if ( pawn.jobs.debugLog )
                {
                    pawn.jobs.DebugLogEvent( "    CanTakePlayerJob is false. Returning." );
                }
                return;
            }
            GenJob.ValidateJob( newJob );
            if ( pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs( newJob ) )
            {
                return;
            }
            pawn.stances.CancelBusyStanceSoft();
            Find.PawnDestinationManager.UnreserveAllFor( pawn );
            if ( newJob.def == JobDefOf.Goto )
            {
                Find.PawnDestinationManager.ReserveDestinationFor( pawn, newJob.targetA.Cell );
            }
            if ( pawn.jobs.debugLog )
            {
                pawn.jobs.DebugLogEvent( "    Queueing job" );
            }
            if ( pawn.jobs.jobQueue == null )
            {
                pawn.jobs.jobQueue = new JobQueue();
            }
            pawn.jobs.jobQueue.Clear();
            pawn.jobs.jobQueue.EnqueueFirst( newJob );
            if ( pawn.jobs.curJob != null )
            {
                pawn.jobs.curDriver.EndJobWith( JobCondition.InterruptForced );
            }
        }
    }
}
