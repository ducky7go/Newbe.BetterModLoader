namespace Newbe.BetterModLoader.DeveloperModes;

internal static class LocalModPanelLayout
{
    private static GameObject? _modalRoot;

    public static void Show()
    {
        LocalModPanelController controller = null!;
        const float labelFontSize = 18f;
        if (_modalRoot != null)
        {
            return;
        }

        // 使用 PanelUI 创建现代化的模态面板，指定适中的 sortingOrder
        var panelBase = PanelUI.CreatePanelBase(L.Get(LK.Mod.TitleDeveloperMode), 500);

        // 添加返回按钮到标题栏
        var (backBtn, backBtnText) =
            panelBase.AddTitleToolbarButton(L.Get(LK.UI.Back), PanelBase.ButtonColorTheme.SkyBlue);

        // 添加上传按钮到标题栏
        var (btnUpload, btnUploadText) =
            panelBase.AddTitleToolbarButton(L.Get(LK.Mod.ButtonUpload), PanelBase.ButtonColorTheme.Green);

        // 左侧列表面板 - 使用 PanelUI 的 LeftPanel（已包含垂直布局）
        var listPanel = panelBase.LeftPanel;

        // 添加列表标题到左侧面板
        var listTitleText = PanelUI.CreateText(listPanel, L.Get(LK.Mod.LocalModListTitle), 16);
        listTitleText.fontStyle = FontStyles.Bold;
        listTitleText.alignment = TextAlignmentOptions.Center;
        var listTitleLE = listTitleText.gameObject.AddComponent<LayoutElement>();
        listTitleLE.preferredHeight = 36f;
        listTitleLE.flexibleHeight = 0f;

        // 创建标准滚动容器（参考 ModDataCheckView.CreateContainer 的实现方式）
        var listContent = CreateScrollContainer(listPanel);
        var listScroll = listContent.transform.parent.gameObject;
        var listSR = listScroll.GetComponent<ScrollRect>();

        // 创建现代化列表项预制体（不添加到场景中，仅作为模板）
        var itemPrefab = PanelUI.CreateUIObject("LocalModListItem_Prefab", null);
        var itemImg = itemPrefab.AddComponent<Image>();
        // 使整个项背景透明，视觉上只用左侧颜色条作为标记
        itemImg.color = new Color(0f, 0f, 0f, 0f);
        var itemLayout = itemPrefab.AddComponent<LayoutElement>();
        itemLayout.preferredHeight = 60;

        // 使用水平布局组织内容：图片 + 名称（无间距，完全贴近边缘）
        var itemContainerHL = itemPrefab.AddComponent<HorizontalLayoutGroup>();
        itemContainerHL.childAlignment = TextAnchor.MiddleLeft;
        itemContainerHL.spacing = 2f; // 进一步减小间距
        itemContainerHL.padding = new RectOffset(0, 8, 6, 6); // 完全移除左边距
        itemContainerHL.childControlWidth = false; // 改回 false，不控制子元素宽度
        itemContainerHL.childForceExpandWidth = false;
        itemContainerHL.childControlHeight = true;
        itemContainerHL.childForceExpandHeight = true;

        // Preview image - 第一个元素，直接贴左
        var previewObj = PanelUI.CreateUIObject("Preview", itemPrefab.transform);
        var previewLE = previewObj.AddComponent<LayoutElement>();
        previewLE.preferredWidth = 48f;
        previewLE.preferredHeight = 48f;
        previewLE.minWidth = 48f;
        previewLE.minHeight = 48f;
        previewLE.flexibleWidth = 0f; // 不允许弹性宽度
        var previewImage = previewObj.AddComponent<Image>();
        previewImage.raycastTarget = false;
        previewImage.type = Image.Type.Simple;
        previewImage.preserveAspect = true;
        previewImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // DisplayName text - 直接与图片在同一行
        var nameTextGO = PanelUI.CreateUIObject("NameText", itemPrefab.transform);
        var nameTMP = nameTextGO.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 16;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
        nameTMP.overflowMode = TextOverflowModes.Ellipsis;
        var nameLE = nameTextGO.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1f; // 占用剩余空间
        nameLE.preferredHeight = 20f;

        var lmUI = itemPrefab.AddComponent<LocalModListItemUI>();
        // IdText 已移除，不再设置
        lmUI.NameText = nameTMP;
        lmUI.Background = itemImg;
        lmUI.PreviewImage = previewImage;
        // LeftStripe 功能已移除
        // NormalColor 和 SelectedColor 已在 LocalModListItemUI 类中设置默认值

        // 添加 Button 交互，但不要在 prefab 上绑定具体实例方法（会在克隆后绑定）
        var itemButton = itemPrefab.AddComponent<Button>();
        itemButton.targetGraphic = itemImg;

        var listUIObj = PanelUI.CreateUIObject("LocalModListUI", listPanel.transform);
        var listUI = listUIObj.AddComponent<LocalModListUI>();
        listUI.ContentParent = listContent.transform;
        listUI.LocalModListItemPrefab = itemPrefab;

        // 右侧详情面板 - 使用 PanelUI 的 RightPanel
        var detailsPanel = panelBase.RightPanel;

        // PanelUI 的 RightPanel 已包含垂直布局，直接使用

        // 直接为右侧面板创建滚动容器，简化层级结构
        // CreateScrollContainer会自动处理ScrollView -> Viewport -> Content的层级
        var detailsScrollContent = CreateScrollContainer(detailsPanel);

        // 调整滚动容器Content的垂直布局参数
        var detailsVLG = detailsScrollContent.GetComponent<VerticalLayoutGroup>();
        detailsVLG.padding = new RectOffset(12, 12, 8, 8);

        // LocalModDetailsUI 添加到 Content 中
        var detailsUIObj = PanelUI.CreateUIObject("LocalModDetailsUI", detailsScrollContent.transform);

        var detailsUI = detailsUIObj.AddComponent<LocalModDetailsUI>();

        // Single VerticalLayoutGroup for all content
        var detailsV = detailsUIObj.AddComponent<VerticalLayoutGroup>();
        detailsV.childAlignment = TextAnchor.UpperLeft;
        // Reduce spacing to bring sections closer together
        detailsV.spacing = 4f;
        // Tighten top padding a bit more
        detailsV.padding = new RectOffset(12, 12, 4, 12);
        detailsV.childControlWidth = true;
        detailsV.childForceExpandWidth = true;
        detailsV.childControlHeight = true;
        detailsV.childForceExpandHeight = false;

        // 重要：不要用 PreferredSize 让内容高度跟随子项总和，否则 flexibleHeight 不会生效
        detailsUIObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Header row: Preview image + name column
        var headerRow = PanelUI.CreateUIObject("HeaderRow", detailsUIObj.transform);
        var headerHL = headerRow.AddComponent<HorizontalLayoutGroup>();
        headerHL.childAlignment = TextAnchor.UpperLeft;
        headerHL.spacing = 10f;
        headerHL.childForceExpandWidth = false;
        headerHL.childForceExpandHeight = false;
        var headerLE = headerRow.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 140f;
        // IMPORTANT: children of a VerticalLayoutGroup must not be stretch-anchored vertically,
        // otherwise they'll fill the parent and visually cover siblings. Anchor them to Top.
        SetAnchors(headerRow, new Vector2(0, 1), new Vector2(1, 1));
        headerRow.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);

