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
            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(VerbTracker).GetMethod("VerbsTick", BindingFlags.Instance | BindingFlags.Public),
                typeof(VerbTrackerCR).GetMethod("VerbsTickCR", BindingFlags.Instance | BindingFlags.Public));

            // Detour TooltipUtility
            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(TooltipUtility).GetMethod("ShotCalculationTipString", BindingFlags.Static | BindingFlags.Public),
                typeof(TooltipUtilityCR).GetMethod("ShotCalculationTipStringCR", BindingFlags.Static | BindingFlags.Public));

            // Detour CalculateBleedingRate
            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(HediffSet).GetMethod("CalculateBleedingRate", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(DetourUtility).GetMethod("CalculateBleedingRateCR", BindingFlags.Static | BindingFlags.Public));

            // Detour DrawTurret
            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(TurretTop).GetMethod("DrawTurret", BindingFlags.Instance | BindingFlags.Public),
                typeof(DetourUtility).GetMethod("DrawTurretCR", BindingFlags.Static | BindingFlags.Public));

            // Detour FloatMenuMaker
            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(FloatMenuMaker).GetMethod("ChoicesAtFor", BindingFlags.Static | BindingFlags.Public),
                typeof(DetourUtility).GetMethod("ChoicesAtForCR", BindingFlags.Static | BindingFlags.Public));

            // *************************************
            // *** Detour Inventory methods ***
            // *************************************

            // ThingContainer

            MethodInfo tryAddSource = typeof(ThingContainer).GetMethod("TryAdd", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Thing) }, null);
            CommunityCoreLibrary.Detours.TryDetourFromTo(tryAddSource, typeof(Detours.Detours_ThingContainer).GetMethod("TryAdd", BindingFlags.Static | BindingFlags.Public));

            MethodInfo tryDrop1Source = typeof(ThingContainer).GetMethod("TryDrop", 
                BindingFlags.Instance | BindingFlags.Public, 
                null, 
                new Type[] { typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType() },
                null);

            MethodInfo tryDrop1Dest = typeof(Detours.Detours_ThingContainer).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(ThingContainer), typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType() },
                null);

            CommunityCoreLibrary.Detours.TryDetourFromTo(tryDrop1Source, tryDrop1Dest);

            MethodInfo tryDrop2Source = typeof(ThingContainer).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(int), typeof(Thing).MakeByRefType() },
                null);

            MethodInfo tryDrop2Dest = typeof(Detours.Detours_ThingContainer).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(ThingContainer), typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(int), typeof(Thing).MakeByRefType() },
                null);

            CommunityCoreLibrary.Detours.TryDetourFromTo(tryDrop2Source, tryDrop2Dest);

            // Pawn_ApparelTracker

            MethodInfo tryDrop3Source = typeof(Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            MethodInfo tryDrop3Dest = typeof(Detours.Detours_Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(Pawn_ApparelTracker), typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            CommunityCoreLibrary.Detours.TryDetourFromTo(tryDrop3Source, tryDrop3Dest);

            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_ApparelTracker).GetMethod("Wear", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours.Detours_Pawn_ApparelTracker).GetMethod("Wear", BindingFlags.Static | BindingFlags.Public));

            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_ApparelTracker).GetMethod("Notify_WornApparelDestroyed", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Detours.Detours_Pawn_ApparelTracker).GetMethod("Notify_WornApparelDestroyed", BindingFlags.Static | BindingFlags.Public));

            // Pawn_EquipmentTracker

            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("AddEquipment", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours.Detours_Pawn_EquipmentTracker).GetMethod("AddEquipment", BindingFlags.Static | BindingFlags.Public));

            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("Notify_PrimaryDestroyed", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Detours.Detours_Pawn_EquipmentTracker).GetMethod("Notify_PrimaryDestroyed", BindingFlags.Static | BindingFlags.Public));

            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("TryDropEquipment", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours.Detours_Pawn_EquipmentTracker).GetMethod("TryDropEquipment", BindingFlags.Static | BindingFlags.Public));

            CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("TryTransferEquipmentToContainer", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours.Detours_Pawn_EquipmentTracker).GetMethod("TryTransferEquipmentToContainer", BindingFlags.Static | BindingFlags.Public));
        }
    }
}
