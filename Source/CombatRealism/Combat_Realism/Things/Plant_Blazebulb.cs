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
        private const int ignitionTemp = 21;                    // Temperature (in Celsius) above which the plant will start catching fire
        private const string fuelDefName = "Puddle_Fuel";       // Name of the def to use for fuel puddles spawned when damaged

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
                // Find existing fuel puddle or spawn one if needed
                Thing fuel = Position.GetThingList().FirstOrDefault(x => x.def == ThingDef.Named(fuelDefName));
                int fuelHPFromDamage = Mathf.CeilToInt(fuel.MaxHitPoints * Mathf.Clamp01(totalDamageDealt / MaxHitPoints));
                if (fuel != null)
                {
                    fuel.HitPoints = Mathf.Min(fuel.MaxHitPoints, fuel.HitPoints + fuelHPFromDamage);
                }
                else
                {
                    fuel = ThingMaker.MakeThing(ThingDef.Named(fuelDefName));
                    GenSpawn.Spawn(fuel, Position);
                    fuel.HitPoints = fuelHPFromDamage;
                }
            }
        }
    }
}