        // Preview image
        var previewGo = PanelUI.CreateUIObject("PreviewImage", headerRow.transform);
        var previewImg = previewGo.AddComponent<Image>();
        previewImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var detailsPreviewLE = previewGo.AddComponent<LayoutElement>();
        detailsPreviewLE.preferredWidth = 140f;
        detailsPreviewLE.preferredHeight = 140f;
        detailsPreviewLE.minWidth = 140f;
        detailsPreviewLE.minHeight = 140f;
        detailsPreviewLE.flexibleHeight = 0f;
        detailsPreviewLE.flexibleWidth = 0f;
        // 保证 RectTransform 的 sizeDelta 也是正方形
        var previewRect2 = previewGo.GetComponent<RectTransform>();
        previewRect2.sizeDelta = new Vector2(140f, 140f);
        detailsUI.PreviewImage = previewImg;

        // Name column (vertical)
        var nameColumn = PanelUI.CreateUIObject("HeaderRight", headerRow.transform);
        var nv = nameColumn.AddComponent<VerticalLayoutGroup>();
        nv.childAlignment = TextAnchor.UpperLeft;
        nv.spacing = 4f;
        // 让列控制子行的宽度并横向拉伸到可用区域
        nv.childControlWidth = true;
        nv.childForceExpandWidth = true;
        nv.childControlHeight = true;
        nv.childForceExpandHeight = false;
        var nameColLE = nameColumn.AddComponent<LayoutElement>();
        nameColLE.flexibleWidth = 1f;
        nameColLE.minWidth = 200f; // 确保有最小宽度

