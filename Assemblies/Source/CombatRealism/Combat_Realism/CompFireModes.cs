using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Combat_Realism
{
    public class CompPropertiesFireModes : CompProperties
    {
        public int aimedBurstShotCount = -1;    //will default to regular burst setting if not specified in def
        public bool aiUseAimMode = false;
        public bool aiUseBurstMode = false;
        public bool noSingleShot = false;

        public CompPropertiesFireModes()
            : base()
        {
        }
        public CompPropertiesFireModes(Type compClass)
            : base(compClass)
        {
        }
    }

    public class CompFireModes : CommunityCoreLibrary.CompRangedGizmoGiver
	{
        new public CompPropertiesFireModes props;

        // Fire mode variables
        private Verb verbInt = null;
        private Verb verb
        {
            get
            {
                if (this.verbInt == null)
                {
                    CompEquippable compEquippable = this.parent.TryGetComp<CompEquippable>();
                    if (compEquippable != null)
                    {
                        this.verbInt = compEquippable.PrimaryVerb;
                    }
                    else
                    {
                        Log.ErrorOnce(this.parent.LabelCap + " has CompFireModes but no CompEquippable", 50020);
                    }
                }
                return this.verbInt;
            }
        }
        public Thing caster
        {
            get
            {
                return this.verb.caster;
            }
        }
        public Pawn casterPawn
        {
            get
            {
                return this.caster as Pawn;
            }
        }
        private List<FireMode> availableFireModes = new List<FireMode>();
        private List<AimMode> availableAimModes = new List<AimMode> { AimMode.Snapshot, AimMode.AimedShot };

        private FireMode currentFireModeInt;
        public FireMode currentFireMode
        {
            get
            {
                return this.currentFireModeInt;
            }
        }
        private AimMode currentAimModeInt;
        public AimMode currentAimMode
        {
            get
            {
                return this.currentAimModeInt;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompPropertiesFireModes cprops = props as CompPropertiesFireModes;
            if (cprops != null)
            {
                this.props = cprops;
            }

            // Calculate available fire modes
            if (this.verb.verbProps.burstShotCount > 1 || this.props.noSingleShot)
            {
                this.availableFireModes.Add(FireMode.AutoFire);
            }
            if (this.props.aimedBurstShotCount > 1)
            {
                if (this.props.aimedBurstShotCount >= this.verb.verbProps.burstShotCount)
                {
                    Log.Warning(this.parent.LabelBaseCap + " burst fire shot count is same or higher than auto fire");
                }
                else
                {
                    this.availableFireModes.Add(FireMode.BurstFire);
                }
            }
            if (!this.props.noSingleShot)
            {
                this.availableFireModes.Add(FireMode.SingleFire);
            }

            // Sanity check in case def changed
            if (!this.availableFireModes.Contains(this.currentFireModeInt))
            {
                this.ResetModes();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue(ref currentFireModeInt, "currentFireMode", FireMode.AutoFire);
            Scribe_Values.LookValue(ref currentAimModeInt, "currentAimMode", AimMode.Snapshot);
        }

        /// <summary>
        /// Cycles through all available fire modes in order
        /// </summary>
        public void ToggleFireMode()
        {
            int currentFireModeNum = this.availableFireModes.IndexOf(this.currentFireModeInt);
            currentFireModeNum = (currentFireModeNum + 1) % this.availableFireModes.Count;
            this.currentFireModeInt = this.availableFireModes.ElementAt(currentFireModeNum);
        }

        public void ToggleAimMode()
        {
            int currentAimModeNum = this.availableAimModes.IndexOf(this.currentAimModeInt);
            currentAimModeNum = (currentAimModeNum + 1) % this.availableAimModes.Count;
            this.currentAimModeInt = this.availableAimModes.ElementAt(currentAimModeNum);
        }

        /// <summary>
        /// Resets the selected fire mode to the first one available (e.g. when the gun is dropped)
        /// </summary>
        public void ResetModes()
        {
            this.currentFireModeInt = this.availableFireModes.ElementAt(0);
            this.currentAimModeInt = this.availableAimModes.ElementAt(0);
        }

        public override string GetDescriptionPart()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (this.availableFireModes.Count > 0)
            {
                stringBuilder.AppendLine("Fire modes: ");
                foreach (FireMode fireMode in this.availableFireModes)
                {
                    stringBuilder.AppendLine("   -" + ("CR_" + fireMode.ToString() + "Label").Translate());
                }
                if (this.props.aimedBurstShotCount > 0 && this.availableFireModes.Contains(FireMode.BurstFire))
                {
                    stringBuilder.AppendLine("Burst shot count: " + GenText.ToStringByStyle(this.props.aimedBurstShotCount, ToStringStyle.Integer));
                }
            }
            return stringBuilder.ToString();
        }
	}
}
