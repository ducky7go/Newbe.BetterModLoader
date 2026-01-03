using System.Collections;
using Duckov.UI.Animations;

namespace Newbe.BetterModLoader.UI;

/// <summary>
/// 在屏幕右半边显示启用/禁用任务进度的面板。
/// 显示加载进度并生成HTML报告作为详细记录。
/// </summary>
internal class ModActivationProgressPanel : MonoBehaviour
{
    /// <summary>
    /// 加载模式枚举，用于区分游戏启动加载和其他场景加载
    /// </summary>
    public enum LoadingMode
    {
        /// <summary>
        /// 游戏启动加载
        /// </summary>
        GameStartup,

        /// <summary>
        /// 其他场景加载
        /// </summary>
        Other
    }

    private static ModActivationProgressPanel? _instance;

    private Canvas? _canvas;

    // 加载视图
    private GameObject? _loadingPanel;
    private RectTransform? _panelRect;
    private RectTransform? _contentRect;

    private GameObject? _closeButton;

    // 顶部标题横幅
    private GameObject? _header;
    private Image? _headerBg;
    private TextMeshProUGUI? _headerText;

    private readonly Dictionary<string, ModItem> _items = new();
    private readonly Dictionary<string, float> _startTimes = new(); // 记录每个模组操作的开始时间
    private BetterModManager.ModOperation[] _jobs = [];
    private int _enableTargetCount = 0;
    private int _totalTarget = 0; // 总任务数（根据场景：全部或仅禁用）
    private int _completedTotal = 0; // 已完成任务数（启用+禁用）

    private class ModItem
    {
        public GameObject Root = null!;
        public Image Preview = null!;
        public Image StatusIndicator = null!; // 覆盖在预览图右下角（占25%面积）
        public TextMeshProUGUI Duration = null!;
        public TextMeshProUGUI TimeLabel = null!; // 左下角实时耗时显示
    }


    private bool _requiresShutdown;
    private LoadingMode _currentLoadingMode = LoadingMode.Other;

    public static void Show(BetterModManager.ModOperation[] jobs, bool requiresShutdownDueToOrderChange,
        LoadingMode loadingMode = LoadingMode.Other)
    {
        EnsureCreated();

        // Respect the developer force restart toggle setting
        var shouldForceRestart = Configs.ShouldForceRestartOnOrderChanges();
        Log.Info("ShouldForceRestartOnOrderChanges: " + shouldForceRestart);
        _instance!._requiresShutdown = requiresShutdownDueToOrderChange && shouldForceRestart;

        Log.Debug(
            $"[ModActivationProgressPanel] OrderChanged: {requiresShutdownDueToOrderChange}, ForceRestartToggle: {shouldForceRestart}, FinalRequiresShutdown: {_instance!._requiresShutdown}");

        _instance!._currentLoadingMode = loadingMode;
        _instance!.ShowInternal(jobs);
    }

