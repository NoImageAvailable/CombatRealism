using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using CommunityCoreLibrary;

namespace Combat_Realism.Detours
{
    class DetourInjector : SpecialInjector
    {
        public override bool Inject()
        {
            // Detour VerbsTick
            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(VerbTracker).GetMethod("VerbsTick", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_VerbTracker).GetMethod("VerbsTick", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Detour TooltipUtility
            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(TooltipUtility).GetMethod("ShotCalculationTipString", BindingFlags.Static | BindingFlags.Public),
                typeof(Detours_TooltipUtility).GetMethod("ShotCalculationTipString", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Detour DrawTurret
            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(TurretTop).GetMethod("DrawTurret", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_TurretTop).GetMethod("DrawTurret", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Detour FloatMenuMaker
            if(!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(FloatMenuMaker).GetMethod("ChoicesAtFor", BindingFlags.Static | BindingFlags.Public),
                typeof(Detours_FloatMenuMaker).GetMethod("ChoicesAtFor", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // *************************************
            // *** Detour Inventory methods ***
            // *************************************

            // ThingContainer

            MethodInfo tryAddSource = typeof(ThingContainer).GetMethod("TryAdd", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Thing) }, null);
            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(tryAddSource, typeof(Detours_ThingContainer).GetMethod("TryAdd", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            MethodInfo tryDrop1Source = typeof(ThingContainer).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType() },
                null);

            MethodInfo tryDrop1Dest = typeof(Detours_ThingContainer).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(ThingContainer), typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType() },
                null);

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(tryDrop1Source, tryDrop1Dest))
                return false;

            MethodInfo tryDrop2Source = typeof(ThingContainer).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(int), typeof(Thing).MakeByRefType() },
                null);

            MethodInfo tryDrop2Dest = typeof(Detours_ThingContainer).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(ThingContainer), typeof(Thing), typeof(IntVec3), typeof(ThingPlaceMode), typeof(int), typeof(Thing).MakeByRefType() },
                null);

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(tryDrop2Source, tryDrop2Dest))
                return false;

            // Pawn_ApparelTracker

            MethodInfo tryDrop3Source = typeof(Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            MethodInfo tryDrop3Dest = typeof(Detours_Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Pawn_ApparelTracker), typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(tryDrop3Source, tryDrop3Dest))
                return false;

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_ApparelTracker).GetMethod("Wear", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_ApparelTracker).GetMethod("Wear", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_ApparelTracker).GetMethod("Notify_WornApparelDestroyed", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Detours_Pawn_ApparelTracker).GetMethod("Notify_WornApparelDestroyed", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Pawn_EquipmentTracker

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("AddEquipment", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("AddEquipment", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("Notify_PrimaryDestroyed", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("Notify_PrimaryDestroyed", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("TryDropEquipment", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("TryDropEquipment", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!CommunityCoreLibrary.Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("TryTransferEquipmentToContainer", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("TryTransferEquipmentToContainer", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            return true;
        }
    }
}
