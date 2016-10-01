﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;
using RimWorld;






namespace DermalRegenerator
{

    
    class Building_DermalRegeneratorNew : Building
    {
        private int count = 0;
        private Pawn JobPawn = null;
        private Pawn OwnerPawn = null;
        private int oldCount = 0;
        private Hediff foundInj = null;
        private Job job1 = new Job();
        private Job job2 = new Job();
        private CompPowerTrader powerComp;

        public virtual bool UsableNow
        {
            get
            {
                return this.powerComp == null || this.powerComp.PowerOn;
                    }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            this.powerComp = base.GetComp<CompPowerTrader>();
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            {
                if (!myPawn.CanReserve(this))
                {
                    FloatMenuOption item = new FloatMenuOption("CannotUseReserved", null);
                    return new List<FloatMenuOption>
				{
					item
				};
                }
                if (!myPawn.CanReach(this, PathEndMode.Touch, Danger.Some))
                {
                    FloatMenuOption item2 = new FloatMenuOption("CannotUseNoPath", null);
                    return new List<FloatMenuOption>
				{
					item2
				};

                }

                if (OwnerPawn == null)
                {
                    Action action = delegate
                    {
                        job1 = new Job(JobDefOf.Goto, this.InteractionCell);
                        job2 = new Job(JobDefOf.Wait, 18100);
                        myPawn.drafter.TakeOrderedJob(job1);
                        myPawn.drafter.pawn.QueueJob(job2);
                        JobPawn = myPawn;
                        myPawn.Reserve(this);
                    };
                    list.Add(new FloatMenuOption("Use Dermal Regenerator", action));
                }
            }
            return list;
        }
      
        /*
        public static void ThrowMicroSparksGreen(Vector3 loc)
        {
            if (!loc.ShouldSpawnMotesAt() || MoteCounter.Saturated)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_HealGreen"), null);
            //moteThrown.ScaleUniform = Rand.Range(1.2f, 1.5f);
            //moteThrown.exactRotationRate = Rand.Range(0f, 0f);
            moteThrown.exactPosition = loc;
            moteThrown.exactPosition += new Vector3(0.5f, 0f, 0.5f);
            //moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
            //moteThrown.SetVelocityAngleSpeed((float)Rand.Range(0, 2), Rand.Range(0.003f, 0.003f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
        }
      
         */

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            if (OwnerPawn != null)
            {
                int scanpercent = count / 90;
                int healpercent = (count - 90) / 90;
                if (count == 0)
                {
                    stringBuilder.AppendLine("Waiting for patient.");
                }
                else if (count <= 90)
                {
                    stringBuilder.AppendLine("Scanning... Progress: " + scanpercent + "%");
                    // TODO possibly change this to current stage percent AND total percent.
                }
                else
                {
                    stringBuilder.AppendLine("Treating... Progress: " + healpercent + "%");
                }
            }
            return stringBuilder.ToString();
        }

        public override void Tick()
        {
            base.Tick();
            if(OwnerPawn != null && OwnerPawn.Position != this.InteractionCell)
            {
                OwnerPawn = null;
                count = 0;
                foundInj = null;
                oldCount = 0;
            }
            if (JobPawn != null)
            {
                if (OwnerPawn != null)
                {
                    string messageText1;
                    messageText1 = "Dermal Regenerator in use.";
                    Messages.Message(messageText1, MessageSound.Benefit);
                    JobPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    JobPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    JobPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    JobPawn = null;
                    return;
                }
                else
                {
                    if (this.UsableNow)
                    {
                        if (JobPawn.Position == this.InteractionCell)
                        {
                            OwnerPawn = JobPawn;
                            JobPawn = null;
                            return;
                        }
                    }
                    else if (OwnerPawn == null && !this.UsableNow)
                    {
                        string messageText2;
                        messageText2 = "Dermal Regenerator doesnt have power.";
                        Messages.Message(messageText2, MessageSound.Benefit);
                        JobPawn.jobs.jobQueue.Clear();
                        JobPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        JobPawn.jobs.EndCurrentJob(JobCondition.InterruptForced); 
                        JobPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        JobPawn = null;
                        return;
                    }
                }
            }
            if (OwnerPawn != null && !this.UsableNow)
            {
                string messageText3;
                messageText3 = "Dermal Regenerator power interupted.";
                Messages.Message(messageText3, MessageSound.Benefit);
                OwnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                OwnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                OwnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                OwnerPawn.health.AddHediff(HediffDef.Named("DermalRegeneratorSickness"), null, null);
                OwnerPawn = null;
                count = 0;
                foundInj = null;
                oldCount = 0;
                return;
            }
            if (OwnerPawn != null && OwnerPawn.Position == this.InteractionCell && this.UsableNow)
            {
                /* throw glow, or mote -> graphical effect for 
                Find.GlowGrid.VisualGlowAt(Position);
                if (count < 90)
                {
                    if (count % 90 == 0)
                    {
                        ThrowMicroSparksBlue(Position.ToVector3());
                    }
                    if (count % 200 == 0)
                    {
                        ThrowLightningGlowBlue(Position.ToVector3(), 1f);
                    }
                }
                else if (count > 90)
                {
                    if (count % 90 == 0)
                    {
                        ThrowMicroSparksGreen(Position.ToVector3());
                    }
                    if (count % 200 == 0)
                    {
                        ThrowLightningGlowGreen(Position.ToVector3(), 1f);
                    }
                }
                */
                count++;

                /* checking hediffs for injuries .. sep 
                foreach (Hediff current in OwnerPawn.health.hediffSet.GetHediffs<Hediff>())
                {
                    if (current is Hediff_Injury && current.IsOld() && (current.Label.Contains("scar") || current.Label.Contains("Scar")))
                    {
                        oldCount++;
                        foundInj = current;



                    }
                } */ 

                if (count >= 180)
                {
                    OwnerPawn.health.hediffSet.hediffs.Remove(foundInj);
                    OwnerPawn.health.Notify_HediffChanged(foundInj);
                    foundInj = null;
                    if (!OwnerPawn.health.ShouldBeTendedNow)
                    {
                        string messageText4;

                        // Howe to getta ta age 
                        messageText4 = "Biological Years :" + OwnerPawn.ageTracker.AgeBiologicalYears;
                        Messages.Message(messageText4, MessageSound.Benefit);

                        // tis one lookses for da penis
                        messageText4 = "Gender :" + OwnerPawn.gender.ToString();

                        Messages.Message(messageText4, MessageSound.Benefit);



                        messageText4 = "OwnerPawn.RaceProps.Humanlike.ToString() : " + OwnerPawn.RaceProps.Humanlike.ToString();
                        Messages.Message(messageText4, MessageSound.Benefit);

                        
                        /* This one gives back Human */
                        messageText4 = "OwnerPawn.def.ToString(); " + OwnerPawn.def.ToString();

                        // OwnerPawn.def.ChangeType

                        Messages.Message(messageText4, MessageSound.Benefit);


                        // find out which race the pawn has, and then change it 
                        // messageText5 = OwnerPawn.RaceProps.GetType();
                        // OwnerPawn.RaceProps;
                        //OwnerPawn.def.ChangeType<TabDrawer>
                        // OwnerPawn.kindDef.race.ChangeType






                        OwnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        OwnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        OwnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                      //  OwnerPawn.health.AddHediff(HediffDef.Named("DermalRegeneratorSickness"), null, null);
                        count = 0;
                        foundInj = null;
                        oldCount = 0;
                        OwnerPawn = null;




                    }
                }

            }
        }
    }
}
