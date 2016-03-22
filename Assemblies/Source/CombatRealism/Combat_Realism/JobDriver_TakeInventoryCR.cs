using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism
{
    class JobDriver_TakeInventoryCR : JobDriver_TakeInventory
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Pawn pawn = this.GetActor();
            CompInventory comp = pawn.TryGetComp<CompInventory>();

            // TODO

            return base.MakeNewToils();
        }
    }
}
