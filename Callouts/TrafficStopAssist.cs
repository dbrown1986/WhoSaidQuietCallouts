using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// TrafficStopAssist.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  An officer has initiated a traffic stop and requests backup (Code 2).
    ///  Upon arrival the situation may develop as compliance, foot pursuit, or armed attack.
    ///  Includes reflective integration support for UltimateBackup and StopThePed.
    /// </summary>
    [CalloutInfo("Traffic Stop Assist", CalloutProbability.Medium)]
    public class TrafficStopAssist : WSQCalloutBase
    {
        private Vector3 _scenePosition;
        private Vehicle _suspectVehicle;
        private Ped _suspect;
        private Ped _backupOfficer;
        private Blip _sceneBlip;
        private Blip _routeBlip;

        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;
        private LHandle _pursuit;

        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _scenePosition = World.GetNextPositionOnStreet(playerPos.Around(600f));

                CalloutMessage = "Officer Requests Assistance With Traffic Stop";
                CalloutPosition = _scenePosition;
                ShowCalloutAreaBlipBeforeAccepting(_scenePosition, 75f);

                Functions.PlayScannerAudioUsingPosition("OFFICER_REQUEST_BACKUP TRAFFIC_STOP IN_OR_ON_POSITION", _scenePosition);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~b~Traffic Stop Assist", "Officer requires Code 2 backup for an ongoing traffic stop.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][TrafficStopAssist] Callout accepted.");
            try
            {
                _suspectVehicle = new Vehicle("PREMIER", _scenePosition);
                _suspectVehicle.IsPersistent = true;

                _suspect = _suspectVehicle.CreateRandomDriver();
                if (!_suspect || !_suspect.Exists())
                {
                    PlayerControlledEnd();
                    return false;
                }
                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;

                _backupOfficer = new Ped("S_M_Y_Cop_01", _scenePosition.Around(3f), 180f);
                if (!_backupOfficer || !_backupOfficer.Exists())
                {
                    PlayerControlledEnd();
                    return false;
                }

                _backupOfficer.IsPersistent = true;
                _backupOfficer.BlockPermanentEvents = false;
                Functions.SetPedAsCop(_backupOfficer);
                _backupOfficer.Tasks.AimWeaponAt(_suspect, -1);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_scenePosition, 40f)
                    {
                        Color = System.Drawing.Color.Blue,
                        Name = "Traffic Stop Assist",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the officer in need of assistance.");
                    _routeBlip = _sceneBlip;
                }
                else
                {
                    Blip gpsRoute = new Blip(_scenePosition)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Traffic Stop Assist"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~b~traffic stop assist~s~ location.");
                    _routeBlip = gpsRoute;
                }

                _sceneActive = true;
                Game.DisplayHelp("Respond Code 2 to assist the officer on ~b~traffic stop~s~.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");

                // Optional backup request via UltimateBackup
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _scenePosition,
                        "Patrol Unit Code 2 – Traffic Stop Assist");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_scenePosition);

            // Arrival triggers random scenario
            if (distance < 40f && !_pursuitStarted)
            {
                int roll = _rng.Next(0, 100);
                if (roll < 50)
                {
                    Game.DisplaySubtitle("~y~Officer: Thanks for the assist, suspect is cooperative.", 4000);
                    _suspect.Tasks.LeaveVehicle(_suspectVehicle, LeaveVehicleFlags.None);
                    _suspect.Tasks.PutHandsUp(-1, _backupOfficer);
                    _backupOfficer.Tasks.AimWeaponAt(_suspect, -1);
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUSPECT_IN_CUSTODY");

                    _callHandled = true;
                    Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                    GameFiber.StartNew(delegate
                    {
                        CalloutUtilities.WaitForPlayerEnd();
                        PlayerControlledEnd();
                    });
                }
                else if (roll < 80)
                {
                    Game.DisplaySubtitle("~r~Suspect fleeing on foot! Join the pursuit!", 4000);
                    _suspect.Tasks.Flee(Game.LocalPlayer.Character.Position, 200f, -1);
                    StartFootPursuit();
                }
                else
                {
                    Game.DisplaySubtitle("~r~Suspect draws a weapon!", 4000);
                    _suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 60, true);
                    _suspect.Tasks.FightAgainstClosestHatedTarget(100f);
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
                Game.DisplaySubtitle("~g~Suspect apprehended. Call cleared.", 4000);
                _callHandled = true;
                Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            if (Game.LocalPlayer.Character.DistanceTo(_scenePosition) > 600f)
            {
                Game.DisplayHelp("You left the area. Press ~y~END~s~ to close this callout.");
                PlayerControlledEnd();
            }
        }

        private void StartFootPursuit()
        {
            try
            {
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;

                Functions.PlayScannerAudio("SUSPECT_FLEEING_ON_FOOT");
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _scenePosition,
                        "Foot Pursuit Assistance – Code 3");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] StartFootPursuit Exception: " + ex.Message);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][TrafficStopAssist] Cleaning up scene entities.");
            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_sceneBlip != null && _sceneBlip.Exists()) _sceneBlip.Delete();
                if (_suspect != null && _suspect.Exists()) _suspect.Dismiss();
                if (_backupOfficer != null && _backupOfficer.Exists()) _backupOfficer.Dismiss();
                if (_suspectVehicle != null && _suspectVehicle.Exists()) _suspectVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Traffic stop assist successfully resolved. Code 4.");
        }
    }
}