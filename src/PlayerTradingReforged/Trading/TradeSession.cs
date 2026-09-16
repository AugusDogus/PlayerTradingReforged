using System;

namespace PlayerTradingReforged.Trading;

// Pure protocol state. Both acceptances refer to the same pair of offer revisions.
internal sealed class TradeSession
{
    internal enum Phase { Editing, Preparing, Prepared, Committed, Cancelled }

    public TradeSession(string id, long peer, bool coordinator)
    {
        Id = id;
        Peer = peer;
        IsCoordinator = coordinator;
    }

    public string Id { get; }
    public long Peer { get; }
    public bool IsCoordinator { get; }
    public Phase State { get; private set; }
    public int LocalRevision { get; private set; }
    public int RemoteRevision { get; private set; }
    public bool LocalAccepted { get; private set; }
    public bool RemoteAccepted { get; private set; }
    public bool CanEdit => State == Phase.Editing;
    public bool BothAccepted => LocalAccepted && RemoteAccepted;
    public bool Matches(long sender, string id) => sender == Peer && id == Id;

    public bool ChangeOffer()
    {
        if (State != Phase.Editing) return false;
        LocalRevision++;
        ResetAcceptance();
        return true;
    }

    public bool ReceiveOffer(int revision)
    {
        if (State != Phase.Editing || revision <= RemoteRevision) return false;
        RemoteRevision = revision;
        ResetAcceptance();
        return true;
    }

    public bool Accept()
    {
        if (State != Phase.Editing) return false;
        LocalAccepted = true;
        return true;
    }

    public bool ReceiveAccept(int theirRevision, int ourRevision)
    {
        if (State != Phase.Editing || !MatchesRevisions(theirRevision, ourRevision)) return false;
        RemoteAccepted = true;
        return true;
    }

    public bool Change()
    {
        if (State != Phase.Editing) return false;
        // Bump even if items did not change, invalidating in-flight accept/prepare messages.
        return ChangeOffer();
    }

    public bool BeginPrepare()
    {
        if (!IsCoordinator || State != Phase.Editing || !BothAccepted) return false;
        State = Phase.Preparing;
        return true;
    }

    public bool Prepare(int theirRevision, int ourRevision)
    {
        if (IsCoordinator || !BothAccepted || !MatchesRevisions(theirRevision, ourRevision)) return false;
        if (State == Phase.Prepared) return true;
        if (State != Phase.Editing) return false;
        State = Phase.Prepared;
        return true;
    }

    public bool Commit(int theirRevision, int ourRevision)
    {
        if (!MatchesRevisions(theirRevision, ourRevision)) return false;
        if (State != (IsCoordinator ? Phase.Preparing : Phase.Prepared)) return false;
        State = Phase.Committed;
        return true;
    }

    public bool Cancel(bool coordinatorAborted = false)
    {
        if (State == Phase.Committed || State == Phase.Cancelled) return false;
        // A prepared participant cannot refund without the coordinator's decision.
        if (State == Phase.Prepared && !coordinatorAborted) return false;
        State = Phase.Cancelled;
        return true;
    }

    private bool MatchesRevisions(int theirs, int ours) => theirs == RemoteRevision && ours == LocalRevision;
    private void ResetAcceptance() { LocalAccepted = false; RemoteAccepted = false; }
}
