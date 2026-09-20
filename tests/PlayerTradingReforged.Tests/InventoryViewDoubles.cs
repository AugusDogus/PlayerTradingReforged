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
        public bool m_equipped;
        public bool IsSameType(ItemData other) => m_shared.m_name == other.m_shared.m_name && m_quality == other.m_quality;
        public Vector2i m_gridPos;
        public SharedData m_shared = new();
        public ItemData Clone() => (ItemData)MemberwiseClone();
        public object GetIcon() => this;
        public sealed class SharedData { public string m_name = "wood"; public bool m_questItem; public int m_maxStackSize = 50, m_maxQuality = 1; }
    }
}
public sealed partial class Inventory
{
    public readonly List<ItemDrop.ItemData> GridItems = new();
    private int _width = 8, _height = 4;
    public Inventory() { }
    public Inventory(string name, object? background, int width, int height) { _width = width; _height = height; }
    public List<ItemDrop.ItemData> m_inventory => GridItems;
    public void Changed() => m_onChanged?.Invoke();
    public int GetWidth() => _width;
    public int GetHeight() => _height;
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
    public Element? VisibleElement;
    public Element? GetElement(int x, int y, int width) => VisibleElement;
    public Element? GetHoveredElement() => null;
    public void CreateItemTooltip(ItemDrop.ItemData item, Tooltip tooltip) { }
}
namespace UnityEngine
{
    public struct Color { public float Value; public static Color grey => new() { Value = 0.5f }; }
}
