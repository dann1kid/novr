using HarmonyLib;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.HarmonyPatches;

internal static class MessageUiLayoutPatch
{
    private const float MaxMessageWidth = 420f;
    private const int MaxLineCharacters = 72;
    private static readonly FieldInfo MessageTextField = AccessTools.Field(typeof(global::MessageUI), "messageText");
    private static readonly FieldInfo ContentSizeFitterField = AccessTools.Field(typeof(global::MessageUI), "contentSizeFitter");
    private static readonly FieldInfo MessageBackgroundField = AccessTools.Field(typeof(global::MessageUI), "messageBackground");

    [HarmonyPatch(typeof(global::MessageUI), "Awake")]
    private static class AwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix(global::MessageUI __instance)
        {
            ConstrainLayout(__instance);
        }
    }

    [HarmonyPatch(typeof(global::MessageUI), nameof(global::MessageUI.SetDynamicBoxSize))]
    private static class SetDynamicBoxSizePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            var ui = SceneSingleton<global::MessageUI>.i;
            if (ui != null)
            {
                ConstrainLayout(ui);
            }
        }
    }

    [HarmonyPatch(typeof(global::MessageUI), nameof(global::MessageUI.GameMessage))]
    private static class GameMessagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref string message)
        {
            message = WrapLongLines(message);
        }
    }

    [HarmonyPatch(typeof(global::MessageUI), nameof(global::MessageUI.KillFeed))]
    private static class KillFeedPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref string message)
        {
            message = WrapLongLines(message);
        }
    }

    [HarmonyPatch(typeof(global::MessageUI), nameof(global::MessageUI.DelayedGameMessage))]
    private static class DelayedGameMessagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref string message)
        {
            message = WrapLongLines(message);
        }
    }

    private static void ConstrainLayout(global::MessageUI ui)
    {
        var messageText = MessageTextField.GetValue(ui) as TextMeshProUGUI;
        var fitter = ContentSizeFitterField.GetValue(ui) as ContentSizeFitter;
        var background = MessageBackgroundField.GetValue(ui) as GameObject;

        if (messageText != null)
        {
            messageText.enableWordWrapping = true;
            messageText.overflowMode = TextOverflowModes.Overflow;
            messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MaxMessageWidth);
        }

        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (background != null)
        {
            var rect = background.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MaxMessageWidth + 20f);
            }
        }
    }

    private static string WrapLongLines(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        var lines = message.Replace("\r\n", "\n").Split('\n');
        var builder = new StringBuilder(message.Length + 16);
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            AppendWrappedLine(builder, lines[i]);
        }

        return builder.ToString();
    }

    private static void AppendWrappedLine(StringBuilder builder, string line)
    {
        var remaining = line;
        var firstChunk = true;
        while (remaining.Length > MaxLineCharacters)
        {
            var split = remaining.LastIndexOf(' ', MaxLineCharacters);
            if (split < MaxLineCharacters / 3)
            {
                split = MaxLineCharacters;
            }

            if (!firstChunk)
            {
                builder.Append('\n');
            }

            builder.Append(remaining, 0, split);
            remaining = remaining.Substring(split).TrimStart();
            firstChunk = false;
        }

        if (!firstChunk)
        {
            builder.Append('\n');
        }

        builder.Append(remaining);
    }
}
