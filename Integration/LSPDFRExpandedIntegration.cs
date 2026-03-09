using System;
using Rage;
using LSPD_First_Response.Mod.API;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// LSPDFRExpandedIntegration.cs
    /// Version 0.9.2 Stable (March 9 2026)
    /// Safe reflection‑based integration without Microsoft.CSharp binder dependency.
    /// </summary>
    public static class LSPDFRExpandedIntegration
    {
        private enum EBackupResponseType { Code2, Code3 }
        private enum EBackupUnitType { LocalUnit, SWAT, AirUnit }

        private static bool _initAttempted;
        private static bool _ultimateBackupAvailable;
        private static bool _stopThePedAvailable;
        private static bool _arrestManagerAvailable;

        private static Type _ubAPI;
        private static Type _stpAPI;
        private static Type _arrestAPI;

        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                DetectUltimateBackup();
                DetectStopThePed();
                DetectArrestManager();

                Game.LogTrivial($"[WSQ][LSPDFR+] Init → UB:{_ultimateBackupAvailable} | STP:{_stopThePedAvailable} | AM:{_arrestManagerAvailable}");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+Init] Initialization Exception: " + ex);
            }
        }

        #region Detection
        private static void DetectUltimateBackup()
        {
            try
            {
                _ubAPI = Type.GetType("UltimateBackup.API.Functions, UltimateBackup", false);
                _ultimateBackupAvailable = _ubAPI != null;
                if (_ultimateBackupAvailable) Game.LogTrivial("[WSQ][LSPDFR+] Ultimate Backup detected.");
            }
            catch { _ultimateBackupAvailable = false; }
        }

        private static void DetectStopThePed()
        {
            try
            {
                _stpAPI = Type.GetType("StopThePed.API.Functions, StopThePed", false);
                _stopThePedAvailable = _stpAPI != null;
                if (_stopThePedAvailable) Game.LogTrivial("[WSQ][LSPDFR+] Stop The Ped detected.");
            }
            catch { _stopThePedAvailable = false; }
        }

        private static void DetectArrestManager()
        {
            try
            {
                _arrestAPI = Type.GetType("ArrestManager.API.Main, ArrestManager", false);
                _arrestManagerAvailable = _arrestAPI != null;
                if (_arrestManagerAvailable) Game.LogTrivial("[WSQ][LSPDFR+] Arrest Manager detected.");
            }
            catch { _arrestManagerAvailable = false; }
        }
        #endregion

        #region Ultimate Backup Integration
        /// <summary>
        /// Requests backup using Ultimate Backup if installed,
        /// otherwise calls LSPDFR RequestBackup reflectively (no binder).
        /// </summary>
        public static void RequestSmartBackup(Vector3 position, string presetName = "LocalPatrol")
        {
            try
            {
                if (_ultimateBackupAvailable && _ubAPI != null)
                {
                    var spawnBackupMethod = _ubAPI.GetMethod("SpawnBackupUnit");
                    if (spawnBackupMethod != null)
                    {
                        spawnBackupMethod.Invoke(null, new object[] { presetName, position });
                        Game.LogTrivial($"[WSQ][LSPDFR+] UB request sent: {presetName}");
                        return;
                    }
                }

                // ── Universal fallback via reflection (no System.Dynamic) ──
                var method = typeof(LSPD_First_Response.Mod.API.Functions).GetMethod(
                    "RequestBackup",
                    new[] { typeof(Vector3), typeof(Enum), typeof(Enum) });

                if (method != null)
                {
                    var respEnum = Enum.ToObject(method.GetParameters()[1].ParameterType, 1); // Code3
                    var unitEnum = Enum.ToObject(method.GetParameters()[2].ParameterType, 0); // LocalUnit
                    method.Invoke(null, new object[] { position, respEnum, unitEnum });
                    Game.LogTrivial("[WSQ][LSPDFR+] Reflected vanilla backup request (Code 3).");
                }
                else
                {
                    Game.LogTrivial("[WSQ][LSPDFR+] RequestBackup method not found – no action taken.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+] RequestSmartBackup Exception: " + ex.Message);
            }
        }
        #endregion

        #region Stop The Ped Integration
        public static void NotifyStopThePed(Ped suspect, string reason)
        {
            try
            {
                if (_stopThePedAvailable && _stpAPI != null && suspect && suspect.Exists())
                {
                    var notifyMethod = _stpAPI.GetMethod("AddPedInteractionNotification");
                    if (notifyMethod != null)
                    {
                        notifyMethod.Invoke(null, new object[] { suspect, reason });
                        Game.LogTrivial($"[WSQ][LSPDFR+] STP notification sent: {reason}");
                        return;
                    }
                }

                Game.LogTrivial("[WSQ][LSPDFR+] Stop The Ped not available – skipped.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+] NotifyStopThePed Exception: " + ex.Message);
            }
        }
        #endregion

        #region Arrest Manager Integration
        public static void TransferToArrestManager(Ped suspect)
        {
            try
            {
                if (_arrestManagerAvailable && _arrestAPI != null)
                {
                    var transferMethod = _arrestAPI.GetMethod("SendPedToStation");
                    if (transferMethod != null)
                    {
                        transferMethod.Invoke(null, new object[] { suspect });
                        Game.LogTrivial("[WSQ][LSPDFR+] Arrest Manager transfer complete.");
                        return;
                    }
                }

                // fallback
                if (suspect && suspect.Exists())
                {
                    suspect.Tasks.Cower(-1);
                    Game.DisplaySubtitle("~o~Arrest Manager not detected — process arrest manually.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+] TransferToArrestManager Exception: " + ex.Message);
            }
        }
        #endregion

        #region Utility
        public static string GetIntegrationSummary() =>
            $"UltimateBackup={_ultimateBackupAvailable}, StopThePed={_stopThePedAvailable}, ArrestManager={_arrestManagerAvailable}";
        #endregion
    }
}