using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompProperties_TurretInit : CompProperties
    {
        public CompProperties_TurretInit()
        {
            this.compClass = typeof(CompTurretInit);
        }
    }
}
