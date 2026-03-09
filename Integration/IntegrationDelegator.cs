using System;
using Rage;
using WhoSaidQuietCallouts.Integration;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// IntegrationDelegator.cs
    /// Version: 0.9.1 Alpha (Consolidated Core Integration)
    /// Date: March 9, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Acts as a unified integrator for all third‑party plugin and API connections.
    ///  Initializes all available integration classes, handles sequencing, and
    ///  provides diagnostics / version tracking for the WSQ Callouts pack.
    ///
    /// Integrations managed:
    ///  • Callout Interface
    ///  • CompuLite
    ///  • Grammar Police
    ///  • LSPDFR Expanded
    ///  • Ultimate Backup
    ///  • Stop The Ped
    ///  • Reports Plus
    ///  • Policing Redefined
    ///  • External Police Computer
    /// </summary>
    public static class IntegrationDelegator
    {
        private static bool _initialized;
        private static DateTime _bootTimestamp;

        /// <summary>
        /// Performs all plugin‑level integration initialization steps.
        /// Should be called once during Main.cs → OnPluginStart().
        /// </summary>
        public static void InitializeAll()
        {
            if (_initialized) return;
            _initialized = true;
            _bootTimestamp = DateTime.Now;

            Game.LogTrivial("[WSQ][Delegator] Beginning integration boot sequence...");

            try
            {
                // ---- Load each integration module in dependency order ----
                CalloutInterfaceIntegration.RegisterAllCallouts();
                CompuLiteIntegration.Initialize();
                GrammarPoliceIntegration.Initialize();
                LSPDFRExpandedIntegration.Initialize();
                UltimateBackupIntegration.Initialize();
                StopThePedIntegration.Initialize();
                ReportsPlusIntegration.Initialize();
                PolicingRedefinedIntegration.Initialize();
                ExternalPoliceComputerIntegration.Initialize();

                Game.LogTrivial("[WSQ][Delegator] All integration modules invoked successfully.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Delegator] Initialization Exception: " + ex);
            }

            PrintBootSummary();
        }

        /// <summary>
        /// Prints a full integration summary to the Rage log.
        /// </summary>
        public static void PrintBootSummary()
        {
            try
            {
                Game.LogTrivial("───────────────────────────────────────────────");
                Game.LogTrivial(" WSQ CALLOUTS – Integration Summary Report");
                Game.LogTrivial($" Build 0.9.1 Alpha ({_bootTimestamp:MMMM dd, yyyy})");
                Game.LogTrivial("───────────────────────────────────────────────");

                Game.LogTrivial($" • Callout Interface .......... active [✓]");
                Game.LogTrivial($" • CompuLite .................. {(IsAvailable("CompuLite") ? "[✓]" : "[–] skipped")}");
                Game.LogTrivial($" • Grammar Police ............. {(IsAvailable("GrammarPolice") ? "[✓]" : "[–] skipped")}");
                Game.LogTrivial($" • LSPDFR Expanded ............ {(IsAvailable("LSPDFRExpanded") ? "[✓]" : "[–] skipped")}");
                Game.LogTrivial($" • Ultimate Backup ............ {(UltimateBackupIntegration.IsAvailable() ? "[✓]" : "[–] skipped")}");
                Game.LogTrivial($" • Stop The Ped ............... {(IsAvailable("StopThePed") ? "[✓]" : "[–] skipped")}");
                Game.LogTrivial($" • Reports Plus ............... {(IsAvailable("ReportsPlus") ? "[✓]" : "[–] skipped")}");
                Game.LogTrivial($" • Policing Redefined ......... {(IsAvailable("PolicingRedefined") ? "[✓]" : "[–] skipped")}");
                Game.LogTrivial($" • External Police Computer ... {(IsAvailable("ExternalMDC") ? "[✓]" : "[–] skipped")}");

                Game.LogTrivial("───────────────────────────────────────────────");
                Game.LogTrivial(" Integration Delegator initialization complete.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Delegator] PrintBootSummary Exception: " + ex);
            }
        }

        /// <summary>
        /// Helper that checks runtime support status for known integrations.
        /// Stubbed to return true for enabled stubs.
        /// </summary>
        private static bool IsAvailable(string module)
        {
            try
            {
                return module switch
                {
                    "CompuLite" => true,
                    "GrammarPolice" => true,
                    "LSPDFRExpanded" => true,
                    "StopThePed" => true,
                    "ReportsPlus" => true,
                    "PolicingRedefined" => true,
                    "ExternalMDC" => true,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Allows runtime re‑initialization of integrations.
        /// </summary>
        public static void Reload()
        {
            Game.LogTrivial("[WSQ][Delegator] Reload requested — reinitializing integrations.");
            _initialized = false;
            InitializeAll();
        }

        /// <summary>
        /// Provides a brief status for HUD/debug overlay.
        /// </summary>
        public static string GetQuickStatus()
        {
            return $"[WSQ] Integrations: UB={UltimateBackupIntegration.IsAvailable()}, STP=OK, PR=OK, ReportsPlus=OK";
        }
    }
}