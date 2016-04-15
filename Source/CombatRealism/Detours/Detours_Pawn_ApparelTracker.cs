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
    internal static class Detours_Pawn_ApparelTracker
    {
        internal static bool TryDrop(this Pawn_ApparelTracker _this, Apparel ap, out Apparel resultingAp, IntVec3 pos, bool forbid = true)
        {
            if (!_this.WornApparel.Contains(ap))
            {
                Log.Warning(_this.pawn.LabelCap + " tried to drop apparel he didn't have: " + ap.LabelCap);
                resultingAp = null;
                return false;
            }
            _this.WornApparel.Remove(ap);
            ap.wearer = null;
            Thing thing = null;
            bool flag = GenThing.TryDropAndSetForbidden(ap, pos, ThingPlaceMode.Near, out thing, forbid);
            resultingAp = (thing as Apparel);
            _this.pawn.Drawer.renderer.graphics.ResolveApparelGraphics();
            if (flag && _this.pawn.outfits != null)
            {
                _this.pawn.outfits.forcedHandler.SetForced(ap, false);
            }
            Utility.TryUpdateInventory(_this.pawn);     // Apparel was dropped, update inventory
            return flag;
        }

        internal static void Wear(this Pawn_ApparelTracker _this, Apparel newApparel, bool dropReplacedApparel = true)
        {
            SlotGroupUtility.Notify_TakingThing(newApparel);
            if (newApparel.Spawned)
            {
                newApparel.DeSpawn();
            }
            if (!ApparelUtility.HasPartsToWear(_this.pawn, newApparel.def))
            {
                Log.Warning(string.Concat(new object[]
		{
			_this.pawn,
			" tried to wear ",
			newApparel,
			" but he has no body parts required to wear it."
		}));
                return;
            }
            for (int i = _this.WornApparel.Count - 1; i >= 0; i--)
            {
                Apparel apparel = _this.WornApparel[i];
                if (!ApparelUtility.CanWearTogether(newApparel.def, apparel.def))
                {
                    bool forbid = _this.pawn.Faction.HostileTo(Faction.OfColony);
                    if (dropReplacedApparel)
                    {
                        Apparel apparel2;
                        if (!_this.TryDrop(apparel, out apparel2, _this.pawn.Position, forbid))
                        {
                            Log.Error(_this.pawn + " could not drop " + apparel);
                            return;
                        }
                    }
                    else
                    {
                        _this.WornApparel.Remove(apparel);
                    }
                }
            }
            _this.WornApparel.Add(newApparel);
            newApparel.wearer = _this.pawn;

            Utility.TryUpdateInventory(_this.pawn);     // Apparel was added, update inventory
            MethodInfo methodInfo = typeof(Pawn_ApparelTracker).GetMethod("SortWornApparelIntoDrawOrder", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(_this, new object[] { });

            LongEventHandler.ExecuteWhenFinished(new Action(_this.pawn.Drawer.renderer.graphics.ResolveApparelGraphics));
        }

        internal static void Notify_WornApparelDestroyed(this Pawn_ApparelTracker _this, Apparel apparel)
        {
            _this.WornApparel.Remove(apparel);
            LongEventHandler.ExecuteWhenFinished(new Action(_this.pawn.Drawer.renderer.graphics.ResolveApparelGraphics));
            if (_this.pawn.outfits != null && _this.pawn.outfits.forcedHandler != null)
            {
                _this.pawn.outfits.forcedHandler.Notify_Destroyed(apparel);
            }
            Utility.TryUpdateInventory(_this.pawn);     // Apparel was destroyed, update inventory
        }
    }
}
