using System.Collections;
using System.Text;
using Newbe.BetterModLoader.Audio;
using static Newbe.BetterModLoader.UI.UIFactory;

namespace Newbe.BetterModLoader.Main;

/// <summary>
/// Builds and refreshes the Mods list UI for a selected group.
/// Adapted from ModListView for BmlModManagerUIView.
/// </summary>
internal class BmlModListView
{
    private readonly MonoBehaviour _host;
    private readonly BmlModManagerUIVm _vm;

    public BmlModListView(MonoBehaviour host, BmlModManagerUIVm vm)
    {
        _host = host;
        _vm = vm;
    }

    public void ShowDeleteGroupButtonIfNeeded(Transform toolbarParent, ModGroup selectedGroup,
        Action<ModGroup> onDelete)
    {
        foreach (Transform child in toolbarParent)
        {
            if (child.name.StartsWith("ToolbarButton_DeleteGroup"))
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        if (selectedGroup.Mods.Count == 0 && !(selectedGroup.Tags?.Contains(ModGroupTags.Locked) ?? false))
        {
            var deleteBtn = CreateToolbarButton(toolbarParent, "DeleteGroup", new Vector2(100, 40));
            var tmp = deleteBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = L.Group.DeleteGroupButton ?? "删除分组";
                tmp.fontStyle = FontStyles.Bold;
                tmp.outlineWidth = 0.2f;
                tmp.outlineColor = new Color(0f, 0f, 0f, 0.8f);
            }

            // 美化删除按钮样式
            var deleteBtnImg = deleteBtn.GetComponent<Image>();
            var deleteSprite = UIResourceManager.GetSprite("error_bg");
            deleteBtnImg.sprite = deleteSprite;
            deleteBtnImg.type = Image.Type.Sliced;
            deleteBtnImg.color = Color.white;

            var deleteBtnComp = deleteBtn.GetComponent<Button>();
            var deleteColors = deleteBtnComp.colors;
            deleteColors.normalColor = Color.white;
            deleteColors.highlightedColor = new Color(1f, 0.9f, 0.9f, 1f);
            deleteColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            deleteBtnComp.colors = deleteColors;

            // 添加边框效果
            var deleteOutline = deleteBtn.AddComponent<Outline>();
            deleteOutline.effectColor = new Color(0.8f, 0.1f, 0.1f, 1f);
            deleteOutline.effectDistance = new Vector2(2, 2);

            var btn = deleteBtn.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                onDelete(selectedGroup);
            });
        }
    }

    public void ShowEnableDisableAllButtons(Transform toolbarParent,
        IEnumerable<ModInfo2> mods,
        bool isGroupLocked,
        Action? onRequestRebuild)
    {
        var list = mods.ToList();

        foreach (Transform child in toolbarParent)
        {
            if (child.name.StartsWith("ToolbarButton_EnableAll") || child.name.StartsWith("ToolbarButton_DisableAll"))
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        if (list.Count < 1) return;

        var anyEnabled = list.Any(m => m.IsEnabled);
        var anyDisabled = list.Any(m => !m.IsEnabled);

        Button enableAllButton = null!;
        Button disableAllButton = null!;

        // Enable All
        var enableAllBtn = CreateToolbarButton(toolbarParent, "EnableAll", new Vector2(120, 40));
        var enableAllText = enableAllBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (enableAllText != null)
        {
            var label = L.Mod.ButtonEnableAll;
            enableAllText.text = string.IsNullOrEmpty(label) ? "启用全部" : label;
            enableAllText.fontStyle = FontStyles.Bold;
            enableAllText.outlineWidth = 0.2f;
            enableAllText.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        }

        // 美化启用全部按钮样式
        var enableAllBtnImg = enableAllBtn.GetComponent<Image>();
        var successSprite = UIResourceManager.GetSprite("success_bg");
        enableAllBtnImg.sprite = successSprite;
        enableAllBtnImg.type = Image.Type.Sliced;
        enableAllBtnImg.color = Color.white;

        enableAllButton = enableAllBtn.GetComponent<Button>();
        if (isGroupLocked)
        {
            enableAllButton.interactable = false;
        }
        else
        {
            enableAllButton.interactable = anyDisabled;
            enableAllButton.onClick.AddListener(() =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                _vm.EnableAllModsInCurrentGroup();
                enableAllButton.interactable = false;
                disableAllButton.interactable = true;
                onRequestRebuild?.Invoke();
            });
        }

        // Disable All
        var disableAllBtn = CreateToolbarButton(toolbarParent, "DisableAll", new Vector2(120, 40));
        var disableAllText = disableAllBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (disableAllText != null)
        {
            var label = L.Mod.ButtonDisableAll;
            disableAllText.text = string.IsNullOrEmpty(label) ? "禁用全部" : label;
            disableAllText.fontStyle = FontStyles.Bold;
            disableAllText.outlineWidth = 0.2f;
            disableAllText.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        }

        // 美化禁用全部按钮样式
        var disableAllBtnImg = disableAllBtn.GetComponent<Image>();
        var warningSprite = UIResourceManager.GetSprite("warning_bg");
        disableAllBtnImg.sprite = warningSprite;
        disableAllBtnImg.type = Image.Type.Sliced;
        disableAllBtnImg.color = Color.white;

        disableAllButton = disableAllBtn.GetComponent<Button>();
        if (isGroupLocked)
        {
            disableAllButton.interactable = false;
        }
        else
        {
            disableAllButton.interactable = anyEnabled;
            disableAllButton.onClick.AddListener(() =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                _vm.DisableAllModsInCurrentGroup();
                disableAllButton.interactable = false;
                enableAllButton.interactable = true;
                onRequestRebuild?.Invoke();
            });
        }
    }

    private readonly Stack<GameObject> _pool = new();

    // Pre-creation cache: ModId -> GameObject
    private readonly Dictionary<string, GameObject> _allModItems = new();
    private Transform _hiddenContainer;
    private bool _isPreCreating = false;

    /// <summary>
    /// Pre-create all mod UI items asynchronously for fast switching.
    /// Should be called once during initialization.
    /// </summary>
    public async UniTask PreCreateAllModItemsAsync(
        IEnumerable<ModInfo2> allMods,
        Transform hiddenContainer,
        bool isGroupLocked,
        Action<ulong> openSteam,
        Func<string, bool> openLocal)
    {
        if (_isPreCreating) return;
        _isPreCreating = true;
        _hiddenContainer = hiddenContainer;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var modList = allMods.ToList();
        var created = 0;

        Log.Debug($"[BML] Starting pre-creation of {modList.Count} mod UI items...");

        foreach (var mod in modList)
        {
            if (_allModItems.ContainsKey(mod.Id))
            {
                continue; // Already created
            }

            // Create the UI structure
            var go = CreateItemStructure(_hiddenContainer);

            // Update with mod data (index doesn't matter for pre-creation, will be set during mounting)
            UpdateItem(go, mod, 0, modList.Count, isGroupLocked, openSteam, openLocal);

            // Hide it initially
            go.SetActive(false);

            // Cache it
            _allModItems[mod.Id] = go;
            created++;

            // Yield every 10 items to avoid blocking
            if (created % 10 == 0)
            {
                await UniTask.Yield();
            }
        }

        sw.Stop();
        Log.Debug($"[BML] Pre-creation completed: {created} items created in {sw.ElapsedMilliseconds}ms");
        _isPreCreating = false;
    }

    public Dictionary<string, GameObject> BuildModList(
        Transform contentParent,
        IEnumerable<ModInfo2> mods,
        List<GameObject> itemsToRecycle,
        bool isGroupLocked,
        Action<ulong> openSteam,
        Func<string, bool> openLocal)
    {
        var scrollRect = contentParent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null) scrollRect.scrollSensitivity = 3f;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var mountedCount = 0;
        var recycledCount = 0;
        var createdCount = 0;

        // Recycle existing items (hide them, don't destroy if they're in cache)
        foreach (var item in itemsToRecycle)
        {
            if (item == null) continue;

            // If it's a cached item, move it to hidden container
            var isCached = false;
            foreach (var kvp in _allModItems)
            {
                if (kvp.Value == item)
                {
                    item.SetActive(false);
                    if (_hiddenContainer != null)
                    {
                        item.transform.SetParent(_hiddenContainer, false);
                    }

                    isCached = true;
                    break;
                }
            }

            // If not cached, add to pool
            if (!isCached)
            {
                item.SetActive(false);
                _pool.Push(item);
            }
        }

        var map = new Dictionary<string, GameObject>();
        var list = mods.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            var mod = list[i];
            GameObject go;

            // Try to use pre-created item first
            if (_allModItems.TryGetValue(mod.Id, out var cachedGo))
            {
                go = cachedGo;
                go.SetActive(true);
                go.transform.SetParent(contentParent, false);
                go.transform.SetAsLastSibling();
                mountedCount++;

                // Update toggle visual in case state changed, but skip animation during bulk refresh
                // Only update if item was newly created (not recycled), to avoid visual flicker
                ApplyToggleVisualToItem(go, mod.IsEnabled, animate: false, forceUpdate: false);

                // Update draggable index
                var draggable = go.GetComponent<BmlDraggableItem>();
                if (draggable != null && draggable.enabled)
                {
                    draggable.Init((BmlModManagerUIView)_host, "mod", mod.Id, i);
                }

                // Update sort buttons with correct index
                var titleRow = go.transform.Find("InfoContainer/TitleRow");
                if (titleRow != null)
                {
                    var upBtnObj = titleRow.Find("IconButton_↑");
                    var downBtnObj = titleRow.Find("IconButton_↓");
                    if (upBtnObj != null && downBtnObj != null)
                    {
                        UpdateSortButtons(upBtnObj, downBtnObj, mod, i, list.Count, isGroupLocked, go);
                    }
                }
            }
            // Fallback to pool or creation
            else if (_pool.Count > 0)
            {
                go = _pool.Pop();
                go.SetActive(true);
                go.transform.SetParent(contentParent, false);
                go.transform.SetAsLastSibling();
                recycledCount++;

                UpdateItem(go, mod, i, list.Count, isGroupLocked, openSteam, openLocal);
            }
            else
            {
                go = CreateItemStructure(contentParent);
                createdCount++;

                UpdateItem(go, mod, i, list.Count, isGroupLocked, openSteam, openLocal);
            }

            map[mod.Id] = go;
        }

        sw.Stop();
        Log.Debug(
            $"[BML] BuildModList took {sw.ElapsedMilliseconds}ms. Total: {list.Count}, Mounted: {mountedCount}, Created: {createdCount}, Recycled: {recycledCount}, Pool: {_pool.Count}");

        return map;
    }

    private GameObject CreateItemStructure(Transform parent)
    {
        var itemObj = CreateUIObject("RichModItem", parent);
        var rect = itemObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layoutElement = itemObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 100;
        layoutElement.minHeight = 100;
        layoutElement.flexibleWidth = 1;

        var bg = itemObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.22f, 0.25f, 1f);
        var outline = itemObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.45f, 0.5f, 1f);
        outline.effectDistance = new Vector2(2, -2);

        // Preview
        var previewObj = CreateUIObject("Preview", itemObj.transform);
        var previewRect = previewObj.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0, 0.5f);
        previewRect.anchorMax = new Vector2(0, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(50, 0);
        previewRect.sizeDelta = new Vector2(80, 80);
        var previewImage = previewObj.AddComponent<Image>();
        previewImage.raycastTarget = false;
        previewImage.type = Image.Type.Simple;
        previewImage.preserveAspect = false;
        previewObj.AddComponent<Outline>().effectColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Info Container
        var infoContainerObj = CreateUIObject("InfoContainer", itemObj.transform);
        var infoRect = infoContainerObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);
        infoRect.anchorMax = new Vector2(1, 1);
        infoRect.offsetMin = new Vector2(100, 5);
        infoRect.offsetMax = new Vector2(-10, -5);
        var verticalLayout = infoContainerObj.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = 4f;
        verticalLayout.padding = new RectOffset(0, 0, 5, 5);
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;

        // Title Row
        var titleRowObj = CreateUIObject("TitleRow", infoContainerObj.transform);
        var titleRowLayout = titleRowObj.AddComponent<HorizontalLayoutGroup>();
        titleRowLayout.spacing = 8f;
        titleRowLayout.childAlignment = TextAnchor.MiddleLeft;
        titleRowLayout.childForceExpandWidth = false;
        titleRowLayout.childForceExpandHeight = true;
        titleRowLayout.childControlWidth = true;
        titleRowLayout.childControlHeight = true;
        titleRowObj.AddComponent<LayoutElement>().preferredHeight = 28;

        // Toggle
        var toggleObj = CreateUIObject("EnableToggle", titleRowObj.transform);
        var toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(86, 28);
        var toggleLE = toggleObj.AddComponent<LayoutElement>();
        toggleLE.preferredWidth = 86;
        toggleLE.minWidth = 86;
        toggleLE.preferredHeight = 28;
        toggleObj.AddComponent<Image>();
        toggleObj.AddComponent<Button>();
        var toggleLabelObj = CreateUIObject("Label", toggleObj.transform);
        var toggleLabel = toggleLabelObj.AddComponent<TextMeshProUGUI>();
        SetAnchors(toggleLabelObj, Vector2.zero, Vector2.one);
        toggleLabel.alignment = TextAlignmentOptions.Center;
        toggleLabel.fontSize = 16;
        toggleLabel.fontStyle = FontStyles.Bold;
        var knobObj = CreateUIObject("Knob", toggleObj.transform);
        var knobRect = knobObj.GetComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(24, 24);
        var knobImg = knobObj.AddComponent<Image>();
        knobImg.color = Color.white;
        knobImg.raycastTarget = false;
        knobObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.35f);

        // Folder Button
        var folderBtnObj = CreateUIObject("FolderBadge", titleRowObj.transform);
        var folderRect = folderBtnObj.GetComponent<RectTransform>();
        folderRect.sizeDelta = new Vector2(80, 24);
        var folderLE = folderBtnObj.AddComponent<LayoutElement>();
        folderLE.preferredWidth = 80;
        folderLE.minWidth = 80;
        folderLE.preferredHeight = 24;
        var folderImg = folderBtnObj.AddComponent<Image>();
        folderImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        var folderBtn = folderBtnObj.AddComponent<Button>();
        folderBtn.targetGraphic = folderImg;
        var folderTxtObj = CreateUIObject("Text", folderBtnObj.transform);
        SetAnchors(folderTxtObj, Vector2.zero, Vector2.one);
        var folderTxt = folderTxtObj.AddComponent<TextMeshProUGUI>();
        folderTxt.text = L.Mod.ButtonFolder;
        folderTxt.fontSize = 14;
        folderTxt.color = Color.white;
        folderTxt.alignment = TextAlignmentOptions.Center;
        folderTxt.fontStyle = FontStyles.Bold;

        // Steam Button (Placeholder, will be enabled/disabled in Update)
        var steamBadgeObj = CreateUIObject("SteamBadge", titleRowObj.transform);
        var steamBadgeRect = steamBadgeObj.GetComponent<RectTransform>();
        steamBadgeRect.sizeDelta = new Vector2(80, 24);
        var steamBadgeLE = steamBadgeObj.AddComponent<LayoutElement>();
        steamBadgeLE.preferredWidth = 80;
        steamBadgeLE.minWidth = 80;
        steamBadgeLE.preferredHeight = 24;
        var steamBadgeImage = steamBadgeObj.AddComponent<Image>();
        steamBadgeImage.color = new Color(0.2f, 0.5f, 0.85f, 1f);
        var steamButton = steamBadgeObj.AddComponent<Button>();
        steamButton.targetGraphic = steamBadgeImage;
        var steamTextObj = CreateUIObject("SteamText", steamBadgeObj.transform);
        SetAnchors(steamTextObj, Vector2.zero, Vector2.one);
        var steamText = steamTextObj.AddComponent<TextMeshProUGUI>();
        steamText.text = L.Mod.BadgeSteam;
        steamText.fontSize = 14;
        steamText.color = Color.white;
        steamText.alignment = TextAlignmentOptions.Center;
        steamText.fontStyle = FontStyles.Bold;
        steamBadgeObj.SetActive(false); // Hidden by default

        // Title Text
        var titleTextObj = CreateUIObject("TitleText", titleRowObj.transform);
        var titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 20;
        titleText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.fontStyle = FontStyles.Bold;
        titleText.overflowMode = TextOverflowModes.Overflow; // Allow text to overflow instead of being truncated
        titleTextObj.AddComponent<LayoutElement>().flexibleWidth = 1;

        // Up/Down Buttons
        CreateSmallIconButton(titleRowObj.transform, "↑");
        CreateSmallIconButton(titleRowObj.transform, "↓");

        // ID Row
        var idRowObj = CreateUIObject("IdRow", infoContainerObj.transform);
        var idText = idRowObj.AddComponent<TextMeshProUGUI>();
        idText.fontSize = 14;
        idText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        idText.alignment = TextAlignmentOptions.Left;
        idText.overflowMode = TextOverflowModes.Ellipsis;
        idRowObj.AddComponent<LayoutElement>().preferredHeight = 18;

        // Version Row
        var versionRowObj = CreateUIObject("VersionRow", infoContainerObj.transform);
        var versionText = versionRowObj.AddComponent<TextMeshProUGUI>();
        versionText.fontSize = 14;
        versionText.color = new Color(0.6f, 0.8f, 1.0f, 1f);
        versionText.alignment = TextAlignmentOptions.Left;
        versionText.overflowMode = TextOverflowModes.Ellipsis;
        versionRowObj.AddComponent<LayoutElement>().preferredHeight = 18;

        // Description
        var descObj = CreateUIObject("Description", infoContainerObj.transform);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 16;
        descText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        descText.alignment = TextAlignmentOptions.Left;
        descText.overflowMode = TextOverflowModes.Overflow; // Allow text to overflow instead of being truncated
        descText.enableWordWrapping = true;
        descText.maxVisibleLines = 2;
        descObj.AddComponent<LayoutElement>().flexibleHeight = 1;

        // Draggable
        itemObj.AddComponent<BmlDraggableItem>();

        return itemObj;
    }

    private void UpdateItem(
        GameObject itemObj,
        ModInfo2 modInfo,
        int index,
        int totalCount,
        bool isGroupLocked,
        Action<ulong> openSteam,
        Func<string, bool> openLocal)
    {
        var originalInfo = modInfo.OriginalModInfo;
        var title = string.IsNullOrWhiteSpace(originalInfo.displayName)
            ? modInfo.GetDisplayName()
            : originalInfo.displayName;
        var description = string.IsNullOrWhiteSpace(originalInfo.description)
            ? (L.UI.ModNoDescription ?? "No description available")
            : originalInfo.description;

        var isSteam = originalInfo.isSteamItem;
        var steamId = originalInfo.publishedFileId;

        // Preview
        var previewImage = itemObj.transform.Find("Preview")?.GetComponent<Image>();
        if (previewImage != null)
        {
            var preview = modInfo.GetPreview();
            if (preview != null)
            {
                previewImage.sprite = Sprite.Create(preview,
                    new Rect(0, 0, preview.width, preview.height),
                    new Vector2(0.5f, 0.5f));
                previewImage.color = Color.white;
            }
            else
            {
                previewImage.sprite = null;
                previewImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }

        // Title Row Components
        var titleRow = itemObj.transform.Find("InfoContainer/TitleRow");

        // Toggle
        var toggleObj = titleRow.Find("EnableToggle");
        var toggleBtn = toggleObj.GetComponent<Button>();
        toggleBtn.onClick.RemoveAllListeners();

        // Apply toggle visual with animation for user interactions, force update to ensure proper state
        ApplyToggleVisualToItem(itemObj, modInfo.IsEnabled, animate: true, forceUpdate: true);

        if (isGroupLocked)
        {
            toggleBtn.interactable = false;
        }
        else
        {
            toggleBtn.interactable = true;

            toggleBtn.onClick.AddListener(() =>
            {
                // Play toggle sound
                BMLAudioManager.PlaySoundStatic(
                    modInfo.IsEnabled ? AudioEffectType.ToggleOff : AudioEffectType.ToggleOn);

                // 立即更新视觉反馈，提供即时响应
                var newState = !modInfo.IsEnabled;
                ApplyToggleVisualToItem(itemObj, newState, animate: true, forceUpdate: true);

                // 然后触发ViewModel状态更改
                _vm.ToggleModEnabled(modInfo.Id);
            });
        }

        // Folder Button
        var folderBtn = titleRow.Find("FolderBadge").GetComponent<Button>();
        folderBtn.onClick.RemoveAllListeners();
        var folderPath = originalInfo.path;
        folderBtn.onClick.AddListener(() =>
        {
            BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
            if (!string.IsNullOrEmpty(folderPath)) openLocal?.Invoke(folderPath);
        });

        // Steam Button
        var steamBadge = titleRow.Find("SteamBadge");
        if (isSteam)
        {
            steamBadge.gameObject.SetActive(true);
            var steamBtn = steamBadge.GetComponent<Button>();
            steamBtn.onClick.RemoveAllListeners();
            var capturedSteamId = steamId;
            steamBtn.onClick.AddListener(() =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                openSteam?.Invoke(capturedSteamId);
            });
        }
        else
        {
            steamBadge.gameObject.SetActive(false);
        }

        // Title Text
        var titleTextTransform = titleRow.Find("TitleText");
        if (titleTextTransform == null)
        {
            Log.Error($"[BML] TitleText not found for mod {modInfo.Id}");
            return;
        }

        var titleText = titleTextTransform.GetComponent<TextMeshProUGUI>();
        if (titleText == null)
        {
            Log.Error($"[BML] TitleText TextMeshProUGUI component not found for mod {modInfo.Id}");
            return;
        }

        titleText.text = title ?? "";

        // Up/Down Buttons
        var upBtnObj = titleRow.Find("IconButton_↑");
        var downBtnObj = titleRow.Find("IconButton_↓");

        UpdateSortButtons(upBtnObj, downBtnObj, modInfo, index, totalCount, isGroupLocked, itemObj);

        // ID Row
        var idText = itemObj.transform.Find("InfoContainer/IdRow").GetComponent<TextMeshProUGUI>();
        idText.text = isSteam
            ? $"{L.Mod.LabelId} {modInfo.Id} | {L.Mod.LabelSteamId} {steamId}"
            : $"{L.Mod.LabelId} {modInfo.Id}";

        // Version Row
        var versionText = itemObj.transform.Find("InfoContainer/VersionRow").GetComponent<TextMeshProUGUI>();
        var version = originalInfo.version ?? "N/A";
        if (string.IsNullOrEmpty(version) || version == "?") version = "N/A";
        var versionLabel = L.Mod.LabelVersion ?? "Version";
        versionText.text = $"{versionLabel}: {version}";

        // Description
        var descTransform = itemObj.transform.Find("InfoContainer/Description");
        var descText = descTransform.GetComponent<TextMeshProUGUI>();
        var filteredDescription = FilterUnsupportedGlyphs(description, descText.font);
        descText.text = filteredDescription;

        // Force text mesh to update
        descText.ForceMeshUpdate();

        // Draggable
        var draggable = itemObj.GetComponent<BmlDraggableItem>();
        if (isGroupLocked)
        {
            draggable.enabled = false;
        }
        else
        {
            draggable.enabled = true;
            // Pass the actual data index (position in current group's mod list)
            draggable.Init((BmlModManagerUIView)_host, "mod", modInfo.Id, index);
        }
    }

    /// <summary>
    /// Update the sort buttons (up/down) with correct index and event handlers.
    /// This is separated so it can be called when mounting cached items.
    /// </summary>
    private void UpdateSortButtons(
        Transform upBtnObj,
        Transform downBtnObj,
        ModInfo2 modInfo,
        int index,
        int totalCount,
        bool isGroupLocked,
        GameObject itemObj)
    {
        SetupSortButton(upBtnObj, true);
        SetupSortButton(downBtnObj, false);

        void SetupSortButton(Transform btnObj, bool isUp)
        {
            var btn = btnObj.GetComponent<Button>();
            var relay = btnObj.GetComponent<PointerClickRelay>() ?? btnObj.gameObject.AddComponent<PointerClickRelay>();
            var btnImage = btnObj.GetComponent<Image>();

            // 美化按钮外观
            try
            {
                var buttonSprite = UIResourceManager.GetSprite("square_bg");
                if (buttonSprite != null)
                {
                    btnImage.sprite = buttonSprite;
                    btnImage.type = Image.Type.Sliced;
                    btnImage.color = isUp
                        ? new Color(0.2f, 0.6f, 0.9f, 0.9f)
                        : // 上移按钮 - 蓝色
                        new Color(0.3f, 0.7f, 0.4f, 0.9f); // 下移按钮 - 绿色
                }
                else
                {
                    // 备用颜色方案
                    btnImage.color = isUp ? new Color(0.2f, 0.6f, 0.9f, 0.9f) : new Color(0.3f, 0.7f, 0.4f, 0.9f);
                }
            }
            catch
            {
                btnImage.color = isUp ? new Color(0.2f, 0.6f, 0.9f, 0.9f) : new Color(0.3f, 0.7f, 0.4f, 0.9f);
            }

            // 添加按钮边框效果的阴影组件
            var shadow = btnObj.GetComponent<Shadow>() ?? btnObj.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);

            // 美化按钮图标（箭头）
            var iconTransform = btnObj.Find("Icon");
            if (iconTransform != null)
            {
                var iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                {
                    try
                    {
                        var arrowSprite = isUp
                            ? UIResourceManager.GetSprite("arrow_up")
                            : UIResourceManager.GetSprite("arrow_down");

                        if (arrowSprite != null)
                        {
                            iconImage.sprite = arrowSprite;
                            iconImage.type = Image.Type.Simple;
                            iconImage.color = Color.white;
                        }
                    }
                    catch
                    {
                        // 如果资源加载失败，使用默认颜色
                        iconImage.color = Color.white;
                    }
                }
            }

            // 设置按钮颜色块以实现悬停效果
            var colorBlock = btn.colors;
            colorBlock.normalColor = isUp ? new Color(0.2f, 0.6f, 0.9f, 0.9f) : new Color(0.3f, 0.7f, 0.4f, 0.9f);
            colorBlock.highlightedColor = isUp ? new Color(0.3f, 0.7f, 1.0f, 1.0f) : new Color(0.4f, 0.8f, 0.5f, 1.0f);
            colorBlock.pressedColor = isUp ? new Color(0.15f, 0.5f, 0.8f, 1.0f) : new Color(0.25f, 0.6f, 0.35f, 1.0f);
            colorBlock.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colorBlock;

            // Reset relay
            relay.OnSingleClick = null;
            relay.OnDoubleClick = null;

            if (isGroupLocked)
            {
                btn.interactable = false;
                btnImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 锁定时显示灰色
            }
            else
            {
                btn.interactable = true;

                if (isUp)
                {
                    relay.OnDoubleClick = () =>
                    {
                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        _vm.MoveModToTop(modInfo.Id);
                    };
                    relay.OnSingleClick = () =>
                    {
                        if (index <= 0)
                        {
                            Shake(itemObj.GetComponent<RectTransform>());
                            return;
                        }

                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        _vm.MoveModUp(modInfo.Id);
                    };
                }
                else
                {
                    relay.OnDoubleClick = () =>
                    {
                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        _vm.MoveModToBottom(modInfo.Id);
                    };
                    relay.OnSingleClick = () =>
                    {
                        if (index >= totalCount - 1)
                        {
                            Shake(itemObj.GetComponent<RectTransform>());
                            return;
                        }

                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        _vm.MoveModDown(modInfo.Id);
                    };
                }
            }

            // 添加DOTween动画效果
            AddButtonClickEffects(btnObj, isUp);
        }
    }

    public static void ApplyToggleVisualToItem(GameObject itemObj, bool enabled, bool animate = true,
        bool forceUpdate = false)
    {
        if (itemObj == null) return;
        var toggleT = itemObj.transform.Find("InfoContainer/TitleRow/EnableToggle");
        if (toggleT == null) return;

        var toggleRect = toggleT.GetComponent<RectTransform>();
        var toggleBg = toggleT.GetComponent<Image>();
        var labelT = toggleT.Find("Label");
        var knobT = toggleT.Find("Knob");
        if (toggleRect == null || toggleBg == null || labelT == null || knobT == null) return;

        var label = labelT.GetComponent<TextMeshProUGUI>();
        var knobRect = knobT.GetComponent<RectTransform>();
        if (label == null || knobRect == null) return;

        // 检查当前状态，如果状态没有改变则跳过处理（除非强制更新）
        var currentLabel = label.text;
        var expectedLabel = enabled ? L.UI.ToggleOn : L.UI.ToggleOff;
        var isStateChanging = currentLabel != expectedLabel;

        // 如果不是强制更新且状态没有改变，则直接跳过所有处理
        if (!forceUpdate && !isStateChanging)
        {
            return;
        }

        // 使用 UIResourceManager 美化开关背景
        try
        {
            var toggleSprite =
                enabled ? UIResourceManager.GetSprite("success_bg") : UIResourceManager.GetSprite("error_bg");

            if (toggleSprite != null)
            {
                toggleBg.sprite = toggleSprite;
                toggleBg.type = Image.Type.Sliced;
                toggleBg.color = Color.white;
            }
            else
            {
                // 备用颜色方案
                toggleBg.color = enabled ? new Color(0.2f, 0.8f, 0.3f, 1f) : new Color(0.85f, 0.25f, 0.25f, 1f);
            }
        }
        catch
        {
            toggleBg.color = enabled ? new Color(0.2f, 0.8f, 0.3f, 1f) : new Color(0.85f, 0.25f, 0.25f, 1f);
        }

        label.text = expectedLabel;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;

        // 添加文字描边效果
        if (label.outlineWidth == 0)
        {
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color(0f, 0f, 0f, 0.5f);
        }

        // 直接设置位置，不使用任何动画
        var bgW = toggleRect.sizeDelta.x;
        var knobW = knobRect.sizeDelta.x;
        var targetOffset = (bgW - knobW) * 0.5f - 2f;
        var targetPosition = new Vector2(enabled ? targetOffset : -targetOffset, 0);
        knobRect.anchoredPosition = targetPosition;

        // 为开关旋钮添加美化效果
        var knobImage = knobT.GetComponent<Image>();
        if (knobImage != null)
        {
            try
            {
                var knobSprite = UIResourceManager.GetSprite("circle_info");
                if (knobSprite != null)
                {
                    knobImage.sprite = knobSprite;
                    knobImage.type = Image.Type.Simple;
                    knobImage.color = Color.white;
                }
            }
            catch
            {
                // 如果资源加载失败，使用默认颜色
                knobImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            }

            // 直接设置最终颜色，不使用动画
            knobImage.color = enabled ? new Color(1f, 1f, 1f, 0.85f) : Color.white;
        }
    }

    /// <summary>
    /// 为按钮添加点击动画效果
    /// </summary>
    private void AddButtonClickEffects(Transform buttonObj, bool isUpButton)
    {
        var button = buttonObj.GetComponent<Button>();
        var buttonImage = buttonObj.GetComponent<Image>();

        if (button != null && buttonImage != null)
        {
            // 原始颜色，用于动画后恢复
            var originalColor = isUpButton ? new Color(0.2f, 0.6f, 0.9f, 0.9f) : new Color(0.3f, 0.7f, 0.4f, 0.9f);

            // 设置按钮颜色过渡效果（不使用动画）
            var colors = button.colors;
            colors.normalColor = originalColor;
            colors.highlightedColor =
                new Color(originalColor.r + 0.2f, originalColor.g + 0.2f, originalColor.b + 0.2f, 1f);
            colors.pressedColor = new Color(originalColor.r - 0.1f, originalColor.g - 0.1f, originalColor.b - 0.1f, 1f);
            button.colors = colors;
        }
    }

    private static string FilterUnsupportedGlyphs(string? input, TMP_FontAsset? font)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (font == null) return input!;
        var sb = new StringBuilder(input!.Length);
        for (var i = 0; i < input!.Length; i++)
        {
            var ch = input[i];
            if (ch == '\n' || ch == '\r' || ch == '\t')
            {
                sb.Append(ch);
                continue;
            }

            if (char.IsHighSurrogate(ch) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
            {
                sb.Append(input[i]); // Keep the surrogate pair
                sb.Append(input[i + 1]);
                i++;
                continue;
            }

            // Only filter out control characters and problematic glyphs, keep all visible characters including Chinese
            if (char.IsControl(ch) && ch != '\n' && ch != '\r' && ch != '\t')
            {
                continue; // Skip control characters except common ones
            }

            // Always append the character - let TextMeshPro handle font fallback
            sb.Append(ch);
        }

        return sb.ToString();
    }

    private void Shake(RectTransform rect)
    {
        if (rect == null) return;
        _host.StartCoroutine(ShakeCoroutine(rect));
    }

    private IEnumerator ShakeCoroutine(RectTransform rect)
    {
        var original = rect.anchoredPosition;
        Vector2[] offsets =
        [
            new Vector2(-6, 0), new Vector2(6, 0),
            new Vector2(-4, 0), new Vector2(4, 0),
            new Vector2(-2, 0), new Vector2(2, 0),
            Vector2.zero
        ];
        for (var i = 0; i < offsets.Length; i++)
        {
            rect.anchoredPosition = original + offsets[i];
            yield return new WaitForSeconds(0.015f);
        }

        rect.anchoredPosition = original;
    }
}

