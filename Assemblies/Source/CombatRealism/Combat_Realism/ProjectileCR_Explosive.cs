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
			BodyPartDamageInfo value = new BodyPartDamageInfo(null, new BodyPartDepth?(BodyPartDepth.Outside));
			ExplosionInfo explosionInfo = default(ExplosionInfo);
			explosionInfo.center = base.Position;
			explosionInfo.radius = this.def.projectile.explosionRadius;
			explosionInfo.dinfo = new DamageInfo(this.def.projectile.damageDef, 999, this.launcher, new BodyPartDamageInfo?(value), null);
			explosionInfo.postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
			explosionInfo.explosionSpawnChance = this.def.projectile.explosionSpawnChance;
			explosionInfo.explosionSound = this.def.projectile.soundExplode;
			explosionInfo.projectile = this.def;
			explosionInfo.DoExplosion();
		}
	}
}
