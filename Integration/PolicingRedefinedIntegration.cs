using System;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// PolicingRedefinedIntegration.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Optional integration with "Policing Redefined" framework.
    ///  Enables cross‑plugin functionality such as:
    ///  - Officer personality and fatigue tracking
    ///  - Dynamic behavioral escalation/de‑escalation modeling
    ///  - Cross‑callout continuity (arrests, trust, morale)
    ///  If Policing Redefined is missing, this integration silently disables itself.
    /// </summary>
    public static class PolicingRedefinedIntegration
    {
        private static bool _initAttempted;
        private static bool _available;
        private static Type _prAPI;

        /// <summary>
        /// Initializes the integration and checks for Policing Redefined presence.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _prAPI = Type.GetType("PolicingRedefined.API.Handler, PolicingRedefined", false);
                _available = _prAPI != null;

                if (_available)
                    Game.LogTrivial("[WSQ][PR] Policing Redefined detected — integration online.");
                else
                    Game.LogTrivial("[WSQ][PR] Policing Redefined not detected — skipping integration.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PR] Initialize Exception: " + ex.Message);
                _available = false;
            }
        }

        /// <summary>
        /// Logs that a WSQ callout has started for internal behavioral tracking within PR.
        /// </summary>
        public static void LogCalloutStart(string calloutName, string situationSummary)
        {
            if (!_available || _prAPI == null) return;

            try
            {
                var logMethod = _prAPI.GetMethod("RecordExternalCalloutStart");
                if (logMethod != null)
                {
                    logMethod.Invoke(null, new object[] { calloutName, situationSummary });
                    Game.LogTrivial($"[WSQ][PR] Logged callout start to Policing Redefined: {calloutName}");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PR] LogCalloutStart Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Notifies PR that a callout has successfully ended. Can increment officer reputation or stress metrics.
        /// </summary>
        public static void LogCalloutCompletion(string calloutName, bool success = true)
        {
            if (!_available || _prAPI == null) return;

            try
            {
                var completeMethod = _prAPI.GetMethod("RecordExternalCalloutFinish");
                completeMethod?.Invoke(null, new object[] { calloutName, success });
                Game.LogTrivial($"[WSQ][PR] Logged callout completion: {calloutName} (Success={success})");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PR] LogCalloutCompletion Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends a soft emotional / stress cue to PR’s officer profile system.
        /// Allows dynamic stress/fatigue modeling per incident.
        /// </summary>
        public static void AddStressImpact(float intensity, string reason = "Incident Response")
        {
            if (!_available || _prAPI == null) return;

            try
            {
                var stressMethod = _prAPI.GetMethod("ApplyStressEffect");
                stressMethod?.Invoke(null, new object[] { intensity, reason });
                Game.LogTrivial($"[WSQ][PR] Stress impact applied: intensity={intensity:0.00} ({reason})");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PR] AddStressImpact Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends officer reputation changes (for commendations or penalties).
        /// </summary>
        public static void AdjustOfficerReputation(float delta, string reason = "Report Outcome")
        {
            if (!_available || _prAPI == null) return;

            try
            {
                var repMethod = _prAPI.GetMethod("AdjustOfficerReputation");
                repMethod?.Invoke(null, new object[] { delta, reason });
                Game.LogTrivial($"[WSQ][PR] Officer reputation adjusted by {delta:+0.0;-0.0} ({reason}).");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PR] AdjustOfficerReputation Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Queries current fatigue or stress values (if supported by Policing Redefined).
        /// Returns -1 if unavailable.
        /// </summary>
        public static float GetCurrentFatigueLevel()
        {
            if (!_available || _prAPI == null) return -1f;

            try
            {
                var fatigueProp = _prAPI.GetProperty("CurrentFatigueLevel");
                if (fatigueProp != null)
                {
                    float val = Convert.ToSingle(fatigueProp.GetValue(null));
                    return val;
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][PR] GetCurrentFatigueLevel Exception: " + ex.Message);
            }

            return -1f;
        }

        /// <summary>
        /// Provides clear log summary for diagnostics.
        /// </summary>
        public static void PrintIntegrationSummary()
        {
            Game.LogTrivial($"[WSQ][PR] Integration status: Available={_available}, APIType={_prAPI?.FullName ?? "null"}");
        }
    }
}
