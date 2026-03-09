using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings access

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// ArmedRobbery.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Player‑Controlled End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// </summary>
    [CalloutInfo("Armed Robbery", CalloutProbability.High)]
    public class ArmedRobbery : WSQCalloutBase
    {
        private Vector3 _spawnPoint;
        private Blip _sceneBlip;
        private readonly List<Ped> _suspects = new List<Ped>();
        private readonly List<Ped> _civilians = new List<Ped>();
        private Vehicle _getawayVehicle;

        private bool _sceneActive;
        private bool _callHandled;
        private int _suspectCount;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(400f));

                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 75f);
                CalloutMessage = "Reports of an Armed Robbery in Progress";
                CalloutPosition = _spawnPoint;

                Functions.PlayScannerAudioUsingPosition(
                    "CITIZENS_REPORT CRIME_ARMED_ROBBERY IN_OR_ON_POSITION", _spawnPoint);

                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch",
                    "~r~Armed Robbery",
                    "Reports of armed suspects inside business. Proceed Code 3.");

                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ArmedRobbery] OnBeforeCalloutDisplayed Exception: " + ex.Message);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            try
            {
                Game.LogTrivial("[WSQ][ArmedRobbery] Callout accepted.");

                // ─── Suspects ───
                _suspectCount = _rng.Next(2, 4);
                for (int i = 0; i < _suspectCount; i++)
                {
                    Ped suspect = new Ped("G_M_Y_Lost_01", _spawnPoint.Around(3f), 0f);
                    suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 90, true);
                    suspect.BlockPermanentEvents = false;
                    suspect.IsPersistent = true;
                    suspect.RelationshipGroup = new RelationshipGroup("WSQ_ROBBER");
                    Game.SetRelationshipBetweenRelationshipGroups("WSQ_ROBBER", "COP", Relationship.Hate);
                    Game.SetRelationshipBetweenRelationshipGroups("WSQ_ROBBER", "PLAYER", Relationship.Hate);
                    _suspects.Add(suspect);
                }

                // ─── Civilians ───
                for (int i = 0; i < 2; i++)
                {
                    Ped civ = new Ped("A_M_Y_Business_02", _spawnPoint.Around(2f), 0f);
                    civ.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    civ.BlockPermanentEvents = true;
                    civ.IsPersistent = true;
                    _civilians.Add(civ);
                }

                // ─── Getaway Vehicle ───
                _getawayVehicle = new Vehicle("SULTAN", _spawnPoint.Around(10f));
                _getawayVehicle.IsPersistent = true;

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _sceneBlip = new Blip(_spawnPoint, 40f)
                    {
                        Color = System.Drawing.Color.Red,
                        Name = "Armed Robbery Scene",
                        Alpha = 0.8f
                    };
                    Game.DisplayHelp("Radar blip set. Navigate manually to the robbery location.");
                }
                else
                {
                    Blip routeBlip = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Robbery"
                    };
                    routeBlip.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~armed robbery~s~ scene.");
                }

                _sceneActive = true;
                _callHandled = false;

                Game.DisplayHelp("Respond Code 3 to the armed robbery. Proceed with caution!");
                Functions.PlayScannerAudio("UNITS_RESPOND_CODE_3");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ArmedRobbery] Exception during OnCalloutAccepted: " + ex);
                PlayerControlledEnd();
            }

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_sceneActive || _callHandled) return;

            bool suspectsAlive = _suspects.Exists(s => s && s.IsAlive);

            if (suspectsAlive)
            {
                float dist = Game.LocalPlayer.Character.DistanceTo(_spawnPoint);
                if (dist < 50f)
                {
                    if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                    {
                        PluginBridge.TryInvoke(
                            "UltimateBackup",
                            "UltimateBackup.API.Functions",
                            "RequestBackupUnit",
                            _spawnPoint,
                            "Code 3 Officer Backup");
                    }
                }
            }
            else
            {
                // ─── Scene Clear ───
                _callHandled = true;
                Game.DisplaySubtitle("~g~All suspects neutralized. Secure the scene and assist civilians.", 5000);
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");

                if (PluginBridge.IsPluginLoaded("StopThePed"))
                {
                    PluginBridge.TryInvoke(
                        "StopThePed",
                        "StopThePed.API.Functions",
                        "CalmNearbyPeds");
                }

                Game.DisplayHelp("Check on civilian victims then press ~y~END~s~ when ready to close the callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }

            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 500f)
            {
                Game.DisplayHelp("You have left the area. Press ~y~END~s~ to end the callout manually.");
                PlayerControlledEnd();
            }
        }

        public override void End()
        {
            Game.LogTrivial("[WSQ][ArmedRobbery] Cleaning up entities.");
            base.End();

            try
            {
                _sceneActive = false;

                foreach (var ped in _suspects)
                    if (ped && ped.Exists()) ped.Dismiss();

                foreach (var civ in _civilians)
                    if (civ && civ.Exists()) civ.Dismiss();

                if (_getawayVehicle && _getawayVehicle.Exists()) _getawayVehicle.Dismiss();
                if (_sceneBlip && _sceneBlip.Exists()) _sceneBlip.Delete();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ArmedRobbery] Cleanup Exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Armed robbery scene cleared. Good work, officer.");
        }
    }
}