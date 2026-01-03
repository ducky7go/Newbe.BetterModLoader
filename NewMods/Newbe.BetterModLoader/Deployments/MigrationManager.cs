namespace Newbe.BetterModLoader.Deployments;

/// <summary>
/// Handles the one-time migration/enable flow. For now, it's a stub that confirms with user
/// and writes the activation marker. Real migration steps can be added later.
/// </summary>
internal static class MigrationManager
{
    private static GameObject? _modalRoot;
    private static Action? _onConfirm;
    private static Action? _onCancel;

    /// <summary>
    /// Start migration flow: show a confirmation modal, and on confirm, perform migration and write marker.
    /// </summary>
    public static void StartMigration(Action? onConfirm = null, Action? onCancel = null)
    {
        try
        {
            if (_modalRoot != null)
            {
                return; // already showing
            }

            _onConfirm = onConfirm;
            _onCancel = onCancel;

            // Build a very small modal on a dedicated Canvas overlay
            var root = new GameObject("[BML_MigrationModal]");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15000; // 提高到 15000，确保显示在 BetterModManagerCanvas (100) 之上
            canvas.overrideSorting = true;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // 与 RollbackManager 保持一致，避免字体被缩放偏大
            root.AddComponent<GraphicRaycaster>();
            UnityEngine.Object.DontDestroyOnLoad(root);

            // Dim background
            var bg = CreateUIObject("BG", root.transform);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.7f);
            StretchFull(bg);

            // Panel (90% screen coverage)
            var panel = CreateUIObject("Panel", bg.transform);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.18f, 0.98f);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.05f, 0.05f);
            pr.anchorMax = new Vector2(0.95f, 0.95f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            pr.anchoredPosition = Vector2.zero;

            // Title
            var titleGO = CreateUIObject("Title", panel.transform);
            var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
            titleTxt.text = L.Migration.Title;
            titleTxt.fontSize = 36;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.fontStyle = FontStyles.Bold;
            var tr = titleGO.GetComponent<RectTransform>();
            // 将标题靠近顶部（与 RollbackManager 对齐），减少占用高度
            tr.anchorMin = new Vector2(0.05f, 0.88f);
            tr.anchorMax = new Vector2(0.95f, 0.97f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            // Body text (Scrollable with visible scrollbar)
            var bodyScroll = CreateUIObject("BodyScroll", panel.transform);
            var bodyScrollRect = bodyScroll.AddComponent<ScrollRect>();
            bodyScrollRect.horizontal = false;
            bodyScrollRect.vertical = true;
            bodyScrollRect.scrollSensitivity = 3;
            var bodyScrollRT = bodyScroll.GetComponent<RectTransform>();
            // 扩大内容区域高度（与 RollbackManager 对齐）
            bodyScrollRT.anchorMin = new Vector2(0.05f, 0.18f);
            bodyScrollRT.anchorMax = new Vector2(0.95f, 0.85f);
            bodyScrollRT.offsetMin = Vector2.zero;
            bodyScrollRT.offsetMax = Vector2.zero;

            // Viewport (留出右侧空间给滚动条)
            var bodyViewport = CreateUIObject("Viewport", bodyScroll.transform);
            var bodyViewportRT = bodyViewport.GetComponent<RectTransform>();
            bodyViewportRT.anchorMin = new Vector2(0, 0);
            bodyViewportRT.anchorMax = new Vector2(0.95f, 1); // 留出右侧 5% 给滚动条
            bodyViewportRT.offsetMin = Vector2.zero;
            bodyViewportRT.offsetMax = Vector2.zero;
            bodyViewport.AddComponent<RectMask2D>();
            bodyScrollRect.viewport = bodyViewportRT;

            // Content
            var bodyContent = CreateUIObject("Content", bodyViewport.transform);
            var bodyContentRT = bodyContent.GetComponent<RectTransform>();
            bodyContentRT.anchorMin = new Vector2(0, 1);
            bodyContentRT.anchorMax = new Vector2(1, 1);
            bodyContentRT.pivot = new Vector2(0.5f, 1);
            bodyContentRT.offsetMin = Vector2.zero;
            bodyContentRT.offsetMax = Vector2.zero;
            bodyScrollRect.content = bodyContentRT;

            // Text
            var bodyText = bodyContent.AddComponent<TextMeshProUGUI>();
            bodyText.text = L.Migration.BodyFull;
            bodyText.fontSize = 22;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            bodyText.enableWordWrapping = true;

            // Auto-size content vertically
            var contentSizeFitter = bodyContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 创建垂直滚动条
            var scrollbar = CreateUIObject("Scrollbar", bodyScroll.transform);
            var scrollbarRT = scrollbar.GetComponent<RectTransform>();
            scrollbarRT.anchorMin = new Vector2(0.96f, 0);
            scrollbarRT.anchorMax = new Vector2(1, 1);
            scrollbarRT.offsetMin = Vector2.zero;
            scrollbarRT.offsetMax = Vector2.zero;

            var scrollbarImg = scrollbar.AddComponent<Image>();
            scrollbarImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var scrollbarComp = scrollbar.AddComponent<Scrollbar>();
            scrollbarComp.direction = Scrollbar.Direction.BottomToTop;

            // 滚动条滑块区域
            var slidingArea = CreateUIObject("Sliding Area", scrollbar.transform);
            var slidingAreaRT = slidingArea.GetComponent<RectTransform>();
            slidingAreaRT.anchorMin = Vector2.zero;
            slidingAreaRT.anchorMax = Vector2.one;
            slidingAreaRT.offsetMin = new Vector2(2, 2);
            slidingAreaRT.offsetMax = new Vector2(-2, -2);

            // 滚动条滑块
            var handle = CreateUIObject("Handle", slidingArea.transform);
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.anchorMin = Vector2.zero;
            handleRT.anchorMax = Vector2.one;
            handleRT.offsetMin = Vector2.zero;
            handleRT.offsetMax = Vector2.zero;

            var handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.9f);

            scrollbarComp.handleRect = handleRT;
            scrollbarComp.targetGraphic = handleImg;

            // 连接滚动条到 ScrollRect
            bodyScrollRect.verticalScrollbar = scrollbarComp;
            bodyScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // Buttons row
            var okBtn = CreateButton(panel.transform, L.Migration.ButtonConfirm, new Vector2(0.35f, 0.12f),
                new Color(0.2f, 0.7f, 0.3f, 1f));
            var cancelBtn = CreateButton(panel.transform, L.Migration.ButtonCancel, new Vector2(0.65f, 0.12f),
                new Color(0.8f, 0.2f, 0.2f, 1f));

            okBtn.onClick.AddListener(() =>
            {
                try
                {
                    // 执行迁移逻辑（已迁移到 DeploymentManager）
                    DeploymentManager.PerformMigration();
                    ToastManager.ShowSuccess(L.Migration.Success, 5f);
                    try
                    {
                        _onConfirm?.Invoke();
                    }
                    catch
                    {
                    }

                    // 延迟关闭游戏
                    var closeHelper = new GameObject("[BML_QuitHelper]");
                    var quitScript = closeHelper.AddComponent<DelayedQuit>();
                    quitScript.DelaySeconds = 5f;
                }
                catch (Exception ex)
                {
                    Log.ErrorException("[BetterModLoader] Migration exception:", ex);
                    ToastManager.ShowError(L.Migration.FailurePrefix + ex.Message);
                }
                finally
                {
                    Close();
                }
            });

            cancelBtn.onClick.AddListener(() =>
            {
                try
                {
                    _onCancel?.Invoke();
                }
                catch
                {
                }

                Close();
            });

            _modalRoot = root;
        }
        catch (Exception ex)
        {
            Log.ErrorException("[BetterModLoader] Failed to start migration flow", ex);
        }
    }

    private static void Close()
    {
        if (_modalRoot != null)
        {
            try
            {
                UnityEngine.Object.Destroy(_modalRoot);
            }
            catch
            {
            }

            _modalRoot = null;
        }

        _onConfirm = null;
        _onCancel = null;
    }


    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchor, Color baseColor)
    {
        var go = CreateUIObject("Button_" + label, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, 50);

        var img = go.AddComponent<Image>();
        img.color = baseColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.2f);
        btn.colors = colors;

        var textGO = CreateUIObject("Text", go.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 26;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        StretchFull(textGO);

        return btn;
    }
}
