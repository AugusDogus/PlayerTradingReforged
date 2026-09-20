using System.Globalization;
using PlayerTradingReforged.Trading;
using Xunit;

public sealed class TradeWeightSnapshotTests
{
    [Fact]
    public void MovingWeightIntoTheOfferKeepsCurrentWeightConstant()
    {
        var before = Assert.IsType<TradeWeightSnapshot>(TradeWeightSnapshot.Parse(TradeWeightSnapshot.Encode(97, 0, 450)));
        var after = Assert.IsType<TradeWeightSnapshot>(TradeWeightSnapshot.Parse(TradeWeightSnapshot.Encode(85, 12, 450)));
        Assert.Equal(before.Current, after.Current);
        Assert.Equal(85, after.InBag);
        Assert.Equal(450, after.Capacity);
    }

    [Theory]
    [InlineData("NaN;0;450")]
    [InlineData("97;Infinity;450")]
    [InlineData("97;85;-1")]
    [InlineData("84;85;450")]
    [InlineData("97;85")]
    [InlineData("97;85;450;0")]
    public void InvalidSnapshotsAreRejected(string encoded) => Assert.Null(TradeWeightSnapshot.Parse(encoded));

    [Fact]
    public void WireFormatDoesNotDependOnThePlayersLocale()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var snapshot = Assert.IsType<TradeWeightSnapshot>(TradeWeightSnapshot.Parse(TradeWeightSnapshot.Encode(85.5f, 12.25f, 450)));
            Assert.Equal(97.75f, snapshot.Current);
            Assert.Equal(85.5f, snapshot.InBag);
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }
}
