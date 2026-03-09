using System;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// UltimateBackupIntegration.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Provides reflection-based, safe integration with Ultimate Backup by BejoIjo.
    ///  Enables advanced backup dispatch options, squad presets, and automatic notifications.
    ///  Gracefully bypasses itself if the plugin is not installed or API changes are detected.
    /// </summary>
    public static class UltimateBackupIntegration
    {
        private static bool _initAttempted;
        private static bool _apiDetected;
        private static Type _ubAPI;

        /// <summary>
        /// Checks for Ultimate Backup installation and availability.
        /// Safe to call once from Main.cs during plugin load.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _ubAPI = Type.GetType("UltimateBackup.API.Functions, UltimateBackup", false);
                _apiDetected = _ubAPI != null;

                if (_apiDetected)
                    Game.LogTrivial("[WSQ][UB] Ultimate Backup API detected — integration enabled.");
                else
                    Game.LogTrivial("[WSQ][UB] Ultimate Backup not found — skipping integration.");
            }
            catch (Exception ex)
            {
                _apiDetected = false;
                Game.LogTrivial("[WSQ][UB] Initialize Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Requests a backup unit using an Ultimate Backup preset (for example, "LocalPatrol" or "SWAT").
        /// Falls back to standard LSPDFR backup if UB is unavailable.
        /// </summary>
        /// <param name="presetName">Backup preset identifier; e.g., "LocalPatrol", "SWAT", "AirSupport".</param>
        /// <param name="position">Optional location. If null, defaults to the player’s current position.</param>
        public static void RequestBackup(string presetName = "LocalPatrol", Vector3? position = null)
        {
            try
            {
                Vector3 spawn = position ?? Game.LocalPlayer.Character.Position;

                if (_apiDetected && _ubAPI != null)
                {
                    var method = _ubAPI.GetMethod("SpawnBackupUnit");
                    if (method != null)
                    {
                        method.Invoke(null, new object[] { presetName, spawn });
                        Game.LogTrivial($"[WSQ][UB] Requested Ultimate Backup preset: {presetName}");
                        return;
                    }
                    else
                    {
                        Game.LogTrivial("[WSQ][UB] SpawnBackupUnit() not present — using fallback backup.");
                    }
                }

                // fallback to vanilla LSPDFR backup
                LSPD_First_Response.Mod.API.Functions.RequestBackup(spawn,
                    LSPD_First_Response.Mod.API.EBackupResponseType.Code2,
                    LSPD_First_Response.Mod.API.EBackupUnitType.LocalUnit);
                Game.LogTrivial("[WSQ][UB] Requested vanilla backup (fallback).");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] RequestBackup Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Requests a specialized tactical team (e.g., SWAT or Noose) through Ultimate Backup.
        /// </summary>
        public static void RequestTacticalBackup(Vector3? position = null)
        {
            if (!_apiDetected)
            {
                RequestBackup("SWAT", position);
                return;
            }

            try
            {
                Vector3 spawn = position ?? Game.LocalPlayer.Character.Position;
                var method = _ubAPI.GetMethod("SpawnBackupUnit");
                if (method != null)
                {
                    method.Invoke(null, new object[] { "SWAT", spawn });
                    Game.LogTrivial("[WSQ][UB] Tactical team (SWAT) requested via Ultimate Backup.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] RequestTacticalBackup Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Calls an air support helicopter if Ultimate Backup supports that preset.
        /// Falls back to built‑in LSPDFR air unit if UB missing.
        /// </summary>
        public static void RequestAirSupport(Vector3? position = null)
        {
            if (!_apiDetected)
            {
                LSPD_First_Response.Mod.API.Functions.RequestBackup(position ?? Game.LocalPlayer.Character.Position,
                    LSPD_First_Response.Mod.API.EBackupResponseType.Code3,
                    LSPD_First_Response.Mod.API.EBackupUnitType.AirUnit);
                return;
            }

            try
            {
                Vector3 spawn = position ?? Game.LocalPlayer.Character.Position;
                var method = _ubAPI.GetMethod("SpawnBackupUnit");
                if (method != null)
                {
                    method.Invoke(null, new object[] { "AirSupport", spawn });
                    Game.LogTrivial("[WSQ][UB] Air support unit requested via Ultimate Backup.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] RequestAirSupport Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Dismisses all backup units spawned by Ultimate Backup (if supported by API).
        /// </summary>
        public static void DismissAllBackup()
        {
            try
            {
                if (_apiDetected && _ubAPI != null)
                {
                    var dismissMethod = _ubAPI.GetMethod("DismissAllBackupUnits");
                    if (dismissMethod != null)
                    {
                        dismissMethod.Invoke(null, null);
                        Game.LogTrivial("[WSQ][UB] Dismissed all Ultimate Backup units.");
                        return;
                    }
                }

                Game.LogTrivial("[WSQ][UB] DismissAllBackupUnits method unavailable or UB not active.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] DismissAllBackup Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns true if Ultimate Backup is currently available.
        /// </summary>
        public static bool IsAvailable()
        {
            return _apiDetected;
        }

        /// <summary>
        /// Prints integration status summary.
        /// </summary>
        public static void PrintIntegrationSummary()
        {
            Game.LogTrivial($"[WSQ][UB] Integration active={_apiDetected}, APIType={_ubAPI?.FullName ?? "null"}");
        }
    }
}
