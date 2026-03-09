using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// SuicideAttempt.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  A citizen threatening self‑harm has been reported. Officer must locate and attempt negotiation.
    ///  The situation may end peacefully, escalate into violence, or require medical response.
    ///  Reflective integration adds plugin support without hard dependencies.
    /// </summary>
    [CalloutInfo("Suicide Attempt", CalloutProbability.Low)]
    public class SuicideAttempt : WSQCalloutBase
    {
        private Ped _subject;
        private Blip _sceneBlip;
        private Blip _routeBlip;
        private Vector3 _spawnPoint;

        private bool _sceneEnded;
        private bool _hostile;
        private bool _negotiationActive;

        private LHandle _pursuit;
        private bool _pursuitActive;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                _spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(450f));
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 55f);
                CalloutMessage = "~r~Possible Suicide Attempt ~s~reported.";
                CalloutPosition = _spawnPoint;
                CalloutAdvisory = "Caller reports a subject threatening self‑harm.";
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT SUICIDE_ATTEMPT IN_OR_ON_POSITION", _spawnPoint);
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] OnBeforeCalloutDisplayed Exception: " + ex.Message);
            }
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] Callout accepted.");

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_spawnPoint, 40f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Suicide Attempt Location",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the reported incident area.");
                    _routeBlip = _sceneBlip;
                }
                else
                {
                    Blip gpsRoute = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Suicide Attempt"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~reported incident~s~ location.");
                    _routeBlip = gpsRoute;
                }

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "~b~Dispatch~s~",
                    "~r~Suicide Attempt", "Respond Code 2 and attempt to negotiate with the subject.");
                Functions.PlayScannerAudio("RESPOND_CODE_2");

                // Spawn logic in a dedicated fiber
                GameFiber.StartNew(CalloutLogic);
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] OnCalloutAccepted Exception: " + ex.Message);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        private void CalloutLogic()
        {
            try
            {
                _subject = new Ped(_spawnPoint);
                if (!_subject || !_subject.Exists())
                {
                    PlayerControlledEnd();
                    return;
                }

                _subject.IsPersistent = true;
                _subject.BlockPermanentEvents = true;
                _subject.Tasks.StandStill(-1);

                _hostile = _rng.Next(0, 100) < 25; // 25% chance of hostility
                _negotiationActive = !_hostile;
                Utilities.Log("SuicideAttempt", $"Subject spawned. Hostile={_hostile}");

                // Wait for officer to arrive
                while (Game.LocalPlayer.Character.DistanceTo(_subject) > 30f && !_sceneEnded)
                {
                    GameFiber.Wait(500);
                }

                if (_routeBlip != null && _routeBlip.Exists())
                    _routeBlip.Delete();

                if (_hostile)
                    HostileSequence();
                else
                    NegotiationSequence();
            }
            catch (Exception ex)
            {
                Utilities.LogException("SuicideAttempt", ex);
                PlayerControlledEnd();
            }
        }

        private void NegotiationSequence()
        {
            Game.DisplaySubtitle("Attempt to negotiate with the subject calmly.", 4000);

            while (!_sceneEnded && _negotiationActive)
            {
                if (Game.LocalPlayer.Character.DistanceTo(_subject) < 4.0f)
                {
                    Game.DisplayHelp("Press ~y~Y~s~ to speak to the subject.");
                    if (Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                    {
                        Game.DisplayNotification("~b~You:~s~ I'm here to help you. Put down the weapon and let’s talk.");
                        GameFiber.Wait(3500);
                        bool surrenders = _rng.Next(0, 100) < 70;

                        if (surrenders)
                        {
                            Game.DisplayNotification("~y~Subject:~s~ ...Okay... I don’t want to die.");
                            GameFiber.Wait(2000);
                            Game.DisplayNotification("~g~Subject has surrendered peacefully.");
                            _negotiationActive = false;
                            FinishScene(true);
                            return;
                        }
                        else
                        {
                            Game.DisplayNotification("~y~Subject:~s~ Stay back! I can’t do this anymore!");
                            GameFiber.Wait(1500);
                            if (_rng.Next(0, 100) < 30)
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
            Game.DisplaySubtitle("~r~The subject turns aggressive! Officer safety!", 4000);
            _subject.Inventory.GiveNewWeapon("WEAPON_PISTOL", 7, true);
            _subject.Tasks.FightAgainst(Game.LocalPlayer.Character);
            Functions.PlayScannerAudio("ATTENTION_ALL_UNITS ASSAULT_WITH_A_DEADLY_WEAPON");

            _pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(_pursuit, _subject);
            Functions.SetPursuitIsActiveForPlayer(_pursuit, true);

            _pursuitActive = true;
            _sceneEnded = false;

            // Optional UltimateBackup support for Code 3 officer‑down assist
            if (PluginBridge.IsPluginLoaded("UltimateBackup"))
            {
                PluginBridge.TryInvoke(
                    "UltimateBackup", "UltimateBackup.API.Functions",
                    "RequestBackupUnit", _subject.Position,
                    "Officer Needs Assistance – Code 3");
            }
        }

        private void FinishScene(bool peaceful)
        {
            _sceneEnded = true;
            try
            {
                if (peaceful)
                {
                    Game.DisplayNotification("~g~Scene Code 4:~s~ Subject stabilized and transported.");
                    if (PluginBridge.IsPluginLoaded("ReportsPlus"))
                    {
                        PluginBridge.TryInvoke(
                            "ReportsPlus", "ReportsPlus.API.Functions", "SubmitIncidentReport",
                            "Suicide Attempt",
                            "Subject peacefully negotiated and transported for evaluation.", true);
                    }
                }
                else
                {
                    Game.DisplayNotification("~r~Subject Deceased~s~ — Incident closed.");
                    if (PluginBridge.IsPluginLoaded("ReportsPlus"))
                    {
                        PluginBridge.TryInvoke(
                            "ReportsPlus", "ReportsPlus.API.Functions", "SubmitIncidentReport",
                            "Suicide Attempt",
                            "Subject died or became hostile; scene cleared.", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException("SuicideAttempt – ReportIntegration", ex);
            }

            Game.DisplayHelp("Press ~y~END~s~ to close this callout.");
            GameFiber.StartNew(delegate
            {
                CalloutUtilities.WaitForPlayerEnd();
                PlayerControlledEnd();
            });
        }

        public override void Process()
        {
            base.Process();
            if (_pursuitActive && _pursuit != null && !Functions.IsPursuitStillRunning(_pursuit))
            {
                FinishScene(false);
                _pursuitActive = false;
            }
        }

        public override void End()
        {
            base.End();
            Utilities.SafeDismissEntity(_subject);
            if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
            Game.LogTrivial("[WSQ][SuicideAttempt] Callout cleaned up.");
            _sceneEnded = true;
            _pursuitActive = false;
        }
    }
}