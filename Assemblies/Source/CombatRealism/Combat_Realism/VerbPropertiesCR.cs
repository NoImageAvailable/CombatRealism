using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class VerbPropertiesCR : VerbProperties
    {
        public Vector2 recoilOffsetX = new Vector2(0, 0);       // Recoil will shift targeting on the x axis within this range
        public Vector2 recoilOffsetY = new Vector2(0, 0);       // Recoil will shift targeting on the y axis within this range
        public int pelletCount = 1;                             // The amount of projectiles to create per shot
        public float indirectFirePenalty = 0;
        public float verbArmorPenetration = 0;
    }
}
