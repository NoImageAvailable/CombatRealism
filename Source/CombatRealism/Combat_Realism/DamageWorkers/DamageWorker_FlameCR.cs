using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Combat_Realism
{
    // Cloned from vanilla DamageWorker_Flame, only change is inheritance from DamageWorker_AddInjuryCR so we can have the new armor system apply to this as well
    public class DamageWorker_FlameCR : DamageWorker_AddInjuryCR
    {
        public override float Apply(DamageInfo dinfo, Thing victim)
        {
            if (!dinfo.InstantOldInjury)
            {
                victim.TryAttachFire(Rand.Range(0.15f, 0.25f));
            }
            Pawn pawn = victim as Pawn;
            if (pawn != null && pawn.Faction == Faction.OfColony)
            {
                Find.TickManager.slower.SignalForceNormalSpeedShort();
            }
            return base.Apply(dinfo, victim);
        }

        public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, bool canThrowMotes)
        {
            base.ExplosionAffectCell(explosion, c, damagedThings, canThrowMotes);
            if (this.def == DamageDefOf.Flame)
            {
                FireUtility.TryStartFireIn(c, Rand.Range(0.2f, 0.6f));
            }
        }
    }
}
