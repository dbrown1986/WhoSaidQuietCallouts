using System;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// GrammarPoliceIntegration.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Optional integration layer for Grammar Police by Albo1125.
    ///  Provides rich dispatch dialogue and dynamic callout descriptions.
    ///  The module automatically detects Grammar Police and safely emits
    ///  narrative call details for immersive radio chatter.
    /// </summary>
    public static class GrammarPoliceIntegration
    {
        private static bool _initAttempted;
        private static Type _gpCalloutManager;
        private static bool _available;

        /// <summary>
        /// Initializes Grammar Police compatibility and binds to its CalloutManager.
        /// Safe to run regardless of Grammar Police installation.
        /// </summary>
        public static void Initialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                _gpCalloutManager = Type.GetType("GrammarPolice.CalloutManager, GrammarPolice", false);
                if (_gpCalloutManager != null)
                {
                    _available = true;
                    Game.LogTrivial("[WSQ][GrammarPolice] Grammar Police detected — integration ready.");
                }
                else
                {
                    Game.LogTrivial("[WSQ][GrammarPolice] Grammar Police not detected. Skipping integration.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GrammarPolice] Initialization Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Broadcasts the callout’s description to Grammar Police for radio narration.
        /// </summary>
        /// <param name="calloutName">The internal class name of the callout.</param>
        /// <param name="description">The spoken/narrative description.</param>
        /// <param name="location">The world position (optional).</param>
        public static void AnnounceCallout(string calloutName, string description, Vector3? location = null)
        {
            if (!_available || _gpCalloutManager == null) return;

            try
            {
                var sendMethod = _gpCalloutManager.GetMethod("AnnounceCallout");
                if (sendMethod != null)
                {
                    string locString = (location.HasValue)
                        ? $"{location.Value.X:0.0}, {location.Value.Y:0.0}"
                        : "unknown location";

                    sendMethod.Invoke(null, new object[] { calloutName, description, locString });
                    Game.LogTrivial($"[WSQ][GrammarPolice] Announced callout '{calloutName}' via Grammar Police.");
                }
                else
                {
                    Game.LogTrivial("[WSQ][GrammarPolice] AnnounceCallout method unavailable in API version.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GrammarPolice] AnnounceCallout Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends contextual unit radio chatter if Grammar Police supports real-time speech addition.
        /// </summary>
        public static void BroadcastDispatchSpeech(string messageKey)
        {
            if (!_available || _gpCalloutManager == null) return;

            try
            {
                var speechMethod = _gpCalloutManager.GetMethod("PlaySpeech");
                if (speechMethod == null)
                {
                    Game.LogTrivial("[WSQ][GrammarPolice] BroadcastDispatchSpeech() unavailable — API mismatch.");
                    return;
                }

                speechMethod.Invoke(null, new object[] { messageKey });
                Game.LogTrivial($"[WSQ][GrammarPolice] Played dispatch key: {messageKey}");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GrammarPolice] BroadcastDispatchSpeech Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Helper for robust registration with Grammar Police’s call list system if exposed.
        /// Includes basic metadata for mission board visibility.
        /// </summary>
        public static void RegisterWithGrammarPoliceList(string calloutName, string displayTitle, string synopsis)
        {
            if (!_available || _gpCalloutManager == null) return;

            try
            {
                var regMethod = _gpCalloutManager.GetMethod("RegisterExternalCallout");
                if (regMethod != null)
                {
                    regMethod.Invoke(null, new object[] { calloutName, displayTitle, synopsis });
                    Game.LogTrivial($"[WSQ][GrammarPolice] Registered '{displayTitle}' for callout list display.");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][GrammarPolice] RegisterWithGrammarPoliceList Exception: " + ex.Message);
            }
        }
    }
}
