using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// DomesticDisturbance.cs
    /// Version: 0.9.1 Alpha (Maintenance & Compatibility Build)
    /// Date: March 9, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  Respond to a report of a domestic disturbance at a residential location.
    ///  Depending on behavior states, involved individuals may be compliant,
    ///  verbally aggressive, or escalate into violence. Player discretion advised.
    /// </summary>
    [CalloutInfo("Domestic Disturbance", CalloutProbability.Medium)]
    public class DomesticDisturbance : WSQCalloutBase
    {
        private Vector3 _sceneLocation;
        private Blip _sceneBlip;

        private Ped _subjectA;  // Primary individual
        private Ped _subjectB;  // Secondary individual
        private readonly List<Ped> _participants = new List<Ped>();

        private bool _sceneActive;
        private bool _callHandled;
        private bool _escalated;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(player.Around(350f));

                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 50f);
                CalloutMessage = "Reports of a disturbance at a residence";
                CalloutPosition = _sceneLocation;

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT DOMESTIC_DISPUTE IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~y~Domestic Disturbance",
                    "Caller reports verbal altercation between a couple.");
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

                _sceneLocation = World.GetNextPositionOnStreet(_sceneLocation);
                _subjectA = new Ped("A_F_Y_BevHills_01", _sceneLocation.Around(2f), 0f);
                _subjectB = new Ped("A_M_Y_BevHills_02", _sceneLocation.Around(3f), 180f);

                if (!_subjectA.Exists() || !_subjectB.Exists())
                {
                    Game.LogTrivial("[WSQ][DomesticDisturbance] Ped spawn failed.");
                    End();
                    return false;
                }

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
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;
            if (!_subjectA.Exists() || !_subjectB.Exists()) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_sceneLocation);

            // Officer arrives
            if (distance < 15f && !_escalated)
            {
                Game.DisplaySubtitle("~y~You arrive at the scene. Speak with both individuals to assess the situation.");

                int behavior = _rng.Next(0, 100);

                if (behavior < 50)
                {
                    // Calm
                    Game.LogTrivial("[WSQ][DomesticDisturbance] Subjects are compliant.");
                    _subjectA.Tasks.StandStill(-1);
                    _subjectB.Tasks.StandStill(-1);
                }
                else if (behavior < 80)
                {
                    // Verbal argument
                    Game.LogTrivial("[WSQ][DomesticDisturbance] Verbal argument scenario.");

                    float headingA = (_subjectB.Position - _subjectA.Position).ToHeading();
                    float headingB = (_subjectA.Position - _subjectB.Position).ToHeading();

                    _subjectA.Tasks.AchieveHeading(headingA, 2000);
                    _subjectB.Tasks.AchieveHeading(headingB, 2000);

                    Game.DisplaySubtitle("~o~Verbal argument escalating — keep calm and separate the parties.");
                }
                else
                {
                    // Escalate to fight
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

            // Resolution
            if (_sceneActive &&
                (_subjectA.IsDead || _subjectB.IsDead ||
                 Functions.IsPedArrested(_subjectA) || Functions.IsPedArrested(_subjectB)))
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~Scene secure. Write a report and conclude call.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                PlayerControlledEnd();
            }

            // Player fled
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
                    if (ped.Exists()) ped.Dismiss();
                }

                if (_sceneBlip?.Exists() == true) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] Cleanup Exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Domestic Disturbance handled.");
        }
    }
}