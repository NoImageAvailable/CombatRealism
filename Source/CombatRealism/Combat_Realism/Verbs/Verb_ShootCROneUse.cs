using System;
using Verse;
using RimWorld;

namespace Combat_Realism
{
	public class Verb_ShootCROneUse : Verb_ShootCR
	{
		protected override bool TryCastShot()
		{
			if (base.TryCastShot())
			{
				if (this.burstShotsLeft <= 1)
				{
					this.SelfConsume();
				}
				return true;
			}
            if (compAmmo != null && compAmmo.hasMagazine && compAmmo.curMagCount <= 0)
            {
                this.SelfConsume();
            }
			else if (this.burstShotsLeft < this.verbProps.burstShotCount)
			{
				this.SelfConsume();
			}
			return false;
		}
		public override void Notify_Dropped()
		{
			if (this.state == VerbState.Bursting && this.burstShotsLeft < this.verbProps.burstShotCount)
			{
				this.SelfConsume();
			}
		}
		private void SelfConsume()
		{
			if (this.ownerEquipment != null && !this.ownerEquipment.Destroyed)
            {
                this.ownerEquipment.Destroy(DestroyMode.Vanish);
			}
		}
	}
}
