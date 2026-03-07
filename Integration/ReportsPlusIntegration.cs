using System;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// ReportsPlusIntegration.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Safe reflection-based interface with "Reports Plus" by Albo1125 and community builds.
    ///  Allows WSQ Callouts to automatically generate and file incident summaries,
    ///  enhancing realism through written report continuity.
    ///  If Reports Plus is not installed, this integration remains inactive.
    /// </summary>
    public static class ReportsPlusIntegration
    {
        private static bool _initAttempted;
        private static bool _available;
        private static Type _rpApi;

        /// <summary>
        /// Checks for Reports Plus installation and prepares integration.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _rpApi = Type.GetType("ReportsPlus.API.ReportAPI, ReportsPlus", false);
                _available = _rpApi != null;

                if (_available)
                    Game.LogTrivial("[WSQ][ReportsPlus] Reports Plus detected — integration active.");
                else
                    Game.LogTrivial("[WSQ][ReportsPlus] Reports Plus not found — skipping integration.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ReportsPlus] Initialize Exception: " + ex.Message);
                _available = false;
            }
        }

        /// <summary>
        /// Submits a detailed report entry for the specified callout to Reports Plus.
        /// </summary>
        /// <param name="calloutName">The internal or display name of the callout.</param>
        /// <param name="summary">Incident summary or brief description.</param>
        /// <param name="officerSuccess">True if the event was resolved successfully.</param>
        public static void SubmitIncidentReport(string calloutName, string summary, bool officerSuccess = true)
        {
            if (!_available || _rpApi == null) return;

            try
            {
                var submitMethod = _rpApi.GetMethod("SubmitIncidentReport");
                if (submitMethod != null)
                {
                    submitMethod.Invoke(null, new object[] { calloutName, summary, officerSuccess });
                    Game.LogTrivial($"[WSQ][ReportsPlus] Submitted report for: {calloutName}");
                }
                else
                {
                    Game.LogTrivial("[WSQ][ReportsPlus] SubmitIncidentReport method not available on current API version.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ReportsPlus] SubmitIncidentReport Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Logs the officer’s performance metrics (time, arrests, injuries) into Reports Plus.
        /// </summary>
        public static void SubmitPerformanceStats(string calloutName, int arrests, int injuredSuspects, float durationMinutes)
        {
            if (!_available || _rpApi == null) return;

            try
            {
                var perfMethod = _rpApi.GetMethod("SubmitPerformanceStats");
                if (perfMethod != null)
                {
                    perfMethod.Invoke(null, new object[] { calloutName, arrests, injuredSuspects, durationMinutes });
                    Game.LogTrivial($"[WSQ][ReportsPlus] Logged performance for {calloutName}: " +
                                    $"Arrests={arrests}, Injured={injuredSuspects}, Duration={durationMinutes:0.0} min");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ReportsPlus] SubmitPerformanceStats Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the last saved report ID for cross-plugin linking (if available).
        /// </summary>
        public static int GetLastReportId()
        {
            if (!_available || _rpApi == null) return -1;

            try
            {
                var prop = _rpApi.GetProperty("LastGeneratedReportId");
                if (prop != null)
                {
                    int id = Convert.ToInt32(prop.GetValue(null));
                    return id;
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][ReportsPlus] GetLastReportId Exception: " + ex.Message);
            }

            return -1;
        }

        /// <summary>
        /// Prints a diagnostic summary to logs for version control confirmation.
        /// </summary>
        public static void PrintIntegrationSummary()
        {
            Game.LogTrivial($"[WSQ][ReportsPlus] Integration status: Available={_available}, APIType={_rpApi?.FullName ?? "null"}");
        }
    }
}
