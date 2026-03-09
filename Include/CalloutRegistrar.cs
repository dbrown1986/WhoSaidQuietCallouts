using System;
using System.Collections.Generic;
using Rage;
using LSPD_First_Response.Mod.API;
using WhoSaidQuietCallouts.Core;

namespace WhoSaidQuietCallouts
{
    /// <summary>
    /// CalloutRegistrar.cs
    /// Version: 0.9.5 Stable (Reflective Registry / Navigation Aware)
    /// Updated March 9 2026 by Who Said Quiet Team.
    ///
    /// Description:
    ///  Registers Who Said Quiet Callouts with LSPDFR using a reflective, config‑driven approach.
    ///  Supports dynamic callout type discovery and logs Radar/GPS navigation preference.
    ///  Reflective integration removes hard dependencies on external plugin assemblies.
    /// </summary>
    public static class CalloutRegistrar
    {
        public static readonly string Version = "0.9.5‑stable";
        public static readonly string BuildDate = "March 9 2026";

        /// <summary>
        /// Public read‑only list of all WSQ Callout class names for dynamic reflection.
        /// </summary>
        public static IReadOnlyList<string> CalloutClassNames { get; } = new List<string>
        {
            "WhoSaidQuietCallouts.Callouts.ArmedRobbery",
            "WhoSaidQuietCallouts.Callouts.PursuitSuspect",
            "WhoSaidQuietCallouts.Callouts.StolenVehicle",
            "WhoSaidQuietCallouts.Callouts.MissingPerson",
            "WhoSaidQuietCallouts.Callouts.SuspiciousVehicle",
            "WhoSaidQuietCallouts.Callouts.DomesticDisturbance",
            "WhoSaidQuietCallouts.Callouts.TrafficStopAssist",
            "WhoSaidQuietCallouts.Callouts.PublicIntoxication",
            "WhoSaidQuietCallouts.Callouts.SuicideAttempt",
            "WhoSaidQuietCallouts.Callouts.WelfareCheck",
            "WhoSaidQuietCallouts.Callouts.VIPEscort",
            "WhoSaidQuietCallouts.Callouts.RoadRage",
            "WhoSaidQuietCallouts.Callouts.BarricadedSuspects",
            "WhoSaidQuietCallouts.Callouts.GangShootout",
            "WhoSaidQuietCallouts.Callouts.Burglary",
            "WhoSaidQuietCallouts.Callouts.AnimalAttack",
            "WhoSaidQuietCallouts.Callouts.SpeedingVehicle",
            "WhoSaidQuietCallouts.Callouts.OfficerDown",
            "WhoSaidQuietCallouts.Callouts.StolenPoliceVehicle",
            "WhoSaidQuietCallouts.Callouts.DrugDeal",
        };

        /// <summary>
        /// Registers all callouts reflectively with LSPDFR and outputs initialization metadata.
        /// </summary>
        public static void RegisterAll()
        {
            try
            {
                Game.LogTrivial("[WSQ][Registrar] ─────────────────────────────────────");
                Game.LogTrivial($"[WSQ][Registrar] Initializing Who Said Quiet Callouts {Version}");
                Game.LogTrivial($"[WSQ][Registrar] Build Date: {BuildDate}");
                Game.LogTrivial($"[WSQ][Registrar] Reflective Integration: Enabled");
                Game.LogTrivial($"[WSQ][Registrar] Navigation Preference: " +
                    (WSQSettings.UseRadarBlipsInsteadOfGPS ? "Radar Blips" : "GPS Routes"));
                Game.LogTrivial("[WSQ][Registrar] Registering callouts …");

                int registeredCount = 0;

                foreach (var className in CalloutClassNames)
                {
                    try
                    {
                        Type calloutType = Type.GetType(className);
                        if (calloutType != null)
                        {
                            Functions.RegisterCallout(calloutType);
                            Game.LogTrivial($"[WSQ][Registrar] → Registered: {className}");
                            registeredCount++;
                        }
                        else
                        {
                            Game.LogTrivial($"[WSQ][Registrar][Warning] Could not resolve type: {className}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Game.LogTrivial($"[WSQ][Registrar][Error] Failed to register {className}: {ex.Message}");
                    }
                }

                Game.LogTrivial($"[WSQ][Registrar] Successfully registered {registeredCount}/{CalloutClassNames.Count} callouts.");
                Game.LogTrivial("[WSQ][Registrar] Initialization complete.");
                Game.LogTrivial("[WSQ][Registrar] ─────────────────────────────────────");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Registrar][Critical] Initialization failed: {ex.Message}");
            }
        }
    }
}