using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerTradingReforged.GUI;

internal sealed class PreviewTradeWindow : TradeWindow
{
    private GameObject? _clone;
    private TMP_Text? _weight;
    public void Initialize()
    {
        var source = InventoryGui.instance.m_container;
        _clone = Instantiate(source.gameObject, InventoryGui.instance.m_inventoryRoot, false);
        _clone.name = "PlayerTradingReforgedReceive";
        Initialize(_clone.GetComponent<RectTransform>(), Plugin.Localization.ToReceiveWindowText, false, Plugin.ToReceiveUserOffset);
        Grid.m_onSelected = null; Grid.m_onRightClick = null;
        Grid.m_onReleased = null; Grid.m_onEnter = null;
        // Instantiating a live container also clones existing slot objects, but not its private slot cache.
        foreach (Transform child in Grid.m_gridRoot) Destroy(child.gameObject);
        Grid.m_elements.Clear(); Grid.m_width = 0; Grid.m_height = 0;
        _clone.transform.Find("container_name").GetComponent<TMP_Text>().text = Inventory.GetName();
        foreach (var button in _clone.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
        _weight = _clone.transform.Find("Weight").GetComponentInChildren<TMP_Text>(true);
        Group.ResetActiveElement(); Group.SetActive(false);
        Hide();
    }
    public override void Show() { base.Show(); if (_clone != null) _clone.SetActive(true); }
    public override void Hide() { base.Hide(); if (_clone != null) _clone.SetActive(false); }
    public override void Reset() { Inventory.RemoveAll(); base.Reset(); Refresh(); }
    public override void Refresh()
    {
        base.Refresh();
        if (_weight != null) _weight.text = Mathf.CeilToInt(Inventory.GetTotalWeight()).ToString();
    }
    private void OnDestroy() { if (_clone != null) Destroy(_clone); }
}
