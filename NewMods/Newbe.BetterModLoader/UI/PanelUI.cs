namespace Newbe.BetterModLoader.UI;

internal class PanelUI
{
    /// <summary>
    /// 创建独立模态弹窗面板（无参数），自动包含顶层 Canvas 检测与创建、半透明遮罩层等关键设置。
    /// 返回的 PanelBase 已经包含完整的模态弹框结构，可直接用于显示。
    /// <para>使用示例：</para>
    /// <code>
    /// var panelBase = UIFactory.CreatePanelBase();
    /// // 面板会自动显示在最顶层，无需传入 parent
    /// </code>
    /// </summary>
    /// <returns>已填充属性的 PanelBase 实例</returns>
    /// <summary>
    /// 创建独立模态弹窗面板（可指定 sortingOrder），自动包含顶层 Canvas 检测与创建、半透明遮罩层等关键设置。
    /// 返回的 PanelBase 已经包含完整的模态弹框结构，可直接用于显示。
    /// <para>使用示例：</para>
    /// <code>
    /// var panelBase = UIFactory.CreatePanelBase(order: 16000);
    /// // 面板会自动显示在最顶层，无需传入 parent
    /// </code>
    /// </summary>
    /// <param name="title">标题文本</param>
    /// <param name="order">Canvas sortingOrder，自动高于当前所有 Canvas，默认自动计算</param>
    /// <returns>已填充属性的 PanelBase 实例</returns>
    public static PanelBase CreatePanelBase(string? title = null, int? order = null)
    {
        // 2. 动态计算 sortingOrder
        int sortingOrder = order ?? CalcNextModalOrder();

        // 3. 创建顶层 Canvas
        var panel = new PanelBase();
        var canvasGo = new GameObject("ModalCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.overrideSorting = true;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        panel.CanvasGo = canvasGo;

        // 4. 添加全屏半透明背景层（带渐变效果）
        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgImg = bgGo.GetComponent<Image>();

        // 创建渐变背景效果
        CreateGradientBackground(bgImg, new Color(0.05f, 0.05f, 0.08f, 0.85f), new Color(0.15f, 0.15f, 0.20f, 0.75f));

        // 添加背景边框效果
        var bgOutline = bgGo.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.2f, 0.2f, 0.25f, 0.5f);
        bgOutline.effectDistance = new Vector2(5, 5);

        StretchFull(bgGo);

        // 5. 主面板（Root）挂载到 Canvas（带增强效果）
        var root = CreateUIObject("MainPanel", canvasGo.transform);
        var rootImg = root.AddComponent<Image>();

        // 尝试使用UIResourceManager的圆角资源，否则回退到Unity Resources
        Sprite? roundedSprite = null;
        try
        {
            roundedSprite = UIResourceManager.GetSprite("square_bg");
        }
        catch
        {
            roundedSprite = Resources.Load<Sprite>("UI_RoundedPanel");
        }

        if (roundedSprite != null)
        {
            rootImg.sprite = roundedSprite;
            rootImg.type = Image.Type.Sliced;
        }

        // 创建主面板渐变背景
        CreateGradientBackground(rootImg, new Color(0.12f, 0.12f, 0.16f, 0.98f), new Color(0.08f, 0.08f, 0.12f, 0.98f));

        // 添加主面板边框和阴影
        var rootOutline = root.AddComponent<Outline>();
        rootOutline.effectColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);
        rootOutline.effectDistance = new Vector2(3, 3);

        var rootShadow = root.AddComponent<Shadow>();
        rootShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        rootShadow.effectDistance = new Vector2(6, -6);

        SetAnchors(root, Vector2.zero, Vector2.one);
        panel.Root = root;

