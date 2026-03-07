using System;
using LSPD_First_Response.Mod.API;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// CalloutInterfaceIntegration.cs
    /// Version: 1.9.1 (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  This manager provides registration, version tracking,
    ///  and optional compatibility with third-party callout interfaces.
    ///  It centralizes callout initialization logic for the entire WSQCallouts package.
    /// </summary>
    public static class CalloutInterfaceIntegration
    {
        // Version and metadata
        public static readonly string PackageName = "Who Said Quiet Callouts";
        public static readonly string PackageVersion = "1.9.1";
        public static readonly string BuildDate = "March 7, 2026";

        private static bool _initialized;
        private static readonly string[] _calloutNames =
        {
            "GangShootout",
            "Burglary",
            "AnimalAttack",
            "StolenVehicle",
            "OfficerDown",
            "RoadRage",
            "BarricadedSuspects",
            "SpeedingVehicle",
            "MissingPerson",
            "DrugDeal",
            "VIPEscort",
            "TrafficStopAssist",
            "WelfareCheck",
            "StolenPoliceVehicle",
            "SuicideAttempt"
        };

        /// <summary>
        /// Entry method called during plugin initialization (from Main.cs OnPluginStart)
        /// </summary>
        public static void RegisterAllCallouts()
        {
            if (_initialized) return;

            Game.LogTrivial($"[WSQ][Init] Registering {_calloutNames.Length} callouts.");
            foreach (string name in _calloutNames)
            {
                try
                {
                    Functions.RegisterCallout($"WhoSaidQuietCallouts.Callouts.{name}");
                    Game.LogTrivial($"[WSQ][Register] {name} successfully registered.");
                }
                catch (Exception ex)
                {
                    Game.LogTrivial($"[WSQ][Error] Failed to register {name}: {ex.Message}");
                }
            }

            _initialized = true;
            Game.LogTrivial($"[WSQ][Init] {_calloutNames.Length} callouts active — build {PackageVersion}, {BuildDate}.");
            TryAnnounceToCalloutInterface();
        }

        /// <summary>
        /// Attempts non-breaking compatibility with Callout Interface (if installed).
        /// </summary>
        private static void TryAnnounceToCalloutInterface()
        {
            try
            {
                Type calloutInterfaceType = Type.GetType("CalloutInterfaceAPI.CalloutInterface, CalloutInterface", false);
                if (calloutInterfaceType != null)
                {
                    Game.LogTrivial("[WSQ][Interface] Detected Callout Interface API. Sending registration event...");
                    calloutInterfaceType.GetMethod("SendMessageToInterface")?.Invoke(null, new object[]
                    {
                        "WSQCallouts", $"Registered {PackageVersion} ({BuildDate})"
                    });
                }
                else
                {
                    Game.LogTrivial("[WSQ][Interface] Callout Interface not found; skipping announcement.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Interface] Compatibility operation failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Displays basic info in console/log about active version.
        /// Useful for diagnostics and ensuring config load.
        /// </summary>
        public static void PrintHeaderBanner()
        {
            string banner = $@"
  ┌────────────────────────────────────────────┐
  │  Who Said Quiet Callouts                   │
  │  Version {PackageVersion}   Build {BuildDate} │
  │  Core Integration Module Initialized        │
  └────────────────────────────────────────────┘";

            Game.LogTrivial(banner);
        }
    }
}
