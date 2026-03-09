using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// AnimalAttack.cs
    /// Version: 0.9.3 Stable (Manual EMS / Dialogue Build)
    /// Ensures animal models are loaded, hostility configured, and player-interactive
    /// medical response rather than automatic.
    /// </summary>
    [CalloutInfo("Animal Attack", CalloutProbability.Medium)]
    public class AnimalAttack : WSQCalloutBase
    {
        private Vector3 _attackLocation;
        private Ped _victim;
        private Ped _bystander;
        private Ped _attackingAnimal;
        private Blip _sceneBlip;

        private bool _sceneActive;
        private bool _callHandled;
        private bool _attackOngoing;

        private readonly Random _rng = new Random();

        private readonly string[] _animalModels =
        {
            "a_c_coyote",
            "a_c_mtlion",
            "a_c_rottweiler",
            "a_c_shepherd",
            "a_c_deer",
            "a_c_boar"
        };

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _attackLocation = World.GetNextPositionOnStreet(playerPos.Around(400f));

                ShowCalloutAreaBlipBeforeAccepting(_attackLocation, 50f);
                CalloutMessage = "Animal Attack Reported";
                CalloutPosition = _attackLocation;

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT ANIMAL_ATTACK IN_OR_ON_POSITION", _attackLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Animal Attack",
                    "Unit requested for animal attack in progress. Proceed  Code 3.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][AnimalAttack] OnBeforeCalloutDisplayed Exception: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][AnimalAttack] Callout accepted.");
            try
            {
                // --- Victim
                _victim = new Ped("A_M_Y_Beach_02", _attackLocation.Around(2f), 0f);
                _victim.IsPersistent = true;
                _victim.BlockPermanentEvents = true;
                _victim.Health = 120;
                _victim.Tasks.Cower(-1);

                // --- Bystander
                _bystander = new Ped("A_M_M_EastSA_02", _attackLocation.Around(4f), 0f);
                _bystander.IsPersistent = true;
                _bystander.BlockPermanentEvents = true;
                _bystander.Tasks.StandStill(-1);

                // --- Animal spawn with model‑load
                string modelName = _animalModels[_rng.Next(_animalModels.Length)];
                Vector3 animalPos = _attackLocation.Around(3f);

                Model animalModel = new Model(modelName);
                if (!animalModel.IsValid)
                {
                    Game.LogTrivial($"[WSQ][AnimalAttack] Invalid model {modelName}. Aborting callout.");
                    End();
                    return false;
                }

                animalModel.LoadAndWait();
                _attackingAnimal = new Ped(animalModel, animalPos, 0f);

                if (!_attackingAnimal || !_attackingAnimal.Exists())
                {
                    Game.LogTrivial("[WSQ][AnimalAttack] Animal spawn failed after load — aborting scene.");
                    End();
                    return false;
                }

                // ─── Animal AI / hostility ───
                _attackingAnimal.IsPersistent = true;
                _attackingAnimal.BlockPermanentEvents = true;
                _attackingAnimal.KeepTasks = true;

                _attackingAnimal.RelationshipGroup = new RelationshipGroup("ANIMAL_ATTACKER");
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "PLAYER", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "CIVMALE", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "CIVFEMALE", Relationship.Hate);

                // 50 % chance of active attack
                if (_rng.Next(0, 100) > 50)
                {
                    _attackingAnimal.Tasks.FightAgainst(_victim);
                    _attackOngoing = true;
                    Game.LogTrivial("[WSQ][AnimalAttack] Animal attacking victim.");
                }
                else
                {
                    _attackingAnimal.Tasks.Wander();
                    _attackOngoing = false;
                    Game.LogTrivial("[WSQ][AnimalAttack] Animal wandering.");
                }

                // Scene marker
                _sceneBlip = new Blip(_attackLocation, 40f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Animal Attack Scene",
                    Alpha = 0.8f
                };

                Game.DisplayHelp("Respond to the ~r~animal attack~w~ in progress. Neutralize or tranquilize the threat.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][AnimalAttack] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_attackLocation);

            if (dist < 30f && _attackOngoing)
            {
                Game.DisplaySubtitle("~r~Animal is attacking a victim! Use lethal or non‑lethal force to stop it.");
            }

            // Prevent early auto‑clear until player approaches
            if (dist > 100f) return;

            // --- Threat neutralized logic ---
            if (_attackingAnimal && (!_attackingAnimal.Exists() || !_attackingAnimal.IsAlive))
            {
                _attackOngoing = false;
                Game.DisplaySubtitle("~g~Threat neutralized. Check on the victim and provide assistance.", 5000);
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                _callHandled = true;

                // Player‑interaction sequence
                if (_victim && _victim.Exists())
                {
                    _victim.KeepTasks = true;
                    _victim.BlockPermanentEvents = true;

                    bool victimConscious = _victim.IsAlive && !_victim.IsRagdoll && _victim.Health > 50;

                    if (victimConscious)
                    {
                        _victim.Tasks.StandStill(-1);
                        Game.DisplayHelp("Approach the victim and press ~y~E~s~ to ask if they're OK.");

                        bool dialogueComplete = false;
                        GameFiber.StartNew(delegate
                        {
                            while (!dialogueComplete && _victim && _victim.Exists() && _victim.IsAlive)
                            {
                                if (Game.IsKeyDown(System.Windows.Forms.Keys.E)
                                    && Game.LocalPlayer.Character.DistanceTo(_victim) < 3f)
                                {
                                    Game.DisplaySubtitle("~b~You:~s~ Are you alright? Do you need medical help?", 4000);
                                    GameFiber.Wait(3500);

                                    int reply = new Random().Next(0, 100);
                                    if (reply <= 60)
                                    {
                                        Game.DisplaySubtitle("~y~Victim:~s~ I'm okay... just shaken up.", 4500);
                                        _victim.Tasks.Cower(-1);
                                        GameFiber.Wait(4000);
                                        Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                                            "Dispatch", "Scene Update",
                                            "Victim declined medical evaluation. Callout will clear shortly.");
                                    }
                                    else
                                    {
                                        Game.DisplaySubtitle("~y~Victim:~s~ I think I need an ambulance...", 4500);
                                        GameFiber.Wait(3500);
                                        Game.DisplayHelp("~b~Call EMS manually~s~ using your EMS or backup plugin.");
                                        Game.LogTrivial("[WSQ][AnimalAttack] Victim requested EMS (manual call).");
                                    }

                                    dialogueComplete = true;
                                    GameFiber.Wait(6000);
                                    Game.DisplayHelp("Scene clear. ~y~Press END~s~ when you are ready to close this callout.");
                                    GameFiber.StartNew(delegate
                                    {
                                        WhoSaidQuietCallouts.CalloutUtilities.WaitForPlayerEnd();
                                        PlayerControlledEnd();
                                    });
                                    break;
                                }
                                GameFiber.Yield();
                            }
                        });
                    }
                    else
                    {
                        // Victim unconscious
                        if (!_victim.IsRagdoll)
                        {
                            _victim.Tasks.PlayAnimation("amb@medic@standing@tendtodead@base",
                                "base", 1f, AnimationFlags.Loop);
                        }

                        Game.DisplaySubtitle("~r~Victim appears unconscious — call EMS immediately!", 5000);
                        Game.DisplayHelp("Use your ~y~EMS/Backup~s~ plugin to request paramedics.");

                        GameFiber.StartNew(delegate
                        {
                            GameFiber.Wait(12000);
                            Game.DisplayHelp("Scene clear. ~y~Press END~s~ when you are ready to close this callout.");
                            GameFiber.StartNew(delegate
                            {
                                WhoSaidQuietCallouts.CalloutUtilities.WaitForPlayerEnd();
                                PlayerControlledEnd();
                            });
                        });
                    }
                }
                else
                {
                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Wait(8000);
                        End();
                    });
                }
            }

            // Victim dies during active attack before you arrive
            if (_victim && _victim.IsDead && !_callHandled)
            {
                Game.DisplaySubtitle("~r~Victim down. You should request EMS immediately.", 5000);
                Game.DisplayHelp("Use your EMS/Backup plugin to request paramedics.");
                _callHandled = true;

                GameFiber.StartNew(delegate
                {
                    GameFiber.Wait(10000);
                    Game.DisplayHelp("Scene clear. ~y~Press END~s~ when you are ready to close this callout.");
                    GameFiber.StartNew(delegate
                    {
                        WhoSaidQuietCallouts.CalloutUtilities.WaitForPlayerEnd();
                        PlayerControlledEnd();
                    });
                });
            }

            // Player leaves scene
            if (Game.LocalPlayer.Character.DistanceTo(_attackLocation) > 500f)
            {
                Game.DisplayHelp("You left the area. Dispatch cleared the call.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][AnimalAttack] Cleaning up entities.");

            try
            {
                if (_sceneBlip?.Exists() == true) _sceneBlip.Delete();
                if (_attackingAnimal?.Exists() == true) _attackingAnimal.Dismiss();
                if (_victim?.Exists() == true) _victim.Dismiss();
                if (_bystander?.Exists() == true) _bystander.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][AnimalAttack] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Animal attack scene cleared.");
        }
    }
}