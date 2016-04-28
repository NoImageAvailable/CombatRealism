using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal static class Detours_WorkGiver_HunterHunt
    {
        internal static bool HasHuntingWeapon(Pawn p)
        {
            if (p.equipment.Primary != null)
            {
                CompAmmoUser comp = p.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (comp == null 
                    || !comp.useAmmo 
                    || (comp.hasMagazine && comp.curMagCount > 0) 
                    || comp.hasAmmo)
                    return true;
            }
            List<Hediff> hediffs = p.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].def.addedPartProps != null && hediffs[i].def.addedPartProps.isGoodWeapon)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
