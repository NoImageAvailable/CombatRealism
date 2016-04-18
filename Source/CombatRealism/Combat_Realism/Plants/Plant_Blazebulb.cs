using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class Plant_Blazebulb : Plant
    {
        private const int ignitionTemp = 21;        // Temperature (in Celsius) above which the plant will start catching fire

        public override void TickLong()
        {
            base.TickLong();
            float temperature = base.Position.GetTemperature();
            if (temperature > ignitionTemp)
            {
                float ignitionChance = 0.005f * Mathf.Pow((temperature - ignitionTemp), 2);
                float rand = UnityEngine.Random.value;
                if(UnityEngine.Random.value < ignitionChance)
                {
                    FireUtility.TryStartFireIn(Position, 0.1f);
                }
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if(dinfo.Def != DamageDefOf.Rotting)
            {
                FireUtility.TryStartFireIn(Position, totalDamageDealt / MaxHitPoints);
            }
        }
    }
}
