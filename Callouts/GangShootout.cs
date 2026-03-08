using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// GangShootout.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  A large-scale firefight has broken out between rival gang groups.
    ///  The player officer must respond Code 3, neutralize active shooters,
    ///  and coordinate with backup units to restore order in the area.
    /// </summary>
    [CalloutInfo("Gang Shootout", CalloutProbability.Medium)]
    public class GangShootout : Callout
    {
        private Vector3 _sceneLocation;
        private Blip _sceneBlip;
        private List<Ped> _gangA = new List<Ped>();
        private List<Ped> _gangB = new List<Ped>();
        private bool _sceneActive;
        private bool _callHandled;
        private bool _backupRequested;
        private Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(playerPos.Around(700f));

                CalloutMessage = "Reports of Gunfire Between Rival Gangs";
                CalloutPosition = _sceneLocation;
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 80f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT GANG-related_GUNFIRE IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Gang Shootout", "Multiple 911 calls reporting exchange of gunfire between gangs.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GangShootout] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][GangShootout] Callout accepted.");
            try
            {
                // Spawn combatants
                for (int i = 0; i < 4; i++)
                {
                    Ped gangAMember = new Ped("G_M_Y_MexGang_01", _sceneLocation.Around(8f), _rng.Next(0, 359));
                    gangAMember.Inventory.GiveNewWeapon("WEAPON_MICROSMG", 150, true);
                    gangAMember.RelationshipGroup = "GANG_A";
                    gangAMember.IsPersistent = true;
                    _gangA.Add(gangAMember);
                }

                for (int i = 0; i < 4; i++)
                {
                    Ped gangBMember = new Ped("G_M_Y_BallasOut_01", _sceneLocation.Around(10f), _rng.Next(0, 359));
                    gangBMember.Inventory.GiveNewWeapon("WEAPON_PISTOL", 90, true);
                    gangBMember.RelationshipGroup = "GANG_B";
                    gangBMember.IsPersistent = true;
                    _gangB.Add(gangBMember);
                }

                // Assign hostility between gangs
                Game.SetRelationshipBetweenRelationshipGroups("GANG_A", "GANG_B", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_B", "GANG_A", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_A", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_B", "COP", Relationship.Hate);

                foreach (var a in _gangA)
                    a.Tasks.FightAgainstClosestHatedTarget(100f);
                foreach (var b in _gangB)
                    b.Tasks.FightAgainstClosestHatedTarget(100f);

                // Create scene marker
                _sceneBlip = new Blip(_sceneLocation, 80f)
                {
                    Color = System.Drawing.Color.Red,
                    Name = "Gang Shootout",
                    Alpha = 0.7f
                };

                Game.DisplayHelp("Respond Code 3. Neutralize armed suspects and secure the area.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GangShootout] OnCalloutAccepted Exception: " + ex);
                End();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (!_sceneActive || _callHandled) return;

            // Automatically request AI backup once firefight confirmed
            if (!_backupRequested && Game.LocalPlayer.Character.DistanceTo(_sceneLocation) < 120f)
            {
                Functions.RequestBackup(_sceneLocation, EBackupResponseType.Code3, EBackupUnitType.LocalUnit);
                _backupRequested = true;
                Game.LogTrivial("[WSQ][GangShootout] Backup requested automatically (Code 3).");
            }

            // Scene clear check
            bool anyAlive = false;
            foreach (var ped in _gangA)
                if (ped && ped.IsAlive) anyAlive = true;
            foreach (var ped in _gangB)
                if (ped && ped.IsAlive) anyAlive = true;

            if (!anyAlive)
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~All suspects neutralized. Secure weapons and await coroner/cleanup units.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                End();
            }

            // If player leaves big radius, terminate
            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 800f)
            {
                Game.DisplayHelp("You have left the incident area. The scene has been cleared by other units.");
                End();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][GangShootout] Cleaning up scene.");

            try
            {
                _sceneActive = false;

                foreach (Ped ped in _gangA)
                    if (ped && ped.Exists()) ped.Dismiss();
                foreach (Ped ped in _gangB)
                    if (ped && ped.Exists()) ped.Dismiss();

                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GangShootout] Cleanup Exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch", "Callout Completed", "Gang shootout contained. Area safe.");
        }
    }
}
