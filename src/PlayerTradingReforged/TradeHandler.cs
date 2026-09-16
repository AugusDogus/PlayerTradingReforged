using System;
using System.Collections.Generic;
using System.IO;
using PlayerTradingReforged.GUI;
using PlayerTradingReforged.Patterns;
using PlayerTradingReforged.Trading;
using UnityEngine;

namespace PlayerTradingReforged;

internal sealed class TradeHandler : MonoSingleton<TradeHandler>
{
    private const string RequestRpc = Plugin.PluginId + ".v1.Request";
    private const string StartRpc = Plugin.PluginId + ".v1.Start";
    private const string MessageRpc = Plugin.PluginId + ".v1.Message";
    private const string RecoveryRpc = Plugin.PluginId + ".v1.Recovery";
    private const float MaxDistance = 5f;
    private sealed class Request
    {
        public Request(string session) { Session = session; Expires = Time.unscaledTime + 10f; }
        public string Session { get; }
        public float Expires { get; }
    }
    private readonly Dictionary<long, Request> _sent = new Dictionary<long, Request>();
    private readonly Dictionary<long, Request> _received = new Dictionary<long, Request>();
    private TradeInstance? _trade;
    private ZRoutedRpc? _network;
    private Player? _player;
    private float _nextRecovery;
    private bool _shutdown;

    protected override void Init()
    {
        _network = ZRoutedRpc.instance;
        _player = Player.m_localPlayer;
        gameObject.AddComponent<TradeWindowManager>();
        _network.Register<string>(RequestRpc, ReceiveRequest);
        _network.Register<string>(StartRpc, ReceiveStart);
        _network.Register<ZPackage>(MessageRpc, ReceiveMessage);
        _network.Register<string, string>(RecoveryRpc, ReceiveRecovery);
        Plugin.OnLocalPlayerChanged += OnNewLocalPlayer;
    }

