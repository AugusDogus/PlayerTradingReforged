using HarmonyLib;

namespace PlayerTradingReforged.Patches;

internal static class PlayerPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetHoverText))]
    private static class Hover
    {
        private static void Postfix(Player __instance, ref string __result)
        {
            var handler = TradeHandler.Current;
            if (handler == null || handler.IsTradeWindowsOpen() || __instance == Player.m_localPlayer) return;
            string modifier = Plugin.UseModifierKey.Value ? Plugin.ModifierKey.Value + " + " : "";
            __result += Localization.instance.Localize("\n[<color=yellow><b>" + modifier + "$KEY_Use</b></color>] " + handler.GetAction(__instance));
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
    private static class LocalPlayer
    {
        private static void Postfix() => Plugin.NewLocalPlayer();
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    private static class Death
    {
        // Return offers before vanilla fills the gravestone inventory.
        private static void Prefix(Player __instance)
        { if (__instance == Player.m_localPlayer) TradeHandler.Current?.TryCancelTradeInstance(); }
    }
}
