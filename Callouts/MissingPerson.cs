using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// MissingPerson.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team Maintenance.
    /// Description:
    ///  A concerned citizen reported a missing family member or friend. Officer must search the area for the individual.
    ///  Outcome varies between safe recovery, medical emergency, or foul play.
    ///  Optional reflective integration adds support for UltimateBackup and StopThePed without extra DLL references.
    /// </summary>
    [CalloutInfo("Missing Person", CalloutProbability.Medium)]
    public class MissingPerson : WSQCalloutBase
    {
        private Vector3 _lastKnownPos;
        private Ped _missingSubject;
        private Blip _searchBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _lastKnownPos = World.GetNextPositionOnStreet(playerPos.Around(800f));

                CalloutMessage = "Reported Missing Person – Check Area";
                CalloutPosition = _lastKnownPos;
                ShowCalloutAreaBlipBeforeAccepting(_lastKnownPos, 100f);
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT MISSING_PERSON IN_OR_ON_POSITION", _lastKnownPos);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~y~Missing Person",
                    "Search the area for the reported missing person.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MissingPerson] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][MissingPerson] Callout accepted.");

            try
            {
                _missingSubject = new Ped("A_M_Y_Beach_01", _lastKnownPos.Around(_rng.Next(5, 25)), _rng.Next(0, 359));
                _missingSubject.IsPersistent = true;
                _missingSubject.BlockPermanentEvents = false;
                _missingSubject.Health = 100;

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _searchBlip = new Blip(_lastKnownPos, 60f)
                    {
                        Color = System.Drawing.Color.Yellow,
                        Name = "Search Area",
                        Alpha = 0.6f
                    };
                    Game.DisplayHelp("Radar blip set. Search the area for the missing person.");
                }
                else
                {
                    Blip routeBlip = new Blip(_lastKnownPos)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Search Area"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~y~missing person~s~ location.");
                    _searchBlip = routeBlip;
                }

                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
                _sceneActive = true;

                // ─── Optional reflective backup request ───
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _lastKnownPos,
                        "Search and Rescue Unit");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MissingPerson] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;
            if (!_missingSubject || !_missingSubject.Exists()) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_missingSubject.Position);

            // ─── Player locates subject ───
            if (dist < 15f)
            {
                int result = _rng.Next(0, 100);
                if (result < 55)
                {
                    Game.DisplaySubtitle("~g~Subject located safe and sound. Notify dispatch.");
                    _missingSubject.Tasks.StandStill(-1);
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUBJECT_FOUND SAFE");
                }
                else if (result < 80)
                {
                    Game.DisplaySubtitle("~r~Subject appears injured or unconscious. Request EMS!", 4000);
                    _missingSubject.Health = 20;
                    _missingSubject.Tasks.Cower(-1);
                    Functions.PlayScannerAudio("UNIT_REPORT EMS_REQUESTED ON_SCENE");

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
                    Game.DisplaySubtitle("~r~Subject found deceased. Notify coroner and secure scene.");
                    _missingSubject.Kill();
                    Functions.PlayScannerAudio("OFFICER_REQUESTS_CORONER");
                }

                _callHandled = true;
                Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            // ─── Player abandoned search area ───
            if (Game.LocalPlayer.Character.DistanceTo(_lastKnownPos) > 800f)
            {
                Game.DisplayHelp("You left the search area. Press ~y~END~s~ to end this callout.");
                PlayerControlledEnd();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][MissingPerson] Cleaning up entities.");

            try
            {
                if (_searchBlip != null && _searchBlip.Exists()) _searchBlip.Delete();
                if (_missingSubject != null && _missingSubject.Exists()) _missingSubject.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MissingPerson] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Missing person case resolved. Code 4.");
        }
    }
}