using System;
using System.IO;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// WSQSettings.cs (Settings Manager)
    /// Version: 0.9.5 Stable (Reflective Integration / Radar–GPS Preference Synced)
    /// Date: March 9 2026
    /// Author: Who Said Quiet Team
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
        public static bool UseRadarBlipsInsteadOfGPS { get; private set; } = false;

        // ───── Integrations ─────
        public static bool AllowStopThePed { get; private set; }
        public static bool AllowCompuLite { get; private set; }
        public static bool AllowGrammarPolice { get; private set; }
        public static bool AllowCalloutInterface { get; private set; }
        public static bool AllowUltimateBackup { get; private set; }
        public static bool AllowLSPDFRExpanded { get; private set; }
        public static bool AllowPolicingRedefined { get; private set; }
        public static bool AllowReportsPlus { get; private set; }
        public static bool AllowExternalPoliceComputer { get; private set; }

        // ───── Callouts ─────
        public static bool ArmedRobbery { get; private set; } = true;
        public static bool PursuitSuspect { get; private set; } = true;
        public static bool DomesticDisturbance { get; private set; } = true;
        public static bool SuspiciousVehicle { get; private set; } = true;
        public static bool Kidnapping { get; private set; } = true;
        public static bool GangShootout { get; private set; } = true;
        public static bool Burglary { get; private set; } = true;
        public static bool AnimalAttack { get; private set; } = true;
        public static bool PublicIntoxication { get; private set; } = true;
        public static bool StolenVehicle { get; private set; } = true;
        public static bool OfficerDown { get; private set; } = true;
        public static bool RoadRage { get; private set; } = true;
        public static bool BarricadedSuspects { get; private set; } = true;
        public static bool SpeedingVehicle { get; private set; } = true;
        public static bool MissingPerson { get; private set; } = true;
        public static bool DrugDeal { get; private set; } = true;
        public static bool VIPEscort { get; private set; } = true;
        public static bool TrafficStopAssist { get; private set; } = true;
        public static bool WelfareCheck { get; private set; } = true;
        public static bool StolenPoliceVehicle { get; private set; } = true;

        // ───── SuicideCallout ─────
        public static bool SuicideAttempt { get; private set; }
        public static bool EnableResponseTimer { get; private set; } = true;
        public static bool EnableHiddenEMSTimer { get; private set; } = true;
        public static bool EnableHelplineOverlay { get; private set; } = true;

        private static bool _initialized;

        // ──────────────────────────────────────────────────────────────────
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
                foreach (string raw in File.ReadAllLines(_settingsFile))
                {
                    string line = raw.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") ||
                        line.StartsWith("#") || line.StartsWith("["))
                        continue;

                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    switch (key)
                    {
                        // --- General ---
                        case "EnableLogging": EnableLogging = ParseBool(val); break;
                        case "LogLevel":
                            if (int.TryParse(val, out int log)) LogLevel = Clamp(log, 0, 3);
                            break;
                        case "MinCalloutCooldownSeconds":
                            if (int.TryParse(val, out int min)) MinCalloutCooldownSeconds = Clamp(min, 5, 900);
                            break;
                        case "MaxCalloutCooldownSeconds":
                            if (int.TryParse(val, out int max)) MaxCalloutCooldownSeconds = Clamp(max, 10, 1800);
                            break;
                        case "UseRadarBlipsInsteadOfGPS":
                            UseRadarBlipsInsteadOfGPS = ParseBool(val);
                            break;

                        // --- Integrations ---
                        case "StopThePed": AllowStopThePed = ParseBool(val); break;
                        case "CompuLite": AllowCompuLite = ParseBool(val); break;
                        case "GrammarPolice": AllowGrammarPolice = ParseBool(val); break;
                        case "CalloutInterface": AllowCalloutInterface = ParseBool(val); break;
                        case "UltimateBackup": AllowUltimateBackup = ParseBool(val); break;
                        case "LSPDFRExpanded": AllowLSPDFRExpanded = ParseBool(val); break;
                        case "PolicingRedefined": AllowPolicingRedefined = ParseBool(val); break;
                        case "ReportsPlus": AllowReportsPlus = ParseBool(val); break;
                        case "ExternalPoliceComputer": AllowExternalPoliceComputer = ParseBool(val); break;

                        // --- Callouts ---
                        case "ArmedRobbery": ArmedRobbery = ParseBool(val); break;
                        case "PursuitSuspect": PursuitSuspect = ParseBool(val); break;
                        case "DomesticDisturbance": DomesticDisturbance = ParseBool(val); break;
                        case "SuspiciousVehicle": SuspiciousVehicle = ParseBool(val); break;
                        case "Kidnapping": Kidnapping = ParseBool(val); break;
                        case "GangShootout": GangShootout = ParseBool(val); break;
                        case "Burglary": Burglary = ParseBool(val); break;
                        case "AnimalAttack": AnimalAttack = ParseBool(val); break;
                        case "PublicIntoxication": PublicIntoxication = ParseBool(val); break;
                        case "StolenVehicle": StolenVehicle = ParseBool(val); break;
                        case "OfficerDown": OfficerDown = ParseBool(val); break;
                        case "RoadRage": RoadRage = ParseBool(val); break;
                        case "BarricadedSuspects": BarricadedSuspects = ParseBool(val); break;
                        case "SpeedingVehicle": SpeedingVehicle = ParseBool(val); break;
                        case "MissingPerson": MissingPerson = ParseBool(val); break;
                        case "DrugDeal": DrugDeal = ParseBool(val); break;
                        case "VIPEscort": VIPEscort = ParseBool(val); break;
                        case "TrafficStopAssist": TrafficStopAssist = ParseBool(val); break;
                        case "WelfareCheck": WelfareCheck = ParseBool(val); break;
                        case "StolenPoliceVehicle": StolenPoliceVehicle = ParseBool(val); break;

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
                    w.WriteLine("; Version: 0.9.4 Stable (Reflective Integration Sync Build)");
                    w.WriteLine($"; Date: {DateTime.Now:MMMM dd, yyyy}");
                    w.WriteLine("; -----------------------------------------------------------------------");
                    w.WriteLine("; This file controls global plugin settings, integrations, and callout behavior.");
                    w.WriteLine("; Comments (lines starting with ';') are ignored by the plugin.");
                    w.WriteLine("; =======================================================================");
                    w.WriteLine();
                    w.WriteLine("[General]");
                    w.WriteLine("; Enables or disables log file creation.");
                    w.WriteLine("EnableLogging=true");
                    w.WriteLine("; Sets the level of verbosity for log output: 0=Normal 3=FullDebug");
                    w.WriteLine("LogLevel=1");
                    w.WriteLine("; Randomized delay range between callouts (seconds)");
                    w.WriteLine("MinCalloutCooldownSeconds=30");
                    w.WriteLine("MaxCalloutCooldownSeconds=300");
                    w.WriteLine();
                    w.WriteLine("; Determines navigation assist type for callouts.");
                    w.WriteLine("; true  = use radar blips only (adds challenge)");
                    w.WriteLine("; false = use GPS route guidance (default)");
                    w.WriteLine("UseRadarBlipsInsteadOfGPS=false");
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
                    w.WriteLine("[Callouts]");
                    w.WriteLine("ArmedRobbery=true");
                    w.WriteLine("PursuitSuspect=true");
                    w.WriteLine("DomesticDisturbance=true");
                    w.WriteLine("SuspiciousVehicle=true");
                    w.WriteLine("Kidnapping=true");
                    w.WriteLine("GangShootout=true");
                    w.WriteLine("Burglary=true");
                    w.WriteLine("AnimalAttack=true");
                    w.WriteLine("PublicIntoxication=true");
                    w.WriteLine("StolenVehicle=true");
                    w.WriteLine("OfficerDown=true");
                    w.WriteLine("RoadRage=true");
                    w.WriteLine("BarricadedSuspects=true");
                    w.WriteLine("SpeedingVehicle=true");
                    w.WriteLine("MissingPerson=true");
                    w.WriteLine("DrugDeal=true");
                    w.WriteLine("VIPEscort=true");
                    w.WriteLine("TrafficStopAssist=true");
                    w.WriteLine("WelfareCheck=true");
                    w.WriteLine("StolenPoliceVehicle=true");
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

        private static bool ParseBool(string v)
            => v.Equals("true", StringComparison.OrdinalIgnoreCase);

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ───── Integration Helper ─────
        public static bool IsIntegrationEnabled(string name)
        {
            return name switch
            {
                "StopThePed" => AllowStopThePed && PluginBridge.IsPluginLoaded("StopThePed"),
                "UltimateBackup" => AllowUltimateBackup && PluginBridge.IsPluginLoaded("UltimateBackup"),
                "ReportsPlus" => AllowReportsPlus && PluginBridge.IsPluginLoaded("ReportsPlus"),
                "CompuLite" => AllowCompuLite && PluginBridge.IsPluginLoaded("CompuLite"),
                "GrammarPolice" => AllowGrammarPolice && PluginBridge.IsPluginLoaded("GrammarPolice"),
                "CalloutInterface" => AllowCalloutInterface && PluginBridge.IsPluginLoaded("CalloutInterface"),
                "LSPDFRExpanded" => AllowLSPDFRExpanded && PluginBridge.IsPluginLoaded("LSPDFRExpanded"),
                "PolicingRedefined" => AllowPolicingRedefined && PluginBridge.IsPluginLoaded("PolicingRedefined"),
                "ExternalPoliceComputer" => AllowExternalPoliceComputer && PluginBridge.IsPluginLoaded("ExternalPoliceComputer"),
                _ => false,
            };
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
                   $"UB={AllowUltimateBackup}, STP={AllowStopThePed}, GP={AllowGrammarPolice}, " +
                   $"CalloutsEnabled={true}, Suicide={SuicideAttempt}";
        }
    }
}