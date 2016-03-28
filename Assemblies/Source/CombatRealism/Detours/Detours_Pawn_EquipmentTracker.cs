using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    public static class Detours_Pawn_EquipmentTracker
    {
        private static FieldInfo pawnFieldInfo = typeof(Pawn_EquipmentTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo primaryIntFieldInfo = typeof(Pawn_EquipmentTracker).GetField("primaryInt", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void AddEquipment(this Pawn_EquipmentTracker _this, ThingWithComps newEq)
        {
            SlotGroupUtility.Notify_TakingThing(newEq);

            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            ThingWithComps primaryInt = (ThingWithComps)primaryIntFieldInfo.GetValue(_this);

            if (_this.AllEquipment.Where(eq => eq.def == newEq.def).Any<ThingWithComps>())
            {
                Log.Error(string.Concat(new object[]
		        {
			        "Pawn ",
			        pawn.LabelCap,
			        " got ability ",
			        newEq,
			        " while already having it."
		        }));
                return;
            }
            if (newEq.def.equipmentType == EquipmentType.Primary && primaryInt != null)
            {
                Log.Error(string.Concat(new object[]
		        {
			        "Pawn ",
			        pawn.LabelCap,
			        " got primaryInt ability ",
			        newEq,
			        " while already having primaryInt ability ",
			        primaryInt
		        }));
                return;
            }
            if (newEq.def.equipmentType == EquipmentType.Primary)
            {
                Log.Message("Assigning primaryInt as newEq");
                primaryIntFieldInfo.SetValue(_this, newEq);
            }
            newEq.GetComp<CompEquippable>().verbTracker.InitVerbs();
            foreach (Verb current in newEq.GetComp<CompEquippable>().AllVerbs)
            {
                current.caster = pawn;
                current.Notify_PickedUp();
            }
            Utility.TryUpdateInventory(pawn);
        }

        public static void Notify_PrimaryDestroyed(this Pawn_EquipmentTracker _this)
        {
            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            ThingWithComps primaryInt = (ThingWithComps)primaryIntFieldInfo.GetValue(_this);

            primaryIntFieldInfo.SetValue(_this, null);
            pawn.stances.CancelBusyStanceSoft();

            Utility.TryUpdateInventory(pawn);
        }

        public static bool TryDropEquipment(this Pawn_EquipmentTracker _this,ThingWithComps eq, out ThingWithComps resultingEq, IntVec3 pos, bool forbid = true)
        {
            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            ThingWithComps primaryInt = (ThingWithComps)primaryIntFieldInfo.GetValue(_this);

            if (!_this.AllEquipment.Contains(eq))
            {
                Log.Warning(pawn.LabelCap + " tried to drop equipment they didn't have: " + eq);
                resultingEq = null;
                return false;
            }
            if (!pos.IsValid)
            {
                Log.Error(string.Concat(new object[]
		        {
			        pawn,
			        " tried to drop ",
			        eq,
			        " at invalid cell."
		        }));
                resultingEq = null;
                return false;
            }
            if (primaryInt == eq)
            {
                primaryIntFieldInfo.SetValue(_this, null);
            }
            Thing thing = null;
            bool flag = GenThing.TryDropAndSetForbidden(eq, pos, ThingPlaceMode.Near, out thing, forbid);
            resultingEq = (thing as ThingWithComps);
            if (flag && resultingEq != null)
            {
                resultingEq.GetComp<CompEquippable>().Notify_Dropped();
            }
            Utility.TryUpdateInventory(pawn);
            return flag;
        }

        public static bool TryTransferEquipmentToContainer(this Pawn_EquipmentTracker _this, ThingWithComps eq, ThingContainer container, out ThingWithComps resultingEq)
        {
            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            ThingWithComps primaryInt = (ThingWithComps)primaryIntFieldInfo.GetValue(_this);

            if (!_this.AllEquipment.Contains(eq))
            {
                Log.Warning(pawn.LabelCap + " tried to transfer equipment he didn't have: " + eq);
                resultingEq = null;
                return false;
            }
            if (container.TryAdd(eq))
            {
                resultingEq = null;
            }
            else
            {
                resultingEq = eq;
            }
            if (primaryInt == eq)
            {
                primaryIntFieldInfo.SetValue(_this, null);
            }
            Utility.TryUpdateInventory(pawn);
            return resultingEq == null;
        }
    }
}
