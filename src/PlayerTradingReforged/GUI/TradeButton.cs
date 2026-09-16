using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PlayerTradingReforged.GUI;

internal sealed class TradeButton : MonoBehaviour
{
    private GameObject? _object;
    private Button? _button;
    private RectTransform? _rect;
    private TMP_Text? _text;
    private Image? _image;
    private Color _original;
    private ConfigEntry<Vector2>? _offset;
    private float _verticalOffset;
    private bool _editing, _moving;

    public void Initialize(string title, UnityAction action, ConfigEntry<Vector2> offset, UIGroupHandler group, string joyButton, string joyHint, float verticalOffset)
    {
        _offset = offset; _verticalOffset = verticalOffset;
        _object = Instantiate(InventoryGui.instance.m_takeAllButton.gameObject, InventoryGui.instance.m_inventoryRoot, false);
        _object.name = "PlayerTradingReforged" + joyHint;
        _button = _object.GetComponent<Button>();
        _rect = _object.GetComponent<RectTransform>();
        _text = _button.GetComponentInChildren<TMP_Text>(true);
        _image = _object.GetComponent<Image>();
        _original = _image.color;
        var gamepad = _object.GetComponent<UIGamePad>();
        gamepad.m_zinputKey = joyButton; gamepad.m_group = group;
        var hint = _object.transform.Find("gamepad_hint").GetComponentInChildren<TMP_Text>(true);
        hint.text = joyHint;
        SetText(title); SetAction(action); UpdatePosition(); SetActive(false);
    }

    private void Update()
    {
        if (_object == null || !_object.activeInHierarchy || _rect == null || _offset == null) return;
        if (_editing)
        {
            Vector2 local = _rect.InverseTransformPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(0) && _rect.rect.Contains(local)) _moving = true;
            if (Input.GetMouseButtonUp(0)) _moving = false;
            if (_moving) _offset.Value += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 8f;
        }
        UpdatePosition();
    }
    private void UpdatePosition()
    {
        if (_rect == null || _offset == null) return;
        Vector2 anchor = new Vector2(0.5f + _offset.Value.x / Screen.width, 0.5f + _verticalOffset / 30f + _offset.Value.y / Screen.height);
        _rect.anchorMin = anchor; _rect.anchorMax = anchor; _rect.anchoredPosition = anchor;
    }
    public void SetText(string text) { if (_text != null) _text.text = text; }
    public void SetAction(UnityAction action)
    {
        if (_button == null) return;
        _button.onClick = new Button.ButtonClickedEvent(); _button.onClick.AddListener(action);
    }
    public void SetActive(bool active) { if (_object != null) _object.SetActive(active); }
    public void SetEditMode(bool editing)
    {
        _editing = editing; _moving = false;
        if (_button != null) _button.interactable = !editing;
        if (_image != null) _image.color = editing ? Color.Lerp(_original, Color.magenta, 0.35f) : _original;
    }
    private void OnDestroy() { if (_object != null) Destroy(_object); }
}
