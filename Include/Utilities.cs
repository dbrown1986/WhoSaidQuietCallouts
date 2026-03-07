using System;
using System.Collections.Generic;
using System.Linq;
using Rage;

namespace WhoSaidQuietCallouts.Core
{
    /// <summary>
    /// Utilities.cs
    /// Version: 1.9.1 (Core Utility Module)
    /// Date: March 7, 2026
    /// Author: Who Said Quiet Team
    ///
    /// Description:
    ///  Provides general helper functions reused throughout WSQ Callouts.
    ///  Includes math, string, location, and common Rage API convenience methods.
    ///  This class is safe to call from any thread (unless otherwise noted).
    /// </summary>
    public static class Utilities
    {
        private static readonly Random RNG = new Random();

        // ---------------------------- RANDOMIZATION ----------------------------

        /// <summary>
        /// Returns a random integer between given min and max values (inclusive of min, exclusive of max).
        /// </summary>
        public static int RandomInt(int min, int max)
        {
            if (max <= min) return min;
            return RNG.Next(min, max);
        }

        /// <summary>
        /// Returns a random float between given bounds.
        /// </summary>
        public static float RandomFloat(float min, float max)
        {
            if (Math.Abs(max - min) < 0.0001f) return min;
            return (float)(RNG.NextDouble() * (max - min) + min);
        }

        /// <summary>
        /// Returns true or false based on the specified probability (0–100%).
        /// </summary>
        public static bool Chance(int percent)
        {
            return RNG.Next(0, 100) < percent;
        }

        /// <summary>
        /// Shuffles a list in place using Fisher–Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = RNG.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        // ---------------------------- WORLD HELPERS ----------------------------

        /// <summary>
        /// Safely spawns a blip at a location with the given color and radius.
        /// </summary>
        public static Blip CreateBlip(Vector3 position, float radius, System.Drawing.Color color, string name = "")
        {
            var blip = new Blip(position, radius);
            blip.Color = color;
            blip.Alpha = 0.8f;
            if (!string.IsNullOrEmpty(name))
                blip.Name = name;
            return blip;
        }

        /// <summary>
        /// Deletes a blip if it exists, ignoring deletion errors.
        /// </summary>
        public static void SafeDeleteBlip(Blip blip)
        {
            try
            {
                if (blip && blip.Exists()) blip.Delete();
            }
            catch { /* ignored for safety */ }
        }

        /// <summary>
        /// Safely dismisses a ped or vehicle (calls .Dismiss() if valid).
        /// </summary>
        public static void SafeDismissEntity(Entity ent)
        {
            try
            {
                if (ent && ent.Exists()) ent.Dismiss();
            }
            catch { /* ignored */ }
        }

        /// <summary>
        /// Returns the player’s current vehicle if valid; null otherwise.
        /// </summary>
        public static Vehicle GetPlayerVehicle()
        {
            try
            {
                var player = Game.LocalPlayer.Character;
                if (player && player.IsInAnyVehicle(false))
                    return player.CurrentVehicle;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Finds a random nearby street position near a given vector within a radius.
        /// </summary>
        public static Vector3 GetNearbyStreet(Vector3 center, float radius = 300f)
        {
            try
            {
                return World.GetNextPositionOnStreet(center.Around(radius));
            }
            catch
            {
                return center;
            }
        }

        // ---------------------------- TEXT / STRING HELPERS ----------------------------

        /// <summary>
        /// Formats seconds into "MM:SS" for log readability.
        /// </summary>
        public static string ToTimeStamp(int seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return $"{t.Minutes:D2}:{t.Seconds:D2}";
        }

        /// <summary>
        /// Returns “Yes” or “No” for a boolean, optionally colored with game text codes.
        /// </summary>
        public static string BoolToText(bool value, bool formatted = false)
        {
            if (!formatted)
                return value ? "Yes" : "No";

            return value ? "~g~Yes~s~" : "~r~No~s~";
        }

        /// <summary>
        /// Capitalizes the first letter of a string safely.
        /// </summary>
        public static string Capitalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Truncates a string if longer than max length.
        /// </summary>
        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length > maxLength ? text[..maxLength] + "..." : text;
        }

        // ---------------------------- MATH HELPERS ----------------------------

        /// <summary>
        /// Clamps a float value between min and max.
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Linearly interpolates between values A and B by factor T.
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0f, 1f);
        }

        /// <summary>
        /// Converts miles per hour to meters per second.
        /// </summary>
        public static float MphToMetersPerSecond(float mph)
        {
            return mph * 0.44704f;
        }

        /// <summary>
        /// Converts meters per second to mph.
        /// </summary>
        public static float MpsToMph(float mps)
        {
            return mps / 0.44704f;
        }

        // ---------------------------- LOGGING ----------------------------

        /// <summary>
        /// Quick shortcut for module-based console logging.
        /// </summary>
        public static void Log(string module, string message)
        {
            Game.LogTrivial($"[WSQ][{module}] {message}");
        }

        /// <summary>
        /// Logs an exception message with detail.
        /// </summary>
        public static void LogException(string module, Exception ex)
        {
            Game.LogTrivial($"[WSQ][{module}] Exception: {ex.Message}");
        }

        // ---------------------------- PLAYER UTILITIES ----------------------------

        /// <summary>
        /// Checks whether player is on foot (not in vehicle).
        /// </summary>
        public static bool PlayerIsOnFoot()
        {
            try
            {
                var player = Game.LocalPlayer.Character;
                return player && !player.IsInAnyVehicle(false);
            }
            catch { return false; }
        }

        /// <summary>
        /// Checks whether a vector is within proximity range of another (2D plane).
        /// </summary>
        public static bool WithinRange(Vector3 a, Vector3 b, float distance)
        {
            try
            {
                return a.DistanceTo(b) <= distance;
            }
            catch { return false; }
        }
    }
}
