using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// Kidnapping.cs
    /// Version: 0.9.1 Alpha (Compatibility Build)
    /// Date: March 9, 2026
    /// 
    /// Description:
    ///  A civilian has been reported abducted and forced into a vehicle.
    ///  Player officer must locate the suspect vehicle, pursue, and safely recover the victim.
    ///  Scenario can escalate into a pursuit or hostage rescue.
    /// </summary>
    [CalloutInfo("Kidnapping", CalloutProbability.Medium)]
    public class Kidnapping : WSQCalloutBase
    {
        private Vector3 _spawnPoint;
        private Vehicle _suspectVehicle;
        private Ped _suspect;
        private Ped _victim;
        private Blip _vehicleBlip;

        private bool _sceneActive;
        private bool _pursuitStarted;
        private bool _callHandled;
        private LHandle _pursuit;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(600f));

                CalloutMessage = "Possible Kidnapping Reported – Victim Forced into Vehicle";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 80f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT KIDNAPPING IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Kidnapping",
                    "Suspect vehicle seen fleeing from the scene.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Kidnapping] OnBeforeCalloutDisplayed Exception: {ex.Message}");
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][Kidnapping] Callout accepted.");
            try
            {
                // Spawn suspect vehicle
                _suspectVehicle = new Vehicle("SPEEDO", _spawnPoint)
                {
                    IsPersistent = true
                };

                // Create suspect (driver)
                _suspect = _suspectVehicle.CreateRandomDriver();
                if (!_suspect.Exists())
                {
                    Game.LogTrivial("[WSQ][Kidnapping] Failed to spawn suspect driver.");
                    End();
                    return false;
                }
                _suspect.BlockPermanentEvents = false;
                _suspect.IsPersistent = true;

                // Create victim (passenger)
                _victim = new Ped("A_F_Y_EastSA_02", _spawnPoint.Around(1.5f), 0f)
                {
                    IsPersistent = true
                };
                if (_victim.Exists())
                {
                    _victim.BlockPermanentEvents = true;
                    _victim.WarpIntoVehicle(_suspectVehicle, 2); // back seat
                    _victim.Tasks.PlayAnimation("random@arrests", "idle_c", 1f, AnimationFlags.Loop);
                }

                // Create blip
                _vehicleBlip = _suspectVehicle.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Red;
                _vehicleBlip.Name = "Kidnapping Suspect";
                _vehicleBlip.IsFriendly = false;

                Game.DisplayHelp("Locate and follow the ~r~suspect vehicle~w~. Await further instructions or initiate a traffic stop.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Kidnapping] OnCalloutAccepted Exception: {ex}");
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            // Engage scenarios when player is close
            if (!_pursuitStarted && distance < 50f)
            {
                int behavior = _rng.Next(0, 100);

                if (behavior < 60)
                {
                    Game.DisplaySubtitle("~y~The suspect vehicle is slowing down. Prepare for a felony stop.");
                    StartStopScenario();
                }
                else
                {
                    StartPursuitScenario();
                }
            }

            // Pursuit ended
            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuit))
            {
                HandleSceneConclusion();
            }

            // Player left area
            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 800f)
            {
                Game.DisplayHelp("You left the area. Dispatch is reassigning this call.");
                End();
            }
        }

        private void StartStopScenario()
        {
            try
            {
                Game.LogTrivial("[WSQ][Kidnapping] Starting felony stop scenario.");

                if (_suspect.Exists())
                {
                    _suspect.Tasks.LeaveVehicle(_suspectVehicle, LeaveVehicleFlags.LeaveDoorOpen);
                    GameFiber.Wait(1500);
                    _suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                }

                if (_victim.Exists())
                {
                    _victim.Tasks.LeaveVehicle(_suspectVehicle, LeaveVehicleFlags.LeaveDoorOpen);

                    // ✅ FIX: Updated Flee() overload to work with current RPH API
                    _victim.Tasks.Flee(Game.LocalPlayer.Character.Position, 200f, -1);

                    Game.DisplaySubtitle("~b~Arrest the suspect and secure the victim.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Kidnapping] StartStopScenario Exception: {ex}");
            }
        }

        private void StartPursuitScenario()
        {
            try
            {
                Game.LogTrivial("[WSQ][Kidnapping] Suspect has fled the scene – pursuit started!");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "Dispatch", "~r~Kidnapping Pursuit", "Suspect is fleeing with victim! Use caution.");
                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_FLEEING_CRIME_SCENE");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Kidnapping] StartPursuitScenario Exception: {ex}");
            }
        }

        private void HandleSceneConclusion()
        {
            try
            {
                Game.DisplaySubtitle("~g~Kidnapping suspect stopped. Secure the victim and clear the call.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Kidnapping] HandleSceneConclusion Exception: {ex.Message}");
            }

            _callHandled = true;
            PlayerControlledEnd();
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][Kidnapping] Cleaning up scene entities.");

            try
            {
                _vehicleBlip?.Delete();
                if (_suspect.Exists()) _suspect.Dismiss();
                if (_victim.Exists()) _victim.Dismiss();
                if (_suspectVehicle.Exists()) _suspectVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Kidnapping] Cleanup Exception: {ex.Message}");
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Kidnapping call handled and scene secure.");
        }
    }
}