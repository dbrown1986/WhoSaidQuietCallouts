using System;
using System.IO;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// WSQSettings.cs
    /// Version: 0.9.1 Alpha (Maintenance & Configuration Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Global configuration handler for Who Said Quiet Callouts.
    ///  Loads and saves user preferences from WSQ_Settings.ini.
    ///  Provides helper accessors for commonly used parameters
    ///  (e.g., AI difficulty, backup callout delay, debug mode toggles, etc.).
    /// </summary>
    public static class WSQSettings
    {
        private static string _settingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "LSPDFR", "WhoSaidQuietCallouts");
        private static string _settingsFile = Path.Combine(_settingsFolder, "WSQ_Settings.ini");

        // --- Runtime Settings (default values) ---
        public static bool EnableDebugLogs { get; private set; } = false;
        public static bool AllowHighRiskCalls { get; private set; } = true;
        public static bool IntegrationsEnabled { get; private set; } = true;
        public static bool EnableImmersionAudio { get; private set; } = true;
        public static int MinCalloutCooldownSeconds { get; private set; } = 90;
        public static int MaxCalloutCooldownSeconds { get; private set; } = 180;

        public static float AmbientCrimeDensity { get; private set; } = 1.0f; // 100%
        public static string PreferredBackupPreset { get; private set; } = "LocalPatrol";

        private static bool _initialized;

        /// <summary>
        /// Initializes and loads settings from file (creates default if missing).
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // Ensure directory exists
                if (!Directory.Exists(_settingsFolder))
                    Directory.CreateDirectory(_settingsFolder);

                if (!File.Exists(_settingsFile))
                {
                    WriteDefaultFile();
                    Game.LogTrivial("[WSQ][Settings] Created default WSQ_Settings.ini.");
                }

                LoadValues();
                _initialized = true;
                Game.LogTrivial("[WSQ][Settings] Settings loaded successfully.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Settings] Initialization Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Reads settings from the .ini file line by line.
        /// </summary>
        private static void LoadValues()
        {
            try
            {
                var lines = File.ReadAllLines(_settingsFile);
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    switch (key)
                    {
                        case "EnableDebugLogs":
                            EnableDebugLogs = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;

                        case "AllowHighRiskCalls":
                            AllowHighRiskCalls = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;

                        case "IntegrationsEnabled":
                            IntegrationsEnabled = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;

                        case "EnableImmersionAudio":
                            EnableImmersionAudio = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;

                        case "MinCalloutCooldownSeconds":
                            int.TryParse(val, out int min);
                            MinCalloutCooldownSeconds = Math.Clamp(min, 30, 600);
                            break;

                        case "MaxCalloutCooldownSeconds":
                            int.TryParse(val, out int max);
                            MaxCalloutCooldownSeconds = Math.Clamp(max, 60, 900);
                            break;

                        case "AmbientCrimeDensity":
                            float.TryParse(val, out float density);
                            AmbientCrimeDensity = Math.Clamp(density, 0.0f, 5.0f);
                            break;

                        case "PreferredBackupPreset":
                            PreferredBackupPreset = val;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Settings] LoadValues Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes a default settings file
