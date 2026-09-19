using System;
using PlayerTradingReforged.Patterns;
using PlayerTradingReforged.Trading;
using UnityEngine;
using TMPro;

namespace PlayerTradingReforged.GUI;

internal sealed class TradeWindowManager : MonoSingleton<TradeWindowManager>
{
    private sealed class Windows
    {
        public Windows(ContainerTradeWindow give, PreviewTradeWindow receive, PreviewTradeWindow partner, TradeButton accept, TradeButton cancel)
        { Give = give; Receive = receive; Partner = partner; Accept = accept; Cancel = cancel; }
        public ContainerTradeWindow Give { get; }
        public PreviewTradeWindow Receive { get; }
        public PreviewTradeWindow Partner { get; }
        public TradeButton Accept { get; }
        public TradeButton Cancel { get; }
    }
    private Windows? _windows;
    private Windows UI => _windows ?? throw new InvalidOperationException("Trade UI has not initialized.");
    private enum Mode { Closed, Trading, Editing, TradingEditing }
    private Mode _mode;
    private float _animationSpeed;
    private Vector3 _playerPosition, _playerScale;
    private Vector2 _playerPivot;
    private TMP_Text? _projectedWeight;
    private bool _partnerAvailable;
    public event Action? OnTradeAcceptPressed;
    public event Action? OnCancelTradePressed;
    public event Action? OnChangeTradePressed;

