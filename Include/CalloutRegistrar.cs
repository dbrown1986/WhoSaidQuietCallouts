using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// CalloutRegistrar.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Centralized class responsible for registering all callouts included in the WSQ package.
    ///  This defines the master callout list, version header, and runtime diagnostics output.
    ///  Designed to be referenced by IntegrationDelegator, Main.cs, or plugin startup fibers.
    /// </summary>
    public static class CalloutRegistrar
    {
        /// <summary>
        /// Ordered list of all callout class names within this package.
        /// </summary>
        public static readonly List<string> CalloutClassNames = new List<string>
        {
            "WhoSaidQuietCallouts.Callouts.GangShootout",
            "WhoSaidQuietCallouts.Callouts.Burglary",
            "WhoSaidQuietCallouts.Callouts.AnimalAttack",
            "WhoSaidQuietCallouts.Callouts.StolenVehicle",
            "WhoSaidQuietCallouts.Callouts.OfficerDown",
            "WhoSaidQuietCallouts.Callouts.RoadRage",
            "WhoSaidQuietCallouts.Callouts.BarricadedSuspects",
            "WhoSaidQuietCallouts.Callouts.SpeedingVehicle",
            "WhoSaidQuietCallouts.Callouts.MissingPerson",
            "WhoSaidQuietCallouts.Callouts.DrugDeal",
            "WhoSaidQuietCallouts.Callouts.VIPEscort",
            "WhoSaidQuietCallouts.Callouts.TrafficStopAssist",
            "WhoSaidQuietCallouts.Callouts.WelfareCheck",
            "WhoSaidQuietCallouts.Callouts.StolenPoliceVehicle",
            "WhoSaidQuietCallouts.Callouts.SuicideAttempt"
        };

        /// <summary>
        /// Current package version (used by Plugin and registry).
        /// </summary>
        public static readonly string Version = "1.9.1-d";
        public static readonly string BuildDate = "March 7, 2026";

        private static bool _registered;
        private static DateTime _registerTime;

        /// <summary>
        /// Executes the registration of all WSQ callouts using LSPDFR’s API.
        /// Prevents duplicate or redundant registrations by enforcing singleton pattern.
        /// </summary>
        public static void RegisterAll()
        {
            if (_registered) return;

            try
            {
                Game.LogTrivial("───────────────────────────────────────────────");
                Game.LogTrivial($"[WSQ][Registrar] Registering {CalloutClassNames.Count} callouts (Build {Version})...");

                foreach (string className in CalloutClassNames)
                {
                    try
                    {
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.ArmedRobbery));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.PursuitSuspect));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.DomesticDisturbance));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.SuspiciousVehicle));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.Kidnapping));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.GangShootout));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.Burglary));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.AnimalAttack));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.PublicIntoxication));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.StolenVehicle));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.OfficerDown));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.RoadRage));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.BarricadedSuspects));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.SpeedingVehicle));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.MissingPerson));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.DrugDeal));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.VIPEscort));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.TrafficStopAssist));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.WelfareCheck));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.StolenPoliceVehicle));
                        Functions.RegisterCallout(typeof(WhoSaidQuietCallouts.Callouts.SuicideAttempt));
                        Game.LogTrivial($"[WSQ][Registrar] Registered: {className}");
                    }
                    catch (Exception ex)
                    {
                        Game.LogTrivial($"[WSQ][Registrar][Error] Failed to register {className}: {ex.Message}");
                    }
                }

                _registered = true;
                _registerTime = DateTime.Now;
                Game.LogTrivial($"[WSQ][Registrar] Successfully initialized {CalloutClassNames.Count} callouts.");
                Game.LogTrivial($"[WSQ][Registrar] Build {Version} — {BuildDate} @ {_registerTime:HH:mm:ss}");
                Game.LogTrivial("───────────────────────────────────────────────");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Registrar] Fatal Exception During Registration: " + ex.Message);
            }
        }

        /// <summary>
        /// Lists all currently loaded callouts and their status in console/log file.
        /// </summary>
        public static void PrintSummary()
        {
            Game.LogTrivial("────────── WSQ Callout Registry Summary ──────────");
            for (int i = 0; i < CalloutClassNames.Count; i++)
            {
                string c = CalloutClassNames[i];
                Game.LogTrivial($"  {(i + 1):00}. {c}");
            }
            Game.LogTrivial("──────────────────────────────────────────────────");
        }

        /// <summary>
        /// Allows easy retrieval of a callout class name by index.
        /// </summary>
        public static string GetCalloutNameByIndex(int index)
        {
            if (index < 0 || index >= CalloutClassNames.Count)
                return null;
            return CalloutClassNames[index];
        }

        /// <summary>
        /// Returns total registered count.
        /// </summary>
        public static int Count => CalloutClassNames.Count;

        /// <summary>
        /// Clears the registry state (for developer hot‑reload or integration testing).
        /// </summary>
        public static void ResetRegistry()
        {
            _registered = false;
            _registerTime = DateTime.MinValue;
            Game.LogTrivial("[WSQ][Registrar] Registry reset complete. Callouts may be re‑registered safely.");
        }
    }
}
