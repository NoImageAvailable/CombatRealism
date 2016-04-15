using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompProperties_AP : CompProperties
    {
        public float armorPenetration = 0f;

        public CompProperties_AP()
        {
            this.compClass = typeof(CompAP);
        }
    }
}