    protected override void Init()
    {
        var receive = gameObject.AddComponent<PreviewTradeWindow>();
        receive.Initialize("PlayerTradingReforgedReceive", Plugin.Localization.ToReceiveWindowText, Plugin.ToReceiveUserOffset);
        var partner = gameObject.AddComponent<PreviewTradeWindow>();
        partner.Initialize("PlayerTradingReforgedPartner", Plugin.Localization.PartnerInventoryText,
            Plugin.PartnerInventoryUserOffset);
        var give = gameObject.AddComponent<ContainerTradeWindow>(); give.Initialize();
        var accept = gameObject.AddComponent<TradeButton>();
        accept.Initialize(Plugin.Localization.AcceptTradeButtonText, AcceptClicked, Plugin.AcceptButtonUserOffset, give.Group, "JoyButtonX", "X");
        var cancel = gameObject.AddComponent<TradeButton>();
        cancel.Initialize(Plugin.Localization.CancelTradeButtonText, CancelClicked, Plugin.CancelButtonUserOffset, give.Group, "JoyButtonB", "B");
        _windows = new Windows(give, receive, partner, accept, cancel);
        _projectedWeight = Instantiate(InventoryGui.instance.m_containerName, InventoryGui.instance.m_inventoryRoot, false);
        _projectedWeight.name = "PlayerTradingReforgedProjectedWeight";
        _projectedWeight.alignment = TextAlignmentOptions.Center;
        _projectedWeight.fontSize = 20;
        _projectedWeight.raycastTarget = false;
        _projectedWeight.gameObject.SetActive(false);
    }
    private void AcceptClicked() => OnTradeAcceptPressed?.Invoke();
    private void CancelClicked() => OnCancelTradePressed?.Invoke();
    private void ChangeClicked() => OnChangeTradePressed?.Invoke();
    public Inventory GetToTradeInventory() => UI.Give.Inventory;
    public Inventory GetToReceiveInventory() => UI.Receive.Inventory;
    public void RefreshToReceiveWindow() => UI.Receive.Refresh();
    public void ShowPartnerInventory(Inventory? inventory)
    {
        _partnerAvailable = inventory != null;
        UI.Partner.Display(inventory ?? TradeInventory.Create(),
            inventory != null ? Plugin.Localization.PartnerInventoryText : Plugin.Localization.PartnerInventoryUnavailable);
    }
    public bool IsInWindowPositionMode() => _mode == Mode.Editing || _mode == Mode.TradingEditing;
    public void SetToTradeAccepted(bool accepted)
    {
        UI.Give.SetAccepted(accepted);
        UI.Accept.SetText(accepted ? Plugin.Localization.ChangeTradeButtonText : Plugin.Localization.AcceptTradeButtonText);
        if (accepted) UI.Accept.SetAction(ChangeClicked); else UI.Accept.SetAction(AcceptClicked);
    }
    public void SetToReceiveAccepted(bool accepted) => UI.Receive.SetAccepted(accepted);
    public void StartNewInstance()
    {
        var playerPanel = InventoryGui.instance.m_player;
        _playerPosition = playerPanel.localPosition; _playerScale = playerPanel.localScale; _playerPivot = playerPanel.pivot;
        _partnerAvailable = false;
        if (_projectedWeight != null) _projectedWeight.gameObject.SetActive(true);
        _mode = Mode.Trading;
        _animationSpeed = InventoryGui.instance.m_animator.speed;
        InventoryGui.instance.CloseContainer();
        InventoryGui.instance.m_animator.speed = 9999f;
        UI.Receive.Reset(); UI.Give.Reset();
        UI.Receive.Show(); UI.Give.Show();
        UI.Partner.Display(TradeInventory.Create(), Plugin.Localization.PartnerInventoryWaiting);
        UI.Partner.Show();
        SetToTradeAccepted(false); SetToReceiveAccepted(false);
        UI.Accept.SetActive(true); UI.Cancel.SetActive(true);
        HUDTools.SetHUDsActive(false);
    }
    public void ToggleWindowPositionMode()
    {
        if (IsInWindowPositionMode()) { DisableWindowPositionMode(); return; }
        if (_mode == Mode.Trading) _mode = Mode.TradingEditing;
        else { StartNewInstance(); _mode = Mode.Editing; }
        SetEditMode(true);
        TradeHandler.Show(Plugin.Localization.EditModeOn);
    }
    public void DisableWindowPositionMode()
    {
        if (!IsInWindowPositionMode()) return;
        SetEditMode(false);
        if (_mode == Mode.TradingEditing) _mode = Mode.Trading;
        else CancelInstance();
        TradeHandler.Show(Plugin.Localization.EditModeOff);
    }
    private void SetEditMode(bool editing)
    {
        UI.Give.SetEditMode(editing); UI.Receive.SetEditMode(editing); UI.Partner.SetEditMode(editing);
        UI.Accept.SetEditMode(editing); UI.Cancel.SetEditMode(editing);
    }
    public void CancelInstance()
    {
        if (_mode == Mode.Closed || _windows == null) return;
        _mode = Mode.Closed;
        SetEditMode(false);
        UI.Accept.SetActive(false); UI.Cancel.SetActive(false);
        UI.Receive.Hide(); UI.Receive.Inventory.RemoveAll(); UI.Give.Hide();
        UI.Partner.Hide(); UI.Partner.Inventory.RemoveAll();
        if (_projectedWeight != null) _projectedWeight.gameObject.SetActive(false);
        if (InventoryGui.instance)
        {
            var panel = InventoryGui.instance.m_player;
            panel.pivot = _playerPivot; panel.localScale = _playerScale; panel.localPosition = _playerPosition;
        }
        SetToTradeAccepted(false); SetToReceiveAccepted(false);
        HUDTools.SetHUDsActive(true);
        if (InventoryGui.instance) InventoryGui.instance.m_animator.speed = _animationSpeed;
    }
    private void LateUpdate()
    {
        if (_mode == Mode.Closed || _windows == null || !InventoryGui.instance) return;
        var root = InventoryGui.instance.m_inventoryRoot.GetComponent<RectTransform>();
        var player = InventoryGui.instance.m_player;
        const float sideSpace = 90, gap = 24, footer = 110;
        float column = Mathf.Max(Mathf.Max(player.rect.width, UI.Partner.Panel.rect.width),
            Mathf.Max(UI.Give.Panel.rect.width, UI.Receive.Panel.rect.width)) + sideSpace;
        float topHeight = Mathf.Max(player.rect.height, UI.Partner.Panel.rect.height);
        float bottomHeight = Mathf.Max(UI.Give.Panel.rect.height, UI.Receive.Panel.rect.height);
        float width = column * 2 + gap;
        float height = topHeight + gap + bottomHeight + footer;
        float scale = Mathf.Min(1f, Mathf.Min((root.rect.width - 64) / width, (root.rect.height - 100) / height));
        var origin = new Vector2(root.rect.center.x - width * scale / 2 + sideSpace * scale / 2,
            root.rect.center.y + height * scale / 2);
        player.pivot = new Vector2(0, 1); player.localScale = Vector3.one * scale;
        player.position = root.TransformPoint(origin);
        UI.Partner.Place(root, origin + new Vector2((column + gap) * scale, 0), scale);
        float lowerY = -(topHeight + gap) * scale;
        UI.Give.Place(root, origin + new Vector2(0, lowerY), scale);
        UI.Receive.Place(root, origin + new Vector2((column + gap) * scale, lowerY), scale);
        float footerY = origin.y + lowerY - (bottomHeight + 20) * scale;
        float center = root.rect.center.x;
        UI.Accept.Place(root, new Vector2(center - 100 * scale, footerY), scale);
        UI.Cancel.Place(root, new Vector2(center + 100 * scale, footerY), scale);
        if (_projectedWeight != null && Player.m_localPlayer != null)
        {
            // Offers are already in escrow, outside each player's current inventory.
            float own = Player.m_localPlayer.GetInventory().GetTotalWeight() + UI.Receive.Inventory.GetTotalWeight();
            string partner = _partnerAvailable
                ? Mathf.CeilToInt(UI.Partner.Inventory.GetTotalWeight() + UI.Give.Inventory.GetTotalWeight()).ToString()
                : "?";
            _projectedWeight.text = string.Format(Plugin.Localization.AfterTradeWeightText,
                Mathf.CeilToInt(own), partner);
            var rect = _projectedWeight.rectTransform;
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(width, 32);
            rect.localScale = Vector3.one * scale;
            rect.position = root.TransformPoint(new Vector2(center, footerY - 48 * scale));
        }
    }
    private void OnDestroy()
    {
        CancelInstance();
        if (_projectedWeight != null) Destroy(_projectedWeight.gameObject);
    }
}
