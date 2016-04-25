using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompProperties_Jamming : CompProperties
    {
        public float baseMalfunctionChance = 0f;
        public bool canExplode = false;
        public float explosionDamage = 0f;
        public float explosionRadius = 1f;
        public SoundDef explosionSound = null;

        public CompProperties_Jamming()
        {
            this.compClass = typeof(CompJamming);
        }
    }
}
