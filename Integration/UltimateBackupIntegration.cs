using System;
using Rage;
using LSPD_First_Response.Mod.API;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// UltimateBackupIntegration.cs
    /// Version 0.9.2 Stable (March 9 2026)
    /// 
    /// Description:
    ///  Provides reflection-based, safe integration with Ultimate Backup by BejoIjo.
    ///  Removes dependency on Microsoft.CSharp runtime binder, and tolerates
    ///  missing LSPDFR enum definitions by invoking RequestBackup via reflection.
    /// </summary>
    public static class UltimateBackupIntegration
    {
        private enum EBackupResponseType { Code2, Code3 }
        private enum EBackupUnitType { LocalUnit, AirUnit, SWAT }

        private static bool _initAttempted;
        private static bool _apiDetected;
        private static Type _ubAPI;

        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _ubAPI = Type.GetType("UltimateBackup.API.Functions, UltimateBackup", false);
                _apiDetected = _ubAPI != null;

                Game.LogTrivial(_apiDetected
                    ? "[WSQ][UB] Ultimate Backup API detected — integration enabled."
                    : "[WSQ][UB] Ultimate Backup not found — skipping integration.");
            }
            catch (Exception ex)
            {
                _apiDetected = false;
                Game.LogTrivial("[WSQ][UB] Initialize Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Requests a backup unit using Ultimate Backup if available,
        /// otherwise falls back to a universal reflected RequestBackup call.
        /// </summary>
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
                        Game.LogTrivial($"[WSQ][UB] Requested Ultimate Backup preset: {presetName}");
                        return;
                    }
                }

                // ── Universal fallback (works even if enum types missing) ──
                SafeRequestBackup(spawn, 0, 1);  // Code2 LocalUnit
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] RequestBackup Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Requests a tactical (SWAT) backup unit.
        /// </summary>
        public static void RequestTacticalBackup(Vector3? position = null)
        {
            if (!_apiDetected)
            {
                SafeRequestBackup(position ?? Game.LocalPlayer.Character.Position, 0, 2); // Code2 SWAT
                return;
            }

            try
            {
                Vector3 spawn = position ?? Game.LocalPlayer.Character.Position;
                var method = _ubAPI.GetMethod("SpawnBackupUnit");
                if (method != null)
                {
                    method.Invoke(null, new object[] { "SWAT", spawn });
                    Game.LogTrivial("[WSQ][UB] SWAT team requested via Ultimate Backup.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] RequestTacticalBackup Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Requests an air support unit or fallback generic backup.
        /// </summary>
        public static void RequestAirSupport(Vector3? position = null)
        {
            if (!_apiDetected)
            {
                SafeRequestBackup(position ?? Game.LocalPlayer.Character.Position, 1, 1); // Code3 AirUnit
                return;
            }

            try
            {
                Vector3 spawn = position ?? Game.LocalPlayer.Character.Position;
                var method = _ubAPI.GetMethod("SpawnBackupUnit");
                if (method != null)
                {
                    method.Invoke(null, new object[] { "AirSupport", spawn });
                    Game.LogTrivial("[WSQ][UB] Air support unit requested via Ultimate Backup.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] RequestAirSupport Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Dismisses all backup units if supported by Ultimate Backup.
        /// </summary>
        public static void DismissAllBackup()
        {
            try
            {
                if (_apiDetected && _ubAPI != null)
                {
                    var m = _ubAPI.GetMethod("DismissAllBackupUnits");
                    if (m != null)
                    {
                        m.Invoke(null, null);
                        Game.LogTrivial("[WSQ][UB] Dismissed all Ultimate Backup units.");
                        return;
                    }
                }
                Game.LogTrivial("[WSQ][UB] DismissAllBackupUnits unavailable or UB inactive.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] DismissAllBackup Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Reflective universal RequestBackup wrapper for legacy safety.
        /// </summary>
        private static void SafeRequestBackup(Vector3 position, int respCode, int unitCode)
        {
            try
            {
                var method = typeof(LSPD_First_Response.Mod.API.Functions).GetMethod(
                    "RequestBackup",
                    new[] { typeof(Vector3), typeof(Enum), typeof(Enum) });

                if (method == null)
                {
                    Game.LogTrivial("[WSQ][UB] RequestBackup method not found (VR fallback).");
                    return;
                }

                var respEnum = Enum.ToObject(method.GetParameters()[1].ParameterType, respCode);
                var unitEnum = Enum.ToObject(method.GetParameters()[2].ParameterType, unitCode);
                method.Invoke(null, new object[] { position, respEnum, unitEnum });

                Game.LogTrivial("[WSQ][UB] RequestBackup invoked via reflection fallback.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][UB] SafeRequestBackup Exception: " + ex.Message);
            }
        }

        public static bool IsAvailable() => _apiDetected;

        public static void PrintIntegrationSummary()
        {
            Game.LogTrivial($"[WSQ][UB] Integration active={_apiDetected}, APIType={_ubAPI?.FullName ?? "null"}");
        }
    }
}