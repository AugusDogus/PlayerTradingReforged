using System.Globalization;
using PlayerTradingReforged.GUI;
using PlayerTradingReforged.Trading;
using UnityEngine;

namespace PlayerTradingReforged;

internal sealed class TradeInstance
{
    private readonly TradeHandler _handler;
    private readonly Player _local;
    private readonly Player _peer;
    private readonly long _peerCharacter;
    private readonly TradeWindowManager _windows;
    private bool _closed;
    private float _prepareStarted;
    private float _lastHeartbeat;
    private float _nextHeartbeat;
    private float _nextInventoryPreview;
    private string? _lastInventoryPreview;
    private float? _lastCarryCapacity;
    public TradeSession Session { get; }
    public Inventory Give { get; }
    private Inventory ReceiveItems { get; }

    public TradeInstance(TradeHandler handler, Player local, Player peer, TradeSession session)
    {
        _handler = handler; _local = local; _peer = peer; Session = session;
        _peerCharacter = peer.GetPlayerID();
        _windows = TradeWindowManager.Instance;
        Give = _windows.GetToTradeInventory(); ReceiveItems = _windows.GetToReceiveInventory();
        _lastHeartbeat = Time.unscaledTime;
    }

    public void Open()
    {
        _windows.StartNewInstance();
        _windows.SetPartnerName(_peer.GetPlayerName());
        _windows.OnTradeAcceptPressed += Accept;
        _windows.OnChangeTradePressed += Change;
        _windows.OnCancelTradePressed += Cancel;
        Give.m_onChanged += OfferChanged;
        Persist();
    }

    private void Persist() => TradeRecovery.Store(_local, Session, _peerCharacter, Give, ReceiveItems);
    private void RefreshAcceptance()
    {
        _windows.SetToTradeAccepted(Session.LocalAccepted);
        _windows.SetToReceiveAccepted(Session.RemoteAccepted);
    }

    public void OfferChanged()
    {
        if (_closed || !Session.ChangeOffer()) return;
        Persist(); RefreshAcceptance();
        _handler.Send(Session, "offer", TradeInventory.Save(Give));
    }

    private void Accept()
    {
        if (_closed || !TradeInventory.CanFit(_local.GetInventory(), ReceiveItems))
        { TradeHandler.Show(Plugin.Localization.NotEnoughInventorySlots); return; }
        if (!Session.Accept()) return;
        InventoryGui.instance.SetupDragItem(null, null, 1);
        RefreshAcceptance();
        _handler.Send(Session, "accept");
        TryPrepare();
    }

    private void Change()
    {
        if (!Session.Change()) return;
        Persist(); RefreshAcceptance();
        _handler.Send(Session, "offer", TradeInventory.Save(Give));
    }

    private void TryPrepare()
    {
        if (!Session.BeginPrepare()) return;
        _prepareStarted = Time.unscaledTime;
        _handler.Send(Session, "prepare");
    }

    public void Receive(string kind, int theirs, int ours, string items)
    {
        if (_closed) return;
        _lastHeartbeat = Time.unscaledTime;
        switch (kind)
        {
            case "heartbeat": break;
            case "capacity":
                _windows.SetPartnerCapacity(float.TryParse(items, NumberStyles.Float, CultureInfo.InvariantCulture, out float capacity)
                    && !float.IsNaN(capacity) && !float.IsInfinity(capacity) && capacity >= 0 ? capacity : null);
                break;
            case "inventory":
                _windows.ShowPartnerInventory(TradeInventory.TryLoadPreview(items, out var preview) ? preview : null);
                break;
            case "offer":
                if (Session.State == TradeSession.Phase.Preparing) { Cancel(); return; }
                if (theirs <= Session.RemoteRevision || Session.State != TradeSession.Phase.Editing) return;
                if (!TradeInventory.TryLoad(items, out var inventory))
                { TradeHandler.Show(Plugin.Localization.TradeInvalid); Cancel(); return; }
                if (!Session.ReceiveOffer(theirs)) return;
                TradeInventory.Replace(ReceiveItems, inventory);
                Persist(); RefreshAcceptance(); _windows.RefreshToReceiveWindow();
                break;
            case "accept":
                if (Session.ReceiveAccept(theirs, ours)) { RefreshAcceptance(); TryPrepare(); }
                break;
            case "prepare":
                if (!TradeInventory.CanFit(_local.GetInventory(), ReceiveItems) || !Session.Prepare(theirs, ours))
                { Cancel(); return; }
                Persist();
                _prepareStarted = Time.unscaledTime;
                _handler.Send(Session, "ready");
                break;
            case "ready":
                if (!Session.IsCoordinator || !TradeInventory.CanFit(_local.GetInventory(), ReceiveItems)) { Cancel(); return; }
                if (Session.Commit(theirs, ours))
                {
                    TradeRecovery.RecordDecision(_local, Session.Id, _peerCharacter, true);
                    _handler.Send(Session, "commit");
                    Finish();
                }
                break;
            case "commit":
                if (!Session.IsCoordinator && Session.Commit(theirs, ours)) Finish();
                break;
            case "cancel":
                if (Session.IsCoordinator) Cancel();
                break;
            case "abort":
                if (!Session.IsCoordinator && Session.Cancel(coordinatorAborted: true)) Refund(false);
                break;
        }
    }

