using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// AnimalAttack.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  A 911 caller reports an aggressive or dangerous animal attacking civilians.
    ///  Player must locate the animal, prevent further harm, and coordinate with Animal Control or EMS.
    ///  Callout features randomized animal types and different scene outcomes.
    /// </summary>
    [CalloutInfo("Animal Attack", CalloutProbability.Medium)]
    public class AnimalAttack : Callout
    {
        private Vector3 _attackLocation;
        private Ped _victim;
        private Ped _bystander;
        private Ped _attackingAnimal;
        private Blip _sceneBlip;
        private bool _sceneActive;
        private bool _callHandled;
        private bool _attackOngoing;
        private Random _rng = new Random();

        private readonly string[] _animalModels = new string[]
        {
            "a_c_coyote",
            "a_c_mtlion",
            "a_c_rottweiler",
            "a_c_shepherd"
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
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Animal Attack", "Unit requested for animal attack in progress. Proceed Code 3.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][AnimalAttack] OnBeforeCalloutDisplayed Exception: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][AnimalAttack] Callout accepted.");

            try
            {
                // Spawn victim and bystander
                _victim = new Ped("A_M_Y_Beach_02", _attackLocation.Around(2f), 0f);
                _victim.IsPersistent = true;
                _victim.BlockPermanentEvents = true;
                _victim.Health = 120;
                _victim.Tasks.Cower(-1);

                _bystander = new Ped("A_M_M_EastSA_02", _attackLocation.Around(4f), 0f);
                _bystander.IsPersistent = true;
                _bystander.BlockPermanentEvents = true;
                _bystander.Tasks.StandStill(-1);

                // Spawn attacking animal
                string model = _animalModels[_rng.Next(_animalModels.Length)];
                _attackingAnimal = new Ped(model, _attackLocation, 0f);
                _attackingAnimal.IsPersistent = true;
                _attackingAnimal.BlockPermanentEvents = false;

                // 50% chance to still be attacking when the player arrives
                if (_rng.Next(0, 100) > 50)
                {
                    _attackingAnimal.Tasks.FightAgainst(_victim);
                    _attackOngoing = true;
                }
                else
                {
                    _attackingAnimal.Tasks.Wander();
                    _attackOngoing = false;
                }

                // Scene marker
                _sceneBlip = new Blip(_attackLocation, 40f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Animal Attack Scene",
                    Alpha = 0.8f
                };

                Game.DisplayHelp("Respond to the ~r~animal attack~w~ in progress. Neutralize or tranquilize the threat.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][AnimalAttack] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_attackLocation);

            // Player arrives
            if (dist < 30f && _attackOngoing)
            {
                Game.DisplaySubtitle("~r~Animal is attacking a victim! Use lethal or non-lethal force to stop it.");
            }

            // Scene resolution conditions
            if (_attackingAnimal && (!_attackingAnimal.Exists() || !_attackingAnimal.IsAlive))
            {
                _attackOngoing = false;
                Game.DisplaySubtitle("~g~Threat neutralized. Check on the victim and call medical aid if necessary.");
                _callHandled = true;
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                End();
            }

            if (_victim && _victim.IsDead && !_callHandled)
            {
                Game.DisplaySubtitle("~r~Victim injured. Requesting EMS to the scene.");
                _callHandled = true;
                Functions.PlayScannerAudio("UNIT_REPORT EMS_REQUESTED ON_SCENE");
                End();
            }

            // Leave area condition
            if (Game.LocalPlayer.Character.DistanceTo(_attackLocation) > 500f)
            {
                Game.DisplayHelp("You left the area. Dispatch has cleared this call.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][AnimalAttack] Cleaning up entities.");

            try
            {
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();

                if (_attackingAnimal && _attackingAnimal.Exists()) _attackingAnimal.Dismiss();
                if (_victim && _victim.Exists()) _victim.Dismiss();
                if (_bystander && _bystander.Exists()) _bystander.Dismiss();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][AnimalAttack] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            _callHandled = true;

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Animal attack scene cleared.");
        }
    }
}
