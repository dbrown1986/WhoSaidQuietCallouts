using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;  // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// SpeedingVehicle.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual End)
    /// Updated March 9 2026 by Who Said Quiet Team Maintenance.
    ///
    /// Description:
    ///  A vehicle has been reported driving at reckless speeds. The officer must locate
    ///  and stop the driver — possible outcomes include compliance, reckless ignoring, or active flight.
    ///  Uses reflective integration for UltimateBackup / StopThePed when loaded.
    /// </summary>
    [CalloutInfo("Speeding Vehicle", CalloutProbability.Medium)]
    public class SpeedingVehicle : WSQCalloutBase
    {
        private Vector3 _spawnPoint;
        private Vehicle _suspectVehicle;
        private Ped _driver;
        private Blip _vehicleBlip;
        private Blip _routeBlip;

        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;

        private LHandle _pursuitHandle;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(player.Around(700f));

                CalloutMessage = "Reports of Vehicle Driving at High Speed";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 80f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT RECKLESS_DRIVER IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~y~Speeding Vehicle",
                    "Caller reports a vehicle weaving through traffic at high speeds.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][SpeedingVehicle] Callout accepted.");
            try
            {
                // ─── Spawn suspect vehicle and driver ───
                _suspectVehicle = new Vehicle("BUFFALO2", _spawnPoint);
                _suspectVehicle.IsPersistent = true;

                _driver = _suspectVehicle.CreateRandomDriver();
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
                    Blip areaBlip = new Blip(_spawnPoint, 80f)
                    {
                        Color = System.Drawing.Color.Orange,
                        Alpha = 0.8f,
                        Name = "Speeding Vehicle Area"
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the reported speeding vehicle area.");
                    _routeBlip = areaBlip;
                }
                else
                {
                    Blip gpsRoute = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Speeding Vehicle"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~y~speeding vehicle~s~ scene.");
                    _routeBlip = gpsRoute;
                }

                // ─── Attach live suspect blip ───
                _vehicleBlip = _suspectVehicle.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Orange;
                _vehicleBlip.Name = "Speeding Vehicle";

                Game.DisplayHelp("Locate the ~y~speeding vehicle~s~ and initiate a traffic stop when safe.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
                _sceneActive = true;

                // Optional UltimateBackup support
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup", "UltimateBackup.API.Functions",
                        "RequestBackupUnit", _spawnPoint, "Traffic Enforcement Unit Code 3");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;
            if (!_suspectVehicle || !_suspectVehicle.Exists() || !_driver || !_driver.Exists()) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            // ─── Behavior trigger ───
            if (distance < 80f && !_pursuitStarted)
            {
                int behavior = _rng.Next(0, 100);

                if (behavior < 60)
                {
                    Game.DisplaySubtitle("~y~Suspect observed speeding — initiate a traffic stop.");
                    _driver.Tasks.CruiseWithVehicle(20f, VehicleDrivingFlags.Normal);
                }
                else if (behavior < 85)
                {
                    Game.DisplaySubtitle("~o~Suspect ignoring sirens and continuing recklessly!");
                    if (_suspectVehicle.Exists())
                    {
                        Vector3 fwd = _suspectVehicle.ForwardVector;
                        _suspectVehicle.Velocity = fwd * 40f;
                    }
                    _driver.Tasks.CruiseWithVehicle(40f, VehicleDrivingFlags.Normal);

                    // Optional StopThePed traffic alert
                    if (PluginBridge.IsPluginLoaded("StopThePed"))
                    {
                        PluginBridge.TryInvoke(
                            "StopThePed", "StopThePed.API.Functions", "AlertNearbyPeds");
                    }
                }
                else
                {
                    StartPursuit();
                }
            }

            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuitHandle))
            {
                HandleCompletion();
            }

            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 800f)
            {
                Game.DisplayHelp("You left the area. Press ~y~END~s~ to close the callout.");
                PlayerControlledEnd();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] Pursuit initiated.");
                _pursuitHandle = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuitHandle, _driver);
                Functions.SetPursuitIsActiveForPlayer(_pursuitHandle, true);
                _pursuitStarted = true;

                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_FLEEING UNITS_RESPOND_CODE_3");
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "Dispatch", "~r~Vehicle Pursuit",
                    "Suspect is fleeing at high speed — use caution!");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] StartPursuit Exception: " + ex.Message);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                _callHandled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplaySubtitle("~g~Suspect stopped. Issue citation or arrest as needed.", 4000);
                Game.DisplayHelp("Press ~y~END~s~ to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] HandleCompletion Exception: " + ex.Message);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][SpeedingVehicle] Cleanup scene entities.");

            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_vehicleBlip != null && _vehicleBlip.Exists()) _vehicleBlip.Delete();
                if (_driver != null && _driver.Exists()) _driver.Dismiss();
                if (_suspectVehicle != null && _suspectVehicle.Exists()) _suspectVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SpeedingVehicle] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Speeding vehicle incident resolved. Code 4.");
        }
    }
}