        // DisplayName Row with HorizontalLayoutGroup
        var displayNameRow = PanelUI.CreateUIObject("DisplayNameRow", nameColumn.transform);
        var dnhLayoutGroup = displayNameRow.AddComponent<HorizontalLayoutGroup>();
        dnhLayoutGroup.spacing = 8f;
        dnhLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        // 水平布局负责根据首选宽度安置子元素，不强制扩展
        dnhLayoutGroup.childControlWidth = true;
        dnhLayoutGroup.childForceExpandWidth = false;
        dnhLayoutGroup.childControlHeight = true;
        dnhLayoutGroup.childForceExpandHeight = false; // 不强制子元素垂直拉伸
        SetAnchors(displayNameRow, new Vector2(0, 1), new Vector2(1, 1));
        displayNameRow.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        // 添加 LayoutElement 以确保行高度由内容决定
        var displayNameRowLE = displayNameRow.AddComponent<LayoutElement>();
        displayNameRowLE.preferredHeight = 24f;

        var displayNameLabelGO = PanelUI.CreateUIObject("DisplayNameLabel", displayNameRow.transform);
        var displayNameLabelTMP = displayNameLabelGO.AddComponent<TextMeshProUGUI>();
        displayNameLabelTMP.fontSize = labelFontSize;
        displayNameLabelTMP.color = Color.gray;
        displayNameLabelTMP.alignment = TextAlignmentOptions.MidlineLeft; // 垂直居中对齐
        displayNameLabelTMP.text = L.Get(LK.Mod.LabelDisplayName);
        displayNameLabelTMP.enableWordWrapping = false;
        var displayNameLabelLE = displayNameLabelGO.AddComponent<LayoutElement>();
        displayNameLabelLE.preferredWidth = 80f;
        displayNameLabelLE.preferredHeight = 24f;
        displayNameLabelLE.flexibleWidth = 0f;

        var displayNameGO = PanelUI.CreateUIObject("DisplayNameText", displayNameRow.transform);
        var displayNameTMP = displayNameGO.AddComponent<TextMeshProUGUI>();
        displayNameTMP.fontSize = 20;
        displayNameTMP.fontStyle = FontStyles.Bold;
        displayNameTMP.color = Color.white;
        displayNameTMP.alignment = TextAlignmentOptions.MidlineLeft; // 垂直居中对齐
        displayNameTMP.overflowMode = TextOverflowModes.Ellipsis;
        displayNameTMP.enableWordWrapping = false;
        var displayNameLE = displayNameGO.AddComponent<LayoutElement>();
        displayNameLE.minWidth = 200f; // 最小宽度
        // 使用弹性宽度交给布局系统分配空间，避免 CSF 与 LayoutGroup 冲突
        displayNameLE.flexibleWidth = 1f;
        displayNameLE.preferredHeight = 24f;
        detailsUI.DisplayNameText = displayNameTMP;

        Log.Current.Debug(
            $"[DeveloperModeView] DisplayNameText configured: fontSize={displayNameTMP.fontSize}, color={displayNameTMP.color}");

