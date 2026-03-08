using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// SpeedingVehicle.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  A vehicle has been reported driving at reckless speeds. Player must locate,
    ///  observe, and stop the suspect driver. Depending on behavior, suspect may comply,
    ///  attempt to evade, or engage in dangerous driving.
    /// </summary>
    [CalloutInfo("Speeding Vehicle", CalloutProbability.Medium)]
    public class SpeedingVehicle : Callout
    {
        private Vector3 _spawnPoint;
        private Vehicle _suspectVehicle;
        private Ped _driver;
        private Blip _vehicleBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;
        private LHandle _pursuitHandle;

        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(700f));

                CalloutMessage = "Reports of Vehicle Driving at High Speed";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 80f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT RECKLESS_DRIVER IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~y~Speeding Vehicle", "Caller reports a vehicle weaving through traffic at high speeds.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][SpeedingVehicle] Callout accepted.");

            try
            {
                // Create vehicle and driver
                _suspectVehicle = new Vehicle("BUFFALO2", _spawnPoint)
                {
                    IsPersistent = true
                };

                _driver = _suspectVehicle.CreateRandomDriver();
                _driver.IsPersistent = true;
                _driver.BlockPermanentEvents = false;

                // Vehicle blip
                _vehicleBlip = _suspectVehicle.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Orange;
                _vehicleBlip.Name = "Speeding Vehicle";

                Game.DisplayHelp("Locate the ~y~speeding vehicle~w~ and initiate a traffic stop when safe.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;
            if (!_suspectVehicle || !_driver) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            // Random vehicle behavior upon player approach
            if (distance < 80f && !_pursuitStarted)
            {
                int behavior = _rng.Next(0, 100);
                if (behavior < 60)
                {
                    // Compliant driver slows down
                    Game.DisplaySubtitle("~y~Suspect vehicle observed speeding. Initiate a traffic stop.");
                    _driver.Tasks.CruiseWithVehicle(20f, VehicleDrivingFlags.Normal);
                }
                else if (behavior < 85)
                {
                    // Reckless driving continues
                    Game.DisplaySubtitle("~o~Suspect ignoring sirens and continues driving recklessly!");
                    _suspectVehicle.Speed = 40f;
                    _driver.Tasks.CruiseWithVehicle(40f, VehicleDrivingFlags.AvoidVehicles);
                }
                else
                {
                    // High-speed pursuit
                    StartPursuit();
                }
            }

            // Conclude when pursuit ends
            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuitHandle))
            {
                HandleCompletion();
            }

            // If player leaves area, terminate call
            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 800f)
            {
                Game.DisplayHelp("You left the area. Dispatch will handle remaining units.");
                End();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] Pursuit initiated.");
                _pursuitHandle = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuitHandle, _driver);
                Functions.SetPursuitIsActiveForPlayer(_pursuitHandle, true);
                _pursuitStarted = true;

                Functions.PlayScannerAudio("WE_HAVE SUSPECT_FLEEING_RECKLESS_DRIVING");
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Vehicle Pursuit", "Suspect is fleeing at high speed! Proceed with caution.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] StartPursuit Exception: " + ex.Message);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                _callHandled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplaySubtitle("~g~Suspect stopped. Issue citations or make arrest as necessary.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] HandleCompletion Exception: " + ex.Message);
            }

            End();
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][SpeedingVehicle] Cleaning up scene.");

            try
            {
                if (_vehicleBlip && _vehicleBlip.Exists()) _vehicleBlip.Delete();
                if (_driver && _driver.Exists()) _driver.Dismiss();
                if (_suspectVehicle && _suspectVehicle.Exists()) _suspectVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Speeding vehicle handled successfully.");
        }
    }
}