    private static void EnsureCreated()
    {
        if (_instance != null) return;

        var go = new GameObject("[ModActivationProgressPanel]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ModActivationProgressPanel>();
        _instance.BuildUI();
    }

    private void OnEnable()
    {
        BetterModManager.ModEnabling += OnModEnabling;
        BetterModManager.ModEnabled += OnModEnabled;
        BetterModManager.ModDisabling += OnModDisabling;
        BetterModManager.ModDisabled += OnModDisabled;
        BetterModManager.AllModJobsCompleted += OnAllModJobsCompleted;
    }

    private void OnDisable()
    {
        BetterModManager.ModEnabling -= OnModEnabling;
        BetterModManager.ModEnabled -= OnModEnabled;
        BetterModManager.ModDisabling -= OnModDisabling;
        BetterModManager.ModDisabled -= OnModDisabled;
        BetterModManager.AllModJobsCompleted -= OnAllModJobsCompleted;
    }

    private void Update()
    {
        // 实时更新正在进行的操作的耗时显示
        foreach (var kvp in _startTimes)
        {
            if (_items.TryGetValue(kvp.Key, out var item))
            {
                float elapsedSeconds = Time.time - kvp.Value;
                // 显示为秒，最多一位小数，少于1秒显示为0
                float displaySeconds = elapsedSeconds < 1f ? 0f : elapsedSeconds;
                item.TimeLabel.text = $"{displaySeconds:F1}s";
            }
        }
    }

    private void BuildUI()
    {
        // 直接在 gameObject 上添加 Canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 120;
        _canvas.overrideSorting = true;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        // 创建主加载面板
        _loadingPanel = new GameObject("LoadingPanel");
        _loadingPanel.transform.SetParent(transform, false);
        _panelRect = _loadingPanel.AddComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(1f, 0);
        _panelRect.anchorMax = new Vector2(1f, 1f);
        _panelRect.pivot = new Vector2(0f, 0.5f);
        _panelRect.sizeDelta = new Vector2(960, 0);
        _panelRect.anchoredPosition = new Vector2(0, 0);
        var bg = _loadingPanel.AddComponent<Image>();

        // 添加渐变背景效果 - 使用游戏配置的颜色
        CreateGradientBackground(bg,
            GameUIConfig.Colors.PanelBackground,
            new Color(0.18f, 0.18f, 0.22f, 0.98f));

        // 添加边框效果 - 使用 UIBuilderHelper
        UIBuilderHelper.AddOutline(_loadingPanel, GameUIConfig.Colors.PanelBorder, new Vector2(2, 2));

        // 左上角关闭按钮（初始隐藏）
        _closeButton = CreateCloseButton(_loadingPanel.transform);
        _closeButton.SetActive(false);

        // 顶部标题横幅（带渐变背景和圆角效果）
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(_loadingPanel.transform, false);
        var headerRect = headerGO.AddComponent<RectTransform>();
        // 横幅居中，仅占面板宽度的50%
        headerRect.anchorMin = new Vector2(0.15f, 0.94f);
        headerRect.anchorMax = new Vector2(0.85f, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        var headerBg = headerGO.AddComponent<Image>();
        CreateGradientBackground(headerBg,
            GameUIConfig.Colors.HeaderBackground,
            new Color(0.15f, 0.15f, 0.20f, 0.9f));

        // 添加圆角效果
        var corners = headerBg.GetComponent<RectTransform>();
        corners.sizeDelta = new Vector2(0, -8f); // 给横幅一些内边距

        var headerTextGO = new GameObject("Text");
        headerTextGO.transform.SetParent(headerGO.transform, false);
        var headerTextRect = headerTextGO.AddComponent<RectTransform>();
        headerTextRect.anchorMin = new Vector2(0, 0);
        headerTextRect.anchorMax = new Vector2(1, 1);
        headerTextRect.offsetMin = new Vector2(15, 4);
        headerTextRect.offsetMax = new Vector2(-15, -4);

        var headerText = headerTextGO.AddComponent<TextMeshProUGUI>();
        headerText.text = L.ModActivation.Header;
        headerText.fontSize = 32;
        headerText.color = GameUIConfig.Colors.HeaderText;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontStyle = FontStyles.Bold;

        // 添加文字阴影效果
        headerText.outlineWidth = 0.3f;
        headerText.outlineColor = GameUIConfig.Effects.OutlineColor;

        _header = headerGO;
        _headerBg = headerBg;
        _headerText = headerText;

        // 内容区域（Grid 布局）
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(_loadingPanel.transform, false);
        var scrollRect = scrollGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.02f, 0.15f);
        scrollRect.anchorMax = new Vector2(0.98f, 0.92f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        // 创建基础的 ScrollRect，但保留自定义设置
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 3;

        // 创建 viewport 和 content（保留原有的创建方式，因为需要特殊设置）
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        UIBuilderHelper.SetStretchFull(viewportRect);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewportRect;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        _contentRect = content.AddComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0, 1);
        _contentRect.anchorMax = new Vector2(1, 1);
        _contentRect.pivot = new Vector2(0.5f, 1);
        _contentRect.sizeDelta = new Vector2(0, 0);
        scroll.content = _contentRect;

        // 使用 UIBuilderHelper 创建网格布局
        var gridLayout = UIBuilderHelper.CreateGridLayout(
            content.transform,
            new Vector2(120, 120), // 紧凑卡片尺寸（64 预览 + 单行耗时）
            new Vector2(8, 8),
            new Vector2(12, 12),
            GridLayoutGroup.Corner.UpperLeft,
            GridLayoutGroup.Axis.Horizontal
        );

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;


        _loadingPanel.SetActive(false);
    }

    private GameObject CreateCloseButton(Transform parent)
    {
        var btnGO = new GameObject("CloseButton");
        btnGO.transform.SetParent(parent, false);
        var btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0, 1);
        btnRect.anchorMax = new Vector2(0, 1);
        btnRect.pivot = new Vector2(0, 1);
        btnRect.anchoredPosition = new Vector2(20, -20);
        btnRect.sizeDelta = new Vector2(50, 50); // 改为正方形以适应图标

        // 使用UIResourceManager获取按钮资源
        var btnImg = btnGO.AddComponent<Image>();
        var closeSprite = UIResourceManager.GetSprite("close_normal");
        btnImg.sprite = closeSprite;
        btnImg.type = Image.Type.Sliced;
        btnImg.color = Color.white;

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(OnCloseButtonClicked);

        // 使用内置的颜色过渡效果
        var colors = btn.colors;
        colors.normalColor = GameUIConfig.Colors.ButtonClose;
        colors.highlightedColor = GameUIConfig.Colors.ButtonCloseHighlight;
        colors.pressedColor = GameUIConfig.Colors.ButtonClosePressed;
        btn.colors = colors;

        return btnGO;
    }

