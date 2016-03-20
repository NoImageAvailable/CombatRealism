using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class CompProperties_Explosive : CompProperties
    {
        public float explosionDamage = -1;
        public DamageDef explosionDamageDef = null;

        public CompProperties_Explosive()
        {
            this.compClass = typeof(CompProperties_Explosive);
        }
    }

    class CompExplosive : ThingComp
    {
        new public CompProperties_Explosive props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompProperties_Explosive cprops = props as CompProperties_Explosive;
            if (cprops != null)
            {
                this.props = cprops;
            }
        }

        /// <summary>
        /// Produces a secondary explosion on impact using the explosion values from the projectile's projectile def. Requires the projectile's launcher to be passed on due to protection level, 
        /// only works when parent can be cast as ProjectileCR. Intended use is for HEAT and similar weapons that spawn secondary explosions while also penetrating, NOT explosive ammo of 
        /// anti-materiel rifles as the explosion just spawns on top of the pawn, not inside the hit body part.
        /// 
        /// Additionally handles fragmentation effects if defined.
        /// </summary>
        /// <param name="launcher">Launcher of the projectile calling the method</param>
		protected virtual void Explode(Thing launcher)
		{
            ProjectileCR parentProjectile = this.parent as ProjectileCR;

            if (parentProjectile != null)
            {
                // Regular explosion stuff
                if (this.props.explosionDamageDef != null && this.props.explosionDamage > 0)
                {
                    BodyPartDamageInfo value = new BodyPartDamageInfo(null, new BodyPartDepth?(BodyPartDepth.Outside));
                    ExplosionInfo explosionInfo = default(ExplosionInfo);
                    explosionInfo.center = parentProjectile.Position;
                    explosionInfo.radius = parentProjectile.def.projectile.explosionRadius;
                    explosionInfo.dinfo = new DamageInfo(this.props.explosionDamageDef, 999, launcher, new BodyPartDamageInfo?(value), null);
                    explosionInfo.postExplosionSpawnThingDef = parentProjectile.def.projectile.postExplosionSpawnThingDef;
                    explosionInfo.explosionSpawnChance = parentProjectile.def.projectile.explosionSpawnChance;
                    explosionInfo.explosionSound = parentProjectile.def.projectile.soundExplode;
                    explosionInfo.projectile = parentProjectile.def;
                    explosionInfo.DoExplosion();
                }
                
                // Fragmentation stuff
                // --TODO--
            }
		}
    }
}
