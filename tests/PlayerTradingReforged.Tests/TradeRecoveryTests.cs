using System;
using PlayerTradingReforged;
using PlayerTradingReforged.GUI;
using PlayerTradingReforged.Trading;
using UnityEngine;
using Xunit;

public sealed class TradeRecoveryTests
{
    private sealed class Peer
    {
        public Player Player { get; }
        public TradeHandler Handler { get; } = new();
        public TradeWindowManager UI { get; } = new();
        public TradeInstance Trade { get; }
        public Peer(Player local, Player remote, string id, bool coordinator)
        {
            Player = local;
            Handler.OnClose = UI.CancelInstance;
            TradeWindowManager.Instance = UI;
            Trade = new TradeInstance(Handler, local, remote, new TradeSession(id, remote.Id, coordinator));
            Trade.Open();
        }
    }
    private static (Peer A, Peer B) Pair()
    {
        Time.unscaledTime = 0;
        string id = Guid.NewGuid().ToString("N");
        var a = new Player { Id = 1 }; var b = new Player { Id = 2 };
        return (new Peer(a, b, id, true), new Peer(b, a, id, false));
    }
    private static void SendOne(Peer from, Peer to)
    {
        var message = from.Handler.Messages.Dequeue();
        to.Trade.Receive(message.Kind, message.Local, message.Remote, message.Items);
    }
    private static void OfferBoth(Peer a, Peer b)
    {
        a.UI.Give.Add("wood"); b.UI.Give.Add("stone");
        SendOne(a, b); SendOne(b, a);
    }
    private static void Prepare(Peer a, Peer b)
    {
        OfferBoth(a, b);
        a.UI.Accept(); b.UI.Accept();
        SendOne(a, b); // A accepts.
        SendOne(b, a); // B accepts, A prepares.
        SendOne(a, b); // B prepares and sends ready.
    }

    [Fact]
    public void SharedReservationsDoNotChangeOffersAndLegacyMessagesCannotEraseThem()
    {
        var (a, b) = Pair();
        a.UI.Reservations.GridItems.Add(new ItemDrop.ItemData { m_stack = 4, m_gridPos = new Vector2i(3, 2) });
        a.Trade.Tick();
        var message = Assert.Single(a.Handler.Messages, m => m.Kind == "inventory-reservations");
        b.Trade.Receive(message.Kind, message.Local, message.Remote, message.Items);
        Assert.Equal(4, Assert.Single(Assert.IsType<Inventory>(b.UI.PartnerReservations).GetAllItems()).m_stack);
        b.Trade.Receive("inventory", 0, 0, "[]");
        Assert.Single(Assert.IsType<Inventory>(b.UI.PartnerReservations).GetAllItems());
        Assert.Empty(b.UI.Give.Items); Assert.Empty(b.UI.Receive.Items);
        Assert.Equal(0, b.Trade.Session.RemoteRevision);
        a.Handler.Messages.Clear(); a.UI.Reservations.GridItems.Clear();
        Time.unscaledTime = 0.5f; a.Trade.Tick();
        message = Assert.Single(a.Handler.Messages, m => m.Kind == "inventory-reservations");
        b.Trade.Receive(message.Kind, message.Local, message.Remote, message.Items);
        Assert.Empty(Assert.IsType<Inventory>(b.UI.PartnerReservations).GetAllItems());
    }

