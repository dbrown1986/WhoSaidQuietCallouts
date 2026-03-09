using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// Burglary.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player Manual End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// </summary>
    [CalloutInfo("Burglary", CalloutProbability.Medium)]
    public class Burglary : WSQCalloutBase
    {
        private Vector3 _scenePosition;
        private Blip _sceneBlip;
        private Ped _suspect;
        private Ped _witness;
        private Vehicle _getawayVehicle;

        private bool _sceneActive;
        private bool _pursuitActive;
        private bool _callHandled;

        private LHandle _pursuitHandle;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _scenePosition = World.GetNextPositionOnStreet(playerPos.Around(550f));

                ShowCalloutAreaBlipBeforeAccepting(_scenePosition, 75f);
                CalloutMessage = "Burglary in Progress";
                CalloutPosition = _scenePosition;

                Functions.PlayScannerAudioUsingPosition(
                    "CITIZENS_REPORT BURGLARY IN_OR_ON_POSITION", _scenePosition);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Burglary in Progress", "Respond Code 3 to the alarm activation.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Burglary] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][Burglary] Callout accepted.");
            try
            {
                // ─── Suspect ───
                _suspect = new Ped("A_M_M_Skater_01", _scenePosition.Around(2f), 0f);
                _suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 50, true);
                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;

                // ─── Optional Witness ───
                if (_rng.Next(0, 100) > 60)
                {
                    _witness = new Ped("A_F_Y_Hipster_02", _scenePosition.Around(6f), 90f);
                    _witness.IsPersistent = true;
                    _witness.BlockPermanentEvents = true;
                    Game.DisplaySubtitle("A witness is nearby — question them for details.");
                }

                // ─── Possible Getaway Vehicle ───
                if (_rng.Next(0, 100) > 40)
                {
                    _getawayVehicle = new Vehicle("INTRUDER", _scenePosition.Around(10f))
                    {
                        IsPersistent = true
                    };
                }

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_scenePosition, 40f)
                    {
                        Color = System.Drawing.Color.Orange,
                        Alpha = 0.8f,
                        Name = "Burglary Scene"
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the burglary location.");
                }
                else
                {
                    // GPS route variant using Blip.IsRouteEnabled
                    Blip routeBlip = new Blip(_scenePosition)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Burglary"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~burglary~s~ scene.");
                }

                _sceneActive = true;
                Game.DisplayHelp("Respond Code 3 to the ~r~burglary scene~s~. Approach with caution.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                // ─── Optional Reflective Backup ───
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _scenePosition,
                        "Code 3 Backup Requested");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Burglary] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        // The rest of Process(), StartPursuit(), HandleSceneCompletion(), and End()
        // remain unchanged from your previous working version.
    }
}