        // Name row: label and value on the same line
        var nameRow = PanelUI.CreateUIObject("NameRow", nameColumn.transform);
        var nameHL = nameRow.AddComponent<HorizontalLayoutGroup>();
        nameHL.childAlignment = TextAnchor.MiddleLeft;
        nameHL.spacing = 8f;
        // 同上：根据首选宽度安置，不强制扩展
        nameHL.childControlWidth = true;
        nameHL.childForceExpandWidth = false;
        nameHL.childControlHeight = true;
        nameHL.childForceExpandHeight = false; // 不强制子元素垂直拉伸
        SetAnchors(nameRow, new Vector2(0, 1), new Vector2(1, 1));
        nameRow.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        var nameRowLE = nameRow.AddComponent<LayoutElement>();
        nameRowLE.preferredHeight = 24f;

        var nameLabelGO = PanelUI.CreateUIObject("NameLabel", nameRow.transform);
        var nameLabelTMP = nameLabelGO.AddComponent<TextMeshProUGUI>();
        nameLabelTMP.fontSize = labelFontSize;
        nameLabelTMP.color = Color.gray;
        nameLabelTMP.alignment = TextAlignmentOptions.MidlineLeft; // 垂直居中对齐
        nameLabelTMP.text = L.Get(LK.Mod.LabelName);
        nameLabelTMP.enableWordWrapping = false;
        var nameLabelLE = nameLabelGO.AddComponent<LayoutElement>();
        nameLabelLE.preferredWidth = 80f;
        nameLabelLE.preferredHeight = 24f;
        nameLabelLE.flexibleWidth = 0f;

        var nameGO = PanelUI.CreateUIObject("NameText", nameRow.transform);
        var nameTextTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTextTMP.fontSize = 16;
        nameTextTMP.color = Color.white;
        nameTextTMP.alignment = TextAlignmentOptions.MidlineLeft; // 垂直居中对齐
        nameTextTMP.overflowMode = TextOverflowModes.Ellipsis;
        nameTextTMP.enableWordWrapping = false;
        var detailsNameLE = nameGO.AddComponent<LayoutElement>();
        detailsNameLE.minWidth = 200f; // 最小宽度
        // 与上同理，使用弹性宽度让其占据剩余空间
        detailsNameLE.flexibleWidth = 1f;
        detailsNameLE.preferredHeight = 24f;
        detailsUI.NameText = nameTextTMP;

        // Version row: label and value on the same line
        var versionRow = PanelUI.CreateUIObject("VersionRow", nameColumn.transform);
        var versionHL = versionRow.AddComponent<HorizontalLayoutGroup>();
        versionHL.childAlignment = TextAnchor.MiddleLeft;
        versionHL.spacing = 8f;
        // 同上：根据首选宽度安置，不强制扩展
        versionHL.childControlWidth = true;
        versionHL.childForceExpandWidth = false;
        versionHL.childControlHeight = true;
        versionHL.childForceExpandHeight = false; // 不强制子元素垂直拉伸
        SetAnchors(versionRow, new Vector2(0, 1), new Vector2(1, 1));
        versionRow.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        var versionRowLE = versionRow.AddComponent<LayoutElement>();
        versionRowLE.preferredHeight = 24f;

        var versionLabelGO = PanelUI.CreateUIObject("VersionLabel", versionRow.transform);
        var versionLabelTMP = versionLabelGO.AddComponent<TextMeshProUGUI>();
        versionLabelTMP.fontSize = labelFontSize;
        versionLabelTMP.color = Color.gray;
        versionLabelTMP.alignment = TextAlignmentOptions.MidlineLeft; // 垂直居中对齐
        versionLabelTMP.text = "版本";
        versionLabelTMP.enableWordWrapping = false;
        var versionLabelLE = versionLabelGO.AddComponent<LayoutElement>();
        versionLabelLE.preferredWidth = 80f;
        versionLabelLE.preferredHeight = 24f;
        versionLabelLE.flexibleWidth = 0f;

        var versionGO = PanelUI.CreateUIObject("VersionText", versionRow.transform);
        var versionTextTMP = versionGO.AddComponent<TextMeshProUGUI>();
        versionTextTMP.fontSize = 16;
        versionTextTMP.color = Color.white;
        versionTextTMP.alignment = TextAlignmentOptions.MidlineLeft; // 垂直居中对齐
        versionTextTMP.overflowMode = TextOverflowModes.Ellipsis;
        versionTextTMP.enableWordWrapping = false;
        var detailsVersionLE = versionGO.AddComponent<LayoutElement>();
        detailsVersionLE.minWidth = 200f; // 最小宽度
        // 与上同理，使用弹性宽度让其占据剩余空间
        detailsVersionLE.flexibleWidth = 1f;
        detailsVersionLE.preferredHeight = 24f;
        detailsUI.VersionText = versionTextTMP;

