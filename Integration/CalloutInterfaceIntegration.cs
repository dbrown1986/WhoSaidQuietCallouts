using System;
using System.Reflection;
using Rage;
using WhoSaidQuietCallouts.Core;   // core utilities (e.g., Utilities.Log)

namespace WhoSaidQuietCallouts.Integration
{
    /// <summary>
    /// CalloutInterfaceIntegration.cs
    /// Version: 0.9.1 Alpha (Modular Reflection Build)
    /// Date: March 9, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Provides runtime / optional integration with FinKone’s Callout Interface plugin.
    ///  Uses reflection to check for CalloutInterface.dll and registers WSQ callouts if available.
    ///
    ///  No direct hard reference is required; ensures safe compilation when the user
    ///  does not have the external Callout Interface installed.
    /// </summary>
    public static class CalloutInterfaceIntegration
    {
        private static bool _available;
        private static Type _functionsType;
        private static MethodInfo _registerMethod;

        /// <summary>
        /// Attempts to detect Callout Interface and retrieve its registration method.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Attempt to load the assembly by name (must exist in Plugins/LSPDFR at runtime)
                Assembly ciAssembly = Assembly.Load("CalloutInterface");

                if (ciAssembly == null)
                {
                    Game.LogTrivial("[WSQ][CI] CalloutInterface.dll not found — integration skipped.");
                    _available = false;
                    return;
                }

                // Locate the main API class type
                _functionsType = ciAssembly.GetType("CalloutInterface.API.CalloutInterfaceFunctions");
                if (_functionsType == null)
                {
                    Game.LogTrivial("[WSQ][CI] API type not found inside assembly — skipping integration.");
                    _available = false;
                    return;
                }

                // Get the static RegisterCallout(Type calloutType) method
                _registerMethod = _functionsType.GetMethod("RegisterCallout", new[] { typeof(Type) });
                if (_registerMethod == null)
                {
                    Game.LogTrivial("[WSQ][CI] RegisterCallout(Type) method not found — skipping integration.");
                    _available = false;
                    return;
                }

                _available = true;
                Game.LogTrivial("[WSQ][CI] Callout Interface integration initialized successfully.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][CI] Initialize Exception: " + ex.Message);
                _available = false;
            }
        }

        /// <summary>
        /// Registers all WSQ callouts with Callout Interface, if available.
        /// Safe to call even if the assembly doesn't exist.
        /// </summary>
        public static void RegisterAllCallouts()
        {
            if (!_available)
            {
                Game.LogTrivial("[WSQ][CI] Not available — skipping callout registration.");
                return;
            }

            try
            {
                RegisterCalloutSafe(typeof(WhoSaidQuietCallouts.Callouts.PublicIntoxication));
                RegisterCalloutSafe(typeof(WhoSaidQuietCallouts.Callouts.Burglary));
                RegisterCalloutSafe(typeof(WhoSaidQuietCallouts.Callouts.SuicideAttempt));
                RegisterCalloutSafe(typeof(WhoSaidQuietCallouts.Callouts.DomesticDisturbance));
                // Add more WSQ callouts here as needed

                Game.LogTrivial("[WSQ][CI] All WSQ callouts registered through Callout Interface.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][CI] RegisterAllCallouts Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Safely invokes the reflection‑based RegisterCallout method.
        /// </summary>
        private static void RegisterCalloutSafe(Type calloutType)
        {
            if (_registerMethod == null) return;

            try
            {
                _registerMethod.Invoke(null, new object[] { calloutType });
                Game.LogTrivial($"[WSQ][CI] Registered callout: {calloutType.Name}");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][CI] Failed to register {calloutType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Reports whether Callout Interface integration is active and working.
        /// </summary>
        public static bool IsAvailable => _available;
    }
}