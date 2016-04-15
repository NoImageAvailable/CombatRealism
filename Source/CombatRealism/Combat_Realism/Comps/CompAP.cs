using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class CompAP : ThingComp
    {
        public CompProperties_AP Props
        {
            get
            {
                return (CompProperties_AP)this.props;
            }
        }

        public override string GetDescriptionPart()
        {
            return "Armor penetration: " + GenText.AsPercent(Props.armorPenetration);
        }
    }
}