        Log.Current.Debug(
            $"[DeveloperModeView] NameText configured: fontSize={nameTextTMP.fontSize}, color={nameTextTMP.color}");

        // Force layout rebuild to get actual positions
        LayoutRebuilder.ForceRebuildLayoutImmediate(headerRow.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();

        // Tags
        var tagsLabelGO = PanelUI.CreateUIObject("TagsLabel", detailsUIObj.transform);
        var tagsLabelTMP = tagsLabelGO.AddComponent<TextMeshProUGUI>();
        tagsLabelTMP.fontSize = labelFontSize;
        tagsLabelTMP.color = Color.gray;
        tagsLabelTMP.alignment = TextAlignmentOptions.Left;
        tagsLabelTMP.text = L.Get(LK.Mod.LabelTags);
        SetAnchors(tagsLabelGO, new Vector2(0, 1), new Vector2(1, 1));
        tagsLabelGO.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);

        var tagsGO = PanelUI.CreateUIObject("TagsText", detailsUIObj.transform);
        var tagsTMP = tagsGO.AddComponent<TextMeshProUGUI>();
        tagsTMP.fontSize = 14;
        tagsTMP.color = Color.cyan;
        tagsTMP.alignment = TextAlignmentOptions.Left;
        SetAnchors(tagsGO, new Vector2(0, 1), new Vector2(1, 1));
        tagsGO.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        detailsUI.TagsText = tagsTMP;

        // Dependencies label
        var depsLabelGO = PanelUI.CreateUIObject("DependenciesLabel", detailsUIObj.transform);
        SetAnchors(depsLabelGO, new Vector2(0, 1), new Vector2(1, 1));
        depsLabelGO.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        var depsLabelTMP = depsLabelGO.AddComponent<TextMeshProUGUI>();
        depsLabelTMP.fontSize = labelFontSize;
        depsLabelTMP.color = Color.gray;
        depsLabelTMP.alignment = TextAlignmentOptions.Left;
        depsLabelTMP.text = L.Get(LK.Mod.LabelDependencies);

        // Dependencies panel
        var depsPanel = PanelUI.CreateUIObject("DependenciesPanel", detailsUIObj.transform);
        var depsV = depsPanel.AddComponent<VerticalLayoutGroup>();
        depsV.childAlignment = TextAnchor.UpperLeft;
        depsV.spacing = 4f;
        SetAnchors(depsPanel, new Vector2(0, 1), new Vector2(1, 1));
        depsPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        detailsUI.DependenciesPanel = depsPanel.transform;

        // Description area (horizontal layout with two scrollable descriptions)
        var descContainer = PanelUI.CreateUIObject("DescriptionContainer", detailsUIObj.transform);
        var descContainerHL = descContainer.AddComponent<HorizontalLayoutGroup>();
        descContainerHL.childAlignment = TextAnchor.UpperLeft;
        descContainerHL.spacing = 8f;
        descContainerHL.childForceExpandWidth = true;
        descContainerHL.childForceExpandHeight = true;
        descContainerHL.childControlWidth = true;
        descContainerHL.childControlHeight = true;
        SetAnchors(descContainer, new Vector2(0, 1), new Vector2(1, 1));
        descContainer.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        var descContainerLE = descContainer.AddComponent<LayoutElement>();
        // 使用弹性高度让描述区域自动占据剩余空间
        descContainerLE.minHeight = 400f; // 增大整体描述区容器高度
        descContainerLE.preferredHeight = 600f;
        descContainerLE.flexibleHeight = 1f;
        descContainerLE.flexibleWidth = 1f; // 补充宽度弹性

