using System.Collections.Generic;

namespace PlayerTradingReforged.Trading;

// Mirrors Valheim's name/quality/world-level stacking rules without calling AddItem,
// which can trigger inventory callbacks and achievement changes even on a scratch inventory.
internal static class TradeCapacity
{
    internal readonly struct Stack
    {
        public Stack(string name, int quality, int worldLevel, int count, int maximum)
        { Name = name; Quality = quality; WorldLevel = worldLevel; Count = count; Maximum = maximum; }
        public string Name { get; }
        public int Quality { get; }
        public int WorldLevel { get; }
        public int Count { get; }
        public int Maximum { get; }
        public Stack WithCount(int count) => new Stack(Name, Quality, WorldLevel, count, Maximum);
    }

    public static bool CanFit(int slots, IReadOnlyList<Stack> existing, IReadOnlyList<Stack> incoming)
    {
        var stacks = new List<Stack>(existing);
        foreach (var item in incoming)
        {
            if (item.Count <= 0 || item.Maximum <= 0 || item.Count > item.Maximum) return false;
            int remaining = item.Count;
            if (item.Maximum > 1)
            {
                for (int i = 0; i < stacks.Count && remaining > 0; i++)
                {
                    var target = stacks[i];
                    if (target.Name != item.Name || target.Quality != item.Quality || target.WorldLevel != item.WorldLevel) continue;
                    int moved = System.Math.Min(remaining, System.Math.Max(0, target.Maximum - target.Count));
                    stacks[i] = target.WithCount(target.Count + moved);
                    remaining -= moved;
                }
            }
            if (remaining == 0) continue;
            if (stacks.Count >= slots) return false;
            stacks.Add(item.WithCount(remaining));
        }
        return true;
    }
}
