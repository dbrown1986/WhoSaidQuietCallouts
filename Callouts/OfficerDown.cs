using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// OfficerDown.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  A fellow officer has gone down during an engagement. The responding unit (player)
    ///  must secure the location, neutralize any ongoing threats, and coordinate emergency
    ///  medical response for the injured officer. Threat presence and suspect behavior vary randomly.
    /// </summary>
    [CalloutInfo("Officer Down", CalloutProbability.Medium)]
    public class OfficerDown : Callout
    {
        private Vector3 _scenePosition;
        private Ped _downedOfficer;
        private List<Ped> _suspects = new List<Ped>();
        private Blip _sceneBlip;

        private bool _sceneActive;
        private bool _backupCalled;
        private bool _handled;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _scenePosition = World.GetNextPositionOnStreet(playerPos.Around(500f));

                ShowCalloutAreaBlipBeforeAccepting(_scenePosition, 75f);
                CalloutMessage = "Officer Down – Shots Fired";
                CalloutPosition = _scenePosition;

                Functions.PlayScannerAudioUsingPosition("WE_HAVE AN_OFFICER_DOWN IN_OR_ON_POSITION", _scenePosition);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Officer Down", "Officer injured in a shootout – respond Code 3 and assist units.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][OfficerDown] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][OfficerDown] Callout accepted.");

            try
            {
                // Spawn injured officer
                _downedOfficer = new Ped("S_M_Y_Cop_01", _scenePosition, 0f);
                _downedOfficer.IsPersistent = true;
                _downedOfficer.BlockPermanentEvents = true;
                _downedOfficer.Health = 50;
                _downedOfficer.Tasks.Cower(-1);
                Functions.SetPedAsCop(_downedOfficer, true);

                // Random threat level — 0: none, 1: armed suspects
                int threatLevel = _rng.Next(0, 100);

                if (threatLevel < 70)
                {
                    // Low risk: area secure, no active suspects
                    Game.LogTrivial("[WSQ][OfficerDown] No active suspects at scene.");
                    Game.DisplaySubtitle("~y~Officer is down, area appears stable. Check for injuries.");
                }
                else
                {
                    // High risk: create armed suspects nearby
                    Game.LogTrivial("[WSQ][OfficerDown] Creating hostile suspects!");
                    for (int i = 0; i < 2; i++)
                    {
                        Ped suspect = new Ped("G_M_Y_BallaEast_01", _scenePosition.Around(10f), _rng.Next(0, 359));
                        suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 90, true);
                        suspect.IsPersistent = true;
                        suspect.BlockPermanentEvents = false;
                        suspect.RelationshipGroup = "CRIMINALS";
                        suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        _suspects.Add(suspect);
                    }

                    // Hostility setup
                    Game.SetRelationshipBetweenRelationshipGroups("CRIMINALS", "COP", Relationship.Hate);
                    Game.DisplaySubtitle("~r~Suspects still on scene! Engage cautiously!");
                    Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3 MULTIPLE_SHOTS_FIRED_OFFICER_INVOLVED");
                }

                // Scene marker
                _sceneBlip = new Blip(_scenePosition, 40f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Officer Down",
                    Alpha = 0.8f
                };

                Game.DisplayHelp("Respond to the ~r~officer down~w~ situation. Secure the area and help EMS.");

                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][OfficerDown] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _handled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_scenePosition);

            // When player approaches and backup not called
            if (dist < 60f && !_backupCalled)
            {
                _backupCalled = true;
                Functions.RequestBackup(_scenePosition, EBackupResponseType.Code3, EBackupUnitType.LocalUnit);
                Game.LogTrivial("[WSQ][OfficerDown] Backup requested (Code 3).");
            }

            // Scene clear conditions
            bool suspectsAlive = _suspects.Exists(s => s && s.IsAlive);
            if (!suspectsAlive && Game.LocalPlayer.Character.DistanceTo(_scenePosition) < 40f)
            {
                Game.DisplaySubtitle("~g~Area secure. Check status of the injured officer. Request medical aid if necessary.");
                if (_downedOfficer && _downedOfficer.IsAlive)
                {
                    _downedOfficer.Tasks.PlayAnimation("amb@medic@standing@tendtodead@base", "base", 1f, AnimationFlags.Loop);
                }
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                _handled = true;
                End();
            }

            // If player leaves scene radius
            if (Game.LocalPlayer.Character.DistanceTo(_scenePosition) > 600f)
            {
                Game.DisplayHelp("You left the area. Other units have taken over the call.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][OfficerDown] Cleaning up scene.");

            try
            {
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();

                if (_downedOfficer && _downedOfficer.Exists()) _downedOfficer.Dismiss();
                foreach (var suspect in _suspects)
                {
                    if (suspect && suspect.Exists()) suspect.Dismiss();
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][OfficerDown] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Officer down scene cleared. Good work out there.");
        }
    }
}
