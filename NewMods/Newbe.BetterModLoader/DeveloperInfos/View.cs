using Newbe.BetterModLoader.Audio;

namespace Newbe.BetterModLoader.DeveloperInfos;

internal partial class DeveloperInfoPanel : MonoBehaviour
{
    public static void Show()
    {
        Log.Debug("[DeveloperInfoPanel] Show called, creating new panel instance.");
        var go = new GameObject("DeveloperInfoPanel");
        // 由 UIFactory.CreatePanelBase 自动创建顶层 Canvas，无需挂载到主 Canvas
        var panel = go.AddComponent<DeveloperInfoPanel>();
        var uploadPanelData = CreateData();
        var vm = new DeveloperInfoVm(uploadPanelData);
        panel.Init(vm);
    }

    public static DeveloperInfoData CreateData()
    {
        var info = ModManager.modInfos.FirstOrDefault(ModIdentifier.IsBetterModLoader);
        return new DeveloperInfoData
        {
            LocalModsFolder = ModManager.DefaultModFolderPath,
            SteamModsFolder = !string.IsNullOrWhiteSpace(info.dllPath)
                ? Path.Combine(Path.GetDirectoryName(info.dllPath)!, "../")
                : string.Empty,
            GamePersistentDataFolder = Application.persistentDataPath,
            BetterModeLoaderConfigFolder = ModLoaderSaveSystem.ModLoaderFolder,
            BetterModeLoaderConfigPath = ModLoaderSaveSystem.ConfigFilePath,
            LogDirectorySizeInMB = 0,
            ModOptionsForAllFolder = ModOptions.ForAllMods.GetConfigFolderPath(),
            ModOptionsForEachFolder = ModOptions.ForName.GetConfigFolderPath(),
            OriginalGameLogsFolder = Application.persistentDataPath,
        };
    }
}

internal partial class DeveloperInfoPanel
{
    private DeveloperInfoVm? _vm;
    private DisposableBag _disposableBag;
    private Button _closeBtn = null!;
    private GameObject _OpenFolderContainer = null!;
    private Toggle _forceRestartToggle = null!;

    private void OnDestroy()
    {
        _disposableBag.Dispose();
    }

    public void Init(DeveloperInfoVm vm)
    {
        _vm = vm;
        _disposableBag.Add(_vm);
        Layout();

        _vm.DeveloperInfoData
            .Subscribe(data =>
            {
                var folders = data.GetAllFolders();
                foreach (Transform child in _OpenFolderContainer.transform)
                {
                    Destroy(child.gameObject);
                }

                foreach (var folder in folders)
                {
                    var item = OpenFolderRaw.Create(_OpenFolderContainer, folder);
                    item.openFolderButton.onClick.AddListener(() =>
                    {
                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        FileHelper.OpenFolder(folder.Path);
                    });
                    item.copyButton.onClick.AddListener(() =>
                    {
                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        FileHelper.CopyToClipboard(folder.Path);
                    });
                }
            })
            .AddTo(ref _disposableBag);
    }
}

internal partial class DeveloperInfoPanel
{
    private PanelBase _panelBase = null!;

    private void Layout()
    {
        // 创建 PanelBase（已自动包含 toolbar/title 行布局）
        _panelBase = PanelUI.CreatePanelBase(L.DeveloperInfo.Title);

        // 配置 LeftToolbar 的 HorizontalLayoutGroup
        (_closeBtn, _) = _panelBase.AddTitleToolbarButton(L.UI.Back, PanelBase.ButtonColorTheme.SkyBlue);
        _closeBtn.onClick.AddListener(() =>
        {
            BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
            Destroy(gameObject);
        });

        // add form at left side
        {
            _forceRestartToggle = PanelUI.CreateToggle(_panelBase.LeftPanel, L.UI.ForceRestartToggle, 16);

            // Subscribe to ViewModel changes
            _vm!.ForceRestartOnOrderChanges
                .Subscribe(value => _forceRestartToggle.isOn = value)
                .AddTo(ref _disposableBag);

            // Subscribe to UI changes and update ViewModel with audio feedback
            _forceRestartToggle.WithToggleAudio().AddTo(ref _disposableBag);
            _forceRestartToggle.OnValueChangedAsObservable()
                .Subscribe(value => _vm!.SetForceRestartOnOrderChanges(value))
                .AddTo(ref _disposableBag);
        }

        // create right side content panel
        {
            // 创建 OpenFolder 容器
            _OpenFolderContainer = CreateOpenFolderContainer(_panelBase.RightPanel);
        }

        _disposableBag.Add(_panelBase);
    }

    private static GameObject CreateOpenFolderContainer(GameObject parent)
    {
        // 创建 ScrollView 容器
        var scrollViewGO = new GameObject("OpenFolderScrollView");
        scrollViewGO.transform.SetParent(parent.transform, false);

        var scrollViewRT = scrollViewGO.AddComponent<RectTransform>();
        scrollViewRT.anchorMin = Vector2.zero;
        scrollViewRT.anchorMax = Vector2.one;
        scrollViewRT.sizeDelta = Vector2.zero;

        var scrollViewLE = scrollViewGO.AddComponent<LayoutElement>();
        scrollViewLE.flexibleHeight = 1f;
        scrollViewLE.preferredHeight = 300f;

        // 添加 ScrollRect 组件
        var scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 3;

        // 创建 Viewport
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollViewGO.transform, false);
        var viewportRT = viewportGO.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;
        viewportRT.pivot = new Vector2(0.5f, 0.5f);

