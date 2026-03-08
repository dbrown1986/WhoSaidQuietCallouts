using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// MissingPerson.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  A concerned citizen has reported a missing family member or friend.
    ///  Player must locate the missing subject using last-known coordinates and behavior clues.
    ///  Outcome varies between safe recovery, medical emergency, or foul play.
    /// </summary>
    [CalloutInfo("Missing Person", CalloutProbability.Medium)]
    public class MissingPerson : Callout
    {
        private Vector3 _lastKnownPos;
        private Ped _missingSubject;
        private Blip _searchBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _lastKnownPos = World.GetNextPositionOnStreet(playerPos.Around(800f));

                CalloutMessage = "Reported Missing Person – Check Area";
                CalloutPosition = _lastKnownPos;
                ShowCalloutAreaBlipBeforeAccepting(_lastKnownPos, 100f);
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT MISSING_PERSON IN_OR_ON_POSITION", _lastKnownPos);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~y~Missing Person", "Search the area for the reported missing subject.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MissingPerson] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][MissingPerson] Callout accepted.");

            try
            {
                // Create subject within area
                _missingSubject = new Ped("A_M_Y_Beach_01", _lastKnownPos.Around(_rng.Next(5, 25)), _rng.Next(0, 359));
                _missingSubject.IsPersistent = true;
                _missingSubject.BlockPermanentEvents = false;
                _missingSubject.Health = 100;

                _searchBlip = new Blip(_lastKnownPos, 60f)
                {
                    Color = System.Drawing.Color.Yellow,
                    Name = "Search Area",
                    Alpha = 0.6f
                };

                Game.DisplayHelp("Search the ~y~marked area~w~ for the missing person.");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MissingPerson] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;
            if (!_missingSubject || !_missingSubject.Exists()) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_missingSubject.Position);

            // Player finds the subject
            if (dist < 15f)
            {
                int result = _rng.Next(0, 100);
                if (result < 55)
                {
                    // Healthy and cooperative
                    Game.DisplaySubtitle("~g~Subject located safe and sound. Notify dispatch.");
                    _missingSubject.Tasks.StandStill(-1);
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUBJECT_FOUND SAFE");
                }
                else if (result < 80)
                {
                    // Medical emergency
                    Game.DisplaySubtitle("~r~Subject appears injured or unconscious. Request EMS immediately!");
                    _missingSubject.Health = 20;
                    _missingSubject.Tasks.Cower(-1);
                    Functions.PlayScannerAudio("UNIT_REPORT EMS_REQUESTED ON_SCENE");
                }
                else
                {
                    // Foul play found
                    Game.DisplaySubtitle("~r~Subject found deceased. Notify coroner and secure area.");
                    _missingSubject.Kill();
                    Functions.PlayScannerAudio("OFFICER_REQUESTS_CORONER");
                }

                _callHandled = true;
                End();
            }

            if (Game.LocalPlayer.Character.DistanceTo(_lastKnownPos) > 800f)
            {
                Game.DisplayHelp("You left the search area. Dispatch is assigning another unit.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][MissingPerson] Cleaning up entities.");

            try
            {
                if (_searchBlip && _searchBlip.Exists()) _searchBlip.Delete();
                if (_missingSubject && _missingSubject.Exists()) _missingSubject.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MissingPerson] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Missing person case resolved.");
        }
    }
}
