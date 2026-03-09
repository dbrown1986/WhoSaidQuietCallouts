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
    /// GangShootout.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// Description:
    ///  Rival gangs are exchanging gunfire. Player must respond Code 3, neutralize suspects,
    ///  and secure the area. Optional reflective integration minimizes hard dependencies.
    /// </summary>
    [CalloutInfo("Gang Shootout", CalloutProbability.Medium)]
    public class GangShootout : WSQCalloutBase
    {
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

                CalloutMessage = "Reports of Gunfire Between Rival Gangs";
                CalloutPosition = _sceneLocation;
                ShowCalloutAreaBlipBeforeAccepting(_sceneLocation, 80f);

                Functions.PlayScannerAudioUsingPosition(
                    "CITIZENS_REPORT GANG_RELATED_GUNFIRE IN_OR_ON_POSITION", _sceneLocation);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Gang Shootout",
                    "911 calls report exchange of gunfire between gang members.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GangShootout] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][GangShootout] Callout accepted.");
            try
            {
                // ─── Gang Spawning ───
                for (int i = 0; i < 4; i++)
                {
                    Ped a = new Ped("G_M_Y_MexGang_01", _sceneLocation.Around(8f), _rng.Next(0, 359));
                    a.Inventory.GiveNewWeapon("WEAPON_MICROSMG", 150, true);
                    a.RelationshipGroup = "GANG_A";
                    a.IsPersistent = true;
                    _gangA.Add(a);
                }

                for (int i = 0; i < 4; i++)
                {
                    Ped b = new Ped("G_M_Y_BallasOut_01", _sceneLocation.Around(10f), _rng.Next(0, 359));
                    b.Inventory.GiveNewWeapon("WEAPON_PISTOL", 90, true);
                    b.RelationshipGroup = "GANG_B";
                    b.IsPersistent = true;
                    _gangB.Add(b);
                }

                // ─── Hostile Relationships ───
                Game.SetRelationshipBetweenRelationshipGroups("GANG_A", "GANG_B", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_B", "GANG_A", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_A", "PLAYER", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_B", "PLAYER", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_A", "COP", Relationship.Hate);
                Game.SetRelationshipBetweenRelationshipGroups("GANG_B", "COP", Relationship.Hate);

                foreach (var p in _gangA) p.Tasks.FightAgainstClosestHatedTarget(100f);
                foreach (var p in _gangB) p.Tasks.FightAgainstClosestHatedTarget(100f);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_sceneLocation, 80f)
                    {
                        Color = System.Drawing.Color.Red,
                        Alpha = 0.7f,
                        Name = "Gang Shootout"
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the shootout scene.");
                }
                else
                {
                    Blip routeBlip = new Blip(_sceneLocation)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Gang Shootout"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~gang shootout~s~ location.");
                }

                _sceneActive = true;
                Game.DisplayHelp("Respond Code 3. Neutralize armed suspects and secure the area.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");

                // ─── Optional Reflective Backup Call ───
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _sceneLocation,
                        "SWAT Team Code 3");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GangShootout] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            // Auto‑Backup Reflection Support
            if (!_backupRequested && Game.LocalPlayer.Character.DistanceTo(_sceneLocation) < 120f)
            {
                if (!PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    try
                    {
                        var method = typeof(Functions).GetMethod("RequestBackup",
                            new[] { typeof(Vector3), typeof(Enum), typeof(Enum) });
                        if (method != null)
                        {
                            var respEnum = Enum.ToObject(method.GetParameters()[1].ParameterType, 1); // Code 3
                            var unitEnum = Enum.ToObject(method.GetParameters()[2].ParameterType, 0); // LocalUnit
                            method.Invoke(null, new object[] { _sceneLocation, respEnum, unitEnum });
                            Game.LogTrivial("[WSQ][GangShootout] Backup requested automatically (Code 3 – reflection).");
                        }
                    }
                    catch (Exception ex)
                    {
                        Game.LogTrivial("[WSQ][GangShootout] Backup reflection exception: " + ex.Message);
                    }
                }
                _backupRequested = true;
            }

            // Suspect Status
            bool anyAlive = false;
            foreach (var p in _gangA) if (p && p.IsAlive) anyAlive = true;
            foreach (var p in _gangB) if (p && p.IsAlive) anyAlive = true;

            if (!anyAlive)
            {
                _callHandled = true;
                Game.DisplaySubtitle(
                    "~g~All suspects neutralized. Secure weapons and await cleanup units.", 4000);
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");

                Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            // Optional Stop The Ped calm routine
            if (PluginBridge.IsPluginLoaded("StopThePed"))
            {
                PluginBridge.TryInvoke(
                    "StopThePed",
                    "StopThePed.API.Functions",
                    "CalmNearbyPeds");
            }

            // Player leaves area
            if (Game.LocalPlayer.Character.DistanceTo(_sceneLocation) > 800f)
            {
                Game.DisplayHelp("You left the incident area. Press ~y~END~s~ to close this callout.");
                PlayerControlledEnd();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][GangShootout] Cleaning up scene.");

            try
            {
                _sceneActive = false;
                foreach (var p in _gangA) if (p && p.Exists()) p.Dismiss();
                foreach (var p in _gangB) if (p && p.Exists()) p.Dismiss();
                if (_sceneBlip?.Exists() == true) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GangShootout] Cleanup Exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Gang shootout contained. Scene code 4.");
        }
    }
}