using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// TrafficStopAssist.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  An officer has initiated a traffic stop and is requesting Code 2 backup.
    ///  Upon arrival, the situation can develop in multiple ways: 
    ///  compliant suspects, resistance, or immediate pursuit.
    /// </summary>
    [CalloutInfo("Traffic Stop Assist", CalloutProbability.Medium)]
    public class TrafficStopAssist : Callout
    {
        private Vector3 _scenePosition;
        private Vehicle _suspectVehicle;
        private Ped _suspect;
        private Ped _backupOfficer;
        private Blip _sceneBlip;

        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;
        private LHandle _pursuit;

        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _scenePosition = World.GetNextPositionOnStreet(playerPos.Around(600f));

                CalloutMessage = "Officer Requests Assistance With Traffic Stop";
                CalloutPosition = _scenePosition;
                ShowCalloutAreaBlipBeforeAccepting(_scenePosition, 75f);

                Functions.PlayScannerAudioUsingPosition("OFFICER_REQUEST_BACKUP TRAFFIC_STOP IN_OR_ON_POSITION", _scenePosition);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~b~Traffic Stop Assist", "Officer requires Code 2 backup for an ongoing traffic stop.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][TrafficStopAssist] Callout accepted.");
            try
            {
                // Create base scene: parked vehicle + officer + suspect
                _suspectVehicle = new Vehicle("PREMIER", _scenePosition)
                {
                    IsPersistent = true
                };

                _suspect = _suspectVehicle.CreateRandomDriver();
                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;

                // Spawn backup officer beside suspect vehicle
                _backupOfficer = new Ped("S_M_Y_Cop_01", _scenePosition.Around(3f), 180f);
                _backupOfficer.IsPersistent = true;
                _backupOfficer.BlockPermanentEvents = false;
                Functions.SetPedAsCop(_backupOfficer, true);
                _backupOfficer.Tasks.AimWeaponAt(_suspect, -1);

                // Scene marker
                _sceneBlip = new Blip(_scenePosition, 40f)
                {
                    Color = System.Drawing.Color.Blue,
                    Name = "Traffic Stop Assist",
                    Alpha = 0.8f
                };

                Game.DisplayHelp("Respond Code 2 to assist the officer on ~b~traffic stop~w~.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] OnCalloutAccepted Exception: " + ex);
                End();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_scenePosition);

            // Player approach triggers possible outcomes
            if (distance < 40f && !_pursuitStarted)
            {
                int roll = _rng.Next(0, 100);

                if (roll < 50)
                {
                    // Compliant suspect
                    Game.DisplaySubtitle("~y~Officer: Thanks for the assist, suspect is cooperative.");
                    _suspect.Tasks.LeaveVehicle(_suspectVehicle, LeaveVehicleFlags.None);
                    _suspect.Tasks.PutHandsUp(-1, _backupOfficer);
                    _backupOfficer.Tasks.AimWeaponAt(_suspect, -1);
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUSPECT_IN_CUSTODY");
                    _callHandled = true;
                    End();
                }
                else if (roll < 80)
                {
                    // Sudden foot pursuit
                    Game.DisplaySubtitle("~r~Suspect fleeing on foot! Assist with the pursuit!");
                    _suspect.Tasks.Flee(Game.LocalPlayer.Character);
                    StartFootPursuit();
                }
                else
                {
                    // Suspect attack
                    Game.DisplaySubtitle("~r~Suspect draws a weapon!");
                    _suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 60, true);
                    _suspect.Tasks.FightAgainstClosestHatedTarget(100f);
                    Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");
                }
            }

            // End if pursuit finishes
            if (_pursuitStarted && _pursuit != null && !Functions.IsPursuitStillRunning(_pursuit))
            {
                Game.DisplaySubtitle("~g~Suspect apprehended. Call cleared.");
                _callHandled = true;
                End();
            }

            // Too far -> Auto cancel
            if (Game.LocalPlayer.Character.DistanceTo(_scenePosition) > 600f)
            {
                Game.DisplayHelp("You left the area. The stop has concluded without your assistance.");
                End();
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
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] StartFootPursuit Exception: " + ex.Message);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][TrafficStopAssist] Cleaning up entities.");
            try
            {
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();

                if (_suspect && _suspect.Exists()) _suspect.Dismiss();
                if (_backupOfficer && _backupOfficer.Exists()) _backupOfficer.Dismiss();
                if (_suspectVehicle && _suspectVehicle.Exists()) _suspectVehicle.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][TrafficStopAssist] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Traffic stop assist handled successfully.");
        }
    }
}
