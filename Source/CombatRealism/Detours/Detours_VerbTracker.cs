using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal static class Detours_VerbTracker
    {
        internal static void VerbsTick(this VerbTracker _this)
        {
            if (_this.AllVerbs == null)
            {
                return;
            }
            foreach (Verb verb in _this.AllVerbs)
            {
                verb.VerbTick();

                // If we have a CR verb, call custom VerbTicker
                Verb_LaunchProjectileCR verbCR = verb as Verb_LaunchProjectileCR;
                if (verbCR != null)
                    verbCR.VerbTickCR();
            }
        }
    }
}
