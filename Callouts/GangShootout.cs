using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;

namespace WhoSaidQuietCallouts.Callouts
{
    [CalloutInfo("Gang Shootout", CalloutProbability.Medium)]
    public class GangShootout : WSQCalloutBase
    {
        // ─── Local fallback enums (avoid dependency on removed LSPDFR types) ───
        private enum EBackupResponseType { Code2, Code3 }
        private enum EBackupUnitType { LocalUnit, SWAT, AirUnit }

        private Vector3 _sceneLocation;
        private Blip _sceneBlip;
        private readonly List<Ped> _gangA = new List<Ped>();
        private readonly List<Ped> _gangB = new List<Ped>();
        private bool _sceneActive;
        private bool _callHandled;
        private bool _backupRequested;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _sceneLocation = World.GetNextPositionOnStreet(playerPos.Around(700f));

                CalloutMessage = "Reports of Gunfire Between Rival Gangs";
                CalloutPosition = _sceneLocation;
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 80f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT GANG_RELATED_GUNFIRE IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Gang Shootout", "911 calls reporting exchange of gunfire between gangs.");
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
                // Spawn gang A
                for (int i = 0; i < 4; i++)
                {
                    Ped m = new Ped("G_M_Y_MexGang_01", _sceneLocation.Around(8f), _rng.Next(0, 359));
                    m.Inventory.GiveNewWeapon("WEAPON_MICROSMG", 150, true);
                    m.RelationshipGroup = "GANG_A";
                    m.IsPersistent = true;
                    _gangA.Add(m);
                }

                // Spawn gang B
                for (int i = 0; i < 4; i++)
                {
                    Ped m = new Ped("G_M_Y_BallasOut_01", _sceneLocation.Around(10f), _rng.Next(0, 359));
                    m.Inventory.GiveNewWeapon("WEAPON_PISTOL", 90, true);
                    m.RelationshipGroup = "GANG_B";
                    m.IsPersistent = true;
                    _gangB.Add(m);
                }

                // Hostility setup
                Game.SetRelationshipBetweenRelationshipGroups("GANG_A", "GANG_B", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_B", "GANG_A", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_A", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_B", "COP", Relationship.Hate);

                foreach (var ped in _gangA) ped.Tasks.FightAgainstClosestHatedTarget(100f);
                foreach (var ped in _gangB) ped.Tasks.FightAgainstClosestHatedTarget(100f);

                // Scene blip
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

            // --- Auto‑request backup once player near scene (reflection‑safe) ---
            if (!_backupRequested && Game.LocalPlayer.Character.DistanceTo(_sceneLocation) < 120f)
            {
                try
                {
                    var method = typeof(LSPD_First_Response.Mod.API.Functions).GetMethod(
                        "RequestBackup",
                        new[] { typeof(Vector3), typeof(Enum), typeof(Enum) });

                    if (method != null)
                    {
                        var respEnum = Enum.ToObject(method.GetParameters()[1].ParameterType, 1); // Code3
                        var unitEnum = Enum.ToObject(method.GetParameters()[2].ParameterType, 0); // LocalUnit
                        method.Invoke(null, new object[] { _sceneLocation, respEnum, unitEnum });
                        Game.LogTrivial("[WSQ][GangShootout] Backup requested automatically (Code 3 – reflected).");
                    }
                    else
                    {
                        Game.LogTrivial("[WSQ][GangShootout] RequestBackup method not found.");
                    }
                }
                catch (Exception ex)
                {
                    Game.LogTrivial("[WSQ][GangShootout] Backup Reflection Exception: " + ex.Message);
                }

                _backupRequested = true;
            }

            // --- Check if any suspects remain alive ---
            bool anyAlive = false;
            foreach (var p in _gangA) if (p && p.IsAlive) anyAlive = true;
            foreach (var p in _gangB) if (p && p.IsAlive) anyAlive = true;

            if (!anyAlive)
            {
                _callHandled = true;
                Game.DisplaySubtitle("~g~All suspects neutralized. Secure weapons and await cleanup units.");
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");
                PlayerControlledEnd();
            }

            // --- Too far → auto‑end ---
            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 800f)
            {
                Game.DisplayHelp("You left the incident area. The scene has been cleared by other units.");
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

                foreach (var p in _gangA) if (p && p.Exists()) p.Dismiss();
                foreach (var p in _gangB) if (p && p.Exists()) p.Dismiss();
                if (_sceneBlip?.Exists() == true) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GangShootout] Cleanup Exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Gang shootout contained. Area secure.");
        }
    }
}