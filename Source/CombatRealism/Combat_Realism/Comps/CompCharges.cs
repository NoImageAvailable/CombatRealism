using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompCharges : ThingComp
    {
        public CompProperties_Charges Props
        {
            get
            {
                return (CompProperties_Charges)this.props;
            }
        }

        public bool GetChargeBracket(float range, out Vector2 bracket)
        {
            bracket = new Vector2(0, 0);
            if (Props.charges.Count <= 0)
            {
                Log.Error("Tried getting charge bracket from empty list.");
                return false;
            }
            foreach (Vector2 vec in Props.charges)
            {
                if (range <= vec.y)
                {
                    bracket = vec;
                    return true;
                }
            }
            return false;
        }
    }
}
