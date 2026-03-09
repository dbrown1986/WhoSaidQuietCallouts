using System;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// StopThePedIntegration.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Safe reflection-based interface for "Stop The Ped" (STP) by BejoIjo.
    ///  Enables WSQ callouts to notify, register, and control suspects or pedestrians
    ///  for investigation, pat‑down, questioning, or handoff to backup units.
    ///  Automatically disables if Stop The Ped is not detected.
    /// </summary>
    public static class StopThePedIntegration
    {
        private static bool _initAttempted;
        private static bool _detected;
        private static Type _stpAPI;

        /// <summary>
        /// Checks whether Stop The Ped is installed and accessible.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _stpAPI = Type.GetType("StopThePed.API.Functions, StopThePed", false);
                _detected = _stpAPI != null;

                if (_detected)
                    Game.LogTrivial("[WSQ][STP] Stop The Ped API detected — integration enabled.");
                else
                    Game.LogTrivial("[WSQ][STP] Stop The Ped not installed — skipping integration.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][STP] Initialize Exception: " + ex.Message);
                _detected = false;
            }
        }

        /// <summary>
        /// Creates a notepad log entry inside STP’s suspect info panel.
        /// </summary>
        public static void AddSuspectNote(Ped suspect, string note)
        {
            if (!_detected || _stpAPI == null || !suspect || !suspect.Exists()) return;

            try
            {
                var method = _stpAPI.GetMethod("AddPedInteractionNotification");
                if (method != null)
                {
                    method.Invoke(null, new object[] { suspect, note });
                    Game.LogTrivial($"[WSQ][STP] Added suspect note: {note}");
                }
                else
                {
                    Game.LogTrivial("[WSQ][STP] AddPedInteractionNotification method unavailable.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][STP] AddSuspectNote Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Flags a ped as "detained" or "under arrest" in STP’s internal registry.
        /// </summary>
        public static void MarkPedDetained(Ped suspect, bool arrested = false)
        {
            if (!_detected || _stpAPI == null || !suspect || !suspect.Exists()) return;

            try
            {
                var markMethod = _stpAPI.GetMethod("FlagPedAsDetained");
                if (markMethod != null)
                {
                    markMethod.Invoke(null, new object[] { suspect, arrested });
                    Game.LogTrivial($"[WSQ][STP] Flagged ped as {(arrested ? "arrested" : "detained")}.");
                }
                else
                {
                    Game.LogTrivial("[WSQ][STP] FlagPedAsDetained method missing — older STP version?");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][STP] MarkPedDetained Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Triggers Stop The Ped’s search/pat‑down routine for the given suspect.
        /// </summary>
        public static void RequestPatDown(Ped suspect)
        {
            if (!_detected || _stpAPI == null || !suspect || !suspect.Exists()) return;

            try
            {
                var searchMethod = _stpAPI.GetMethod("CallPedSearch");
                if (searchMethod != null)
                {
                    searchMethod.Invoke(null, new object[] { suspect });
                    Game.LogTrivial("[WSQ][STP] Triggered pat‑down on suspect.");
                }
                else
                {
                    Game.LogTrivial("[WSQ][STP] CallPedSearch method missing in current STP build.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][STP] RequestPatDown Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Plays a predefined STP conversation line for immersion.
        /// </summary>
        public static void PlaySTPDialogue(string key)
        {
            if (!_detected || _stpAPI == null) return;

            try
            {
                var talkMethod = _stpAPI.GetMethod("PlaySpeechLine");
                talkMethod?.Invoke(null, new object[] { key });
                Game.LogTrivial($"[WSQ][STP] Played STP dialogue: {key}");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][STP] PlaySTPDialogue Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the STP integration state (for debugging or logging).
        /// </summary>
        public static string GetStatus()
        {
            return $"StopThePed Integration Active = {_detected}";
        }
    }
}
