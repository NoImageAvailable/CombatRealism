using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal static class Detours_TurretTop
    {
        // *** Turret rendering ***

        private static readonly FieldInfo parentTurretFieldInfo = typeof(TurretTop).GetField("parentTurret", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo curRotationPropertyInfo = typeof(TurretTop).GetProperty("CurRotation", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void DrawTurret(this TurretTop _this)
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
