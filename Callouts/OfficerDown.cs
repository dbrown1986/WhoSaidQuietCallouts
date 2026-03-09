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
    /// OfficerDown.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// Description:
    ///  Respond to an officer‑down situation with potential active threats.
    ///  Integrates optional reflective plugin calls (backup, AI control) while requiring only RPH & LSPDFR references.
    /// </summary>
    [CalloutInfo("Officer Down", CalloutProbability.Medium)]
    public class OfficerDown : WSQCalloutBase
    {
        private Vector3 _scenePosition;
        private Ped _downedOfficer;
        private readonly List<Ped> _suspects = new List<Ped>();
        private Blip _sceneBlip;
        private Blip _routeBlip;

        private bool _sceneActive;
        private bool _backupCalled;
        private bool _handled;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _scenePosition = World.GetNextPositionOnStreet(playerPos.Around(500f));

                ShowCalloutAreaBlipBeforeAccepting(_scenePosition, 75f);
                CalloutMessage = "Officer Down – Shots Fired";
                CalloutPosition = _scenePosition;

                Functions.PlayScannerAudioUsingPosition("WE_HAVE AN_OFFICER_DOWN IN_OR_ON_POSITION", _scenePosition);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Officer Down",
                    "Officer injured in a shootout – respond Code 3 and assist units.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][OfficerDown] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][OfficerDown] Callout accepted.");
            try
            {
                _downedOfficer = new Ped("S_M_Y_Cop_01", _scenePosition, 0f);
                if (!_downedOfficer.Exists())
                {
                    Game.LogTrivial("[WSQ][OfficerDown] Failed to spawn officer, aborting.");
                    PlayerControlledEnd();
                    return false;
                }

                _downedOfficer.IsPersistent = true;
                _downedOfficer.BlockPermanentEvents = true;
                _downedOfficer.Health = 50;
                _downedOfficer.Tasks.Cower(-1);
                Functions.SetPedAsCop(_downedOfficer);

                int threatLevel = _rng.Next(0, 100);
                if (threatLevel < 70)
                {
                    Game.DisplaySubtitle("~y~Officer down. Scene appears stable – check for injuries.");
                }
                else
                {
                    Game.LogTrivial("[WSQ][OfficerDown] Spawning hostile suspects.");
                    for (int i = 0; i < 2; i++)
                    {
                        Ped suspect = new Ped("G_M_Y_BallaEast_01", _scenePosition.Around(10f), _rng.Next(0, 359));
                        if (!suspect.Exists()) continue;

                        suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 90, true);
                        suspect.IsPersistent = true;
                        suspect.BlockPermanentEvents = false;
                        suspect.RelationshipGroup = "CRIMINALS";
                        suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        _suspects.Add(suspect);
                    }

                    Game.SetRelationshipBetweenRelationshipGroups("CRIMINALS", "COP", Relationship.Hate);
                    Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3 MULTIPLE_SHOTS_FIRED_OFFICER_INVOLVED");

                    // Optional backup plugin integration
                    if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                    {
                        PluginBridge.TryInvoke(
                            "UltimateBackup",
                            "UltimateBackup.API.Functions",
                            "RequestBackupUnit",
                            _scenePosition,
                            "SWAT Team Code 3");
                    }
                }

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_scenePosition, 40f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Officer Down",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the officer‑down location.");
                    _routeBlip = _sceneBlip;
                }
                else
                {
                    Blip route = new Blip(_scenePosition)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Officer Down"
                    };
                    route.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~officer‑down~s~ scene.");
                    _routeBlip = route;
                }

                Game.DisplayHelp("Respond Code 3 to the ~r~officer down~s~ scene. Secure area and assist EMS.");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
                _sceneActive = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][OfficerDown] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _handled) return;

            float dist = Game.LocalPlayer.Character.DistanceTo(_scenePosition);

            // Reflective backup fallback
            if (!_backupCalled && dist < 60f)
            {
                _backupCalled = true;
                try
                {
                    if (!PluginBridge.IsPluginLoaded("UltimateBackup"))
                    {
                        var method = typeof(Functions).GetMethod(
                            "RequestBackup",
                            new[] { typeof(Vector3), typeof(Enum), typeof(Enum) });
                        if (method != null)
                        {
                            var resp = Enum.ToObject(method.GetParameters()[1].ParameterType, 1); // Code 3
                            var unit = Enum.ToObject(method.GetParameters()[2].ParameterType, 0); // Local unit
                            method.Invoke(null, new object[] { _scenePosition, resp, unit });
                            Game.LogTrivial("[WSQ][OfficerDown] Backup requested – reflection fallback.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Game.LogTrivial("[WSQ][OfficerDown] Backup reflection exception: " + ex.Message);
                }
            }

            // Scene resolution
            bool suspectsAlive = _suspects.Exists(s => s && s.IsAlive);
            if (!suspectsAlive && Game.LocalPlayer.Character.DistanceTo(_scenePosition) < 40f)
            {
                Game.DisplaySubtitle("~g~Area secure. Check injured officer and request EMS if required.", 4000);
                if (_downedOfficer.Exists() && _downedOfficer.IsAlive)
                {
                    _downedOfficer.Tasks.PlayAnimation("amb@medic@standing@tendtodead@base", "base", 1f, AnimationFlags.Loop);
                }

                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");

                if (PluginBridge.IsPluginLoaded("StopThePed"))
                {
                    PluginBridge.TryInvoke(
                        "StopThePed",
                        "StopThePed.API.Functions",
                        "CalmNearbyPeds");
                }

                _handled = true;
                Game.DisplayHelp("Press ~y~END~s~ when ready to close the callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            // Officer leaves area
            if (dist > 600f)
            {
                Game.DisplayHelp("You left the area. Press ~y~END~s~ to close the callout.");
                PlayerControlledEnd();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][OfficerDown] Cleaning up entities.");

            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_downedOfficer != null && _downedOfficer.Exists()) _downedOfficer.Dismiss();
                foreach (var s in _suspects)
                {
                    if (s != null && s.Exists()) s.Dismiss();
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][OfficerDown] Cleanup Exception: " + ex.Message);
            }

            _sceneActive = false;
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Officer‑down scene cleared. Code 4 acknowledged.");
        }
    }
}