using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;  // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// StolenPoliceVehicle.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  A marked law‑enforcement vehicle has been stolen. Officer must locate and recover the unit.
    ///  Scenario may involve a traffic stop, pursuit, or armed confrontation.
    ///  Reflective integration adds support for UltimateBackup and StopThePed without extra dependencies.
    /// </summary>
    [CalloutInfo("Stolen Police Vehicle", CalloutProbability.Medium)]
    public class StolenPoliceVehicle : WSQCalloutBase
    {
        private Vector3 _spawnPoint;
        private Vehicle _stolenUnit;
        private Ped _suspect;
        private Blip _vehicleBlip;
        private Blip _routeBlip;
        private LHandle _pursuit;

        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(700f));

                CalloutMessage = "Stolen Police Vehicle Reported";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 80f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT STOLEN_POLICE_VEHICLE IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Stolen Police Vehicle",
                    "A marked patrol unit has been stolen. Locate and recover the vehicle.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][StolenPoliceVehicle] Callout accepted.");
            try
            {
                _stolenUnit = new Vehicle("POLICE3", _spawnPoint);
                _stolenUnit.IsPersistent = true;

                _suspect = _stolenUnit.CreateRandomDriver();
                if (!_suspect || !_suspect.Exists())
                {
                    PlayerControlledEnd();
                    return false;
                }

                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;
                _suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 70, true);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    var radarBlip = new Blip(_spawnPoint, 80f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Stolen Unit Area",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Search the area for the stolen police unit.");
                    _routeBlip = radarBlip;
                }
                else
                {
                    var gpsBlip = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Stolen Unit"
                    };
                    gpsBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~stolen police vehicle~s~ location.");
                    _routeBlip = gpsBlip;
                }

                // ─── Attach vehicle tracker ───
                _vehicleBlip = _stolenUnit.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Red;
                _vehicleBlip.Name = "Stolen Police Vehicle";

                _sceneActive = true;
                Game.DisplayHelp("Locate the ~r~stolen police vehicle~s~ and recover it safely.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                // Optional UltimateBackup support
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _spawnPoint,
                        "Air Unit and Ground Patrol Response");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;
            if (!_stolenUnit || !_suspect) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            if (distance < 60f && !_pursuitStarted)
            {
                int behavior = _rng.Next(0, 100);

                if (behavior < 50)
                {
                    Game.DisplaySubtitle("~y~Suspect slowing down — initiate a traffic stop and detain driver.");
                    _suspect.Tasks.CruiseWithVehicle(10f, VehicleDrivingFlags.FollowTraffic);
                }
                else if (behavior < 85)
                {
                    StartPursuit();
                }
                else
                {
                    Game.DisplaySubtitle("~r~Suspect exiting vehicle with a weapon!");
                    _suspect.Tasks.LeaveVehicle(_stolenUnit, LeaveVehicleFlags.LeaveDoorOpen);
                    _suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");

                    if (PluginBridge.IsPluginLoaded("StopThePed"))
                    {
                        PluginBridge.TryInvoke(
                            "StopThePed",
                            "StopThePed.API.Functions",
                            "CalmNearbyPeds");
                    }
                }
            }

            if (_pursuitStarted && _pursuit != null && !Functions.IsPursuitStillRunning(_pursuit))
            {
                HandleRecovery();
            }

            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 800f)
            {
                Game.DisplayHelp("You left the incident area. Press ~y~END~s~ to close the callout.");
                PlayerControlledEnd();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] Pursuit initiated.");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;

                Functions.PlayScannerAudio("WE_HAVE SUSPECT_FLEEING_CODE_3");
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "Dispatch", "~r~Vehicle Pursuit",
                    "Suspect is fleeing in the stolen unit — use extreme caution.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] StartPursuit Exception: " + ex.Message);
            }
        }

        private void HandleRecovery()
        {
            try
            {
                _callHandled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplaySubtitle("~g~Stolen unit recovered. Good work officer.", 4000);
                Game.DisplayHelp("Press ~y~END~s~ to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] HandleRecovery Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][StolenPoliceVehicle] Cleaning up scene entities.");

            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_vehicleBlip != null && _vehicleBlip.Exists()) _vehicleBlip.Delete();
                if (_suspect != null && _suspect.Exists()) _suspect.Dismiss();
                if (_stolenUnit != null && _stolenUnit.Exists()) _stolenUnit.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed",
                "Stolen police vehicle recovered successfully. Code 4.");
        }
    }
}