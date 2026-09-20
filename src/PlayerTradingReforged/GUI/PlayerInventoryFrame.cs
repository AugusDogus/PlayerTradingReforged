using TMPro;
using UnityEngine;

namespace PlayerTradingReforged.GUI;

// Use the same complete frame and heading as the partner panel, extending above the player grid.
internal sealed class PlayerInventoryFrame
{
    public const float HeaderHeight = 50;
    private readonly GameObject _original;
    private readonly RectTransform _frame;
    private bool _wasActive;

    public PlayerInventoryFrame(RectTransform player, RectTransform partner)
    {
        _original = player.Find("Bkg").gameObject;
        _frame = Object.Instantiate(partner.Find("Bkg").GetComponent<RectTransform>(), player, false);
        _frame.name = "PlayerTradingReforgedPlayerFrame";
        _frame.SetSiblingIndex(_original.transform.GetSiblingIndex());
        _frame.anchorMin = Vector2.zero; _frame.anchorMax = Vector2.one;
        _frame.offsetMin = Vector2.zero; _frame.offsetMax = new Vector2(0, HeaderHeight);
        var title = Object.Instantiate(partner.Find("container_name").GetComponent<TMP_Text>(), _frame, false);
        title.text = Plugin.Localization.YourInventoryText;
        title.raycastTarget = false;
        title.alignment = TextAlignmentOptions.Center;
        var rect = title.rectTransform;
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(0, HeaderHeight);
        rect.anchoredPosition = Vector2.zero;
        _frame.gameObject.SetActive(false);
    }
    public void Show()
    {
        _wasActive = _original.activeSelf;
        _original.SetActive(false);
        _frame.gameObject.SetActive(true);
    }
    public void Hide()
    {
        if (_original) _original.SetActive(_wasActive);
        if (_frame) _frame.gameObject.SetActive(false);
    }
    public void Destroy() { if (_frame) Object.Destroy(_frame.gameObject); }
}
