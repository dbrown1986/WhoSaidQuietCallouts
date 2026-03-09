using System;
using System.IO;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// WSQSettings.cs (Settings Manager)
    /// Version: 0.9.1 Alpha (Compatibility Build)
    /// Date: March 9, 2026
    /// Author: Who Said Quiet Team
    /// </summary>
    public static class WSQSettings
    {
        private static readonly string _settingsFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "LSPDFR", "WhoSaidQuietCallouts");
        private static readonly string _settingsFile =
            Path.Combine(_settingsFolder, "WhoSaidQuietCallouts.ini");

        // ───── General ─────
        public static bool EnableLogging { get; private set; } = true;
        public static int LogLevel { get; private set; } = 1;
        public static int MinCalloutCooldownSeconds { get; private set; } = 30;
        public static int MaxCalloutCooldownSeconds { get; private set; } = 300;

        // ───── Integrations ─────
        public static bool StopThePed { get; private set; }
        public static bool CompuLite { get; private set; }
        public static bool GrammarPolice { get; private set; }
        public static bool CalloutInterface { get; private set; }
        public static bool UltimateBackup { get; private set; }
        public static bool LSPDFRExpanded { get; private set; }
        public static bool PolicingRedefined { get; private set; }
        public static bool ReportsPlus { get; private set; }
        public static bool ExternalPoliceComputer { get; private set; }

        // ───── SuicideCallout ─────
        public static bool SuicideAttempt { get; private set; }
        public static bool EnableResponseTimer { get; private set; } = true;
        public static bool EnableHiddenEMSTimer { get; private set; } = true;
        public static bool EnableHelplineOverlay { get; private set; } = true;

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                if (!Directory.Exists(_settingsFolder))
                    Directory.CreateDirectory(_settingsFolder);

                if (!File.Exists(_settingsFile))
                {
                    WriteDefaultFile();
                    Game.LogTrivial("[WSQ][Settings] Created default WhoSaidQuietCallouts.ini.");
                }

                LoadValues();
                _initialized = true;
                Game.LogTrivial("[WSQ][Settings] Settings loaded successfully.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Settings] Initialize Exception: " + ex.Message);
            }
        }

        private static void LoadValues()
        {
            try
            {
                foreach (var raw in File.ReadAllLines(_settingsFile))
                {
                    string line = raw.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["))
                        continue;

                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    switch (key)
                    {
                        // --- General ---
                        case "EnableLogging":
                            EnableLogging = ParseBool(val); break;
                        case "LogLevel":
                            if (int.TryParse(val, out int logLevel))
                                LogLevel = Clamp(logLevel, 0, 3);
                            break;
                        case "MinCalloutCooldownSeconds":
                            if (int.TryParse(val, out int min))
                                MinCalloutCooldownSeconds = Clamp(min, 5, 900);
                            break;
                        case "MaxCalloutCooldownSeconds":
                            if (int.TryParse(val, out int max))
                                MaxCalloutCooldownSeconds = Clamp(max, 10, 1800);
                            break;

                        // --- Integrations ---
                        case "StopThePed": StopThePed = ParseBool(val); break;
                        case "CompuLite": CompuLite = ParseBool(val); break;
                        case "GrammarPolice": GrammarPolice = ParseBool(val); break;
                        case "CalloutInterface": CalloutInterface = ParseBool(val); break;
                        case "UltimateBackup": UltimateBackup = ParseBool(val); break;
                        case "LSPDFRExpanded": LSPDFRExpanded = ParseBool(val); break;
                        case "PolicingRedefined": PolicingRedefined = ParseBool(val); break;
                        case "ReportsPlus": ReportsPlus = ParseBool(val); break;
                        case "ExternalPoliceComputer": ExternalPoliceComputer = ParseBool(val); break;

                        // --- SuicideCallout ---
                        case "SuicideAttempt": SuicideAttempt = ParseBool(val); break;
                        case "EnableResponseTimer": EnableResponseTimer = ParseBool(val); break;
                        case "EnableHiddenEMSTimer": EnableHiddenEMSTimer = ParseBool(val); break;
                        case "EnableHelplineOverlay": EnableHelplineOverlay = ParseBool(val); break;
                        default: break;
                    }
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Settings] LoadValues Exception: " + ex.Message);
            }
        }

        private static void WriteDefaultFile()
        {
            try
            {
                using (StreamWriter w = new StreamWriter(_settingsFile, false))
                {
                    w.WriteLine("; =======================================================================");
                    w.WriteLine("; WHO SAID QUIET CALLOUTS - CONFIGURATION FILE");
                    w.WriteLine("; Version: 0.9.1 Alpha (Compatibility Build)");
                    w.WriteLine($"; Date: {DateTime.Now:MMMM dd, yyyy}");
                    w.WriteLine("; -----------------------------------------------------------------------");
                    w.WriteLine("[General]");
                    w.WriteLine("EnableLogging=true");
                    w.WriteLine("LogLevel=1");
                    w.WriteLine("MinCalloutCooldownSeconds=30");
                    w.WriteLine("MaxCalloutCooldownSeconds=300");
                    w.WriteLine();
                    w.WriteLine("[Integrations]");
                    w.WriteLine("StopThePed=false");
                    w.WriteLine("CompuLite=false");
                    w.WriteLine("GrammarPolice=false");
                    w.WriteLine("CalloutInterface=false");
                    w.WriteLine("UltimateBackup=false");
                    w.WriteLine("LSPDFRExpanded=false");
                    w.WriteLine("PolicingRedefined=false");
                    w.WriteLine("ReportsPlus=false");
                    w.WriteLine("ExternalPoliceComputer=false");
                    w.WriteLine();
                    w.WriteLine("[SuicideCallout]");
                    w.WriteLine("SuicideAttempt=false");
                    w.WriteLine("EnableResponseTimer=true");
                    w.WriteLine("EnableHiddenEMSTimer=true");
                    w.WriteLine("EnableHelplineOverlay=true");
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Settings] WriteDefaultFile Exception: " + ex.Message);
            }
        }

        private static bool ParseBool(string v) =>
            v.Equals("true", StringComparison.OrdinalIgnoreCase);

        // ✅ Manual clamp works in .NET 4.8
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static void Reload()
        {
            try
            {
                LoadValues();
                Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                    "~b~Who Said Quiet Callouts~s~", "Reload Complete",
                    "Settings reloaded successfully.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Settings] Reload Exception: " + ex.Message);
            }
        }

        public static string Summary()
        {
            return $"[WSQ Settings] LogLevel={LogLevel}, " +
                   $"Cooldown={MinCalloutCooldownSeconds}-{MaxCalloutCooldownSeconds}s, " +
                   $"UB={UltimateBackup}, STP={StopThePed}, GP={GrammarPolice}, Suicide={SuicideAttempt}";
        }
    }
}