using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// SuspiciousVehicle.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  Investigate a suspiciously parked or occupied vehicle. Player must identify potential criminal activity,
    ///  run vehicle information, and respond accordingly. Scenario may escalate into a pursuit or arrest.
    /// </summary>
    [CalloutInfo("Suspicious Vehicle", CalloutProbability.Medium)]
    public class SuspiciousVehicle : Callout
    {
        private Vector3 _vehicleLocation;
        private Vehicle _suspiciousVehicle;
        private Ped _driver;
        private Blip _sceneBlip;
        private bool _sceneActive;
        private bool _handled;
        private bool _pursuitStarted;
        private LHandle _pursuit;

        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _vehicleLocation = World.GetNextPositionOnStreet(player.Around(450f));
                CalloutMessage = "Suspicious Vehicle Reported";
                CalloutPosition = _vehicleLocation;
                ShowCalloutAreaBlipBeforeAccepting(_vehicleLocation, 50f);
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT SUSPICIOUS_VEHICLE IN_OR_ON_POSITION", _vehicleLocation);

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] OnBeforeCalloutDisplayed exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][SuspiciousVehicle] Callout accepted.");

            try
            {
                // Spawn vehicle and driver
                _suspiciousVehicle = new Vehicle("FUGITIVE", _vehicleLocation)
                {
                    IsPersistent = true
                };

                _driver = _suspiciousVehicle.CreateRandomDriver();
                _driver.IsPersistent = true;
                _driver.BlockPermanentEvents = false;

                // Mark location
                _sceneBlip = new Blip(_vehicleLocation, 30f)
                {
                    Color = System.Drawing.Color.Yellow,
                    Alpha = 0.8f,
                    Name = "Suspicious Vehicle"
                };

                _sceneActive = true;
                _handled = false;

                Game.DisplayHelp("Investigate the ~y~suspicious vehicle~w~.  Approach carefully and observe driver behavior.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] Error creating scene: " + ex.Message);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _handled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_vehicleLocation);

            // Player arrives
            if (dist < 20f && !_pursuitStarted)
            {
                Game.DisplaySubtitle("Approach the driver and initiate a conversation.");

                // Randomize driver behavior once player is near
                int action = _rng.Next(0, 100);

                if (action < 50)
                {
                    // Compliant behavior
                    Game.LogTrivial("[WSQ][SuspiciousVehicle] Driver compliant scenario.");
                    Game.DisplaySubtitle("~y~Driver appears calm. Run plates and issue citation if warranted.");
                }
                else if (action < 80)
                {
                    // Nervous driver scenario
                    Game.LogTrivial("[WSQ][SuspiciousVehicle] Nervous behavior.");
                    _driver.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    Game.DisplaySubtitle("~o~Driver seems overly nervous. Possible warrants—verify ID.");
                }
                else
                {
                    // Fleeing suspect
                    StartPursuitScenario();
                }
            }

            // If pursuit ended
            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuit))
            {
                _handled = true;
                Game.DisplaySubtitle("~g~Suspect apprehended or pursuit concluded.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                End();
            }

            // Player leaves area
            if (Game.LocalPlayer.Character.DistanceTo(_vehicleLocation) > 450f)
            {
                End();
            }
        }

        private void StartPursuitScenario()
        {
            try
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] Driver fleeing—pursuit initiated!");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _driver);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Vehicle Pursuit", "Suspect is fleeing! Engage pursuit procedures.");
                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_RESISTING_ARREST");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] StartPursuitScenario exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][SuspiciousVehicle] Cleaning up callout entities.");

            try
            {
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
                if (_driver && _driver.Exists()) _driver.Dismiss();
                if (_suspiciousVehicle && _suspiciousVehicle.Exists()) _suspiciousVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] Cleanup exception: " + ex.Message);
            }

            _sceneActive = false;
            _handled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Suspicious vehicle scene cleared.");
        }
    }
}
