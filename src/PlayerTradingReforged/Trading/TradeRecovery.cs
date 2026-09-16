using System;
using UnityEngine;

namespace PlayerTradingReforged.Trading;

// Stored with the character inventory so autosaves preserve escrow and decisions together.
// A prepared trade is never refunded merely because its peer disappears.
internal static class TradeRecovery
{
    private const string ActiveKey = Plugin.PluginId + ".escrow.v1";
    private const string ReceiptPrefix = Plugin.PluginId + ".receipt.v1.";

    internal enum RecoveryState { Refund, AwaitingDecision, CoordinatorOffer }

    [Serializable]
    internal sealed class Entry
    {
        public string Session = "";
        public long PeerCharacter;
        public RecoveryState State;
        public string Give = "";
        public string Receive = "";
    }

    public static void Store(Player player, TradeSession session, long peerCharacter, Inventory give, Inventory receive)
    {
        player.m_customData[ActiveKey] = JsonUtility.ToJson(new Entry {
            Session = session.Id, PeerCharacter = peerCharacter,
            State = session.State == TradeSession.Phase.Prepared ? RecoveryState.AwaitingDecision :
                session.IsCoordinator ? RecoveryState.CoordinatorOffer : RecoveryState.Refund,
            Give = TradeInventory.Save(give), Receive = TradeInventory.Save(receive)
        });
    }

    public static Entry? Read(Player player)
    {
        if (!player.m_customData.TryGetValue(ActiveKey, out var json)) return null;
        try
        {
            var entry = JsonUtility.FromJson<Entry>(json);
            if (entry != null && Guid.TryParseExact(entry.Session, "N", out _) && entry.PeerCharacter != 0 && Enum.IsDefined(typeof(RecoveryState), entry.State)) return entry;
        }
        catch (ArgumentException error) { Plugin.Warn($"Cannot read saved trade: {error.Message}"); }
        // Keep corrupt data available for recovery instead of silently overwriting it.
        Plugin.Warn("Saved trade recovery data is invalid. New trades remain blocked; preserve the character save and inspect its PlayerTradingReforged custom data before recovery.");
        return null;
    }

    public static bool HasPending(Player player) => player.m_customData.ContainsKey(ActiveKey);
    public static void Clear(Player player) => player.m_customData.Remove(ActiveKey);

    public static void RecordDecision(Player player, string session, long peerCharacter, bool committed)
    {
        player.m_customData[ReceiptPrefix + session] = peerCharacter.ToString(System.Globalization.CultureInfo.InvariantCulture) + (committed ? ":commit" : ":abort");
    }

    public static string? Decision(Player player, string session, long peerCharacter)
    {
        if (!player.m_customData.TryGetValue(ReceiptPrefix + session, out var receipt)) return null;
        string prefix = peerCharacter.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":";
        return receipt.StartsWith(prefix, StringComparison.Ordinal) ? receipt.Substring(prefix.Length) : null;
    }

    public static bool Resolve(Player player, Entry entry, bool committed)
    {
        // The decision is final even if decoding fails. Persist the correct owned payload first,
        // so a later retry cannot refund an offer that the other player already received.
        entry.Give = committed ? entry.Receive : entry.Give;
        entry.Receive = "";
        entry.State = RecoveryState.Refund;
        player.m_customData[ActiveKey] = JsonUtility.ToJson(entry);
        if (!TradeInventory.TryLoad(entry.Give, out Inventory items))
        {
            Plugin.Warn("Saved trade items could not be read. Escrow is preserved in character custom data; restore the matching item mods before retrying.");
            return false;
        }
        player.GetInventory().MoveAll(items);
        if (items.NrOfItems() != 0)
        {
            // Remaining items are now unconditionally owned locally. Retry when slots become available.
            entry.State = RecoveryState.Refund;
            entry.Give = TradeInventory.Save(items);
            entry.Receive = TradeInventory.Save(TradeInventory.Create());
            player.m_customData[ActiveKey] = JsonUtility.ToJson(entry);
            return false;
        }
        Clear(player);
        return true;
    }
}
