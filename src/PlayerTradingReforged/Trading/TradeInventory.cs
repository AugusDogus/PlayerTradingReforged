using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayerTradingReforged.Trading;

internal static class TradeInventory
{
    public const int Width = 6;
    public const int Height = 4;
    public const int MaxPackageBytes = 256 * 1024;
    public static Inventory Create() => new Inventory("Trade", null, Width, Height);
    public static string Save(Inventory inventory)
    {
        var package = new ZPackage();
        inventory.Save(package);
        return package.GetBase64();
    }

    public static bool TryLoad(string encoded, out Inventory inventory)
    {
        inventory = Create();
        if (encoded.Length > MaxPackageBytes * 4 / 3 + 4) return false;
        try
        {
            var package = new ZPackage(encoded);
            var current = new ZPackage();
            Create().Save(current);
            current.SetPos(0);
            int version = current.ReadInt();
            if (package.ReadInt() != version) return false;
            int count = package.ReadUShort();
            if (count > Width * Height) return false;
            var positions = new HashSet<Vector2i>();
            for (int i = 0; i < count; i++)
            {
                // Inventory.Load silently clamps oversized stacks and skips missing prefabs.
                // Decode and validate first so neither can turn into a different accepted offer.
                var (prefabHash, item) = ItemDrop.ItemData.Load(package, (global::Version.Item)version);
                var prefab = ObjectDB.instance.GetItemPrefab(prefabHash);
                if (prefab == null || !prefab.TryGetComponent<ItemDrop>(out var drop)) return false;
                item.m_shared = drop.m_itemData.m_shared;
                item.m_dropPrefab = prefab;
                if (item.m_stack <= 0 || item.m_stack > item.m_shared.m_maxStackSize || item.m_shared.m_questItem ||
                    item.m_quality < 1 || item.m_quality > item.m_shared.m_maxQuality || item.m_variant < 0 ||
                    item.m_variant >= item.m_shared.m_icons.Length ||
                    float.IsNaN(item.m_durability) || float.IsInfinity(item.m_durability) || item.m_durability < 0 ||
                    item.m_gridPos.x < 0 || item.m_gridPos.x >= Width || item.m_gridPos.y < 0 || item.m_gridPos.y >= Height ||
                    !positions.Add(item.m_gridPos)) return false;
                item.m_equipped = false;
                inventory.m_inventory.Add(item);
            }
            if (package.GetPos() != package.Size()) return false;
            inventory.Changed();
            return true;
        }
        catch (Exception error) when (error is FormatException || error is IOException || error is ArgumentException || error is OverflowException)
        {
            Plugin.Warn($"Rejected unreadable trade inventory: {error.Message}");
            return false;
        }
    }

    public static bool CanFit(Inventory destination, Inventory incoming)
    {
        TradeCapacity.Stack Describe(ItemDrop.ItemData item) => new TradeCapacity.Stack(
            item.m_shared.m_name, item.m_quality, item.m_worldLevel, item.m_stack, item.m_shared.m_maxStackSize);
        return TradeCapacity.CanFit(destination.GetWidth() * destination.GetHeight(),
            destination.GetAllItems().Select(Describe).ToArray(), incoming.GetAllItems().Select(Describe).ToArray());
    }

    public static void Replace(Inventory target, Inventory source)
    {
        target.RemoveAll();
        foreach (var item in source.GetAllItems()) target.m_inventory.Add(item.Clone());
        target.Changed();
    }
}
