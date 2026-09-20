using System.Collections.Generic;
using PlayerTradingReforged.Trading;
using Xunit;

public sealed class ReservationLedgerTests
{
    private static Dictionary<object, int> Items(params (object Item, int Count)[] items)
    {
        var result = new Dictionary<object, int>();
        foreach (var pair in items) result.Add(pair.Item, pair.Count);
        return result;
    }

    [Fact]
    public void OfferingAndReturningPartOfAStackTracksOnlyTheOfferedAmount()
    {
        var ledger = new ReservationLedger<object>();
        var wood = new object();
        ledger.Update(Items(), Items((wood, 20)), 7);
        Assert.Equal(20, ledger.CountAt(7));
        ledger.Update(Items((wood, 20)), Items((wood, 12)), null);
        Assert.Equal(12, ledger.CountAt(7));
        ledger.Update(Items((wood, 12)), Items(), null);
        Assert.Empty(ledger.Slots);
    }

    [Fact]
    public void MergingTwoSourceStacksKeepsBothOriginalSlotsReserved()
    {
        var ledger = new ReservationLedger<object>();
        var wood = new object();
        ledger.Update(Items(), Items((wood, 20)), 3);
        ledger.Update(Items((wood, 20)), Items((wood, 50)), 8);
        Assert.Equal(20, ledger.CountAt(3));
        Assert.Equal(30, ledger.CountAt(8));
        Assert.Equal(new[] { 3, 8 }, ledger.Origins(wood));
    }

    [Fact]
    public void SplittingAndRearrangingAnOfferKeepsItsOriginalSlots()
    {
        var ledger = new ReservationLedger<object>();
        var wood = new object(); var split = new object(); var moved = new object();
        ledger.Update(Items(), Items((wood, 20)), 3);
        ledger.Update(Items((wood, 20)), Items((wood, 12), (split, 8)), null);
        Assert.Equal(20, ledger.CountAt(3));
        Assert.Equal(new[] { 3 }, ledger.Origins(split));
        ledger.Update(Items((wood, 12), (split, 8)), Items((wood, 12), (moved, 8)), null);
        Assert.Equal(20, ledger.CountAt(3));
        Assert.Empty(ledger.Origins(split));
        Assert.Equal(new[] { 3 }, ledger.Origins(moved));
    }

    [Fact]
    public void FailedMovesAndRepeatedRefreshesDoNotChangeReservations()
    {
        var ledger = new ReservationLedger<object>();
        var wood = new object();
        ledger.Update(Items(), Items((wood, 20)), 3);
        ledger.Update(Items((wood, 20)), Items((wood, 20)), 8);
        ledger.Update(Items((wood, 20)), Items((wood, 20)), null);
        Assert.Equal(20, ledger.CountAt(3));
        Assert.Equal(0, ledger.CountAt(8));
    }

    [Fact]
    public void RemovingAnOfferOutsideTheGridReleasesItsSlots()
    {
        var ledger = new ReservationLedger<object>();
        var wood = new object(); var stone = new object();
        ledger.Update(Items(), Items((wood, 20)), 3);
        ledger.Update(Items((wood, 20)), Items((wood, 20), (stone, 5)), 8);
        ledger.Update(Items((wood, 20), (stone, 5)), Items((stone, 5)), null);
        Assert.Equal(0, ledger.CountAt(3));
        Assert.Equal(5, ledger.CountAt(8));
        ledger.Clear();
        Assert.Empty(ledger.Slots);
    }
}