        // Language Detection Panel - replaces old Description and BBCode sections
        var langDetectBlock = PanelUI.CreateUIObject("LanguageDetectionBlock", descContainer.transform);
        var langDetectVL = langDetectBlock.AddComponent<VerticalLayoutGroup>();
        langDetectVL.childAlignment = TextAnchor.UpperLeft;
        langDetectVL.spacing = 8f;
        langDetectVL.childForceExpandWidth = true;
        langDetectVL.childForceExpandHeight = false;
        langDetectVL.childControlWidth = true;
        langDetectVL.childControlHeight = true;
        SetAnchors(langDetectBlock, Vector2.zero, Vector2.one);
        var langDetectLE = langDetectBlock.AddComponent<LayoutElement>();
        langDetectLE.flexibleWidth = 1f;
        langDetectLE.flexibleHeight = 1f;
        langDetectLE.minWidth = 0f;
        langDetectLE.minHeight = 0f;
        langDetectLE.preferredHeight = 400f;

        // Language Detection scroll area - 使用标准滚动容器
        var langDetectContent = CreateScrollContainer(langDetectBlock);
        // CreateScrollContainer 已经添加了 VerticalLayoutGroup，只需调整参数
        var existingVLG = langDetectContent.GetComponent<VerticalLayoutGroup>();
        existingVLG.childAlignment = TextAnchor.UpperLeft;
        existingVLG.spacing = 4f;

        detailsUI.LanguageDetectionPanel = langDetectContent.transform;

        // Add action buttons to PanelBase RightToolbar
        var rightToolbar = panelBase.RightToolbar;
        var rightToolbarGO = rightToolbar.gameObject;

        // Add LayoutElement to control toolbar height (like ModDataCheckView)
        var rightToolbarLE = rightToolbarGO.GetComponent<LayoutElement>() ??
                             rightToolbarGO.AddComponent<LayoutElement>();
        rightToolbarLE.minHeight = 32f; // Fixed toolbar height
        rightToolbarLE.preferredHeight = 32f;
        rightToolbarLE.flexibleHeight = 0f; // Don't allow expansion

        // Configure RightToolbar layout to match ModDataCheckView
        var rightToolbarLayout = rightToolbarGO.GetComponent<HorizontalLayoutGroup>();
        rightToolbarLayout.spacing = 5f; // Reduce spacing
        rightToolbarLayout.padding = new RectOffset(10, 10, 4, 4); // Adjust padding
        rightToolbarLayout.childForceExpandWidth = false;
        rightToolbarLayout.childForceExpandHeight = false; // Important: don't expand child heights
        rightToolbarLayout.childControlWidth = true;
        rightToolbarLayout.childControlHeight = true;
        rightToolbarLayout.childAlignment = TextAnchor.MiddleLeft;

        // Create action buttons for RightToolbar using same style as ModDataCheckView
        // Each button gets its own section container to prevent height stretching
        var btnFolder = CreateRightToolbarButtonInSection(rightToolbar.transform, L.Get(LK.Mod.ButtonFolder),
            new Color(0.2f, 0.4f, 0.6f, 1f));
        var btnSteam = CreateRightToolbarButtonInSection(rightToolbar.transform, L.Get(LK.Mod.ButtonSteam),
            new Color(0.4f, 0.7f, 1.0f, 1f));

        var controllerObj = PanelUI.CreateUIObject("DeveloperModeController", panelBase.CanvasGo.transform);
        controller = controllerObj.AddComponent<LocalModPanelController>();
        controller.LocalModListUI = listUI;
        controller.LocalModDetailsUI = detailsUI;
        controller.btnFolder = btnFolder;
        controller.btnSteam = btnSteam;
        controller.btnUpload = btnUpload;

        // 绑定返回按钮事件
        backBtn.OnClickAsObservable()
            .Subscribe(_ => Close())
            .AddTo(ref controller._disposables);

