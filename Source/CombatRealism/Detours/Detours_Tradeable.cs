using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal static class Detours_Tradeable
    {
        // Copy-pasted from Tradeable so we don't have to fetch it through reflection
        private static readonly SimpleCurve LaunchPricePostFactorCurve = new SimpleCurve
        {
            new CurvePoint(2000f, 1f),
            new CurvePoint(12000f, 0.5f),
            new CurvePoint(200000f, 0.2f)
        };

        internal static float PriceFor(this Tradeable _this, TradeAction action)
        {
            float num = TradeSession.trader.TraderKind.PriceTypeFor(_this.ThingDef, action).PriceMultiplier();
            float num2 = TradeUtility.RandomPriceFactorFor(TradeSession.trader, _this);
            float num3 = 1f;
            if (TradeSession.playerNegotiator != null)
            {
                float num4 = Mathf.Clamp01(TradeSession.playerNegotiator.health.capacities.GetEfficiency(PawnCapacityDefOf.Talking));
                if (action == TradeAction.PlayerBuys)
                {
                    num3 += 1f - num4;
                }
                else
                {
                    num3 -= 0.5f * (1f - num4);
                }
            }
            float num5;
            if (action == TradeAction.PlayerBuys)
            {
                num5 = _this.BaseMarketValue * (1f - TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement, true)) * num3 * num * num2;
                num5 = Mathf.Max(num5, 0.01f);
            }
            else
            {
                num5 = _this.BaseMarketValue * Find.Storyteller.difficulty.baseSellPriceFactor * _this.AnyThing.GetStatValue(StatDefOf.SellPriceFactor, true) * (1f + TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement, true)) * num3 * num * num2;
                num5 *= Detours_Tradeable.LaunchPricePostFactorCurve.Evaluate(num5);
                num5 = Mathf.Max(num5, 0.01f);
                if (num5 >= _this.PriceFor(TradeAction.PlayerBuys))
                {
                    Log.ErrorOnce("Skill of negotitator trying to put sell price above buy price.", 65387);
                    num5 = _this.PriceFor(TradeAction.PlayerBuys);
                }
            }
            if (num5 > 99.5f)
            {
                num5 = Mathf.Round(num5);
            }
            return num5;
        }
    }
}
