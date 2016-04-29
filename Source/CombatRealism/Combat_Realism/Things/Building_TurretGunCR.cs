using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class Building_TurretGunCR : Building_TurretGun
    {
        CompAmmoUser _compAmmo = null;
        CompFireModes _compFireModes = null;

        CompAmmoUser compAmmo
        {
            get
            {
                if (_compAmmo == null && gun != null) _compAmmo = gun.TryGetComp<CompAmmoUser>();
                return _compAmmo;
            }
        }
        CompFireModes compFireModes
        {
            get
            {
                if (_compFireModes == null && gun != null) _compFireModes = gun.TryGetComp<CompFireModes>();
                return _compFireModes;
            }
        }

        public void SetWarmupTicksTo(int ticks)
        {
            burstWarmupTicksLeft = ticks;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (compAmmo != null)
            {
                foreach (Command com in compAmmo.CompGetGizmosExtra())
                {
                    yield return com;
                }
            }
            if (compFireModes != null)
            {
                foreach (Command com in compFireModes.GenerateGizmos())
                {
                    yield return com;
                }
            }
            foreach(Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
    }
}