    [Fact]
    public void PartnerCurrentWeightDoesNotMixOfferAndInventoryMessageTiming()
    {
        var (a, b) = Pair();
        a.Player.Inventory.Weight = 97;
        a.Trade.Tick();
        var first = Assert.Single(a.Handler.Messages, m => m.Kind == "weights");
        b.Trade.Receive(first.Kind, first.Local, first.Remote, first.Items);
        Assert.Equal(97, Assert.IsType<TradeWeightSnapshot>(b.UI.PartnerWeights).Current);
        a.Handler.Messages.Clear();
        a.UI.Give.Add("wood");
        a.UI.Give.Weight = 12; a.Player.Inventory.Weight = 85;
        SendOne(a, b); // Offer arrives before the next inventory/weight sample.
        Assert.Equal(97, Assert.IsType<TradeWeightSnapshot>(b.UI.PartnerWeights).Current);
        Time.unscaledTime = 0.5f; a.Trade.Tick();
        var updated = Assert.Single(a.Handler.Messages, m => m.Kind == "weights");
        b.Trade.Receive(updated.Kind, updated.Local, updated.Remote, updated.Items);
        Assert.Equal(97, Assert.IsType<TradeWeightSnapshot>(b.UI.PartnerWeights).Current);
        Assert.Equal(85, Assert.IsType<TradeWeightSnapshot>(b.UI.PartnerWeights).InBag);
    }

    [Fact]
    public void TradeUsesTheOtherCharactersName()
    {
        var (a, b) = Pair();
        Assert.Equal(b.Player.GetPlayerName(), a.UI.PartnerName);
    }

