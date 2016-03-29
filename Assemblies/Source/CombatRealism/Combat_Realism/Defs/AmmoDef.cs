using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class AmmoDef : ThingDef
    {
        public enum AmmoClass
        {
            // Regular pistol/rifle ammo
            FullMetalJacket,
            HollowPoint,
            ArmorPiercing,
            Sabot,
            ExplosiveAP,
            IncendiaryAP,

            // Shotgun shells
            Shot,
            Slug,
            Beanbag,
            ElectroSlug,

            // Grenades
            FragGrenade,
            ElectroGrenade,

            // Futuristic ammo
            Charged,
            Ionized,
            IncendiaryCell,
            ThermobaricCell,

            // Rockets
            AntiTankRocket,
            FragRocket,
            ThermobaricRocket,
            ElectroRocket
        }

        public ThingDef linkedProjectile;
        public AmmoClass ammoClass;
    }
}
