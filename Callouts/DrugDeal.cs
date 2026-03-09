using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings access

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// DrugDeal.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// Description:
    ///  Reports indicate a suspected narcotics transaction in progress.
    ///  Player must locate, observe, and intervene. Outcomes include arrest, pursuit, or shootout.
    /// </summary>
    [CalloutInfo("Drug Deal", CalloutProbability.Medium)]
    public class DrugDeal : WSQCalloutBase
    {
        private Vector3 _dealLocation;
        private Ped _dealer;
        private Ped _buyer;
        private Blip _sceneBlip;

        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;

        private LHandle _pursuitHandle;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _dealLocation = World.GetNextPositionOnStreet(playerPos.Around(500f));

                CalloutMessage = "Possible Narcotics Deal in Progress";
                CalloutPosition = _dealLocation;
                ShowCalloutAreaBlipBeforeAccepting(_dealLocation, 75f);

                Functions.PlayScannerAudioUsingPosition(
                    "CITIZENS_REPORT NARCOTICS_ACTIVITY IN_OR_ON_POSITION", _dealLocation);

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "Dispatch", "~r~Drug Deal",
                    "Suspicious transaction in progress. Respond Code 2 and investigate.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][DrugDeal] Callout accepted.");

            try
            {
                // ─── Spawn Dealer and Buyer ───
                _dealer = new Ped("A_M_Y_Downtown_01", _dealLocation.Around(1.5f), _rng.Next(0, 359));
                _buyer = new Ped("A_M_M_Skater_01", _dealLocation.Around(2.5f), _rng.Next(0, 359));

                if (!_dealer.Exists() || !_buyer.Exists())
                {
                    Game.LogTrivial("[WSQ][DrugDeal] Ped spawn failed — callout aborted.");
                    PlayerControlledEnd();
                    return false;
                }

                _dealer.IsPersistent = _buyer.IsPersistent = true;
                _dealer.BlockPermanentEvents = _buyer.BlockPermanentEvents = false;

                _dealer.RelationshipGroup = "DEALER";
                _buyer.RelationshipGroup = "BUYER";

                _dealer.Tasks.StandStill(-1);
                float heading = (_dealer.Position - _buyer.Position).ToHeading();
                _buyer.Tasks.AchieveHeading(heading, 2000);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_dealLocation, 40f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Drug Deal Scene",
                        Alpha = 0.75f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the suspected drug deal.");
                }
                else
                {
                    Blip routeBlip = new Blip(_dealLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Drug Deal"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~drug deal~s~ location.");
                }

                _sceneActive = true;
                Game.DisplayHelp("Respond to the ~r~drug deal~s~. Observe before intervening if possible.");

                // ─── Optional Reflective Backup ───
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _dealLocation,
                        "Code 2 Backup");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;
            if (!_dealer.Exists() || !_buyer.Exists()) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_dealLocation);

            // ─── Observation Distance ───
            if (dist < 30f && !_pursuitStarted)
            {
                int outcome = _rng.Next(0, 100);

                if (outcome < 40)
                {
                    Game.DisplaySubtitle("~y~Both suspects caught exchanging items. Detain them.", 4000);
                    _dealer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _buyer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _callHandled = true;
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUSPECTS_IN_CUSTODY");

                    Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                    GameFiber.StartNew(delegate
                    {
                        CalloutUtilities.WaitForPlayerEnd();
                        PlayerControlledEnd();
                    });
                }
                else if (outcome < 75)
                {
                    Game.DisplaySubtitle("~r~Buyer fleeing on foot! Dealer remaining on scene.", 4000);
                    _buyer.Tasks.Flee(Game.LocalPlayer.Character.Position, 200f, -1);
                    _dealer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _pursuitStarted = true;
                    StartFootPursuit(_buyer);
                }
                else
                {
                    Game.DisplaySubtitle("~r~Suspects are armed! Take cover!", 4000);
                    _dealer.Inventory.GiveNewWeapon("WEAPON_PISTOL", 80, true);
                    _buyer.Inventory.GiveNewWeapon("WEAPON_PISTOL", 60, true);
                    _dealer.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    _buyer.Tasks.FightAgainst(Game.LocalPlayer.Character);
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

            // ─── Pursuit Completion ───
            if (_pursuitStarted && _pursuitHandle != null && !Functions.IsPursuitStillRunning(_pursuitHandle))
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~Foot pursuit concluded. Scene handled.", 4000);
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                Game.DisplayHelp("Press ~y~END~s~ when ready to close the callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            // ─── Officer Left Area ───
            if (Game.LocalPlayer.Character.DistanceTo(_dealLocation) > 500f)
            {
                Game.DisplayHelp("You left the incident area. Press ~y~END~s~ to end this callout.");
                PlayerControlledEnd();
            }
        }

        private void StartFootPursuit(Ped fleeing)
        {
            try
            {
                Game.LogTrivial("[WSQ][DrugDeal] Starting foot pursuit.");
                _pursuitHandle = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuitHandle, fleeing);
                Functions.SetPursuitIsActiveForPlayer(_pursuitHandle, true);
                _pursuitStarted = true;
                Functions.PlayScannerAudio("SUSPECT_FLEEING_ON_FOOT");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] StartFootPursuit Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][DrugDeal] Cleaning up entities.");

            try
            {
                if (_sceneBlip?.Exists() == true) _sceneBlip.Delete();
                if (_dealer?.Exists() == true) _dealer.Dismiss();
                if (_buyer?.Exists() == true) _buyer.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed",
                "Drug deal scene cleared and code 4 advised.");
        }
    }
}