using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts.Core;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    [CalloutInfo("Suicide Attempt", CalloutProbability.Low)]
    public class SuicideAttempt : WSQCalloutBase
    {
        private Ped _subject;
        private Blip _sceneBlip;
        private Vector3 _spawnPoint;

        private bool _sceneEnded;
        private bool _hostile;
        private bool _negotiationActive;

        private LHandle _pursuit;
        private bool _pursuitActive;   // ✅ added flag to fix CS0103

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                _spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(450f));
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 55f);
                CalloutMessage = "~r~Possible Suicide Attempt~s~ reported.";
                CalloutPosition = _spawnPoint;
                CalloutAdvisory = "Caller reports a subject threatening self‑harm.";
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT SUICIDE_ATTEMPT IN_OR_ON_POSITION", _spawnPoint);
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] OnBeforeCalloutDisplayed Exception: " + ex.Message);
            }
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] Callout accepted.");
                _sceneBlip = new Blip(_spawnPoint, 40f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Suicide Attempt Location"
                };
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "~b~Dispatch~s~",
                    "~r~Suicide Attempt", "Respond Code 2 and attempt to negotiate with the subject.");

                Functions.PlayScannerAudio("RESPOND_CODE_2");
                GameFiber.StartNew(CalloutLogic);
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] OnCalloutAccepted Exception: " + ex.Message);
            }

            return base.OnCalloutAccepted();
        }

        private void CalloutLogic()
        {
            try
            {
                _subject = new Ped(_spawnPoint);
                if (!_subject.Exists()) { End(); return; }

                _subject.IsPersistent = true;
                _subject.BlockPermanentEvents = true;
                _subject.Tasks.StandStill(-1);

                _hostile = Utilities.Chance(25);   // 25% chance of hostility
                _negotiationActive = !_hostile;

                Utilities.Log("SuicideAttempt", $"Subject spawned. Hostile={_hostile}");

                // Wait for officer to arrive
                while (Game.LocalPlayer.Character.DistanceTo(_subject) > 30f && !_sceneEnded)
                {
                    GameFiber.Wait(500);
                }

                if (_sceneBlip.Exists()) _sceneBlip.Delete();

                if (_hostile)
                    HostileSequence();
                else
                    NegotiationSequence();
            }
            catch (Exception ex)
            {
                Utilities.LogException("SuicideAttempt", ex);
                End();
            }
        }

        private void NegotiationSequence()
        {
            Game.DisplaySubtitle("Attempt to negotiate with the suspect calmly.", 4000);

            while (!_sceneEnded && _negotiationActive)
            {
                if (Game.LocalPlayer.Character.DistanceTo(_subject) < 4.0f)
                {
                    Game.DisplayHelp("Press ~y~Y~s~ to speak to the subject.");
                    if (Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                    {
                        Game.DisplayNotification("~b~You:~s~ I'm here to help you. Put down the weapon and let’s talk.");
                        GameFiber.Wait(3500);
                        bool surrenders = Utilities.Chance(70);

                        if (surrenders)
                        {
                            Game.DisplayNotification("~y~Subject:~s~ ...Okay... I don’t want to die.");
                            GameFiber.Wait(2000);
                            Game.DisplayNotification("~g~Subject has surrendered peacefully.");
                            _negotiationActive = false;
                            FinishScene(true);
                            return;
                        }
                        else
                        {
                            Game.DisplayNotification("~y~Subject:~s~ Stay back! I can’t do this anymore!");
                            GameFiber.Wait(1500);
                            bool flips = Utilities.Chance(30);
                            if (flips)
                            {
                                _hostile = true;
                                HostileSequence();
                                return;
                            }
                        }
                    }
                }
                GameFiber.Yield();
            }
        }

        private void HostileSequence()
        {
            Game.DisplaySubtitle("The subject turns aggressive! Officer safety!", 4000);
            _subject.Inventory.GiveNewWeapon("WEAPON_PISTOL", 7, true);
            _subject.Tasks.FightAgainst(Game.LocalPlayer.Character);
            Functions.PlayScannerAudio("ATTENTION_ALL_UNITS ASSAULT_WITH_A_DEADLY_WEAPON");

            _pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(_pursuit, _subject);
            Functions.SetPursuitIsActiveForPlayer(_pursuit, true);

            _pursuitActive = true;      // ✅ mark pursuit started
            _sceneEnded = false;
        }

        private void FinishScene(bool peaceful)
        {
            _sceneEnded = true;

            if (peaceful)
            {
                Game.DisplayNotification("~g~Scene Code 4:~s~ Subject stabilized and transported.");
                ReportsPlusIntegration.SubmitIncidentReport("Suicide Attempt",
                    "Subject peacefully negotiated and transported for evaluation.", true);
            }
            else
            {
                Game.DisplayNotification("~r~Subject Deceased~s~ — Incident closed.");
                ReportsPlusIntegration.SubmitIncidentReport("Suicide Attempt",
                    "Subject died or became aggressive; scene cleared.", false);
            }

            PlayerControlledEnd();
        }

        public override void Process()
        {
            base.Process();

            if (_pursuitActive && _pursuit != null && !Functions.IsPursuitStillRunning(_pursuit))
                FinishScene(false);
        }

        public override void End()
        {
            base.End();
            Utilities.SafeDismissEntity(_subject);
            Utilities.SafeDeleteBlip(_sceneBlip);
            Game.LogTrivial("[WSQ][SuicideAttempt] Callout cleaned up.");
            _sceneEnded = true;
            _pursuitActive = false;
        }
    }
}