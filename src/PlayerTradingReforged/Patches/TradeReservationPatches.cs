using System;
using HarmonyLib;
using PlayerTradingReforged.GUI;
using UnityEngine;

namespace PlayerTradingReforged.Patches;

internal static class TradeReservationPatches
{
    private static OfferedInventoryView? View => TradeWindowManager.Current?.OfferedView;

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData))]
    private static class QuickMove
    {
        private static void Prefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item,
            out OfferedInventoryView.Transfer? __state) => __state = View?.Begin(__instance, fromInventory, item);
        private static void Finalizer(OfferedInventoryView.Transfer? __state) { if (__state != null) View?.End(__state); }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
    private static class MoveToSlot
    {
        private static void Prefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item,
            out OfferedInventoryView.Transfer? __state) => __state = View?.Begin(__instance, fromInventory, item);
        private static void Finalizer(OfferedInventoryView.Transfer? __state) { if (__state != null) View?.End(__state); }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    private static class EmptySlot
    {
        private static bool Prefix(Inventory __instance, bool topFirst, ref Vector2i __result)
        {
            var view = View;
            if (view == null || !view.IsPlayerInventory(__instance)) return true;
            __result = view.FindEmptySlot(topFirst); return false;
        }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
    private static class EmptyCount
    {
        private static void Postfix(Inventory __instance, ref int __result)
        {
            var view = View;
            if (view != null && view.IsPlayerInventory(__instance)) __result = Math.Max(0, __result - view.EmptyReservedSlots);
        }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
    private static class HasSlot
    {
        private static void Postfix(Inventory __instance, ref bool __result)
        {
            var view = View;
            if (view != null && view.IsPlayerInventory(__instance)) __result = view.FindEmptySlot(true).x >= 0;
        }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool))]
    private static class AddToSlot
    {
        private static bool Prefix(Inventory __instance, int x, int y, ref bool __result)
        {
            var view = View;
            if (view == null || !view.IsPlayerInventory(__instance) || view.CanAdd(new Vector2i(x, y))) return true;
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindFreeStackItem))]
    private static class FreeStack
    {
        private static void Postfix(Inventory __instance, string name, int quality, float worldLevel, ref ItemDrop.ItemData? __result)
        {
            var view = View;
            if (view == null || !view.IsPlayerInventory(__instance) || __result == null || view.CanAdd(__result.m_gridPos)) return;
            __result = null;
            foreach (var item in __instance.GetAllItems())
                if (item.m_shared.m_name == name && item.m_quality == quality && item.m_worldLevel == worldLevel &&
                    item.m_stack < item.m_shared.m_maxStackSize && view.CanAdd(item.m_gridPos))
                { __result = item; break; }
        }
    }
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    private static class DropOnReservation
    {
        private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos, ref bool __result)
        {
            var view = View;
            if (view == null) return true;
            var destination = __instance.GetInventory();
            var target = destination.GetItemAt(pos.x, pos.y);
            // Vanilla swaps remove the dragged item before moving either side. A reserved
            // source slot cannot be replaced, so reject swaps before that destructive step.
            if ((view.IsOfferInventory(destination) || view.IsOfferInventory(fromInventory)) &&
                target != null && target != item && (!target.IsSameType(item) || target.m_shared.m_maxStackSize == 1))
            { __result = false; return false; }
            if (!view.IsPlayerInventory(destination) || !view.Reserved(pos)) return true;
            if (!view.IsPlayerInventory(fromInventory) && view.CanReturn(item, pos)) return true;
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    private static class DrawReservations
    {
        private static void Postfix(InventoryGrid __instance)
        {
            var view = View;
            if (view != null && __instance == InventoryGui.instance.m_playerGrid) view.Draw(__instance);
        }
    }
}
