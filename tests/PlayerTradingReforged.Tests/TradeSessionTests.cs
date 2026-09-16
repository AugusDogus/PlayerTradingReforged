using PlayerTradingReforged.Trading;
using Xunit;

public sealed class TradeSessionTests
{
    [Fact]
    public void OldOfferAcceptanceCannotAcceptChangedItems()
    {
        var session = new TradeSession("trade", 42, true);
        session.Accept();
        session.ChangeOffer();
        Assert.False(session.ReceiveAccept(0, 0));
        Assert.False(session.BothAccepted);
        Assert.True(session.CanEdit);
    }

    [Fact]
    public void AcceptedOfferCanStillBeEditedBeforeFinalConfirmation()
    {
        var session = new TradeSession("trade", 42, false);
        session.Accept(); session.ReceiveAccept(0, 0);
        Assert.True(session.CanEdit);
        Assert.True(session.ChangeOffer());
        Assert.False(session.LocalAccepted);
        Assert.False(session.RemoteAccepted);
        Assert.False(session.Prepare(0, 0));
    }

    [Fact]
    public void ChangingMindInvalidatesInFlightPrepare()
    {
        var session = new TradeSession("trade", 42, false);
        session.Accept();
        session.ReceiveAccept(0, 0);
        session.Change();
        Assert.False(session.Prepare(0, 0));
        Assert.Equal(TradeSession.Phase.Editing, session.State);
    }

    [Fact]
    public void DuplicateAndOutOfOrderOffersDoNotReplacePreview()
    {
        var session = new TradeSession("trade", 42, true);
        Assert.True(session.ReceiveOffer(2));
        session.Accept();
        Assert.False(session.ReceiveOffer(1));
        Assert.False(session.ReceiveOffer(2));
        Assert.True(session.LocalAccepted);
    }

    [Fact]
    public void CommitRequiresBothAcceptancesAndPrepareHandshake()
    {
        var coordinator = new TradeSession("trade", 42, true);
        var participant = new TradeSession("trade", 24, false);
        Assert.False(coordinator.BeginPrepare());
        Assert.False(participant.Commit(0, 0));
        coordinator.Accept(); participant.Accept();
        coordinator.ReceiveAccept(0, 0); participant.ReceiveAccept(0, 0);
        Assert.True(coordinator.BeginPrepare());
        Assert.True(participant.Prepare(0, 0));
        Assert.False(participant.Cancel());
        Assert.False(participant.Change());
        Assert.False(participant.ChangeOffer());
        Assert.True(coordinator.Commit(0, 0));
        Assert.True(participant.Commit(0, 0));
        Assert.False(participant.Commit(0, 0));
        Assert.False(coordinator.Cancel());
    }

    [Fact]
    public void RepeatedPrepareCanResendReadyWithoutUnlockingItems()
    {
        var session = new TradeSession("trade", 42, false);
        session.Accept(); session.ReceiveAccept(0, 0);
        Assert.True(session.Prepare(0, 0));
        Assert.True(session.Prepare(0, 0));
        Assert.False(session.CanEdit);
        Assert.False(session.Cancel());
    }

    [Fact]
    public void CoordinatorCanAbortPreparedParticipant()
    {
        var session = new TradeSession("trade", 42, false);
        session.Accept(); session.ReceiveAccept(0, 0); session.Prepare(0, 0);
        Assert.True(session.Cancel(coordinatorAborted: true));
        Assert.False(session.Commit(0, 0));
    }

    [Fact]
    public void MessagesMustMatchBothPeerAndSession()
    {
        var session = new TradeSession("new", 42, true);
        Assert.False(session.Matches(43, "new"));
        Assert.False(session.Matches(42, "old"));
        Assert.True(session.Matches(42, "new"));
    }
}