        var viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = true;

        var viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        scrollRect.viewport = viewportRT;

        // 创建 Content（实际内容容器）
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 0f);

        scrollRect.content = contentRT;

        // Content 添加垂直布局
        var vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 5f;
        vLayout.padding = new RectOffset(10, 10, 10, 10);
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
}

internal class OpenFolderRaw : MonoBehaviour
{
    public TextMeshProUGUI folderNameLabel = null!;
    public TextMeshProUGUI folderPathText = null!;
    public Button openFolderButton = null!;
    public Button copyButton = null!;

    /// <summary>
    /// 创建一个文件夹/文件信息展示元素
    /// </summary>
    /// <param name="parent">父对象</param>
    /// <param name="folder">文件夹信息</param>
    /// <returns>OpenFolderRaw 组件</returns>
    public static OpenFolderRaw Create(GameObject parent, LabeledFolderPath folder)
    {
        // 创建主容器（垂直布局）
        var itemGO = PanelUI.CreateUIObject("OpenFolderItem", parent.transform);
        var itemLE = itemGO.AddComponent<LayoutElement>();
        itemLE.preferredHeight = 50f;
        itemLE.flexibleHeight = 0f;

        // 添加背景
        var bgImg = itemGO.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);

        // 创建主垂直布局
        var vLayout = itemGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 0f;
        vLayout.padding = new RectOffset(8, 8, 3, 3);
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;

        // 第一行：水平布局（文件夹按钮 + 复制按钮 + Label）
        var firstRowGO = PanelUI.CreateUIObject("FirstRow", itemGO.transform);
        var hLayout = firstRowGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 10f;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = true;

        var firstRowLE = firstRowGO.AddComponent<LayoutElement>();
        firstRowLE.preferredHeight = 22f;
        firstRowLE.flexibleWidth = 1f;

        // 文件夹按钮
        var (openFolderBtn, openFolderTxt) = CreateSimpleButton(firstRowGO, L.DeveloperInfo.OpenFolder);
        var openFolderLE = openFolderBtn.gameObject.AddComponent<LayoutElement>();
        openFolderLE.preferredWidth = 70f;
        openFolderLE.preferredHeight = 22f;

        // 复制按钮
        var (copyBtn, copyBtnTxt) = CreateSimpleButton(firstRowGO, L.DeveloperInfo.CopyPath);
        var copyLE = copyBtn.gameObject.AddComponent<LayoutElement>();
        copyLE.preferredWidth = 70f;
        copyLE.preferredHeight = 22f;

        // Label 文本
        var labelGO = PanelUI.CreateUIObject("FolderLabel", firstRowGO.transform);
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = folder.Label;
        labelText.fontSize = 16;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.fontStyle = FontStyles.Bold;
        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f;
        labelLE.preferredHeight = 22f;

        // 第二行：路径显示
        var secondRowGO = PanelUI.CreateUIObject("SecondRow", itemGO.transform);
        var pathText = secondRowGO.AddComponent<TextMeshProUGUI>();
        pathText.text = folder.Path;
        pathText.fontSize = 18;
        pathText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        pathText.alignment = TextAlignmentOptions.Left;
        pathText.fontStyle = FontStyles.Bold;
        pathText.overflowMode = TextOverflowModes.Ellipsis;
        var secondRowLE = secondRowGO.AddComponent<LayoutElement>();
        secondRowLE.flexibleWidth = 1f;
        secondRowLE.preferredHeight = 22f;

        // 添加组件并赋值
        var view = itemGO.AddComponent<OpenFolderRaw>();
        view.folderNameLabel = labelText;
        view.folderPathText = pathText;
        view.openFolderButton = openFolderBtn;
        view.copyButton = copyBtn;

        return view;
    }

    /// <summary>
    /// 创建简单按钮（内部辅助方法）
    /// </summary>
    private static (Button btn, TextMeshProUGUI text) CreateSimpleButton(GameObject parent, string label)
    {
        var btnObj = PanelUI.CreateUIObject("Button_" + label, parent.transform);

        // 添加背景图片
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.5f, 0.7f, 1f);

        // 添加按钮组件
        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.5f, 0.7f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f, 1f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        btn.colors = colors;

        // 添加文本
        var textObj = PanelUI.CreateUIObject("Text", btnObj.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        PanelUI.SetAnchors(textObj, Vector2.zero, Vector2.one);

        return (btn, tmp);
    }
}

internal partial class DeveloperInfoPanel
{
    /// <summary>
    /// Gets the current force restart setting from mod options.
    /// This can be used by other components to check if restart is required.
    /// </summary>
    /// <returns>True if restart is required on order changes, false otherwise</returns>
    public static bool ShouldForceRestartOnOrderChanges()
    {
        return Configs.ShouldForceRestartOnOrderChanges();
    }
}
