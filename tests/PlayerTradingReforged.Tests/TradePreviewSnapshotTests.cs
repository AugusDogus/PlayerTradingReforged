using PlayerTradingReforged.GUI;
using PlayerTradingReforged.Trading;
using Xunit;

public sealed class TradePreviewSnapshotTests
{
    private static ItemDrop.ItemData Item(int count) => new()
    { m_stack = count, m_gridPos = new Vector2i(3, 2), m_shared = new() { m_maxStackSize = 30 } };

    [Fact]
    public void WholeOfferKeepsItsOriginalSlotAcrossSharingAndUsesTheDisabledStackLabel()
    {
        var bag = new Inventory(); var offer = new Inventory(); var item = Item(4); bag.GridItems.Add(item);
        var view = new OfferedInventoryView(bag, offer);
        var transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(offer, bag, item));
        bag.GridItems.Clear(); item.m_gridPos = new Vector2i(0, 0); offer.GridItems.Add(item); view.End(transfer);
        var shared = Assert.IsType<TradePreviewSnapshot>(TradePreviewSnapshot.Parse(new TradePreviewSnapshot(bag, view.CreateReservations()).Encode()));
        var reserved = Assert.Single(shared.Reserved.GetAllItems());
        Assert.Equal(3, reserved.m_gridPos.x); Assert.Equal(2, reserved.m_gridPos.y);
        Assert.Equal(4, reserved.m_stack);
        Assert.Empty(shared.Available.GetAllItems());
        var element = new InventoryGrid.Element();
        ReservedInventoryRenderer.Draw(new InventoryGrid { VisibleElement = element }, shared.Available, shared.Reserved);
        Assert.True(element.m_icon.enabled);
        Assert.Equal(0.5f, element.m_icon.color.Value);
        Assert.Equal("4/30", element.m_amount.text);
        Assert.Empty(bag.GetAllItems());
        Assert.Same(item, Assert.Single(offer.GetAllItems()));
    }

    [Fact]
    public void PartialOffersKeepTheUsableAndReservedCountsSeparate()
    {
        var bag = new Inventory(); var reserved = new Inventory(); bag.GridItems.Add(Item(2)); reserved.GridItems.Add(Item(2));
        var shared = Assert.IsType<TradePreviewSnapshot>(TradePreviewSnapshot.Parse(new TradePreviewSnapshot(bag, reserved).Encode()));
        Assert.Equal(2, Assert.Single(shared.Available.GetAllItems()).m_stack);
        Assert.Equal(2, Assert.Single(shared.Reserved.GetAllItems()).m_stack);
        var element = new InventoryGrid.Element();
        ReservedInventoryRenderer.Draw(new InventoryGrid { VisibleElement = element }, shared.Available, shared.Reserved);
        Assert.Equal("2/30", element.m_amount.text);
        Assert.False(element.m_icon.enabled); // The normal grid owns the remaining item's appearance.
    }

    [Fact]
    public void ReturningAnOfferClearsTheSharedReservation()
    {
        var bag = new Inventory(); var offer = new Inventory(); var item = Item(4); bag.GridItems.Add(item);
        var view = new OfferedInventoryView(bag, offer);
        var transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(offer, bag, item));
        bag.GridItems.Clear(); offer.GridItems.Add(item); view.End(transfer);
        transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(bag, offer, item));
        offer.GridItems.Clear(); bag.GridItems.Add(item); view.End(transfer);
        var shared = Assert.IsType<TradePreviewSnapshot>(TradePreviewSnapshot.Parse(new TradePreviewSnapshot(bag, view.CreateReservations()).Encode()));
        Assert.Empty(shared.Reserved.GetAllItems());
        Assert.Equal(4, Assert.Single(shared.Available.GetAllItems()).m_stack);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("[]|bad")]
    [InlineData("[]|[]|[]")]
    public void BrokenEnvelopesAreRejected(string text) => Assert.Null(TradePreviewSnapshot.Parse(text));

    [Fact]
    public void OverlappingCountsCannotExceedTheStackLimit()
    {
        var bag = new Inventory(); var reserved = new Inventory(); bag.GridItems.Add(Item(29)); reserved.GridItems.Add(Item(2));
        Assert.Null(TradePreviewSnapshot.Parse(new TradePreviewSnapshot(bag, reserved).Encode()));
    }
}
