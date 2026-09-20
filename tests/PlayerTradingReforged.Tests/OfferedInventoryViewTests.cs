using PlayerTradingReforged.GUI;
using Xunit;

public sealed class OfferedInventoryViewTests
{
    private static ItemDrop.ItemData Item(int slot, int count) => new() { m_gridPos = new Vector2i(slot % 8, slot / 8), m_stack = count };

    [Theory]
    [InlineData(4, "4/30")]
    [InlineData(2, "2/30")]
    public void ReservedStacksKeepTheNormalCountAndLimitFormat(int offered, string expected)
    {
        var player = new Inventory(); var offer = new Inventory(); var item = Item(0, 4);
        item.m_shared.m_maxStackSize = 30;
        player.GridItems.Add(item);
        var view = new OfferedInventoryView(player, offer);
        var transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(offer, player, item));
        var reserved = item.Clone(); reserved.m_stack = offered; offer.GridItems.Add(reserved);
        item.m_stack -= offered;
        if (item.m_stack == 0) player.GridItems.Remove(item);
        view.End(transfer);
        var element = new InventoryGrid.Element();
        view.Draw(new InventoryGrid { VisibleElement = element });
        Assert.Equal(expected, element.m_amount.text);
    }

    [Fact]
    public void OfferedSlotStaysReservedWithoutPuttingADuplicateInTheOwnedInventory()
    {
        var player = new Inventory(); var offer = new Inventory(); var item = Item(0, 20);
        player.GridItems.Add(item);
        var view = new OfferedInventoryView(player, offer);
        var transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(offer, player, item));
        player.GridItems.Remove(item); item.m_gridPos = new Vector2i(3, 0); offer.GridItems.Add(item);
        view.End(transfer);
        Assert.True(view.Reserved(new Vector2i(0, 0)));
        Assert.False(view.Reserved(new Vector2i(3, 0)));
        Assert.False(view.CanAdd(new Vector2i(0, 0)));
        Assert.Equal(1, view.EmptyReservedSlots);
        Assert.Equal(1, view.FindEmptySlot(true).x);
        Assert.Empty(player.GridItems);
        Assert.Same(item, Assert.Single(offer.GridItems));
    }

    [Fact]
    public void ReturningOfferCanUseItsReservedSlotThenReleasesTheReservation()
    {
        var player = new Inventory(); var offer = new Inventory(); var item = Item(0, 20);
        player.GridItems.Add(item);
        var view = new OfferedInventoryView(player, offer);
        var transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(offer, player, item));
        player.GridItems.Clear(); offer.GridItems.Add(item); view.End(transfer);
        transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(player, offer, item));
        Assert.True(view.CanAdd(new Vector2i(0, 0)));
        Assert.Equal(0, view.FindEmptySlot(true).x);
        offer.GridItems.Clear(); player.GridItems.Add(item); view.End(transfer);
        Assert.False(view.Reserved(new Vector2i(0, 0)));
        Assert.Equal(0, view.EmptyReservedSlots);
    }

    [Fact]
    public void FailedReturnDoesNotUnlockTheReservedSlot()
    {
        var player = new Inventory(); var offer = new Inventory(); var item = Item(0, 20);
        player.GridItems.Add(item);
        var view = new OfferedInventoryView(player, offer);
        var transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(offer, player, item));
        player.GridItems.Clear(); offer.GridItems.Add(item); view.End(transfer);
        transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(player, offer, item));
        view.End(transfer);
        Assert.True(view.Reserved(new Vector2i(0, 0)));
        Assert.False(view.CanAdd(new Vector2i(0, 0)));
    }

    [Fact]
    public void PartialOfferDoesNotCountTheOccupiedSourceSlotAsAnExtraUsedSlot()
    {
        var player = new Inventory(); var offer = new Inventory(); var item = Item(0, 20);
        player.GridItems.Add(item);
        var view = new OfferedInventoryView(player, offer);
        var transfer = Assert.IsType<OfferedInventoryView.Transfer>(view.Begin(offer, player, item));
        item.m_stack = 12; offer.GridItems.Add(Item(3, 8)); view.End(transfer);
        Assert.True(view.Reserved(new Vector2i(0, 0)));
        Assert.Equal(0, view.EmptyReservedSlots);
        Assert.Equal(12, Assert.Single(player.GridItems).m_stack);
        view.Clear();
        Assert.False(view.Reserved(new Vector2i(0, 0)));
        Assert.Equal(8, Assert.Single(offer.GridItems).m_stack);
    }
}
