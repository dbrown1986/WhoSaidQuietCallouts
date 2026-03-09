using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// StolenVehicle.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  A flagged plate has been reported as stolen. Player must locate the vehicle,
    ///  verify the registration, and decide whether to initiate a pursuit or make an arrest
    ///  depending on the suspect’s behavior.
    /// </summary>
    [CalloutInfo("Stolen Vehicle", CalloutProbability.Medium)]
    public class StolenVehicle : WSQCalloutBase
    {
        private Vector3 _spawnPoint;
        private Vehicle _vehicle;
        private Ped _driver;
        private Blip _vehicleBlip;
        private LHandle _pursuit;
        private bool _sceneActive;
        private bool _isPursuit;
        private bool _callHandled;

        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(600f));

                CalloutMessage = "Reported Stolen Vehicle Spotted";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 75f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT STOLEN_VEHICLE IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Stolen Vehicle", "Vehicle reported as stolen. Locate and confirm plate status.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][StolenVehicle] Callout accepted.");

            try
            {
                // Spawn the suspect vehicle
                _vehicle = new Vehicle("FELON", _spawnPoint)
                {
                    IsPersistent = true
                };

                // Create random driver
                _driver = _vehicle.CreateRandomDriver();
                _driver.BlockPermanentEvents = false;
                _driver.IsPersistent = true;

                // Attach blip for tracking
                _vehicleBlip = _vehicle.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Red;
                _vehicleBlip.Name = "Stolen Vehicle";

                // Dispatch message
                Game.DisplayHelp("Locate the ~r~stolen vehicle~w~. Run the plate before initiating a stop.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");

                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            // Engage when nearby
            if (dist < 50f && !_isPursuit)
            {
                Game.DisplaySubtitle("Vehicle located. Verify registration and initiate a traffic stop if necessary.");

                int behavior = _rng.Next(0, 100);
                if (behavior < 60)
                {
                    // Cooperative driver, will yield
                    Game.LogTrivial("[WSQ][StolenVehicle] Driver compliant.");
                    Game.DisplaySubtitle("~y~Driver appears calm. Proceed with standard traffic stop protocol.");
                    _driver.Tasks.CruiseWithVehicle(10f, VehicleDrivingFlags.Normal);
                }
                else if (behavior < 85)
                {
                    // Nervous driver — might flee if approached
                    Game.LogTrivial("[WSQ][StolenVehicle] Suspicious behavior detected.");
                    Game.DisplaySubtitle("~o~Driver appears nervous. Approach cautiously.");
                }
                else
                {
                    // Immediate pursuit trigger
                    StartPursuit();
                }
            }

            if (_isPursuit && !Functions.IsPursuitStillRunning(_pursuit))
            {
                // Pursuit concluded
                Game.LogTrivial("[WSQ][StolenVehicle] Pursuit terminated. Handling completion.");
                HandleCompletion();
            }

            // Player leaves area
            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 700f)
            {
                Game.DisplayHelp("You left the area. Dispatch has reassigned the call.");
                End();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Game.LogTrivial("[WSQ][StolenVehicle] Suspect fleeing. Starting pursuit.");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _driver);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _isPursuit = true;
                Functions.PlayScannerAudio("WE_HAVE SUSPECT_FLEEING_STOLEN_VEHICLE");
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Vehicle Pursuit", "Suspect is fleeing in the stolen vehicle!");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] StartPursuit Exception: " + ex);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                Game.DisplaySubtitle("~g~Suspect in custody or scene secure. Vehicle recovered.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");

                _callHandled = true;
                PlayerControlledEnd();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] HandleCompletion Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][StolenVehicle] Cleaning up entities.");

            try
            {
                if (_vehicleBlip && _vehicleBlip.Exists()) _vehicleBlip.Delete();
                if (_vehicle && _vehicle.Exists()) _vehicle.Dismiss();
                if (_driver && _driver.Exists()) _driver.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Stolen vehicle recovered and suspect detained.");
        }
    }
}
