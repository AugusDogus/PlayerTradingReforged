using System.Collections.Generic;
using System.Linq;
using PlayerTradingReforged.Trading;
using UnityEngine;

namespace PlayerTradingReforged.GUI;

// Draws reserved items in their original slots while the existing escrow owns them.
internal sealed class OfferedInventoryView
{
    internal sealed class Transfer
    {
        public Transfer(Dictionary<ItemDrop.ItemData, int> before, int? sourceSlot, ItemDrop.ItemData? previousReturn)
        { Before = before; SourceSlot = sourceSlot; PreviousReturn = previousReturn; }
        public Dictionary<ItemDrop.ItemData, int> Before { get; }
        public int? SourceSlot { get; }
        public ItemDrop.ItemData? PreviousReturn { get; }
    }
    private readonly Inventory _player, _offer;
    private readonly ReservationLedger<ItemDrop.ItemData> _ledger = new();
    private readonly Dictionary<int, ItemDrop.ItemData> _templates = new();
    private Dictionary<ItemDrop.ItemData, int> _last = new();
    private ItemDrop.ItemData? _returning;
    public OfferedInventoryView(Inventory player, Inventory offer) { _player = player; _offer = offer; }
    public bool IsPlayerInventory(Inventory inventory) => inventory == _player;
    public bool IsOfferInventory(Inventory inventory) => inventory == _offer;
    private int Slot(Vector2i position) => position.y * _player.GetWidth() + position.x;
    private Vector2i Position(int slot) => new Vector2i(slot % _player.GetWidth(), slot / _player.GetWidth());
    private Dictionary<ItemDrop.ItemData, int> Snapshot() => _offer.GetAllItems().ToDictionary(item => item, item => item.m_stack);
    public bool Reserved(Vector2i position) => _ledger.CountAt(Slot(position)) > 0;
    public bool CanReturn(ItemDrop.ItemData item, Vector2i position) => _ledger.Origins(item).Contains(Slot(position));
    public bool CanAdd(Vector2i position) => !Reserved(position) || _returning != null && CanReturn(_returning, position);
    public int EmptyReservedSlots => _ledger.Slots.Count(slot => _player.GetItemAt(Position(slot).x, Position(slot).y) == null);

    public Transfer? Begin(Inventory destination, Inventory source, ItemDrop.ItemData item)
    {
        if (source != _offer && destination != _offer) return null;
        Reconcile();
        int? origin = source == _player && destination == _offer ? Slot(item.m_gridPos) : null;
        if (origin.HasValue) _templates[origin.Value] = item.Clone();
        var transfer = new Transfer(Snapshot(), origin, _returning);
        if (source == _offer && destination == _player) _returning = item;
        return transfer;
    }
    public void End(Transfer transfer)
    {
        try
        {
            var after = Snapshot();
            _ledger.Update(transfer.Before, after, transfer.SourceSlot);
            _last = after;
        }
        finally { _returning = transfer.PreviousReturn; }
    }
    private void Reconcile()
    {
        var after = Snapshot();
        _ledger.Update(_last, after, null);
        _last = after;
    }
    public Vector2i FindEmptySlot(bool topFirst)
    {
        if (_returning != null)
            foreach (int slot in _ledger.Origins(_returning))
            {
                var position = Position(slot);
                if (_player.GetItemAt(position.x, position.y) == null) return position;
            }
        for (int row = 0; row < _player.GetHeight(); row++)
        {
            int y = topFirst ? row : _player.GetHeight() - 1 - row;
            for (int x = 0; x < _player.GetWidth(); x++)
                if (_player.GetItemAt(x, y) == null && !Reserved(new Vector2i(x, y))) return new Vector2i(x, y);
        }
        return new Vector2i(-1, -1);
    }
    public Inventory CreateReservations()
    {
        Reconcile();
        var inventory = new Inventory("Reservations", null, _player.GetWidth(), _player.GetHeight());
        foreach (int slot in _ledger.Slots)
        {
            if (!_templates.TryGetValue(slot, out var template)) continue;
            var item = template.Clone();
            item.m_stack = _ledger.CountAt(slot);
            item.m_gridPos = Position(slot);
            item.m_equipped = false;
            inventory.m_inventory.Add(item);
        }
        inventory.Changed();
        return inventory;
    }
    public void Draw(InventoryGrid grid) => ReservedInventoryRenderer.Draw(grid, _player, CreateReservations());
    public void Clear() { _ledger.Clear(); _templates.Clear(); _last.Clear(); _returning = null; }
}
