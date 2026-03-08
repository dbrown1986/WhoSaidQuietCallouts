using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// DrugDeal.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  Reports indicate a suspected narcotics transaction in progress.
    ///  Player must locate the participants, observe behavior, and intervene as needed.
    ///  Random outcomes include compliant arrests, pursuit, or shootout.
    /// </summary>
    [CalloutInfo("Drug Deal", CalloutProbability.Medium)]
    public class DrugDeal : Callout
    {
        private Vector3 _dealLocation;
        private Ped _dealer;
        private Ped _buyer;
        private Blip _sceneBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private bool _pursuitStarted;
        private LHandle _pursuitHandle;
        private Random _rng = new Random();

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
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Drug Deal", "Suspicious transaction in progress, proceed Code 2.");
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
                // Create dealer and buyer
                _dealer = new Ped("A_M_Y_Downtown_01", _dealLocation.Around(1.5f), _rng.Next(0, 359));
                _buyer  = new Ped("A_M_M_Skater_01",   _dealLocation.Around(2.5f), _rng.Next(0, 359));

                _dealer.IsPersistent = true;
                _dealer.BlockPermanentEvents = false;
                _buyer.IsPersistent = true;
                _buyer.BlockPermanentEvents = false;

                // Set behaviors
                _dealer.RelationshipGroup = "DEALER";
                _buyer.RelationshipGroup  = "BUYER";

                _dealer.Tasks.StandStill(-1);
                _buyer.Tasks.TurnToFaceEntity(_dealer, 2000);

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
            if (!_dealer || !_buyer) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_dealLocation);

            // Player close enough to observe
            if (distance < 30f && !_pursuitStarted)
            {
                int outcome = _rng.Next(0, 100);

                if (outcome < 40)
                {
                    // Compliant arrest scenario
                    Game.DisplaySubtitle("~y~Both suspects spotted exchanging items. Move in for arrests.");
                    _dealer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _buyer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _callHandled = true;
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUSPECTS_IN_CUSTODY");
                    End();
                }
                else if (outcome < 75)
                {
                    // One suspect flees while the other surrenders
                    Game.DisplaySubtitle("~r~Buyer fleeing on foot! Dealer remaining on scene.");
                    _buyer.Tasks.Flee(Game.LocalPlayer.Character.Position);
                    _dealer.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    _pursuitStarted = true;
                    StartFootPursuit(_buyer);
                }
                else
                {
                    // Gunfight eruption
                    Game.DisplaySubtitle("~r~Suspects are armed! Take cover and return fire!");
                    _dealer.Inventory.GiveNewWeapon("WEAPON_PISTOL", 80, true);
                    _buyer.Inventory.GiveNewWeapon("WEAPON_PISTOL", 60, true);
                    _dealer.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    _buyer.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");
                }
            }

            // Check if player leaves area
            if (Game.LocalPlayer.Character.DistanceTo(_dealLocation) > 500f)
            {
                Game.DisplayHelp("You left the incident area. Dispatch is assigning another unit.");
                End();
            }

            // Conclude pursuit once over
            if (_pursuitStarted && _pursuitHandle != null && !Functions.IsPursuitStillRunning(_pursuitHandle))
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~Foot pursuit concluded. Scene handled.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                End();
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
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
                if (_dealer && _dealer.Exists()) _dealer.Dismiss();
                if (_buyer && _buyer.Exists()) _buyer.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DrugDeal] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Drug deal call handled successfully.");
        }
    }
}