        // 保存 PanelBase 以便后续清理
        _modalRoot = panelBase.CanvasGo;
    }

    public static void Close()
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
    }

    /// <summary>
    /// 创建标准滚动容器（参考 ModDataCheckView.CreateContainer 的实现方式）
    /// </summary>
    private static GameObject CreateScrollContainer(GameObject parent)
    {
        // 创建 ScrollView 容器
        var scrollViewGO = PanelUI.CreateUIObject("ScrollView", parent.transform);
        var scrollViewRT = scrollViewGO.GetComponent<RectTransform>();

        // 关键：ScrollView必须填充父容器
        scrollViewRT.anchorMin = Vector2.zero;
        scrollViewRT.anchorMax = Vector2.one;
        scrollViewRT.offsetMin = Vector2.zero;
        scrollViewRT.offsetMax = Vector2.zero;

        // 关键：添加LayoutElement让ScrollView填充剩余空间
        var scrollViewLE = scrollViewGO.AddComponent<LayoutElement>();
        scrollViewLE.flexibleHeight = 1f; // 填充剩余垂直空间
        scrollViewLE.preferredHeight = 300f; // 最小高度

        // 添加 ScrollRect 组件
        var scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 3; // 项目规范：滚动列表灵敏度固定为 3

        // 创建 Viewport - 使用 RectMask2D 而不是 Mask，并确保填满ScrollView
        var viewportGO = PanelUI.CreateUIObject("Viewport", scrollViewGO.transform);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewportRT.pivot = new Vector2(0.5f, 0.5f);

        // 使用 RectMask2D 代替 Mask，裁剪更准确
        viewportGO.AddComponent<RectMask2D>();

        var viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);

        // 为viewport添加边框效果
        var viewportOutline = viewportGO.AddComponent<Outline>();
        viewportOutline.effectColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);
        viewportOutline.effectDistance = new Vector2(1, 1);

        scrollRect.viewport = viewportRT;

        // 创建 Content（实际内容容器）
        var contentGO = PanelUI.CreateUIObject("Content", viewportGO.transform);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        scrollRect.content = contentRT;

        // 关键：Content必须有VerticalLayoutGroup + ContentSizeFitter
        var vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 6f;
        vLayout.padding = new RectOffset(0, 0, 0, 0); // 移除padding，让列表贴近边缘
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childAlignment = TextAnchor.UpperLeft;

        // Content 添加 ContentSizeFitter 自动调整高度
        var sizeFitter = contentGO.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 返回 Content，子项将添加到这里
        return contentGO;
    }

    internal static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 创建现代化按钮（与 PanelUI 样式一致）
    /// </summary>
    private static Button CreateModernButton(Transform parent, string text, Color baseColor)
    {
        var buttonGO = PanelUI.CreateUIObject("Button_" + text, parent);
        var buttonLE = buttonGO.AddComponent<LayoutElement>();
        buttonLE.preferredWidth = 100f;
        buttonLE.preferredHeight = 16f; // 缩小高度一半（从32f改为16f）
        buttonLE.flexibleWidth = 0f;
        buttonLE.flexibleHeight = 0f;

        var buttonImg = buttonGO.AddComponent<Image>();

        // 尝试使用 UIResourceManager 的资源
        try
        {
            var buttonSprite = UIResourceManager.GetSprite("square_bg");
            buttonImg.sprite = buttonSprite;
            buttonImg.type = Image.Type.Sliced;
            buttonImg.color = Color.white;
        }
        catch
        {
            buttonImg.color = baseColor;
        }

        var button = buttonGO.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        // 设置禁用状态为红色
        colors.disabledColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.targetGraphic = buttonImg;

        // 添加按钮边框效果
        var btnOutline = buttonGO.AddComponent<Outline>();
        btnOutline.effectColor = baseColor;
        btnOutline.effectDistance = new Vector2(1, 1);

        var textGO = PanelUI.CreateUIObject("Text", buttonGO.transform);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = text;
        textTMP.fontSize = 12f; // 缩小字体大小以适应较小的按钮高度
        textTMP.fontStyle = FontStyles.Bold;
        textTMP.color = Color.white;
        textTMP.alignment = TextAlignmentOptions.Center;
        textTMP.outlineWidth = 0.2f;
        textTMP.outlineColor = new Color(0f, 0f, 0f, 0.7f);
        PanelUI.SetAnchors(textGO, Vector2.zero, Vector2.one);
        textTMP.raycastTarget = false;

        return button;
    }

    /// <summary>
    /// 创建 RightToolbar 按钮，参考 ModDataCheckView 的实现
    /// </summary>
    private static Button CreateRightToolbarButton(Transform parent, string text, Color baseColor)
    {
        var buttonGO = PanelUI.CreateUIObject("Button_" + text, parent);

        // 使用固定尺寸，与 ModDataCheckView 保持一致
        var buttonLE = buttonGO.AddComponent<LayoutElement>();
        buttonLE.minWidth = 100f; // 固定最小宽度
        buttonLE.preferredWidth = 100f; // 固定首选宽度
        buttonLE.minHeight = 24f; // 固定高度
        buttonLE.preferredHeight = 24f;
        buttonLE.ignoreLayout = false; // 参与布局但使用固定尺寸

        var buttonImg = buttonGO.AddComponent<Image>();

        // 尝试使用 UIResourceManager 的资源
        try
        {
            var buttonSprite = UIResourceManager.GetSprite("square_bg");
            buttonImg.sprite = buttonSprite;
            buttonImg.type = Image.Type.Sliced;
            buttonImg.color = Color.white;
        }
        catch
        {
            buttonImg.color = baseColor;
        }

        var button = buttonGO.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.targetGraphic = buttonImg;

        // 添加按钮边框效果
        var btnOutline = buttonGO.AddComponent<Outline>();
        btnOutline.effectColor = baseColor;
        btnOutline.effectDistance = new Vector2(1, 1);

        var textGO = PanelUI.CreateUIObject("Text", buttonGO.transform);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = text;
        textTMP.fontSize = 12f;
        textTMP.fontStyle = FontStyles.Bold;
        textTMP.color = Color.black; // 黑色文字与白色背景形成对比
        textTMP.alignment = TextAlignmentOptions.Center;
        textTMP.raycastTarget = false;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    /// <summary>
    /// 创建带section容器的RightToolbar按钮，参考ModDataCheckView的完整实现
    /// </summary>
    private static Button CreateRightToolbarButtonInSection(Transform parent, string text, Color baseColor)
    {
        // Create section container first (like ModDataCheckView's activeSection)
        var sectionGO = PanelUI.CreateUIObject("Section_" + text, parent);

        var sectionLayout = sectionGO.AddComponent<HorizontalLayoutGroup>();
        sectionLayout.spacing = 5f;
        sectionLayout.padding = new RectOffset(5, 5, 4, 4);
        sectionLayout.childForceExpandWidth = false;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandHeight = false; // Important: don't expand vertically
        sectionLayout.childAlignment = TextAnchor.MiddleLeft;

        // Now create the button inside the section
        var buttonGO = PanelUI.CreateUIObject("Button_" + text, sectionGO.transform);

        // 使用固定尺寸，与 ModDataCheckView 保持一致
        var buttonLE = buttonGO.AddComponent<LayoutElement>();
        buttonLE.minWidth = 100f; // 固定最小宽度
        buttonLE.preferredWidth = 100f; // 固定首选宽度
        buttonLE.minHeight = 24f; // 固定高度
        buttonLE.preferredHeight = 24f;
        buttonLE.ignoreLayout = false; // 参与布局但使用固定尺寸

        var buttonImg = buttonGO.AddComponent<Image>();

        // 尝试使用 UIResourceManager 的资源
        try
        {
            var buttonSprite = UIResourceManager.GetSprite("square_bg");
            buttonImg.sprite = buttonSprite;
            buttonImg.type = Image.Type.Sliced;
            buttonImg.color = Color.white;
        }
        catch
        {
            buttonImg.color = baseColor;
        }

        var button = buttonGO.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.targetGraphic = buttonImg;

        // 添加按钮边框效果
        var btnOutline = buttonGO.AddComponent<Outline>();
        btnOutline.effectColor = baseColor;
        btnOutline.effectDistance = new Vector2(1, 1);

        var textGO = PanelUI.CreateUIObject("Text", buttonGO.transform);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = text;
        textTMP.fontSize = 12f;
        textTMP.fontStyle = FontStyles.Bold;
        textTMP.color = Color.black; // 黑色文字与白色背景形成对比
        textTMP.alignment = TextAlignmentOptions.Center;
        textTMP.raycastTarget = false;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }
}
