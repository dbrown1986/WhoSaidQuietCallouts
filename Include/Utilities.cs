using System;
using System.Collections.Generic;
using System.Linq;
using Rage;

namespace WhoSaidQuietCallouts.Core

/// Version: 0.9.5 Stable (Utility Compatibility Verified)
/// Updated: March 9 2026 by Who Said Quiet Team

{
    public static class Utilities
    {
        private static readonly Random RNG = new Random();

        // ---------------------------- RANDOMIZATION ----------------------------

        public static int RandomInt(int min, int max)
        {
            if (max <= min) return min;
            return RNG.Next(min, max);
        }

        public static float RandomFloat(float min, float max)
        {
            if (Math.Abs(max - min) < 0.0001f) return min;
            return (float)(RNG.NextDouble() * (max - min) + min);
        }

        public static bool Chance(int percent) => RNG.Next(0, 100) < percent;

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

        public static Blip CreateBlip(Vector3 position, float radius, System.Drawing.Color color, string name = "")
        {
            var blip = new Blip(position, radius)
            {
                Color = color,
                Alpha = 0.8f
            };
            if (!string.IsNullOrEmpty(name))
                blip.Name = name;
            return blip;
        }

        public static void SafeDeleteBlip(Blip blip)
        {
            try { if (blip && blip.Exists()) blip.Delete(); } catch { }
        }

        public static void SafeDismissEntity(Entity ent)
        {
            try { if (ent && ent.Exists()) ent.Dismiss(); } catch { }
        }

        public static Vehicle GetPlayerVehicle()
        {
            try
            {
                var p = Game.LocalPlayer.Character;
                if (p && p.IsInAnyVehicle(false))
                    return p.CurrentVehicle;
            }
            catch { }
            return null;
        }

        public static Vector3 GetNearbyStreet(Vector3 center, float radius = 300f)
        {
            try { return World.GetNextPositionOnStreet(center.Around(radius)); }
            catch { return center; }
        }

        // ---------------------------- TEXT / STRING HELPERS ----------------------------

        public static string ToTimeStamp(int seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            return $"{t.Minutes:D2}:{t.Seconds:D2}";
        }

        public static string BoolToText(bool value, bool formatted = false) =>
            formatted ? (value ? "~g~Yes~s~" : "~r~No~s~") : (value ? "Yes" : "No");

        public static string Capitalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
        }

        // ---------------------------- MATH HELPERS ----------------------------

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Lerp(float a, float b, float t)
        {
            // ✅ .NET 4.8‑safe clamp (Math.Clamp not available)
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            return a + (b - a) * t;
        }

        public static float MphToMetersPerSecond(float mph) => mph * 0.44704f;
        public static float MpsToMph(float mps) => mps / 0.44704f;

        // ---------------------------- LOGGING ----------------------------

        public static void Log(string module, string message) =>
            Game.LogTrivial($"[WSQ][{module}] {message}");

        public static void LogException(string module, Exception ex) =>
            Game.LogTrivial($"[WSQ][{module}] Exception: {ex.Message}");

        // ---------------------------- PLAYER UTILITIES ----------------------------

        public static bool PlayerIsOnFoot()
        {
            try
            {
                var player = Game.LocalPlayer.Character;
                return player && !player.IsInAnyVehicle(false);
            }
            catch { return false; }
        }

        public static bool WithinRange(Vector3 a, Vector3 b, float distance)
        {
            try { return a.DistanceTo(b) <= distance; }
            catch { return false; }
        }
    }
}