        // ContentPanel（内容区域带增强效果）
        var contentPanel = CreateUIObject("ContentPanel", root.transform);
        SetAnchors(contentPanel, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        var contentImg = contentPanel.AddComponent<Image>();

        // 创建内容区渐变背景
        CreateGradientBackground(contentImg, new Color(0.16f, 0.16f, 0.20f, 0.95f),
            new Color(0.12f, 0.12f, 0.16f, 0.95f));

        // 添加内容区边框
        if (roundedSprite != null)
        {
            contentImg.sprite = roundedSprite;
            contentImg.type = Image.Type.Sliced;
        }

        var contentOutline = contentPanel.AddComponent<Outline>();
        contentOutline.effectColor = new Color(0.25f, 0.25f, 0.30f, 0.8f);
        contentOutline.effectDistance = new Vector2(2, 2);

        panel.ContentPanel = contentPanel;

        // TitlePanel + TitleText + Toolbar（水平布局，带美化效果）
        var titlePanel = CreateUIObject("TitlePanel", contentPanel.transform);
        SetAnchors(titlePanel, new Vector2(0f, 0.9f), new Vector2(1f, 1f));

        // 添加标题面板背景
        var titlePanelImg = titlePanel.AddComponent<Image>();
        CreateGradientBackground(titlePanelImg, new Color(0.22f, 0.22f, 0.28f, 0.95f),
            new Color(0.16f, 0.16f, 0.20f, 0.95f));

        var titlePanelOutline = titlePanel.AddComponent<Outline>();
        titlePanelOutline.effectColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);
        titlePanelOutline.effectDistance = new Vector2(2, 2);

        var titleRow = CreateUIObject("TitleRow", titlePanel.transform);
        // TitleRow 需要填充整个 TitlePanel
        SetAnchors(titleRow, Vector2.zero, Vector2.one);
        var titleRowLayout = titleRow.AddComponent<HorizontalLayoutGroup>();
        titleRowLayout.spacing = 12f;
        titleRowLayout.childAlignment = TextAnchor.MiddleLeft;
        titleRowLayout.childForceExpandWidth = false;
        titleRowLayout.childForceExpandHeight = true;
        titleRowLayout.padding = new RectOffset(10, 10, 8, 8);

        // 左侧 Toolbar（美化）
        var leftToolbar = CreateUIObject("TitleLeftToolbar", titleRow.transform);
        var leftToolbarImg = leftToolbar.AddComponent<Image>();
        CreateGradientBackground(leftToolbarImg, new Color(0.18f, 0.18f, 0.24f, 0.9f),
            new Color(0.14f, 0.14f, 0.18f, 0.9f));

        var leftToolbarOutline = leftToolbar.AddComponent<Outline>();
        leftToolbarOutline.effectColor = new Color(0.25f, 0.25f, 0.30f, 0.8f);
        leftToolbarOutline.effectDistance = new Vector2(1, 1);

        var ltLayout = leftToolbar.AddComponent<HorizontalLayoutGroup>();
        ltLayout.spacing = 10f;
        ltLayout.childAlignment = TextAnchor.MiddleLeft;
        ltLayout.childForceExpandWidth = false;
        ltLayout.childForceExpandHeight = true;
        ltLayout.padding = new RectOffset(8, 8, 4, 4);
        // 使用 ContentSizeFitter 自动适应按钮宽度
        var ltFitter = leftToolbar.AddComponent<ContentSizeFitter>();
        ltFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ltFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        panel.TitleLeftToolbar = leftToolbar;

        // 右侧 TitleText（美化文本样式）
        var titleText = CreateText(titleRow, title ?? "Title", 32);
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.fontStyle = FontStyles.Bold;
        titleText.outlineWidth = 0.3f;
        titleText.outlineColor = new Color(0f, 0f, 0f, 0.7f);
        // 让标题占据剩余空间
        var titleTextLe = titleText.gameObject.AddComponent<LayoutElement>();
        titleTextLe.flexibleWidth = 1f;
        titleTextLe.minWidth = 100f;
        panel.TitleText = titleText;
        panel.TitlePanel = titlePanel;

