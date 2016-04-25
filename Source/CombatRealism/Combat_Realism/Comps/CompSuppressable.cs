using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompSuppressable : ThingComp
    {
        public CompProperties_Suppressable Props
        {
            get
            {
                return (CompProperties_Suppressable)this.props;
            }
        }

        // --------------- Global constants ---------------

        public const float minSuppressionDist = 10f;        //Minimum distance to be suppressed from, so melee won't be suppressed if it closes within this distance
        private const float maxSuppression = 100f;          //Cap to prevent suppression from building indefinitely
        private const float suppressionDecayRate = 7.5f;    //How much suppression decays per second
        private const int ticksPerMote = 150;               //How many ticks between throwing a mote

        // --------------- Location calculations ---------------

        /*
         * We track the initial location from which a pawn was suppressed and the total amount of suppression coming from that location separately.
         * That way if suppression stops coming from location A but keeps coming from location B the location will get updated without bouncing 
         * pawns or having to track fire coming from multiple locations
         */
        private IntVec3 suppressorLocInt;
        public IntVec3 suppressorLoc
        {
            get
            {
                return this.suppressorLocInt;
            }
        }
        private float locSuppressionAmount = 0f;

        // --------------- Suppression calculations ---------------
        private float currentSuppressionInt = 0f;
        public float currentSuppression
        {
            get
            {
                return this.currentSuppressionInt;
            }
        }
        public float parentArmor
        {
            get
            {
                float armorValue = 0f;
                Pawn pawn = this.parent as Pawn;
                if (pawn != null)
                {
                    //Get most protective piece of armor
                    if (pawn.apparel.WornApparel != null && pawn.apparel.WornApparel.Count > 0)
                    {
                        List<Apparel> wornApparel = new List<Apparel>(pawn.apparel.WornApparel);
                        foreach (Apparel apparel in wornApparel)
                        {
                            float apparelArmor = apparel.GetStatValue(StatDefOf.ArmorRating_Sharp, true);
                            if (apparelArmor > armorValue)
                            {
                                armorValue = apparelArmor;
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("Tried to get parent armor of non-pawn");
                }
                return armorValue;
            }
        }
        private float suppressionThreshold
        {
            get
            {
                float threshold = 0f;
                Pawn pawn = this.parent as Pawn;
                if (pawn != null)
                {
                    //Get morale
                    float hardBreakThreshold = pawn.GetStatValue(StatDefOf.MentalBreakThreshold) + 0.15f;
                    float currentMood = pawn.needs != null && pawn.needs.mood != null ? pawn.needs.mood.CurLevel : 0.5f;
                    threshold = Mathf.Max(0, (currentMood - hardBreakThreshold));
                }
                else
                {
                    Log.Error("Tried to get suppression threshold of non-pawn");
                }
                return threshold * maxSuppression * 0.5f;
            }
        }

        public bool isSuppressed = false;
        public bool isHunkering
        {
            get
            {
                if (this.currentSuppressionInt > this.suppressionThreshold * 2)
                {
                    if (this.isSuppressed)
                    {
                        return true;
                    }
                    else
                    {
                        Log.Warning("Hunkering without suppression, this should never happen");
                    }
                }
                return false;
            }
        }

        // --------------- Public functions ---------------
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue<float>(ref currentSuppressionInt, "currentSuppression", 0f);
            Scribe_Values.LookValue<IntVec3>(ref suppressorLocInt, "suppressorLoc");
            Scribe_Values.LookValue<float>(ref locSuppressionAmount, "locSuppression", 0f);
        }

        public void AddSuppression(float amount, IntVec3 origin)
        {
            //Add suppression to global suppression counter
            this.currentSuppressionInt += amount;
            if (this.currentSuppressionInt > maxSuppression)
            {
                this.currentSuppressionInt = maxSuppression;
            }

            //Add suppression to current suppressor location if appropriate
            if (this.suppressorLocInt == origin)
            {
                this.locSuppressionAmount += amount;
            }
            else if (this.locSuppressionAmount < this.suppressionThreshold)
            {
                this.suppressorLocInt = origin;
                this.locSuppressionAmount = this.currentSuppressionInt;
            }

            //Assign suppressed status and interrupt activity if necessary
            if (!this.isSuppressed && this.currentSuppressionInt > this.suppressionThreshold)
            {
                this.isSuppressed = true;
                Pawn pawn = this.parent as Pawn;
                if (pawn != null)
                {
                    if (pawn.jobs != null)
                    {
                        pawn.jobs.StopAll(false);
                    }
                }
                else
                {
                    Log.Error("Trying to suppress non-pawn " + this.parent.ToString() + ", this should never happen");
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            //Apply decay once per second
            if (Gen.IsHashIntervalTick(this.parent, 60))
            {
                //Decay global suppression
                if (this.currentSuppressionInt > suppressionDecayRate)
                {
                    this.currentSuppressionInt -= suppressionDecayRate;

                    //Check if pawn is still suppressed
                    if (this.isSuppressed && this.currentSuppressionInt <= this.suppressionThreshold)
                    {
                        this.isSuppressed = false;
                    }
                }
                else if (this.currentSuppressionInt > 0)
                {
                    this.currentSuppressionInt = 0;
                    this.isSuppressed = false;
                }

                //Decay location suppression
                if (this.locSuppressionAmount > suppressionDecayRate)
                {
                    this.locSuppressionAmount -= suppressionDecayRate;
                }
                else if (this.locSuppressionAmount > 0)
                {
                    this.locSuppressionAmount = 0;
                }
            }
            //Throw mote at set interval
            if (Gen.IsHashIntervalTick(this.parent, ticksPerMote))
            {
                if (this.isSuppressed)
                {
                    MoteThrower.ThrowText(this.parent.Position.ToVector3Shifted(), "CR_SuppressedMote".Translate());
                }
            }
        }
    }
}
