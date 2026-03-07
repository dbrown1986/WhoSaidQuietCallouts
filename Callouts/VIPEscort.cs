using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// VIPEscort.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  A high-profile individual requires safe escort from one location to another.
    ///  Player must provide motorcade protection while ensuring the VIP’s vehicle
    ///  reaches the destination safely.  Possible threats may appear en route.
    /// </summary>
    [CalloutInfo("VIP Escort", CalloutProbability.Medium)]
    public class VIPEscort : Callout
    {
        private Vector3 _pickupLocation;
        private Vector3 _destination;
        private Vehicle _vipVehicle;
        private Ped _vip;
        private Blip _pickupBlip;
        private Blip _destinationBlip;
        private bool _escortStarted;
        private bool _sceneActive;
        private bool _callHandled;
        private Random _rng = new Random();
        private List<Ped> _attackers = new List<Ped>();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _pickupLocation = World.GetNextPositionOnStreet(playerPos.Around(600f));
                _destination = World.GetNextPositionOnStreet(playerPos.Around(1200f));

                CalloutMessage = "VIP Escort Requested";
                CalloutPosition = _pickupLocation;
                ShowCalloutAreaBlipBeforeAccepting(_pickupLocation, 100f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT VIP_REQUIRING_ESCORT IN_OR_ON_POSITION", _pickupLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~b~VIP Escort", "Protect the assigned VIP and escort them safely to the destination.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][VIPEscort] Callout accepted.");
            try
            {
                // Create VIP vehicle and passenger
                _vipVehicle = new Vehicle("SCHAFTER2", _pickupLocation)
                {
                    IsPersistent = true
                };

                _vip = _vipVehicle.CreateRandomDriver();
                _vip.IsPersistent = true;
                _vip.BlockPermanentEvents = false;

                _vip.Inventory.GiveNewWeapon("WEAPON_PISTOL", 60, true);
                _pickupBlip = _vipVehicle.AttachBlip();
                _pickupBlip.Color = System.Drawing.Color.Blue;
                _pickupBlip.Name = "VIP Pickup Point";

                _sceneActive = true;

                Game.DisplayHelp("Proceed to the ~b~pickup location~w~ and meet the VIP for escort duty.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled)
                return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_pickupLocation);

            // Begin escort when near VIP
            if (!_escortStarted && distance < 30f)
            {
                _escortStarted = true;
                Game.DisplaySubtitle("~b~VIP ready for escort. Follow the vehicle to the destination safely.");
                Functions.PlayScannerAudio("UNITS_BEGIN_ESCORT");
                _destinationBlip = new Blip(_destination)
                {
                    Color = System.Drawing.Color.LightBlue,
                    Name = "Destination"
                };

                // Have VIP start driving toward destination
                _vip.Tasks.DriveToPosition(_destination, 25f, VehicleDrivingFlags.Normal);
            }

            // Dynamic event chance en route
            if (_escortStarted && _vipVehicle && _vipVehicle.Exists() && _vipVehicle.DistanceTo(_destination) < 600f && _attackers.Count == 0)
            {
                int chance = _rng.Next(0, 100);
                if (chance > 70) // 30% chance for ambush
                {
                    StartAmbushEvent();
                }
            }

            // Escort complete
            if (_escortStarted && _vipVehicle && _vipVehicle.Exists() && _vipVehicle.DistanceTo(_destination) < 50f)
            {
                HandleCompletion();
            }

            // Player leaves area fail-safe
            if (Game.LocalPlayer.Character.DistanceTo(_pickupLocation) > 1000f)
            {
                Game.DisplayHelp("You left the escort route. Dispatch is assigning another unit.");
                End();
            }
        }

        private void StartAmbushEvent()
        {
            try
            {
                Game.LogTrivial("[WSQ][VIPEscort] Ambush triggered!");
                Functions.PlayScannerAudio("SHOTS_FIRED_IN_TRANSIT");

                // Spawn attackers
                for (int i = 0; i < 3; i++)
                {
                    Ped attacker = new Ped("G_M_Y_Lost_02", _vipVehicle.GetOffsetPositionFront(20f).Around(5f), _rng.Next(0, 359));
                    attacker.Inventory.GiveNewWeapon("WEAPON_SMG", 150, true);
                    attacker.IsPersistent = true;
                    attacker.BlockPermanentEvents = false;
                    attacker.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    _attackers.Add(attacker);
                }

                Game.DisplaySubtitle("~r~Ambush! Protect the VIP!");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] StartAmbushEvent Exception: " + ex);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                _callHandled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT VIP_SECURE");
                Game.DisplaySubtitle("~g~VIP successfully reached the destination. Escort complete.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] HandleCompletion Exception: " + ex.Message);
            }

            End();
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][VIPEscort] Cleaning up entities.");

            try
            {
                if (_pickupBlip && _pickupBlip.Exists()) _pickupBlip.Delete();
                if (_destinationBlip && _destinationBlip.Exists()) _destinationBlip.Delete();

                if (_vip && _vip.Exists()) _vip.Dismiss();
                if (_vipVehicle && _vipVehicle.Exists()) _vipVehicle.Dismiss();

                foreach (Ped attacker in _attackers)
                    if (attacker && attacker.Exists()) attacker.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "VIP escort completed successfully.");
        }
    }
}
