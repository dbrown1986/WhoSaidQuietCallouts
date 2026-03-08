using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// StolenPoliceVehicle.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  A marked law enforcement vehicle has been reported stolen.
    ///  Player officer must locate the suspect, initiate a stop or pursuit,
    ///  and recover the stolen police car.  May escalate into an armed confrontation.
    /// </summary>
    [CalloutInfo("Stolen Police Vehicle", CalloutProbability.Medium)]
    public class StolenPoliceVehicle : Callout
    {
        private Vector3 _spawnPoint;
        private Vehicle _stolenUnit;
        private Ped _suspect;
        private Blip _vehicleBlip;
        private LHandle _pursuit;
        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(700f));

                CalloutMessage = "Stolen Police Vehicle Reported";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 80f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT STOLEN_POLICE_VEHICLE IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Stolen Police Vehicle", "A marked patrol unit has been stolen. Locate and recover the vehicle.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][StolenPoliceVehicle] Callout accepted.");
            try
            {
                // Create stolen police car and suspect driver
                _stolenUnit = new Vehicle("POLICE3", _spawnPoint)
                {
                    IsPersistent = true
                };

                _suspect = _stolenUnit.CreateRandomDriver();
                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;

                _suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 70, true);

                // Attach tracking blip
                _vehicleBlip = _stolenUnit.AttachBlip();
                _vehicleBlip.Color = System.Drawing.Color.Red;
                _vehicleBlip.Name = "Stolen Police Vehicle";

                _sceneActive = true;
                Game.DisplayHelp("Locate the ~r~stolen police vehicle~w~ and recover it safely.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;
            if (!_stolenUnit || !_suspect) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);

            // Behavior when officer closes in
            if (distance < 60f && !_pursuitStarted)
            {
                int behavior = _rng.Next(0, 100);
                if (behavior < 50)
                {
                    // Compliant suspect pulls over
                    Game.DisplaySubtitle("~y~Suspect slowing down. Initiate traffic stop and detain driver.");
                    _suspect.Tasks.CruiseWithVehicle(10f, VehicleDrivingFlags.FollowTraffic);
                }
                else if (behavior < 85)
                {
                    // Fleeing suspect (pursuit)
                    StartPursuit();
                }
                else
                {
                    // Armed confrontation – stops & shoots
                    Game.DisplaySubtitle("~r~Suspect exiting vehicle with a weapon!");
                    _suspect.Tasks.LeaveVehicle(_stolenUnit, LeaveVehicleFlags.None);
                    _suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");
                }
            }

            // Check pursuit resolution
            if (_pursuitStarted && _pursuit != null && !Functions.IsPursuitStillRunning(_pursuit))
            {
                Game.DisplaySubtitle("~g~Pursuit over. Vehicle recovered.");
                _callHandled = true;
                End();
            }

            // Failsafe distance cancel
            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 800f)
            {
                Game.DisplayHelp("You left the incident area. Dispatch has cleared your call.");
                End();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] Pursuit initiated.");
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;
                Functions.PlayScannerAudio("WE_HAVE SUSPECT_FLEEING_CODE_3");
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Vehicle Pursuit", "Suspect is fleeing in the stolen police unit!");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] StartPursuit Exception: " + ex.Message);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][StolenPoliceVehicle] Cleaning up entities.");

            try
            {
                if (_vehicleBlip && _vehicleBlip.Exists()) _vehicleBlip.Delete();
                if (_suspect && _suspect.Exists()) _suspect.Dismiss();
                if (_stolenUnit && _stolenUnit.Exists()) _stolenUnit.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][StolenPoliceVehicle] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Stolen police vehicle recovered successfully.");
        }
    }
}
