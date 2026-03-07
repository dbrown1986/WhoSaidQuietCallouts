using System;
using Rage;
using LSPD_First_Response.Mod.API;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// LSPDFRExpandedIntegration.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Provides safe, modular compatibility hooks for "LSPDFR Expanded" ecosystem mods.
    ///  Each feature is optional — integration auto-detects supported APIs such as:
    ///  - Ultimate Backup
    ///  - Stop the Ped (STP)
    ///  - Traffic Policer / Arrest Manager
    ///  When not detected, this system silently skips integration to maintain stability.
    /// </summary>
    public static class LSPDFRExpandedIntegration
    {
        private static bool _initAttempted;
        private static bool _ultimateBackupAvailable;
        private static bool _stopThePedAvailable;
        private static bool _arrestManagerAvailable;

        private static Type _ubAPI;
        private static Type _stpAPI;
        private static Type _arrestAPI;

        /// <summary>
        /// Initializes compatibility checks and binds to known plugin APIs.
        /// Safe to call multiple times — executes only once.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                DetectUltimateBackup();
                DetectStopThePed();
                DetectArrestManager();

                Game.LogTrivial($"[WSQ][LSPDFR+Init] UltimateBackup={_ultimateBackupAvailable} | StopThePed={_stopThePedAvailable} | ArrestManager={_arrestManagerAvailable}");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+Init] Initialization Exception: " + ex);
            }
        }

        #region Detection Methods

        private static void DetectUltimateBackup()
        {
            try
            {
                _ubAPI = Type.GetType("UltimateBackup.API.Functions, UltimateBackup", false);
                _ultimateBackupAvailable = _ubAPI != null;
                if (_ultimateBackupAvailable)
                    Game.LogTrivial("[WSQ][LSPDFR+] Ultimate Backup detected.");
            }
            catch { _ultimateBackupAvailable = false; }
        }

        private static void DetectStopThePed()
        {
            try
            {
                _stpAPI = Type.GetType("StopThePed.API.Functions, StopThePed", false);
                _stopThePedAvailable = _stpAPI != null;
                if (_stopThePedAvailable)
                    Game.LogTrivial("[WSQ][LSPDFR+] Stop The Ped detected.");
            }
            catch { _stopThePedAvailable = false; }
        }

        private static void DetectArrestManager()
        {
            try
            {
                _arrestAPI = Type.GetType("ArrestManager.API.Main, ArrestManager", false);
                _arrestManagerAvailable = _arrestAPI != null;
                if (_arrestManagerAvailable)
                    Game.LogTrivial("[WSQ][LSPDFR+] Arrest Manager detected.");
            }
            catch { _arrestManagerAvailable = false; }
        }

        #endregion

        #region Ultimate Backup Integration

        /// <summary>
        /// Requests backup using Ultimate Backup if installed.
        /// Otherwise uses the standard LSPDFR backup system.
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
                        Game.LogTrivial($"[WSQ][LSPDFR+] Ultimate Backup request sent: {presetName}");
                        return;
                    }
                }

                // fallback to vanilla LSPDFR backup
                Functions.RequestBackup(position, EBackupResponseType.Code3, EBackupUnitType.LocalUnit);
                Game.LogTrivial("[WSQ][LSPDFR+] Used default LSPDFR backup request.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+] RequestSmartBackup Exception: " + ex.Message);
            }
        }

        #endregion

        #region Stop The Ped Integration

        /// <summary>
        /// Adds a suspect check command for STP arrest menu if supported.
        /// </summary>
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
                        Game.LogTrivial($"[WSQ][LSPDFR+] Sent StopThePed notification: {reason}");
                        return;
                    }
                }

                Game.LogTrivial("[WSQ][LSPDFR+] StopThePed not available, skipped suspect notification.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+] NotifyStopThePed Exception: " + ex.Message);
            }
        }

        #endregion

        #region Arrest Manager Integration

        /// <summary>
        /// Transfers suspect to Arrest Manager system if available, 
        /// or falls back to a vanilla dismiss/arrest animation.
        /// </summary>
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
                        Game.LogTrivial("[WSQ][LSPDFR+] Suspect transferred via Arrest Manager API.");
                        return;
                    }
                }

                // fallback
                if (suspect && suspect.Exists())
                {
                    suspect.Tasks.Cower(-1);
                    Game.DisplaySubtitle("~o~Arrest Manager not detected — process arrest manually.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][LSPDFR+] TransferToArrestManager Exception: " + ex.Message);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Returns a summary string of detected LSPDFR ecosystem plugins.
        /// </summary>
        public static string GetIntegrationSummary()
        {
            return $"UltimateBackup={_ultimateBackupAvailable}, StopThePed={_stopThePedAvailable}, ArrestManager={_arrestManagerAvailable}";
        }

        #endregion
    }
}
