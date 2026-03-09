using System;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// ExternalPoliceComputerIntegration.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Universal compatibility layer for police computer or MDT-related plugins.
    ///  Provides optional reflection-based connection to a variety of external APIs:
    ///  - CompuLite (Albo1125)
    ///  - Police Smart Radio or MDT overlays
    ///  - RMS Extended / TACCall / SmartDispatch APIs
    ///  The system will dynamically adjust behavior depending on which API is found.
    /// </summary>
    public static class ExternalPoliceComputerIntegration
    {
        private static bool _initAttempted;
        private static bool _compuLiteDetected;
        private static bool _smartRadioDetected;
        private static bool _rmsDetected;

        private static Type _compuLiteAPI;
        private static Type _smartRadioAPI;
        private static Type _rmsAPI;

        /// <summary>
        /// Launches automated detection and loads available computer system APIs.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _compuLiteAPI = Type.GetType("CompuLite.API.CompuLiteAPI, CompuLite", false);
                _smartRadioAPI = Type.GetType("PoliceSmartRadio.API.RadioAPI, PoliceSmartRadio", false);
                _rmsAPI = Type.GetType("RMSExtended.API.RecordsAPI, RMSExtended", false);

                _compuLiteDetected = _compuLiteAPI != null;
                _smartRadioDetected = _smartRadioAPI != null;
                _rmsDetected = _rmsAPI != null;

                Game.LogTrivial($"[WSQ][MDC] Detection → CompuLite={_compuLiteDetected}, SmartRadio={_smartRadioDetected}, RMS={_rmsDetected}");

                if (!_compuLiteDetected && !_smartRadioDetected && !_rmsDetected)
                    Game.LogTrivial("[WSQ][MDC] No external police computer system found. Running default event logging only.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MDC] Initialize Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Adds a call entry to the external police computer log if supported.
        /// </summary>
        public static void SubmitCallEntry(string callTitle, string description, string code = "CODE 2")
        {
            try
            {
                if (_compuLiteDetected && _compuLiteAPI != null)
                {
                    var addEntry = _compuLiteAPI.GetMethod("AddCustomCalloutLogEntry");
                    addEntry?.Invoke(null, new object[] { "WSQ_Callouts", callTitle, description, code });
                    Game.LogTrivial($"[WSQ][MDC] CompuLite log entry added: {callTitle}");
                }

                if (_smartRadioDetected && _smartRadioAPI != null)
                {
                    var announceCall = _smartRadioAPI.GetMethod("AnnounceCall");
                    announceCall?.Invoke(null, new object[] { callTitle, description, code });
                    Game.LogTrivial($"[WSQ][MDC] Smart Radio dispatch broadcast: {callTitle}");
                }

                if (_rmsDetected && _rmsAPI != null)
                {
                    var recordCall = _rmsAPI.GetMethod("RegisterCallEntry");
                    recordCall?.Invoke(null, new object[] { callTitle, description, true });
                    Game.LogTrivial($"[WSQ][MDC] RMS entry recorded: {callTitle}");
                }

                if (!_compuLiteDetected && !_smartRadioDetected && !_rmsDetected)
                {
                    Game.LogTrivial($"[WSQ][MDC] Default internal log: {callTitle} — {description}");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MDC] SubmitCallEntry Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Updates an ongoing call entry (for progress tracking).
        /// </summary>
        public static void UpdateCallStatus(string callTitle, string newStatus)
        {
            try
            {
                if (_rmsDetected && _rmsAPI != null)
                {
                    var updateStatus = _rmsAPI.GetMethod("UpdateCallStatus");
                    updateStatus?.Invoke(null, new object[] { callTitle, newStatus });
                    Game.LogTrivial($"[WSQ][MDC] RMS Updated Call Status → {callTitle}: {newStatus}");
                }

                if (_compuLiteDetected && _compuLiteAPI != null)
                {
                    var updateNotes = _compuLiteAPI.GetMethod("AppendToCalloutEntry");
                    updateNotes?.Invoke(null, new object[] { callTitle, newStatus });
                    Game.LogTrivial($"[WSQ][MDC] CompuLite note appended: {callTitle}");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MDC] UpdateCallStatus Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Closes a call in all integrated systems once resolved.
        /// </summary>
        public static void CloseCall(string callTitle, bool successful = true)
        {
            try
            {
                string result = successful ? "Code 4 – Scene Secure" : "Incident Unresolved";
                
                if (_rmsDetected && _rmsAPI != null)
                {
                    var closeCall = _rmsAPI.GetMethod("CloseCall");
                    closeCall?.Invoke(null, new object[] { callTitle, result });
                }

                if (_compuLiteDetected && _compuLiteAPI != null)
                {
                    var finishCall = _compuLiteAPI.GetMethod("MarkCalloutAsComplete");
                    finishCall?.Invoke(null, new object[] { callTitle, result });
                }

                Game.LogTrivial($"[WSQ][MDC] Closed call: {callTitle} [{result}]");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][MDC] CloseCall Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Prints current integration summary to console/logs for diagnostics.
        /// </summary>
        public static void PrintIntegrationSummary()
        {
            Game.LogTrivial($"[WSQ][MDC] Systems: CompuLite={_compuLiteDetected}, SmartRadio={_smartRadioDetected}, RMS={_rmsDetected}");
        }
    }
}
