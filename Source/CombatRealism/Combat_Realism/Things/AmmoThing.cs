using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class AmmoThing : ThingWithComps
    {
        AmmoDef ammoDef => def as AmmoDef;

        public override string GetDescription()
        {
            if(ammoDef != null && ammoDef.ammoClass != null && ammoDef.linkedProjectile != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(base.GetDescription());

                // Append ammo class description
                if (!string.IsNullOrEmpty(ammoDef.ammoClass.description))
                    stringBuilder.AppendLine("\n" + 
                        (string.IsNullOrEmpty(ammoDef.ammoClass.LabelCap) ? "" : ammoDef.ammoClass.LabelCap + ":\n") + 
                        ammoDef.ammoClass.description);

                // Append ammo stats
                ProjectilePropertiesCR props = ammoDef.linkedProjectile.projectile as ProjectilePropertiesCR;
                if (props != null)
                {
                    // Damage type/amount
                    stringBuilder.AppendLine("\n" + "CR_DescDamage".Translate() + ": ");
                    stringBuilder.AppendLine("   " + props.damageDef.LabelCap + ": " + GenText.ToStringByStyle(props.damageAmountBase, ToStringStyle.Integer));
                    if (!props.secondaryDamage.NullOrEmpty())
                    {
                        foreach(SecondaryDamage sec in props.secondaryDamage)
                        {
                            stringBuilder.AppendLine("   " + sec.def.LabelCap + ": " + GenText.ToStringByStyle(sec.amount, ToStringStyle.Integer));
                        }
                    }
                    // Explosion radius
                    if (props.explosionRadius > 0)
                        stringBuilder.AppendLine("CR_DescExplosionRadius".Translate() + ": " + GenText.ToStringByStyle(props.explosionRadius, ToStringStyle.FloatTwo));

                    // Secondary explosion
                    // -TODO-

                    // CR stats
                    stringBuilder.AppendLine("CR_DescArmorPenetration".Translate() + ": " + GenText.ToStringByStyle(props.armorPenetration, ToStringStyle.PercentTwo));
                    if (props.pelletCount > 1)
                        stringBuilder.AppendLine("CR_DescPelletCount".Translate() + ": " + GenText.ToStringByStyle(props.pelletCount, ToStringStyle.Integer));
                    if (props.spreadMult != 1)
                        stringBuilder.AppendLine("CR_DescSpreadMult".Translate() + ": " + GenText.ToStringByStyle(props.spreadMult, ToStringStyle.PercentZero));
                }

                return stringBuilder.ToString();
            }
            return base.GetDescription();
        }
    }
}
