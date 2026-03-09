using System;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using WhoSaidQuietCallouts;
using WhoSaidQuietCallouts.Core;   // ✅ Added for WSQSettings reference

namespace WhoSaidQuietCallouts.Callouts
{
    /// <summary>
    /// PursuitSuspect.cs
    /// Version: 0.9.5 Stable (Navigation Preference / Reflective Integration / Manual End)
    /// Updated March 9 2026 by Who Said Quiet Team.
    /// Description:
    ///  An ongoing vehicle pursuit has been reported. Player assists in tracking and intercepting the fleeing suspect.
    ///  Optional plugin integration via reflection for backup support without external dependencies.
    /// </summary>
    [CalloutInfo("Pursuit Suspect", CalloutProbability.High)]
    public class PursuitSuspect : WSQCalloutBase
    {
        private Vehicle _suspectVehicle;
        private Ped _suspect;
        private LHandle _pursuit;
        private Blip _callBlip;
        private Blip _routeBlip;

        private Vector3 _spawnPoint;
        private bool _pursuitCreated;
        private bool _pursuitEnded;
        private readonly Random _rng = new Random();

        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                Vector3 playerPos = Game.LocalPlayer.Character.Position;
                _spawnPoint = World.GetNextPositionOnStreet(playerPos.Around(600f));

                CalloutMessage = "A suspect is fleeing officers in a vehicle!";
                CalloutPosition = _spawnPoint;
                ShowCalloutAreaBlipBeforeAccepting(_spawnPoint, 75f);

                Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_RESIST_ARREST IN_OR_ON_POSITION", _spawnPoint);
                Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", "Dispatch", "~r~Vehicle Pursuit",
                    "Assist other units in pursuit of fleeing suspect.");
                return base.OnBeforeCalloutDisplayed();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] OnBeforeCalloutDisplayed Exception: " + ex);
                return false;
            }
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("[WSQ][PursuitSuspect] Callout accepted.");
            try
            {
                // ─── Spawn vehicle and suspect ───
                _suspectVehicle = new Vehicle("BUFFALO", _spawnPoint);
                _suspectVehicle.IsPersistent = true;

                _suspect = _suspectVehicle.CreateRandomDriver();
                _suspect.IsPersistent = true;
                _suspect.BlockPermanentEvents = false;
                _suspect.RelationshipGroup = "SUSPECT";
                _suspect.Tasks.CruiseWithVehicle(25f, VehicleDrivingFlags.Normal);

                // ─── Navigation Preference ───
                if (WSQSettings.UseRadarBlipsInsteadOfGPS)
                {
                    _routeBlip = new Blip(_spawnPoint, 75f)
                    {
                        Color = System.Drawing.Color.Red,
                        Alpha = 0.7f,
                        Name = "Pursuit Area"
                    };
                    Game.DisplayHelp("Radar blip set. Head to the area of the pursuit.");
                }
                else
                {
                    Blip gpsRoute = new Blip(_spawnPoint)
                    {
                        Color = System.Drawing.Color.Purple,
                        Name = "GPS Route to Pursuit"
                    };
                    gpsRoute.IsRouteEnabled = true;
                    Game.DisplayHelp("GPS route set to the ~r~pursuit~s~ area.");
                    _routeBlip = gpsRoute;
                }

                // ─── Attach suspect indicator ───
                _callBlip = _suspect.AttachBlip();
                _callBlip.Color = System.Drawing.Color.Red;
                _callBlip.Name = "Pursuit Suspect";
                _callBlip.IsFriendly = false;

                _pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(_pursuit, _suspect);
                Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                _pursuitCreated = true;

                Functions.PlayScannerAudio("WE_HAVE A_SUSPECT_ELUDING_POLICE UNITS_RESPOND_CODE_3");
                Game.DisplayHelp("Join the ~r~pursuit~s~ and assist unit in apprehending the suspect.");

                // ─── Optional reflective backup request ───
                if (PluginBridge.IsPluginLoaded("UltimateBackup"))
                {
                    PluginBridge.TryInvoke(
                        "UltimateBackup",
                        "UltimateBackup.API.Functions",
                        "RequestBackupUnit",
                        _spawnPoint,
                        "Pursuit Assistance – Code 3");
                }
                else
                {
                    try
                    {
                        var method = typeof(Functions).GetMethod(
                            "RequestBackup",
                            new[] { typeof(Vector3), typeof(Enum), typeof(Enum) });
                        if (method != null)
                        {
                            var resp = Enum.ToObject(method.GetParameters()[1].ParameterType, 1); // Code 3
                            var unit = Enum.ToObject(method.GetParameters()[2].ParameterType, 0); // Local unit
                            method.Invoke(null, new object[] { _spawnPoint, resp, unit });
                            Game.LogTrivial("[WSQ][PursuitSuspect] Backup requested via LSPDFR reflection fallback.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Game.LogTrivial("[WSQ][PursuitSuspect] Backup reflection exception: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] OnCalloutAccepted Exception: " + ex);
                PlayerControlledEnd();
            }
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!_pursuitCreated || _pursuitEnded) return;

            if (!Functions.IsPursuitStillRunning(_pursuit))
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] Pursuit ended in‑game.");
                HandlePursuitEnd();
            }

            // Player too far away
            if (Game.LocalPlayer.Character.DistanceTo(_spawnPoint) > 700f)
            {
                Game.DisplayHelp("You moved too far from the pursuit area. Dispatch has reassigned the call.");
                HandlePursuitEnd();
            }
        }

        private void HandlePursuitEnd()
        {
            try
            {
                _pursuitEnded = true;
                Game.DisplaySubtitle("~g~Pursuit concluded. Scene secure or suspect in custody.", 4000);
                Functions.PlayScannerAudio("CODE_4_ADAM COPY_THAT");

                Game.DisplayHelp("Press ~y~END~s~ when ready to close this callout.");
                GameFiber.StartNew(delegate
                {
                    CalloutUtilities.WaitForPlayerEnd();
                    PlayerControlledEnd();
                });
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] HandlePursuitEnd Exception: " + ex);
                PlayerControlledEnd();
            }
        }

        public override void End()
        {
            base.End();
            Game.LogTrivial("[WSQ][PursuitSuspect] Cleanup entities.");
            try
            {
                if (_routeBlip != null && _routeBlip.Exists()) _routeBlip.Delete();
                if (_callBlip != null && _callBlip.Exists()) _callBlip.Delete();
                if (_suspectVehicle != null && _suspectVehicle.Exists()) _suspectVehicle.Dismiss();
                if (_suspect != null && _suspect.Exists()) _suspect.Dismiss();
                _pursuitCreated = false;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PursuitSuspect] Cleanup Exception: " + ex.Message);
            }

            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE", "Dispatch",
                "Callout Completed", "Vehicle pursuit assistance concluded. Code 4.");
        }
    }
}