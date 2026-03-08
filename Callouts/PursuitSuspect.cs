using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// PursuitSuspect.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  An ongoing vehicle pursuit has been reported. The player officer must assist in tracking,
    ///  intercepting, and apprehending the fleeing suspect vehicle before it escapes the area.
    /// </summary>
    [CalloutInfo("Pursuit Suspect", CalloutProbability.High)]
    public class PursuitSuspect : Callout
    {
        private Vehicle _suspectVehicle;
        private Ped _suspect;
        private LHandle _pursuit;
        private Blip _callBlip;

        private Vector3 _spawnPoint;
        private bool _pursuitCreated;
        private bool _pursuitEnded;

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(600f));

                CalloutMessage = "A suspect is fleeing officers in a vehicle";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 75f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_RESIST_ARREST IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Dispatch", "~r~Vehicle Pursuit", "Assist other units in pursuit of fleeing suspect.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] Exception in OnBeforeCalloutDisplayed: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][PursuitSuspect] Callout accepted.");
            try
            {
                // Create suspect vehicle a few hundred meters away
                _suspectVehicle = new Vehicle("BUFFALO", _spawnPoint);
                _suspectVehicle.IsPersistent = true;

                _suspect = _suspectVehicle.CreateRandomDriver();
                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;
                _suspect.RelationshipGroup = "SUSPECT";
                _suspect.Tasks.CruiseWithVehicle(25f, VehicleDrivingFlags.Normal);

                _callBlip = _suspect.AttachBlip();
                _callBlip.Color = System.Drawing.Color.Red;
                _callBlip.Name = "Pursuit Suspect";
                _callBlip.IsFriendly = false;

                // Create pursuit
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);

                _pursuitCreated = true;
                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_ELUDING_POLICE UNITS_RESPOND_CODE_3");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] Exception in OnCalloutAccepted: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_pursuitCreated || _pursuitEnded) return;

            // Check if pursuit concluded
            if (!Functions.IsPursuitStillRunning(_pursuit))
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] Pursuit ended per game logic.");
                HandlePursuitEnd();
            }

            // Optional distance check for failure
            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 700f)
            {
                Game.DisplayHelp("You are too far from the pursuit area. Dispatch has reassigned this call.");
                HandlePursuitEnd();
            }
        }

        private void HandlePursuitEnd()
        {
            try
            {
                _pursuitEnded = true;
                Game.DisplaySubtitle("~g~Pursuit concluded. Scene secure or suspect in custody.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                End();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] HandlePursuitEnd exception: " + ex);
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][PursuitSuspect] Cleaning up entities.");

            try
            {
                if (_callBlip && _callBlip.Exists()) _callBlip.Delete();
                if (_suspectVehicle && _suspectVehicle.Exists()) _suspectVehicle.Dismiss();
                if (_suspect && _suspect.Exists()) _suspect.Dismiss();

                _pursuitCreated = false;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] Cleanup exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Vehicle pursuit assistance complete.");
        }
    }
}
