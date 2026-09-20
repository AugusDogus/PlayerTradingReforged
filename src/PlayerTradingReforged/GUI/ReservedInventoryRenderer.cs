using UnityEngine;

namespace PlayerTradingReforged.GUI;

internal static class ReservedInventoryRenderer
{
    public static void Draw(InventoryGrid grid, Inventory availableInventory, Inventory reservations)
    {
        foreach (var template in reservations.GetAllItems())
        {
            var position = template.m_gridPos;
            var element = grid.GetElement(position.x, position.y, availableInventory.GetWidth());
            if (element == null) continue;
            int reserved = template.m_stack;
            var available = availableInventory.GetItemAt(position.x, position.y);
            if (available == null)
            {
                element.m_icon.enabled = true;
                element.m_icon.sprite = template.GetIcon();
                element.m_icon.color = Color.grey; // Same tint as vanilla's dragged item.
                element.m_quality.enabled = template.m_shared.m_maxQuality > 1;
                element.m_quality.text = template.m_quality.ToString();
                element.m_amount.enabled = template.m_shared.m_maxStackSize > 1;
                element.m_amount.text = reserved + "/" + template.m_shared.m_maxStackSize;
                if (grid.GetHoveredElement() == element)
                {
                    template.m_stack = reserved;
                    grid.CreateItemTooltip(template, element.m_tooltip);
                }
            }
            else
            {
                // Keep the normal usable-count/stack-limit format; the tooltip explains the reservation.
                element.m_amount.enabled = true;
                element.m_amount.text = available.m_stack + "/" + available.m_shared.m_maxStackSize;
            }
            if (grid.GetHoveredElement() == element)
                element.m_tooltip.m_text += "\n" + string.Format(Plugin.Localization.ReservedForTradeText, reserved);
        }
    }
}
