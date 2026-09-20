using TMPro;
using UnityEngine;

namespace PlayerTradingReforged.GUI;

// Reuse the game's weight badge without changing the original inventory widget.
internal sealed class TradeWeightBadge
{
    private readonly RectTransform _original;
    private readonly RectTransform _badge;
    private readonly TMP_Text _text;
    private readonly Color _color;
    private bool _originalActive;

    public TradeWeightBadge(RectTransform original)
    {
        _original = original;
        _badge = Object.Instantiate(original, original.parent, false);
        _badge.name = "PlayerTradingReforgedWeight";
        _badge.pivot = new Vector2(0, 0.5f);
        _badge.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 125);
        _badge.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 96);
        _text = _badge.GetComponentInChildren<TMP_Text>(true);
        _color = _text.color;
        _text.enableAutoSizing = false;
        _text.fontSize = 16;
        _text.alignment = TextAlignmentOptions.Center;
        var textRect = _text.rectTransform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 4); textRect.offsetMax = new Vector2(-4, -8);
        _badge.gameObject.SetActive(false);
    }

    public void Show()
    {
        _originalActive = _original.gameObject.activeSelf;
        _original.gameObject.SetActive(false);
        _badge.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_original) _original.gameObject.SetActive(_originalActive);
        if (_badge) _badge.gameObject.SetActive(false);
    }

    public void Display(float? current, float? projected, float? capacity)
    {
        // Keep the badge just outside the inventory's right edge, like the vanilla weight widget.
        _badge.anchorMin = _badge.anchorMax = new Vector2(1, 0);
        _badge.anchoredPosition = new Vector2(0, 65);
        string max = Format(capacity);
        string after = Format(projected) + "/" + max;
        if (projected.HasValue && capacity.HasValue && projected.Value > capacity.Value)
            after = "<color=#FF5555>" + after + "</color>";
        _text.color = _color;
        _text.text = Format(current) + "/" + max + "\n<size=12>" + Plugin.Localization.ProjectedWeightText + "</size>\n" + after;
    }

    private static string Format(float? value) => value.HasValue ? Mathf.CeilToInt(value.Value).ToString() : "?";
    public void Destroy() { if (_badge) Object.Destroy(_badge.gameObject); }
}
