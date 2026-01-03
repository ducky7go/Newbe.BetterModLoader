using Newbe.BetterModLoader.DeveloperInfos;
using Newbe.BetterModLoader.DeveloperModes;
using Newbe.BetterModLoader.ModDataChecks;
using Newbe.BetterModLoader.UI.ManagerUI.Views;
using Newbe.BetterModLoader.Audio;
using Steamworks;

namespace Newbe.BetterModLoader.Main;

internal class BmlModManagerUIView : MonoBehaviour
{
    private BmlModManagerUIVm _vm;
    private DisposableBag _disposableBag;

    // UI References
    private Canvas _canvas;
    private GameObject _mainPanel;
    private GameObject _closeBtnGO;
    private GameObject _applyBtnGO;
    private GameObject _discardBtnGO;
    private DropdownMenuView _dropdownMenuView;

    private Transform _groupContentTransform;
    private Transform _modContentTransform;
    private RectTransform _groupScrollViewRect;
    private GroupFooterEditorView _groupFooterView;
    private ScrollRect _modScrollRect;

    private BmlModListView _modListView;
    private Dictionary<string, GameObject> _modItemObjects = new();
    private Transform _hiddenContainer;

    // Rename tracking
    private ModGroup? _renamingGroup;
    private GameObject? _renameInputGO;
    private TMP_InputField? _renameInputField;

    public static void Show()
    {
        var go = new GameObject("[BmlModManagerUI]");
        DontDestroyOnLoad(go);
        var view = go.AddComponent<BmlModManagerUIView>();
        view.Init(new BmlModManagerUIVm());
    }

    public void Init(BmlModManagerUIVm vm)
    {
        _vm = vm;
        _modListView = new BmlModListView(this, _vm);
        CreateUI();
        BindViewModel();
        SubscribeToDragEvents();
    }

    private void CreateUI()
    {
        // Canvas Setup
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        _canvas.overrideSorting = true;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        // Create hidden container for pre-created items
        var hiddenGO = new GameObject("HiddenContainer");
        hiddenGO.transform.SetParent(transform, false);
        hiddenGO.SetActive(false);
        _hiddenContainer = hiddenGO.transform;

        CreateMainPanel();
    }

    // Removed EnsureTopmostSortingOrder to match BetterModManagerCanvas behavior (fixed sortingOrder = 100)


    private void CreateMainPanel()
    {
        // Main Panel Background
        _mainPanel = CreateUIObject("MainPanel", transform);
        var bg = _mainPanel.AddComponent<Image>();
        CreateGradientBackground(bg, new Color(0.05f, 0.05f, 0.08f, 0.92f), new Color(0.12f, 0.12f, 0.16f, 0.92f));
        SetAnchors(_mainPanel, Vector2.zero, Vector2.one);

        // 添加边框效果
        var mainOutline = _mainPanel.AddComponent<Outline>();
        mainOutline.effectColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);
        mainOutline.effectDistance = new Vector2(2, 2);

        // Content Panel
        var contentPanel = CreateUIObject("ContentPanel", _mainPanel.transform);
        SetAnchors(contentPanel, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        var contentBg = contentPanel.AddComponent<Image>();
        CreateGradientBackground(contentBg, new Color(0.12f, 0.12f, 0.16f, 0.95f),
            new Color(0.08f, 0.08f, 0.12f, 0.95f));

        // 添加内容面板边框
        var contentOutline = contentPanel.AddComponent<Outline>();
        contentOutline.effectColor = new Color(0.25f, 0.25f, 0.30f, 0.8f);
        contentOutline.effectDistance = new Vector2(1, 1);

        // Title Panel with enhanced styling
        var titlePanel = CreateUIObject("TitlePanel", contentPanel.transform);
        SetAnchors(titlePanel, new Vector2(0, 0.9f), new Vector2(1, 1));
        var titleBg = titlePanel.AddComponent<Image>();
        CreateGradientBackground(titleBg, new Color(0.20f, 0.20f, 0.25f, 0.9f), new Color(0.15f, 0.15f, 0.20f, 0.9f));

        var titleText = CreateText(titlePanel, L.UI.Title, 32);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.outlineWidth = 0.3f;
        titleText.outlineColor = new Color(0f, 0f, 0f, 0.7f);

        // Top Rolling Text
        CreateTopRollingText(contentPanel);

        // Horizontal Split Panel
        var horizontalPanel = CreateUIObject("HorizontalPanel", contentPanel.transform);
        SetAnchors(horizontalPanel, new Vector2(0, 0), new Vector2(1, 0.9f));
        var layout = horizontalPanel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // Left Group Panel
        CreateGroupPanel(horizontalPanel.transform);

        // Right Mod Panel
        CreateModPanel(horizontalPanel.transform);

        // Button Strip (Top Left)
        CreateButtonStrip(titlePanel.transform);

        // Danger Menu (Top Right)
        CreateDangerMenu(titlePanel.transform);
    }

    private void CreateTopRollingText(GameObject contentPanel)
    {
        var topRollingGO = CreateUIObject("TopRollingText", _mainPanel.transform);
        var topRect = topRollingGO.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.05f, 0.96f);
        topRect.anchorMax = new Vector2(0.65f, 1f);
        topRect.pivot = new Vector2(0f, 1f);
        topRect.anchoredPosition = Vector2.zero;
        topRect.offsetMin = new Vector2(10f, topRect.offsetMin.y);
        topRect.sizeDelta = new Vector2(0f, 16f);

        var topBgImg = topRollingGO.AddComponent<Image>();
        topBgImg.color = new Color(0f, 0f, 0f, 0f);