        // HorizontalPanel
        var horizontalPanel = CreateUIObject("HorizontalPanel", contentPanel.transform);
        SetAnchors(horizontalPanel, new Vector2(0f, 0f), new Vector2(1f, 0.9f));
        var hLayout = horizontalPanel.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 15f;
        hLayout.padding = new RectOffset(10, 10, 10, 10);
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = true;
        panel.HorizontalPanel = horizontalPanel;

        // LeftPanel (left) - 增强效果
        var leftPanel = CreateUIObject("LeftPanel", horizontalPanel.transform);
        var lpImg = leftPanel.AddComponent<Image>();
        CreateGradientBackground(lpImg, new Color(0.20f, 0.20f, 0.25f, 0.95f), new Color(0.14f, 0.14f, 0.18f, 0.95f));

        // 添加左面板边框和阴影
        var leftPanelOutline = leftPanel.AddComponent<Outline>();
        leftPanelOutline.effectColor = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        leftPanelOutline.effectDistance = new Vector2(2, 2);

        var leftPanelShadow = leftPanel.AddComponent<Shadow>();
        leftPanelShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        leftPanelShadow.effectDistance = new Vector2(3, -3);

        var lpRect = leftPanel.GetComponent<RectTransform>();
        lpRect.sizeDelta = new Vector2(300f, 0f);
        var lpLe = leftPanel.AddComponent<LayoutElement>();
        lpLe.preferredWidth = 300f;
        lpLe.flexibleWidth = 0f;
        // 添加垂直布局组件
        var lpVLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
        lpVLayout.spacing = 12f;
        lpVLayout.padding = new RectOffset(12, 12, 12, 12);
        lpVLayout.childForceExpandWidth = true;
        lpVLayout.childForceExpandHeight = false;
        lpVLayout.childAlignment = TextAnchor.UpperCenter;
        lpVLayout.childControlWidth = true;
        lpVLayout.childControlHeight = true;
        panel.LeftPanel = leftPanel;
        panel.LeftPanelLayout = lpVLayout;

        // RightPanel (right) - 增强效果
        var rightPanel = CreateUIObject("RightPanel", horizontalPanel.transform);
        var rpImg = rightPanel.AddComponent<Image>();
        CreateGradientBackground(rpImg, new Color(0.24f, 0.24f, 0.20f, 0.95f), new Color(0.18f, 0.18f, 0.14f, 0.95f));

        // 添加右面板边框和阴影
        var rightPanelOutline = rightPanel.AddComponent<Outline>();
        rightPanelOutline.effectColor = new Color(0.25f, 0.25f, 0.20f, 0.8f);
        rightPanelOutline.effectDistance = new Vector2(2, 2);

        var rightPanelShadow = rightPanel.AddComponent<Shadow>();
        rightPanelShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        rightPanelShadow.effectDistance = new Vector2(3, -3);

        var rpLe = rightPanel.AddComponent<LayoutElement>();
        rpLe.flexibleWidth = 1f;
        // 添加垂直布局组件
        var rpVLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
        rpVLayout.spacing = 12f;
        rpVLayout.padding = new RectOffset(12, 12, 12, 12);
        rpVLayout.childForceExpandWidth = true;
        rpVLayout.childForceExpandHeight = false;
        rpVLayout.childAlignment = TextAnchor.UpperCenter;
        rpVLayout.childControlWidth = true;
        rpVLayout.childControlHeight = true;
        panel.RightPanel = rightPanel;
        panel.RightPanelLayout = rpVLayout;

        // RightToolbar（增强效果）
        var rightToolbar = CreateUIObject("RightToolbar", rightPanel.transform);
        SetAnchors(rightToolbar, new Vector2(0f, 0.92f), new Vector2(1f, 1f));

        var rightToolbarImg = rightToolbar.AddComponent<Image>();
        CreateGradientBackground(rightToolbarImg, new Color(0.21f, 0.21f, 0.17f, 0.95f),
            new Color(0.16f, 0.16f, 0.12f, 0.95f));

        var rightToolbarOutline = rightToolbar.AddComponent<Outline>();
        rightToolbarOutline.effectColor = new Color(0.25f, 0.25f, 0.20f, 0.8f);
        rightToolbarOutline.effectDistance = new Vector2(1, 1);

