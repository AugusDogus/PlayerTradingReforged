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