    [Fact]
    public void CapacityChangesAreSharedWithoutInventoryChanges()
    {
        var (a, b) = Pair();
        a.Player.MaxCarryWeight = 450;
        a.Trade.Tick();
        var message = Assert.Single(a.Handler.Messages, m => m.Kind == "capacity");
        b.Trade.Receive(message.Kind, message.Local, message.Remote, message.Items);
        Assert.Equal(450f, b.UI.PartnerCapacity);
        a.Handler.Messages.Clear();
        Time.unscaledTime = 0.5f; a.Trade.Tick();
        Assert.DoesNotContain(a.Handler.Messages, m => m.Kind == "capacity");
        a.Player.MaxCarryWeight = 300;
        Time.unscaledTime = 1f; a.Trade.Tick();
        message = Assert.Single(a.Handler.Messages, m => m.Kind == "capacity");
        b.Trade.Receive(message.Kind, message.Local, message.Remote, message.Items);
        Assert.Equal(300f, b.UI.PartnerCapacity);
        Assert.Equal(0, b.Trade.Session.RemoteRevision);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-1")]
    [InlineData("invalid")]
    public void InvalidCapacityClearsTheDisplayWithoutCancellingTrade(string value)
    {
        var (a, _) = Pair();
        a.Trade.Receive("capacity", 0, 0, "450");
        a.Trade.Receive("capacity", 0, 0, value);
        Assert.Null(a.UI.PartnerCapacity);
        Assert.Equal(TradeSession.Phase.Editing, a.Trade.Session.State);
    }

    [Fact]
    public void TradeSharesInventoryOnFirstTickAndOnlyResendsChanges()
    {
        var (a, _) = Pair();
        a.Player.Inventory.Add("wood");
        a.Trade.Tick();
        var snapshot = Assert.Single(a.Handler.Messages, message => message.Kind == "inventory");
        Assert.Equal("[\"wood\"]", snapshot.Items);
        a.Handler.Messages.Clear();
        Time.unscaledTime = 0.5f; a.Trade.Tick();
        Assert.DoesNotContain(a.Handler.Messages, message => message.Kind == "inventory");
        a.Player.Inventory.Add("stone");
        Time.unscaledTime = 1f; a.Trade.Tick();
        snapshot = Assert.Single(a.Handler.Messages, message => message.Kind == "inventory");
        Assert.Equal("[\"wood\",\"stone\"]", snapshot.Items);
    }

    [Fact]
    public void PreviewDoesNotChangeOffersAcceptancesOrOwnedItems()
    {
        var (a, b) = Pair(); OfferBoth(a, b);
        a.UI.Accept();
        a.Trade.Receive("inventory", 0, 0, "[\"sword\",\"helmet\"]");
        Assert.Equal(new[] { "sword", "helmet" }, Assert.IsType<Inventory>(a.UI.PartnerInventory).Items);
        Assert.Equal(new[] { "wood" }, a.UI.Give.Items);
        Assert.Equal(new[] { "stone" }, a.UI.Receive.Items);
        Assert.Empty(a.Player.Inventory.Items);
        Assert.True(a.Trade.Session.LocalAccepted);
        Assert.Equal(1, a.Trade.Session.LocalRevision);
        Assert.Equal(1, a.Trade.Session.RemoteRevision);
        Assert.Equal("[\"stone\"]", Assert.IsType<TradeRecovery.Entry>(TradeRecovery.Read(a.Player)).Receive);
    }

    [Fact]
    public void UnreadablePreviewClearsStaleViewWithoutCancellingTrade()
    {
        var (a, _) = Pair();
        a.Trade.Receive("inventory", 0, 0, "[\"wood\"]");
        Assert.Equal(new[] { "wood" }, Assert.IsType<Inventory>(a.UI.PartnerInventory).Items);
        a.Trade.Receive("inventory", 0, 0, "invalid snapshot");
        Assert.Null(a.UI.PartnerInventory);
        Assert.Equal(TradeSession.Phase.Editing, a.Trade.Session.State);
        Assert.Equal(0, a.Handler.Closed);
    }

    [Fact]
    public void InventorySharingStopsWhenTradeCloses()
    {
        var (a, _) = Pair();
        a.Trade.Receive("inventory", 0, 0, "[\"stone\"]");
        Assert.NotNull(a.UI.PartnerInventory);
        a.Trade.Cancel(); a.Handler.Messages.Clear();
        a.Player.Inventory.Add("wood");
        Time.unscaledTime = 1f; a.Trade.Tick();
        a.Trade.Receive("inventory", 0, 0, "[\"stone\"]");
        Assert.Empty(a.Handler.Messages);
        Assert.Null(a.UI.PartnerInventory);
    }

    [Fact]
    public void SuccessfulTradeDeliversExactlyOnceAndClearsEscrow()
    {
        var (a, b) = Pair(); Prepare(a, b);
        var ready = b.Handler.Messages.Peek();
        SendOne(b, a); SendOne(a, b);
        a.Trade.Receive(ready.Kind, ready.Local, ready.Remote, ready.Items);
        b.Trade.Receive("commit", 1, 1, "");
        Assert.Equal(new[] { "stone" }, a.Player.Inventory.Items);
        Assert.Equal(new[] { "wood" }, b.Player.Inventory.Items);
        Assert.False(TradeRecovery.HasPending(a.Player)); Assert.False(TradeRecovery.HasPending(b.Player));
        Assert.Equal(1, a.Handler.Closed); Assert.Equal(1, b.Handler.Closed);
    }

    [Fact]
    public void FullInventoryCancellationRetainsOverflowUntilSpaceIsAvailable()
    {
        var (a, _) = Pair(); a.UI.Give.Add("wood"); a.UI.Give.Add("stone");
        a.Player.Inventory.Capacity = 1;
        a.Trade.Cancel(); a.Trade.Cancel();
        Assert.Equal(new[] { "wood" }, a.Player.Inventory.Items);
        var entry = Assert.IsType<TradeRecovery.Entry>(TradeRecovery.Read(a.Player));
        Assert.Equal(TradeRecovery.RecoveryState.Refund, entry.State);
        a.Player.Inventory.Capacity = 2;
        Assert.True(TradeRecovery.Resolve(a.Player, entry, false));
        Assert.Equal(new[] { "wood", "stone" }, a.Player.Inventory.Items);
        Assert.False(TradeRecovery.HasPending(a.Player));
    }

    // Upstream #3 and #4: distance cancellation must preserve items even with full bags.
    [Fact]
    public void WalkingAwayWithFullBagsRetainsEveryOfferedItem()
    {
        var (a, b) = Pair(); a.UI.Give.Add("wood"); a.UI.Give.Add("stone");
        a.Player.Inventory.Capacity = 0;
        b.Player.transform.position.X = 6;
        a.Trade.Tick();
        Assert.Empty(a.Player.Inventory.Items);
        var entry = Assert.IsType<TradeRecovery.Entry>(TradeRecovery.Read(a.Player));
        a.Player.Inventory.Capacity = 2;
        Assert.True(TradeRecovery.Resolve(a.Player, entry, false));
        Assert.Equal(new[] { "wood", "stone" }, a.Player.Inventory.Items);
        Assert.False(TradeRecovery.HasPending(a.Player));
    }

    [Fact]
    public void LostCommitIsRecoveredFromCoordinatorsDecisionWithoutRefundingOffer()
    {
        var (a, b) = Pair(); Prepare(a, b);
        SendOne(b, a); // A commits, but drop the commit packet to B.
        b.Trade.Cancel();
        Assert.Empty(b.Player.Inventory.Items);
        var entry = Assert.IsType<TradeRecovery.Entry>(TradeRecovery.Read(b.Player));
        Assert.Equal(TradeRecovery.RecoveryState.AwaitingDecision, entry.State);
        Assert.Equal("commit", TradeRecovery.Decision(a.Player, entry.Session, b.Player.Id));
        Assert.Null(TradeRecovery.Decision(a.Player, entry.Session, 999));
        Assert.True(TradeRecovery.Resolve(b.Player, entry, true));
        Assert.Equal(new[] { "wood" }, b.Player.Inventory.Items);
        Assert.False(TradeRecovery.HasPending(b.Player));
    }

    [Fact]
    public void CoordinatorAbortReturnsPreparedParticipantsOffer()
    {
        var (a, b) = Pair(); Prepare(a, b);
        a.Trade.Cancel(); SendOne(a, b);
        Assert.Equal(new[] { "wood" }, a.Player.Inventory.Items);
        Assert.Equal(new[] { "stone" }, b.Player.Inventory.Items);
        Assert.False(TradeRecovery.HasPending(b.Player));
    }

    [Fact]
    public void CapacityChangeDuringHandshakeCancelsBeforeCommit()
    {
        var (a, b) = Pair(); Prepare(a, b);
        a.Player.Inventory.Capacity = 0;
        SendOne(b, a); SendOne(a, b);
        Assert.NotEqual("commit", TradeRecovery.Decision(a.Player, a.Trade.Session.Id, b.Player.Id));
        Assert.Equal(new[] { "stone" }, b.Player.Inventory.Items);
        Assert.True(TradeRecovery.HasPending(a.Player));
    }

    [Fact]
    public void FailedCommitRecoveryPreservesIncomingOwnershipForLaterRetries()
    {
        var player = new Player { Id = 1 };
        var entry = new TradeRecovery.Entry { Session = Guid.NewGuid().ToString("N"), PeerCharacter = 2,
            State = TradeRecovery.RecoveryState.AwaitingDecision, Give = "[\"wood\"]", Receive = "unavailable item" };
        Assert.False(TradeRecovery.Resolve(player, entry, true));
        var saved = Assert.IsType<TradeRecovery.Entry>(TradeRecovery.Read(player));
        Assert.Equal(TradeRecovery.RecoveryState.Refund, saved.State);
        Assert.Equal("unavailable item", saved.Give);
        Assert.Empty(player.Inventory.Items);
    }

    [Fact]
    public void DeathAndDistanceCancelBeforeConfirmation()
    {
        var (a, _) = Pair(); a.UI.Give.Add("wood");
        a.Player.Dead = true; a.Trade.Tick();
        Assert.Equal(new[] { "wood" }, a.Player.Inventory.Items);
        Assert.Equal(1, a.Handler.Closed);
        var (c, d) = Pair(); c.UI.Give.Add("stone");
        d.Player.transform.position.X = 6; c.Trade.Tick();
        Assert.Equal(new[] { "stone" }, c.Player.Inventory.Items);
    }
}
