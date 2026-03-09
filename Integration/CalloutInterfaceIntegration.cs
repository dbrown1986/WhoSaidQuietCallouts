using System;
using System.Reflection;
using Rage;
using WhoSaidQuietCallouts.Core;   // core utilities (e.g., Utilities.Log)

namespace WhoSaidQuietCallouts.Integration
{
    /// <summary>
    /// CalloutInterfaceIntegration.cs
    /// Version: 0.9.5 Stable (Reflective Integration / Optional Plugin Bridge)
    /// Date: March 9 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Dynamically integrates WSQ callouts with FinKone’s Callout Interface plugin (if present).
    ///  Uses reflection to mirror the reflective WSQ Callout Registry (CalloutRegistrar)
    ///  automatically — no hard dependencies required.
    /// </summary>
    public static class CalloutInterfaceIntegration
    {
        private static bool _available;
        private static Type _functionsType;
        private static MethodInfo _registerMethod;

        /// <summary>
        /// Attempts to detect Callout Interface and retrieve its registration method.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Attempt to load the assembly by name (must exist in Plugins/LSPDFR at runtime)
                Assembly ciAssembly = Assembly.Load("CalloutInterface");

                if (ciAssembly == null)
                {
                    Game.LogTrivial("[WSQ][CI][v0.9.5] CalloutInterface.dll not found — integration skipped.");
                    _available = false;
                    return;
                }

                // Locate the main API class type
                _functionsType = ciAssembly.GetType("CalloutInterface.API.CalloutInterfaceFunctions");
                if (_functionsType == null)
                {
                    Game.LogTrivial("[WSQ][CI][v0.9.5] API type not found inside assembly — skipping integration.");
                    _available = false;
                    return;
                }

                // Get the static RegisterCallout(Type calloutType) method
                _registerMethod = _functionsType.GetMethod("RegisterCallout", new[] { typeof(Type) });
                if (_registerMethod == null)
                {
                    Game.LogTrivial("[WSQ][CI][v0.9.5] RegisterCallout(Type) method not found — skipping integration.");
                    _available = false;
                    return;
                }

                _available = true;
                Game.LogTrivial("[WSQ][CI][v0.9.5] Callout Interface integration initialized successfully.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][CI][v0.9.5] Initialize Exception: " + ex.Message);
                _available = false;
            }
        }

        /// <summary>
        /// Registers all WSQ callouts with Callout Interface, if available.
        /// Safe to call even if assembly doesn’t exist.
        /// </summary>
        public static void RegisterAllCallouts()
        {
            if (!_available)
            {
                Game.LogTrivial("[WSQ][CI][v0.9.5] Not available — skipping callout registration.");
                return;
            }

            try
            {
                // Reflective registration — use the same callout list as CalloutRegistrar
                foreach (var name in WhoSaidQuietCallouts.CalloutRegistrar.CalloutClassNames)
                {
                    Type calloutType = Type.GetType(name);
                    if (calloutType != null)
                        RegisterCalloutSafe(calloutType);
                    else
                        Game.LogTrivial($"[WSQ][CI][v0.9.5] Could not resolve type: {name}");
                }

                Game.LogTrivial("[WSQ][CI][v0.9.5] All WSQ callouts registered through Callout Interface.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][CI][v0.9.5] RegisterAllCallouts Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Safely invokes the reflection‑based RegisterCallout method.
        /// </summary>
        private static void RegisterCalloutSafe(Type calloutType)
        {
            if (_registerMethod == null) return;

            try
            {
                _registerMethod.Invoke(null, new object[] { calloutType });
                Game.LogTrivial($"[WSQ][CI][v0.9.5] Registered callout: {calloutType.Name}");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][CI][v0.9.5] Failed to register {calloutType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Reports whether Callout Interface integration is active and working.
        /// </summary>
        public static bool IsAvailable => _available;
    }
}