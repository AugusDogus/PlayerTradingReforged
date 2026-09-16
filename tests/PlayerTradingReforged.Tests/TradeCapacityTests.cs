using PlayerTradingReforged.Trading;
using Xunit;

public sealed class TradeCapacityTests
{
    private static TradeCapacity.Stack Wood(int count, int quality = 1, int world = 0) => new("wood", quality, world, count, 50);

    [Fact]
    public void FullInventoryAcceptsItemsThatFitExistingStack()
    {
        Assert.True(TradeCapacity.CanFit(1, new[] { Wood(40) }, new[] { Wood(10) }));
        Assert.False(TradeCapacity.CanFit(1, new[] { Wood(40) }, new[] { Wood(11) }));
    }

    [Fact]
    public void IncomingStacksShareTheSameRemainingCapacity()
    {
        Assert.False(TradeCapacity.CanFit(1, new[] { Wood(40) }, new[] { Wood(6), Wood(6) }));
        Assert.True(TradeCapacity.CanFit(2, new[] { Wood(40) }, new[] { Wood(30), Wood(30) }));
        Assert.False(TradeCapacity.CanFit(2, new[] { Wood(40) }, new[] { Wood(31), Wood(30) }));
    }

    [Fact]
    public void DifferentQualitiesAndWorldLevelsDoNotStack()
    {
        Assert.False(TradeCapacity.CanFit(1, new[] { Wood(1) }, new[] { Wood(1, quality: 2) }));
        Assert.False(TradeCapacity.CanFit(1, new[] { Wood(1) }, new[] { Wood(1, world: 1) }));
    }

    [Fact]
    public void ExistingStacksAreNotRepackedToInventFreeSlots()
    {
        var stone = new TradeCapacity.Stack("stone", 1, 0, 1, 50);
        Assert.False(TradeCapacity.CanFit(2, new[] { Wood(1), Wood(1) }, new[] { stone }));
    }

    [Fact]
    public void NonstackableItemsEachRequireOneSlot()
    {
        var sword = new TradeCapacity.Stack("sword", 1, 0, 1, 1);
        Assert.False(TradeCapacity.CanFit(1, new[] { sword }, new[] { sword }));
        Assert.True(TradeCapacity.CanFit(2, new[] { sword }, new[] { sword }));
    }
}
