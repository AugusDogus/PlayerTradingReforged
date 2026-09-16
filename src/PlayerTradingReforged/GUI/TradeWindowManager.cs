using System;
using PlayerTradingReforged.Patterns;
using UnityEngine;

namespace PlayerTradingReforged.GUI;

internal sealed class TradeWindowManager : MonoSingleton<TradeWindowManager>
{
    private sealed class Windows
    {
        public Windows(ContainerTradeWindow give, PreviewTradeWindow receive, TradeButton accept, TradeButton cancel)
        { Give = give; Receive = receive; Accept = accept; Cancel = cancel; }
        public ContainerTradeWindow Give { get; }
        public PreviewTradeWindow Receive { get; }
        public TradeButton Accept { get; }
        public TradeButton Cancel { get; }
    }
    private Windows? _windows;
    private Windows UI => _windows ?? throw new InvalidOperationException("Trade UI has not initialized.");
    private enum Mode { Closed, Trading, Editing, TradingEditing }
    private Mode _mode;
    private float _animationSpeed;
    public event Action? OnTradeAcceptPressed;
    public event Action? OnCancelTradePressed;
    public event Action? OnChangeTradePressed;

    protected override void Init()
    {
        var receive = gameObject.AddComponent<PreviewTradeWindow>(); receive.Initialize();
        var give = gameObject.AddComponent<ContainerTradeWindow>(); give.Initialize();
        var accept = gameObject.AddComponent<TradeButton>();
        accept.Initialize(Plugin.Localization.AcceptTradeButtonText, AcceptClicked, Plugin.AcceptButtonUserOffset, give.Group, "JoyButtonX", "X", 1f);
        var cancel = gameObject.AddComponent<TradeButton>();
        cancel.Initialize(Plugin.Localization.CancelTradeButtonText, CancelClicked, Plugin.CancelButtonUserOffset, give.Group, "JoyButtonB", "B", -1f);
        _windows = new Windows(give, receive, accept, cancel);
    }
    private void AcceptClicked() => OnTradeAcceptPressed?.Invoke();
    private void CancelClicked() => OnCancelTradePressed?.Invoke();
    private void ChangeClicked() => OnChangeTradePressed?.Invoke();
    public Inventory GetToTradeInventory() => UI.Give.Inventory;
    public Inventory GetToReceiveInventory() => UI.Receive.Inventory;
    public void RefreshToReceiveWindow() => UI.Receive.Refresh();
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
        _mode = Mode.Trading;
        _animationSpeed = InventoryGui.instance.m_animator.speed;
        InventoryGui.instance.CloseContainer();
        InventoryGui.instance.m_animator.speed = 9999f;
        UI.Receive.Reset(); UI.Give.Reset();
        UI.Receive.Show(); UI.Give.Show();
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
        UI.Give.SetEditMode(editing); UI.Receive.SetEditMode(editing);
        UI.Accept.SetEditMode(editing); UI.Cancel.SetEditMode(editing);
    }
    public void CancelInstance()
    {
        if (_mode == Mode.Closed || _windows == null) return;
        _mode = Mode.Closed;
        SetEditMode(false);
        UI.Accept.SetActive(false); UI.Cancel.SetActive(false);
        UI.Receive.Hide(); UI.Receive.Inventory.RemoveAll(); UI.Give.Hide();
        SetToTradeAccepted(false); SetToReceiveAccepted(false);
        HUDTools.SetHUDsActive(true);
        if (InventoryGui.instance) InventoryGui.instance.m_animator.speed = _animationSpeed;
    }
    private void OnDestroy() => CancelInstance();
}