    private Sprite CreateXIcon()
    {
        // 创建一个简单的 X 图标
        var tex = new Texture2D(64, 64);
        var pixels = new Color32[64 * 64];

        // 清空背景（透明）
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 画 X 形状的线条（厚度为3像素）
        for (var i = 8; i < 56; i++)
        {
            // 主对角线
            SetPixelSafe(pixels, i, i, Color.white, 64);
            SetPixelSafe(pixels, i + 1, i, Color.white, 64);
            SetPixelSafe(pixels, i, i + 1, Color.white, 64);

            // 副对角线
            SetPixelSafe(pixels, 63 - i, i, Color.white, 64);
            SetPixelSafe(pixels, 62 - i, i, Color.white, 64);
            SetPixelSafe(pixels, 63 - i, i + 1, Color.white, 64);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    private void SetPixelSafe(Color32[] pixels, int x, int y, Color color, int size)
    {
        if (x >= 0 && x < size && y >= 0 && y < size)
        {
            pixels[y * size + x] = color;
        }
    }

    private void CreateGradientBackground(Image image, Color topColor, Color bottomColor)
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

    // 顶部横幅状态更新
    private void UpdateHeaderProgress()
    {
        if (_headerBg != null)
        {
            _headerBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        }

        if (_headerText != null)
        {
            _headerText.color = new Color(0.95f, 0.95f, 1f, 1f);
            _headerText.text = $"{L.ModActivation.Loading} {_completedTotal}/{_totalTarget}";
        }
    }

    private void SetHeaderNeutralText(string text)
    {
        if (_headerBg != null) _headerBg.color = GameUIConfig.Colors.HeaderBackground;
        if (_headerText != null)
        {
            _headerText.color = GameUIConfig.Colors.HeaderText;
            _headerText.text = text;
        }
    }

    private void SetHeaderFailure()
    {
        if (_headerBg != null) _headerBg.color = GameUIConfig.Colors.ErrorRed;
        if (_headerText != null)
        {
            _headerText.color = Color.white;
            _headerText.text = L.ModActivation.FailureHeader;
        }
    }

    private void SetHeaderRestart()
    {
        if (_headerBg != null) _headerBg.color = Color.black;
        if (_headerText != null)
        {
            _headerText.color = Color.white;
            _headerText.text = L.ModActivation.RestartRequired;
        }
    }

    private void OnCloseButtonClicked()
    {
        SlideOut().Forget();
    }

    private void ShowInternal(BetterModManager.ModOperation[] jobs)
    {
        _jobs = jobs ?? [];
        _completedTotal = 0;
        _totalTarget = _requiresShutdown
            ? _jobs.Count(j => j.Action == BetterModManager.ModActivationAction.Disable)
            : _jobs.Length;

        BuildModItems();
        UpdateHeaderProgress();
        if (_loadingPanel != null) _loadingPanel.SetActive(true);

        // 启动滑入动画，然后执行任务
        SlideInAndRun().Forget();
    }

    private async UniTask SlideInAndRun()
    {
        // 滑入动画：从右侧滑入
        if (_panelRect != null)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0, 0); // 在屏幕外右侧
            Vector2 endPos = new Vector2(-960, 0); // 滑到左边，覆盖右半屏

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                _panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            }

            _panelRect.anchoredPosition = endPos;
        }

