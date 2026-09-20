using System.Linq;
using HarmonyLib;
using PlayerTradingReforged.GUI;
using UnityEngine;

namespace PlayerTradingReforged.Patches;

internal static class InventoryGuiPatches
{
    private static bool Active => TradeHandler.Current != null && TradeHandler.Current.IsTradeWindowsOpen();
    private static Inventory? Offer => TradeHandler.Current?.TryGetToTradeInventory();
    private static bool Editable => TradeHandler.Current != null && TradeHandler.Current.CanEditOffer;

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
    private static class UpdateContainer
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (!Active || __instance.m_currentContainer != null) return true;
            var inventory = Offer ?? TradeWindowManager.Instance.GetToTradeInventory();
            if (__instance.m_animator.GetBool("visible"))
            {
                __instance.m_container.gameObject.SetActive(true);
                __instance.m_containerGrid.UpdateInventory(inventory, null, __instance.m_dragItem);
                __instance.m_containerName.text = inventory.GetName();
                if (__instance.m_firstContainerUpdate)
                { __instance.m_containerGrid.ResetView(); __instance.m_firstContainerUpdate = false; }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainerWeight))]
    private static class UpdateWeight
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (!Active || __instance.m_currentContainer != null) return true;
            var inventory = Offer ?? TradeWindowManager.Instance.GetToTradeInventory();
            __instance.m_containerWeight.text = Mathf.CeilToInt(inventory.GetTotalWeight()).ToString();
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsContainerOpen))]
    private static class IsContainerOpen
    {
        private static void Postfix(ref bool __result) { if (Active) __result = true; }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class KeepOpenOnUse
    {
        private static void Prefix()
        {
            if (!Active) return;
            ZInput.ResetButtonStatus("Use"); ZInput.ResetButtonStatus("JoyUse");
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    private static class CancelOnHide
    {
        private static void Prefix()
        {
            if (!Active) return;
            TradeHandler.Current?.TryCancelTradeInstance();
            TradeHandler.Current?.TryCancelWindowEditMode();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTakeAll))]
    private static class TakeAll
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (!Active || __instance.m_currentContainer != null) return true;
            if (Editable && Offer is Inventory inventory && !Player.m_localPlayer.IsTeleporting())
            {
                __instance.SetupDragItem(null, null, 1);
                foreach (var item in inventory.GetAllItems().ToArray())
                    Player.m_localPlayer.GetInventory().MoveItemToThis(inventory, item);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    private static class SelectItem
    {
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, InventoryGrid.Modifier mod)
        {
            if (!Active || __instance.m_currentContainer != null) return true;
            // Preview and placement mode are read-only. Final confirmation freezes item movement.
            if (!Editable || Offer is not Inventory offer) return false;
            var player = Player.m_localPlayer;
            if (player == null || player.IsTeleporting()) return false;
            if (grid.GetInventory() != offer && grid.GetInventory() != player.GetInventory()) return false;
            if (__instance.m_dragGo || mod != InventoryGrid.Modifier.Move) return true;
            // Only quick-move needs replacing. Vanilla handles drag, split, drop and equipment changes.
            if (item == null || item.m_shared.m_questItem) return false;
            player.RemoveEquipAction(item); player.UnequipItem(item);
            var destination = grid.GetInventory() == offer ? player.GetInventory() : offer;
            destination.MoveItemToThis(grid.GetInventory(), item);
            __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity);
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    private static class UseItem
    {
        private static bool Prefix(InventoryGrid grid) => !Active || (Editable && grid.GetInventory() == Player.m_localPlayer.GetInventory());
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDropOutside))]
    private static class DropItem
    {
        private static bool Prefix() => !Active || Editable;
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateGamepad))]
    private static class Gamepad
    {
        private static bool Prefix(InventoryGui __instance)
        {
            if (!Active || __instance.m_currentContainer != null) return true;
            if (__instance.m_inventoryGroup.IsActive)
            {
                if (ZInput.GetButtonDown("JoyTabLeft")) __instance.SetActiveGroup(1);
                if (ZInput.GetButtonDown("JoyTabRight")) __instance.SetActiveGroup(0);
                if (__instance.m_activeGroup != 0 && __instance.m_activeGroup != 1) __instance.SetActiveGroup(1);
            }
            return false;
        }
    }
}
