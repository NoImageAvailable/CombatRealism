using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class IncendiaryFuel : Thing
    {
        private const float maxFireSize = 1.75f;

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            List<Thing> list = new List<Thing>(Position.GetThingList());
            foreach (Thing thing in list)
            {
                if (thing.HasAttachment(ThingDefOf.Fire))
                {
                    Fire fire = (Fire)thing.GetAttachment(ThingDefOf.Fire);
                    if (fire != null)
                        fire.fireSize = maxFireSize;
                }
                else
                {
                    thing.TryAttachFire(maxFireSize);
                }
            }
        }

        public override void Tick()
        {
            if(Position.GetThingList().Any(x => x.def == ThingDefOf.FilthFireFoam))
            {
                if (!Destroyed)
                    Destroy();
            }
            else
            {
                FireUtility.TryStartFireIn(Position, maxFireSize);
            }
        }
    }
}
