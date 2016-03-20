using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;

namespace Combat_Realism
{
    public class CompTurretInit : ThingComp
    {
        public Thing gun;

        public override void Initialize(Verse.CompProperties props)
        {
            base.Initialize(props);
            Building_TurretGun turret = parent as Building_TurretGun;
            if (turret != null && turret.gun == null)
            {
                gun = (Thing)ThingMaker.MakeThing(parent.def.building.turretGunDef);
                turret.gun = gun;
                turret.GunCompEq.verbTracker.InitVerbs();
            }
        }

        public override void PostSpawnSetup()
        {
            base.PostSpawnSetup();
            //It just needed first time.
            if (gun != null)
            {
                gun.Destroy();
                gun = null;
            }
        }
    }
}
