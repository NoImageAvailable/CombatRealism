using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace Combat_Realism
{
    public abstract class ProjectileCR : ThingWithComps
    {
        protected Vector3 origin;
        protected Vector3 destination;
        protected Thing assignedTarget;
        public bool canFreeIntercept;
        protected ThingDef equipmentDef;
        protected Thing launcher;
        private Thing assignedMissTargetInt;
        protected bool landed;
        protected int ticksToImpact;
        private Sustainer ambientSustainer;
        private static List<IntVec3> checkedCells = new List<IntVec3>();
        
        public Thing AssignedMissTarget
        {
            get
            {
                return this.assignedMissTargetInt;
            }
            set
            {
                if (value.def.Fillage == FillCategory.Full)
                {
                    return;
                }
                this.assignedMissTargetInt = value;
            }
        }
        protected int StartingTicksToImpact
        {
            get
            {
            	int num = Mathf.RoundToInt((float)((this.origin - this.destination).magnitude / (Math.Cos(this.shotAngle) * this.shotSpeed / 100f)));
                if (num < 1)
                {
                    num = 1;
                }
                return num;
            }
        }
        protected IntVec3 DestinationCell
        {
            get
            {
                return new IntVec3(this.destination);
            }
        }
        public virtual Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                return this.origin + b + Vector3.up * this.def.Altitude;
            }
        }
        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.destination - this.origin);
            }
        }
        public override Vector3 DrawPos
        {
            get
            {
                return this.ExactPosition;
            }
        }

        //New variables
        private const float treeCollisionChance = 0.5f; //Tree collision chance is multiplied by this factor
        public float shotAngle = 0f;
        public float shotHeight = 0f;
        public float shotSpeed = -1f;
        private float distanceFromOrigin
        {
        	get
        	{
                Vector3 currentPos = Vector3.Scale(this.ExactPosition, new Vector3(1,0,1));
                return (float)((currentPos - this.origin).magnitude);
        	}
        }

        
        /*
         * *** End of class variables ***
        */

        //Keep track of new variables
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving && this.launcher != null && this.launcher.Destroyed)
            {
                this.launcher = null;
            }
            Scribe_Values.LookValue<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.LookValue<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_References.LookReference<Thing>(ref this.assignedTarget, "assignedTarget");
            Scribe_Values.LookValue<bool>(ref this.canFreeIntercept, "canFreeIntercept", false, false);
            Scribe_Defs.LookDef<ThingDef>(ref this.equipmentDef, "equipmentDef");
            Scribe_References.LookReference<Thing>(ref this.launcher, "launcher");
            Scribe_References.LookReference<Thing>(ref this.assignedMissTargetInt, "assignedMissTarget");
            Scribe_Values.LookValue<bool>(ref this.landed, "landed", false, false);
            Scribe_Values.LookValue<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);

            //Here be new variables
            Scribe_Values.LookValue<float>(ref this.shotAngle, "shotAngle", 0f, true);
            Scribe_Values.LookValue<float>(ref this.shotAngle, "shotHeight", 0f, true);
            Scribe_Values.LookValue<float>(ref this.shotSpeed, "shotSpeed", 0f, true);
        }

        public float GetProjectileHeight(float zeroheight, float distance, float angle, float velocity)
        {
            const float gravity = Utility.gravityConst;
			float height = (float)(zeroheight + ((distance * Math.Tan(angle)) - (gravity * Math.Pow(distance, 2)) / (2 * Math.Pow(velocity * Math.Cos(angle), 2))));

        	return height;
        }
        
        //Added new calculations for downed pawns, destination
        public virtual void Launch(Thing launcher, Vector3 origin, TargetInfo targ, Thing equipment = null)
        {
            if (this.shotSpeed < 0)
            {
                this.shotSpeed = this.def.projectile.speed;
            }
            this.launcher = launcher;
            this.origin = origin;
            if (equipment != null)
            {
                this.equipmentDef = equipment.def;
            }
            else
            {
                this.equipmentDef = null;
            }
            //Checking if target was downed on launch
            if (targ.Thing != null)
            {
                this.assignedTarget = targ.Thing;
            }
            //Checking if a new destination was set
            if (this.destination == null)
            {
                this.destination = targ.Cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            }

            this.ticksToImpact = this.StartingTicksToImpact;
            if (!this.def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo info = SoundInfo.InWorld(this, MaintenanceType.PerTick);
                this.ambientSustainer = this.def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }

        //Added new method, takes Vector3 destination as argument
        public void Launch(Thing launcher, Vector3 origin, TargetInfo targ, Vector3 target, Thing equipment = null)
        {
            this.destination = target;
            Launch(launcher, origin, targ, equipment);
        }

        //Removed minimum collision distance
        private bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
        {
            IntVec3 lastPos = lastExactPos.ToIntVec3();
            IntVec3 newPos = newExactPos.ToIntVec3();
            if (newPos == lastPos)
            {
                return false;
            }
            if (!lastPos.InBounds() || !newPos.InBounds())
            {
                return false;
            }
            if ((newPos - lastPos).LengthManhattan == 1)
            {
                return this.CheckForFreeIntercept(newPos);
            }
            //Check for minimum collision distance
            float distToTarget = this.assignedTarget != null ? (this.assignedTarget.DrawPos - this.origin).MagnitudeHorizontal() : (this.destination - this.origin).MagnitudeHorizontal();
            if (this.def.projectile.alwaysFreeIntercept 
                || distToTarget <= 1f ? this.origin.ToIntVec3().DistanceToSquared(newPos) > 1f : this.origin.ToIntVec3().DistanceToSquared(newPos) > Mathf.Min(12f, distToTarget / 2))
            {

                Vector3 currentExactPos = lastExactPos;
                Vector3 flightVec = newExactPos - lastExactPos;
                Vector3 sectionVec = flightVec.normalized * 0.2f;
                int numSections = (int)(flightVec.MagnitudeHorizontal() / 0.2f);
                ProjectileCR.checkedCells.Clear();
                int currentSection = 0;
                while (true)
                {
                    currentExactPos += sectionVec;
                    IntVec3 intVec3 = currentExactPos.ToIntVec3();
                    if (!ProjectileCR.checkedCells.Contains(intVec3))
                    {
                        if (this.CheckForFreeIntercept(intVec3))
                        {
                            break;
                        }
                        ProjectileCR.checkedCells.Add(intVec3);
                    }
                    currentSection++;
                    if (currentSection > numSections)
                    {
                        return false;
                    }
                    if (intVec3 == newPos)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        //Added collision detection for cover objects, changed pawn collateral chances
        private bool CheckForFreeIntercept(IntVec3 cell)
        {
            //Check for minimum collision distance
            float distFromOrigin = (cell.ToVector3Shifted() - this.origin).MagnitudeHorizontal();
            float distToTarget = this.assignedTarget != null ? (this.assignedTarget.DrawPos - this.origin).MagnitudeHorizontal() : (this.destination - this.origin).MagnitudeHorizontal();
            if (!this.def.projectile.alwaysFreeIntercept 
                && distToTarget <= 1f ? distFromOrigin < 1f : distFromOrigin < Mathf.Min(12f, distToTarget / 2))
            {
                return false;
            }
            List<Thing> mainThingList = new List<Thing>(Find.ThingGrid.ThingsListAt(cell));

            //Find pawns in adjacent cells and append them to main list
            List<IntVec3> adjList = new List<IntVec3>();
            Vector3 shotVec = (this.destination - this.origin).normalized;

            //Check if bullet is going north-south or west-east
            if (Math.Abs(shotVec.x) < Math.Abs(shotVec.z))
            {
                adjList = GenAdj.CellsAdjacentCardinal(cell, this.Rotation, new IntVec2(0,1)).ToList<IntVec3>();
            }
            else
            {
                adjList = GenAdj.CellsAdjacentCardinal(cell, this.Rotation, new IntVec2(1, 0)).ToList<IntVec3>();
            }

            //Iterate through adjacent cells and find all the pawns
            for (int i = 0; i < adjList.Count; i++)
            {
                if (adjList[i].InBounds() && !adjList[i].Equals(cell))
                {
                    List<Thing> thingList = new List<Thing>(Find.ThingGrid.ThingsListAt(adjList[i]));
                    var pawns = thingList.Where(thing => thing.def.category == ThingCategory.Pawn && !mainThingList.Contains(thing)).ToList();
                    mainThingList.AddRange(pawns);
                }
            }

            //Check for entries first so we avoid doing costly height calculations
            if (mainThingList.Count > 0)
            {
                float height = GetProjectileHeight(this.shotHeight, this.distanceFromOrigin, this.shotAngle, this.shotSpeed);
                for (int i = 0; i < mainThingList.Count; i++)
                {
                    Thing thing = mainThingList[i];
                    if (thing.def.Fillage == FillCategory.Full)	//ignore height
                    {
                        this.Impact(thing);
                        return true;
                    }
                    //Check for trees		--		HARDCODED RNG IN HERE
                    if (thing.def.category == ThingCategory.Plant && thing.def.altitudeLayer == AltitudeLayer.BuildingTall && Rand.Value < thing.def.fillPercent * Mathf.Clamp(distFromOrigin / 40, 0f, (1f / treeCollisionChance)) * treeCollisionChance)
                    {
                        this.Impact(thing);
                        return true;
                    }
                    //Checking for pawns/cover
                    else if (thing.def.category == ThingCategory.Pawn || (this.ticksToImpact < this.StartingTicksToImpact / 2 && thing.def.fillPercent > 0)) //Need to check for fillPercent here or else will be impacting things like motes, etc.
                    {
                        return this.ImpactThroughBodySize(thing, height);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Takes into account the target being downed and the projectile having been fired while the target was downed, and the target's bodySize
        /// </summary>
        private bool ImpactThroughBodySize(Thing thing, float height)
        {
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                //Add suppression
                CompSuppressable compSuppressable = pawn.TryGetComp<CompSuppressable>();
                if (compSuppressable != null)
                {
                    float suppressionAmount = this.def.projectile.damageAmountBase;
                    ProjectilePropertiesCR propsCR = def.projectile as ProjectilePropertiesCR;
                    float penetrationAmount = propsCR == null ? 0f : propsCR.armorPenetration;
                    suppressionAmount *= 1 - Mathf.Clamp(compSuppressable.parentArmor - penetrationAmount, 0, 1);
                    compSuppressable.AddSuppression(suppressionAmount, this.origin.ToIntVec3());
                }

                //Check horizontal distance
                Vector3 dest = this.destination;
                Vector3 orig = this.origin;
                Vector3 pawnPos = pawn.DrawPos;
                float closestDistToPawn = Math.Abs((dest.z - orig.z) * pawnPos.x - (dest.x - orig.x) * pawnPos.z + dest.x * orig.z - dest.z * orig.x)
                    / (float)Math.Sqrt((dest.z - orig.z) * (dest.z - orig.z) + (dest.x - orig.x) * (dest.x - orig.x));
                if (closestDistToPawn <= Utility.GetCollisionWidth(pawn))
                {
                    //Check vertical distance
                    float pawnHeight = Utility.GetCollisionHeight(pawn);
                    if (height < pawnHeight)
                    {
                        this.Impact(thing);
                        return true;
                    }
                }
            }
            if (thing.def.fillPercent > 0 || thing.def.Fillage == FillCategory.Full)
            {
                if (height < Utility.GetCollisionHeight(thing) || thing.def.Fillage == FillCategory.Full)
            	{
            		this.Impact(thing);
            		return true;
            	}
            }
            return false;
        }

        //Modified collision with downed pawns
        private void ImpactSomething()
        {
            //Not modified, just mortar code
            if (this.def.projectile.flyOverhead)
            {
                RoofDef roofDef = Find.RoofGrid.RoofAt(base.Position);
                if (roofDef != null && roofDef.isThickRoof)
                {
                    this.def.projectile.soundHitThickRoof.PlayOneShot(base.Position);
                    this.Destroy(DestroyMode.Vanish);
                    return;
                }
            }
            
            //Modified
            if (this.assignedTarget != null && this.assignedTarget.Position == this.Position)	//it was aimed at something and that something is still there
            {
                this.ImpactThroughBodySize(this.assignedTarget, GetProjectileHeight(this.shotHeight, this.distanceFromOrigin, this.shotAngle, this.shotSpeed));
                return;
            }
            else
            {
                Thing thing = Find.ThingGrid.ThingAt(base.Position, ThingCategory.Pawn);
                if (thing != null)
                {
                    this.ImpactThroughBodySize(thing, GetProjectileHeight(this.shotHeight, this.distanceFromOrigin, this.shotAngle, this.shotSpeed));
                    return;
                }
                List<Thing> list = Find.ThingGrid.ThingsListAt(base.Position);
                float height = (list.Count > 0) ? GetProjectileHeight(this.shotHeight, this.distanceFromOrigin, this.shotAngle, this.shotSpeed) : 0;
                if (height > 0) {
	                for (int i = 0; i < list.Count; i++)
	                {
	                    Thing thing2 = list[i];
	                    bool impacted = this.ImpactThroughBodySize(thing2, height);
	                    if (impacted)
	                    	return;
	                }
                }
                this.Impact(null);
                return;
            }
        }

        //Unmodified
        public void Launch(Thing launcher, TargetInfo targ, Thing equipment = null)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, null);
        }

        //Unmodified
        public override void Tick()
        {
            base.Tick();
            if (this.landed)
            {
                return;
            }
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            if (!this.ExactPosition.InBounds())
            {
                this.ticksToImpact++;
                base.Position = this.ExactPosition.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            Vector3 exactPosition2 = this.ExactPosition;
            if (!this.def.projectile.flyOverhead && this.canFreeIntercept && this.CheckForFreeInterceptBetween(exactPosition, exactPosition2))
            {
                return;
            }
            base.Position = this.ExactPosition.ToIntVec3();
            if ((float)this.ticksToImpact == 60f && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && this.def.projectile.soundImpactAnticipate != null)
            {
                this.def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (this.ticksToImpact <= 0)
            {
                if (this.DestinationCell.InBounds())
                {
                    base.Position = this.DestinationCell;
                }
                this.ImpactSomething();
                return;
            }
            if (this.ambientSustainer != null)
            {
                this.ambientSustainer.Maintain();
            }
        }

        //Unmodified
        public override void Draw()
        {
            Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.def.DrawMatSingle, 0);
            base.Comps_PostDraw();
        }

        //Unmodified
        protected virtual void Impact(Thing hitThing)
        {
            CompExplosiveCR comp = this.TryGetComp<CompExplosiveCR>();
            if (comp != null)
            {
                comp.Explode(this.launcher);
            }
            this.Destroy(DestroyMode.Vanish);
        }

        //Unmodified
        public void ForceInstantImpact()
        {
            if (!this.DestinationCell.InBounds())
            {
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            this.ticksToImpact = 0;
            base.Position = this.DestinationCell;
            this.ImpactSomething();
        }
    }
}
