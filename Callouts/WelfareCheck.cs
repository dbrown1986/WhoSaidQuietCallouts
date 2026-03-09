using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;  // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// WelfareCheck.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  Respond to a welfare check call. A concerned neighbor has requested an officer to verify
    ///  the well‑being of a resident. Outcomes: safe encounter, medical emergency, or death.
    ///  Now supports reflective plugin integration and WSQ navigation preference for radar vs GPS.
    /// </summary>
    [CalloutInfo("Welfare Check", CalloutProbability.Medium)]
    public class WelfareCheck : WSQCalloutBase
    {
        private Vector3 _sceneLocation;
        private Ped _resident;
        private Blip _sceneBlip;
        private Blip _routeBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(player.Around(750f));

                CalloutMessage = "Request for Welfare Check";
                CalloutPosition = _sceneLocation;
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 70f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT REQUEST_FOR_WELFARE_CHECK IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~b~Welfare Check",
                    "Neighbor reports not seeing resident for several days. Respond and verify status.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][WelfareCheck] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][WelfareCheck] Callout accepted.");

            try
            {
                _resident = new Ped("A_M_Y_BevHills_02", _sceneLocation.Around(2f), _rng.Next(0, 359));
                if (!_resident || !_resident.Exists())
                {
                    PlayerControlledEnd();
                    return false;
                }

                _resident.IsPersistent = true;
                _resident.BlockPermanentEvents = false;

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_sceneLocation, 40f)
                    {
                        Color = System.Drawing.Color.Blue,
                        Name = "Welfare Check Area",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the resident’s address.");
                    _routeBlip = _sceneBlip;
                }
                else
                {
                    Blip gpsRoute = new Blip(_sceneLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Welfare Check"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the resident’s address.");
                    _routeBlip = gpsRoute;
                }

                _sceneActive = true;
                Game.DisplayHelp("Drive to the ~b~resident’s address~s~ and check their welfare.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");

                // Optional UltimateBackup support for medical presence
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _sceneLocation,
                        "EMS and Patrol Assist – Code 2");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][WelfareCheck] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled || _resident == null || !_resident.Exists()) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_sceneLocation);

            if (distance < 25f)
            {
                int result = _rng.Next(0, 100);
                _callHandled = true;

                if (result < 60)
                {
                    Game.DisplaySubtitle("~g~Resident found safe and cooperative. Notify dispatch.", 4000);
                    _resident.Tasks.StandStill(-1);
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUBJECT_FOUND_SAFE");

                    if (PluginBridge.IsPluginLoaded("ReportsPlus"))
                    {
                        PluginBridge.TryInvoke(
                            "ReportsPlus",
                            "ReportsPlus.API.Functions",
                            "SubmitIncidentReport",
                            "Welfare Check", "Resident found safe and verified.", true);
                    }
                }
                else if (result < 85)
                {
                    Game.DisplaySubtitle("~r~Resident appears unresponsive — request EMS!", 4000);
                    _resident.Health = 15;
                    Functions.PlayScannerAudio("UNIT_REPORT EMS_REQUESTED ON_SCENE");

                    if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                    {
                        PluginBridge.TryInvoke(
                            "UltimateBackup",
                            "UltimateBackup.API.Functions",
                            "RequestBackupUnit",
                            _sceneLocation,
                            "Medical Unit – Code 3");
                    }
                }
                else
                {
                    Game.DisplaySubtitle("~r~Resident discovered deceased. Secure scene for coroner.", 4000);
                    _resident.Kill();
                    Functions.PlayScannerAudio("OFFICER_REQUESTS_CORONER");

                    if (PluginBridge.IsPluginLoaded("ReportsPlus"))
                    {
                        PluginBridge.TryInvoke(
                            "ReportsPlus",
                            "ReportsPlus.API.Functions",
                            "SubmitIncidentReport",
                            "Welfare Check", "Deceased subject found, scene secured.", false);
                    }
                }

                Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 600f)
            {
                Game.DisplayHelp("You left the area. Press ~y~END~s~ to close this callout.");
                PlayerControlledEnd();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][WelfareCheck] Cleaning up entities.");
            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_sceneBlip != null && _sceneBlip.Exists()) _sceneBlip.Delete();
                if (_resident != null && _resident.Exists()) _resident.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][WelfareCheck] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Welfare check scene secured. Code 4.");
        }
    }
}