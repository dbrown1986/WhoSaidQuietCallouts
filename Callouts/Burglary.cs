using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// Burglary.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  Respond to a burglary in progress at a residential or commercial property.
    ///  Player must determine if suspects are on scene, detain them if possible, 
    ///  and secure the location. Scenarios vary between silent alarm, break-in in progress,
    ///  and suspect fleeing the scene.
    /// </summary>
    [CalloutInfo("Burglary", CalloutProbability.Medium)]
    public class Burglary : WSQCalloutBase
    {
        private Vector3 _scenePosition;
        private Blip _sceneBlip;
        private Ped _suspect;
        private Ped _witness;
        private Vehicle _getawayVehicle;
        private bool _sceneActive;
        private bool _pursuitActive;
        private bool _callHandled;

        private LHandle _pursuitHandle;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _scenePosition = World.GetNextPositionOnStreet(playerPos.Around(550f));

                ShowCalloutAreaBlipBeforeAccepting(_scenePosition, 75f);
                CalloutMessage = "Burglary in Progress";
                CalloutPosition = _scenePosition;

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT BURGLARY IN_OR_ON_POSITION", _scenePosition);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Burglary in Progress", "Respond Code 3 to the alarm activation.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Burglary] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][Burglary] Callout accepted.");
            try
            {
                // Create suspect near location
                _suspect = new Ped("A_M_M_Skater_01", _scenePosition.Around(2f), 0f);
                _suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 50, true);
                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;

                // Optional witness spawn
                if (_rng.Next(0, 100) > 60)
                {
                    _witness = new Ped("A_F_Y_Hipster_02", _scenePosition.Around(6f), 90f);
                    _witness.IsPersistent = true;
                    _witness.BlockPermanentEvents = true;
                    Game.DisplaySubtitle("A witness is nearby — question them for details.");
                }

                // Possible getaway vehicle
                if (_rng.Next(0, 100) > 40)
                {
                    _getawayVehicle = new Vehicle("INTRUDER", _scenePosition.Around(10f))
                    {
                        IsPersistent = true
                    };
                }

                _sceneBlip = new Blip(_scenePosition, 40f)
                {
                    Color = System.Drawing.Color.Orange,
                    Alpha = 0.8f,
                    Name = "Burglary Scene"
                };

                Game.DisplayHelp("Respond to the ~r~burglary scene~w~. Approach with caution.");

                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Burglary] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_scenePosition);

            // When officer arrives on scene
            if (distance < 25f && !_pursuitActive)
            {
                int behavior = _rng.Next(0, 100);

                if (behavior < 50)
                {
                    // Compliant suspect
                    Game.DisplaySubtitle("~y~Suspect spotted near door. Detain them for questioning.");
                    _suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                }
                else if (behavior < 80)
                {
                    // Attempted escape
                    Game.DisplaySubtitle("~r~Suspect attempting to flee on foot!");
                    _suspect.Tasks.Flee(Game.LocalPlayer.Character.Position, 200f, -1);
                }
                else
                {
                    // Armed standoff
                    Game.DisplaySubtitle("~r~Suspect draws a weapon!");
                    _suspect.Tasks.AimWeaponAt(Game.LocalPlayer.Character, -1);
                    Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");
                    if (_getawayVehicle && _getawayVehicle.Exists()) _suspect.Tasks.EnterVehicle(_getawayVehicle, -1, 3f);
                    StartPursuit();
                }
            }

            // Check pursuit end
            if (_pursuitActive && !Functions.IsPursuitStillRunning(_pursuitHandle))
            {
                HandleSceneCompletion();
            }

            // Too far away -> auto end
            if (Game.LocalPlayer.Character.DistanceTo(_scenePosition) > 600f)
            {
                Game.DisplayHelp("You left the area. Dispatch has cleared the call.");
                End();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Game.LogTrivial("[WSQ][Burglary] Pursuit started.");
                _pursuitHandle = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuitHandle, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuitHandle, true);
                _pursuitActive = true;
                Functions.PlayScannerAudio("WE_HAVE SUSPECT_FLIGHT_FROM_BURGLARY");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Burglary] StartPursuit Exception: " + ex);
            }
        }

        private void HandleSceneCompletion()
        {
            try
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~Burglary scene cleared. File supplemental report and await release.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Burglary] HandleSceneCompletion Exception: " + ex.Message);
            }

            End();
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][Burglary] Cleaning up entities.");

            try
            {
                if (_suspect && _suspect.Exists()) _suspect.Dismiss();
                if (_witness && _witness.Exists()) _witness.Dismiss();
                if (_getawayVehicle && _getawayVehicle.Exists()) _getawayVehicle.Dismiss();
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Burglary] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Burglary handled successfully.");
        }
    }
}
