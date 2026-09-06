using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NOVR;

internal static class HarmonyPatchApplier
{
    public static Harmony Apply(Assembly assembly)
    {
        var harmony = new Harmony("deltawing.novr");
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(type => type != null).ToArray()!;
            Debug.LogError("[NOVR] Some plugin types failed to load; applying the rest. " + ex);
        }

        foreach (var type in types)
        {
            if (!HasHarmonyAnnotations(type))
            {
                continue;
            }

            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NOVR] Skipping Harmony patch {type.FullName}: {ex.Message}");
            }
        }

        return harmony;
    }

    private static bool HasHarmonyAnnotations(Type type)
    {
        if (type.GetCustomAttributes(typeof(HarmonyAttribute), true).Length > 0)
        {
            return true;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        foreach (var member in type.GetMethods(flags))
        {
            if (member.GetCustomAttributes(typeof(HarmonyAttribute), true).Length > 0)
            {
                return true;
            }
        }

        return false;
    }
}
