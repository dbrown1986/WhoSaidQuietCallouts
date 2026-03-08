using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// DomesticDisturbance.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  Respond to a report of a domestic disturbance at a residential location.
    ///  Depending on behavior states, involved individuals may be compliant,
    ///  verbally aggressive, or escalate into violence.  Player discretion advised.
    /// </summary>
    [CalloutInfo("Domestic Disturbance", CalloutProbability.Medium)]
    public class DomesticDisturbance : Callout
    {
        private Vector3 _sceneLocation;
        private Blip _sceneBlip;

        private Ped _subjectA;  // Primary individual
        private Ped _subjectB;  // Secondary individual
        private List<Ped> _participants = new List<Ped>();

        private bool _sceneActive;
        private bool _callHandled;
        private Random _rng = new Random();

        // Tracks whether aggression escalated
        private bool _escalated;

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(player.Around(350f));

                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 50f);
                CalloutMessage = "Reports of a Disturbance at a Residence";
                CalloutPosition = _sceneLocation;

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT DOMESTIC_DISPUTE IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~y~Domestic Disturbance", "Caller reports verbal altercation between a couple.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] Exception during setup: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] Callout accepted by officer.");

                // Spawn two subjects near residence
                _sceneLocation = World.GetNextPositionOnStreet(_sceneLocation);
                _subjectA = new Ped("A_F_Y_BevHills_01", _sceneLocation.Around(2f), 0f);
                _subjectB = new Ped("A_M_Y_BevHills_02", _sceneLocation.Around(3f), 180f);

                _subjectA.BlockPermanentEvents = false;
                _subjectB.BlockPermanentEvents = false;
                _subjectA.IsPersistent = true;
                _subjectB.IsPersistent = true;

                _participants.Add(_subjectA);
                _participants.Add(_subjectB);

                _sceneBlip = new Blip(_sceneLocation, 30f)
                {
                    Color = System.Drawing.Color.Yellow,
                    Alpha = 0.75f,
                    Name = "Domestic Disturbance"
                };

                _sceneActive = true;
                _callHandled = false;

                Game.DisplayHelp("Approach the scene carefully and investigate the disturbance.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] Error while creating participants: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;

            // Ensure subjects exist
            if (!_subjectA || !_subjectB) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_sceneLocation);

            // Player arrives at the scene
            if (distance < 15f && !_escalated)
            {
                Game.DisplaySubtitle("~y~You arrive at the scene. Speak with both individuals to assess the situation.");
                
                int behavior = _rng.Next(0, 100);
                if (behavior < 50)
                {
                    // Calm dialog scenario
                    Game.LogTrivial("[WSQ][DomesticDisturbance] Subjects are compliant.");
                    _subjectA.Tasks.StandStill(-1);
                    _subjectB.Tasks.StandStill(-1);
                }
                else if (behavior < 80)
                {
                    // Minor aggression (verbal altercation)
                    Game.LogTrivial("[WSQ][DomesticDisturbance] Verbal argument scenario.");
                    _subjectB.Tasks.TurnToFaceEntity(_subjectA, 2000);
                    _subjectA.Tasks.TurnToFaceEntity(_subjectB, 2000);
                    Game.DisplaySubtitle("~o~Verbal argument escalating — keep calm and separate the parties.");
                }
                else
                {
                    // Escalates into violence
                    _escalated = true;
                    Game.LogTrivial("[WSQ][DomesticDisturbance] Scenario escalated into fight!");
                    _subjectA.Inventory.GiveNewWeapon("WEAPON_UNARMED", 0, true);
                    _subjectB.Inventory.GiveNewWeapon("WEAPON_UNARMED", 0, true);
                    _subjectA.Tasks.FightAgainst(_subjectB);
                    _subjectB.Tasks.FightAgainst(_subjectA);
                    Functions.PlayScannerAudio("ASSAULT_WITH_A_DEADLY_WEAPON");
                    Game.DisplaySubtitle("~r~Subjects are fighting! Break it up or make arrests.");
                }
            }

            // Check if both subjects are neutralized or arrested
            if (_sceneActive && (_subjectA.IsDead || _subjectB.IsDead || Functions.IsPedArrested(_subjectA) || Functions.IsPedArrested(_subjectB)))
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~Scene secure. Write a report and conclude call.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                End();
            }

            // If player leaves the area
            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 400f)
            {
                Game.DisplayHelp("You left the area. Dispatch has cleared the call.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][DomesticDisturbance] Cleaning up scene entities.");

            try
            {
                _sceneActive = false;

                foreach (Ped ped in _participants)
                {
                    if (ped && ped.Exists()) ped.Dismiss();
                }

                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] Cleanup Exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Domestic Disturbance handled.");
        }
    }
}
