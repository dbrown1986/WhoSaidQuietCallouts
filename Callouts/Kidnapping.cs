using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// Kidnapping.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team Maintenance.
    /// Description:
    ///  A civilian has been reported abducted and forced into a vehicle.
    ///  Player must locate the suspect vehicle, pursue if necessary, and safely recover the victim.
    ///  Optional reflective integration adds support for backup plugins without extra DLL references.
    /// </summary>
    [CalloutInfo("Kidnapping", CalloutProbability.Medium)]
    public class Kidnapping : WSQCalloutBase
    {
        private Vector3 _spawnPoint;
        private Vehicle _suspectVehicle;
        private Ped _suspect;
        private Ped _victim;
        private Blip _vehicleBlip;
        private Blip _routeBlip;

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

                CalloutMessage = "Possible Kidnapping – Victim Forced into Vehicle";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 80f);

                Functions.PlayScannerAudioUsingPosition(
                    "CITIZENS_REPORT KIDNAPPING IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Kidnapping",
                    "Suspect vehicle seen fleeing from the scene.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Kidnapping] OnBeforeCalloutDisplayed Exception: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][Kidnapping] Callout accepted.");
            try
            {
                // ─── Spawn suspect vehicle and occupants ───
                _suspectVehicle = new Vehicle("SPEEDO", _spawnPoint);
                _suspectVehicle.IsPersistent = true;

                _suspect = _suspectVehicle.CreateRandomDriver();
                if (!_suspect.Exists())
                {
                    Game.LogTrivial("[WSQ][Kidnapping] Failed to spawn suspect driver.");
                    PlayerControlledEnd();
                    return false;
                }
                _suspect.BlockPermanentEvents = false;
                _suspect.IsPersistent = true;

                _victim = new Ped("A_F_Y_EastSA_02", _spawnPoint.Around(1.5f), 0f);
                if (_victim.Exists())
                {
                    _victim.IsPersistent = true;
                    _victim.BlockPermanentEvents = true;
                    _victim.WarpIntoVehicle(_suspectVehicle, 2);
                    _victim.Tasks.PlayAnimation("random@arrests", "idle_c", 1f, AnimationFlags.Loop);
                }

                // ─── Vehicle blip (suspect indicator) ───
                _vehicleBlip = _suspectVehicle.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Red;
                _vehicleBlip.Name = "Kidnapping Suspect";
                _vehicleBlip.IsFriendly = false;

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    Blip areaBlip = new Blip(_spawnPoint, 80f)
                    {
                        Color = System.Drawing.Color.Red,
                        Alpha = 0.7f,
                        Name = "Kidnapping Area"
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the kidnapping scene.");
                    _routeBlip = areaBlip;
                }
                else
                {
                    Blip gpsRoute = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Kidnapping"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~kidnapping~s~ scene.");
                    _routeBlip = gpsRoute;
                }

                _sceneActive = true;
                Game.DisplayHelp("Locate and follow the ~r~suspect vehicle~s~. Prepare for possible pursuit or felony stop.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                // ─── Optional reflective backup ───
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _spawnPoint,
                        "Air Unit and Patrol Car Response");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Kidnapping] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            if (!_pursuitStarted && distance < 50f)
            {
                int behavior = _rng.Next(0, 100);
                if (behavior < 60)
                {
                    Game.DisplaySubtitle("~y~Suspect vehicle slowing down — prepare for felony stop.");
                    StartStopScenario();
                }
                else
                {
                    StartPursuitScenario();
                }
            }

            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuit))
            {
                HandleSceneConclusion();
            }

            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 800f)
            {
                Game.DisplayHelp("You left the area. Press ~y~END~s~ to close the callout.");
                PlayerControlledEnd();
            }
        }

        private void StartStopScenario()
        {
            try
            {
                Game.LogTrivial("[WSQ][Kidnapping] Executing felony stop scenario.");

                if (_suspect.Exists())
                {
                    _suspect.Tasks.LeaveVehicle(_suspectVehicle, LeaveVehicleFlags.LeaveDoorOpen);
                    GameFiber.Wait(1500);
                    _suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                }

                if (_victim.Exists())
                {
                    _victim.Tasks.LeaveVehicle(_suspectVehicle, LeaveVehicleFlags.LeaveDoorOpen);
                    _victim.Tasks.Flee(Game.LocalPlayer.Character.Position, 200f, -1);
                    Game.DisplaySubtitle("~b~Arrest the suspect and secure the victim.");

                    if (PluginBridge.IsPluginLoaded("StopThePed"))
                    {
                        PluginBridge.TryInvoke(
                            "StopThePed",
                            "StopThePed.API.Functions",
                            "CalmNearbyPeds");
                    }
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Kidnapping] StartStopScenario Exception: " + ex);
            }
        }

        private void StartPursuitScenario()
        {
            try
            {
                Game.LogTrivial("[WSQ][Kidnapping] Suspect has fled — pursuit in progress.");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "Dispatch", "~r~Kidnapping Pursuit",
                    "Suspect is fleeing with victim! Use caution.");
                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_FLEEING_CRIME_SCENE");

                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _spawnPoint,
                        "Pursuit Assistance");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Kidnapping] StartPursuitScenario Exception: " + ex);
            }
        }

        private void HandleSceneConclusion()
        {
            try
            {
                Game.DisplaySubtitle(
                    "~g~Kidnapping suspect stopped. Secure victim and wrap up the scene.", 4000);
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplayHelp("Press ~y~END~s~ when ready to close the callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Kidnapping] HandleSceneConclusion Exception: " + ex.Message);
            }

            _callHandled = true;
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][Kidnapping] Cleaning up entities.");

            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_vehicleBlip != null && _vehicleBlip.Exists()) _vehicleBlip.Delete();
                if (_suspect != null && _suspect.Exists()) _suspect.Dismiss();
                if (_victim != null && _victim.Exists()) _victim.Dismiss();
                if (_suspectVehicle != null && _suspectVehicle.Exists()) _suspectVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Kidnapping] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed",
                "Kidnapping scene cleared and code 4 confirmed.");
        }
    }
}