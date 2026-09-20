using System.Globalization;

namespace PlayerTradingReforged.Trading;

// Both amounts come from one owner-side sample, never from independently delivered offers.
internal sealed class TradeWeightSnapshot
{
    private TradeWeightSnapshot(float current, float inBag, float capacity)
    { Current = current; InBag = inBag; Capacity = capacity; }
    public float Current { get; }
    public float InBag { get; }
    public float Capacity { get; }
    public static string Encode(float inBag, float offered, float capacity) =>
        (inBag + offered).ToString("R", CultureInfo.InvariantCulture) + ";" +
        inBag.ToString("R", CultureInfo.InvariantCulture) + ";" + capacity.ToString("R", CultureInfo.InvariantCulture);
    public static TradeWeightSnapshot? Parse(string text)
    {
        if (text.Length > 128) return null;
        var parts = text.Split(';');
        if (parts.Length != 3 || !Read(parts[0], out float current) || !Read(parts[1], out float inBag) ||
            !Read(parts[2], out float capacity) || current < inBag) return null;
        return new TradeWeightSnapshot(current, inBag, capacity);
    }
    private static bool Read(string text, out float value) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
}
