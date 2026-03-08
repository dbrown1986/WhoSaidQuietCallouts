using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// BarricadedSuspects.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  A group of armed suspects has barricaded themselves inside a building.
    ///  Player officer must coordinate a tactical approach—either contain the perimeter 
    ///  or initiate a SWAT entry.  Randomized outcomes include negotiation success, ambush, or surrender.
    /// </summary>
    [CalloutInfo("Barricaded Suspects", CalloutProbability.Medium)]
    public class BarricadedSuspects : Callout
    {
        private Vector3 _buildingEntrance;
        private Blip _sceneBlip;
        private List<Ped> _suspects = new List<Ped>();
        private Ped _negotiator;
        private bool _sceneActive;
        private bool _handled;
        private bool _negotiationAttempted;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _buildingEntrance = World.GetNextPositionOnStreet(playerPos.Around(700f));

                CalloutMessage = "Barricaded Armed Suspects Reported";
                CalloutPosition = _buildingEntrance;
                ShowCalloutAreaBlipBeforeAccepting(_buildingEntrance, 100f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT HOSTAGE_SITUATION IN_OR_ON_POSITION", _buildingEntrance);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Barricaded Suspects", "Suspects armed inside structure. Proceed Code 3 and establish perimeter.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][BarricadedSuspects] OnBeforeCalloutDisplayed Exception: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][BarricadedSuspects] Callout accepted.");

            try
            {
                // Simulate suspects inside building by spawning near entrance
                for (int i = 0; i < 3; i++)
                {
                    Ped suspect = new Ped("G_M_Y_BallaEast_01", _buildingEntrance.Around(5f), _rng.Next(0, 359));
                    suspect.Inventory.GiveNewWeapon("WEAPON_MICROSMG", 150, true);
                    suspect.IsPersistent = true;
                    suspect.BlockPermanentEvents = false;
                    suspect.RelationshipGroup = "BARRICADED";
                    _suspects.Add(suspect);
                }

                // Negotiator / commanding officer on scene
                _negotiator = new Ped("S_M_Y_SWAT_01", _buildingEntrance.Around(10f), 90f);
                _negotiator.IsPersistent = true;
                _negotiator.BlockPermanentEvents = true;
                _negotiator.Tasks.StandStill(-1);

                // Relationship setup
                Game.SetRelationshipBetweenRelationshipGroups("BARRICADED", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("BARRICADED", "PLAYER", Relationship.Hate);

                // Scene marker
                _sceneBlip = new Blip(_buildingEntrance, 80f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Barricaded Suspects",
                    Alpha = 0.8f
                };

                Game.DisplayHelp("Respond to the ~r~barricaded suspects~w~. Secure the area and await SWAT or attempt negotiations.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][BarricadedSuspects] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _handled) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_buildingEntrance);

            // Attempt negotiation once officer is close enough and hasn't yet tried
            if (distance < 30f && !_negotiationAttempted)
            {
                _negotiationAttempted = true;
                int roll = _rng.Next(0, 100);

                if (roll < 50)
                {
                    // Negotiation success
                    Game.DisplaySubtitle("~g~Negotiations successful. Suspects are surrendering.");
                    foreach (var s in _suspects)
                    {
                        if (s && s.Exists())
                        {
                            s.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                        }
                    }
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUSPECTS_SURRENDERED");
                    _handled = true;
                    End();
                }
                else if (roll < 80)
                {
                    // Standoff / no progress
                    Game.DisplaySubtitle("~o~Suspects refuse to surrender. SWAT preparing breach plan.");
                    Game.DisplayHelp("Hold perimeter positions until breach order given by negotiator (manual RP trigger).");
                }
                else
                {
                    // Ambush!
                    Game.DisplaySubtitle("~r~Suspects open fire! Take cover!");
                    foreach (var s in _suspects)
                    {
                        if (s && s.Exists())
                        {
                            s.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        }
                    }
                    Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");
                }
            }

            // Detect scene resolution
            bool anyAlive = false;
            foreach (var ped in _suspects)
                if (ped && ped.IsAlive) anyAlive = true;

            if (!anyAlive && _sceneActive)
            {
                Game.DisplaySubtitle("~g~All suspects neutralized. Scene secure. SWAT standing down.");
                _handled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                End();
            }

            // Area leave failsafe
            if (Game.LocalPlayer.Character.DistanceTo(_buildingEntrance) > 700f)
            {
                Game.DisplayHelp("You left the barricade scene. The situation will be handled by tactical units.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][BarricadedSuspects] Cleaning up scene.");

            try
            {
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();

                foreach (var s in _suspects)
                    if (s && s.Exists()) s.Dismiss();

                if (_negotiator && _negotiator.Exists()) _negotiator.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][BarricadedSuspects] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Barricaded suspects resolved. Good work, officer.");
        }
    }
}
