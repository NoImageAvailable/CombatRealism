﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public class LoadoutGenerator
    {
        public float skipChance = 0f;
        public ThingDef thingDef;
        public int minCount = 0;
        public int maxCount = 0;
        protected CompInventory compInvInt = null;
        protected ThingDef thingToMake;

        /// <summary>
        /// Generates a loadout from available ThingDefs with respect to inventory constraints and deposits items straight into inventory
        /// </summary>
        /// <param name="inventory">Inventory comp to fill</param>
        public virtual void GenerateLoadout(CompInventory inventory)
        {
            if (inventory == null)
            {
                Log.Error("Tried generating loadout without inventory");
                return;
            }
            if (UnityEngine.Random.value < skipChance)
            {
                return;
            }
            compInvInt = inventory;

            // Calculate thing count
            int thingsToMake = UnityEngine.Random.Range(minCount, maxCount);

            // Make things
            while (thingsToMake > 0)
            {
                Thing thing = GenerateLoadoutThing(UnityEngine.Random.Range(1, thingsToMake));
                if (thing == null)
                {
                    return;
                }
                int maxCount;
                if(compInvInt.CanFitInInventory(thing, out maxCount))
                {
                    GenSpawn.Spawn(thing, inventory.parent.Position);

                    // If we cant fit the whole stack, fit as much as we can and return
                    if(maxCount < thing.stackCount)
                    {
                        thing.stackCount = maxCount;
                        compInvInt.container.TryAdd(thing, thing.stackCount);
                        return;
                    }
                    compInvInt.container.TryAdd(thing, thing.stackCount);
                    thingsToMake -= maxCount;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Generates a random thing from the available things list
        /// </summary>
        /// <param name="max">Maximum stack count to generate</param>
        protected virtual Thing GenerateLoadoutThing(int max)
        {
            if (thingToMake == null)
            {
                if(thingDef == null)
                {
                    Log.Error("Tried to make thing from null def");
                    return null;
                }
                thingToMake = thingDef;
            }
            Thing thing = ThingMaker.MakeThing(thingToMake);
            thing.stackCount = UnityEngine.Random.Range(1, Mathf.Min(thing.def.stackLimit, max));
            return thing;
        }
    }
}
