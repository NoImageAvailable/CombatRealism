using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal class Detours_FloatMenuMaker
    {
        internal static List<FloatMenuOption> ChoicesAtFor(Vector3 clickPos, Pawn pawn)
        {
            IntVec3 clickCell = IntVec3.FromVector3(clickPos);
            DangerUtility.NotifyDirectOrderingThisFrame(pawn);
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            if (!clickCell.InBounds())
            {
                return list;
            }

            // ***** Beginning of drafted options *****

            if (pawn.Drafted)
            {
                foreach (TargetInfo attackTarg in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true))
                {
                    // *** Fire at option ***

                    if (pawn.equipment.Primary != null && !pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.MeleeRange)
                    {
                        string str;
                        Action rangedAct = FloatMenuUtility.GetRangedAttackAction(pawn, attackTarg, out str);
                        string text = "FireAt".Translate(new object[]
				{
					attackTarg.Thing.LabelCap
				});
                        FloatMenuOption floatMenuOption = new FloatMenuOption();
                        floatMenuOption.priority = MenuOptionPriority.High;
                        if (rangedAct == null)
                        {
                            text = text + " (" + str + ")";
                        }
                        else
                        {
                            floatMenuOption.autoTakeable = true;
                            floatMenuOption.action = new Action(delegate
                            {
                                MoteThrower.ThrowStatic(attackTarg.Thing.DrawPos, ThingDefOf.Mote_FeedbackAttack, 1f);
                                rangedAct();
                            });
                        }
                        floatMenuOption.label = text;
                        list.Add(floatMenuOption);
                    }

                    // *** Melee attack option ***

                    string str2;
                    Action meleeAct = FloatMenuUtility.GetMeleeAttackAction(pawn, attackTarg, out str2);
                    Pawn pawn2 = attackTarg.Thing as Pawn;
                    string text2;
                    if (pawn2 != null && pawn2.Downed)
                    {
                        text2 = "MeleeAttackToDeath".Translate(new object[]
				{
					attackTarg.Thing.LabelCap
				});
                    }
                    else
                    {
                        text2 = "MeleeAttack".Translate(new object[]
				{
					attackTarg.Thing.LabelCap
				});
                    }
                    Thing thing = attackTarg.Thing;
                    FloatMenuOption floatMenuOption2 = new FloatMenuOption(string.Empty, null, MenuOptionPriority.High, null, thing);
                    if (meleeAct == null)
                    {
                        text2 = text2 + " (" + str2 + ")";
                    }
                    else
                    {
                        floatMenuOption2.action = new Action(delegate
                        {
                            MoteThrower.ThrowStatic(attackTarg.Thing.DrawPos, ThingDefOf.Mote_FeedbackAttack, 1f);
                            meleeAct();
                        });
                    }
                    floatMenuOption2.label = text2;
                    list.Add(floatMenuOption2);
                }

                // *** Arrest option ***

                if (pawn.RaceProps.Humanlike && !pawn.Downed)
                {
                    foreach (TargetInfo current2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), true))
                    {
                        TargetInfo dest = current2;
                        if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
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
                            Action action = new Action(delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(pTarg, pawn, true, false, false);
                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Arrest, pTarg, building_Bed);
                                job.playerForced = true;
                                job.maxNumToCarry = 1;
                                pawn.drafter.TakeOrderedJob(job);
                                TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies);
                            });
                            List<FloatMenuOption> arg_3F1_0 = list;
                            Thing thing = dest.Thing;
                            arg_3F1_0.Add(new FloatMenuOption("TryToArrest".Translate(new object[]
					{
						dest.Thing.LabelCap
					}), action, MenuOptionPriority.Medium, null, thing));
                        }
                    }
                }

                // *** Goto option ***

                int num = GenRadial.NumCellsInRadius(2.9f);
                for (int i = 0; i < num; i++)
                {
                    IntVec3 curLoc = GenRadial.RadialPattern[i] + clickCell;
                    if (curLoc.Standable())
                    {
                        if (curLoc != pawn.Position)
                        {
                            if (!pawn.CanReach(curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                            {
                                FloatMenuOption item = new FloatMenuOption("CannotGoNoPath".Translate(), null, MenuOptionPriority.Low, null, null);
                                list.Add(item);
                            }
                            else
                            {
                                Action action2 = new Action(delegate
                                {
                                    IntVec3 dest = Pawn_DraftController.BestGotoDestNear(curLoc, pawn);
                                    Job job = new Job(JobDefOf.Goto, dest);
                                    job.playerForced = true;
                                    pawn.drafter.TakeOrderedJob(job);
                                    MoteThrower.ThrowStatic(dest, ThingDefOf.Mote_FeedbackGoto, 1f);
                                });
                                list.Add(new FloatMenuOption("GoHere".Translate(), action2, MenuOptionPriority.Low, null, null)
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

            // *** Beginning of humanlike options ***

            if (pawn.RaceProps.Humanlike)
            {
                int num2 = 0;
                if (pawn.story != null)
                {
                    num2 = pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire);
                }

                // *** Consume option ***

                foreach (Thing current3 in clickCell.GetThingList())
                {
                    Thing t = current3;
                    if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                    {
                        FloatMenuOption item2;
                        if (t.def.ingestible.isPleasureDrug && num2 < 0)
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						t.LabelBaseShort
					}) + " (" + "Teetotaler".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						t.LabelBaseShort
					}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReserve(t, 1))
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						t.LabelBaseShort
					}) + " (" + "ReservedBy".Translate(new object[]
					{
						Find.Reservations.FirstReserverOf(t, pawn.Faction).LabelBaseShort
					}) + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else
                        {
                            item2 = new FloatMenuOption("ConsumeThing".Translate(new object[]
					{
						t.LabelBaseShort
					}), new Action(delegate
                            {
                                t.SetForbidden(false, true);
                                Job job = new Job(JobDefOf.Ingest, t);
                                job.maxNumToCarry = t.def.ingestible.maxNumToIngestAtOnce;
                                pawn.drafter.TakeOrderedJob(job);
                            }), MenuOptionPriority.Medium, null, null);
                        }
                        list.Add(item2);
                    }
                }

                // *** Rescue/Capture downed option ***

                foreach (TargetInfo current4 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    Pawn victim = (Pawn)current4.Thing;
                    if (!victim.InBed() && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1) && !victim.IsPrisonerOfColony)
                    {
                        if ((victim.Faction == Faction.OfColony && victim.MentalStateDef == null) || (victim.Faction != Faction.OfColony && victim.MentalStateDef == null && !victim.IsPrisonerOfColony && (victim.Faction == null || !victim.Faction.HostileTo(Faction.OfColony))))
                        {
                            List<FloatMenuOption> arg_8E1_0 = list;
                            Pawn victim2 = victim;
                            arg_8E1_0.Add(new FloatMenuOption("Rescue".Translate(new object[]
					{
						victim2.LabelCap
					}), new Action(delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                                if (building_Bed == null)
                                {
                                    string str;
                                    if (victim.RaceProps.Animal)
                                    {
                                        str = "NoAnimalBed".Translate();
                                    }
                                    else
                                    {
                                        str = "NoNonPrisonerBed".Translate();
                                    }
                                    Messages.Message("CannotRescue".Translate() + ": " + str, victim, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Rescue, victim, building_Bed);
                                job.maxNumToCarry = 1;
                                job.playerForced = true;
                                pawn.drafter.TakeOrderedJob(job);
                                ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                            }), MenuOptionPriority.Medium, null, victim2));
                        }
                        if (victim.MentalStateDef != null || (victim.RaceProps.Humanlike && victim.Faction != Faction.OfColony))
                        {
                            List<FloatMenuOption> arg_962_0 = list;
                            Pawn victim2 = victim;
                            arg_962_0.Add(new FloatMenuOption("Capture".Translate(new object[]
					{
						victim2.LabelCap
					}), new Action(delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Capture, victim, building_Bed);
                                job.maxNumToCarry = 1;
                                job.playerForced = true;
                                pawn.drafter.TakeOrderedJob(job);
                                ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                            }), MenuOptionPriority.Medium, null, victim2));
                        }
                    }
                }

                // *** Carry to cryosleep option ***

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
                        Action action3 = new Action(delegate
                        {
                            Building_CryptosleepCasket building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn);
                            if (building_CryptosleepCasket == null)
                            {
                                Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim, MessageSound.RejectInput);
                                return;
                            }
                            Job job = new Job(jDef, victim, building_CryptosleepCasket);
                            job.maxNumToCarry = 1;
                            job.playerForced = true;
                            pawn.drafter.TakeOrderedJob(job);
                        });
                        List<FloatMenuOption> arg_A80_0 = list;
                        Pawn victim2 = victim;
                        arg_A80_0.Add(new FloatMenuOption(label, action3, MenuOptionPriority.Medium, null, victim2));
                    }
                }

                // *** Strip option ***

                foreach (TargetInfo current6 in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), true))
                {
                    TargetInfo stripTarg = current6;
                    FloatMenuOption item3;
                    if (!pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
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
					Find.Reservations.FirstReserverOf(stripTarg, pawn.Faction).LabelBaseShort
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
                        }), MenuOptionPriority.Medium, null, null);
                    }
                    list.Add(item3);
                }

                // *** Equip option ***

                CompInventory compInventory = pawn.TryGetComp<CompInventory>();      // Need compInventory here for equip and wear options

                if (pawn.equipment != null)
                {
                    ThingWithComps equipment = null;
                    List<Thing> thingList = clickCell.GetThingList();
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
                        string eqLabel = GenLabel.ThingLabel(equipment.def, equipment.Stuff, 1);
                        FloatMenuOption equipOption;
                        if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            equipOption = new FloatMenuOption("CannotEquip".Translate(new object[]
					        {
						        eqLabel
					        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReserve(equipment, 1))
                        {
                            equipOption = new FloatMenuOption("CannotEquip".Translate(new object[]
					        {
						        eqLabel
					        }) + " (" + "ReservedBy".Translate(new object[]
					        {
						        Find.Reservations.FirstReserverOf(equipment, pawn.Faction).LabelBaseShort
					        }) + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        {
                            equipOption = new FloatMenuOption("CannotEquip".Translate(new object[]
					        {
						        eqLabel
					        }) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else
                        {
                            // Added check for inventory space here
                            int count;
                            if (compInventory != null && !compInventory.CanFitInInventory(equipment, out count, true))
                            {
                                equipOption = new FloatMenuOption("CannotEquip".Translate(new object[] { eqLabel }) + " (" + "CR_InventoryFull".Translate() + ")", null);
                            }
                            else
                            {
                                string equipOptionLabel = "Equip".Translate(new object[]
					                {
						                eqLabel
					                });
                                if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                                {
                                    equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
                                }
                                equipOption = new FloatMenuOption(equipOptionLabel, new Action(delegate
                                {
                                    equipment.SetForbidden(false, true);
                                    Job job = new Job(JobDefOf.Equip, equipment);
                                    job.playerForced = true;
                                    pawn.drafter.TakeOrderedJob(job);
                                    MoteThrower.ThrowStatic(equipment.DrawPos, ThingDefOf.Mote_FeedbackEquip, 1f);
                                    ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                                }), MenuOptionPriority.Medium, null, null);
                            }
                        }
                        list.Add(equipOption);
                    }
                }

                // *** Wear option ***

                if (pawn.apparel != null)
                {
                    Apparel apparel = Find.ThingGrid.ThingAt<Apparel>(clickCell);
                    if (apparel != null)
                    {
                        FloatMenuOption wearOption;
                        if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            wearOption = new FloatMenuOption("CannotWear".Translate(new object[]
					{
						apparel.Label
					}) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!pawn.CanReserve(apparel, 1))
                        {
                            Pawn pawn3 = Find.Reservations.FirstReserverOf(apparel, pawn.Faction);
                            wearOption = new FloatMenuOption("CannotWear".Translate(new object[]
					{
						apparel.Label
					}) + " (" + "ReservedBy".Translate(new object[]
					{
						pawn3.LabelBaseShort
					}) + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                        {
                            wearOption = new FloatMenuOption("CannotWear".Translate(new object[]
					{
						apparel.Label
					}) + " (" + "CannotWearBecauseOfMissingBodyParts".Translate() + ")", null, MenuOptionPriority.Medium, null, null);
                        }
                        else
                        {
                            // Added check for inventory capacity
                            int count;
                            if (compInventory != null && !compInventory.CanFitInInventory(apparel, out count, false, true))
                            {
                                wearOption = new FloatMenuOption("CannotWear".Translate(new object[] { apparel.Label }) + " (" + "CR_InventoryFull".Translate() + ")", null);
                            }
                            else
                            {
                                wearOption = new FloatMenuOption("ForceWear".Translate(new object[] { apparel.LabelBaseShort }), 
                                    new Action(delegate
                                        {
                                            apparel.SetForbidden(false, true);
                                            Job job = new Job(JobDefOf.Wear, apparel);
                                            job.playerForced = true;
                                            pawn.drafter.TakeOrderedJob(job);
                                        }), 
                                        MenuOptionPriority.Medium, null, null);
                            }
                        }
                        list.Add(wearOption);
                    }
                }

                // *** NEW: Pick up option ***

                if (compInventory != null)
                {
                    List<Thing> thingList = clickCell.GetThingList();
                    if (!thingList.NullOrEmpty<Thing>())
                    {
                        Thing item = thingList.FirstOrDefault(thing => thing.def.alwaysHaulable && !(thing is Corpse));
                        if (item != null)
                        {
                            FloatMenuOption pickUpOption;
                            int count = 0;
                            if (!pawn.CanReach(item, PathEndMode.Touch, Danger.Deadly))
                            {
                                pickUpOption = new FloatMenuOption("CR_CannotPickUp".Translate() + " " + item.LabelBaseShort + " (" + "NoPath".Translate() + ")", null);
                            }
                            else if (!pawn.CanReserve(item))
                            {
                                pickUpOption = new FloatMenuOption("CR_CannotPickUp".Translate() + " " + item.LabelBaseShort + " (" + "ReservedBy".Translate(new object[] { Find.Reservations.FirstReserverOf(item, pawn.Faction) }), null);
                            }
                            else if (!compInventory.CanFitInInventory(item, out count))
                            {
                                pickUpOption = new FloatMenuOption("CR_CannotPickUp".Translate() + " " + item.LabelBaseShort + " (" + "CR_InventoryFull".Translate() + ")", null);
                            }
                            else
                            {
                                pickUpOption = new FloatMenuOption("CR_PickUp".Translate() + " " + item.LabelBaseShort,
                                    new Action(delegate
                                    {
                                        item.SetForbidden(false);
                                        Job job = new Job(JobDefOf.TakeInventory, item) { maxNumToCarry = 1 };
                                        job.playerForced = true;
                                        pawn.drafter.TakeOrderedJob(job);
                                    }));
                            }
                            list.Add(pickUpOption);
                            if (count > 1 && item.stackCount > 1)
                            {
                                int numToCarry = Math.Min(count, item.stackCount);
                                FloatMenuOption pickUpStackOption = new FloatMenuOption("CR_PickUp".Translate() + " " + item.LabelBaseShort + " x" + numToCarry.ToString(),
                                    new Action(delegate
                                    {
                                        item.SetForbidden(false);
                                        Job job = new Job(JobDefOf.TakeInventory, item) { maxNumToCarry = numToCarry };
                                        job.playerForced = true;
                                        pawn.drafter.TakeOrderedJob(job);
                                    }));
                                list.Add(pickUpStackOption);
                            }
                        }
                    }
                }

                // *** Deposit/drop equipment options ***

                if (pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    Thing thing2 = Find.ThingGrid.ThingAt(clickCell, ThingDefOf.EquipmentRack);
                    if (thing2 != null)
                    {
                        if (!pawn.CanReach(thing2, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
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
                                        Action action4 = new Action(delegate
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
								}), action4, MenuOptionPriority.Medium, null, null));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (pawn.equipment != null && GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true).Any<TargetInfo>())
                    {
                        Action action5 = new Action(delegate
                        {
                            ThingWithComps thingWithComps;
                            pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.Position, true);
                            pawn.drafter.TakeOrderedJob(new Job(JobDefOf.Wait, 20, false));
                        }
                );
                        list.Add(new FloatMenuOption("Drop".Translate(new object[]
				{
					pawn.equipment.Primary.LabelCap
				}), action5, MenuOptionPriority.Medium, null, null));
                    }
                }

                // *** Trade with option ***

                foreach (TargetInfo current7 in GenUI.TargetsAt(clickPos, TargetingParameters.ForTrade(), true))
                {
                    TargetInfo dest2 = current7;
                    if (!pawn.CanReach(dest2, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        list.Add(new FloatMenuOption("CannotTrade".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null));
                    }
                    else if (!pawn.CanReserve(dest2.Thing, 1))
                    {
                        list.Add(new FloatMenuOption("CannotTrade".Translate() + " (" + "Reserved".Translate() + ")", null, MenuOptionPriority.Medium, null, null));
                    }
                    else
                    {
                        Pawn pTarg = (Pawn)dest2.Thing;
                        Action action6 = new Action(delegate
                        {
                            Job job = new Job(JobDefOf.TradeWithPawn, pTarg);
                            job.playerForced = true;
                            pawn.drafter.TakeOrderedJob(job);
                            ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                        });
                        string str3 = string.Empty;
                        if (pTarg.Faction != null)
                        {
                            str3 = " (" + pTarg.Faction.name + ")";
                        }
                        List<FloatMenuOption> arg_142E_0 = list;
                        Thing thing = dest2.Thing;
                        arg_142E_0.Add(new FloatMenuOption("TradeWith".Translate(new object[]
				{
					pTarg.LabelBaseShort
				}) + str3, action6, MenuOptionPriority.Medium, null, thing));
                    }
                }
                foreach (Thing current8 in Find.ThingGrid.ThingsAt(clickCell))
                {
                    foreach (FloatMenuOption current9 in current8.GetFloatMenuOptions(pawn))
                    {
                        list.Add(current9);
                    }
                }
            }

            // *** End of humanlike options ***

            // *** Beginning of non-drafted options ***

            if (!pawn.Drafted)
            {
                bool flag = false;
                bool flag2 = false;
                foreach (Thing current10 in Find.ThingGrid.ThingsAt(clickCell))
                {
                    flag2 = true;
                    if (pawn.CanReach(current10, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
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
                foreach (Thing current11 in Find.ThingGrid.ThingsAt(clickCell))
                {
                    Pawn pawn4 = Find.Reservations.FirstReserverOf(current11, pawn.Faction);
                    if (pawn4 != null && pawn4 != pawn)
                    {
                        list.Add(new FloatMenuOption("IsReservedBy".Translate(new object[]
				{
					current11.LabelBaseShort.CapitalizeFirst(),
					pawn4.LabelBaseShort
				}), null, MenuOptionPriority.Medium, null, null));
                    }
                    else
                    {
                        JobGiver_Work jobGiver_Work = pawn.thinker.TryGetMainTreeThinkNode<JobGiver_Work>();
                        if (jobGiver_Work != null)
                        {
                            foreach (WorkTypeDef current12 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                            {
                                for (int k = 0; k < current12.workGiversByPriority.Count; k++)
                                {
                                    WorkGiver_Scanner workGiver_Scanner = current12.workGiversByPriority[k].Worker as WorkGiver_Scanner;
                                    if (workGiver_Scanner != null)
                                    {
                                        if (workGiver_Scanner.def.directOrderable)
                                        {
                                            if (!workGiver_Scanner.ShouldSkip(pawn))
                                            {
                                                JobFailReason.Clear();
                                                Job job;
                                                if (!workGiver_Scanner.HasJobOnThingForced(pawn, current11))
                                                {
                                                    job = null;
                                                }
                                                else
                                                {
                                                    job = workGiver_Scanner.JobOnThingForced(pawn, current11);
                                                }
                                                if (workGiver_Scanner.PotentialWorkThingRequest.Accepts(current11) || (workGiver_Scanner.PotentialWorkThingsGlobal(pawn) != null && workGiver_Scanner.PotentialWorkThingsGlobal(pawn).Contains(current11)))
                                                {
                                                    if (job == null)
                                                    {
                                                        if (JobFailReason.HaveReason)
                                                        {
                                                            string label2 = "CannotGenericWork".Translate(new object[]
													{
														workGiver_Scanner.def.verb,
														current11.LabelBaseShort
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
														current11.LabelBaseShort
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
                                                        else if (job.def == JobDefOf.Research && current11 is Building_ResearchBench)
                                                        {
                                                            label = "CannotPrioritizeResearch".Translate();
                                                        }
                                                        else if (current11.IsForbidden(pawn))
                                                        {
                                                            label = "CannotPrioritizeForbidden".Translate(new object[]
													{
														current11.Label
													});
                                                        }
                                                        else if (!pawn.CanReach(current11, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                                                        {
                                                            label = current11.Label + ": " + "NoPath".Translate();
                                                        }
                                                        else
                                                        {
                                                            label = "PrioritizeGeneric".Translate(new object[]
													{
														workGiver_Scanner.def.gerund,
														current11.Label
													});
                                                            Job localJob = job;
                                                            WorkTypeDef localWorkTypeDef = workType;
                                                            action7 = new Action(delegate { pawn.thinker.GetMainTreeThinkNode<JobGiver_Work>().TryStartPrioritizedWorkOn(pawn, localJob, localWorkTypeDef); });
                                                        }
                                                        if (!list.Any(op => op.label == label))
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

            foreach (FloatMenuOption current13 in pawn.GetExtraFloatMenuOptionsFor(clickCell))
            {
                list.Add(current13);
            }
            DangerUtility.DoneDirectOrdering();
            return list;
        }

    }
}
