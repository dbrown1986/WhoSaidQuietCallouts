using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// RoadRage.cs
    /// Version: 0.9.1 Alpha (Compatibility Build)
    /// Date: March 9, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  A violent road rage incident has been reported involving two or more vehicles.
    ///  Player must respond, de‑escalate the situation, and handle suspects appropriately.
    ///  Randomized outcomes include verbal disputes, physical altercations, or vehicle pursuits.
    /// </summary>
    [CalloutInfo("Road Rage", CalloutProbability.Medium)]
    public class RoadRage : WSQCalloutBase
    {
        private Vector3 _sceneLocation;
        private Vehicle _vehA;
        private Vehicle _vehB;
        private Ped _driverA;
        private Ped _driverB;
        private Blip _sceneBlip;

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

                CalloutMessage = "Road Rage Disturbance Reported";
                CalloutPosition = _sceneLocation;
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 60f);
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT TRAFFIC_DISTURBANCE IN_OR_ON_POSITION", _sceneLocation);

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~y~Road Rage",
                    "Caller reports two drivers fighting in the street.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][RoadRage] Callout accepted.");
            try
            {
                // Create both vehicles and drivers
                _vehA = new Vehicle("FUTO", _sceneLocation.Around(4f)) { IsPersistent = true };
                _vehB = new Vehicle("PENUMBRA", _sceneLocation.Around(8f)) { IsPersistent = true };

                _driverA = _vehA.CreateRandomDriver();
                _driverB = _vehB.CreateRandomDriver();

                if (!_driverA.Exists() || !_driverB.Exists())
                {
                    Game.LogTrivial("[WSQ][RoadRage] Driver spawn failed — aborting callout.");
                    End();
                    return false;
                }

                _driverA.IsPersistent = true;
                _driverA.BlockPermanentEvents = false;
                _driverB.IsPersistent = true;
                _driverB.BlockPermanentEvents = false;

                _sceneBlip = new Blip(_sceneLocation, 40f)
                {
                    Color = System.Drawing.Color.Orange,
                    Name = "Road Rage Scene",
                    Alpha = 0.8f
                };

                Game.DisplayHelp("Respond to the ~y~road rage~w~. Approach carefully — suspects may be aggressive.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] OnCalloutAccepted Exception: " + ex.Message);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _handled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_sceneLocation);

            // Behavior once player arrives close
            if (distance < 30f && !_pursuitStarted)
            {
                int behavior = _rng.Next(0, 100);

                if (behavior < 50)
                {
                    // 🗣 Verbal argument
                    Game.DisplaySubtitle("~y~Drivers are arguing loudly. De‑escalate the situation.");

                    float headingA = (_driverB.Position - _driverA.Position).ToHeading();
                    float headingB = (_driverA.Position - _driverB.Position).ToHeading();

                    _driverA.Tasks.AchieveHeading(headingA, 2000);
                    _driverB.Tasks.AchieveHeading(headingB, 2000);
                }
                else if (behavior < 80)
                {
                    // 🤜 Physical altercation
                    Game.DisplaySubtitle("~r~Drivers have started fighting!");
                    _driverA.Inventory.GiveNewWeapon("WEAPON_UNARMED", 0, true);
                    _driverB.Inventory.GiveNewWeapon("WEAPON_UNARMED", 0, true);
                    _driverA.Tasks.FightAgainst(_driverB);
                    _driverB.Tasks.FightAgainst(_driverA);
                    Functions.PlayScannerAudio("ASSAULT_WITH_A_DEADLY_WEAPON");
                }
                else
                {
                    // 🚗 One driver flees
                    Game.LogTrivial("[WSQ][RoadRage] One suspect fleeing – pursuit initiated.");
                    StartPursuit();
                }
            }

            // Scene resolution
            if (_pursuitStarted && !Functions.IsPursuitStillRunning(_pursuit))
            {
                Game.DisplaySubtitle("~g~Pursuit concluded. Scene secure.");
                HandleCompletion();
            }

            // End if both detained/dead after fight
            if (_driverA.Exists() && _driverB.Exists() && !_pursuitStarted)
            {
                bool driverANeutralized = !_driverA.IsAlive || Functions.IsPedArrested(_driverA);
                bool driverBNeutralized = !_driverB.IsAlive || Functions.IsPedArrested(_driverB);

                if (driverANeutralized && driverBNeutralized)
                    HandleCompletion();
            }

            // Leave‑area failsafe
            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 600f)
            {
                Game.DisplayHelp("You left the area. Dispatch has cleared this call.");
                End();
            }
        }

        private void StartPursuit()
        {
            try
            {
                Ped fleeingSuspect = (_rng.Next(0, 2) == 0) ? _driverA : _driverB;

                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, fleeingSuspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitStarted = true;

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "Dispatch", "~r~Vehicle Pursuit", "Suspect is fleeing the scene. Engage the pursuit!");
                Functions.PlayScannerAudio("WE_HAVE SUSPECT_ELUDING_POLICE UNITS_RESPOND_CODE_3");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] StartPursuit Exception: " + ex.Message);
            }
        }

        private void HandleCompletion()
        {
            try
            {
                _handled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                    "Dispatch", "Callout Completed", "Road rage scene resolved.");
                PlayerControlledEnd();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] HandleCompletion Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][RoadRage] Cleaning up entities.");

            try
            {
                if (_sceneBlip?.Exists() == true) _sceneBlip.Delete();
                if (_vehA?.Exists() == true) _vehA.Dismiss();
                if (_vehB?.Exists() == true) _vehB.Dismiss();
                if (_driverA?.Exists() == true) _driverA.Dismiss();
                if (_driverB?.Exists() == true) _driverB.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][RoadRage] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _handled = true;
        }
    }
}