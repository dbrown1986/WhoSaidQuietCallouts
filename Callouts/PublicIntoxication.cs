using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using System.Windows.Forms;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// PublicIntoxication.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team Maintenance.
    /// Description:
    ///  Low‑priority call: intoxicated individual reported loitering or acting erratically.
    ///  Random behaviors: cooperative, disorderly, or resistant.
    ///  Optional plugin integrations executed via reflection to avoid external dependencies.
    /// </summary>
    [CalloutInfo("Public Intoxication", CalloutProbability.Low)]
    public class PublicIntoxication : WSQCalloutBase
    {
        private Ped _suspect;
        private Blip _sceneBlip;
        private Vector3 _spawnPoint;
        private LHandle _pursuit;
        private bool _sceneCompleted;
        private bool _pursuitActive;
        private bool _interactionComplete;

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                _spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(400f));
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 45f);
                CalloutMessage = "~b~Public Intoxication~s~ reported in the area.";
                CalloutPosition = _spawnPoint;
                CalloutAdvisory = "A visibly intoxicated individual has been reported by a concerned citizen.";
                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CODE_2 INTOXICATED_PERSON IN_OR_ON_POSITION", _spawnPoint);
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PublicIntoxication] OnBeforeCalloutDisplayed Exception: " + ex.Message);
            }
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][PublicIntoxication] Callout accepted.");

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_spawnPoint, 40f)
                    {
                        Color = System.Drawing.Color.LightBlue,
                        Alpha = 0.7f,
                        Name = "Intoxicated Person – Last Known Location"
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the suspected area.");
                }
                else
                {
                    Blip route = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Intoxicated Person"
                    };
                    route.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~b~public intoxication~s~ scene.");
                    _sceneBlip = route;
                }

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "~b~Dispatch:~s~", "~y~Public Intoxication~s~",
                    "Locate and assess the individual reported as intoxicated.");
                Functions.PlayScannerAudio("RESPOND_CODE_2");

                StartCallout();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PublicIntoxication] OnCalloutAccepted Exception: " + ex.Message);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        private void StartCallout()
        {
            GameFiber.StartNew(CalloutLogic);
        }

        private void CalloutLogic()
        {
            try
            {
                GameFiber.Wait(2000);
                _suspect = new Ped(_spawnPoint);
                if (!_suspect || !_suspect.Exists())
                {
                    PlayerControlledEnd();
                    return;
                }

                _suspect.BlockPermanentEvents = true;
                _suspect.IsPersistent = true;
                Utilities.Log("PublicIntoxication", "Suspect spawned and scenario started.");

                while (Game.LocalPlayer.Character.DistanceTo(_suspect.Position) > 30f && !_sceneCompleted)
                {
                    GameFiber.Wait(1000);
                }

                if (_sceneBlip != null && _sceneBlip.Exists()) _sceneBlip.Delete();

                int behavior = Utilities.RandomInt(0, 3);
                switch (behavior)
                {
                    case 0: CooperativeBehavior(); break;
                    case 1: DisorderlyBehavior(); break;
                    case 2: ResistantBehavior(); break;
                }

                while (!_sceneCompleted)
                {
                    GameFiber.Wait(1000);
                    if (!_suspect.Exists() || _suspect.IsDead)
                    {
                        PlayerControlledEnd();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException("PublicIntoxication", ex);
                PlayerControlledEnd();
            }
        }

        private void CooperativeBehavior()
        {
            Game.DisplaySubtitle("The suspect appears calm but disoriented. Approach and speak with them.");
            Functions.PlayScannerAudio("ATTENTION_UNIT OFFICER_ARRIVED_ON_SCENE");
            GameFiber.Wait(2000);

            _suspect.Tasks.AchieveHeading(Game.LocalPlayer.Character.Heading - 180f);
            _suspect.Tasks.StandStill(-1);

            while (!_interactionComplete)
            {
                GameFiber.Yield();
                if (Game.LocalPlayer.Character.DistanceTo(_suspect) < 3f && Game.IsKeyDown(Keys.Y))
                {
                    Game.DisplayNotification("~b~You:~s~ Sir, have you been drinking tonight?");
                    GameFiber.Wait(2500);
                    Game.DisplayNotification("~y~Suspect:~s~ I'm... fine officer... just walking home...");
                    GameFiber.Wait(2500);
                    Game.DisplayNotification("~b~You:~s~ Alright, I need to see some ID.");
                    _interactionComplete = true;
                    FinishScene(true);
                }
            }
        }

        private void DisorderlyBehavior()
        {
            Game.DisplaySubtitle("The suspect is yelling and staggering around.");
            _suspect.Tasks.Wander();
            GameFiber.Wait(4000);
            Game.DisplayNotification("~b~Dispatch:~s~ Handle as you see fit.");

            while (!_interactionComplete)
            {
                if (Utilities.WithinRange(Game.LocalPlayer.Character.Position, _suspect.Position, 4f))
                {
                    Game.DisplayHelp("Press ~y~Y~s~ to attempt to calm the suspect.");
                    if (Game.IsKeyDown(Keys.Y))
                    {
                        Game.DisplayNotification("~b~You:~s~ Calm down, sir, I need you to relax.");
                        GameFiber.Wait(2000);

                        bool complies = Utilities.Chance(70);
                        if (complies)
                        {
                            Game.DisplayNotification("~y~Suspect:~s~ Okay... okay, I'm sorry...");
                            _interactionComplete = true;
                            FinishScene(true);
                        }
                        else
                        {
                            Game.DisplayNotification("~y~Suspect:~s~ Get away from me!");
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
            Game.DisplaySubtitle("The suspect refuses to comply and walks away!");
            _suspect.Tasks.Flee(Game.LocalPlayer.Character.Position, 200f, -1);
            _pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(_pursuit, _suspect);
            Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
            _pursuitActive = true;

            Game.DisplayNotification("~r~Suspect is resisting!~s~ Pursue and detain.");
        }

        private void FinishScene(bool peaceful)
        {
            try
            {
                _sceneCompleted = true;
                if (peaceful)
                {
                    Game.DisplayNotification("~g~Scene Code 4:~s~ Suspect processed.");
                    if (PluginBridge.IsPluginLoaded("StopThePed"))
                    {
                        PluginBridge.TryInvoke("StopThePed", "StopThePed.API.Functions", "AddSuspectNote",
                            _suspect, "Appeared intoxicated but cooperative");
                    }
                    if (PluginBridge.IsPluginLoaded("ReportsPlus"))
                    {
                        PluginBridge.TryInvoke("ReportsPlus", "ReportsPlus.API.Functions", "SubmitIncidentReport",
                            "Public Intoxication", "Suspect detained and safely transported.", true);
                    }
                }
                else
                {
                    if (PluginBridge.IsPluginLoaded("ReportsPlus"))
                    {
                        PluginBridge.TryInvoke("ReportsPlus", "ReportsPlus.API.Functions", "SubmitIncidentReport",
                            "Public Intoxication", "Suspect resisted arrest; force applied.", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException("PublicIntoxication", ex);
            }

            Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
            GameFiber.StartNew(delegate
            {
                CalloutUtilities.WaitForPlayerEnd();
                PlayerControlledEnd();
            });
        }

        public override void Process()
        {
            base.Process();
            if (_pursuitActive && !Functions.IsPursuitStillRunning(_pursuit))
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
            Game.LogTrivial("[WSQ][PublicIntoxication] Cleanup complete and callout ended.");
            _sceneCompleted = true;
        }
    }
}