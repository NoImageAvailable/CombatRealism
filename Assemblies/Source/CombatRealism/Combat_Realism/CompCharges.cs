using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class CompProperties_Charges : CompProperties
    {
        // Charges are paired as velocity / range
        public List<Vector2> charges = new List<Vector2>();

        public CompProperties_Charges()
        {
            this.compClass = typeof(CompProperties_Charges);
        }
    }

    public class CompCharges : ThingComp
    {
        private CompProperties_Charges cprops;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompProperties_Charges cprops = props as CompProperties_Charges;
            if (cprops != null)
            {
                this.cprops = cprops;
            }
        }

        public bool GetChargeBracket(float range, out Vector2 bracket)
        {
            bracket = new Vector2(0, 0);
            if (this.cprops == null || cprops.charges.Count <= 0)
            {
                Log.Error("Tried getting charge bracket from empty list.");
                return false;
            }
            foreach (Vector2 vec in cprops.charges)
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
