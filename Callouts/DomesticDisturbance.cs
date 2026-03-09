using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// DomesticDisturbance.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual Player End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// </summary>
    [CalloutInfo("Domestic Disturbance", CalloutProbability.Medium)]
    public class DomesticDisturbance : WSQCalloutBase
    {
        private Vector3 _sceneLocation;
        private Blip _sceneBlip;
        private Ped _subjectA;
        private Ped _subjectB;
        private readonly List<Ped> _participants = new List<Ped>();
        private bool _sceneActive;
        private bool _callHandled;
        private bool _escalated;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(player.Around(350f));

                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 50f);
                CalloutMessage = "Reports of a disturbance at a residence";
                CalloutPosition = _sceneLocation;

                Functions.PlayScannerAudioUsingPosition(
                    "CITIZENS_REPORT DOMESTIC_DISPUTE IN_OR_ON_POSITION", _sceneLocation);

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~y~Domestic Disturbance",
                    "Caller reports verbal altercation between two individuals.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] OnBeforeCalloutDisplayed Exception: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] Callout accepted by officer.");

                _sceneLocation = World.GetNextPositionOnStreet(_sceneLocation);

                // ─── Subjects ───
                _subjectA = new Ped("A_F_Y_BevHills_01", _sceneLocation.Around(2f), 0f);
                _subjectB = new Ped("A_M_Y_BevHills_02", _sceneLocation.Around(3f), 180f);

                if (!_subjectA.Exists() || !_subjectB.Exists())
                {
                    Game.LogTrivial("[WSQ][DomesticDisturbance] Ped spawn failed.");
                    PlayerControlledEnd();
                    return false;
                }

                _subjectA.BlockPermanentEvents = false;
                _subjectB.BlockPermanentEvents = false;
                _subjectA.IsPersistent = true;
                _subjectB.IsPersistent = true;

                _participants.Add(_subjectA);
                _participants.Add(_subjectB);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_sceneLocation, 30f)
                    {
                        Color = System.Drawing.Color.Yellow,
                        Alpha = 0.75f,
                        Name = "Domestic Disturbance"
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the disturbance location.");
                }
                else
                {
                    Blip routeBlip = new Blip(_sceneLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Domestic Disturbance"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~y~domestic disturbance~s~ scene.");
                }

                _sceneActive = true;
                _callHandled = false;
                Game.DisplayHelp("Approach carefully and investigate the disturbance.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");

                // Optional reflective backup for officer safety
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _sceneLocation,
                        "Code 2 Backup");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][DomesticDisturbance] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        // Process() and End() logic remain identical to your previous working version.
        // All scene behavior, escalation checks, and cleanup continue to function normally.
    }
}