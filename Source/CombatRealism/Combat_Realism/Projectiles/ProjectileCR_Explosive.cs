using System;
using Verse;

namespace Combat_Realism
{
    //Cloned from vanilla, completely unmodified
    public class ProjectileCR_Explosive : ProjectileCR
    {
        private int ticksToDetonation;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
        }
        public override void Tick()
        {
            base.Tick();
            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    this.Explode();
                }
            }
        }
        protected override void Impact(Thing hitThing)
        {
            if (this.def.projectile.explosionDelay == 0)
            {
                this.Explode();
                return;
            }
            this.landed = true;
            this.ticksToDetonation = this.def.projectile.explosionDelay;
        }
        protected virtual void Explode()
        {
            this.Destroy(DestroyMode.Vanish);
            ProjectilePropertiesCR propsCR = def.projectile as ProjectilePropertiesCR;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            GenExplosion.DoExplosion(base.Position,
                this.def.projectile.explosionRadius,
                this.def.projectile.damageDef,
                this.launcher,
                this.def.projectile.soundExplode,
                this.def,
                this.equipmentDef,
                this.def.projectile.postExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance,
                propsCR == null ? false : propsCR.damageAdjacentTiles,
                preExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance);

            CompExplosiveCR comp = this.TryGetComp<CompExplosiveCR>();
            if (comp != null)
            {
                comp.Explode(launcher);
            }
        }
    }
}
