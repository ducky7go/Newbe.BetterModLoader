using System.Collections;
using static Newbe.BetterModLoader.UI.UIFactory;

namespace Newbe.BetterModLoader.UI.ManagerUI.Views;

/// <summary>
/// Handles the top-right dropdown menu creation, toggling and outside-click close.
/// </summary>
internal class DropdownMenuView
{
    private readonly MonoBehaviour _host;
    private readonly Canvas _canvas;
    private GameObject? _dropdownMenu;

    public event Action? OnRevertRequested;

    public DropdownMenuView(MonoBehaviour host, Canvas canvas)
    {
        _host = host;
        _canvas = canvas;
    }

    public void Toggle(Transform parent)
    {
        if (_dropdownMenu != null)
        {
            UnityEngine.Object.Destroy(_dropdownMenu);
            _dropdownMenu = null;
            return;
        }

        _dropdownMenu = CreateUIObject("DropdownMenu", parent);
        var menuRect = _dropdownMenu.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(1, 1);
        menuRect.anchorMax = new Vector2(1, 1);
        menuRect.pivot = new Vector2(1, 1);
        menuRect.anchoredPosition = new Vector2(-10, -80);
        menuRect.sizeDelta = new Vector2(250, 60);

        var menuBg = _dropdownMenu.AddComponent<Image>();
        menuBg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        var outline = _dropdownMenu.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        outline.effectDistance = new Vector2(2, -2);

        var layout = _dropdownMenu.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.spacing = 5f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        var revertBtn =
            CreateDropdownButton(_dropdownMenu.transform, L.Rollback.Title, new Color(0.8f, 0.2f, 0.2f, 1f));
        revertBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            Close();
            OnRevertRequested?.Invoke();
        });

        _host.StartCoroutine(CloseMenuOnClickOutside());
    }

    public void Close()
    {
        if (_dropdownMenu != null)
        {
            UnityEngine.Object.Destroy(_dropdownMenu);
            _dropdownMenu = null;
        }
    }

    private GameObject CreateDropdownButton(Transform parent, string label, Color color)
    {
        var btnObj = CreateUIObject("DropdownButton_" + label, parent);
        var layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 45;

        var img = btnObj.AddComponent<Image>();
        img.color = color;

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        btn.colors = colors;

        var textObj = CreateUIObject("Text", btnObj.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        SetAnchors(textObj, Vector2.zero, Vector2.one);

        return btnObj;
    }

    private IEnumerator CloseMenuOnClickOutside()
    {
        yield return null; // avoid immediate close on same click
        while (_dropdownMenu != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_dropdownMenu != null && !RectTransformUtility.RectangleContainsScreenPoint(
                        _dropdownMenu.GetComponent<RectTransform>(), Input.mousePosition, _canvas.worldCamera))
                {
                    Close();
                    yield break;
                }
            }

            yield return null;
        }
    }
}
