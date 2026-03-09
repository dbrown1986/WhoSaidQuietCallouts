using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;  // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// VIPEscort.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  Escort a high‑profile individual to safety. Maintain motorcade protection and react to possible ambushes.
    ///  Adds reflective integration for backup and AI behavior control.
    /// </summary>
    [CalloutInfo("VIP Escort", CalloutProbability.Medium)]
    public class VIPEscort : WSQCalloutBase
    {
        private Vector3 _pickupLocation;
        private Vector3 _destination;
        private Vehicle _vipVehicle;
        private Ped _vip;
        private Blip _pickupBlip;
        private Blip _destinationBlip;
        private Blip _routeBlip;

        private bool _escortStarted;
        private bool _sceneActive;
        private bool _callHandled;

        private readonly Random _rng = new Random();
        private readonly List<Ped> _attackers = new List<Ped>();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _pickupLocation = World.GetNextPositionOnStreet(player.Around(600f));
                _destination = World.GetNextPositionOnStreet(player.Around(1200f));

                CalloutMessage = "VIP Escort Requested";
                CalloutPosition = _pickupLocation;
                ShowCalloutAreaBlipBeforeAccepting(_pickupLocation, 100f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT VIP_REQUIRING_ESCORT IN_OR_ON_POSITION", _pickupLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~b~VIP Escort", "Protect the assigned VIP and escort them safely to the destination.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][VIPEscort] Callout accepted.");
            try
            {
                _vipVehicle = new Vehicle("SCHAFTER2", _pickupLocation);
                _vipVehicle.IsPersistent = true;

                _vip = _vipVehicle.CreateRandomDriver();
                if (!_vip || !_vip.Exists())
                {
                    PlayerControlledEnd();
                    return false;
                }

                _vip.IsPersistent = true;
                _vip.BlockPermanentEvents = false;
                _vip.Inventory.GiveNewWeapon("WEAPON_PISTOL", 60, true);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _pickupBlip = new Blip(_pickupLocation, 100f)
                    {
                        Color = System.Drawing.Color.Blue,
                        Name = "VIP Pickup Zone",
                        Alpha = 0.7f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to pick up the VIP.");
                    _routeBlip = _pickupBlip;
                }
                else
                {
                    Blip gpsRoute = new Blip(_pickupLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to VIP Pickup"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~b~VIP pickup point~s~.");
                    _routeBlip = gpsRoute;
                }

                _sceneActive = true;
                Game.DisplayHelp("Proceed to the ~b~pickup location~s~ and begin the escort operation.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");

                // Optional backup support
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _pickupLocation,
                        "Motor Patrol Escort Assistance Code 2");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float playerDist = Game.LocalPlayer.Character.DistanceTo(_pickupLocation);

            // ─── Begin Escort ───
            if (!_escortStarted && playerDist < 30f)
            {
                _escortStarted = true;
                Game.DisplaySubtitle("~b~VIP ready for escort — follow them to the destination.", 4000);
                Functions.PlayScannerAudio("UNITS_BEGIN_ESCORT");

                // Replace route to destination
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();

                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _destinationBlip = new Blip(_destination, 100f)
                    {
                        Color = System.Drawing.Color.Cyan,
                        Name = "Destination Area",
                        Alpha = 0.7f
                    };
                    Game.DisplayHelp("Radar blip set for VIP destination.");
                    _routeBlip = _destinationBlip;
                }
                else
                {
                    Blip gpsDest = new Blip(_destination)
                    {
                        Color = System.Drawing.Color.LightBlue,
                        Name = "GPS Route to Destination"
                    };
                    gpsDest.IsRouteEnabled = true;
                    _routeBlip = gpsDest;
                    Game.DisplayHelp("GPS route set to the VIP destination.");
                }

                _vip.Tasks.DriveToPosition(_destination, 25f, VehicleDrivingFlags.Normal);
            }

            // ─── Potential ambush event ───
            if (_escortStarted && _vipVehicle != null && _vipVehicle.Exists() &&
                _vipVehicle.DistanceTo(_destination) < 600f && _attackers.Count == 0)
            {
                if (_rng.Next(0, 100) > 70)
                    StartAmbushEvent();
            }

            // ─── Arrival — successful completion ───
            if (_escortStarted && _vipVehicle != null && _vipVehicle.Exists() &&
                _vipVehicle.DistanceTo(_destination) < 50f)
            {
                HandleCompletion();
            }

            // ─── Player abandons route ───
            if (Game.LocalPlayer.Character.DistanceTo(_pickupLocation) > 1000f)
            {
                Game.DisplayHelp("You left the escort route. Press ~y~END~s~ to close this callout.");
                PlayerControlledEnd();
            }
        }

        private void StartAmbushEvent()
        {
            try
            {
                Game.LogTrivial("[WSQ][VIPEscort] Ambush triggered.");
                Functions.PlayScannerAudio("SHOTS_FIRED_IN_TRANSIT");

                for (int i = 0; i < 3; i++)
                {
                    Ped attacker = new Ped("G_M_Y_Lost_02", _vipVehicle.GetOffsetPositionFront(20f).Around(5f), _rng.Next(0, 359));
                    attacker.Inventory.GiveNewWeapon("WEAPON_SMG", 150, true);
                    attacker.IsPersistent = true;
                    attacker.BlockPermanentEvents = false;
                    attacker.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    _attackers.Add(attacker);
                }

                if (PluginBridge.IsPluginLoaded("StopThePed"))
                {
                    PluginBridge.TryInvoke(
                        "StopThePed",
                        "StopThePed.API.Functions",
                        "CalmNearbyPeds");
                }

                Game.DisplaySubtitle("~r~Ambush! Protect the VIP!", 4000);
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] StartAmbushEvent Exception: " + ex);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                _callHandled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT VIP_SECURE");
                Game.DisplaySubtitle("~g~VIP secure. Escort operation complete.", 4000);
                Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] HandleCompletion Exception: " + ex.Message);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][VIPEscort] Cleaning up entities.");
            try
            {
                if (_pickupBlip != null && _pickupBlip.Exists()) _pickupBlip.Delete();
                if (_destinationBlip != null && _destinationBlip.Exists()) _destinationBlip.Delete();
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_vip != null && _vip.Exists()) _vip.Dismiss();
                if (_vipVehicle != null && _vipVehicle.Exists()) _vipVehicle.Dismiss();
                foreach (Ped attacker in _attackers)
                {
                    if (attacker != null && attacker.Exists()) attacker.Dismiss();
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][VIPEscort] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "VIP escort mission completed. Code 4.");
        }
    }
}