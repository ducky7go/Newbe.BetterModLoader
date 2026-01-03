namespace Newbe.BetterModLoader.Deployments;

/// <summary>
/// 管理从新版模组管理器回退到旧版的流程
/// </summary>
internal static class RollbackManager
{
    /// <summary>
    /// 显示返回旧版确认弹窗
    /// </summary>
    public static void ShowRevertConfirmation(Action onConfirm, Action onCancel)
    {
        // 创建 Canvas (使用与 MigrationManager 相同的 sortingOrder)
        var canvasObj = new GameObject("[BML_RollbackModal]");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15000; // 与 MigrationManager 一致，确保显示在 BetterModManagerCanvas (100) 之上
        canvas.overrideSorting = true;
        UnityEngine.Object.DontDestroyOnLoad(canvasObj);

        var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 创建半透明背景 (全屏覆盖)
        var bgObj = CreateUIObject("Background", canvasObj.transform);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);

        Log.Debug("[BetterModLoader] Rollback modal background created with full screen coverage");

        // 创建弹窗面板 (大尺寸，占据屏幕大部分区域)
        var panelObj = CreateUIObject("Panel", bgObj.transform);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.05f); // 距离屏幕边缘 5%
        panelRect.anchorMax = new Vector2(0.95f, 0.95f); // 占据屏幕中间 90% 区域
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.15f, 0.15f, 0.18f, 0.98f);

        Log.Debug("[BetterModLoader] Rollback modal panel created with size covering 90% of screen");

        // 创建标题
        var titleObj = CreateUIObject("Title", panelObj.transform);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.88f);
        titleRect.anchorMax = new Vector2(0.95f, 0.97f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = L.Rollback.Title;
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // 创建可滚动内容区域
        var scrollObj = CreateUIObject("BodyScroll", panelObj.transform);
        var scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.18f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scrollView = scrollObj.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        scrollView.scrollSensitivity = 3;

        // Viewport (留出右侧空间给滚动条)
        var viewportObj = CreateUIObject("Viewport", scrollObj.transform);
        var viewportRect = viewportObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0, 0);
        viewportRect.anchorMax = new Vector2(0.95f, 1); // 留出右侧 5% 给滚动条
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportObj.AddComponent<RectMask2D>();
        scrollView.viewport = viewportRect;

        // Content
        var contentObj = CreateUIObject("Content", viewportObj.transform);
        var contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        scrollView.content = contentRect;

        // 添加内容文本
        var contentText = contentObj.AddComponent<TextMeshProUGUI>();
        contentText.text = L.Rollback.BodyFull;
        contentText.fontSize = 22; // 与 MigrationManager 一致
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.color = new Color(0.9f, 0.9f, 0.9f, 1f); // 与 MigrationManager 一致

        var contentSizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 创建滚动条
        var scrollbarObj = CreateUIObject("Scrollbar", scrollObj.transform);
        var scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(0.96f, 0);
        scrollbarRect.anchorMax = new Vector2(1f, 1);
        scrollbarRect.pivot = new Vector2(0.5f, 0.5f);
        scrollbarRect.offsetMin = Vector2.zero;
        scrollbarRect.offsetMax = Vector2.zero;

        var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollView.verticalScrollbar = scrollbar;
        scrollView.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        var scrollbarBg = scrollbarObj.AddComponent<Image>();
        scrollbarBg.color = new Color(0.2f, 0.2f, 0.22f, 0.8f);

        var handleArea = CreateUIObject("Sliding Area", scrollbarObj.transform);
        SetAnchors(handleArea, Vector2.zero, Vector2.one);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.offsetMin = new Vector2(2, 2);
        handleAreaRect.offsetMax = new Vector2(-2, -2);

        var handleObj = CreateUIObject("Handle", handleArea.transform);
        SetAnchors(handleObj, Vector2.zero, Vector2.one);
        scrollbar.handleRect = handleObj.GetComponent<RectTransform>();

        var handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.5f, 0.5f, 0.55f, 1f);
        scrollbar.targetGraphic = handleImg;

        // 创建按钮 (使用与 MigrationManager 一致的样式)
        var confirmBtn = CreateSimpleButton(panelObj.transform, L.Rollback.ButtonConfirm, new Vector2(0.35f, 0.12f),
            new Color(0.2f, 0.7f, 0.3f, 1f));
        confirmBtn.onClick.AddListener(() =>
        {
            UnityEngine.Object.Destroy(canvasObj);
            DeploymentManager.PerformRollback();
            ToastManager.ShowSuccess(L.Migration.Success, 5f);
            try
            {
                onConfirm?.Invoke();
            }
            catch
            {
            }

            // 延迟关闭游戏
            var closeHelper = new GameObject("[BML_QuitHelper]");
            var quitScript = closeHelper.AddComponent<DelayedQuit>();
            quitScript.DelaySeconds = 5f;
        });

        var cancelBtn = CreateSimpleButton(panelObj.transform, L.Rollback.ButtonCancel, new Vector2(0.65f, 0.12f),
            new Color(0.8f, 0.2f, 0.2f, 1f));
        cancelBtn.onClick.AddListener(() =>
        {
            UnityEngine.Object.Destroy(canvasObj);
            onCancel?.Invoke();
        });
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void SetAnchors(GameObject obj, Vector2 min, Vector2 max)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Button CreateSimpleButton(Transform parent, string label, Vector2 anchor, Color baseColor)
    {
        var btnObj = CreateUIObject("Button_" + label, parent);
        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, 50);

        var img = btnObj.AddComponent<Image>();
        img.color = baseColor;

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.2f);
        btn.colors = colors;

        var textObj = CreateUIObject("Text", btnObj.transform);
        SetAnchors(textObj, Vector2.zero, Vector2.one);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26; // 与 MigrationManager 一致
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
