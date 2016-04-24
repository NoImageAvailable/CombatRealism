using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class Apparel_Backpack : Apparel_VisibleAccessory
    {
        protected override float GetAltitudeOffset(Rot4 rotation)
        {
            float offset = 0.001f;
            if (rotation == Rot4.North)
            {
                return 0.06f;
            }
            offset += 0.035f;
            if (wearer.apparel.WornApparel.Any(x => x.def.apparel.LastLayer == ApparelLayer.Shell))
            {
                offset += 0.006f;
            }
            return offset;
        }
    }
}
