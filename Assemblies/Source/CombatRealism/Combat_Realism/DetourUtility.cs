using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism
{
    public static class DetourUtility
    {
        public static float CalculateBleedingRateCR(this HediffSet _this)
        {
            if (!_this.pawn.RaceProps.isFlesh || _this.pawn.health.Dead)
            {
                return 0f;
            }
            float bleedAmount = 0f;
            for (int i = 0; i < _this.hediffs.Count; i++)
            {
                float hediffBleedRate = _this.hediffs[i].BleedRate;
                if (_this.hediffs[i].Part != null)
                {
                    hediffBleedRate *= _this.hediffs[i].Part.def.bleedingRateMultiplier;
                }
                bleedAmount += hediffBleedRate;
            }
            float value = 0.0142857144f * bleedAmount * 2 / _this.pawn.HealthScale;
            return Mathf.Max(0, value);
        }

        private static FieldInfo parentTurretFieldInfo = typeof(TurretTop).GetField("parentTurret", BindingFlags.Instance | BindingFlags.NonPublic);
        private static PropertyInfo curRotationPropertyInfo = typeof(TurretTop).GetProperty("CurRotation", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void DrawTurretCR(this TurretTop _this)
        {
            Matrix4x4 matrix = default(Matrix4x4);
            Vector3 vec = new Vector3(1, 1, 1);
            Building_Turret parentTurret = (Building_Turret)parentTurretFieldInfo.GetValue(_this);
            float curRotation = (float)curRotationPropertyInfo.GetValue(_this, null);
            Material topMat = parentTurret.def.building.turretTopMat;
            if (topMat.mainTexture.height >= 256 || topMat.mainTexture.width >= 256)
            {
                vec.x = 2;
                vec.z = 2;
            }
            matrix.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, curRotation.ToQuat(), vec);
            Graphics.DrawMesh(MeshPool.plane20, matrix, parentTurret.def.building.turretTopMat, 0);
        }

        public static List<FloatMenuOption> ChoicesAtForCR(Vector3 clickPos, Pawn pawn)
        {
            IntVec3 intVec = IntVec3.FromVector3(clickPos);
            DangerUtility.NotifyDirectOrderingThisFrame(pawn);
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            if (!intVec.InBounds())
            {
                return list;
            }

            // *** Drafted options ***
            if (pawn.Drafted)
            {
                foreach (TargetInfo current in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttack(pawn), true))
                {
                    // Fire at target option
                    if (pawn.equipment.Primary != null && !pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.MeleeRange)
                    {
                        FloatMenuOption floatMenuOption = new FloatMenuOption();
                        floatMenuOption.priority = MenuOptionPriority.High;
                        if (!pawn.equipment.PrimaryEq.PrimaryVerb.CanHitTarget(current))
                        {
                            if (!pawn.Position.InHorDistOf(current.Cell, pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range))
                            {
                                floatMenuOption.label = "FireAt".Translate(new object[]
						{
							current.Thing.LabelCap
						}) + " (" + "OutOfRange".Translate() + ")";
                            }
                            else
                            {
                                floatMenuOption.label = "FireAt".Translate(new object[]
						{
							current.Thing.LabelCap
						}) + " (" + "CannotHitTarget".Translate() + ")";
                            }
                        }
                        else if (pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                        {
                            floatMenuOption.label = "FireAt".Translate(new object[]
					{
						current.Thing.LabelCap
					}) + " (" + "CannotFight".Translate(new object[]
					{
						pawn.LabelCap
					}) + ")";
                        }
                        else
                        {
                            floatMenuOption.label = "FireAt".Translate(new object[]
					{
						current.Thing.LabelCap
					});
                            floatMenuOption.autoTakeable = true;
                            floatMenuOption.action = new Action(delegate
                            {

                                Job job = new Job(JobDefOf.AttackStatic, current);
                                job.playerForced = true;
                                pawn.drafter.TakeOrderedJob(job);
                                MoteThrower.ThrowStatic(current.Thing.DrawPos, ThingDefOf.Mote_FeedbackAttack, 1f);
                            });
                        }
                        list.Add(floatMenuOption);
                    }

                    // Melee attack option
                    String choiceLabel = "MeleeAttack".Translate() + current.Thing.LabelCap;
                    Action action;
                    if (!pawn.CanReach(current, PathEndMode.Touch, Danger.Deadly, false))
                    {
                        choiceLabel = choiceLabel + " (" + "NoPath".Translate() + ")";
                        action = null;
                    }
                    else if (pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                    {
                        choiceLabel = choiceLabel + " (" + "CannotFight".Translate(new object[]
				{
					pawn.LabelCap
				}) + ")";
                        action = null;
                    }
                    else if (pawn.meleeVerbs.TryGetMeleeVerb() == null)
                    {
                        choiceLabel = choiceLabel + " (" + "Incapable".Translate() + ")";
                        action = null;
                    }
                    else
                    {
                        action = new Action(delegate
                        {
                            Job job = new Job(JobDefOf.AttackMelee, current);
                            job.playerForced = true;
                            Pawn tPawn = current.Thing as Pawn;
                            if (tPawn != null)
                            {
                                job.killIncappedTarget = pawn.Downed;
                                choiceLabel = "MeleeAttackToDeath".Translate(new object[]
                            {
                                current.Thing.LabelCap
                            });
                            }
                            pawn.drafter.TakeOrderedJob(job);
                            MoteThrower.ThrowStatic(current.Thing.DrawPos, ThingDefOf.Mote_FeedbackAttack, 1f);
                        });
                    }
                    List<FloatMenuOption> arg_3FE_0 = list;
                    Thing thing = current.Thing;
                    arg_3FE_0.Add(new FloatMenuOption(choiceLabel, action, MenuOptionPriority.High, null, thing));
                }

                // Arrest option
                if (pawn.RaceProps.Humanlike)
                {
                    foreach (TargetInfo current2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), true))
                    {
                        TargetInfo dest = current2;
                        if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly, false))
                        {
                            list.Add(new FloatMenuOption("CannotArrest".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null));
                        }
                        else if (!pawn.CanReserve(dest.Thing, 1))
                        {
                            list.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "Reserved".Translate(), null, MenuOptionPriority.Medium, null, null));
                        }
                        else
                        {
                            Pawn pTarg = (Pawn)dest.Thing;
                            Action action2 = new Action(delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(pTarg, pawn, true, false, false);
                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Arrest, pTarg, building_Bed);
                                job.playerForced = true;
                                job.maxNumToCarry = 1;
                                pawn.drafter.TakeOrderedJob(job);
                                TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies);
                            });
                            List<FloatMenuOption> arg_563_0 = list;
                            Thing thing = dest.Thing;
                            arg_563_0.Add(new FloatMenuOption("TryToArrest".Translate(new object[]
					{
						dest.Thing.LabelCap
					}), action2, MenuOptionPriority.Medium, null, thing));
                        }
                    }
                }

                // GoTo option
                int num = GenRadial.NumCellsInRadius(2.9f);
                for (int i = 0; i < num; i++)
                {
                    IntVec3 curLoc = GenRadial.RadialPattern[i] + intVec;
                    if (curLoc.Standable())
                    {
                        if (curLoc != pawn.Position)
                        {
                            if (!pawn.CanReach(curLoc, PathEndMode.OnCell, Danger.Deadly, false))
                            {
                                FloatMenuOption item = new FloatMenuOption("CannotGoNoPath".Translate(), null, MenuOptionPriority.Low, null, null);
                                list.Add(item);
                            }
                            else
                            {
                                Action action3 = new Action(delegate
                                {
                                    IntVec3 bestDest = Pawn_DraftController.BestGotoDestNear(curLoc, pawn);
                                    Job job = new Job(JobDefOf.Goto, bestDest);
                                    job.playerForced = true;
                                    pawn.drafter.TakeOrderedJob(job);
                                    MoteThrower.ThrowStatic(bestDest, ThingDefOf.Mote_FeedbackGoto, 1f);
                                });
                                list.Add(new FloatMenuOption("GoHere".Translate(), action3, MenuOptionPriority.Low, null, null)
                                {
                                    autoTakeable = true
                                });
                            }
                        }
                        break;
                    }
                }
            }
            // *** End of drafted options ***

            // *** Humanlike options ***
            if (pawn.RaceProps.Humanlike)
            {
                int num2 = 0;
                if (pawn.story != null)
                {
                    num2 = pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire);
                }

                // Consume option
                foreach (Thing current3 in intVec.GetThingList())
                {
                    if (current3.def.ingestible != null && pawn.RaceProps.CanEverEat(current3))
                    {
                        FloatMenuOption item2;
                        if (current3.def.ingestible.isPleasureDrug && num2 < 0)
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						current3.LabelBaseShort
					}) + " (" + "Teetotaler".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReach(current3, PathEndMode.OnCell, Danger.Deadly, false))
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						current3.LabelBaseShort
					}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReserve(current3, 1))
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						current3.LabelBaseShort
					}) + " (" + "ReservedBy".Translate(new object[]
					{
						Find.Reservations.FirstReserverOf(current3, pawn.Faction)
					}) + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						current3.LabelBaseShort
					}), new Action(delegate
                    {
                        current3.SetForbidden(false, true);
                        Job job = new Job(JobDefOf.Ingest, current3);
                        job.maxNumToCarry = current3.def.ingestible.maxNumToIngestAtOnce;
                        pawn.drafter.TakeOrderedJob(job);
                    }
                ), MenuOptionPriority.Medium, null, null);
                        }
                        list.Add(item2);
                    }
                }

                // Rescue/Capture downed option
                foreach (TargetInfo current4 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    Pawn victim = (Pawn)current4.Thing;
                    if (!victim.InBed() && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1) && !victim.IsPrisonerOfColony)
                    {
                        if ((victim.Faction == Faction.OfColony && victim.BrokenStateDef == null) || (victim.Faction != Faction.OfColony && victim.BrokenStateDef == null && !victim.IsPrisonerOfColony && (victim.Faction == null || !victim.Faction.HostileTo(Faction.OfColony))))
                        {
                            List<FloatMenuOption> arg_A3B_0 = list;
                            arg_A3B_0.Add(new FloatMenuOption("Rescue".Translate(new object[]
					{
						victim.LabelCap
					}), new Action(delegate
                    {
                        Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                        if (building_Bed == null)
                        {
                            Messages.Message("CannotRescue".Translate() + ": " + "NoNonPrisonerBed".Translate(), MessageSound.RejectInput);
                            return;
                        }
                        Job job = new Job(JobDefOf.Rescue, victim, building_Bed);
                        job.maxNumToCarry = 1;
                        job.playerForced = true;
                        pawn.drafter.TakeOrderedJob(job);
                        ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                    }),
                                MenuOptionPriority.Medium, null, victim));
                        }
                        if (victim.BrokenStateDef != null || (victim.RaceProps.Humanlike && victim.Faction != Faction.OfColony))
                        {
                            List<FloatMenuOption> arg_ABC_0 = list;
                            arg_ABC_0.Add(new FloatMenuOption("Capture".Translate(new object[]
					{
						victim.LabelCap
					}), new Action(delegate
                    {
                        Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                        if (building_Bed == null)
                        {
                            Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), MessageSound.RejectInput);
                            return;
                        }
                        Job job = new Job(JobDefOf.Capture, victim, building_Bed);
                        job.maxNumToCarry = 1;
                        job.playerForced = true;
                        pawn.drafter.TakeOrderedJob(job);
                        ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                    }),
                                MenuOptionPriority.Medium, null, victim));
                        }
                    }
                }

                // Carry to cryosleep option
                foreach (TargetInfo current5 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    TargetInfo targetInfo = current5;
                    Pawn victim = (Pawn)targetInfo.Thing;
                    if (victim.Downed && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1) && Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn) != null)
                    {
                        string label = "CarryToCryptosleepCasket".Translate(new object[]
				{
					targetInfo.Thing.LabelCap
				});
                        JobDef jDef = JobDefOf.CarryToCryptosleepCasket;
                        Action action4 = new Action(delegate
                        {
                            Building_CryptosleepCasket building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn);
                            if (building_CryptosleepCasket == null)
                            {
                                Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), MessageSound.RejectInput);
                                return;
                            }
                            Job job = new Job(jDef, victim, building_CryptosleepCasket);
                            job.maxNumToCarry = 1;
                            job.playerForced = true;
                            pawn.drafter.TakeOrderedJob(job);
                        });
                        List<FloatMenuOption> arg_BDA_0 = list;
                        arg_BDA_0.Add(new FloatMenuOption(label, action4, MenuOptionPriority.Medium, null, victim));
                    }
                }

                // Strip option
                foreach (TargetInfo current6 in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), true))
                {
                    TargetInfo stripTarg = current6;
                    FloatMenuOption item3;
                    if (!pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly, false))
                    {
                        item3 = new FloatMenuOption("CannotStrip".Translate(new object[]
				{
					stripTarg.Thing.LabelCap
				}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                    }
                    else if (!pawn.CanReserveAndReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly, 1))
                    {
                        item3 = new FloatMenuOption("CannotStrip".Translate(new object[]
				{
					stripTarg.Thing.LabelCap
				}) + " (" + "ReservedBy".Translate(new object[]
				{
					Find.Reservations.FirstReserverOf(stripTarg, pawn.Faction)
				}) + ")", null, MenuOptionPriority.Medium, null, null);
                    }
                    else
                    {
                        item3 = new FloatMenuOption("Strip".Translate(new object[]
				{
					stripTarg.Thing.LabelCap
				}), new Action(delegate
                {
                    stripTarg.Thing.SetForbidden(false, false);
                    Job job = new Job(JobDefOf.Strip, stripTarg);
                    job.playerForced = true;
                    pawn.drafter.TakeOrderedJob(job);
                }),
                            MenuOptionPriority.Medium, null, null);
                    }
                    list.Add(item3);
                }

                // Equip option
                if (pawn.equipment != null)
                {
                    ThingWithComps equipment = null;
                    List<Thing> thingList = intVec.GetThingList();
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j].TryGetComp<CompEquippable>() != null)
                        {
                            equipment = (ThingWithComps)thingList[j];
                            break;
                        }
                    }
                    if (equipment != null)
                    {
                        string text = GenLabel.ThingLabel(equipment.def, equipment.Stuff, 1);
                        FloatMenuOption equipOption;
                        if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false))
                        {
                            equipOption = new FloatMenuOption("CannotEquip".Translate(new object[]
					{
						text
					}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReserve(equipment, 1))
                        {
                            equipOption = new FloatMenuOption("CannotEquip".Translate(new object[]
					{
						text
					}) + " (" + "ReservedBy".Translate(new object[]
					{
						Find.Reservations.FirstReserverOf(equipment, pawn.Faction)
					}) + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        {
                            equipOption = new FloatMenuOption("CannotEquip".Translate(new object[]
					{
						text
					}) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else
                        {
                            // Added check for inventory space here
                            CompInventory comp = pawn.TryGetComp<CompInventory>();
                            int count;
                            if (comp != null && !comp.CanFitInInventory(equipment, out count, false))
                            {
                                equipOption = new FloatMenuOption("CannotEquip".Translate(new object[] { text }) + " (Inventory is full)", null);
                            }
                            else
                            {
                                string text2 = "Equip".Translate(new object[] { text });
                                if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                                {
                                    text2 = text2 + " " + "EquipWarningBrawler".Translate();
                                }
                                equipOption = new FloatMenuOption(text2, new Action(delegate
                                {
                                    equipment.SetForbidden(false, true);
                                    Job job = new Job(JobDefOf.Equip, equipment);
                                    job.playerForced = true;
                                    pawn.drafter.TakeOrderedJob(job);
                                    MoteThrower.ThrowStatic(equipment.DrawPos, ThingDefOf.Mote_FeedbackEquip, 1f);
                                    ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                                }),
                                    MenuOptionPriority.Medium, null, null);
                            }
                        }
                        list.Add(equipOption);
                    }
                }

                // Wear option
                if (pawn.apparel != null)
                {
                    Apparel apparel = Find.ThingGrid.ThingAt<Apparel>(intVec);
                    if (apparel != null)
                    {
                        FloatMenuOption item5;
                        if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly, false))
                        {
                            item5 = new FloatMenuOption("CannotWear".Translate(new object[]
					{
						apparel.Label
					}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReserve(apparel, 1))
                        {
                            Pawn pawn2 = Find.Reservations.FirstReserverOf(apparel, pawn.Faction);
                            item5 = new FloatMenuOption("CannotWear".Translate(new object[]
					{
						apparel.Label
					}) + " (" + "ReservedBy".Translate(new object[]
					{
						pawn2
					}) + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else
                        {
                            item5 = new FloatMenuOption("ForceWear".Translate(new object[]
					{
						apparel.LabelBaseShort
					}), new Action(delegate
                    {
                        apparel.SetForbidden(false, true);
                        Job job = new Job(JobDefOf.Wear, apparel);
                        job.playerForced = true;
                        pawn.drafter.TakeOrderedJob(job);
                    }),
                                MenuOptionPriority.Medium, null, null);
                        }
                        list.Add(item5);
                    }
                }

                // Deposit option
                if (pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    Thing thing2 = Find.ThingGrid.ThingAt(intVec, ThingDefOf.EquipmentRack);
                    if (thing2 != null)
                    {
                        if (!pawn.CanReach(thing2, PathEndMode.ClosestTouch, Danger.Deadly, false))
                        {
                            list.Add(new FloatMenuOption("CannotDeposit".Translate(new object[]
					{
						pawn.equipment.Primary.LabelCap,
						thing2.def.label
					}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null));
                        }
                        else
                        {
                            using (IEnumerator<IntVec3> enumerator7 = GenAdj.CellsOccupiedBy(thing2).GetEnumerator())
                            {
                                while (enumerator7.MoveNext())
                                {
                                    IntVec3 c = enumerator7.Current;
                                    if (c.GetStorable() == null && pawn.CanReserveAndReach(c, PathEndMode.ClosestTouch, Danger.Deadly, 1))
                                    {
                                        Action action5 = new Action(delegate
                                        {
                                            ThingWithComps t;
                                            if (pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out t, pawn.Position, true))
                                            {
                                                t.SetForbidden(false, true);
                                                Job job = new Job(JobDefOf.HaulToCell, t, c);
                                                job.haulMode = HaulMode.ToCellStorage;
                                                job.maxNumToCarry = 1;
                                                job.playerForced = true;
                                                pawn.drafter.TakeOrderedJob(job);
                                            }
                                        });
                                        list.Add(new FloatMenuOption("Deposit".Translate(new object[]
								{
									pawn.equipment.Primary.LabelCap,
									thing2.def.label
								}), action5, MenuOptionPriority.Medium, null, null));
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Drop equipment option
                    if (pawn.equipment != null && GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true).Any<TargetInfo>())
                    {
                        Action dropEquipmentAction = new Action(delegate
                        {
                            ThingWithComps thingWithComps;
                            pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.Position, true);
                            pawn.drafter.TakeOrderedJob(new Job(JobDefOf.Wait, 20, false));
                        });
                        Action action6 = new Action(dropEquipmentAction);
                        list.Add(new FloatMenuOption("Drop".Translate(new object[]
				{
					pawn.equipment.Primary.LabelCap
				}), action6, MenuOptionPriority.Medium, null, null));
                    }
                }
                foreach (Thing current7 in Find.ThingGrid.ThingsAt(intVec))
                {
                    foreach (FloatMenuOption current8 in current7.GetFloatMenuOptions(pawn))
                    {
                        list.Add(current8);
                    }
                }
            }
            // *** End of humanlike options ***

            // *** Non-drafted options ***
            if (!pawn.Drafted)
            {
                bool flag = false;
                bool flag2 = false;
                foreach (Thing current9 in Find.ThingGrid.ThingsAt(intVec))
                {
                    flag2 = true;
                    if (pawn.CanReach(current9, PathEndMode.Touch, Danger.Deadly, false))
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag2 && !flag)
                {
                    list.Add(new FloatMenuOption("(" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null));
                    return list;
                }
                foreach (Thing current10 in Find.ThingGrid.ThingsAt(intVec))
                {
                    Pawn pawn3 = Find.Reservations.FirstReserverOf(current10, pawn.Faction);
                    if (pawn3 != null && pawn3 != pawn)
                    {
                        list.Add(new FloatMenuOption("IsReservedBy".Translate(new object[]
				{
					current10.LabelBaseShort.CapitalizeFirst(),
					pawn3.LabelBaseShort
				}), null, MenuOptionPriority.Medium, null, null));
                    }
                    else
                    {
                        JobGiver_Work jobGiver_Work = pawn.thinker.TryGetThinkNode<JobGiver_Work>();
                        if (jobGiver_Work != null)
                        {
                            foreach (WorkTypeDef current11 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                            {
                                for (int k = 0; k < current11.workGiversByPriority.Count; k++)
                                {
                                    WorkGiver_Scanner workGiver_Scanner = current11.workGiversByPriority[k].Worker as WorkGiver_Scanner;
                                    if (workGiver_Scanner != null)
                                    {
                                        if (workGiver_Scanner.def.directOrderable)
                                        {
                                            if (!workGiver_Scanner.ShouldSkip(pawn))
                                            {
                                                JobFailReason.Clear();
                                                Job job;
                                                if (!workGiver_Scanner.HasJobOnThing(pawn, current10))
                                                {
                                                    job = null;
                                                }
                                                else
                                                {
                                                    job = workGiver_Scanner.JobOnThing(pawn, current10);
                                                }
                                                if (workGiver_Scanner.PotentialWorkThingRequest.Accepts(current10) || (workGiver_Scanner.PotentialWorkThingsGlobal(pawn) != null && workGiver_Scanner.PotentialWorkThingsGlobal(pawn).Contains(current10)))
                                                {
                                                    if (job == null)
                                                    {
                                                        if (JobFailReason.HaveReason)
                                                        {
                                                            string label2 = "CannotGenericWork".Translate(new object[]
													{
														workGiver_Scanner.def.verb,
														current10.LabelBaseShort
													}) + " (" + JobFailReason.Reason + ")";
                                                            list.Add(new FloatMenuOption(label2, null, MenuOptionPriority.Medium, null, null));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string label;
                                                        WorkTypeDef workType = workGiver_Scanner.def.workType;
                                                        Action action7 = null;
                                                        PawnCapacityDef pawnCapacityDef = workGiver_Scanner.MissingRequiredCapacity(pawn);
                                                        if (pawnCapacityDef != null)
                                                        {
                                                            label = "CannotMissingHealthActivities".Translate(new object[]
													{
														pawnCapacityDef.label
													});
                                                        }
                                                        else if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(job))
                                                        {
                                                            label = "CannotGenericAlreadyAm".Translate(new object[]
													{
														workType.gerundLabel,
														current10.LabelBaseShort
													});
                                                        }
                                                        else if (pawn.workSettings.GetPriority(workType) == 0)
                                                        {
                                                            label = "CannotPrioritizeIsNotA".Translate(new object[]
													{
														pawn.NameStringShort,
														workType.pawnLabel
													});
                                                        }
                                                        else if (job.def == JobDefOf.Research && current10.def == ThingDefOf.ResearchBench)
                                                        {
                                                            label = "CannotPrioritizeResearch".Translate();
                                                        }
                                                        else if (current10.IsForbidden(pawn))
                                                        {
                                                            label = "CannotPrioritizeForbidden".Translate(new object[]
													{
														current10.Label
													});
                                                        }
                                                        else if (!pawn.CanReach(current10, PathEndMode.Touch, Danger.Deadly, false))
                                                        {
                                                            label = current10.Label + ": " + "NoPath".Translate();
                                                        }
                                                        else
                                                        {
                                                            label = "PrioritizeGeneric".Translate(new object[]
													{
														workGiver_Scanner.def.gerund,
														current10.Label
													});
                                                            Job localJob = job;
                                                            WorkTypeDef localWorkTypeDef = workType;
                                                            action7 = new Action(delegate
                                                            {
                                                                pawn.thinker.GetThinkNode<JobGiver_Work>().TryStartPrioritizedWorkOn(pawn, localJob, localWorkTypeDef);
                                                            });
                                                        }
                                                        if (!list.Any(option => option.label == label))
                                                        {
                                                            list.Add(new FloatMenuOption(label, action7, MenuOptionPriority.Medium, null, null));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // *** End of non-drafted options ***

            foreach (FloatMenuOption current12 in pawn.GetExtraFloatMenuOptionsFor(intVec))
            {
                list.Add(current12);
            }
            DangerUtility.DoneDirectOrdering();
            return list;
        }
    }
}