internal class PointerClickRelay : MonoBehaviour, IPointerClickHandler
{
    public Action? OnSingleClick;
    public Action? OnDoubleClick;
    public float DoubleClickDelay = 0.25f;

    private bool _waitingSecond;
    private float _lastClickTime;
    private Coroutine? _pendingSingle;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var now = Time.unscaledTime;
        if (_waitingSecond && now - _lastClickTime <= DoubleClickDelay)
        {
            _waitingSecond = false;
            _lastClickTime = 0f;
            if (_pendingSingle != null)
            {
                StopCoroutine(_pendingSingle);
                _pendingSingle = null;
            }

            OnDoubleClick?.Invoke();
            return;
        }

        _waitingSecond = true;
        _lastClickTime = now;
        if (_pendingSingle != null) StopCoroutine(_pendingSingle);

        _pendingSingle = StartCoroutine(InvokeSingleAfterDelay());
    }

    private IEnumerator InvokeSingleAfterDelay()
    {
        var end = Time.unscaledTime + DoubleClickDelay;
        while (Time.unscaledTime < end)
        {
            yield return new WaitForEndOfFrame();
        }

        if (_waitingSecond)
        {
            _waitingSecond = false;
            _pendingSingle = null;
            OnSingleClick?.Invoke();
        }
    }
}
