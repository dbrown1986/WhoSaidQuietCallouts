using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// ArmedRobbery.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// Description:
    ///  Classic Armed Robbery scenario where multiple armed suspects are holding up a business.
    ///  Player must respond, secure the area, and neutralize threats while ensuring civilian safety.
    /// </summary>
    [CalloutInfo("Armed Robbery", CalloutProbability.High)]
    public class ArmedRobbery : Callout
    {
        private Vector3 _spawnPoint;
        private Blip _sceneBlip;
        private List<Ped> _suspects = new List<Ped>();
        private List<Ped> _civilians = new List<Ped>();
        private Vehicle _getawayVehicle;

        private bool _sceneActive;
        private bool _callHandled;
        private int _suspectCount;

        public override bool OnBeforeCalloutDisplayed()
        {
            // Generate a callout location at a random business near the player.
            Vector3 playerPos = Game.LocalPlayer.Character.Position;
            _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(400f));

            // Set callout message and position
            ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 75f);
            CalloutMessage = "Reports of an Armed Robbery in Progress";
            CalloutPosition = _spawnPoint;

            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_ARMED_ROBBERY IN_OR_ON_POSITION", _spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][ArmedRobbery] Callout accepted.");

                // Create suspects and civilians near the store area
                _suspectCount = new Random().Next(2, 4);
                for (int i = 0; i < _suspectCount; i++)
                {
                    Ped suspect = new Ped("G_M_Y_Lost_01", _spawnPoint.Around(3f), 0f);
                    suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 90, true);
                    suspect.BlockPermanentEvents = false;
                    _suspects.Add(suspect);
                }

                // Add civilians (store clerks or customers)
                for (int i = 0; i < 2; i++)
                {
                    Ped civ = new Ped("A_M_Y_Business_02", _spawnPoint.Around(2f), 0f);
                    civ.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    civ.BlockPermanentEvents = true;
                    _civilians.Add(civ);
                }

                // Create robbery getaway vehicle nearby
                _getawayVehicle = new Vehicle("SULTAN", _spawnPoint.Around(10f));
                _getawayVehicle.IsPersistent = true;

                // Create blip on the location
                _sceneBlip = new Blip(_spawnPoint, 40f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Armed Robbery Scene",
                    Alpha = 0.8f
                };

                _sceneActive = true;
                _callHandled = false;

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Armed Robbery", "Proceed with caution. Suspects are reportedly ~r~armed and dangerous~w~.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ArmedRobbery] Exception during OnCalloutAccepted: " + ex);
                End();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;

            // If all suspects neutralized, mark the call as handled
            bool suspectsAlive = _suspects.Exists(s => s && s.IsAlive);
            if (!suspectsAlive)
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~All suspects neutralized. Secure the scene and await further instructions.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY THAT");
                End();
            }

            // If player becomes too far away, terminate
            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 500f)
            {
                Game.DisplayHelp("You have left the callout area. The scene has been cleared.");
                End();
            }
        }

        public override void End()
        {
            Game.LogTrivial("[WSQ][ArmedRobbery] Cleaning up entities.");
            base.End();

            try
            {
                _sceneActive = false;

                foreach (var ped in _suspects)
                {
                    if (ped && ped.Exists()) ped.Dismiss();
                }
                foreach (var civ in _civilians)
                {
                    if (civ && civ.Exists()) civ.Dismiss();
                }

                if (_getawayVehicle && _getawayVehicle.Exists()) _getawayVehicle.Dismiss();
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ArmedRobbery] Cleanup exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Armed Robbery scene cleared. Good work, officer.");
        }
    }
}
