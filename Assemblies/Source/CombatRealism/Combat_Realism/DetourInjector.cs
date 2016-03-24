using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using CommunityCoreLibrary;

namespace Combat_Realism
{
    class DetourInjector : SpecialInjector
    {
        public override void Inject()
        {
            // Detour VerbsTick
            Log.Message("Attempting detour from VerbsTick to VerbsTickCR");
            Detours.TryDetourFromTo(typeof(VerbTracker).GetMethod("VerbsTick", BindingFlags.Instance | BindingFlags.Public),
                typeof(VerbTrackerCR).GetMethod("VerbsTickCR", BindingFlags.Instance | BindingFlags.Public));

            // Detour TooltipUtility
            Log.Message("Attempting detour from ShotCalculationTipString to ShotCalculationTipStringCR");
            Detours.TryDetourFromTo(typeof(TooltipUtility).GetMethod("ShotCalculationTipString", BindingFlags.Static | BindingFlags.Public),
                typeof(TooltipUtilityCR).GetMethod("ShotCalculationTipStringCR", BindingFlags.Static | BindingFlags.Public));

            // Detour CalculateBleedingRate
            Log.Message("Attempting detour from CalculateBleedingRate to CalculateBleedingRateCR");
            Detours.TryDetourFromTo(typeof(HediffSet).GetMethod("CalculateBleedingRate", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(DetourUtility).GetMethod("CalculateBleedingRateCR", BindingFlags.Static | BindingFlags.Public));

            // Detour DrawTurret
            Log.Message("Attempting detour from DrawTurret to DrawTurretCR");
            Detours.TryDetourFromTo(typeof(TurretTop).GetMethod("DrawTurret", BindingFlags.Instance | BindingFlags.Public),
                typeof(DetourUtility).GetMethod("DrawTurretCR", BindingFlags.Static | BindingFlags.Public));

            // Detour FloatMenuMaker
            Log.Message("Attempting detour from ChoicesAtFor to ChoicesAtForCR");
            Detours.TryDetourFromTo(typeof(FloatMenuMaker).GetMethod("ChoicesAtFor", BindingFlags.Static | BindingFlags.Public),
                typeof(DetourUtility).GetMethod("ChoicesAtForCR", BindingFlags.Static | BindingFlags.Public));
        }
    }
}
