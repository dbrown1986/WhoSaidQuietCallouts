using System;
using System.IO;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// Logger.cs
    /// Version: 0.9.1 Alpha (Maintenance & Documentation Cleanup Build)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Centralized logging system for WSQ Callouts. 
    ///  Provides timestamped, categorized logs written both to RagePluginHook console 
    ///  and an optional text file (Logs/WSQ_Callouts.log).
    ///  Ensures consistent diagnostic output formatting across all modules.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "LSPDFR", "WhoSaidQuietCallouts", "Logs");
        private static string _logFile = Path.Combine(_logDirectory, "WSQ_Callouts.log");

        private static bool _initialized;
        private static bool _writeToFile = true;

        /// <summary>
        /// Initializes the logging subsystem (creates directories and file headers).
        /// Safe to call multiple times.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                if (!Directory.Exists(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);

                WriteFileSeparator();
                WritePlain($"[INIT] Who Said Quiet Callouts Logger initialized ({DateTime.Now:MMMM dd, yyyy HH:mm:ss}).");

                _initialized = true;
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Logger] Initialization failed: " + ex.Message);
                _writeToFile = false;
            }
        }

        /// <summary>
        /// Logs an informational line to console and file.
        /// </summary>
        public static void Info(string message)
        {
            Write("[INFO]", message, ConsoleColor.Gray);
        }

        /// <summary>
        /// Logs a success line.
        /// </summary>
        public static void Success(string message)
        {
            Write("[SUCCESS]", message, ConsoleColor.Green);
        }

        /// <summary>
        /// Logs a warning line (non‑fatal).
        /// </summary>
        public static void Warning(string message)
        {
            Write("[WARN]", message, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Logs an error or exception‑related message.
        /// </summary>
        public static void Error(string message, Exception ex = null)
        {
            string exceptionText = ex == null ? "" : " — " + ex.Message;
            Write("[ERROR]", message + exceptionText, ConsoleColor.Red);
        }

        /// <summary>
        /// Logs a debug line (only visible in file or if Release==false).
        /// </summary>
        public static void Debug(string message)
        {
#if DEBUG
            Write("[DEBUG]", message, ConsoleColor.Cyan);
#endif
        }

        /// <summary>
        /// Generic internal writer that handles console color and file writes.
        /// </summary>
        private static void Write(string prefix, string message, ConsoleColor color)
        {
            try
            {
                string formatted = $"{DateTime.Now:HH:mm:ss} {prefix} {message}";
                lock (_lock)
                {
                    Console.ForegroundColor = color;
                    Game.LogTrivial($"[WSQ]{formatted}");
                    Console.ResetColor();

                    if (_writeToFile)
                    {
                        File.AppendAllText(_logFile, formatted + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Logger] Write Exception: " + ex.Message);
                _writeToFile = false;
            }
        }

        /// <summary>
        /// Writes a simple header or divider line without prefix tags.
        /// </summary>
        public static void WritePlain(string message)
        {
            try
            {
                string formatted = $"{DateTime.Now:HH:mm:ss}  {message}";
                lock (_lock)
                {
                    Game.LogTrivial("[WSQ] " + formatted);
                    if (_writeToFile)
                        File.AppendAllText(_logFile, formatted + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Logger] WritePlain Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes a file section separator for readability.
        /// </summary>
        public static void WriteFileSeparator()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);

                string line = Environment.NewLine + "───────────────────────────────────────────────" + Environment.NewLine;
                if (_writeToFile)
                    File.AppendAllText(_logFile, line);
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Logger] WriteFileSeparator Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Records plugin termination in logs.
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                WritePlain("[SHUTDOWN] WSQ Logger terminated at " + DateTime.Now.ToString("HH:mm:ss"));
                WriteFileSeparator();
            }
            catch (Exception ex)
            {
                Game.LogTrivial("[WSQ][Logger] Shutdown Exception: " + ex.Message);
            }
        }
    }
}
