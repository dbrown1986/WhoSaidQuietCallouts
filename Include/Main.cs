using System;
using Rage;
using LSPD_First_Response.Mod.API;
using WhoSaidQuietCallouts.Core;

[assembly: Rage.Attributes.Plugin("Who Said Quiet Callouts", Author = "Who Said Quiet Team", Description = "Immersive and dynamic callout collection for LSPDFR.")]

namespace WhoSaidQuietCallouts
{
    /// <summary>
    /// Main.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Primary plugin entrypoint for the Who Said Quiet Callouts package.
    ///  Registers all callouts and executes integration initialization via IntegrationDelegator.
    /// </summary>
    public class Main : Plugin
    {
        private static DateTime _startupTime;

        public override void Initialize()
        {
            _startupTime = DateTime.Now;
            try
            {
                Game.LogTrivial("───────────────────────────────────────────────");
                Game.LogTrivial(" Who Said Quiet Callouts ✦ v0.9.5‑stable");
                Game.LogTrivial($" Build Date: March 9, 2026  |  Startup: {_startupTime:HH:mm:ss}");
                Game.LogTrivial("───────────────────────────────────────────────");

                // Initialize all integrations safely
                IntegrationDelegator.InitializeAll();

                // Final header confirmation
                Game.LogTrivial("[WSQ][Main] Initialization complete. Awaiting LSPDFR callout registration.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Main] Error during initialization: " + ex);
            }

            // Run within a GameFiber for safe startup queueing (avoiding Rage initialization race)
            GameFiber.StartNew(delegate
            {
                try
                {
                    // Register callouts once LSPDFR API available
                    RegisterWSQCallouts();
                    Game.LogTrivial("[WSQ][Main] All callouts registered successfully!");
                    Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                        "~b~Who Said Quiet Callouts",
                        "Version 0.9.5‑stable",
                        "~g~Successfully loaded.  Stay safe out there, Officer.");
                }
                catch (Exception ex)
                {
                    Game.LogTrivial("[WSQ][Main] GameFiber Exception: " + ex);
                }
            });
        }

        public override void Finally()
        {
            try
            {
                Game.LogTrivial("[WSQ][Main] Plugin terminated. Cleaning up resources.");
                Game.LogTrivial($"[WSQ][Main] Total runtime: {(DateTime.Now - _startupTime).TotalMinutes:0.00} minutes.");
                Game.LogTrivial("───────────────────────────────────────────────");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Main] Finally() Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Handles registration of all callouts with the LSPDFR engine.
        /// </summary>
        private void RegisterWSQCallouts()
        {
            try
            {
                CalloutRegistrar.RegisterAll();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Main] Callout registration failed: " + ex);
            }
        }
    }
}
