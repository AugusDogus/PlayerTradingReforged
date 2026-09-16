using HarmonyLib;
using TMPro;

namespace PlayerTradingReforged.Patches;

internal static class MessageHudPatches
{
    [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.UpdateBiomeFound))]
    private static class MessageWrapping
    {
        // Let the game manage its own message queue and animation lifecycle.
        private static void Postfix(MessageHud __instance)
        {
            if (__instance.m_biomeMsgInstance == null) return;
            var title = __instance.m_biomeMsgInstance.GetComponentInChildren<TMP_Text>();
            if (title != null) title.overflowMode = TextOverflowModes.Overflow;
        }
    }
}
