using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class CompProperties_AP : CompProperties
    {
        public float armorPenetration = 0f;
        
        public CompProperties_AP()
        {
            this.compClass = typeof(CompProperties_AP);
        }
    }

    class CompAP : ThingComp
    {
        new public CompProperties_AP props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompProperties_AP cprops = props as CompProperties_AP;
            if (cprops != null)
            {
                this.props = cprops;
            }
        }

        public override string GetDescriptionPart()
        {
            return "Armor penetration: " + GenText.AsPercent(props.armorPenetration);
        }
    }
}