        var rolling = topRollingGO.AddComponent<RollingText>();
        rolling.Direction = RollingText.DirectionType.Up;
        rolling.Duration = 5f;
        rolling.Loop = true;
        rolling.Fade = true;
        rolling.MoveDistance = 16f;

        UpdateRollingTextAsync(rolling).Forget();
    }

    private void CreateGroupPanel(Transform parent)
    {
        var groupPanel = CreateUIObject("GroupPanel", parent);
        var groupPanelImage = groupPanel.AddComponent<Image>();
        CreateGradientBackground(groupPanelImage, new Color(0.18f, 0.18f, 0.22f, 0.95f),
            new Color(0.14f, 0.14f, 0.18f, 0.95f));

        // 添加面板边框和阴影
        var groupOutline = groupPanel.AddComponent<Outline>();
        groupOutline.effectColor = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        groupOutline.effectDistance = new Vector2(2, 2);

        var groupShadow = groupPanel.AddComponent<Shadow>();
        groupShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        groupShadow.effectDistance = new Vector2(3, -3);

        var groupLayoutElement = groupPanel.AddComponent<LayoutElement>();
        groupLayoutElement.preferredWidth = 300;
        groupLayoutElement.flexibleWidth = 0;

        // Toolbar
        var groupToolbar = CreateUIObject("GroupToolbar", groupPanel.transform);
        SetAnchors(groupToolbar, new Vector2(0, 0.92f), new Vector2(1, 1));
        var toolbarBg = groupToolbar.AddComponent<Image>();
        CreateGradientBackground(toolbarBg, new Color(0.25f, 0.25f, 0.30f, 0.9f), new Color(0.15f, 0.15f, 0.19f, 0.9f));

        var groupToolbarLayout = groupToolbar.AddComponent<HorizontalLayoutGroup>();
        groupToolbarLayout.spacing = 8f;
        groupToolbarLayout.padding = new RectOffset(8, 8, 5, 5);
        groupToolbarLayout.childForceExpandWidth = false;
        groupToolbarLayout.childForceExpandHeight = true;
        groupToolbarLayout.childAlignment = TextAnchor.MiddleLeft;

        var groupToolbarTitle = CreateText(groupToolbar, L.UI.GroupList, 20);
        groupToolbarTitle.alignment = TextAlignmentOptions.Left;
        groupToolbarTitle.fontStyle = FontStyles.Bold;
        groupToolbarTitle.outlineWidth = 0.2f;
        groupToolbarTitle.outlineColor = new Color(0f, 0f, 0f, 0.8f);

        // ScrollView
        var groupScrollView = CreateUIObject("GroupScrollView", groupPanel.transform);
        SetAnchors(groupScrollView, new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.91f));
        var groupScrollRect = groupScrollView.AddComponent<ScrollRect>();
        groupScrollRect.horizontal = false;
        groupScrollRect.vertical = true;
        groupScrollRect.scrollSensitivity = 3;
        _groupScrollViewRect = groupScrollView.GetComponent<RectTransform>();

        var groupViewport = CreateUIObject("Viewport", groupScrollView.transform);
        SetAnchors(groupViewport, Vector2.zero, Vector2.one);
        groupViewport.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.5f);
        groupViewport.AddComponent<RectMask2D>();
        groupScrollRect.viewport = groupViewport.GetComponent<RectTransform>();

        var groupContent = CreateUIObject("Content", groupViewport.transform);
        var groupContentRect = groupContent.GetComponent<RectTransform>();
        groupContentRect.anchorMin = new Vector2(0, 1);
        groupContentRect.anchorMax = new Vector2(1, 1);
        groupContentRect.pivot = new Vector2(0.5f, 1);
        groupContentRect.offsetMin = Vector2.zero;
        groupContentRect.offsetMax = Vector2.zero;
        groupScrollRect.content = groupContentRect;

        var groupLayout = groupContent.AddComponent<VerticalLayoutGroup>();
        groupLayout.spacing = 6f;
        groupLayout.padding = new RectOffset(0, 0, 10, 10);
        groupLayout.childForceExpandWidth = true;
        groupLayout.childForceExpandHeight = false;
        groupLayout.childControlWidth = true;
        groupLayout.childControlHeight = true;
        groupLayout.childScaleWidth = false;
        groupLayout.childScaleHeight = false;
        groupContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _groupContentTransform = groupContent.transform;

        // Footer (New Group)
        _groupFooterView = new GroupFooterEditorView(this, _groupScrollViewRect);
        _groupFooterView.OnCreateGroup += name => _vm.OnCreateGroup.OnNext(name);
        _groupFooterView.Build(groupPanel.transform);
    }

    private void CreateModPanel(Transform parent)
    {
        var modListPanel = CreateUIObject("ModListPanel", parent);
        var modListPanelImage = modListPanel.AddComponent<Image>();
        CreateGradientBackground(modListPanelImage, new Color(0.22f, 0.22f, 0.18f, 0.95f),
            new Color(0.16f, 0.16f, 0.14f, 0.95f));

        // 添加面板边框和阴影
        var modOutline = modListPanel.AddComponent<Outline>();
        modOutline.effectColor = new Color(0.2f, 0.25f, 0.15f, 0.8f);
        modOutline.effectDistance = new Vector2(2, 2);

        var modShadow = modListPanel.AddComponent<Shadow>();
        modShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        modShadow.effectDistance = new Vector2(3, -3);

        var modLayoutElement = modListPanel.AddComponent<LayoutElement>();
        modLayoutElement.flexibleWidth = 1;

        // Toolbar
        var modToolbar = CreateUIObject("ModToolbar", modListPanel.transform);
        SetAnchors(modToolbar, new Vector2(0, 0.92f), new Vector2(1, 1));
        var toolbarBg = modToolbar.AddComponent<Image>();
        CreateGradientBackground(toolbarBg, new Color(0.25f, 0.25f, 0.20f, 0.9f), new Color(0.19f, 0.19f, 0.15f, 0.9f));
        modToolbar.name = "ModToolbar"; // Ensure name for finding

        var modToolbarLayout = modToolbar.AddComponent<HorizontalLayoutGroup>();
        modToolbarLayout.spacing = 8f;
        modToolbarLayout.padding = new RectOffset(8, 8, 5, 5);
        modToolbarLayout.childForceExpandWidth = false;
        modToolbarLayout.childForceExpandHeight = true;
        modToolbarLayout.childAlignment = TextAnchor.MiddleLeft;

        var modToolbarPlaceholder = CreateText(modToolbar, L.UI.ModList, 20);
        modToolbarPlaceholder.alignment = TextAlignmentOptions.Left;
        modToolbarPlaceholder.fontStyle = FontStyles.Bold;
        modToolbarPlaceholder.outlineWidth = 0.2f;
        modToolbarPlaceholder.outlineColor = new Color(0f, 0f, 0f, 0.8f);

        // ScrollView
        var modScrollView = CreateUIObject("ModScrollView", modListPanel.transform);
        SetAnchors(modScrollView, new Vector2(0.02f, 0.02f), new Vector2(0.95f, 0.91f));
        _modScrollRect = modScrollView.AddComponent<ScrollRect>();
        _modScrollRect.horizontal = false;
        _modScrollRect.vertical = true;
        _modScrollRect.scrollSensitivity = 3f;

        // Scrollbar
        var modScrollbar = CreateUIObject("Scrollbar", modListPanel.transform);
        SetAnchors(modScrollbar, new Vector2(0.96f, 0.02f), new Vector2(0.98f, 0.91f));
        var modScrollbarImg = modScrollbar.AddComponent<Image>();
        modScrollbarImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var modScrollbarComp = modScrollbar.AddComponent<Scrollbar>();
        modScrollbarComp.direction = Scrollbar.Direction.BottomToTop;
        _modScrollRect.verticalScrollbar = modScrollbarComp;
        _modScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        var modScrollbarSliding = CreateUIObject("Sliding Area", modScrollbar.transform);
        SetAnchors(modScrollbarSliding, Vector2.zero, Vector2.one);
        modScrollbarSliding.GetComponent<RectTransform>().offsetMin = new Vector2(2, 2);
        modScrollbarSliding.GetComponent<RectTransform>().offsetMax = new Vector2(-2, -2);

        var modScrollbarHandle = CreateUIObject("Handle", modScrollbarSliding.transform);
        SetAnchors(modScrollbarHandle, Vector2.zero, Vector2.one);
        var modScrollbarHandleImg = modScrollbarHandle.AddComponent<Image>();
        modScrollbarHandleImg.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        modScrollbarComp.handleRect = modScrollbarHandle.GetComponent<RectTransform>();
        modScrollbarComp.targetGraphic = modScrollbarHandleImg;

        // Viewport
        var modViewport = CreateUIObject("Viewport", modScrollView.transform);
        var modViewportRect = modViewport.GetComponent<RectTransform>();
        modViewportRect.anchorMin = Vector2.zero;
        modViewportRect.anchorMax = Vector2.one;
        modViewportRect.offsetMin = Vector2.zero;
        modViewportRect.offsetMax = Vector2.zero;
        modViewport.AddComponent<RectMask2D>();
        _modScrollRect.viewport = modViewportRect;

        // Content
        var modContent = CreateUIObject("Content", modViewport.transform);
        var modContentRect = modContent.GetComponent<RectTransform>();
        modContentRect.anchorMin = new Vector2(0, 1);
        modContentRect.anchorMax = new Vector2(1, 1);
        modContentRect.pivot = new Vector2(0.5f, 1);
        modContentRect.offsetMin = Vector2.zero;
        modContentRect.offsetMax = Vector2.zero;
        _modScrollRect.content = modContentRect;

        var modLayout = modContent.AddComponent<VerticalLayoutGroup>();
        modLayout.spacing = 6f;
        modLayout.padding = new RectOffset(0, 0, 10, 10);
        modLayout.childForceExpandWidth = true;
        modLayout.childForceExpandHeight = false;
        modLayout.childControlWidth = true;
        modLayout.childControlHeight = true;
        modLayout.childScaleWidth = false;
        modLayout.childScaleHeight = false;
        modContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _modContentTransform = modContent.transform;
    }

    private void CreateButtonStrip(Transform parent)
    {
        var buttonStrip = CreateUIObject("ButtonStrip", parent);
        var bsRect = buttonStrip.GetComponent<RectTransform>();
        bsRect.anchorMin = new Vector2(0f, 0f);
        bsRect.anchorMax = new Vector2(0f, 1f);
        bsRect.pivot = new Vector2(0f, 0.5f);
        bsRect.anchoredPosition = new Vector2(10f, 0f);
        bsRect.sizeDelta = new Vector2(0f, 0f);

        var bsLayout = buttonStrip.AddComponent<HorizontalLayoutGroup>();
        bsLayout.spacing = 6f;
        bsLayout.padding = new RectOffset(8, 8, 5, 5);
        bsLayout.childForceExpandWidth = false;
        bsLayout.childForceExpandHeight = true;
        bsLayout.childControlWidth = true;
        bsLayout.childControlHeight = true;

        var bsFitter = buttonStrip.AddComponent<ContentSizeFitter>();
        bsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bsFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Back Button
        _closeBtnGO = CreateToolbarButton(buttonStrip.transform, L.UI.Back, L.Mod.ButtonBack, new Vector2(100, 40));

        // Developer Info
        var developerInfoBtn = CreateToolbarButton(buttonStrip.transform, L.UI.DeveloperInfo, L.UI.DeveloperInfo,
            new Vector2(120, 40));
        developerInfoBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
            DeveloperInfoPanel.Show();
        });

        // Developer Mode
        var developerModeBtn = CreateToolbarButton(buttonStrip.transform, L.Mod.ButtonDeveloperMode,
            L.Mod.ButtonDeveloperMode, new Vector2(120, 40));
        developerModeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
            try
            {
                LocalModPanelLayout.Show();
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to open Developer Mode panel");
            }
        });

        // Apply Changes
        _applyBtnGO = CreateToolbarButton(buttonStrip.transform, L.UI.ApplyChanges, L.Mod.ButtonApplyChanges,
            new Vector2(140, 40));
        _applyBtnGO.SetActive(false);
        var applyBtn = _applyBtnGO.GetComponent<Button>();
        var applyBtnImg = _applyBtnGO.GetComponent<Image>();

        // 使用UIResourceManager增强按钮样式
        var successBgSprite = UIResourceManager.GetSprite("success_bg");
        applyBtnImg.sprite = successBgSprite;
        applyBtnImg.type = Image.Type.Sliced;
        applyBtnImg.color = Color.white;

        // Discard Changes
        _discardBtnGO = CreateToolbarButton(buttonStrip.transform, L.UI.CancelAndExit, L.Mod.ButtonCancelAndExit,
            new Vector2(160, 40));
        _discardBtnGO.SetActive(false);
        var discardBtn = _discardBtnGO.GetComponent<Button>();
        var discardBtnImg = _discardBtnGO.GetComponent<Image>();

        // 使用UIResourceManager增强按钮样式
        var errorBgSprite = UIResourceManager.GetSprite("error_bg");
        discardBtnImg.sprite = errorBgSprite;
        discardBtnImg.type = Image.Type.Sliced;
        discardBtnImg.color = Color.white;
    }

    private void CreateDangerMenu(Transform parent)
    {
        var menuBtn = CreateButton(parent, L.UI.DangerIcon, L.Mod.ButtonDanger, new Vector2(0.95f, 0.5f),
            new Vector2(50, 50));
        var menuBtnImg = menuBtn.GetComponent<Image>();
        var menuBtnComponent = menuBtn.GetComponent<Button>();

        // 使用UIResourceManager增强危险按钮样式
        var errorBgSprite = UIResourceManager.GetSprite("error_bg");
        menuBtnImg.sprite = errorBgSprite;
        menuBtnImg.type = Image.Type.Sliced;
        menuBtnImg.color = Color.white;

        var menuColors = menuBtnComponent.colors;
        menuColors.normalColor = Color.white;
        menuColors.highlightedColor = new Color(1f, 0.9f, 0.9f, 1f);
        menuColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        menuBtnComponent.colors = menuColors;

        // 添加边框和阴影效果
        var menuOutline = menuBtn.AddComponent<Outline>();
        menuOutline.effectColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        menuOutline.effectDistance = new Vector2(2, 2);

        var menuShadow = menuBtn.AddComponent<Shadow>();
        menuShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        menuShadow.effectDistance = new Vector2(2, -2);

        var menuText = menuBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (menuText != null)
        {
            menuText.text = L.UI.DangerIcon;
            menuText.fontStyle = FontStyles.Bold;
            menuText.outlineWidth = 0.3f;
            menuText.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        }

        _dropdownMenuView = new DropdownMenuView(this, _canvas);
        _dropdownMenuView.OnRevertRequested += () => _vm.OnRevertRequested.OnNext(Unit.Default);
        // Use _canvas.transform as parent to ensure it's on top of everything
        menuBtnComponent.onClick.AddListener(() => _dropdownMenuView.Toggle(_canvas.transform));
    }

    private void BindViewModel()
    {
        // Bind Groups List - rebuild when signaled (for create/reorder operations)
        _vm.OnGroupListChanged.Subscribe(_ => RebuildGroupList()).AddTo(ref _disposableBag);

        // Bind Mods List - rebuild on any structural change
        Observable.Merge(
                _vm.CurrentGroupMods.ObserveAdd().AsUnitObservable(),
                _vm.CurrentGroupMods.ObserveRemove().AsUnitObservable(),
                _vm.CurrentGroupMods.ObserveReplace().AsUnitObservable(),
                _vm.CurrentGroupMods.ObserveReset().AsUnitObservable(),
                _vm.CurrentGroupMods.ObserveMove().AsUnitObservable()
            )
            .Where(_ => !_vm.IsRefreshing)
            .Subscribe(_ => RebuildModList())
            .AddTo(ref _disposableBag);

        // Also rebuild when batch refresh completes
        _vm.OnModListRefreshed.Subscribe(_ => RebuildModList()).AddTo(ref _disposableBag);

        // Bind Selection
        _vm.SelectedGroup.Subscribe(g => UpdateGroupSelectionVisuals(g)).AddTo(ref _disposableBag);

        // Bind Dirty State
        _vm.IsDirty.Subscribe(dirty =>
        {
            _applyBtnGO.SetActive(dirty);
            _discardBtnGO.SetActive(dirty);
            _closeBtnGO.SetActive(!dirty);
        }).AddTo(ref _disposableBag);

        // Bind Buttons with Audio Feedback
        _applyBtnGO.GetComponent<Button>().OnClickAsObservable().Subscribe(_ =>
        {
            BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
            _vm.OnApplyChanges.OnNext(Unit.Default);
        }).AddTo(ref _disposableBag);
        _discardBtnGO.GetComponent<Button>().OnClickAsObservable()
            .Subscribe(_ =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                _vm.OnDiscardChanges.OnNext(Unit.Default);
            }).AddTo(ref _disposableBag);
        _closeBtnGO.GetComponent<Button>().OnClickAsObservable().Subscribe(_ =>
        {
            BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
            _vm.RequestClose.OnNext(Unit.Default);
        }).AddTo(ref _disposableBag);

        // Close Request
        _vm.RequestClose.Subscribe(_ => Destroy(gameObject)).AddTo(ref _disposableBag);

        // Manually trigger initial population
        RebuildGroupList();
        if (_vm.SelectedGroup.Value != null)
        {
            RebuildModList();
            UpdateGroupSelectionVisuals(_vm.SelectedGroup.Value);
        }

        // Pre-create all mod UI items asynchronously for fast switching
        PreCreateAllModItemsAsync().Forget();
    }

    private async UniTask PreCreateAllModItemsAsync()
    {
        // Collect all unique mods from all groups
        var allMods = _vm.Groups
            .SelectMany(g => g.Mods)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .ToList();

        if (allMods.Count == 0)
        {
            Log.Current.Debug("[BML] No mods to pre-create");
            return;
        }

        Log.Current.Debug($"[BML] Starting async pre-creation for {allMods.Count} unique mods...");

        // Use a representative group's lock state (or false if no groups)
        var isGroupLocked = false;

        await _modListView.PreCreateAllModItemsAsync(
            allMods,
            _hiddenContainer,
            isGroupLocked,
            OpenSteamWorkshopPage,
            OpenLocalFolder
        );

        Log.Current.Debug("[BML] Pre-creation completed, switching should now be faster");
    }

    private void SubscribeToDragEvents()
    {
        // Subscribe to drag-and-drop events
        BmlDraggableItem.OnModDroppedOnGroup += OnModDroppedOnGroupHandler;
        BmlDraggableItem.OnItemReordered += OnItemReorderedHandler;
    }

    private void OnModDroppedOnGroupHandler(BmlDraggableItem modItem, BmlDraggableItem groupItem)
    {
        if (modItem == null || groupItem == null) return;
        var modId = modItem.DataId;
        var groupName = groupItem.DataId;

        Log.Debug($"[BML] Mod '{modId}' dropped on group '{groupName}'");
        _vm.MoveMod(modId, groupName);

        // Refresh group list to update counts
        RebuildGroupList();
    }

    private void OnItemReorderedHandler(BmlDraggableItem item, int oldIndex, int newIndex)
    {
        if (item == null) return;

        Log.Debug($"[BML] ===== OnItemReorderedHandler START =====");
        Log.Debug($"[BML] Item ID: '{item.DataId}', Type: '{item.ItemType}'");
        Log.Debug($"[BML] Old Index: {oldIndex}, New Index: {newIndex}");

        if (item.ItemType == "group")
        {
            Log.Debug($"[BML] Calling ReorderGroup for '{item.DataId}'");
            _vm.ReorderGroup(item.DataId, oldIndex, newIndex);
        }
        else if (item.ItemType == "mod")
        {
            var group = _vm.SelectedGroup.Value;
            if (group != null)
            {
                Log.Debug($"[BML] Current group: '{group.Name}', Mod count: {group.Mods.Count}");
                Log.Debug($"[BML] Calling ReorderMod for '{item.DataId}' to index {newIndex}");
                _vm.ReorderMod(item.DataId, newIndex);
            }
            else
            {
                Log.Warn($"[BML] No group selected, cannot reorder mod");
            }
        }

        Log.Debug($"[BML] ===== OnItemReorderedHandler END =====");
    }

    private void RebuildGroupList()
    {
        // Clear all children
        foreach (Transform child in _groupContentTransform) Destroy(child.gameObject);

        // Split groups: Top vs Others
        var allGroups = _vm.Groups.ToList();
        var topGroups = allGroups.Where(g => g.Tags != null && g.Tags.Contains(ModGroupTags.Top)).ToList();
        var otherGroups = allGroups.Where(g => !(g.Tags != null && g.Tags.Contains(ModGroupTags.Top))).ToList();

        // Section: Top (Preset Groups)
        if (topGroups.Count > 0)
        {
            var topHeader = CreateUIObject("TopHeader", _groupContentTransform);
            var topHeaderText = topHeader.AddComponent<TextMeshProUGUI>();
            topHeaderText.text = L.Group.ListTopHeader ?? "预设分组";
            topHeaderText.fontSize = 18;
            topHeaderText.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            topHeaderText.alignment = TextAlignmentOptions.Left;
            var topHeaderLE = topHeader.AddComponent<LayoutElement>();
            topHeaderLE.preferredHeight = 24;

            var topSection = CreateUIObject("TopSection", _groupContentTransform);
            var topLayout = topSection.AddComponent<VerticalLayoutGroup>();
            topLayout.spacing = 6f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = false;
            topSection.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (var i = 0; i < topGroups.Count; i++)
            {
                CreateGroupItem(topGroups[i], topSection.transform, i);
            }
        }

        // Section: Others (Custom Groups)
        if (otherGroups.Count > 0)
        {
            var otherHeader = CreateUIObject("OtherHeader", _groupContentTransform);
            var otherHeaderText = otherHeader.AddComponent<TextMeshProUGUI>();
            otherHeaderText.text = L.Group.ListOtherHeader ?? "自定义分组";
            otherHeaderText.fontSize = 18;
            otherHeaderText.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            otherHeaderText.alignment = TextAlignmentOptions.Left;
            var otherHeaderLE = otherHeader.AddComponent<LayoutElement>();
            otherHeaderLE.preferredHeight = 24;

            var otherSection = CreateUIObject("OtherSection", _groupContentTransform);
            var otherLayout = otherSection.AddComponent<VerticalLayoutGroup>();
            otherLayout.spacing = 6f;
            otherLayout.childControlWidth = true;
            otherLayout.childControlHeight = true;
            otherLayout.childForceExpandWidth = true;
            otherLayout.childForceExpandHeight = false;
            otherSection.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (var i = 0; i < otherGroups.Count; i++)
            {
                CreateGroupItem(otherGroups[i], otherSection.transform, i);
            }
        }
    }

    private void RebuildModList()
    {
        // Collect existing items for recycling
        var itemsToRecycle = new List<GameObject>(_modItemObjects.Values);
        _modItemObjects.Clear();

        var group = _vm.SelectedGroup.Value;
        if (group == null)
        {
            // Just recycle everything if no group selected
            foreach (var item in itemsToRecycle)
            {
                item.SetActive(false);
                Destroy(item); // Or push to pool if we had access, but BmlModListView handles pooling internally if passed
            }

            // Actually BmlModListView.BuildModList handles recycling.
            // But if we have no group, we might want to clear.
            // Let's just call BuildModList with empty list to recycle.
            _modListView.BuildModList(
                _modContentTransform,
                Enumerable.Empty<ModInfo2>(),
                itemsToRecycle,
                false,
                null,
                null
            );
            return;
        }

        var isLocked = group.Tags != null && group.Tags.Contains(ModGroupTags.Locked);

        _modItemObjects = _modListView.BuildModList(
            _modContentTransform,
            _vm.CurrentGroupMods,
            itemsToRecycle,
            isLocked,
            OpenSteamWorkshopPage,
            OpenLocalFolder
        );

        // Toolbar Buttons
        var modToolbar = _mainPanel.transform.Find("ContentPanel/HorizontalPanel/ModListPanel/ModToolbar");
        if (modToolbar != null)
        {
            _modListView.ShowEnableDisableAllButtons(
                modToolbar,
                _vm.CurrentGroupMods,
                isLocked,
                () =>
                {
                    // Skip visual refresh to avoid animation conflicts
                    // The R3 system will handle state changes through individual mod updates
                    // This prevents unnecessary visual updates that cause animation flicker
                }
            );

            _modListView.ShowDeleteGroupButtonIfNeeded(modToolbar, group, g =>
            {
                // Delete group logic
                _vm.DeleteGroup(g);
            });
        }
    }

    private void CreateGroupItem(ModGroup group, Transform parent, int dataIndex)
    {
        var item = CreateUIObject($"Group_{group.Name}", parent);
        var img = item.AddComponent<Image>();
        CreateGradientBackground(img, new Color(0.22f, 0.22f, 0.25f, 1f), new Color(0.18f, 0.18f, 0.22f, 1f));

        // 添加边框和阴影效果
        var itemOutline = item.AddComponent<Outline>();
        itemOutline.effectColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        itemOutline.effectDistance = new Vector2(1, 1);

        var itemShadow = item.AddComponent<Shadow>();
        itemShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        itemShadow.effectDistance = new Vector2(2, -2);

        var layout = item.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 10, 5, 5); // No left padding - stripe will provide spacing
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // Add left stripe for selection indicator (like LocalModListItemUI)
        // This must be the FIRST child to appear on the left
        var stripe = CreateUIObject("LeftStripe", item.transform);
        var stripeImg = stripe.AddComponent<Image>();
        stripeImg.color = new Color(0f, 0f, 0f, 0f); // Transparent by default
        var stripeLayout = stripe.AddComponent<LayoutElement>();
        stripeLayout.preferredWidth = 4f;
        stripeLayout.minWidth = 4f;
        stripeLayout.flexibleWidth = 0;
        stripeLayout.preferredHeight = -1; // Use parent height

        // Ensure stripe is first child
        stripe.transform.SetAsFirstSibling();

        var text = CreateText(item, $"{group.Name} ({group.Mods.Count})", 18);
        var textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1; // Text takes remaining space
        text.fontStyle = FontStyles.Bold;
        text.outlineWidth = 0.15f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.7f);

        var btn = item.AddComponent<Button>();
        var btnColors = btn.colors;
        btnColors.normalColor = new Color(1f, 1f, 1f, 0f);
        btnColors.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
        btnColors.pressedColor = new Color(1f, 1f, 1f, 0.1f);
        btn.colors = btnColors;
        // Note: Button click is now handled by PointerClickRelay.OnSingleClick to avoid conflicts

        item.AddComponent<LayoutElement>().preferredHeight = 40;

        var draggable = item.AddComponent<BmlDraggableItem>();
        draggable.Init(this, "group", group.Name, dataIndex);

        // Add double-click detection for rename
        var clickRelay = item.AddComponent<PointerClickRelay>();
        clickRelay.OnSingleClick = () => _vm.SelectedGroup.Value = group;
        clickRelay.OnDoubleClick = () => StartGroupRename(group, item);
    }

    /// <summary>
    /// 开始重命名分组
    /// </summary>
    private void StartGroupRename(ModGroup group, GameObject groupItemGO)
    {
        // Cancel any existing rename operation
        CancelGroupRename();

        _renamingGroup = group;
        CreateRenameInput(group, groupItemGO);
    }

    /// <summary>
    /// 创建重命名输入框
    /// </summary>
    private void CreateRenameInput(ModGroup group, GameObject parentGO)
    {
        // Find the text object to position the input field over it
        var textObj = parentGO.transform.GetComponentInChildren<TextMeshProUGUI>();
        if (textObj == null) return;

        // Create input field container
        _renameInputGO = CreateUIObject("RenameInput", parentGO.transform);

        // Position it exactly over the text
        var rectTransform = _renameInputGO.GetComponent<RectTransform>();
        var sourceTextRect = textObj.GetComponent<RectTransform>();

        // Copy position and size from text
        rectTransform.anchorMin = sourceTextRect.anchorMin;
        rectTransform.anchorMax = sourceTextRect.anchorMax;
        rectTransform.anchoredPosition = sourceTextRect.anchoredPosition;
        rectTransform.sizeDelta = sourceTextRect.sizeDelta;
        rectTransform.pivot = sourceTextRect.pivot;
        rectTransform.rotation = sourceTextRect.rotation;
        rectTransform.localScale = sourceTextRect.localScale;

        // Set as last sibling to appear on top
        _renameInputGO.transform.SetAsLastSibling();

        // Create input field
        _renameInputField = _renameInputGO.AddComponent<TMP_InputField>();

        // Create text viewport and text components
        var viewportGO = CreateUIObject("Viewport", _renameInputGO.transform);
        var viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var textGO = CreateUIObject("Text", viewportGO.transform);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 0); // Small padding
        textRect.offsetMax = new Vector2(-4, 0);

        var inputText = textGO.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = textObj.fontSize;
        inputText.fontStyle = textObj.fontStyle;
        inputText.alignment = textObj.alignment;
        inputText.color = Color.white;

        // Configure input field
        _renameInputField.textViewport = viewportRect;
        _renameInputField.textComponent = inputText;
        _renameInputField.text = group.Name;
        // Note: fontSize and fontStyle are set on the textComponent, not the input field

        // Style the input field background
        var backgroundImage = _renameInputGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        // Add visual feedback
        var outline = _renameInputGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        // Select all text and focus
        _renameInputField.Select();
        _renameInputField.ActivateInputField();

        // Wire up events
        _renameInputField.onSubmit.AddListener(OnRenameSubmitted);
        _renameInputField.onDeselect.AddListener(OnRenameDeselected);
    }

    /// <summary>
    /// 处理重命名提交
    /// </summary>
    private void OnRenameSubmitted(string newName)
    {
        if (_renamingGroup != null)
        {
            _vm.OnRenameGroup.OnNext((_renamingGroup, newName));
        }

        CleanupRenameInput();
    }

    /// <summary>
    /// 处理输入框失去焦点
    /// </summary>
    private void OnRenameDeselected(string text)
    {
        // Delay cleanup to allow for other interactions
        StartCoroutine(DelayedRenameCleanup());
    }

    /// <summary>
    /// 延迟清理重命名输入框
    /// </summary>
    private IEnumerator<object> DelayedRenameCleanup()
    {
        yield return new WaitForSeconds(0.1f);

        // Check if input field is still valid and not focused
        if (_renameInputField != null && !_renameInputField.isFocused)
        {
            CleanupRenameInput();
        }
    }

    /// <summary>
    /// 取消分组重命名
    /// </summary>
    private void CancelGroupRename()
    {
        if (_renamingGroup != null)
        {
            _vm.OnCancelRename.OnNext(_renamingGroup);
        }

        CleanupRenameInput();
    }

    /// <summary>
    /// 清理重命名输入框
    /// </summary>
    private void CleanupRenameInput()
    {
        if (_renameInputField != null)
        {
            _renameInputField.onSubmit.RemoveListener(OnRenameSubmitted);
            _renameInputField.onDeselect.RemoveListener(OnRenameDeselected);
            _renameInputField = null;
        }

        if (_renameInputGO != null)
        {
            Destroy(_renameInputGO);
            _renameInputGO = null;
        }

        _renamingGroup = null;
    }

    private void UpdateGroupSelectionVisuals(ModGroup selectedGroup)
    {
        // Search in both TopSection and OtherSection
        var topSection = _groupContentTransform.Find("TopSection");
        var otherSection = _groupContentTransform.Find("OtherSection");

        void UpdateSection(Transform section)
        {
            if (section == null) return;

            foreach (Transform child in section)
            {
                var isSelected = child.name == $"Group_{selectedGroup?.Name}";
                var itemImg = child.GetComponent<Image>();

                // Update item background for selected state
                if (itemImg != null)
                {
                    if (isSelected)
                    {
                        // Selected: enhanced gradient with blue tint
                        CreateGradientBackground(itemImg,
                            new Color(0.30f, 0.45f, 0.60f, 1f),
                            new Color(0.20f, 0.35f, 0.50f, 1f));

                        // Enhanced selection outline
                        var outline = child.GetComponent<Outline>();
                        if (outline != null)
                        {
                            outline.effectColor = new Color(0.25f, 0.55f, 0.85f, 1f);
                            outline.effectDistance = new Vector2(2, 2);
                        }

                        // Add glow effect for selected item
                        var glow = child.GetComponent<Shadow>();
                        if (glow != null)
                        {
                            glow.effectColor = new Color(0.3f, 0.6f, 0.9f, 0.4f);
                            glow.effectDistance = new Vector2(4, 4);
                        }
                    }
                    else
                    {
                        // Not selected: restore default gradient
                        CreateGradientBackground(itemImg,
                            new Color(0.22f, 0.22f, 0.25f, 1f),
                            new Color(0.18f, 0.18f, 0.22f, 1f));

                        // Reset outline to default
                        var outline = child.GetComponent<Outline>();
                        if (outline != null)
                        {
                            outline.effectColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
                            outline.effectDistance = new Vector2(1, 1);
                        }

                        // Reset shadow to default
                        var shadow = child.GetComponent<Shadow>();
                        if (shadow != null)
                        {
                            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
                            shadow.effectDistance = new Vector2(2, -2);
                        }
                    }
                }

                // Find the LeftStripe child
                var stripe = child.Find("LeftStripe");
                if (stripe != null)
                {
                    var stripeImg = stripe.GetComponent<Image>();
                    if (stripeImg != null)
                    {
                        // Selected: blue stripe with glow, Not selected: transparent
                        stripeImg.color = isSelected
                            ? new Color(0.35f, 0.65f, 0.95f, 1f) // Bright blue (selected)
                            : new Color(0f, 0f, 0f, 0f); // Transparent (not selected)
                    }
                }
            }
        }

        UpdateSection(topSection);
        UpdateSection(otherSection);
    }

    // Helpers
    public ScrollRect GetModScrollRect() => _modScrollRect;
    public BmlModManagerUIVm GetVm() => _vm;

    private GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        return go;
    }

    private void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateText(GameObject parent, string content, int fontSize)
    {
        var go = CreateUIObject("Text", parent.transform);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Left;
        SetAnchors(go, Vector2.zero, Vector2.one);
        return txt;
    }

    private GameObject CreateButton(Transform parent, string label, string text, Vector2 anchorPos, Vector2 size)
    {
        var btnObj = CreateUIObject("Button_" + label, parent);
        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

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
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(textObj, Vector2.zero, Vector2.one);

        return btnObj;
    }

    private GameObject CreateToolbarButton(Transform parent, string label, string text, Vector2 size)
    {
        var btnObj = CreateUIObject("ToolbarButton_" + label, parent);
        var rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        var img = btnObj.AddComponent<Image>();

        // 统一使用 PanelBase 的按钮样式系统
        if (label == L.UI.Back)
        {
            // 使用与 LocalModPanelLayout 一致的天蓝色主题
            img.color = new Color(0.4f, 0.7f, 1.0f, 1f); // SkyBlue 主题的 normalColor
        }
        else
        {
            img.color = new Color(0.2f, 0.4f, 0.6f, 1f);
        }

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        if (label == L.UI.Back)
        {
            // SkyBlue 主题颜色，与 LocalModPanelLayout 保持一致
            colors.normalColor = new Color(0.4f, 0.7f, 1.0f, 1f);
            colors.highlightedColor = new Color(0.5f, 0.8f, 1.0f, 1f);
            colors.pressedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            colors.selectedColor = new Color(0.45f, 0.75f, 0.95f, 1f);
        }
        else
        {
            colors.normalColor = new Color(0.2f, 0.4f, 0.6f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.5f, 0.75f, 1f);
            colors.pressedColor = new Color(0.15f, 0.35f, 0.55f, 1f);
        }

        // 统一设置禁用状态为红色
        colors.disabledColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.fadeDuration = 0.1f;

        btn.colors = colors;

        var textObj = CreateUIObject("Text", btnObj.transform);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(textObj, Vector2.zero, Vector2.one);

        // 为返回按钮添加边框效果，与其他按钮保持一致
        if (label == L.UI.Back)
        {
            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.7f, 1.0f, 1f); // SkyBlue 主题边框颜色
            outline.effectDistance = new Vector2(2, 2);

            var shadow = btnObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        return btnObj;
    }

    private UniTask UpdateRollingTextAsync(RollingText rolling)
    {
        var superDuckyAnnouncement = new RollingText.RollingItem(
            "功能更强的启动器「超级鸭鸭」已开始公测！点击查看详情",
            RollingText.MessageLevel.Success);
        rolling.AddItem(superDuckyAnnouncement);

        rolling.StartRolling();
        rolling.OnItemClicked += (idx, item) =>
        {
            if (item == superDuckyAnnouncement)
            {
                Application.OpenURL("https://ducky7go.github.io/SuperDucky.docs/");
                return;
            }
        };

        return UniTask.CompletedTask;
    }

    private void OpenSteamWorkshopPage(ulong publishedFileId)
    {
        if (publishedFileId == 0) return;
        var url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={publishedFileId}";
        try
        {
            SteamFriends.ActivateGameOverlayToWebPage(url);
        }
        catch (Exception ex)
        {
            Log.Error($"[BML] Failed to open Steam page: {ex.Message}");
        }
    }

    private bool OpenLocalFolder(string folderPath)
    {
        try
        {
            if (!string.IsNullOrEmpty(folderPath))
            {
                var targetPath = folderPath;
                if (!Directory.Exists(targetPath))
                {
                    var dir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(dir)) targetPath = dir;
                }

                if (!string.IsNullOrEmpty(targetPath))
                {
                    var url = targetPath.StartsWith("/") ? $"file://{targetPath}" : targetPath;
                    Application.OpenURL(url);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[BML] Failed to open local folder: {ex.Message}");
        }

        return false;
    }

    private void OnDestroy()
    {
        // Cancel any ongoing rename operation
        CleanupRenameInput();

        // Unsubscribe from drag events
        BmlDraggableItem.OnModDroppedOnGroup -= OnModDroppedOnGroupHandler;
        BmlDraggableItem.OnItemReordered -= OnItemReorderedHandler;

        _disposableBag.Dispose();
        _vm?.Dispose();
    }

    /// <summary>
    /// 创建渐变背景
    /// </summary>
    /// <param name="image">要设置渐变的Image组件</param>
    /// <param name="topColor">顶部颜色</param>
    /// <param name="bottomColor">底部颜色</param>
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
}