        // 执行任务
        await RunJobs();
    }

    private async UniTask SlideOut()
    {
        // 滑出动画：向右侧滑出
        if (_panelRect != null)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector2 startPos = _panelRect.anchoredPosition;
            Vector2 endPos = new Vector2(0, 0); // 滑到屏幕外右侧

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                _panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                await UniTask.Yield();
            }

            _panelRect.anchoredPosition = endPos;
        }

        if (_loadingPanel != null) _loadingPanel.SetActive(false);
    }

    private void OnAllModJobsCompleted(IReadOnlyList<BetterModManager.ModOperationResult> results)
    {
        Log.Debug($"[ModActivationProgressPanel] OnAllModJobsCompleted called with {results.Count} results");

        // Play completion sound for game startup mode (regardless of success/failure)
        if (_currentLoadingMode == LoadingMode.GameStartup)
        {
            AutoInto();
        }

        // 统计成功/失败
        int successCount = results.Count(r => r.Success);
        int failCount = results.Count(r => !r.Success);

        Log.Debug($"[ModActivationProgressPanel] Success: {successCount}, Failed: {failCount}");

        // 如果有失败的任务，保持加载面板可见，显示关闭按钮
        if (failCount > 0)
        {
            Log.Debug("[ModActivationProgressPanel] Has failures, keeping loading panel visible");
            if (_closeButton != null) _closeButton.SetActive(true);
            // 更新标题为失败状态
            SetHeaderFailure();
            return;
        }

        // 成功情况下，立即滑出加载面板（不显示统计面板）
        SlideOut().Forget();

        Log.Debug("[ModActivationProgressPanel] Stats view built successfully");
    }

    /// <summary>
    /// Plays startup completion sound when mod loading finishes in GameStartup mode
    /// </summary>
    private void AutoInto()
    {
        try
        {
            StartCoroutine(AutoClickToContinue());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ModActivationProgressPanel] Failed to play startup completion sound");
        }
    }

    /// <summary>
    /// 延迟一帧后自动点击 MainTitle 以继续游戏
    /// </summary>
    private IEnumerator AutoClickToContinue()
    {
        yield return null;

        // 使用与 AnyKeySkip 相同的方法查找 MainTitle
        var mainTitleGameObject = GameObject.Find("Canvas/MainTitle");

        if (mainTitleGameObject != null)
        {
            Log.Debug($"[ModActivationProgressPanel] Found MainTitle GameObject: {mainTitleGameObject.name}");

            // 使用反射获取 Title 组件
            var titleComponent = (Title)mainTitleGameObject.GetComponent(typeof(Title));
            if (titleComponent is not null)
            {
                // 使用已有的反射扩展方法获取 fadeGroup
                var fadeGroup = titleComponent.GetField<Title, FadeGroup>("fadeGroup");
                {
                    var methodInfo = typeof(Title).GetMethod("ContinueTask",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                    var isShown = fadeGroup.IsShown;

                    if (isShown)
                    {
                        Log.Debug("[ModActivationProgressPanel] MainTitle is shown, triggering ContinueTask");

                        // 使用反射调用 ContinueTask() 方法
                        ((UniTask)methodInfo.Invoke(titleComponent, null)).Forget();
                        Log.Debug("[ModActivationProgressPanel] Successfully triggered MainTitle.ContinueTask()");
                    }
                    else
                    {
                        Log.Debug("[ModActivationProgressPanel] MainTitle fadeGroup is not shown, waiting...");
                        // 如果还没有显示，等待一帧再试
                        yield return new WaitForSeconds(0.1f);
                        var isShownNow = fadeGroup.IsShown;
                        if (isShownNow)
                        {
                            ((UniTask)methodInfo.Invoke(titleComponent, null)).Forget();
                            Log.Debug(
                                "[ModActivationProgressPanel] Successfully triggered MainTitle.ContinueTask() after delay");
                        }
                        else
                        {
                            Log.Warn(
                                "[ModActivationProgressPanel] MainTitle still not shown, using fallback click");
                        }
                    }
                }
            }
            else
            {
                Log.Warn("[ModActivationProgressPanel] MainTitle GameObject found but no Title component");
            }
        }
        else
        {
            Log.Debug("[ModActivationProgressPanel] MainTitle GameObject not found, using fallback click");
        }
    }


    private float GetJobDuration(BetterModManager.ModOperationResult r)
    {
        // 直接从 JobResult 获取耗时
        return (float)r.DurationMs;
    }

    private async UniTask RunJobs()
    {
        if (_requiresShutdown)
        {
            // 仅执行禁用任务，然后提示即将退出并保持面板
            var disableJobs = _jobs.Where(j => j.Action == BetterModManager.ModActivationAction.Disable)
                .OrderBy(j => j.Order)
                .ToArray();
            if (disableJobs.Length > 0)
            {
                foreach (var job in disableJobs)
                {
                    BetterModManager.ProcessModOperation(job);
                    await UniTask.Yield();
                }
            }

            // 顶部显示重启提示
            SetHeaderRestart();
            var go = new GameObject("[BML_Quit]");
            var dq = go.AddComponent<DelayedQuit>();
            dq.DelaySeconds = 3f;
            DontDestroyOnLoad(go);
        }
        else if (_jobs.Length > 0)
        {
            // 逐个执行任务并收集结果
            foreach (var result in BetterModManager.Instance.ProcessModOperations(_jobs))
            {
                await UniTask.Yield();
            }
        }
        else
        {
            // 无任务：顶部显示后自动关闭
            SetHeaderNeutralText(L.ModActivation.NoJobs);
            await UniTask.Delay(2000);
            await SlideOut();
        }
    }


    private void BuildModItems()
    {
        if (_contentRect == null) return;

        // 清空
        for (int i = _contentRect.childCount - 1; i >= 0; i--)
        {
            var child = _contentRect.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }

        _items.Clear();

        if (_jobs.Length == 0)
        {
            // 空状态卡片（无预览，仅提示）
            var dummyInfo = new ModInfo2();
            dummyInfo.Id = "empty";
            var card = CreateModCard(dummyInfo, out var item);
            item.Duration.text = L.ModActivation.NoJobs;
            item.Duration.color = GameUIConfig.Colors.MutedText;
            item.Preview.color = new Color(0.35f, 0.4f, 0.45f, 0.6f);
            return;
        }

        // 创建每个 mod 的卡片
        foreach (var job in _jobs)
        {
            var cardGO = CreateModCard(job.Info, out var item);
            _items[job.Info.Id] = item;
        }
    }

    private GameObject CreateModCard(ModInfo2 info, out ModItem item)
    {
        var id = info.Id;
        var cardGO = new GameObject("ModCard_" + id);
        cardGO.transform.SetParent(_contentRect, false);

        // 卡片背景 - 添加渐变效果
        var cardImg = cardGO.AddComponent<Image>();
        CreateGradientBackground(cardImg,
            GameUIConfig.Colors.PanelBackground,
            new Color(0.18f, 0.18f, 0.20f, 1f));

        // 垂直布局：预览图（带角标） + 文本
        var v = cardGO.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.padding = new RectOffset(8, 8, 8, 8);
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        v.childAlignment = TextAnchor.UpperCenter;

        // 预览图容器
        const float previewSize = 64f;
        var previewGO = new GameObject("Preview");
        previewGO.transform.SetParent(cardGO.transform, false);
        var previewRect = previewGO.AddComponent<RectTransform>();
        previewRect.sizeDelta = new Vector2(previewSize, previewSize);
        var preview = previewGO.AddComponent<Image>();
        preview.raycastTarget = false;
        preview.type = Image.Type.Simple;
        preview.preserveAspect = true; // 居中并保持比例显示
        var tex = info.GetPreview();
        if (tex != null)
        {
            // 与管理器列表一致，使用 Sprite.Create 包裹 Texture2D
            preview.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            preview.color = Color.white;
        }
        else
        {
            // 占位
            preview.color = new Color(0.35f, 0.4f, 0.45f, 1f);
        }

        // 右下角状态角标，占预览图面积的 25%（宽高各 50%）
        var indicatorGO = new GameObject("Indicator");
        indicatorGO.transform.SetParent(previewGO.transform, false);
        var indicatorRect = indicatorGO.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(1f, 0f);
        indicatorRect.anchorMax = new Vector2(1f, 0f);
        indicatorRect.pivot = new Vector2(1f, 0f);
        indicatorRect.sizeDelta = new Vector2(previewSize * 0.5f, previewSize * 0.5f);
        indicatorRect.anchoredPosition = Vector2.zero;
        var indicator = indicatorGO.AddComponent<Image>();
        indicator.color = new Color(0.7f, 0.7f, 0.7f, 0.9f); // 默认灰色

        // 添加阴影效果
        UIBuilderHelper.AddShadow(indicatorGO, GameUIConfig.Effects.ShadowColor, new Vector2(2, -2));

        // 为状态指示器添加图标背景（使用小号的圆点图标）
        var statusIcon = UIResourceManager.GetSprite("check_success");
        indicator.sprite = statusIcon;
        indicator.type = Image.Type.Sliced;
        indicator.color = new Color(0.7f, 0.7f, 0.7f, 0.9f); // 默认灰色

        // 左下角实时耗时标签（容器 + 背景）- 扩大为两倍
        var timeLabelContainerGO = new GameObject("TimeLabelContainer");
        timeLabelContainerGO.transform.SetParent(previewGO.transform, false);
        var timeLabelContainerRect = timeLabelContainerGO.AddComponent<RectTransform>();
        timeLabelContainerRect.anchorMin = new Vector2(0f, 0f);
        timeLabelContainerRect.anchorMax = new Vector2(0f, 0f);
        timeLabelContainerRect.pivot = new Vector2(0f, 0f);
        timeLabelContainerRect.sizeDelta =
            new Vector2(previewSize * 1.0f, previewSize * 0.6f); // 从0.7和0.3扩大到1.4和0.6（两倍）
        timeLabelContainerRect.anchoredPosition = Vector2.zero;

        // 添加黑色背景
        var timeLabelBg = timeLabelContainerGO.AddComponent<Image>();
        timeLabelBg.color = new Color(0f, 0f, 0f, 0.5f); // 半透明黑色背景

        // 文本子对象
        var timeLabelTextGO = new GameObject("Text");
        timeLabelTextGO.transform.SetParent(timeLabelContainerGO.transform, false);
        var timeLabelTextRect = timeLabelTextGO.AddComponent<RectTransform>();
        timeLabelTextRect.anchorMin = Vector2.zero;
        timeLabelTextRect.anchorMax = Vector2.one;
        timeLabelTextRect.offsetMin = new Vector2(2, 0); // 左边留2px边距
        timeLabelTextRect.offsetMax = new Vector2(-2, 0); // 右边留2px边距

        var timeLabelTmp = timeLabelTextGO.AddComponent<TextMeshProUGUI>();
        timeLabelTmp.text = "";
        timeLabelTmp.fontSize = 20; // 从11扩大到16，与容器大小匹配
        timeLabelTmp.color = new Color(1f, 1f, 1f, 1f);
        timeLabelTmp.alignment = TextAlignmentOptions.BottomLeft;
        timeLabelTmp.fontStyle = FontStyles.Bold;
        timeLabelTmp.enableWordWrapping = false;
        timeLabelTmp.overflowMode = TextOverflowModes.Overflow;

        // 耗时
        var durGO = new GameObject("Duration");
        durGO.transform.SetParent(cardGO.transform, false);
        var durTmp = durGO.AddComponent<TextMeshProUGUI>();
        durTmp.text = "";
        durTmp.fontSize = 14;
        durTmp.color = GameUIConfig.Colors.NormalText;
        durTmp.alignment = TextAlignmentOptions.Center;
        durTmp.enableWordWrapping = false;
        durTmp.overflowMode = TextOverflowModes.Ellipsis;

        item = new ModItem
        {
            Root = cardGO,
            Preview = preview,
            StatusIndicator = indicator,
            Duration = durTmp,
            TimeLabel = timeLabelTmp
        };
        return cardGO;
    }

    private void OnModEnabling(ModInfo2 info)
    {
        if (_items.TryGetValue(info.Id, out var item))
        {
            // 黄色带发光效果：加载中
            var loadingColor = GameUIConfig.Colors.LoadingOrange;
            item.StatusIndicator.color = loadingColor;

            // 记录开始时间
            _startTimes[info.Id] = Time.time;
        }
    }

    private void OnModEnabled(BetterModManager.ModEnableCompletedEventArgs args)
    {
        if (_items.TryGetValue(args.Info.Id, out var item))
        {
            // 停止实时更新（移除开始时间记录）
            _startTimes.Remove(args.Info.Id);

            // 显示最终耗时
            if (args.DurationMs > 0)
            {
                float finalSeconds = (float)args.DurationMs / 1000f;
                float displaySeconds = finalSeconds < 1f ? 0f : finalSeconds;
                item.TimeLabel.text = $"{displaySeconds:F1}s";
            }

            if (args.Outcome == BetterModManager.ModEnableOutcome.Success)
            {
                // 绿色带发光效果：成功
                item.StatusIndicator.color = GameUIConfig.Colors.SuccessGreen;
            }
            else
            {
                // 红色带发光效果：失败
                item.StatusIndicator.color = GameUIConfig.Colors.ErrorRed;

                // 在耗时行追加失败标记（名称已移除）
                var reason = args.FailureReason?.ToString() ?? L.ModActivation.ReasonUnknown;
                if (args.DurationMs > 0)
                {
                    item.Duration.text = $"{args.DurationMs:F0} ms · {L.ModActivation.ReasonFailurePrefix}{reason}";
                }
                else
                {
                    item.Duration.text = $"{L.ModActivation.ReasonFailurePrefix}{reason}";
                }
            }

            if (args.DurationMs > 0)
            {
                // 如果之前未覆盖（失败场景已经设置），则显示耗时
                if (string.IsNullOrEmpty(item.Duration.text) || !item.Duration.text.Contains("ms"))
                {
                    item.Duration.text = $"{args.DurationMs:F0} ms";
                }
            }
        }

        _completedTotal = Mathf.Min(_completedTotal + 1, _totalTarget);
        UpdateHeaderProgress();
    }

    private void OnModDisabling(ModInfo2 info)
    {
        if (_items.TryGetValue(info.Id, out var item))
        {
            // 橙色带发光效果：禁用中
            var disablingColor = GameUIConfig.Colors.WarningYellow;
            item.StatusIndicator.color = disablingColor;

            // 记录开始时间
            _startTimes[info.Id] = Time.time;
        }
    }

    private void OnModDisabled(BetterModManager.ModDisableCompletedEventArgs args)
    {
        if (_items.TryGetValue(args.Info.Id, out var item))
        {
            // 停止实时更新（移除开始时间记录）
            _startTimes.Remove(args.Info.Id);

            // 显示最终耗时
            if (args.DurationMs > 0)
            {
                float finalSeconds = (float)args.DurationMs / 1000f;
                float displaySeconds = finalSeconds < 1f ? 0f : finalSeconds;
                item.TimeLabel.text = $"{displaySeconds:F1}s";
            }

            if (args.Outcome == BetterModManager.ModDisableOutcome.Success)
            {
                // 蓝色带发光效果：已禁用
                item.StatusIndicator.color = GameUIConfig.Colors.DisabledBlue;
            }
            else
            {
                // 深红色带发光效果：禁用失败
                item.StatusIndicator.color = GameUIConfig.Colors.ErrorRed;

                // 显示禁用失败原因到耗时行（名称已移除）
                string reason = args.FailureReason switch
                {
                    BetterModManager.ModDisableFailureReason.NotActive => L.ModActivation.ReasonNotActive,
                    BetterModManager.ModDisableFailureReason.Exception => L.ModActivation.ReasonException,
                    _ => L.ModActivation.ReasonUnknown
                };
                if (args.DurationMs > 0)
                {
                    item.Duration.text =
                        $"{args.DurationMs:F0} ms · {L.ModActivation.ReasonDisableFailurePrefix}{reason}";
                }
                else
                {
                    item.Duration.text = $"{L.ModActivation.ReasonDisableFailurePrefix}{reason}";
                }

                Log.Warn($"[BetterModLoader] Disable failed for '{args.Info.Id}': {reason}");
            }

            if (args.DurationMs > 0)
            {
                if (string.IsNullOrEmpty(item.Duration.text) || !item.Duration.text.Contains("ms"))
                {
                    item.Duration.text = $"{args.DurationMs:F0} ms";
                }
            }
        }

        _completedTotal = Mathf.Min(_completedTotal + 1, _totalTarget);
        UpdateHeaderProgress();
    }
}
