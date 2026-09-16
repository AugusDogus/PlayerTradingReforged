using System;
using UnityEngine;

namespace PlayerTradingReforged.GUI;

internal sealed class ContainerTradeWindow : TradeWindow
{
    private bool _flipped;
    private RectTransform? _takeAll;
    private RectTransform? _gridRoot;
    private GameObject? _stackAll;
    private bool _stackAllWasActive;
    private Transform? _originalParent;
    private int _originalSibling;

    public void Initialize()
    {
        var gui = InventoryGui.instance;
        Initialize(gui.m_container, Plugin.Localization.ToGiveWindowText, true, Plugin.ToGiveUserOffset);
        _originalParent = gui.m_container.parent;
        _originalSibling = gui.m_container.GetSiblingIndex();
        _takeAll = gui.m_takeAllButton.GetComponent<RectTransform>();
        _gridRoot = Grid.transform.Find("Root").GetComponent<RectTransform>();
        _stackAll = gui.m_stackAllButton.gameObject;
    }

    public override void Show()
    {
        Panel.SetParent(InventoryGui.instance.m_inventoryRoot, false);
        if (!_flipped) Flip();
        base.Show();
        if (_stackAll != null) { _stackAllWasActive = _stackAll.activeSelf; _stackAll.SetActive(false); }
        InventoryGui.instance.m_containerName.text = Inventory.GetName();
        InventoryGui.instance.Show(null);
    }

    public override void Hide()
    {
        base.Hide();
        if (_flipped && InventoryGui.instance) Flip();
        if (_originalParent != null && InventoryGui.instance)
        { Panel.SetParent(_originalParent, false); Panel.SetSiblingIndex(_originalSibling); }
        RestorePosition();
        if (_stackAll != null) _stackAll.SetActive(_stackAllWasActive);
        if (InventoryGui.instance) { InventoryGui.instance.CloseContainer(); InventoryGui.instance.Hide(); }
    }

    private void Flip()
    {
        if (_takeAll == null || _gridRoot == null) return;
        Vector2 position = _takeAll.anchoredPosition;
        RectTransformUtility.FlipLayoutOnAxis(Panel, 0, true, true);
        RectTransformUtility.FlipLayoutOnAxis(_gridRoot, 0, true, true);
        _takeAll.anchoredPosition = position;
        _flipped = !_flipped;
    }

    public override void Reset()
    {
        if (Inventory.NrOfItems() != 0) throw new InvalidOperationException("Cannot open a trade before its previous items are recovered.");
        base.Reset();
    }
}
