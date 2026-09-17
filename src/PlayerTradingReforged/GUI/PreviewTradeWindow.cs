using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerTradingReforged.GUI;

internal sealed class PreviewTradeWindow : TradeWindow
{
    private GameObject? _clone;
    private TMP_Text? _weight;
    private TMP_Text? _title;
    private RectTransform? _placement;
    private float _horizontalPadding;
    public void Initialize(string name, string title, ConfigEntry<Vector2> offset, RectTransform? placement = null)
    {
        var source = InventoryGui.instance.m_container;
        _clone = Instantiate(source.gameObject, InventoryGui.instance.m_inventoryRoot, false);
        _clone.name = name;
        Initialize(_clone.GetComponent<RectTransform>(), title, false, offset);
        _placement = placement;
        _horizontalPadding = Mathf.Max(0f, Panel.rect.width - Grid.GetComponent<RectTransform>().rect.width);
        Grid.m_onSelected = null; Grid.m_onRightClick = null;
        Grid.m_onReleased = null; Grid.m_onEnter = null;
        Grid.OnMoveToUpperInventoryGrid = null; Grid.OnMoveToLowerInventoryGrid = null;
        Grid.OnSetTouchSelection = null;
        Grid.CanDropDragOntoItem = _ => false;
        // Instantiating a live container also clones existing slot objects, but not its private slot cache.
        foreach (Transform child in Grid.m_gridRoot) Destroy(child.gameObject);
        Grid.m_elements.Clear(); Grid.m_width = 0; Grid.m_height = 0;
        _title = _clone.transform.Find("container_name").GetComponent<TMP_Text>();
        _title.richText = false;
        _title.text = title;
        foreach (var button in _clone.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
        _weight = _clone.transform.Find("Weight").GetComponentInChildren<TMP_Text>(true);
        Group.ResetActiveElement(); Group.SetActive(false);
        Hide();
    }
    public void Display(Inventory inventory, string title)
    {
        Inventory = inventory;
        if (_title != null) _title.text = title;
        // Extra rows scroll within the existing container; fit wider modded inventories to the crafting area.
        Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            _horizontalPadding + inventory.GetWidth() * Grid.m_elementSpace);
        UpdatePosition();
        Refresh();
    }
    protected override void UpdatePosition()
    {
        if (_placement == null) { base.UpdatePosition(); return; }
        Panel.pivot = new Vector2(0, 1);
        Panel.localScale = Vector3.one * Mathf.Min(1f, _placement.rect.width / Panel.rect.width);
        Panel.position = _placement.TransformPoint(new Vector3(_placement.rect.xMin, _placement.rect.yMax, 0));
        Panel.anchoredPosition += new Vector2(UserOffset.x, -UserOffset.y);
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
