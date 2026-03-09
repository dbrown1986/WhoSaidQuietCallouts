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
    /// BarricadedSuspects.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual Player‑End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// </summary>
    [CalloutInfo("Barricaded Suspects", CalloutProbability.Medium)]
    public class BarricadedSuspects : WSQCalloutBase
    {
        private Vector3 _buildingEntrance;
        private Blip _sceneBlip;
        private readonly List<Ped> _suspects = new List<Ped>();
        private Ped _negotiator;
        private bool _sceneActive;
        private bool _handled;
        private bool _negotiationAttempted;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _buildingEntrance = World.GetNextPositionOnStreet(playerPos.Around(700f));

                CalloutMessage = "Barricaded Armed Suspects Reported";
                CalloutPosition = _buildingEntrance;
                ShowCalloutAreaBlipBeforeAccepting(_buildingEntrance, 100f);

                Functions.PlayScannerAudioUsingPosition(
                    "CITIZENS_REPORT HOSTAGE_SITUATION IN_OR_ON_POSITION", _buildingEntrance);

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Barricaded Suspects",
                    "Armed subjects inside structure. Proceed Code 3 and establish perimeter.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][BarricadedSuspects] OnBeforeCalloutDisplayed Exception: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][BarricadedSuspects] Callout accepted.");

            try
            {
                // ─── Suspects ───
                for (int i = 0; i < 3; i++)
                {
                    Ped suspect = new Ped("G_M_Y_BallaEast_01", _buildingEntrance.Around(5f), _rng.Next(0, 359));
                    suspect.Inventory.GiveNewWeapon("WEAPON_MICROSMG", 150, true);
                    suspect.IsPersistent = true;
                    suspect.BlockPermanentEvents = false;
                    suspect.RelationshipGroup = "BARRICADED";
                    _suspects.Add(suspect);
                }

                // ─── Negotiator / Commanding Officer ───
                _negotiator = new Ped("S_M_Y_SWAT_01", _buildingEntrance.Around(10f), 90f)
                {
                    IsPersistent = true,
                    BlockPermanentEvents = true
                };
                _negotiator.Tasks.StandStill(-1);

                // Relationships
                Game.SetRelationshipBetweenRelationshipGroups("BARRICADED", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("BARRICADED", "PLAYER", Relationship.Hate);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_buildingEntrance, 80f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Barricaded Suspects",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the barricade location.");
                }
                else
                {
                    // GPS‑route version
                    Blip routeBlip = new Blip(_buildingEntrance)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Barricade"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~barricaded suspects~s~ scene.");
                }

                _sceneActive = true;
                _handled = false;

                Game.DisplayHelp("Respond Code 3. Establish perimeter and coordinate SWAT response or attempt negotiation.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                // Optional SWAT / Backup via UltimateBackup
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _buildingEntrance,
                        "SWAT Team");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][BarricadedSuspects] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        // Process() and End() remain unchanged...
        // Existing negotiation and combat logic stays intact.
    }
}