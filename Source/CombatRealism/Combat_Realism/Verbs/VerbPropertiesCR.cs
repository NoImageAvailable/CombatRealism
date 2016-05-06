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
        public RecoilPattern recoilPattern = RecoilPattern.None;
        public float recoilAmount = 0;
        public float indirectFirePenalty = 0;
        public float meleeArmorPenetration = 0;
    }
}
