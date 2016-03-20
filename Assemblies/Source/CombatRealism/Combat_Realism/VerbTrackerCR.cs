using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class VerbTrackerCR : VerbTracker
    {
        /// <summary>
        /// This is a hacky way of getting access to cachedVerbs, which is private
        /// </summary>
        private List<Verb> cachedVerbsCR
        {
            get
            {
                return this.AllVerbs;
            }
        }

        /// <summary>
        /// VerbsTick is detoured to here. Mimicks all the functionality of the vanilla method while also calling custom CR ticker method from CR verbs.
        /// </summary>
        public void VerbsTickCR()
        {
            if (this.cachedVerbsCR == null)
            {
                return;
            }
            for (int i = 0; i < this.cachedVerbsCR.Count; i++)
            {
                this.cachedVerbsCR[i].VerbTick();
                Verb_LaunchProjectileCR verbCR = this.cachedVerbsCR[i] as Verb_LaunchProjectileCR;
                if (verbCR != null)
                {
                    verbCR.VerbTickCR();
                }
            }
        }
    }
}
