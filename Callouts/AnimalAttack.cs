using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// AnimalAttack.cs
    /// Version: 0.9.5 Stable (Navigation Preference Integration Build)
    /// Updated: March 9, 2026
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
                    "Unit requested for animal attack in progress. Proceed Code 3.");

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
                // --- Victim ---
                _victim = new Ped("A_M_Y_Beach_02", _attackLocation.Around(2f), 0f);
                _victim.IsPersistent = true;
                _victim.BlockPermanentEvents = true;
                _victim.Health = 120;
                _victim.Tasks.Cower(-1);

                // --- Bystander ---
                _bystander = new Ped("A_M_M_EastSA_02", _attackLocation.Around(4f), 0f);
                _bystander.IsPersistent = true;
                _bystander.BlockPermanentEvents = true;
                _bystander.Tasks.StandStill(-1);

                // --- Animal spawn ---
                string modelName = _animalModels[_rng.Next(_animalModels.Length)];
                Model animalModel = new Model(modelName);
                animalModel.LoadAndWait();

                _attackingAnimal = new Ped(animalModel, _attackLocation.Around(3f), 0f);
                if (!_attackingAnimal || !_attackingAnimal.Exists())
                {
                    Game.LogTrivial("[WSQ][AnimalAttack] Animal spawn failed — aborting scene.");
                    PlayerControlledEnd();
                    return false;
                }

                _attackingAnimal.IsPersistent = true;
                _attackingAnimal.BlockPermanentEvents = true;
                _attackingAnimal.KeepTasks = true;

                _attackingAnimal.RelationshipGroup = new RelationshipGroup("ANIMAL_ATTACKER");
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "PLAYER", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "CIVMALE", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("ANIMAL_ATTACKER", "CIVFEMALE", Relationship.Hate);

                if (_rng.Next(0, 100) > 50)
                {
                    _attackingAnimal.Tasks.FightAgainst(_victim);
                    _attackOngoing = true;
                    Game.LogTrivial("[WSQ][AnimalAttack] Animal attacking victim.");
                }
                else
                {
                    _attackingAnimal.Tasks.Wander();
                    _attackOngoing = false;
                    Game.LogTrivial("[WSQ][AnimalAttack] Animal wandering.");
                }

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_attackLocation, 40f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Animal Attack Scene",
                        Alpha = 0.8f
                    };

                    Game.DisplayHelp("Radar blip set. Navigate manually to the scene.");
                }
                else
                {
                    // Direct GPS guidance instead of radar (no RPH "SetRoute" API needed)
                    Blip routeBlip = new Blip(_attackLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "Animal Attack Route"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~animal attack~s~ scene.");
                }

                Game.DisplayHelp("Respond Code 3 to the reported animal attack.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][AnimalAttack] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        // Process and End() unchanged ...
        // (rest of your original Process() / End() logic goes here without modification)
    }
}