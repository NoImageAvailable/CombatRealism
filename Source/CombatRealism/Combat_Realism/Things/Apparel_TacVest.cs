using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class Apparel_TacVest : Apparel_VisibleAccessory
    {
        protected override float GetAltitudeOffset(Rot4 rotation)
        {
            float offset = 0.033f;
            if (wearer.apparel.WornApparel.Any(x => x.def.apparel.LastLayer == ApparelLayer.Shell))
            {
                offset += 0.006f;
                if (rotation == Rot4.North)
                {
                    offset += 0.02f;
                }
            }
            else if (rotation == Rot4.North)
            {
                offset -= 0.005f;
            }
            return offset;
        }
    }
}
