using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;

namespace Combat_Realism
{
    public class CompTurretCR : ThingComp
    {
        public CompProperties_TurretCR Props
        {
            get
            {
                return (CompProperties_TurretCR)this.props;
            }
        }

        public Thing gun
        {
            get
            {
                Building_TurretGun turret = parent as Building_TurretGun;
                if (turret != null) return turret.gun;
                return null;
            }
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            Log.Message("CompTurretCR :: CompGetGizmosExtra :: calling");
            if (gun != null)
            {
                Log.Message("CompTurretCR :: CompGetGizmosExtra :: checking for compAmmo");
                CompAmmoUser compAmmo = gun.TryGetComp<CompAmmoUser>();
                if (compAmmo != null)
                {
                    Log.Message("CompTurretCR :: CompGetGizmosExtra :: returning compAmmo gizmos");
                    foreach (Command com in compAmmo.CompGetGizmosExtra())
                    {
                        yield return com;
                    }
                }
                Log.Message("CompTurretCR :: CompGetGizmosExtra :: checking for compModes");
                CompFireModes compModes = gun.TryGetComp<CompFireModes>();
                if (compModes != null)
                {
                    Log.Message("CompTurretCR :: CompGetGizmosExtra :: returning compModes gizmos");
                    foreach (Command com in compModes.GenerateGizmos())
                    {
                        yield return com;
                    }
                }
            }
        }
    }
}
