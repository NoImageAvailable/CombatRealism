using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal static class Detours_TooltipUtility
    {
        internal static string ShotCalculationTipString(Thing target)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Find.Selector.SingleSelectedThing != null)
            {
                Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
                if (pawn != null && pawn != target && pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    Verb_LaunchProjectile verb_LaunchProjectile = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_LaunchProjectile;
                    if (verb_LaunchProjectile != null)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.Append("ShotBy".Translate(new object[]
				        {
					        pawn.LabelBaseShort
				        }) + ":");
                        if (verb_LaunchProjectile.CanHitTarget(target))
                        {
                            HitReport hitReport = verb_LaunchProjectile.HitReportFor(target);
                            stringBuilder.Append(hitReport.GetTextReadout());
                        }
                        else
                        {
                            stringBuilder.Append("CannotHit".Translate());
                        }
                    }
                    // Append CR tooltip
                    else
                    {
                        Verb_LaunchProjectileCR verbCR = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_LaunchProjectileCR;
                        if (verbCR != null)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.Append("ShotBy".Translate(new object[] { pawn.LabelBaseShort }) + ":\n");
                            if (verbCR.CanHitTarget(target))
                            {
                                ShiftVecReport report = verbCR.ShiftVecReportFor(target);
                                stringBuilder.Append(report.GetTextReadout());
                            }
                            else
                            {
                                stringBuilder.Append("CannotHit".Translate());
                            }
                        }
                    }
                }
            }
            return stringBuilder.ToString();
        }
    }
}
