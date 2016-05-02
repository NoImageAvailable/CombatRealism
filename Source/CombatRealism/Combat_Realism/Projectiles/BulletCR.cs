using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Combat_Realism
{
    public class BulletCR : ProjectileCR
    {
        private const float StunChance = 0.1f;
        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            if (hitThing != null)
            {
                int damageAmountBase = this.def.projectile.damageAmountBase;

                BodyPartDamageInfo value;
                DamageDef_CR damDefCR = def.projectile.damageDef as DamageDef_CR;
                if (damDefCR != null && damDefCR.harmOnlyOutsideLayers) value = new BodyPartDamageInfo(null, BodyPartDepth.Outside);
                else value = new BodyPartDamageInfo(null, null);

                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, damageAmountBase, this.launcher, this.ExactRotation.eulerAngles.y, new BodyPartDamageInfo?(value), this.def);

                ProjectilePropertiesCR propsCR = def.projectile as ProjectilePropertiesCR;
                if (propsCR != null && !propsCR.secondaryDamage.NullOrEmpty())
                {
                    // Get the correct body part
                    Pawn pawn = hitThing as Pawn;
                    if (pawn != null && def.projectile.damageDef.workerClass == typeof(DamageWorker_AddInjuryCR))
                    {
                        dinfo = new DamageInfo(dinfo.Def, 
                            dinfo.Amount, 
                            dinfo.Instigator, 
                            dinfo.Angle, 
                            new BodyPartDamageInfo(DamageWorker_AddInjuryCR.GetExactPartFromDamageInfo(dinfo, pawn), false, (HediffDef)null), 
                            dinfo.Source);
                    }
                    List<DamageInfo> dinfoList = new List<DamageInfo>() { dinfo };
                    foreach(SecondaryDamage secDamage in propsCR.secondaryDamage)
                    {
                        dinfoList.Add(new DamageInfo(secDamage.def, secDamage.amount, dinfo.Instigator, dinfo.Part, dinfo.Source));
                    }
                    foreach(DamageInfo curDinfo in dinfoList)
                    {
                        hitThing.TakeDamage(curDinfo);
                    }
                }
                else
                {
                    hitThing.TakeDamage(dinfo);
                }
            }
            else
            {
                SoundDefOf.BulletImpactGround.PlayOneShot(base.Position);
                MoteThrower.ThrowStatic(this.ExactPosition, ThingDefOf.Mote_ShotHit_Dirt, 1f);
            }
        }
    }
}
