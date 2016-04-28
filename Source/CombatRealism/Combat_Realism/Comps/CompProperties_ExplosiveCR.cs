using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompProperties_ExplosiveCR : CompProperties
    {
        public float explosionDamage = -1;
        public DamageDef explosionDamageDef;
        public float explosionRadius = 0f;
        public ThingDef preExplosionSpawnThingDef;
        public ThingDef postExplosionSpawnThingDef;
        public float explosionSpawnChance = 1f;
        public SoundDef soundExplode;
        public List<ThingCount> fragments = new List<ThingCount>();
        public float fragRange = 0f;
        public bool damageAdjacentTiles = false;

        public CompProperties_ExplosiveCR()
        {
            this.compClass = typeof(CompExplosiveCR);
        }
    }
}
