using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;  // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// SuspiciousVehicle.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  Investigate a suspiciously parked or occupied vehicle. The officer must identify potential
    ///  criminal activity, run information, and respond appropriately. Random outcomes include
    ///  compliance, nervous behavior, or flight.
    ///  Adds reflective integration for optional plugin support.
    /// </summary>
    [CalloutInfo("Suspicious Vehicle", CalloutProbability.Medium)]
    public class SuspiciousVehicle : WSQCalloutBase
    {
        private Vector3 _vehicleLocation;
        private Vehicle _suspiciousVehicle;
        private Ped _driver;
        private Blip _sceneBlip;
        private Blip _routeBlip;
        private bool _sceneActive;
        private bool _handled;
        private bool _pursuitStarted;
        private LHandle _pursuit;

        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _vehicleLocation = World.GetNextPositionOnStreet(player.Around(450f));

                CalloutMessage = "Suspicious Vehicle Reported";
                CalloutPosition = _vehicleLocation;
                ShowCalloutAreaBlipBeforeAccepting(_vehicleLocation, 50f);
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT SUSPICIOUS_VEHICLE IN_OR_ON_POSITION", _vehicleLocation);

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][SuspiciousVehicle] Callout accepted.");
            try
            {
                _suspiciousVehicle = new Vehicle("FUGITIVE", _vehicleLocation);
                _suspiciousVehicle.IsPersistent = true;

                _driver = _suspiciousVehicle.CreateRandomDriver();
                if (!_driver || !_driver.Exists())
                {
                    PlayerControlledEnd();
                    return false;
                }

                _driver.IsPersistent = true;
                _driver.BlockPermanentEvents = false;

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_vehicleLocation, 30f)
                    {
                        Color = System.Drawing.Color.Yellow,
                        Alpha = 0.8f,
                        Name = "Suspicious Vehicle Area"
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the suspicious vehicle location.");
                    _routeBlip = _sceneBlip;
                }
                else
                {
                    Blip gpsRoute = new Blip(_vehicleLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Suspicious Vehicle"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~y~suspicious vehicle~s~ scene.");
                    _routeBlip = gpsRoute;
                }

                _sceneActive = true;
                _handled = false;

                Game.DisplayHelp("Investigate the ~y~suspicious vehicle~s~ and observe driver behavior.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");

                // Optional backup support
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _vehicleLocation,
                        "Patrol Unit Code 2 – Assist Suspicious Vehicle");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] OnCalloutAccepted Exception: " + ex.Message);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _handled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_vehicleLocation);

            if (dist < 20f && !_pursuitStarted)
            {
                Game.DisplaySubtitle("Approach the driver and initiate a conversation.", 3000);

                int action = _rng.Next(0, 100);
                if (action < 50)
                {
                    Game.LogTrivial("[WSQ][SuspiciousVehicle] Compliant driver scenario.");
                    Game.DisplaySubtitle("~y~Driver appears calm — run plates and issue citation if warranted.");
                }
                else if (action < 80)
                {
                    Game.LogTrivial("[WSQ][SuspiciousVehicle] Nervous behavior scenario.");
                    _driver.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    Game.DisplaySubtitle("~o~Driver appears nervous — verify ID for possible warrants.");
                }
                else
                {
                    StartPursuitScenario();
                }
            }

            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuit))
            {
                _handled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplaySubtitle("~g~Suspect apprehended or pursuit concluded.", 4000);
                Game.DisplayHelp("Press ~y~END~s~ when ready to close the callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            if (Game.LocalPlayer.Character.DistanceTo(_vehicleLocation) > 450f)
            {
                Game.DisplayHelp("You left the incident area. Press ~y~END~s~ to close callout.");
                PlayerControlledEnd();
            }
        }

        private void StartPursuitScenario()
        {
            try
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] Fleeing driver — initiating pursuit!");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _driver);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;

                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_RESISTING_ARREST");
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Vehicle Pursuit",
                    "Suspect is fleeing — engage pursuit procedures.");

                // Optional StopThePed AI calming
                if (PluginBridge.IsPluginLoaded("StopThePed"))
                {
                    PluginBridge.TryInvoke(
                        "StopThePed",
                        "StopThePed.API.Functions",
                        "CalmNearbyPeds");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] StartPursuitScenario Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][SuspiciousVehicle] Cleaning up scene entities.");

            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_sceneBlip != null && _sceneBlip.Exists()) _sceneBlip.Delete();
                if (_driver != null && _driver.Exists()) _driver.Dismiss();
                if (_suspiciousVehicle != null && _suspiciousVehicle.Exists()) _suspiciousVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuspiciousVehicle] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _handled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Suspicious vehicle scene cleared. Code 4.");
        }
    }
}