        var rtLayout = rightToolbar.AddComponent<HorizontalLayoutGroup>();
        rtLayout.spacing = 10f;
        rtLayout.padding = new RectOffset(10, 10, 6, 6);
        rtLayout.childForceExpandWidth = false;
        rtLayout.childForceExpandHeight = true;
        rtLayout.childAlignment = TextAnchor.MiddleLeft;
        panel.RightToolbar = rightToolbar;

        return panel;
    }

    /// <summary>
    /// 将 RectTransform 拉伸为全屏（锚点 0,0 到 1,1，偏移为 0）。
    /// </summary>
    /// <param name="obj">目标 GameObject</param>
    public static void StretchFull(GameObject obj)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// 获取下一个弹窗的 sortingOrder（自动高于所有现有 Canvas）。
    /// </summary>
    /// <returns>下一个可用的 sortingOrder</returns>
    private static int CalcNextModalOrder()
    {
        var canvases = GameObject.FindObjectsOfType<Canvas>();
        int maxOrder = 15000;
        foreach (var c in canvases)
        {
            if (c.overrideSorting && c.sortingOrder > maxOrder)
            {
                maxOrder = c.sortingOrder;
            }
        }

        return maxOrder + 10;
    }

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

    public static TextMeshProUGUI CreateText(GameObject parent, string text, int fontSize)
    {
        var textObj = CreateUIObject("Text", parent.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        // 添加默认文本样式
        tmp.outlineWidth = 0.15f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.6f);
        SetAnchors(textObj, Vector2.zero, Vector2.one);
        return tmp;
    }

    /// <summary>
    /// 创建一个带标签的 Toggle（Checkbox）组件
    /// </summary>
    /// <param name="parent">父对象</param>
    /// <param name="labelText">标签文本</param>
    /// <param name="fontSize">字体大小，默认 18</param>
    /// <returns>Toggle 组件</returns>
    public static Toggle CreateToggle(GameObject parent, string labelText, int fontSize = 18)
    {
        // 创建容器行
        var row = CreateUIObject("ToggleRow", parent.transform);
        var rowHL = row.AddComponent<HorizontalLayoutGroup>();
        rowHL.spacing = 10f;
        rowHL.childAlignment = TextAnchor.MiddleLeft;
        rowHL.childForceExpandWidth = false;
        rowHL.childForceExpandHeight = false;
        rowHL.childControlWidth = false;
        rowHL.childControlHeight = false;

        // 创建 Toggle
        var toggleGO = CreateUIObject("Toggle", row.transform);
        var toggle = toggleGO.AddComponent<Toggle>();

        // 设置 Toggle 大小
        var toggleRect = toggleGO.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(24, 24);

        // 背景（美化）
        var bgGO = CreateUIObject("Background", toggleGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        CreateGradientBackground(bgImg, new Color(0.6f, 0.6f, 0.65f, 1f), new Color(0.4f, 0.4f, 0.45f, 1f));

        // 添加背景边框
        var bgOutline = bgGO.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        bgOutline.effectDistance = new Vector2(1, 1);

        SetAnchors(bgGO, Vector2.zero, Vector2.one);

        // 勾选标记（美化）
        var checkGO = CreateUIObject("Checkmark", bgGO.transform);
        var checkImg = checkGO.AddComponent<Image>();

        // 使用UIResourceManager的资源，否则回退到颜色
        try
        {
            var checkSprite = UIResourceManager.GetSprite("check_success");
            checkImg.sprite = checkSprite;
            checkImg.type = Image.Type.Sliced;
            checkImg.color = Color.white;
        }
        catch
        {
            CreateGradientBackground(checkImg, new Color(0.3f, 0.8f, 0.4f, 1f), new Color(0.2f, 0.6f, 0.3f, 1f));
        }

        SetAnchors(checkGO, Vector2.zero, Vector2.one);
        var checkRect = checkGO.GetComponent<RectTransform>();
        checkRect.offsetMin = new Vector2(4, 4);
        checkRect.offsetMax = new Vector2(-4, -4);

        toggle.graphic = checkImg;
        toggle.targetGraphic = bgImg;

        // 标签
        var labelGO = CreateUIObject("Label", row.transform);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = fontSize;
        labelTMP.color = Color.white;
        labelTMP.alignment = TextAlignmentOptions.Left;

        // 为整个行添加 LayoutElement（如果父对象有 VerticalLayoutGroup）
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 40f;
        rowLE.flexibleHeight = 0f;

        return toggle;
    }

    /// <summary>
    /// 创建带标签和中间文本的进度条
    /// </summary>
    /// <param name="parent">父对象</param>
    /// <param name="labelText">标签文本</param>
    /// <param name="progressHeight">进度条高度，默认 30</param>
    /// <returns>包含 Slider 和 TextMeshProUGUI 的元组</returns>
    public static (Slider slider, TextMeshProUGUI centerText) CreateProgressBar(GameObject parent, string labelText,
        float progressHeight = 30f)
    {
        // 创建容器（用于标签）
        var container = CreateUIObject("ProgressContainer", parent.transform);
        var containerLE = container.AddComponent<LayoutElement>();
        containerLE.preferredHeight = 70f; // 标签 + 进度条
        containerLE.flexibleHeight = 0f;

        // 标签
        var labelGO = CreateUIObject("Label", container.transform);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = 18;
        labelTMP.color = Color.white;
        labelTMP.alignment = TextAlignmentOptions.Left;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 25f);

        // 进度条 Slider
        var sliderGO = CreateUIObject("ProgressBar", container.transform);
        var slider = sliderGO.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 0f);
        sliderRect.sizeDelta = new Vector2(0f, progressHeight);

        // 背景（美化）
        var bgGO = CreateUIObject("Background", sliderGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        CreateGradientBackground(bgImg, new Color(0.25f, 0.25f, 0.30f, 1f), new Color(0.15f, 0.15f, 0.20f, 1f));

        // 添加背景边框
        var bgOutline = bgGO.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        bgOutline.effectDistance = new Vector2(1, 1);

        // 添加背景阴影
        var bgShadow = bgGO.AddComponent<Shadow>();
        bgShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        bgShadow.effectDistance = new Vector2(2, -2);

        SetAnchors(bgGO, Vector2.zero, Vector2.one);

        // Fill Area
        var fillAreaGO = CreateUIObject("FillArea", sliderGO.transform);
        SetAnchors(fillAreaGO, Vector2.zero, Vector2.one);

        // Fill（美化）
        var fillGO = CreateUIObject("Fill", fillAreaGO.transform);
        var fillImg = fillGO.AddComponent<Image>();
        CreateGradientBackground(fillImg, new Color(0.3f, 0.9f, 0.4f, 1f), new Color(0.2f, 0.7f, 0.3f, 1f));
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // 添加填充区域发光效果
        var fillGlow = fillGO.AddComponent<Shadow>();
        fillGlow.effectColor = new Color(0.2f, 0.8f, 0.3f, 0.6f);
        fillGlow.effectDistance = new Vector2(1, 1);

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImg;

        // 中间文本（覆盖在进度条上）
        var centerTextGO = CreateUIObject("CenterText", sliderGO.transform);
        var centerText = centerTextGO.AddComponent<TextMeshProUGUI>();
        centerText.text = "0%";
        centerText.fontSize = 16;
        centerText.color = Color.white;
        centerText.alignment = TextAlignmentOptions.Center;
        centerText.fontStyle = FontStyles.Bold;
        SetAnchors(centerTextGO, Vector2.zero, Vector2.one);

        return (slider, centerText);
    }

    /// <summary>
    /// 创建渐变背景
    /// </summary>
    /// <param name="image">要设置渐变的Image组件</param>
    /// <param name="topColor">顶部颜色</param>
    /// <param name="bottomColor">底部颜色</param>
    public static void CreateGradientBackground(Image image, Color topColor, Color bottomColor)
    {
        // 创建渐变纹理
        var tex = new Texture2D(2, 256);
        var pixels = new Color32[2 * 256];

        for (var y = 0; y < 256; y++)
        {
            var t = y / 255f;
            var color = Color.Lerp(topColor, bottomColor, t);
            for (var x = 0; x < 2; x++)
            {
                pixels[y * 2 + x] = color;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;

        var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 256), new Vector2(0.5f, 0.5f));
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }
}

