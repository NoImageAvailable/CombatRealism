using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class CompProperties_Jamming : CompProperties
    {
        public float baseMalfunctionChance = 0f;
        public bool canExplode = false;
        public float explosionDamage = 0f;
        public float explosionRadius = 1f;
        public SoundDef explosionSound = null;

        public CompProperties_Jamming()
        {
            this.compClass = typeof(CompProperties_Jamming);
        }
    }

    class CompJamming : ThingComp
    {
        new public CompProperties_Jamming props;

        private Verb verbInt = null;
        private Verb verb
        {
            get
            {
                if (verbInt == null)
                {
                    CompEquippable compEquippable = this.parent.TryGetComp<CompEquippable>();
                    if (compEquippable != null)
                    {
                        this.verbInt = compEquippable.PrimaryVerb;
                    }
                    else
                    {
                        Log.ErrorOnce(this.parent.LabelCap + " has CompJamming but no CompEquippable.", 50010);
                    }
                }
                return this.verbInt;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompProperties_Jamming cprops = props as CompProperties_Jamming;
            if (cprops != null)
            {
                this.props = cprops;
            }
        }

        /// <summary>
        /// Returns a factor to scale malfunction chance by quality. If the parent doesn't have a CompQuality it will return a factor of 1.
        /// </summary>
        /// <returns>Quality-based scale factor</returns>
        private float GetQualityFactor()
        {
            CompQuality compQuality = this.parent.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                switch (compQuality.Quality)
                {
                    case QualityCategory.Awful:
                        return 4f;
                    case QualityCategory.Shoddy:
                        return 3f;
                    case QualityCategory.Poor:
                        return 2f;
                    case QualityCategory.Normal:
                        return 1f;
                    case QualityCategory.Good:
                        return 0.85f;
                    case QualityCategory.Excellent:
                        return 0.7f;
                    case QualityCategory.Superior:
                        return 0.55f;
                    case QualityCategory.Masterwork:
                        return 0.4f;
                    case QualityCategory.Legendary:
                        return 0.25f;
                }
            }
            return 1f;
        }

        public void DoMalfunction()
        {
            float jamChance = this.props.baseMalfunctionChance * (1 - this.parent.HitPoints / this.parent.MaxHitPoints) * this.GetQualityFactor();
            float explodeChance = Mathf.Clamp01(jamChance);

            if (this.props.canExplode && UnityEngine.Random.value < explodeChance)
            {
                this.Explode();
            }
            if (UnityEngine.Random.value < jamChance)
            {
                //TODO
            }
        }

        /// <summary>
        /// Causes explosion and destroys parent equipment
        /// </summary>
        private void Explode()
        {
            if (!this.parent.Destroyed)
            {
                this.parent.Destroy(DestroyMode.Vanish);
            }
            BodyPartDamageInfo value = new BodyPartDamageInfo(null, new BodyPartDepth?(BodyPartDepth.Outside));
            ExplosionInfo explosionInfo = default(ExplosionInfo);
            explosionInfo.center = this.parent.Position;
            explosionInfo.radius = this.props.explosionRadius;
            explosionInfo.dinfo = new DamageInfo(DamageDefOf.Bomb, 999, this.parent, new BodyPartDamageInfo?(value), null);
            explosionInfo.explosionSound = this.props.explosionSound;
            explosionInfo.DoExplosion();
        }
    }
}
