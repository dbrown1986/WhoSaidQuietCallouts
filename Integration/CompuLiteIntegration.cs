using System;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// CompuLiteIntegration.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    /// 
    /// Description:
    ///  Provides optional integration with the CompuLite plugin. 
    ///  When available, it registers callouts and metadata for display 
    ///  within CompuLite's user interface, providing officers with 
    ///  enhanced in-game records and callout previews.
    /// </summary>
    public static class CompuLiteIntegration
    {
        private static bool _initAttempted;
        private static Type _compuLiteInterfaceType;

        /// <summary>
        /// Attempts to connect with CompuLite when the plugin starts.
        /// Safe to call even if CompuLite is not installed — will gracefully skip.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _compuLiteInterfaceType = Type.GetType("CompuLite.API.CompuLiteAPI, CompuLite", false);

                if (_compuLiteInterfaceType != null)
                {
                    Game.LogTrivial("[WSQ][CompuLite] CompuLite API detected — integration active.");
                    RegisterCallouts();
                }
                else
                {
                    Game.LogTrivial("[WSQ][CompuLite] CompuLite not detected. Skipping integration.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][CompuLite] Initialization failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends this plugin’s callout metadata to CompuLite’s UI registry.
        /// </summary>
        private static void RegisterCallouts()
        {
            try
            {
                if (_compuLiteInterfaceType == null)
                {
                    Game.LogTrivial("[WSQ][CompuLite] RegisterCallouts() skipped — CompuLite not present.");
                    return;
                }

                var registerMethod = _compuLiteInterfaceType.GetMethod("RegisterExternalPluginEntry");
                if (registerMethod == null)
                {
                    Game.LogTrivial("[WSQ][CompuLite] RegisterExternalPluginEntry not found — API mismatch?");
                    return;
                }

                string header = "Who Said Quiet Callouts";
                string description =
                    "Community-driven collection of immersive LSPDFR callouts.\n" +
                    "Version 1.9.1 (d Revision) – March 7 2026\n" +
                    "Featuring tactical, behavioral, and narrative-based responses.";

                // Call API method
                registerMethod.Invoke(null, new object[]
                {
                    header,
                    description,
                    "WSQ_Callouts", // internal tag
                    "https://github.com/WhoSaidQuiet/LSPDFR" // safe placeholder link
                });

                Game.LogTrivial("[WSQ][CompuLite] Plugin registered successfully with CompuLite UI.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][CompuLite] RegisterCallouts Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Used internally when a callout begins — sends quick summary entry to CompuLite if supported.
        /// </summary>
        public static void SendActiveCalloutEntry(string calloutName, string details, string code = "CODE 3")
        {
            try
            {
                if (_compuLiteInterfaceType == null) return;

                var logMethod = _compuLiteInterfaceType.GetMethod("AddCustomCalloutLogEntry");
                if (logMethod == null) return;

                logMethod.Invoke(null, new object[] { "WSQCallouts", calloutName, details, code });
                Game.LogTrivial($"[WSQ][CompuLite] Sent callout entry: {calloutName}");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][CompuLite] SendActiveCalloutEntry Exception: " + ex.Message);
            }
        }
    }
}
