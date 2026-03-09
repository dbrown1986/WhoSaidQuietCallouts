using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;  // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// StolenVehicle.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  A flagged plate has been reported as stolen. Officer must locate the vehicle,
    ///  verify registration, and determine action based on suspect behavior.
    ///  Optional reflective plugin support added for backup and scene AI control.
    /// </summary>
    [CalloutInfo("Stolen Vehicle", CalloutProbability.Medium)]
    public class StolenVehicle : WSQCalloutBase
    {
        private Vector3 _spawnPoint;
        private Vehicle _vehicle;
        private Ped _driver;
        private Blip _vehicleBlip;
        private Blip _routeBlip;
        private LHandle _pursuit;

        private bool _sceneActive;
        private bool _isPursuit;
        private bool _callHandled;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(600f));

                CalloutMessage = "Reported Stolen Vehicle Spotted";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 75f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT STOLEN_VEHICLE IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Stolen Vehicle",
                    "Vehicle reported as stolen. Locate and confirm plate status.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][StolenVehicle] Callout accepted.");
            try
            {
                _vehicle = new Vehicle("FELON", _spawnPoint);
                _vehicle.IsPersistent = true;

                _driver = _vehicle.CreateRandomDriver();
                if (!_driver || !_driver.Exists())
                {
                    PlayerControlledEnd();
                    return false;
                }

                _driver.BlockPermanentEvents = false;
                _driver.IsPersistent = true;

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    Blip radar = new Blip(_spawnPoint, 75f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Stolen Vehicle Area",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the suspected stolen vehicle area.");
                    _routeBlip = radar;
                }
                else
                {
                    Blip gpsRoute = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Stolen Vehicle"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~stolen vehicle~s~ area.");
                    _routeBlip = gpsRoute;
                }

                // ─── Vehicle tracker blip ───
                _vehicleBlip = _vehicle.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Red;
                _vehicleBlip.Name = "Stolen Vehicle";

                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
                Game.DisplayHelp("Locate the ~r~stolen vehicle~s~ and run the plate before initiating a stop.");
                _sceneActive = true;

                // Optional reflective backup request
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _spawnPoint,
                        "Traffic Enforcement Unit Code 2 – Stolen Vehicle Assistance");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            if (dist < 50f && !_isPursuit)
            {
                Game.DisplaySubtitle("Vehicle located. Verify registration and initiate a traffic stop if necessary.");
                int behavior = _rng.Next(0, 100);

                if (behavior < 60)
                {
                    Game.LogTrivial("[WSQ][StolenVehicle] Driver compliant.");
                    Game.DisplaySubtitle("~y~Driver appears calm. Proceed with standard traffic stop.");
                    _driver.Tasks.CruiseWithVehicle(10f, VehicleDrivingFlags.Normal);
                }
                else if (behavior < 85)
                {
                    Game.LogTrivial("[WSQ][StolenVehicle] Nervous driver detected.");
                    Game.DisplaySubtitle("~o~Driver appears nervous — approach with caution.");
                }
                else
                {
                    StartPursuit();
                }
            }

            if (_isPursuit && !Functions.IsPursuitStillRunning(_pursuit))
            {
                Game.LogTrivial("[WSQ][StolenVehicle] Pursuit ended — handling completion.");
                HandleCompletion();
            }

            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 700f)
            {
                Game.DisplayHelp("You left the area. Press ~y~END~s~ to close this callout.");
                PlayerControlledEnd();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Game.LogTrivial("[WSQ][StolenVehicle] Suspect fleeing — starting pursuit.");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _driver);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _isPursuit = true;

                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_FLEEING_STOLEN_VEHICLE");
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Vehicle Pursuit",
                    "Suspect is fleeing in the stolen vehicle — pursue with caution.");

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
                Game.LogTrivial("[WSQ][StolenVehicle] StartPursuit Exception: " + ex);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplaySubtitle("~g~Suspect detained and vehicle recovered.", 4000);
                Game.DisplayHelp("Press ~y~END~s~ to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });

                _callHandled = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] HandleCompletion Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][StolenVehicle] Cleaning up scene entities.");

            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_vehicleBlip != null && _vehicleBlip.Exists()) _vehicleBlip.Delete();
                if (_vehicle != null && _vehicle.Exists()) _vehicle.Dismiss();
                if (_driver != null && _driver.Exists()) _driver.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenVehicle] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Stolen vehicle case resolved. Code 4.");
        }
    }
}