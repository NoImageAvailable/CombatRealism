using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public static class DetourUtility
    {
        public static float CalculateBleedingRateCR(this HediffSet _this)
        {
            if (!_this.pawn.RaceProps.isFlesh || _this.pawn.health.Dead)
            {
                return 0f;
            }
            float bleedAmount = 0f;
            for (int i = 0; i < _this.hediffs.Count; i++)
            {
                float hediffBleedRate = _this.hediffs[i].BleedRate;
                if (_this.hediffs[i].Part != null)
                {
                    hediffBleedRate *= _this.hediffs[i].Part.def.bleedingRateMultiplier;
                }
                bleedAmount += hediffBleedRate;
            }
            float value = 0.0142857144f * bleedAmount * 2 / _this.pawn.HealthScale;
            return Mathf.Max(0, value);
        }

        private static FieldInfo parentTurretFieldInfo = typeof(TurretTop).GetField("parentTurret", BindingFlags.Instance | BindingFlags.NonPublic);
        private static PropertyInfo curRotationPropertyInfo = typeof(TurretTop).GetProperty("CurRotation", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void DrawTurretCR(this TurretTop _this)
        {
            Matrix4x4 matrix = default(Matrix4x4);
            Vector3 vec = new Vector3(1, 1, 1);
            Building_Turret parentTurret = (Building_Turret)parentTurretFieldInfo.GetValue(_this);
            float curRotation = (float)curRotationPropertyInfo.GetValue(_this, null);
            Material topMat = parentTurret.def.building.turretTopMat;
            if (topMat.mainTexture.height >= 256 || topMat.mainTexture.width >= 256)
            {
                vec.x = 2;
                vec.z = 2;
            }
            matrix.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, curRotation.ToQuat(), vec);
            Graphics.DrawMesh(MeshPool.plane20, matrix, parentTurret.def.building.turretTopMat, 0);
        }
    }
}
