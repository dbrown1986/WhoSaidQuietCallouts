using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// WelfareCheck.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Respond to a welfare check call. A concerned neighbor or friend has requested
    ///  an officer to verify the well-being of an individual. Outcomes range from
    ///  a safe encounter to medical emergencies or deceased subjects.
    /// </summary>
    [CalloutInfo("Welfare Check", CalloutProbability.Medium)]
    public class WelfareCheck : WSQCalloutBase

    {
        private Vector3 _sceneLocation;
        private Ped _resident;
        private Blip _sceneBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(playerPos.Around(750f));

                CalloutMessage = "Request for Welfare Check";
                CalloutPosition = _sceneLocation;
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 70f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT REQUEST_FOR_WELFARE_CHECK IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~b~Welfare Check", "A neighbor reports not seeing a resident for several days. Please respond.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][WelfareCheck] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][WelfareCheck] Callout accepted.");
            try
            {
                // Spawn resident NPC
                _resident = new Ped("A_M_Y_BevHills_02", _sceneLocation.Around(2f), _rng.Next(0, 359));
                _resident.IsPersistent = true;
                _resident.BlockPermanentEvents = false;

                // Scene marker
                _sceneBlip = new Blip(_sceneLocation, 40f)
                {
                    Color = System.Drawing.Color.Blue,
                    Name = "Welfare Check",
                    Alpha = 0.8f
                };

                Game.DisplayHelp("Drive to the ~b~resident's address~w~ and check on their welfare.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][WelfareCheck] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;
            if (!_resident || !_resident.Exists()) return;

            float distance = Game.LocalPlayer.Character.DistanceTo(_sceneLocation);

            // Player arrives near scene
            if (distance < 25f)
            {
                int result = _rng.Next(0, 100);

                if (result < 60)
                {
                    // Safe outcome
                    Game.DisplaySubtitle("~g~Resident found safe and cooperative. File welfare report with dispatch.");
                    _resident.Tasks.StandStill(-1);
                    Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUBJECT_FOUND_SAFE");
                    _callHandled = true;
                }
                else if (result < 85)
                {
                    // Medical emergency
                    Game.DisplaySubtitle("~r~Resident appears unresponsive. Requesting EMS!");
                    _resident.Health = 15;
                    Functions.PlayScannerAudio("UNIT_REPORT EMS_REQUESTED ON_SCENE");
                    _callHandled = true;
                }
                else
                {
                    // Deceased
                    Game.DisplaySubtitle("~r~Resident discovered deceased. Secure scene for coroner.");
                    _resident.Kill();
                    Functions.PlayScannerAudio("OFFICER_REQUESTS_CORONER");
                    _callHandled = true;
                }

                PlayerControlledEnd();
            }

            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 600f)
            {
                Game.DisplayHelp("You left the area. Dispatch has reassigned the call.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][WelfareCheck] Cleaning up entities.");

            try
            {
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
                if (_resident && _resident.Exists()) _resident.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][WelfareCheck] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Welfare check handled and scene secured.");
        }
    }
}
