using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions are found here
using Verse;         // RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
using Verse.Sound;   // Needed when you do something with the Sound

namespace Combat_Realism
{
    /// <summary>
    /// Explosive with fragmentation effect
    /// </summary>
	public class ProjectileCR_Frag : ProjectileCR_Explosive
	{
        private Thing equipment = null;

        //frag variables
        private int fragAmountSmall = 0;
        private int fragAmountMedium = 0;
        private int fragAmountLarge = 0;

        private float fragRange = 0;

        private ThingDef fragProjectileSmall = null;
        private ThingDef fragProjectileMedium = null;
        private ThingDef fragProjectileLarge = null;

        /// <summary>
        /// Read parameters from XML file
        /// </summary>
        /// <returns>True if parameters are in order, false otherwise</returns>
        public bool getParameters()
        {
            ThingDef_ProjectileFrag projectileDef = this.def as ThingDef_ProjectileFrag;
            if (projectileDef.fragAmountSmall + projectileDef.fragAmountMedium + projectileDef.fragAmountLarge > 0
                && projectileDef.fragRange > 0
                && projectileDef.fragProjectileSmall != null
                && projectileDef.fragProjectileMedium != null
                && projectileDef.fragProjectileLarge != null)
            {
                this.fragAmountSmall = projectileDef.fragAmountSmall;
                this.fragAmountMedium = projectileDef.fragAmountMedium;
                this.fragAmountLarge = projectileDef.fragAmountLarge;

                this.fragRange = projectileDef.fragRange;

                this.fragProjectileSmall = projectileDef.fragProjectileSmall;
                this.fragProjectileMedium = projectileDef.fragProjectileMedium;
                this.fragProjectileLarge = projectileDef.fragProjectileLarge;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Scatters fragments around
        /// </summary>
        protected virtual void ScatterFragments(ThingDef projectileDef)
        {
            ProjectileCR projectile = (ProjectileCR)ThingMaker.MakeThing(projectileDef, null);
            projectile.canFreeIntercept = true;
            /*
            projectile.shotAngle = Random.Range(-3.0f, 0.5f);
            projectile.shotHeight = 0.1f;
             */

            Vector3 exactTarget = this.ExactPosition + (new Vector3(1, 0, 1) * Random.Range(0, this.fragRange)).RotatedBy(Random.Range(0, 360));
            TargetInfo targetCell = exactTarget.ToIntVec3();
            GenSpawn.Spawn(projectile, this.Position);

            projectile.Launch(this, this.ExactPosition, targetCell, exactTarget, equipment);
        }

        /// <summary>
        /// Explode and scatter fragments around
        /// </summary>
		protected override void Explode()
        {
            if (this.getParameters())
            {
                //Spawn projectiles
                for (int i = 0; i < fragAmountSmall; i++)
                {
                    this.ScatterFragments(this.fragProjectileSmall);
                }
                for (int i = 0; i < fragAmountMedium; i++)
                {
                    this.ScatterFragments(this.fragProjectileMedium);
                }
                for (int i = 0; i < fragAmountLarge; i++)
                {
                    this.ScatterFragments(this.fragProjectileLarge);
                }
            }
            base.Explode();
		}

        public override void Launch(Thing launcher, Vector3 origin, TargetInfo targ, Thing equipment = null)
        {
            base.Launch(launcher, origin, targ, equipment);
            this.equipment = equipment;
        }
	}
}
