// Minimal Unity/inventory ports for exercising the real trade coordinator and recovery code.
// Capacity and Harmony/API compatibility have separate checks against their own contracts.
using System;
using System.Collections.Generic;
using System.Text.Json;
using PlayerTradingReforged.Trading;

public sealed partial class Inventory
{
    public int Capacity = 24;
    public readonly List<string> Items = new();
    public Action? m_onChanged;
    public float Weight;
    public float GetTotalWeight() => Weight;
    public int NrOfItems() => Items.Count;
    public void RemoveAll() { Items.Clear(); m_onChanged?.Invoke(); }
    public void Add(string item) { Items.Add(item); m_onChanged?.Invoke(); }
    public void MoveAll(Inventory source)
    {
        while (Items.Count < Capacity && source.Items.Count > 0)
        { Items.Add(source.Items[0]); source.Items.RemoveAt(0); }
        m_onChanged?.Invoke(); source.m_onChanged?.Invoke();
    }
}

public sealed class Player
{
    public long Id;
    public float MaxCarryWeight = 300;
    public float GetMaxCarryWeight() => MaxCarryWeight;
    public string GetPlayerName() => "Trader " + Id;
    public bool Dead;
    public bool Teleporting;
    public readonly Dictionary<string, string> m_customData = new();
    public readonly UnityEngine.Transform transform = new();
    public readonly Inventory Inventory = new();
    public Inventory GetInventory() => Inventory;
    public long GetPlayerID() => Id;
    public bool IsDead() => Dead;
    public bool IsTeleporting() => Teleporting;
    public static implicit operator bool(Player? player) => player != null;
}

public sealed class InventoryGui
{
    public static readonly InventoryGui instance = new();
    public void SetupDragItem(object? item, object? inventory, int amount) { }
}

namespace UnityEngine
{
    public static class Time { public static float unscaledTime; }
    public sealed class Transform { public Vector3 position; }
    public struct Vector3 { public float X; public static float Distance(Vector3 a, Vector3 b) => Math.Abs(a.X - b.X); }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, Options);
        public static T? FromJson<T>(string value)
        {
            try { return JsonSerializer.Deserialize<T>(value, Options); }
            catch (JsonException error) { throw new ArgumentException("Invalid JSON", error); }
        }
    }
}

namespace PlayerTradingReforged
{
    internal static class Plugin
    {
        public const string PluginId = "tests.trading";
        public static readonly Strings Localization = new();
        internal sealed class Strings
        {
            public string ReservedForTradeText = "{0} reserved for trade";
            public string NotEnoughInventorySlots = "full", TradeInvalid = "invalid", TradeSuccessful = "success",
                TradePending = "pending", LocalPlayerCancelledTrade = "cancelled", XHasCancelledTrade = "remote cancelled";
        }
        public static void Warn(string message) { }
    }
    internal sealed class TradeHandler
    {
        public readonly Queue<(string Kind, int Local, int Remote, string Items)> Messages = new();
        public int Closed;
        public Action? OnClose;
        public void Send(TradeSession session, string kind, string items = "") => Messages.Enqueue((kind, session.LocalRevision, session.RemoteRevision, items));
        public void Close(TradeInstance instance) { Closed++; OnClose?.Invoke(); }
        public float GetMaxDistance() => 5;
        public static void Show(string message) { }
    }
}

namespace PlayerTradingReforged.GUI
{
    internal sealed class TradeWindowManager
    {
        public static TradeWindowManager Instance { get; set; } = new();
        public Inventory Give = new(), Receive = new();
        public Inventory? PartnerInventory;
        public float? PartnerCapacity;
        public TradeWeightSnapshot? PartnerWeights;
        public void SetPartnerWeights(TradeWeightSnapshot? weights) => PartnerWeights = weights;
        public string? PartnerName;
        public void SetPartnerCapacity(float? capacity) => PartnerCapacity = capacity;
        public void SetPartnerName(string name) => PartnerName = name;
        public event Action? OnTradeAcceptPressed;
        public event Action? OnCancelTradePressed;
        public event Action? OnChangeTradePressed;
        public void Accept() => OnTradeAcceptPressed?.Invoke();
        public void Cancel() => OnCancelTradePressed?.Invoke();
        public void Change() => OnChangeTradePressed?.Invoke();
        public Inventory GetToTradeInventory() => Give;
        public Inventory GetToReceiveInventory() => Receive;
        public void StartNewInstance() { }
        public void ClearOfferedView() { }
        public void CancelInstance() => PartnerInventory = null;
        public void SetToTradeAccepted(bool value) { }
        public void SetToReceiveAccepted(bool value) { }
        public void RefreshToReceiveWindow() { }
        public void ShowPartnerInventory(Inventory? inventory) => PartnerInventory = inventory;
    }
}

namespace PlayerTradingReforged.Trading
{
    internal static class TradeInventory
    {
        public static Inventory Create() => new();
        public static string Save(Inventory inventory) => JsonSerializer.Serialize(inventory.Items);
        public static string SavePreview(Inventory inventory) => Save(inventory);
        public static bool TryLoadPreview(string value, out Inventory inventory) => TryLoad(value, out inventory);
        public static bool TryLoad(string value, out Inventory inventory)
        {
            inventory = new Inventory();
            try
            {
                var items = JsonSerializer.Deserialize<List<string>>(value);
                if (items == null) return false;
                inventory.Items.AddRange(items); return true;
            }
            catch (JsonException) { return false; }
        }
        public static bool CanFit(Inventory destination, Inventory incoming) => destination.Items.Count + incoming.Items.Count <= destination.Capacity;
        public static void Replace(Inventory target, Inventory source) { target.Items.Clear(); target.Items.AddRange(source.Items); }
    }
}
