using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism
{
    static class Utility
    {
        #region Misc

        public static List<ThingDef> allWeaponDefs = new List<ThingDef>();

        /// <summary>
        /// Generates a random Vector2 in a circle with given radius
        /// </summary>
        public static Vector2 GenRandInCircle(float radius)
        {
            //Fancy math to get random point in circle
            System.Random rand = new System.Random();
            double angle = rand.NextDouble() * Math.PI * 2;
            double range = Math.Sqrt(rand.NextDouble()) * radius;
            return new Vector2((float)(range * Math.Cos(angle)), (float)(range * Math.Sin(angle)));
        }

        /// <summary>
        /// Calculates the actual current movement speed of a pawn
        /// </summary>
        /// <param name="pawn">Pawn to calculate speed of</param>
        /// <returns>Move speed in cells per second</returns>
        public static float GetMoveSpeed(Pawn pawn)
        {
            float movePerTick = 60 / pawn.GetStatValue(StatDefOf.MoveSpeed, false);    //Movement per tick
            movePerTick += PathGrid.CalculatedCostAt(pawn.Position, false);
            Building edifice = pawn.Position.GetEdifice();
            if (edifice != null)
            {
                movePerTick += (int)edifice.PathWalkCostFor(pawn);
            }

            //Case switch to handle walking, jogging, etc.
            if (pawn.CurJob != null)
            {
                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        movePerTick *= 3;
                        if (movePerTick < 60)
                        {
                            movePerTick = 60;
                        }
                        break;
                    case LocomotionUrgency.Walk:
                        movePerTick *= 2;
                        if (movePerTick < 50)
                        {
                            movePerTick = 50;
                        }
                        break;
                    case LocomotionUrgency.Jog:
                        break;
                    case LocomotionUrgency.Sprint:
                        movePerTick = Mathf.RoundToInt(movePerTick * 0.75f);
                        break;
                }
            }
            return 60 / movePerTick;
        }

        /// <summary>
        /// Attempts to find a turret operator. Accepts any Thing as input and does a sanity check to make sure it is an actual turret.
        /// </summary>
        /// <param name="thing">The turret to check for an operator</param>
        /// <returns>Turret operator if one is found, null if not</returns>
        public static Pawn TryGetTurretOperator(Thing thing)
        {
            Pawn manningPawn = null;
            Building_TurretGun turret = thing as Building_TurretGun;
            if (turret != null)
            {
                CompMannable comp = turret.TryGetComp<CompMannable>();
                if (comp != null && comp.MannedNow)
                {
                    manningPawn = comp.ManningPawn;
                }
            }
            return manningPawn;
        }

        #endregion Misc

        #region Physics

        public const float gravityConst = 9.8f;
        public const float collisionHeightFactor = 1.0f;
        public const float collisionWidthFactor = 0.5f;
        public const float collisionWidthFactorHumanoid = 0.25f;
        public static readonly String[] humanoidBodyList = { "Human", "Scyther" };
        /// <summary>
        /// Returns the collision height of a Thing
        /// </summary>
        public static float GetCollisionHeight(Thing thing)
        {
            if (thing == null)
            {
                return 0;
            }
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                float collisionHeight = pawn.BodySize;
                if (!humanoidBodyList.Contains(pawn.def.race.body.defName)) collisionHeight *= 0.5f;
                if (pawn.GetPosture() != PawnPosture.Standing)
                {
                    collisionHeight = pawn.BodySize > 1 ? pawn.BodySize - 0.8f : 0.2f * pawn.BodySize;
                }
                return collisionHeight * collisionHeightFactor;
            }
            return thing.def.fillPercent * collisionHeightFactor;
        }

        /// <summary>
        /// Calculates the width of an object for purposes of bullet collision. Return value is distance from center of object to its edge in cells, so a wall filling out an entire cell has a width of 0.5.
        /// Also accounts for general body type, humanoids must be specified in the humanoidBodyList and will have reduced width relative to their overall body size.
        /// </summary>
        /// <param name="thing">The Thing to measure width of</param>
        /// <returns>Distance from center of Thing to its edge in cells</returns>
        public static float GetCollisionWidth(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return 0.5f;    //Buildings, etc. fill out half a square to each side
            }
            return pawn.BodySize * (Utility.humanoidBodyList.Contains(pawn.RaceProps.body.defName) ? collisionWidthFactorHumanoid : collisionWidthFactor);
        }

        #endregion Physics

        #region Armor

        public static readonly DamageDef absorbDamageDef = DamageDefOf.Blunt;   //The damage def to convert absorbed shots into

        /// <summary>
        /// Calculates deflection chance and damage through armor
        /// </summary>
        public static int GetAfterArmorDamage(Pawn pawn, int damAmountInt, BodyPartRecord part, DamageInfo dinfo, bool damageArmor, ref bool deflected)
        {
            DamageDef damageDef = dinfo.Def;
            if (damageDef.armorCategory == DamageArmorCategory.IgnoreArmor)
            {
                return damAmountInt;
            }

            float damageAmount = (float)damAmountInt;
            StatDef deflectionStat = damageDef.armorCategory.DeflectionStat();

            // Get armor penetration value
            float pierceAmount = 0f;
            if (dinfo.Source != null)
            {
                ProjectilePropertiesCR projectileProps = dinfo.Source.projectile as ProjectilePropertiesCR;
                if (projectileProps != null)
                {
                    pierceAmount = projectileProps.armorPenetration;
                }
                else if (dinfo.Instigator != null)
                {
                    Pawn instigatorPawn = dinfo.Instigator as Pawn;
                    if (instigatorPawn != null)
                    {
                        if (instigatorPawn.equipment != null && instigatorPawn.equipment.Primary != null)
                        {
                            pierceAmount = instigatorPawn.equipment.Primary.GetStatValue(StatDef.Named("ArmorPenetration"));
                        }
                        else
                        {
                            pierceAmount = instigatorPawn.GetStatValue(StatDef.Named("ArmorPenetration"));
                        }
                    }
                }
            }

            // Run armor calculations on all apparel
            if (pawn.apparel != null)
            {
                List<Apparel> wornApparel = new List<Apparel>(pawn.apparel.WornApparel);
                for (int i = wornApparel.Count - 1; i >= 0; i--)
                {
                    if (wornApparel[i].def.apparel.CoversBodyPart(part))
                    {
                        Thing armorThing = damageArmor ? wornApparel[i] : null;

                        //Check for deflection
                        if (Utility.ApplyArmor(ref damageAmount, ref pierceAmount, wornApparel[i].GetStatValue(deflectionStat, true), armorThing, damageDef))
                        {
                            deflected = true;
                            if (damageDef != absorbDamageDef)
                            {
                                damageDef = absorbDamageDef;
                                deflectionStat = damageDef.armorCategory.DeflectionStat();
                                i++;
                            }
                        }
                        if (damageAmount < 0.001)
                        {
                            return 0;
                        }
                    }
                }
            }
            // Check for pawn racial armor
            float pawnArmorAmount = 0f;
            bool partCoveredByArmor = false;
            if (part.IsInGroup(DefDatabase<BodyPartGroupDef>.GetNamed("CoveredByNaturalArmor")))
            {
                partCoveredByArmor = true;
            }
            else
            {
                BodyPartRecord outerPart = part;
                while (outerPart.parent != null && outerPart.depth != BodyPartDepth.Outside)
                {
                    outerPart = outerPart.parent;
                }
                partCoveredByArmor = outerPart != part && outerPart.IsInGroup(DefDatabase<BodyPartGroupDef>.GetNamed("CoveredByNaturalArmor"));
            }
            if (partCoveredByArmor)
            {
                pawnArmorAmount = pawn.GetStatValue(deflectionStat);
            }

            if (pawnArmorAmount > 0 && Utility.ApplyArmor(ref damageAmount, ref pierceAmount, pawnArmorAmount, null, damageDef))
            {
                deflected = true;
                if (damageAmount < 0.001)
                {
                    return 0;
                }
                damageDef = absorbDamageDef;
                deflectionStat = damageDef.armorCategory.DeflectionStat();
                Utility.ApplyArmor(ref damageAmount, ref pierceAmount, pawn.GetStatValue(deflectionStat, true), pawn, damageDef);
            }
            return Mathf.RoundToInt(damageAmount);
        }

        private static bool ApplyArmor(ref float damAmount, ref float pierceAmount, float armorRating, Thing armorThing, DamageDef damageDef)
        {
            float originalDamage = damAmount;
            bool deflected = false;
            DamageDef_CR damageDefCR = damageDef as DamageDef_CR;
            float penetrationChance = 1;
            if(damageDefCR != null && damageDefCR.deflectable)
                penetrationChance = Mathf.Clamp((pierceAmount - armorRating) * 6, 0, 1);

            //Shot is deflected
            if (penetrationChance == 0 || Rand.Value > penetrationChance)
            {
                deflected = true;
            }
            //Damage calculations
            float dMult = 1;
            if (damageDefCR != null)
            {
                if (damageDefCR.absorbable && deflected)
                {
                    dMult = 0;
                }
                else if (damageDefCR.deflectable)
                {
                    dMult = Mathf.Clamp01(0.5f + (pierceAmount - armorRating) * 3);
                }
            }
            else
            {
                dMult = Mathf.Clamp01(1 - armorRating);
            }
            damAmount *= dMult;

            //Damage armor
            if (armorThing != null && armorThing as Pawn == null)
            {
                float absorbedDamage = (originalDamage - damAmount) * Mathf.Min(pierceAmount, 1f);
                if (deflected)
                {
                    absorbedDamage *= 0.5f;
                }
                armorThing.TakeDamage(new DamageInfo(damageDef, Mathf.CeilToInt(absorbedDamage), null, null, null));
            }

            pierceAmount *= dMult;
            return deflected;
        }

        #endregion Armor

        #region Inventory

        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn != null)
            {
                CompInventory comp = pawn.TryGetComp<CompInventory>();
                if (comp != null)
                {
                    comp.UpdateInventory();
                }
            }
        }

        public static void TryUpdateInventory(Pawn_InventoryTracker tracker)
        {
            if (tracker != null && tracker.pawn != null)
            {
                TryUpdateInventory(tracker.pawn);
            }
        }

        #endregion Inventory
    }
}