    private void Finish()
    {
        _closed = true;
        _windows.ClearOfferedView();
        Give.RemoveAll();
        // Preserve any unexpected overflow from another mod as recoverable local items.
        var entry = new TradeRecovery.Entry { Session = Session.Id, PeerCharacter = _peerCharacter,
            Give = TradeInventory.Save(ReceiveItems), Receive = TradeInventory.Save(ReceiveItems) };
        TradeRecovery.Resolve(_local, entry, true);
        TradeHandler.Show(Plugin.Localization.TradeSuccessful);
        Close();
    }

    public void Tick()
    {
        if (_closed) return;
        if (!_local || !_peer || _local.IsDead() || _peer.IsDead() || _local.IsTeleporting() || _peer.IsTeleporting() ||
            Vector3.Distance(_local.transform.position, _peer.transform.position) > _handler.GetMaxDistance() ||
            Time.unscaledTime - _lastHeartbeat > 15f)
        { Cancel(); return; }
        if (Time.unscaledTime >= _nextHeartbeat)
        { _nextHeartbeat = Time.unscaledTime + 2f; _handler.Send(Session, "heartbeat"); }
        if (Time.unscaledTime >= _nextInventoryPreview)
        {
            _nextInventoryPreview = Time.unscaledTime + 0.5f;
            // Poll while trading to include durability/equipment changes that do not raise Inventory.Changed.
            float capacity = _local.GetMaxCarryWeight();
            if (capacity != _lastCarryCapacity)
            {
                _handler.Send(Session, "capacity", capacity.ToString("R", CultureInfo.InvariantCulture));
                _lastCarryCapacity = capacity;
            }
            string preview = TradeInventory.SavePreview(_local.GetInventory());
            if (preview != _lastInventoryPreview)
            {
                _handler.Send(Session, "inventory", preview);
                _lastInventoryPreview = preview;
            }
        }
        if ((Session.State == TradeSession.Phase.Preparing || Session.State == TradeSession.Phase.Prepared) && Time.unscaledTime - _prepareStarted > 10f)
            Cancel();
    }

    public void Cancel()
    {
        if (_closed) return;
        if (!Session.Cancel())
        {
            // Commit may already have happened remotely. Retain escrow and query the decision after reconnect.
            Persist();
            _closed = true;
            TradeHandler.Show(Plugin.Localization.TradePending);
            Give.RemoveAll();
            Close();
            return;
        }
        _handler.Send(Session, Session.IsCoordinator ? "abort" : "cancel");
        Refund(true);
    }

    private void Refund(bool localCancellation)
    {
        _closed = true;
        _windows.ClearOfferedView();
        if (Session.IsCoordinator) TradeRecovery.RecordDecision(_local, Session.Id, _peerCharacter, false);
        var entry = new TradeRecovery.Entry { Session = Session.Id, PeerCharacter = _peerCharacter, Give = TradeInventory.Save(Give) };
        TradeRecovery.Resolve(_local, entry, false);
        Give.RemoveAll();
        TradeHandler.Show(localCancellation ? Plugin.Localization.LocalPlayerCancelledTrade : Plugin.Localization.XHasCancelledTrade);
        Close();
    }

    private void Close()
    {
        Give.m_onChanged -= OfferChanged;
        _windows.OnTradeAcceptPressed -= Accept;
        _windows.OnChangeTradePressed -= Change;
        _windows.OnCancelTradePressed -= Cancel;
        _handler.Close(this);
    }
}
