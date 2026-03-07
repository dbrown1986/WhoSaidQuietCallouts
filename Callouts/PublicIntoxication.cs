using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts.Core;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// PublicIntoxication.cs
    /// Version: 1.9.1 (Maintenance & Polished Behavior Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  A low‑priority callout where an intoxicated person is reported loitering
    ///  or behaving erratically in public. Officer must locate, assess, and handle the suspect.
    ///  Random outcomes: cooperative, disorderly, or resistant behavior.
    /// </summary>
    [CalloutInfo("Public Intoxication", CalloutProbability.Low)]
    public class PublicIntoxication : Callout
    {
        private Ped _suspect;
        private Blip _sceneBlip;
        private Vector3 _spawnPoint;
        private LHandle _pursuitHandle;
        private bool _sceneCompleted;
        private bool _pursuitActive;
        private bool _interactionComplete;

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                _spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(400f));
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 45f);
                CalloutMessage = "~b~Public Intoxication~s~ reported in the area.";
                CalloutPosition = _spawnPoint;
                CalloutAdvisory = "A visibly intoxicated individual has been reported by a concerned citizen.";
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CODE_2 INTOXICATED_PERSON IN_OR_ON_POSITION", _spawnPoint);
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][PublicIntoxication] OnBeforeCalloutDisplayed Exception: {ex.Message}");
            }
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][PublicIntoxication] Callout accepted by player.");

                _sceneBlip = new Blip(_spawnPoint, 40f)
                {
                    Color = System.Drawing.Color.LightBlue,
                    Alpha = 0.7f,
                    Name = "Intoxicated Person - Last Known Location"
                };

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911",
                    "~b~Dispatch:~s~", "~y~Public Intoxication~s~",
                    "Locate and assess the individual reported to be intoxicated.");

                Functions.PlayScannerAudio("RESPOND_CODE_2");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PublicIntoxication] OnCalloutAccepted Exception: " + ex.Message);
            }
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutStarted()
        {
            base.OnCalloutStarted();
            GameFiber.StartNew(CalloutLogic);
        }

        private void CalloutLogic()
        {
            try
            {
                GameFiber.Wait(2000);
                _suspect = new Ped(_spawnPoint);

                if (!_suspect.Exists())
                {
                    End();
                    return;
                }

                _suspect.BlockPermanentEvents = true;
                _suspect.IsPersistent = true;

                Utilities.Log("PublicIntoxication", "Suspect spawned and scenario started.");

                // Wait for officer to arrive
                while (Game.LocalPlayer.Character.DistanceTo(_suspect.Position) > 30f && !_sceneCompleted)
                {
                    GameFiber.Wait(1000);
                }

                if (_sceneBlip)
                    _sceneBlip.Delete();

                // Random behavior pattern
                int behavior = Utilities.RandomInt(0, 3);
                switch (behavior)
                {
                    case 0:
                        CooperativeBehavior();
                        break;
                    case 1:
                        DisorderlyBehavior();
                        break;
                    case 2:
                        ResistantBehavior();
                        break;
                }

                while (!_sceneCompleted)
                {
                    GameFiber.Wait(1000);

                    if (!_suspect.Exists() || _suspect.IsDead)
                    {
                        End();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException("PublicIntoxication", ex);
                End();
            }
        }

        private void CooperativeBehavior()
        {
            Game.DisplaySubtitle("The suspect appears calm but disoriented. Approach and speak with them.");
            Functions.PlayScannerAudio("ATTENTION_UNIT OFFICER_ARRIVED_ON_SCENE");
            GameFiber.Wait(3000);

            _suspect.Tasks.AchieveHeading(Game.LocalPlayer.Character.Heading - 180f);
            _suspect.Tasks.StandStill(-1);

            while (!_interactionComplete)
            {
                GameFiber.Yield();
                if (Game.LocalPlayer.Character.DistanceTo(_suspect) < 3f && Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                {
                    Game.DisplayNotification("~b~You:~s~ Sir, have you been drinking tonight?");
                    GameFiber.Wait(3000);
                    Game.DisplayNotification("~y~Suspect:~s~ I'm... fine officer... I was just walking home...");
                    GameFiber.Wait(3000);
                    Game.DisplayNotification("~b~You:~s~ Okay, I need to see some ID.");
                    GameFiber.Wait(3000);
                    _interactionComplete = true;
                    FinishScene(true);
                }
            }
        }

        private void DisorderlyBehavior()
        {
            Game.DisplaySubtitle("The suspect is yelling incoherently and staggering...");
            _suspect.Tasks.Wander();
            GameFiber.Wait(4000);
            Game.DisplayNotification("~b~Dispatch:~s~ Handle as you see fit, officer.");

            while (!_interactionComplete)
            {
                if (Utilities.WithinRange(Game.LocalPlayer.Character.Position, _suspect.Position, 4f))
                {
                    Game.DisplayHelp("Press ~y~Y~s~ to attempt to calm the suspect.");
                    if (Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                    {
                        Game.DisplayNotification("~b~You:~s~ Calm down, sir, I need you to relax.");
                        GameFiber.Wait(2500);
                        bool complies = Utilities.Chance(70);
                        if (complies)
                        {
                            Game.DisplayNotification("~y~Suspect:~s~ Okay... okay, I'm sorry...");
                            _interactionComplete = true;
                            FinishScene(true);
                        }
                        else
                        {
                            Game.DisplayNotification("~y~Suspect:~s~ Get away from me!");
                            ResistantBehavior();
                            return;
                        }
                    }
                }
                GameFiber.Yield();
            }
        }

        private void ResistantBehavior()
        {
            Game.DisplaySubtitle("The suspect refuses to comply and begins walking away!");
            _suspect.Tasks.Flee(Game.LocalPlayer.Character);
            _pursuitHandle = Functions.CreatePursuit();
            Functions.AddPedToPursuit(_pursuitHandle, _suspect);
            Functions.SetPursuitIsActiveForPlayer(_pursuitHandle, true);
            _pursuitActive = true;
            Game.DisplayNotification("~r~Suspect is resisting!~s~ Pursue and detain.");
        }

        private void FinishScene(bool peaceful)
        {
            try
            {
                _sceneCompleted = true;

                if (peaceful)
                {
                    Game.DisplayNotification("~g~Scene Code 4:~s~ Suspect processed.");
                    StopThePedIntegration.AddSuspectNote(_suspect, "Appeared intoxicated but cooperative.");
                    ReportsPlusIntegration.SubmitIncidentReport("Public Intoxication", "Suspect detained and safely transported.", true);
                }
                else
                {
                    ReportsPlusIntegration.SubmitIncidentReport("Public Intoxication", "Suspect resisted arrest; use of force applied.", false);
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException("PublicIntoxication", ex);
            }
            End();
        }

        public override void Process()
        {
            base.Process();

            if (_pursuitActive && !Functions.IsPursuitStillRunning(_pursuitHandle))
            {
                FinishScene(false);
                _pursuitActive = false;
            }
        }

        public override void End()
        {
            base.End();

            Utilities.SafeDismissEntity(_suspect);
            Utilities.SafeDeleteBlip(_sceneBlip);

            Game.LogTrivial("[WSQ][PublicIntoxication] Callout ended/cleaned up.");
            _sceneCompleted = true;
        }
    }
}