    private void Update()
    {
        if (_shutdown || _player == null) return;
        _trade?.Tick();
        if (_trade == null && Time.unscaledTime >= _nextRecovery)
        {
            _nextRecovery = Time.unscaledTime + 3f;
            RecoverItems();
        }
        if (Chat.instance != null && Chat.instance.HasFocus() || Console.IsVisible() || Menu.IsVisible() || TextInput.IsVisible()) return;
        if (Input.GetKeyDown(Plugin.EditWindowLayoutKey.Value))
            TradeWindowManager.Instance.ToggleWindowPositionMode();
        if (_trade != null || IsTradeWindowsOpen() || InventoryGui.IsVisible() || _player.IsDead() || _player.IsTeleporting()) return;
        if ((ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse")) &&
            (!Plugin.UseModifierKey.Value || Input.GetKey(Plugin.ModifierKey.Value)))
        {
            _player.UpdateHover();
            if (_player.m_hoveringCreature is Player target && CanTradeWith(target)) RequestTrade(target);
        }
    }

    private bool CanTradeWith(Player target) => _player != null && target != _player && target && !target.IsDead() &&
        !_player.IsDead() && !_player.IsTeleporting() && !target.IsTeleporting() &&
        Vector3.Distance(_player.transform.position, target.transform.position) <= MaxDistance;
    private bool Available => _trade == null && _player != null && !TradeRecovery.HasPending(_player) && !IsTradeWindowsOpen();
    private static bool ValidSession(string id) => id.Length == 32 && Guid.TryParseExact(id, "N", out _);
    private static bool Active(Dictionary<long, Request> requests, long peer, out Request? request)
    {
        if (requests.TryGetValue(peer, out var found) && found.Expires >= Time.unscaledTime) { request = found; return true; }
        requests.Remove(peer); request = null; return false;
    }

    private void RequestTrade(Player target)
    {
        if (!Available) { Show(Plugin.Localization.CantStartNewTradeInstance); return; }
        long peer = target.GetOwner();
        if (Active(_received, peer, out var incoming) && incoming != null)
        {
            if (_player != null && Active(_sent, peer, out _) && _player.GetOwner() < peer) return;
            StartTrade(target, incoming.Session, false);
            _network?.InvokeRoutedRPC(peer, StartRpc, incoming.Session);
        }
        else if (Active(_sent, peer, out _)) Show(Plugin.Localization.TradeRecentlySent);
        else
        {
            var request = new Request(Guid.NewGuid().ToString("N"));
            _sent[peer] = request;
            _network?.InvokeRoutedRPC(peer, RequestRpc, request.Session);
            Show(Plugin.Localization.TradeRequestSent);
        }
    }

    private void ReceiveRequest(long sender, string session)
    {
        if (!ValidSession(session) || !Available) return;
        Player? target = ZNetUtils.GetPlayer(sender);
        if (target == null || !CanTradeWith(target) || Active(_received, sender, out _)) return;
        _received[sender] = new Request(session);
        if (MessageHud.instance) MessageHud.instance.ShowBiomeFoundMsg(target.GetPlayerName() + " " + Plugin.Localization.XWantsToTrade, false);
    }

    private void ReceiveStart(long sender, string session)
    {
        if (!Available || !Active(_sent, sender, out var request) || request == null || request.Session != session) return;
        Player? target = ZNetUtils.GetPlayer(sender);
        if (target != null && CanTradeWith(target)) StartTrade(target, session, true);
    }

    private void StartTrade(Player target, string session, bool coordinator)
    {
        if (_player == null) return;
        _sent.Clear(); _received.Clear();
        // Assign before opening the vanilla inventory, whose patches query the active trade.
        _trade = new TradeInstance(this, _player, target, new TradeSession(session, target.GetOwner(), coordinator));
        _trade.Open();
        Show(Plugin.Localization.StartedTradeWithX + " " + target.GetPlayerName());
    }

    private void ReceiveMessage(long sender, ZPackage package)
    {
        if (_trade == null || sender != _trade.Session.Peer || package.Size() > TradeInventory.MaxPackageBytes * 2) return;
        try
        {
            string session = package.ReadString();
            if (!_trade.Session.Matches(sender, session)) return;
            string kind = package.ReadString();
            int theirs = package.ReadInt();
            int ours = package.ReadInt();
            string items = package.ReadString();
            if (package.GetPos() != package.Size() || theirs < 0 || ours < 0) { _trade.Cancel(); return; }
            _trade.Receive(kind, theirs, ours, items);
        }
        catch (Exception error) when (error is IOException || error is ArgumentException || error is OverflowException)
        {
            Plugin.Warn($"Rejected malformed trade message from {sender}: {error.Message}");
            _trade?.Cancel();
        }
    }

    internal void Send(TradeSession session, string kind, string items = "")
    {
        var package = new ZPackage();
        package.Write(session.Id); package.Write(kind);
        package.Write(session.LocalRevision); package.Write(session.RemoteRevision); package.Write(items);
        _network?.InvokeRoutedRPC(session.Peer, MessageRpc, package);
    }

    internal void Close(TradeInstance trade)
    {
        if (_trade != trade) return;
        _trade = null;
        TradeWindowManager.Instance.CancelInstance();
    }

    private void RecoverItems()
    {
        if (_player == null || _player.IsDead() || _player.IsTeleporting()) return;
        var entry = TradeRecovery.Read(_player);
        if (entry == null) return;
        if (!ValidSession(entry.Session)) return;
        if (entry.State == TradeRecovery.RecoveryState.AwaitingDecision)
        {
            Player? peer = ZNetUtils.GetCharacter(entry.PeerCharacter);
            if (peer != null) _network?.InvokeRoutedRPC(peer.GetOwner(), RecoveryRpc, entry.Session, "query");
            return;
        }
        bool committed = entry.State == TradeRecovery.RecoveryState.CoordinatorOffer &&
            TradeRecovery.Decision(_player, entry.Session, entry.PeerCharacter) == "commit";
        if (entry.State == TradeRecovery.RecoveryState.CoordinatorOffer && !committed)
            TradeRecovery.RecordDecision(_player, entry.Session, entry.PeerCharacter, false);
        if (TradeRecovery.Resolve(_player, entry, committed)) Show(Plugin.Localization.ReturnedItems);
    }

    private void ReceiveRecovery(long sender, string session, string decision)
    {
        if (_player == null || !ValidSession(session)) return;
        Player? peer = ZNetUtils.GetPlayer(sender);
        if (peer == null || peer == _player) return;
        if (decision == "query")
        {
            // A live coordinator still preparing can safely abort. A saved decision is immutable.
            if (_trade != null && _trade.Session.Matches(sender, session) && _trade.Session.IsCoordinator) _trade.Cancel();
            string? saved = TradeRecovery.Decision(_player, session, peer.GetPlayerID());
            if (saved != null) _network?.InvokeRoutedRPC(sender, RecoveryRpc, session, saved);
            return;
        }
        if (_player.IsDead() || _player.IsTeleporting() || (decision != "commit" && decision != "abort")) return;
        var entry = TradeRecovery.Read(_player);
        if (entry == null || entry.State != TradeRecovery.RecoveryState.AwaitingDecision || entry.Session != session || entry.PeerCharacter != peer.GetPlayerID()) return;
        TradeRecovery.Resolve(_player, entry, decision == "commit");
        Show(decision == "commit" ? Plugin.Localization.TradeSuccessful : Plugin.Localization.ReturnedItems);
    }

    public bool IsTradeWindowsOpen() => _trade != null || (TradeWindowManager.Current != null && TradeWindowManager.Current.IsInWindowPositionMode());
    public bool InTradeInstance() => _trade != null;
    public bool CanEditOffer => _trade != null && _trade.Session.CanEdit && !TradeWindowManager.Instance.IsInWindowPositionMode();
    public Inventory? TryGetToTradeInventory() => _trade?.Give;
    public float GetMaxDistance() => MaxDistance;
    public string GetAction(Player target) => Active(_received, target.GetOwner(), out _) ? Plugin.Localization.AcceptRequest : Plugin.Localization.RequestTrade;
    public void TryCancelTradeInstance() => _trade?.Cancel();
    public void TryCancelWindowEditMode() => TradeWindowManager.Current?.DisableWindowPositionMode();
    public void OnNewLocalPlayer() { _trade?.Cancel(); _player = Player.m_localPlayer; _sent.Clear(); _received.Clear(); }
    internal static void Show(string message) { if (MessageHud.instance) MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message); }

    public void Shutdown()
    {
        if (_shutdown) return;
        _shutdown = true;
        _trade?.Cancel();
        if (TradeWindowManager.Current != null) TradeWindowManager.Current.DisableWindowPositionMode();
        Plugin.OnLocalPlayerChanged -= OnNewLocalPlayer;
        if (_network != null)
            foreach (string rpc in new[] { RequestRpc, StartRpc, MessageRpc, RecoveryRpc }) _network.m_functions.Remove(rpc.GetStableHashCode());
    }
    private void OnDestroy() => Shutdown();
}
