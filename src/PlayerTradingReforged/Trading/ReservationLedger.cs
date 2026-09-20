using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTradingReforged.Trading;

// Tracks original slots across vanilla offer stack merges, splits, and partial returns.
// Item identities are live inventory objects; this ledger never owns or saves items.
internal sealed class ReservationLedger<T> where T : class
{
    internal sealed class Portion
    {
        public Portion(int slot, int count) { Slot = slot; Count = count; }
        public int Slot { get; }
        public int Count { get; set; }
    }
    private readonly Dictionary<T, List<Portion>> _items = new();
    public IEnumerable<int> Slots => _items.Values.SelectMany(parts => parts).Select(part => part.Slot).Distinct();
    public int CountAt(int slot) => _items.Values.SelectMany(parts => parts).Where(part => part.Slot == slot).Sum(part => part.Count);
    public IEnumerable<int> Origins(T item) => _items.TryGetValue(item, out var parts)
        ? parts.Select(part => part.Slot).Distinct() : Enumerable.Empty<int>();
    public void Clear() => _items.Clear();

    public void Update(IReadOnlyDictionary<T, int> before, IReadOnlyDictionary<T, int> after, int? sourceSlot)
    {
        var pool = new Queue<Portion>();
        foreach (var pair in before)
        {
            int removed = pair.Value - (after.TryGetValue(pair.Key, out int remaining) ? remaining : 0);
            if (removed <= 0 || !_items.TryGetValue(pair.Key, out var parts)) continue;
            while (removed > 0 && parts.Count > 0)
            {
                var part = parts[0];
                int count = Math.Min(removed, part.Count);
                pool.Enqueue(new Portion(part.Slot, count));
                part.Count -= count; removed -= count;
                if (part.Count == 0) parts.RemoveAt(0);
            }
            if (parts.Count == 0) _items.Remove(pair.Key);
        }
        int added = after.Sum(pair => Math.Max(0, pair.Value - (before.TryGetValue(pair.Key, out int count) ? count : 0)));
        int incoming = added - pool.Sum(part => part.Count);
        if (sourceSlot.HasValue && incoming > 0) pool.Enqueue(new Portion(sourceSlot.Value, incoming));
        foreach (var pair in after)
        {
            int count = pair.Value - (before.TryGetValue(pair.Key, out int previous) ? previous : 0);
            if (count <= 0) continue;
            if (!_items.TryGetValue(pair.Key, out var parts)) { parts = new List<Portion>(); _items.Add(pair.Key, parts); }
            while (count > 0 && pool.Count > 0)
            {
                var part = pool.Peek();
                int moved = Math.Min(count, part.Count);
                parts.Add(new Portion(part.Slot, moved));
                part.Count -= moved; count -= moved;
                if (part.Count == 0) pool.Dequeue();
            }
        }
    }
}
