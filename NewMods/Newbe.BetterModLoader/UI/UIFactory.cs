namespace Newbe.BetterModLoader.UI;

/// <summary>
/// Static factory helpers for building common UI elements in Better Mod Loader Manager UI.
/// Keep these methods presentation-only (no business state).
///
/// Scroll Sensitivity Standard:
/// All ScrollRect components should use scrollSensitivity = 3 for consistent user experience.
/// Use CreateScrollRect() method for standardized scroll view creation.
/// </summary>
internal static class UIFactory
{
    public static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    public static void SetAnchors(GameObject obj, Vector2 min, Vector2 max)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static GameObject CreateToolbarButton(Transform parent, string label, Vector2 size)
    {
        var btnObj = CreateUIObject("ToolbarButton_" + label, parent);
        var rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.6f, 1f);

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.5f, 0.75f, 1f);
        colors.pressedColor = new Color(0.15f, 0.35f, 0.55f, 1f);
        btn.colors = colors;

        var textObj = CreateUIObject("Text", btnObj.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(textObj, Vector2.zero, Vector2.one);

        return btnObj;
    }

    public static GameObject CreateSmallIconButton(Transform parent, string label)
    {
        var btnObj = CreateUIObject("IconButton_" + label, parent);
        var rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(32, 32);
        var le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 32;
        le.preferredHeight = 32;
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.6f, 1f);
        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.5f, 0.75f, 1f);
        colors.pressedColor = new Color(0.15f, 0.35f, 0.55f, 1f);
        btn.colors = colors;
        var textObj = CreateUIObject("Text", btnObj.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(textObj, Vector2.zero, Vector2.one);
        return btnObj;
    }

    /// <summary>
    /// Creates a ScrollRect with standardized sensitivity of 3
    /// </summary>
    /// <param name="name">Name for the ScrollRect GameObject</param>
    /// <param name="parent">Parent transform</param>
    /// <param name="horizontal">Enable horizontal scrolling</param>
    /// <param name="vertical">Enable vertical scrolling</param>
    /// <returns>Configured ScrollRect component</returns>
    public static ScrollRect CreateScrollRect(string name, Transform parent, bool horizontal = false,
        bool vertical = true)
    {
        var scrollGO = CreateUIObject(name, parent);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = horizontal;
        scrollRect.vertical = vertical;
        scrollRect.scrollSensitivity = 3; // Standardized sensitivity
        return scrollRect;
    }
}
