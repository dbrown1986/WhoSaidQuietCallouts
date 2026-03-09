using System;
using System.Reflection;
using Rage;

namespace WhoSaidQuietCallouts
{
    internal static class PluginBridge
    {
        /// <summary>
        /// PluginBridge.cs
        /// Version: 0.9.5 Stable (Reflective Integration Verified)
        /// Updated: March 9 2026 by Who Said Quiet Team
        /// Description:
        ///  Provides runtime reflection utilities for optional plugin support.
        ///  No dependencies on navigation or registration modules.
        /// </summary>
        public static object TryInvoke(string assemblyName, string typeName, string methodName, params object[] args)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                    {
                        Type t = asm.GetType(typeName, throwOnError: false);
                        MethodInfo m = t?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                        if (m != null)
                        {
                            Game.LogTrivial($"[WSQ][Bridge] Invoking {assemblyName}.{typeName}.{methodName}()");
                            return m.Invoke(m.IsStatic ? null : Activator.CreateInstance(t), args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"[WSQ][Bridge] Reflection failed for {assemblyName}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Checks if the plugin assembly is loaded.
        /// </summary>
        public static bool IsPluginLoaded(string assemblyName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                if (asm.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}