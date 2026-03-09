using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// DrugDeal.cs
    /// Version: 0.9.1 Alpha (Compatibility Build)
    /// Date: March 9, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Reports indicate a suspected narcotics transaction in progress.
    ///  Player must locate the participants, observe behavior, and intervene as needed.
    ///  Random outcomes include compliant arrests, pursuit, or shootout.
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

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT NARCOTICS_ACTIVITY IN_OR_ON_POSITION", _dealLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "Dispatch", "~r~Drug Deal", "Suspicious transaction in progress, proceed Code 2.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][DrugDeal] Callout accepted.");

            try
            {
                // Spawn dealer and buyer
                _dealer = new Ped("A_M_Y_Downtown_01", _dealLocation.Around(1.5f), _rng.Next(0, 359));
                _buyer = new Ped("A_M_M_Skater_01", _dealLocation.Around(2.5f), _rng.Next(0, 359));

                if (!_dealer.Exists() || !_buyer.Exists())
                {
                    Game.LogTrivial("[WSQ][DrugDeal] Ped spawn failed — callout aborted.");
                    End();
                    return false;
                }

                _dealer.IsPersistent = true;
                _buyer.IsPersistent = true;
                _dealer.BlockPermanentEvents = false;
                _buyer.BlockPermanentEvents = false;

                _dealer.RelationshipGroup = "DEALER";
                _buyer.RelationshipGroup = "BUYER";

                _dealer.Tasks.StandStill(-1);

                // 🔧 replaced TurnToFaceEntity
                float heading = (_dealer.Position - _buyer.Position).ToHeading();
                _buyer.Tasks.AchieveHeading(heading, 2000);

                // Scene blip
                _sceneBlip = new Blip(_dealLocation, 40f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Drug Deal Scene",
                    Alpha = 0.75f
                };

                _sceneActive = true;
                Game.DisplayHelp("Respond to the ~r~drug deal~w~. Observe before intervening if possible.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;
            if (!_dealer.Exists() || !_buyer.Exists()) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_dealLocation);

            // Player close enough to observe
            if (distance < 30f && !_pursuitStarted)
            {
                int outcome = _rng.Next(0, 100);

                if (outcome < 40)
                {
                    // 🟢 Compliant arrest
                    Game.DisplaySubtitle("~y~Both suspects spotted exchanging items. Move in for arrests.");
                    _dealer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _buyer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _callHandled = true;
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUSPECTS_IN_CUSTODY");
                    PlayerControlledEnd();
                }
                else if (outcome < 75)
                {
                    // 🟡 One suspect flees
                    Game.DisplaySubtitle("~r~Buyer fleeing on foot! Dealer remaining on scene.");
                    _buyer.Tasks.Flee(Game.LocalPlayer.Character.Position, 200f, -1);  // fixed overload
                    _dealer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _pursuitStarted = true;
                    StartFootPursuit(_buyer);
                }
                else
                {
                    // 🔴 Gunfight eruption
                    Game.DisplaySubtitle("~r~Suspects are armed! Take cover and return fire!");
                    _dealer.Inventory.GiveNewWeapon("WEAPON_PISTOL", 80, true);
                    _buyer.Inventory.GiveNewWeapon("WEAPON_PISTOL", 60, true);
                    _dealer.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    _buyer.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");
                }
            }

            // Player leaves area
            if (Game.LocalPlayer.Character.DistanceTo(_dealLocation) > 500f)
            {
                Game.DisplayHelp("You left the incident area. Dispatch is assigning another unit.");
                End();
            }

            // Conclude pursuit once done
            if (_pursuitStarted && _pursuitHandle != null && !Functions.IsPursuitStillRunning(_pursuitHandle))
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~Foot pursuit concluded. Scene handled.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                PlayerControlledEnd();
            }
        }

        private void StartFootPursuit(Ped fleeingSuspect)
        {
            try
            {
                Game.LogTrivial("[WSQ][DrugDeal] Starting foot pursuit.");
                _pursuitHandle = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuitHandle, fleeingSuspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuitHandle, true);
                Functions.PlayScannerAudio("SUSPECT_FLEEING_ON_FOOT");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] StartFootPursuit Exception: " + ex);
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][DrugDeal] Cleaning up entities.");

            try
            {
                if (_sceneBlip?.Exists() == true) _sceneBlip.Delete();
                if (_dealer?.Exists() == true) _dealer.Dismiss();
                if (_buyer?.Exists() == true) _buyer.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Drug deal call handled successfully.");
        }
    }
}