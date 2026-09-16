using System;
using BepInEx.Configuration;
using PlayerTradingReforged.Trading;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerTradingReforged.GUI;

internal abstract class TradeWindow : MonoBehaviour
{
    private RectTransform? _panel;
    private Image? _background;
    private InventoryGrid? _grid;
    private ConfigEntry<Vector2>? _offset;
    private Color _originalColor;
    private Vector2 _anchorMin, _anchorMax, _position;
    private bool _editing, _moving, _accepted;
    protected bool Visible { get; private set; }
    protected bool IsLeft { get; private set; }
    protected RectTransform Panel => _panel ?? throw new InvalidOperationException("Trade window has not initialized.");
    protected InventoryGrid Grid => _grid ?? throw new InvalidOperationException("Trade grid has not initialized.");
    public Inventory Inventory { get; private set; } = TradeInventory.Create();
    public UIGroupHandler Group => Panel.GetComponent<UIGroupHandler>();

    protected void Initialize(RectTransform panel, string title, bool left, ConfigEntry<Vector2> offset)
    {
        _panel = panel;
        _background = panel.Find("Bkg").GetComponent<Image>();
        _grid = panel.GetComponentInChildren<InventoryGrid>(true);
        _offset = offset;
        _originalColor = _background.color;
        _anchorMin = panel.anchorMin; _anchorMax = panel.anchorMax; _position = panel.anchoredPosition;
        IsLeft = left;
        Inventory = new Inventory(title, null, TradeInventory.Width, TradeInventory.Height);
    }

    protected virtual void Update()
    {
        if (!Visible || _panel == null || _offset == null) return;
        if (_editing)
        {
            Vector2 local = _panel.InverseTransformPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(0) && _panel.rect.Contains(local)) _moving = true;
            if (Input.GetMouseButtonUp(0)) _moving = false;
            if (_moving)
                _offset.Value += new Vector2(Input.GetAxis("Mouse X") * (IsLeft ? -8f : 8f), -Input.GetAxis("Mouse Y") * 8f);
        }
        UpdatePosition();
    }

    protected void UpdatePosition()
    {
        if (_panel == null || _offset == null) return;
        float scale = PlayerPrefs.GetFloat("GuiScale", 1f);
        float x = Screen.width / 30f * scale + _offset.Value.x;
        float y = Screen.height / 30f * scale + _offset.Value.y;
        Vector2 anchor = new Vector2(0.5f + (IsLeft ? -x : x) / Screen.width, 0.5f - y / Screen.height);
        _panel.anchorMin = anchor; _panel.anchorMax = anchor; _panel.anchoredPosition = anchor;
    }

    protected void RestorePosition()
    {
        if (_panel == null) return;
        _panel.anchorMin = _anchorMin; _panel.anchorMax = _anchorMax; _panel.anchoredPosition = _position;
    }

    public void SetAccepted(bool accepted) { _accepted = accepted; UpdateColor(); }
    public void SetEditMode(bool editing) { _editing = editing; _moving = false; UpdateColor(); }
    private void UpdateColor()
    {
        if (_background != null)
            _background.color = _editing ? Color.Lerp(_originalColor, Color.magenta, 0.35f) :
                _accepted ? Color.Lerp(_originalColor, Color.green, 0.35f) : _originalColor;
    }

    public virtual void Show() { Visible = true; UpdatePosition(); }
    public virtual void Hide() { Visible = false; _moving = false; }
    public virtual void Reset()
    {
        SetAccepted(false);
        Grid.m_inventory = Inventory;
        Grid.UpdateInventory(Inventory, null, null);
    }
    public virtual void Refresh() => Grid.UpdateInventory(Inventory, null, null);
}
