using System.Collections.Generic;
using UnityEngine;

namespace PlayerTradingReforged;

public static class HUDTools
{
    private static readonly Dictionary<GameObject, bool> States = new Dictionary<GameObject, bool>();
    public static void SetHUDsActive(bool active)
    {
        if (active)
        {
            foreach (var pair in States) if (pair.Key) pair.Key.SetActive(pair.Value);
            States.Clear();
            return;
        }
        if (!InventoryGui.instance || States.Count != 0) return;
        foreach (var panel in new[] { InventoryGui.instance.m_crafting.gameObject, InventoryGui.instance.m_info.gameObject,
            InventoryGui.instance.m_armor.transform.parent.gameObject })
        { States[panel] = panel.activeSelf; panel.SetActive(false); }
    }
}
