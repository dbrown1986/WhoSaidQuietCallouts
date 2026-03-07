using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// SuicideAttempt.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Respond to a suicidal subject. Officers must locate the individual,
    ///  assess their condition, and attempt peaceful negotiation.
    ///  Escalations may occur if the subject becomes violent or flees.
    /// </summary>
    [CalloutInfo("Suicide Attempt", CalloutProbability.Medium)]
    public class SuicideAttempt : Callout
    {
        private Vector3 _sceneLocation;
        private Ped _subject;
        private Blip _sceneBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private bool _armed;
        private bool _hostile;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 player = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(player.Around(800f));

                CalloutPosition = _sceneLocation;
                CalloutMessage = "Attempted Suicide Reported";
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 70f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT SUICIDE_ATTEMPT IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Suicide Attempt", "Possible suicidal individual, respond Code 2 and attempt verbal intervention.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][SuicideAttempt] Callout accepted.");
            try
            {
                _subject = new Ped("A_M_Y_Business_01", _sceneLocation.Around(1.5f), _rng.Next(0, 359))
                {
                    IsPersistent = true,
                    BlockPermanentEvents = false
                };

                int armChance = _rng.Next(0, 100);
                _armed = armChance > 65; // 35% chance armed

                if (_armed)
                {
                    _subject.Inventory.GiveNewWeapon("WEAPON_PISTOL", 20, true);
                }

                _sceneBlip = new Blip(_sceneLocation, 40f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Suicidal Subject",
                    Alpha = 0.7f
                };

                _sceneActive = true;
                Game.DisplayHelp("Respond to the ~r~suicidal subject~w~. Approach calmly and attempt negotiation.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_2");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;
            if (!_subject || !_subject.Exists()) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_subject.Position);
            if (dist < 20f)
            {
                // Determine behavior outcome
                int behavior = _rng.Next(0, 100);

                if (behavior < 60)
                {
                    // Peaceful compliance
                    Game.DisplaySubtitle("~y~Subject appears distraught but cooperative. Engage in dialogue.");
                    _subject.Tasks.StandStill(-1);
                    AttemptDeescalation();
                }
                else if (behavior < 85)
                {
                    // Fleeing subject
                    Game.DisplaySubtitle("~r~Subject fleeing! Attempt to detain safely!");
                    _subject.Tasks.Flee(Game.LocalPlayer.Character.Position);
                }
                else
                {
                    // Hostile action — if armed
                    _hostile = true;
                    if (_armed)
                    {
                        Game.DisplaySubtitle("~r~Subject raises weapon! Take cover!");
                        _subject.Tasks.AimWeaponAt(Game.LocalPlayer.Character, -1);
                        Functions.PlayScannerAudio("SHOTS_FIRED_OFFICER_INVOLVED");
                    }
                    else
                    {
                        Game.DisplaySubtitle("~r~Subject charges at you!");
                        _subject.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    }
                }

                _callHandled = true;
            }

            // Player leaves far from scene — auto end
            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 600f)
            {
                Game.DisplayHelp("You left the area. Other units will take over.");
                End();
            }
        }

        private void AttemptDeescalation()
        {
            GameFiber.StartNew(() =>
            {
                try
                {
                    GameFiber.Wait(4000);
                    int resolution = _rng.Next(0, 100);
                    if (resolution < 70)
                    {
                        Game.DisplaySubtitle("~g~Subject calmed down after brief dialogue. Scene safe.");
                        Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT SUBJECT_SAFE");
                    }
                    else if (resolution < 90)
                    {
                        Game.DisplaySubtitle("~r~Subject suddenly flees on foot!");
                        _subject.Tasks.Flee(Game.LocalPlayer.Character.Position);
                    }
                    else
                    {
                        Game.DisplaySubtitle("~r~Subject pulls weapon during negotiation!");
                        if (!_armed)
                        {
                            _armed = true;
                            _subject.Inventory.GiveNewWeapon("WEAPON_PISTOL", 15, true);
                        }
                        _subject.Tasks.AimWeaponAt(Game.LocalPlayer.Character, -1);
                        Functions.PlayScannerAudio("SHOT_FIRED_OFFICER_INVOLVED");
                    }
                }
                catch (Exception ex)
                {
                    Game.LogTrivial("[WSQ][SuicideAttempt] AttemptDeescalation Exception: " + ex.Message);
                }
                finally
                {
                    End();
                }
            });
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][SuicideAttempt] Cleaning up entities.");

            try
            {
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
                if (_subject && _subject.Exists()) _subject.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][SuicideAttempt] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Suicide attempt scene cleared. Report filed.");
        }
    }
}
