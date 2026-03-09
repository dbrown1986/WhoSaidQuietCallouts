using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// RoadRage.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  A violent road‑rage incident involving two drivers. Officer must respond and de‑escalate verbal,
    ///  physical, or flight scenarios. Optional integration adds plugin support via reflection.
    /// </summary>
    [CalloutInfo("Road Rage", CalloutProbability.Medium)]
    public class RoadRage : WSQCalloutBase
    {
        private Vector3 _sceneLocation;
        private Vehicle _vehA;
        private Vehicle _vehB;
        private Ped _driverA;
        private Ped _driverB;
        private Blip _sceneBlip;
        private Blip _routeBlip;

        private bool _sceneActive;
        private bool _handled;
        private bool _pursuitStarted;
        private LHandle _pursuit;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(player.Around(500f));

                CalloutMessage = "Road Rage Disturbance Reported";
                CalloutPosition = _sceneLocation;
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 60f);
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT TRAFFIC_DISTURBANCE IN_OR_ON_POSITION", _sceneLocation);

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~y~Road Rage",
                    "Caller reports two drivers fighting in the street.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][RoadRage] Callout accepted.");
            try
            {
                _vehA = new Vehicle("FUTO", _sceneLocation.Around(4f));
                _vehB = new Vehicle("PENUMBRA", _sceneLocation.Around(8f));
                _vehA.IsPersistent = true;
                _vehB.IsPersistent = true;

                _driverA = _vehA.CreateRandomDriver();
                _driverB = _vehB.CreateRandomDriver();

                if (!_driverA || !_driverB)
                {
                    Game.LogTrivial("[WSQ][RoadRage] Driver spawn failed—aborting callout.");
                    PlayerControlledEnd();
                    return false;
                }

                _driverA.IsPersistent = true;
                _driverB.IsPersistent = true;
                _driverA.BlockPermanentEvents = false;
                _driverB.BlockPermanentEvents = false;

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_sceneLocation, 40f)
                    {
                        Color = System.Drawing.Color.Orange,
                        Name = "Road Rage Scene",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the road rage scene.");
                    _routeBlip = _sceneBlip;
                }
                else
                {
                    Blip route = new Blip(_sceneLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Road Rage Scene"
                    };
                    route.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~y~road rage~s~ scene.");
                    _routeBlip = route;
                }

                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
                Game.DisplayHelp("Respond Code 2 to the ~y~road rage scene~s~ and de‑escalate if possible.");
                _sceneActive = true;

                // Optional backup support via reflection or plugin
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _sceneLocation,
                        "Patrol Unit Code 2 – Assist Road Rage");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] OnCalloutAccepted Exception: " + ex.Message);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _handled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_sceneLocation);

            if (distance < 30f && !_pursuitStarted)
            {
                int behavior = _rng.Next(0, 100);

                if (behavior < 50)
                {
                    Game.DisplaySubtitle("~y~Drivers are arguing loudly — de‑escalate the situation.");
                    float headingA = (_driverB.Position - _driverA.Position).ToHeading();
                    float headingB = (_driverA.Position - _driverB.Position).ToHeading();
                    _driverA.Tasks.AchieveHeading(headingA, 2000);
                    _driverB.Tasks.AchieveHeading(headingB, 2000);
                }
                else if (behavior < 80)
                {
                    Game.DisplaySubtitle("~r~Drivers engaged in a physical fight!");
                    _driverA.Inventory.GiveNewWeapon("WEAPON_UNARMED", 0, true);
                    _driverB.Inventory.GiveNewWeapon("WEAPON_UNARMED", 0, true);
                    _driverA.Tasks.FightAgainst(_driverB);
                    _driverB.Tasks.FightAgainst(_driverA);
                    Functions.PlayScannerAudio("ASSAULT_WITH_A_DEADLY_WEAPON");

                    // Optional StopThePed AI calm
                    if (PluginBridge.IsPluginLoaded("StopThePed"))
                    {
                        PluginBridge.TryInvoke(
                            "StopThePed",
                            "StopThePed.API.Functions",
                            "CalmNearbyPeds");
                    }
                }
                else
                {
                    Game.LogTrivial("[WSQ][RoadRage] A suspect is fleeing — initiating pursuit.");
                    StartPursuit();
                }
            }

            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuit))
            {
                Game.DisplaySubtitle("~g~Pursuit concluded. Scene is secure.");
                HandleCompletion();
            }

            if (_driverA && _driverB && !_pursuitStarted)
            {
                bool A_done = !_driverA.IsAlive || Functions.IsPedArrested(_driverA);
                bool B_done = !_driverB.IsAlive || Functions.IsPedArrested(_driverB);
                if (A_done && B_done) HandleCompletion();
            }

            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 600f)
            {
                Game.DisplayHelp("You left the incident area. Press ~y~END~s~ to close the callout.");
                PlayerControlledEnd();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Ped fleeing = (_rng.Next(0, 2) == 0) ? _driverA : _driverB;
                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, fleeing);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Vehicle Pursuit",
                    "Suspect fleeing scene — engage with caution.");
                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_ELUDING_POLICE UNITS_RESPOND_CODE_3");

                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _sceneLocation, "Pursuit Assistance – Code 3");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] StartPursuit Exception: " + ex.Message);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                _handled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                    "Dispatch", "Callout Completed", "Road rage scene resolved.");
                Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] HandleCompletion Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][RoadRage] Cleanup entities.");
            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_vehA != null && _vehA.Exists()) _vehA.Dismiss();
                if (_vehB != null && _vehB.Exists()) _vehB.Dismiss();
                if (_driverA != null && _driverA.Exists()) _driverA.Dismiss();
                if (_driverB != null && _driverB.Exists()) _driverB.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _handled = true;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "All units Code 4.");
        }
    }
}