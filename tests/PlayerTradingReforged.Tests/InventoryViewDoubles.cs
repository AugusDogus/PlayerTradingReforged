// Inventory identities/positions for the real reservation view. Rendering still needs Unity.
using System.Collections.Generic;
using System.Linq;

public struct Vector2i
{
    public int x, y;
    public Vector2i(int x, int y) { this.x = x; this.y = y; }
}
public sealed class ItemDrop
{
    public sealed class ItemData
    {
        public int m_stack, m_quality;
        public Vector2i m_gridPos;
        public SharedData m_shared = new();
        public ItemData Clone() => (ItemData)MemberwiseClone();
        public object GetIcon() => this;
        public sealed class SharedData { public int m_maxStackSize = 50, m_maxQuality = 1; }
    }
}
public sealed partial class Inventory
{
    public readonly List<ItemDrop.ItemData> GridItems = new();
    public int GetWidth() => 8;
    public int GetHeight() => 4;
    public List<ItemDrop.ItemData> GetAllItems() => GridItems;
    public ItemDrop.ItemData? GetItemAt(int x, int y) => GridItems.FirstOrDefault(item => item.m_gridPos.x == x && item.m_gridPos.y == y);
}
public sealed class InventoryGrid
{
    public sealed class Label { public string text = ""; public bool enabled; }
    public sealed class Icon { public object? sprite; public UnityEngine.Color color; public bool enabled; }
    public sealed class Tooltip { public string m_text = ""; }
    public sealed class Element
    {
        public readonly Icon m_icon = new();
        public readonly Label m_quality = new(), m_amount = new();
        public readonly Tooltip m_tooltip = new();
    }
    public Element? GetElement(int x, int y, int width) => null;
    public Element? GetHoveredElement() => null;
    public void CreateItemTooltip(ItemDrop.ItemData item, Tooltip tooltip) { }
}
namespace UnityEngine
{
    public struct Color { public static Color grey => new(); }
}