file class PanelUIHelper
{
    public static GameObject CreateTitleToolbarButton(
        PanelBase panelBase,
        string label,
        Vector2 size,
        Vector2? anchorMin = null,
        Vector2? anchorMax = null,
        Vector2? pivot = null,
        Vector2? sizeDelta = null,
        PanelBase.ButtonColorTheme colorTheme = PanelBase.ButtonColorTheme.Default)
    {
        var parent = panelBase.TitleLeftToolbar.transform;
        var btnObj = PanelUI.CreateUIObject("ToolbarButton_" + label, parent);
        var rect = btnObj.GetComponent<RectTransform>();

        // 在 HorizontalLayoutGroup 中，不需要手动设置锚点
        // 由 LayoutElement 控制大小

        // 默认 LayoutElement 设置（宽度增加，更好的视觉效果）
        var layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.minWidth = 100f;
        layoutElement.preferredWidth = 140f;
        layoutElement.flexibleWidth = 0f;
        layoutElement.minHeight = 36f;
        layoutElement.preferredHeight = 45f;
        layoutElement.flexibleHeight = 0f;

        // 根据颜色主题设置颜色
        var (normalColor, highlightedColor, pressedColor) = GetColorsByTheme(colorTheme);

        var img = btnObj.AddComponent<Image>();

        // 使用UIResourceManager增强按钮样式
        try
        {
            var buttonSprite = colorTheme switch
            {
                PanelBase.ButtonColorTheme.Green => UIResourceManager.GetSprite("success_bg"),
                PanelBase.ButtonColorTheme.Red => UIResourceManager.GetSprite("error_bg"),
                _ => UIResourceManager.GetSprite("square_bg")
            };
            img.sprite = buttonSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        catch
        {
            // 回退到颜色主题
            img.color = normalColor;
        }

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        // 统一设置禁用状态为红色
        colors.disabledColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.targetGraphic = img;

        // 添加按钮边框效果
        var btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = GetOutlineColorByTheme(colorTheme);
        btnOutline.effectDistance = new Vector2(2, 2);

        // 添加按钮阴影效果
        var btnShadow = btnObj.AddComponent<Shadow>();
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnShadow.effectDistance = new Vector2(2, -2);

        var textObj = PanelUI.CreateUIObject("Text", btnObj.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.7f);
        PanelUI.SetAnchors(textObj, Vector2.zero, Vector2.one);

        return btnObj;
    }

    /// <summary>
    /// 根据颜色主题获取按钮颜色
    /// </summary>
    /// <param name="theme">颜色主题</param>
    /// <returns>(normalColor, highlightedColor, pressedColor)</returns>
    private static (Color normalColor, Color highlightedColor, Color pressedColor) GetColorsByTheme(
        PanelBase.ButtonColorTheme theme)
    {
        return theme switch
        {
            PanelBase.ButtonColorTheme.SkyBlue => (
                new Color(0.4f, 0.7f, 1.0f, 1f), // 天蓝色
                new Color(0.5f, 0.8f, 1.0f, 1f), // 高亮
                new Color(0.3f, 0.6f, 0.9f, 1f) // 按下
            ),
            PanelBase.ButtonColorTheme.Default => (
                new Color(0.2f, 0.4f, 0.6f, 1f), // 默认深蓝色
                new Color(0.25f, 0.5f, 0.75f, 1f), // 高亮
                new Color(0.15f, 0.35f, 0.55f, 1f) // 按下
            ),
            PanelBase.ButtonColorTheme.Red => (
                new Color(0.8f, 0.2f, 0.2f, 1f), // 红色
                new Color(0.9f, 0.3f, 0.3f, 1f), // 高亮
                new Color(0.7f, 0.15f, 0.15f, 1f) // 按下
            ),
            PanelBase.ButtonColorTheme.Green => (
                new Color(0.2f, 0.7f, 0.3f, 1f), // 绿色
                new Color(0.3f, 0.8f, 0.4f, 1f), // 高亮
                new Color(0.15f, 0.6f, 0.25f, 1f) // 按下
            ),
            _ => (
                new Color(0.2f, 0.4f, 0.6f, 1f),
                new Color(0.25f, 0.5f, 0.75f, 1f),
                new Color(0.15f, 0.35f, 0.55f, 1f)
            )
        };
    }

    /// <summary>
    /// 根据颜色主题获取边框颜色
    /// </summary>
    /// <param name="theme">颜色主题</param>
    /// <returns>边框颜色</returns>
    private static Color GetOutlineColorByTheme(PanelBase.ButtonColorTheme theme)
    {
        return theme switch
        {
            PanelBase.ButtonColorTheme.Green => new Color(0.2f, 0.7f, 0.3f, 1f),
            PanelBase.ButtonColorTheme.Red => new Color(0.8f, 0.2f, 0.2f, 1f),
            PanelBase.ButtonColorTheme.SkyBlue => new Color(0.4f, 0.7f, 1.0f, 1f),
            _ => new Color(0.3f, 0.3f, 0.35f, 1f)
        };
    }
}

internal class PanelBase : IDisposable
{
    /// <summary>
    /// 预定义的按钮颜色主题
    /// </summary>
    public enum ButtonColorTheme
    {
        /// <summary>天蓝色</summary>
        SkyBlue,

        /// <summary>默认色（深蓝色）</summary>
        Default,

        /// <summary>红色</summary>
        Red,

        /// <summary>绿色</summary>
        Green
    }

    public GameObject CanvasGo { get; set; } = null!;
    public GameObject Root { get; set; } = null!;
    public GameObject ContentPanel { get; set; } = null!;
    public GameObject TitlePanel { get; set; } = null!;
    public TextMeshProUGUI TitleText { get; set; } = null!;
    public GameObject HorizontalPanel { get; set; } = null!;
    public GameObject LeftPanel { get; set; } = null!;
    public VerticalLayoutGroup LeftPanelLayout { get; set; } = null!;
    public GameObject RightPanel { get; set; } = null!;
    public VerticalLayoutGroup RightPanelLayout { get; set; } = null!;
    public GameObject TitleLeftToolbar { get; set; } = null!;
    public GameObject RightToolbar { get; set; } = null!;

    /// <summary>
    /// 创建标准化的 Title Toolbar Button，仅需 label，布局和大小由内部统一设置。
    /// </summary>
    /// <param name="label">按钮文本</param>
    /// <param name="colorTheme">按钮颜色主题，默认为 Default</param>
    /// <returns>Button 组件</returns>
    public (Button btn, TextMeshProUGUI txt) AddTitleToolbarButton(
        string label,
        ButtonColorTheme colorTheme = ButtonColorTheme.Default)
    {
        var buttonObj = PanelUIHelper.CreateTitleToolbarButton(
            this,
            label,
            new Vector2(40f, 40f),
            colorTheme: colorTheme
        );
        var btnComp = buttonObj.GetComponent<Button>();
        var txtComp = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        return (btnComp, txtComp);
    }

    public void SetLeftToolBarText(string text)
    {
        var textObj = PanelUI.CreateUIObject("LeftToolbarText", TitleLeftToolbar.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        PanelUI.SetAnchors(textObj, Vector2.zero, Vector2.one);
    }

    public void Dispose()
    {
        if (CanvasGo != null)
        {
            UnityEngine.Object.Destroy(CanvasGo);
            CanvasGo = null!;
        }
    }
}
