using Steamworks;
using Newbe.BetterModLoader.Audio;

namespace Newbe.BetterModLoader.ModDataChecks;

internal partial class ModDataCheckView : MonoBehaviour
{
    public static void Show(List<ModInfo2> mods, List<SteamModDependencyInfo> allMods, Action onForceApply)
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ModDataCheckView] Failed to show ModDataCheckView");
        }

        return;

        void RunCore()
        {
            var go = new GameObject("ModDataCheckView");
            // 由 UIFactory.CreatePanelBase 自动创建顶层 Canvas，无需挂载到主 Canvas
            var panel = go.AddComponent<ModDataCheckView>();
            var uploadPanelData = CreateData(mods, allMods);

            // 加载忽略状态并应用到数据
            LoadIgnoredState(uploadPanelData);

            if (uploadPanelData.IsAllPassed || !uploadPanelData.HasActiveFailures)
            {
                onForceApply();
                return;
            }

            var vm = new ModDataChecksVm(uploadPanelData);
            panel.Init(vm, new ModDataCheckViewButtonOption(L.ModDataCheck.ButtonCancel, onForceApply));
        }
    }

    public static void ShowForUpdate(List<SteamModDependencyInfo> infos)
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ModDataCheckView] Failed to show ModDataCheckView for update");
        }

        return;

        void RunCore()
        {
            var go = new GameObject("ModDataCheckView");
            // 由 UIFactory.CreatePanelBase 自动创建顶层 Canvas，无需挂载到主 Canvas
            var panel = go.AddComponent<ModDataCheckView>();
            var uploadPanelData = new ModDataCheckResult();

            foreach (var info in infos)
            {
                uploadPanelData.AllItems.Add(new NeedToUpdateCheckItem(info));
            }

            // 加载忽略状态并应用到数据
            LoadIgnoredState(uploadPanelData);

            var vm = new ModDataChecksVm(uploadPanelData);
            panel.Init(vm, new ModDataCheckViewButtonOption(L.ModDataCheck.ButtonBack, null));
        }
    }

    private static ModDataCheckResult CreateData(List<ModInfo2> myMods, List<SteamModDependencyInfo> allMods)
    {
        var result = new ModDataCheckResult();
        var reverseDict = myMods
            .Select((mod, index) => new { mod, index })
            .ToDictionary(x => x.mod, x => x.index);
        var modDict = allMods
            .ToDictionary(x => x.SteamId);

        foreach (var myMod in myMods)
        {
            if (!modDict.TryGetValue(myMod.OriginalModInfo.publishedFileId, out var modInfo))
            {
                continue;
            }

            if (modInfo.LocalInstallTime < modInfo.OnlineUpdateTime)
            {
                // re-check install time from steam api. since topMods may have stale data
                if (SteamUGC.GetItemInstallInfo(
                        new PublishedFileId_t(modInfo.SteamId),
                        out var sizeOnDisk,
                        out var folder,
                        1024,
                        out var timeStamp)
                    && DateTimeOffset.FromUnixTimeSeconds(timeStamp).UtcDateTime < modInfo.OnlineUpdateTime)
                {
                    result.AllItems.Add(new NeedToUpdateCheckItem(modInfo));
                }
            }

            if (modInfo.DependencyIds.Count <= 0)
            {
                continue;
            }

            foreach (var modInfoDependencyId in modInfo.DependencyIds)
            {
                var dep =
                    myMods.FirstOrDefault(a => a.OriginalModInfo.publishedFileId == modInfoDependencyId);

                if (dep == null)
                {
                    var findInAll = allMods.FirstOrDefault(a => a.SteamId == modInfoDependencyId);
                    if (findInAll != null)
                    {
                        result.AllItems.Add(new MissingDependencyCheckItem(modInfo, findInAll));
                    }
                    else
                    {
                        Log.Info(
                            $"[ModDataCheck] 依赖缺失但未找到信息：Mod {modInfo.SteamId} 依赖 {modInfoDependencyId}");
                    }

                    continue;
                }

                // 检查是否存在循环依赖：如果依赖项也依赖当前 Mod，则视为无依赖关系
                var isCircularDependency =
                    modDict.TryGetValue(modInfoDependencyId, out var potentialDepInfo)
                    && potentialDepInfo.DependencyIds.Contains(modInfo.SteamId);

                if (isCircularDependency)
                {
                    Log.Info(
                        $"[ModDataCheck] 检测到循环依赖：Mod {modInfo.SteamId} <-> {modInfoDependencyId}，忽略依赖检查");
                    continue;
                }

                if (reverseDict[dep] > reverseDict[myMod])
                {
                    SteamModDependencyInfo dependencyModInfo;
                    if (modDict.TryGetValue(modInfoDependencyId, out var foundDepInfo))
                    {
                        dependencyModInfo = foundDepInfo;
                    }
                    else
                    {
                        dependencyModInfo = new SteamModDependencyInfo
                        {
                            SteamId = modInfoDependencyId,
                        };
                    }

                    var modOrderError = new ModOrderError(modInfo, dependencyModInfo);
                    result.AllItems.Add(new ModOrderErrorCheckItem(modOrderError));
                }

                if (ModIdentifier.IsCustomThings(modInfoDependencyId))
                {
                    // add a warning check item for required CustomThings mod
                    result.AllItems.Add(new CustomThingsDependencyWarningItem(modInfo));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 从ModOptions加载忽略状态并应用到检查结果
    /// </summary>
    private static void LoadIgnoredState(ModDataCheckResult data)
    {
        try
        {
            var ignoredIds = ModDataChecksConfigs.GetIgnoredChecks();

            foreach (var item in data.AllItems)
            {
                item.IsIgnored = ignoredIds.Contains(item.Id);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[ModDataCheckView] Failed to load ignored checks, using empty list");
        }
    }
}

internal partial class ModDataCheckView
{
    private ModDataChecksVm? _vm;
    private DisposableBag _disposableBag;
    private Button? _forceButton;
    private Button _cancelButton = null!;
    private Button? _ignoreAllButton;
    private Button? _restoreAllButton;
    private GameObject _activeContainerGO = null!;
    private GameObject _ignoredContainerGO = null!;
    private ModDataCheckViewButtonOption _option;
    private GameObject _rightToolbarGO = null!;

    private void OnDestroy()
    {
        _disposableBag.Dispose();
    }

    public void Init(ModDataChecksVm vm, ModDataCheckViewButtonOption option)
    {
        Log.Info($"[ModDataCheckView] Init started with option: {option.BackText}");
        _vm = vm;
        _disposableBag.Add(_vm);
        _option = option;

        // 创建布局（包括容器）- 先创建UI再设置订阅
        Log.Info("[ModDataCheckView] Starting layout creation");
        Layout(option);
        Log.Info("[ModDataCheckView] Layout creation completed");

        // 布局创建后再设置订阅，避免提前触发
        _vm.CheckResult.Subscribe(UpdateChecklist)
            .AddTo(ref _disposableBag);

        _cancelButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                Log.Info("[ModDataCheckView] Cancel button clicked, destroying panel");
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            })
            .AddTo(ref _disposableBag);
        if (_forceButton != null)
        {
            _forceButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                    // Disable buttons immediately to prevent double-clicks
                    if (_forceButton != null) _forceButton.interactable = false;
                    if (_cancelButton != null) _cancelButton.interactable = false;
                    if (_ignoreAllButton != null) _ignoreAllButton.interactable = false;
                    if (_restoreAllButton != null) _restoreAllButton.interactable = false;

                    try
                    {
                        _option.OnApply?.Invoke();
                    }
                    finally
                    {
                        // Destroy panel if it still exists
                        if (gameObject != null)
                        {
                            Destroy(gameObject);
                        }
                    }
                })
                .AddTo(ref _disposableBag);

            // Add ignore all button handler
            if (_ignoreAllButton != null)
            {
                _ignoreAllButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        _vm?.IgnoreAll();
                    })
                    .AddTo(ref _disposableBag);
            }

            // Add restore all button handler
            if (_restoreAllButton != null)
            {
                _restoreAllButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                        _vm?.RestoreAll();
                    })
                    .AddTo(ref _disposableBag);
            }
        }
    }

    private void UpdateChecklist(ModDataCheckResult data)
    {
        Log.Info(
            $"[ModDataCheckView] UpdateChecklist started. ActiveItems: {data.ActiveItems.Count}, IgnoredItems: {data.IgnoredItems.Count}");

        // Dynamically show/hide the right toolbar based on content availability
        if (_rightToolbarGO != null)
        {
            var hasContent = data.ActiveItems.Count > 0 || data.IgnoredItems.Count > 0;
            _rightToolbarGO.SetActive(hasContent);
            Log.Info($"[ModDataCheckView] Right toolbar visibility set to: {hasContent}");
        }

        // Clear active items container
        if (_activeContainerGO != null)
        {
            var activeContent = GetScrollContent(_activeContainerGO);
            foreach (Transform child in activeContent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Clear ignored items container completely (restore all button is now outside)
        if (_ignoredContainerGO != null)
        {
            var ignoredContent = GetScrollContent(_ignoredContainerGO);
            foreach (Transform child in ignoredContent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        Log.Info($"[ModDataCheckView] Adding {data.ActiveItems.Count} active items to left container");
        // Add active items to left container
        if (_activeContainerGO != null)
        {
            var activeContent = GetScrollContent(_activeContainerGO);
            foreach (var item in data.ActiveItems)
            {
                Log.Info($"[ModDataCheckView] Creating active item UI: {item.Id} - {GetItemDescription(item)}");
                CreateCheckItemUI(activeContent, item, false);
            }
        }

        Log.Info($"[ModDataCheckView] Adding {data.IgnoredItems.Count} ignored items to right container");
        // Add ignored items to right container (skip the first child which is restore all button)
        if (_ignoredContainerGO != null)
        {
            var ignoredContent = GetScrollContent(_ignoredContainerGO);
            foreach (var item in data.IgnoredItems)
            {
                Log.Info($"[ModDataCheckView] Creating ignored item UI: {item.Id} - {GetItemDescription(item)}");
                CreateCheckItemUI(ignoredContent, item, true);
            }
        }

        // Cleanup resolved ignored items
        _vm?.CleanupResolvedIgnoredItems();

        Log.Info($"[ModDataCheckView] UpdateChecklist completed");

        // Debug container status after update
        DebugContainerStatus();
    }

    private void CreateCheckItemUI(GameObject parent, CheckResultItem item, bool isIgnored)
    {
        var itemGO = PanelUI.CreateUIObject($"CheckItem_{item.Id}", parent.transform);
        var itemLE = itemGO.AddComponent<LayoutElement>();
        itemLE.minHeight = 60f;
        itemLE.preferredHeight = 60f;
        itemLE.flexibleWidth = 1f;

        var horizontalLayout = itemGO.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = 5f;
        horizontalLayout.padding = new RectOffset(10, 10, 5, 5);
        horizontalLayout.childForceExpandWidth = true;
        horizontalLayout.childControlWidth = true; // Allow parent to control width
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;

        var itemBG = itemGO.AddComponent<Image>();
        itemBG.color = isIgnored ? new Color(0.3f, 0.3f, 0.3f, 0.8f) : new Color(0.2f, 0.2f, 0.25f, 0.9f);

        // Color indicator - 6px bar with type-specific colors (fixed stretching)
        var indicatorGO = PanelUI.CreateUIObject("ColorIndicator", itemGO.transform);
        var indicatorLE = indicatorGO.AddComponent<LayoutElement>();
        indicatorLE.minWidth = 6f;
        indicatorLE.preferredWidth = 6f;
        indicatorLE.ignoreLayout = true; // Don't affect parent layout

        // Fix stretching by setting RectTransform directly
        var indicatorRT = indicatorGO.GetComponent<RectTransform>();
        indicatorRT.anchorMin = new Vector2(0, 0.5f);
        indicatorRT.anchorMax = new Vector2(0, 0.5f);
        indicatorRT.pivot = new Vector2(0, 0.5f);
        indicatorRT.sizeDelta = new Vector2(6f, 32f);
        indicatorRT.anchoredPosition = Vector2.zero;

        var indicatorImage = indicatorGO.AddComponent<Image>();
        Color indicatorColor;

        // Type-specific color coding
        switch (item)
        {
            case NeedToUpdateCheckItem:
                indicatorColor = Color.cyan; // Blue for update checks
                break;
            case MissingDependencyCheckItem:
                indicatorColor = Color.red; // Red for missing dependencies
                break;
            case ModOrderErrorCheckItem:
                indicatorColor = Color.yellow; // Yellow for order errors
                break;
            default:
                indicatorColor = Color.gray; // Gray for unknown types
                break;
        }

        indicatorImage.color = isIgnored ? Color.gray * 0.5f : indicatorColor;

        // Text content
        var textGO = PanelUI.CreateUIObject("Text", itemGO.transform);
        var textLE = textGO.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1f;

        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = GetItemDescription(item);
        textTMP.fontSize = 14f;
        textTMP.color = isIgnored ? Color.gray : Color.white;
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;
        textTMP.enableWordWrapping = true;
        textTMP.overflowMode = TextOverflowModes.Overflow;

        // Special action buttons for different check types
        CreateSpecialActionButtons(itemGO.transform, item);

        // Ignore/Restore button
        var buttonGO = PanelUI.CreateUIObject("Button", itemGO.transform);
        var buttonLE = buttonGO.AddComponent<LayoutElement>();
        buttonLE.ignoreLayout = true; // Don't affect parent layout

        // Fix stretching by setting RectTransform directly (like color indicator)
        var buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(1, 0.5f); // Right side
        buttonRT.anchorMax = new Vector2(1, 0.5f);
        buttonRT.pivot = new Vector2(1, 0.5f);
        buttonRT.sizeDelta = new Vector2(60f, 28f); // Fixed size
        buttonRT.anchoredPosition = Vector2.zero;

        var button = buttonGO.AddComponent<Button>();
        var buttonImage = buttonGO.AddComponent<Image>();

        // Use red for ignore actions, green for restore actions
        if (isIgnored)
        {
            buttonImage.sprite = UIResourceManager.GetSprite("success_bg");
            buttonImage.color = new Color(0.8f, 1f, 0.8f, 1f); // Light green tint
        }
        else
        {
            buttonImage.sprite = UIResourceManager.GetSprite("warning_bg");
            buttonImage.color = new Color(1f, 0.8f, 0.8f, 1f); // Light red tint
        }

        buttonImage.type = Image.Type.Sliced;

        var buttonColors = button.colors;
        if (isIgnored)
        {
            // Green color scheme for restore button
            buttonColors.normalColor = new Color(0.8f, 1f, 0.8f, 1f);
            buttonColors.highlightedColor = new Color(0.7f, 0.9f, 0.7f, 1f);
            buttonColors.pressedColor = new Color(0.6f, 0.8f, 0.6f, 1f);
        }
        else
        {
            // Red color scheme for ignore button
            buttonColors.normalColor = new Color(1f, 0.8f, 0.8f, 1f);
            buttonColors.highlightedColor = new Color(0.9f, 0.7f, 0.7f, 1f);
            buttonColors.pressedColor = new Color(0.8f, 0.6f, 0.6f, 1f);
        }

        button.colors = buttonColors;
        button.targetGraphic = buttonImage;

        var buttonTextGO = PanelUI.CreateUIObject("ButtonText", buttonGO.transform);
        var buttonTextTMP = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonTextTMP.text = isIgnored ? L.ModDataCheck.ButtonRestore : L.ModDataCheck.ButtonIgnore;
        buttonTextTMP.fontSize = 12f;
        buttonTextTMP.color = Color.black;
        buttonTextTMP.alignment = TextAlignmentOptions.Center;
        buttonTextTMP.raycastTarget = false;
        var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        // Button click handler
        button.OnClickAsObservable()
            .Subscribe(_ =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                if (isIgnored)
                {
                    _vm?.RestoreItem(item);
                }
                else
                {
                    _vm?.IgnoreItem(item);
                }
            })
            .AddTo(ref _disposableBag);
    }

    private string GetItemDescription(CheckResultItem item)
    {
        return item switch
        {
            NeedToUpdateCheckItem updateItem =>
                $"{L.ModDataCheck.DescriptionNeedUpdate}\n{updateItem.ModInfo.Title ?? $"Mod {updateItem.ModInfo.SteamId}"}",
            MissingDependencyCheckItem missingItem =>
                $"{L.ModDataCheck.DescriptionMissingDependency}\n{missingItem.Mod.Title ?? $"Mod {missingItem.Mod.SteamId}"} - {string.Format(L.ModDataCheck.MissingDependency, missingItem.MissingDependency.Title ?? $"Mod {missingItem.MissingDependency.SteamId}")}",
            ModOrderErrorCheckItem orderItem =>
                $"{L.ModDataCheck.DescriptionModOrderError}\n{orderItem.Error.Mod.Title ?? $"Mod {orderItem.Error.Mod.SteamId}"} - {string.Format(L.ModDataCheck.OrderError, orderItem.Error.Dependency.Title ?? $"Mod {orderItem.Error.Dependency.SteamId}")}",
            CustomThingsDependencyWarningItem customThingsItem =>
                $"{L.ModDataCheck.DescriptionCustomThingsDependency}\n{customThingsItem.Mod.Title ?? $"Mod {customThingsItem.Mod.SteamId}"}",
            _ => L.ModDataCheck.DescriptionUnknown
        };
    }

    /// <summary>
    /// 为不同类型的检查项创建特殊操作按钮
    /// </summary>
    private void CreateSpecialActionButtons(Transform parent, CheckResultItem item)
    {
        switch (item)
        {
            case NeedToUpdateCheckItem updateItem:
                CreateViewPageButton(parent, updateItem.ModInfo.SteamId);
                break;
            case MissingDependencyCheckItem missingItem:
                CreateViewModButton(parent, missingItem.Mod.SteamId);
                CreateViewDependencyButton(parent, missingItem.MissingDependency.SteamId);
                break;
            case ModOrderErrorCheckItem:
                // ModOrderError 原始实现没有特殊按钮
                break;
        }
    }

    /// <summary>
    /// 创建"查看页面"按钮（用于需要更新的模组）
    /// </summary>
    private void CreateViewPageButton(Transform parent, ulong steamId)
    {
        var buttonGO = PanelUI.CreateUIObject("ViewPageButton", parent);
        var buttonLE = buttonGO.AddComponent<LayoutElement>();
        buttonLE.ignoreLayout = true;

        // Position to the left of ignore/restore button
        var buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(1, 0.5f);
        buttonRT.anchorMax = new Vector2(1, 0.5f);
        buttonRT.pivot = new Vector2(1, 0.5f);
        buttonRT.sizeDelta = new Vector2(80f, 28f);
        buttonRT.anchoredPosition = new Vector2(-65f, 0f); // Left of ignore button

        var button = buttonGO.AddComponent<Button>();
        var buttonImage = buttonGO.AddComponent<Image>();

        var sprite = UIResourceManager.GetSprite("logs_normal");
        buttonImage.sprite = sprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Color.white;

        var buttonColors = button.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        buttonColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = buttonColors;
        button.targetGraphic = buttonImage;

        var buttonTextGO = PanelUI.CreateUIObject("Text", buttonGO.transform);
        var buttonTextTMP = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonTextTMP.text = L.ModDataCheck.ButtonViewPage;
        buttonTextTMP.fontSize = 12f;
        buttonTextTMP.color = Color.black;
        buttonTextTMP.alignment = TextAlignmentOptions.Center;
        buttonTextTMP.fontStyle = FontStyles.Bold;
        buttonTextTMP.raycastTarget = false;

        var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(() =>
        {
            Log.Info($"[ModDataCheck] 打开 Steam Workshop 页面: {steamId}");
            SteamFriends.ActivateGameOverlayToWebPage(
                $"https://steamcommunity.com/sharedfiles/filedetails/?id={steamId}");
        });
    }

    /// <summary>
    /// 创建"查看模组"按钮（用于缺失依赖的当前模组）
    /// </summary>
    private void CreateViewModButton(Transform parent, ulong steamId)
    {
        var buttonGO = PanelUI.CreateUIObject("ViewModButton", parent);
        var buttonLE = buttonGO.AddComponent<LayoutElement>();
        buttonLE.ignoreLayout = true;

        // Position to the left of ignore/restore button
        var buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(1, 0.5f);
        buttonRT.anchorMax = new Vector2(1, 0.5f);
        buttonRT.pivot = new Vector2(1, 0.5f);
        buttonRT.sizeDelta = new Vector2(80f, 28f);
        buttonRT.anchoredPosition = new Vector2(-65f, 0f); // Left of ignore button

        var button = buttonGO.AddComponent<Button>();
        var buttonImage = buttonGO.AddComponent<Image>();

        var sprite = UIResourceManager.GetSprite("success_bg");
        buttonImage.sprite = sprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Color.white;

        var buttonColors = button.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        buttonColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = buttonColors;
        button.targetGraphic = buttonImage;

        var buttonTextGO = PanelUI.CreateUIObject("Text", buttonGO.transform);
        var buttonTextTMP = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonTextTMP.text = L.ModDataCheck.ButtonViewMod;
        buttonTextTMP.fontSize = 12f;
        buttonTextTMP.color = Color.black;
        buttonTextTMP.alignment = TextAlignmentOptions.Center;
        buttonTextTMP.fontStyle = FontStyles.Bold;
        buttonTextTMP.raycastTarget = false;

        var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(() =>
        {
            Log.Info($"[ModDataCheck] 打开本模组的 Steam Workshop 页面: {steamId}");
            SteamFriends.ActivateGameOverlayToWebPage(
                $"https://steamcommunity.com/sharedfiles/filedetails/?id={steamId}");
        });
    }

    /// <summary>
    /// 创建"查看依赖"按钮（用于缺失的依赖模组）
    /// </summary>
    private void CreateViewDependencyButton(Transform parent, ulong steamId)
    {
        var buttonGO = PanelUI.CreateUIObject("ViewDependencyButton", parent);
        var buttonLE = buttonGO.AddComponent<LayoutElement>();
        buttonLE.ignoreLayout = true;

        // Position to the left of view mod button
        var buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(1, 0.5f);
        buttonRT.anchorMax = new Vector2(1, 0.5f);
        buttonRT.pivot = new Vector2(1, 0.5f);
        buttonRT.sizeDelta = new Vector2(80f, 28f);
        buttonRT.anchoredPosition = new Vector2(-150f, 0f); // Left of view mod button

        var button = buttonGO.AddComponent<Button>();
        var buttonImage = buttonGO.AddComponent<Image>();

        var sprite = UIResourceManager.GetSprite("square_bg");
        buttonImage.sprite = sprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Color.white;

        var buttonColors = button.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        buttonColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = buttonColors;
        button.targetGraphic = buttonImage;

        var buttonTextGO = PanelUI.CreateUIObject("Text", buttonGO.transform);
        var buttonTextTMP = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonTextTMP.text = L.ModDataCheck.ButtonViewDependency;
        buttonTextTMP.fontSize = 12f;
        buttonTextTMP.color = Color.black;
        buttonTextTMP.alignment = TextAlignmentOptions.Center;
        buttonTextTMP.fontStyle = FontStyles.Bold;
        buttonTextTMP.raycastTarget = false;

        var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(() =>
        {
            Log.Info($"[ModDataCheck] 打开缺失依赖的 Steam Workshop 页面: {steamId}");
            SteamFriends.ActivateGameOverlayToWebPage(
                $"https://steamcommunity.com/sharedfiles/filedetails/?id={steamId}");
        });
    }
}

internal partial class ModDataCheckView
{
    private PanelBase _panelBase = null!;

    private void Layout(ModDataCheckViewButtonOption option)
    {
        // 创建 PanelBase（已自动包含 toolbar/title 行布局）
        _panelBase = PanelUI.CreatePanelBase(L.ModDataCheck.Title);

        // 配置 LeftToolbar 的 HorizontalLayoutGroup
        Log.Info($"[ModDataCheckView] Creating cancel button with text: {option.BackText}");
        (_cancelButton, _) =
            _panelBase.AddTitleToolbarButton(option.BackText, PanelBase.ButtonColorTheme.Green);
        Log.Info($"[ModDataCheckView] Cancel button created: {(_cancelButton != null)}");

        if (option.OnApply != null)
        {
            Log.Info($"[ModDataCheckView] Creating force apply button");
            (_forceButton, _) =
                _panelBase.AddTitleToolbarButton(L.ModDataCheck.ButtonForceApply, PanelBase.ButtonColorTheme.Red);
            Log.Info($"[ModDataCheckView] Force apply button created: {(_forceButton != null)}");
        }

        // We'll add section titles and buttons to the right panel toolbar later

        // add form at left side
        {
            // Add title for form area
            var formTitleGO = PanelUI.CreateUIObject("FormTitle", _panelBase.LeftPanel.transform);
            var formTitleLE = formTitleGO.AddComponent<LayoutElement>();
            formTitleLE.minHeight = 30f;
            formTitleLE.preferredHeight = 30f;

            var formTitleTMP = formTitleGO.AddComponent<TextMeshProUGUI>();
            formTitleTMP.text = L.ModDataCheck.TitleOperationArea;
            formTitleTMP.fontSize = 14f;
            formTitleTMP.color = Color.white;
            formTitleTMP.alignment = TextAlignmentOptions.Left;
            formTitleTMP.fontStyle = FontStyles.Bold;
            formTitleTMP.raycastTarget = false;

            // create a label for button to subscribe auto update
            var infoLabelGO = PanelUI.CreateUIObject("InfoLabel", _panelBase.LeftPanel.transform);
            var infoLabelTMP = infoLabelGO.AddComponent<TextMeshProUGUI>();
            infoLabelTMP.text = L.ModDataCheck.LabelSubscribeAutoUpdate;
            infoLabelTMP.fontSize = 14f;
            infoLabelTMP.color = Color.white;
            infoLabelTMP.alignment = TextAlignmentOptions.TopLeft;
            // 禁用 raycastTarget 防止拦截按钮点击
            infoLabelTMP.raycastTarget = false;
            var infoLabelLE = infoLabelGO.AddComponent<LayoutElement>();
            infoLabelLE.minHeight = 30f;
            infoLabelLE.preferredHeight = 30f;

            // create a button to subscribe auto update
            var subscribeButtonGO =
                PanelUI.CreateUIObject("SubscribeButton", _panelBase.LeftPanel.transform);

            // 添加 LayoutElement 确保按钮有正确的尺寸
            var subscribeButtonLE = subscribeButtonGO.AddComponent<LayoutElement>();
            subscribeButtonLE.minHeight = 40f;
            subscribeButtonLE.preferredHeight = 50f;

            var subscribeButton = subscribeButtonGO.AddComponent<Button>();
            var subscribeButtonImage = subscribeButtonGO.AddComponent<Image>();

            // 使用UIResourceManager增强按钮样式
            var successSprite = UIResourceManager.GetSprite("success_bg");
            subscribeButtonImage.sprite = successSprite;
            subscribeButtonImage.type = Image.Type.Sliced;
            subscribeButtonImage.color = Color.white;

            // 配置按钮的颜色过渡效果
            var buttonColors = subscribeButton.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = new Color(1f, 0.9f, 0.9f, 1f);
            buttonColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            subscribeButton.colors = buttonColors;
            subscribeButton.targetGraphic = subscribeButtonImage;

            // 添加边框效果
            var subscribeOutline = subscribeButtonGO.AddComponent<Outline>();
            subscribeOutline.effectColor = new Color(0.2f, 0.7f, 0.3f, 1f);
            subscribeOutline.effectDistance = new Vector2(2, 2);

            var subscribeButtonTextGO = PanelUI.CreateUIObject("Text", subscribeButtonGO.transform);
            var subscribeButtonText = subscribeButtonTextGO.AddComponent<TextMeshProUGUI>();
            subscribeButtonText.text = L.ModDataCheck.ButtonSubscribeAutoUpdate;
            subscribeButtonText.fontSize = 15f;
            subscribeButtonText.color = Color.white;
            subscribeButtonText.alignment = TextAlignmentOptions.Center;
            // 禁用文本的 raycastTarget，让按钮的 Image 接收点击
            subscribeButtonText.raycastTarget = false;
            var subscribeButtonTextRect = subscribeButtonTextGO.GetComponent<RectTransform>();
            subscribeButtonTextRect.anchorMin = Vector2.zero;
            subscribeButtonTextRect.anchorMax = Vector2.one;
            subscribeButtonTextRect.offsetMin = Vector2.zero;
            subscribeButtonTextRect.offsetMax = Vector2.zero;
            subscribeButton.onClick.AddListener(() =>
            {
                Log.Info("[ModDataCheck] 订阅所有需要更新的 Mod 的自动更新");
                _vm?.SubscribeAutoUpdateToAllNeededMods();
            });

            // create a label for learn to dev mod by myself
            var learnLabelGO = PanelUI.CreateUIObject("LearnLabel", _panelBase.LeftPanel.transform);
            var learnLabelTMP = learnLabelGO.AddComponent<TextMeshProUGUI>();
            learnLabelTMP.text = L.ModDataCheck.LabelLearnToDevMod;
            learnLabelTMP.fontSize = 14f;
            learnLabelTMP.color = Color.white;
            learnLabelTMP.alignment = TextAlignmentOptions.TopLeft;
            // 禁用 raycastTarget 防止拦截按钮点击
            learnLabelTMP.raycastTarget = false;
            var learnLabelLE = learnLabelGO.AddComponent<LayoutElement>();
            learnLabelLE.minHeight = 30f;
            learnLabelLE.preferredHeight = 30f;

            // create a button to open dev mod tutorial
            var learnButtonGO =
                PanelUI.CreateUIObject("LearnButton", _panelBase.LeftPanel.transform);
            // 添加 LayoutElement 确保按钮有正确的尺寸
            var learnButtonLE = learnButtonGO.AddComponent<LayoutElement>();
            learnButtonLE.minHeight = 40f;
            learnButtonLE.preferredHeight = 50f;
            var learnButton = learnButtonGO.AddComponent<Button>();
            var learnButtonImage = learnButtonGO.AddComponent<Image>();

            // 使用UIResourceManager增强按钮样式
            var infoSprite = UIResourceManager.GetSprite("square_bg");
            learnButtonImage.sprite = infoSprite;
            learnButtonImage.type = Image.Type.Sliced;
            learnButtonImage.color = Color.white;

            // 配置按钮的颜色过渡效果
            var learnButtonColors = learnButton.colors;
            learnButtonColors.normalColor = Color.white;
            learnButtonColors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
            learnButtonColors.pressedColor = new Color(0.7f, 0.7f, 0.8f, 1f);
            learnButton.colors = learnButtonColors;
            learnButton.targetGraphic = learnButtonImage;

            // 添加边框效果
            var learnOutline = learnButtonGO.AddComponent<Outline>();
            learnOutline.effectColor = new Color(0.3f, 0.3f, 0.7f, 1f);
            learnOutline.effectDistance = new Vector2(2, 2);
            var learnButtonTextGO = PanelUI.CreateUIObject("Text", learnButtonGO.transform);
            var learnButtonText = learnButtonTextGO.AddComponent<TextMeshProUGUI>();
            learnButtonText.text = L.ModDataCheck.ButtonLearnToDevMod;
            learnButtonText.fontSize = 15f;
            learnButtonText.color = Color.white;
            learnButtonText.alignment = TextAlignmentOptions.Center;
            // 禁用文本的 raycastTarget，让按钮的 Image 接收点击
            learnButtonText.raycastTarget = false;
            var learnButtonTextRect = learnButtonTextGO.GetComponent<RectTransform>();
            learnButtonTextRect.anchorMin = Vector2.zero;
            learnButtonTextRect.anchorMax = Vector2.one;
            learnButtonTextRect.offsetMin = Vector2.zero;
            learnButtonTextRect.offsetMax = Vector2.zero;
            learnButton.onClick.AddListener(() => { Application.OpenURL(LinkProvider.Instance.GetSampleRepoLink()); });

            // create a label to display tip for resubscribe manually
            var tipLabelGO = PanelUI.CreateUIObject("TipLabel", _panelBase.LeftPanel.transform);
            var tipLabelTMP = tipLabelGO.AddComponent<TextMeshProUGUI>();
            tipLabelTMP.text = L.ModDataCheck.LabelResubscribeTip;
            tipLabelTMP.fontSize = 15f;
            tipLabelTMP.color = Color.white;
            tipLabelTMP.alignment = TextAlignmentOptions.TopLeft;
            // 禁用 raycastTarget 防止拦截按钮点击
            tipLabelTMP.raycastTarget = false;
            // 启用自动换行并允许换行后根据容器宽度调整高度
            tipLabelTMP.enableWordWrapping = true;
            tipLabelTMP.overflowMode = TextOverflowModes.Overflow;
            // 使 RectTransform 横向拉伸以匹配父容器宽度
            var tipLabelRect = tipLabelGO.GetComponent<RectTransform>();
            tipLabelRect.anchorMin = new Vector2(0f, 1f);
            tipLabelRect.anchorMax = new Vector2(1f, 1f);
            tipLabelRect.offsetMin = Vector2.zero;
            tipLabelRect.offsetMax = Vector2.zero;
            // 添加 LayoutElement 与 ContentSizeFitter：让布局系统按父宽度布局并根据文本内容计算高度
            var tipLabelLE = tipLabelGO.AddComponent<LayoutElement>();
            tipLabelLE.flexibleWidth = 1f;
            var tipLabelFitter = tipLabelGO.AddComponent<ContentSizeFitter>();
            tipLabelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            tipLabelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }


        // Add section titles and buttons to the PanelBase's built-in RightToolbar with proper two-column alignment
        {
            Log.Info("[ModDataCheckView] Setting up built-in RightToolbar");

            // Configure PanelBase's built-in RightToolbar
            var rightToolbar = _panelBase.RightToolbar;
            _rightToolbarGO = rightToolbar.gameObject;

            // Add LayoutElement to control toolbar height
            var rightToolbarLE = _rightToolbarGO.GetComponent<LayoutElement>() ??
                                 _rightToolbarGO.AddComponent<LayoutElement>();
            rightToolbarLE.minHeight = 32f; // Fixed toolbar height
            rightToolbarLE.preferredHeight = 32f;
            rightToolbarLE.flexibleHeight = 0f; // Don't allow expansion

            // Set up horizontal layout for two-column alignment matching content
            var rightToolbarLayout = _rightToolbarGO.GetComponent<HorizontalLayoutGroup>() ??
                                     _rightToolbarGO.AddComponent<HorizontalLayoutGroup>();
            rightToolbarLayout.spacing = 5f; // Same spacing as content
            rightToolbarLayout.padding = new RectOffset(0, 0, 0, 0); // No padding
            rightToolbarLayout.childForceExpandWidth = true; // Same as content
            rightToolbarLayout.childControlWidth = true;
            rightToolbarLayout.childControlHeight = true; // Control child heights

            // Left section container (aligns with active items area)
            var activeSectionGO = PanelUI.CreateUIObject("ActiveSection", _rightToolbarGO.transform);
            var activeSectionLE = activeSectionGO.AddComponent<LayoutElement>();
            activeSectionLE.flexibleWidth = 1f; // Takes half the space

            var activeSectionLayout = activeSectionGO.AddComponent<HorizontalLayoutGroup>();
            activeSectionLayout.spacing = 5f;
            activeSectionLayout.padding = new RectOffset(5, 5, 4, 4);
            activeSectionLayout.childForceExpandWidth = false;
            activeSectionLayout.childControlWidth = true; // Allow control of child widths
            activeSectionLayout.childControlHeight = true;
            activeSectionLayout.childForceExpandHeight = false; // Don't expand vertically
            activeSectionLayout.childAlignment = TextAnchor.MiddleLeft; // Left align for title

            // Active section title
            var activeTitleGO = PanelUI.CreateUIObject("ActiveTitle", activeSectionGO.transform);
            var activeTitleLE = activeTitleGO.AddComponent<LayoutElement>();
            activeTitleLE.flexibleWidth = 1f; // Title takes remaining space

            var activeTitleTMP = activeTitleGO.AddComponent<TextMeshProUGUI>();
            activeTitleTMP.text = L.ModDataCheck.TitleActiveItems;
            activeTitleTMP.fontSize = 14f;
            activeTitleTMP.color = Color.white;
            activeTitleTMP.fontStyle = FontStyles.Bold;
            activeTitleTMP.alignment = TextAlignmentOptions.Left; // Left align
            activeTitleTMP.raycastTarget = false;

            // Ignore all button - fix dimensions (width should be larger than height)
            var ignoreAllToolbarButtonGO = PanelUI.CreateUIObject("IgnoreAllToolbarButton", activeSectionGO.transform);
            var ignoreAllToolbarLE = ignoreAllToolbarButtonGO.AddComponent<LayoutElement>();
            ignoreAllToolbarLE.minWidth = 100f; // Wider button
            ignoreAllToolbarLE.preferredWidth = 100f;
            ignoreAllToolbarLE.minHeight = 24f; // Fixed height
            ignoreAllToolbarLE.preferredHeight = 24f;
            ignoreAllToolbarLE.ignoreLayout = false; // Participate in layout but with fixed size

            var ignoreAllToolbarButton = ignoreAllToolbarButtonGO.AddComponent<Button>();
            var ignoreAllToolbarImage = ignoreAllToolbarButtonGO.AddComponent<Image>();

            var warningSprite = UIResourceManager.GetSprite("warning_bg");
            ignoreAllToolbarImage.sprite = warningSprite;
            ignoreAllToolbarImage.type = Image.Type.Sliced;
            ignoreAllToolbarImage.color = new Color(1f, 0.8f, 0.8f, 1f);

            var ignoreAllToolbarColors = ignoreAllToolbarButton.colors;
            ignoreAllToolbarColors.normalColor = new Color(1f, 0.8f, 0.8f, 1f);
            ignoreAllToolbarColors.highlightedColor = new Color(0.9f, 0.7f, 0.7f, 1f);
            ignoreAllToolbarColors.pressedColor = new Color(0.8f, 0.6f, 0.6f, 1f);
            ignoreAllToolbarButton.colors = ignoreAllToolbarColors;
            ignoreAllToolbarButton.targetGraphic = ignoreAllToolbarImage;

            var ignoreAllToolbarTextGO = PanelUI.CreateUIObject("Text", ignoreAllToolbarButtonGO.transform);
            var ignoreAllToolbarText = ignoreAllToolbarTextGO.AddComponent<TextMeshProUGUI>();
            ignoreAllToolbarText.text = L.ModDataCheck.ButtonIgnoreAll;
            ignoreAllToolbarText.fontSize = 12f;
            ignoreAllToolbarText.color = Color.black;
            ignoreAllToolbarText.alignment = TextAlignmentOptions.Center;
            ignoreAllToolbarText.raycastTarget = false;
            var ignoreAllToolbarTextRect = ignoreAllToolbarTextGO.GetComponent<RectTransform>();
            ignoreAllToolbarTextRect.anchorMin = Vector2.zero;
            ignoreAllToolbarTextRect.anchorMax = Vector2.one;
            ignoreAllToolbarTextRect.offsetMin = Vector2.zero;
            ignoreAllToolbarTextRect.offsetMax = Vector2.zero;

            // Right section container (aligns with ignored items area)
            var ignoredSectionGO = PanelUI.CreateUIObject("IgnoredSection", _rightToolbarGO.transform);
            var ignoredSectionLE = ignoredSectionGO.AddComponent<LayoutElement>();
            ignoredSectionLE.flexibleWidth = 1f; // Takes half the space

            var ignoredSectionLayout = ignoredSectionGO.AddComponent<HorizontalLayoutGroup>();
            ignoredSectionLayout.spacing = 5f;
            ignoredSectionLayout.padding = new RectOffset(5, 5, 4, 4);
            ignoredSectionLayout.childForceExpandWidth = false;
            ignoredSectionLayout.childControlWidth = true; // Allow control of child widths
            ignoredSectionLayout.childControlHeight = true;
            ignoredSectionLayout.childForceExpandHeight = false; // Don't expand vertically
            ignoredSectionLayout.childAlignment = TextAnchor.MiddleLeft; // Left align for title

            // Ignored section title
            var ignoredTitleGO = PanelUI.CreateUIObject("IgnoredTitle", ignoredSectionGO.transform);
            var ignoredTitleLE = ignoredTitleGO.AddComponent<LayoutElement>();
            ignoredTitleLE.flexibleWidth = 1f; // Title takes remaining space

            var ignoredTitleTMP = ignoredTitleGO.AddComponent<TextMeshProUGUI>();
            ignoredTitleTMP.text = L.ModDataCheck.TitleIgnoredItems;
            ignoredTitleTMP.fontSize = 14f;
            ignoredTitleTMP.color = Color.white;
            ignoredTitleTMP.fontStyle = FontStyles.Bold;
            ignoredTitleTMP.alignment = TextAlignmentOptions.Left; // Left align
            ignoredTitleTMP.raycastTarget = false;

            // Restore all button - fix dimensions (width should be larger than height)
            var restoreAllToolbarButtonGO =
                PanelUI.CreateUIObject("RestoreAllToolbarButton", ignoredSectionGO.transform);
            var restoreAllToolbarLE = restoreAllToolbarButtonGO.AddComponent<LayoutElement>();
            restoreAllToolbarLE.minWidth = 100f; // Wider button
            restoreAllToolbarLE.preferredWidth = 100f;
            restoreAllToolbarLE.minHeight = 24f; // Fixed height
            restoreAllToolbarLE.preferredHeight = 24f;
            restoreAllToolbarLE.ignoreLayout = false; // Participate in layout but with fixed size

            var restoreAllToolbarButton = restoreAllToolbarButtonGO.AddComponent<Button>();
            var restoreAllToolbarImage = restoreAllToolbarButtonGO.AddComponent<Image>();

            var successSprite = UIResourceManager.GetSprite("success_bg");
            restoreAllToolbarImage.sprite = successSprite;
            restoreAllToolbarImage.type = Image.Type.Sliced;
            restoreAllToolbarImage.color = new Color(0.8f, 1f, 0.8f, 1f);

            var restoreAllToolbarColors = restoreAllToolbarButton.colors;
            restoreAllToolbarColors.normalColor = new Color(0.8f, 1f, 0.8f, 1f);
            restoreAllToolbarColors.highlightedColor = new Color(0.7f, 0.9f, 0.7f, 1f);
            restoreAllToolbarColors.pressedColor = new Color(0.6f, 0.8f, 0.6f, 1f);
            restoreAllToolbarButton.colors = restoreAllToolbarColors;
            restoreAllToolbarButton.targetGraphic = restoreAllToolbarImage;

            var restoreAllToolbarTextGO = PanelUI.CreateUIObject("Text", restoreAllToolbarButtonGO.transform);
            var restoreAllToolbarText = restoreAllToolbarTextGO.AddComponent<TextMeshProUGUI>();
            restoreAllToolbarText.text = L.ModDataCheck.ButtonRestoreAll;
            restoreAllToolbarText.fontSize = 12f;
            restoreAllToolbarText.color = Color.black;
            restoreAllToolbarText.alignment = TextAlignmentOptions.Center;
            restoreAllToolbarText.raycastTarget = false;
            var restoreAllToolbarTextRect = restoreAllToolbarTextGO.GetComponent<RectTransform>();
            restoreAllToolbarTextRect.anchorMin = Vector2.zero;
            restoreAllToolbarTextRect.anchorMax = Vector2.one;
            restoreAllToolbarTextRect.offsetMin = Vector2.zero;
            restoreAllToolbarTextRect.offsetMax = Vector2.zero;

            // Update button references to use right toolbar buttons
            _ignoreAllButton = ignoreAllToolbarButton;
            _restoreAllButton = restoreAllToolbarButton;
        }

        // create the right side content panel with split layout
        {
            Log.Info("[ModDataCheckView] Creating right side split layout");
            // Create a split container for active and ignored items
            var splitContainerGO =
                PanelUI.CreateUIObject("SplitContainer", _panelBase.RightPanelLayout.gameObject.transform);
            var splitContainerLE = splitContainerGO.AddComponent<LayoutElement>();
            splitContainerLE.flexibleHeight = 1f;
            splitContainerLE.preferredHeight = 300f;

            var splitLayout = splitContainerGO.AddComponent<HorizontalLayoutGroup>();
            splitLayout.spacing = 5f;
            splitLayout.padding = new RectOffset(0, 0, 0, 0);
            splitLayout.childForceExpandWidth = true;
            splitLayout.childControlWidth = true;
            splitLayout.childControlHeight = true;
            splitLayout.childAlignment = TextAnchor.UpperLeft;

            var splitRT = splitContainerGO.GetComponent<RectTransform>();
            splitRT.anchorMin = Vector2.zero;
            splitRT.anchorMax = Vector2.one;
            splitRT.sizeDelta = Vector2.zero;
            Log.Info("[ModDataCheckView] Split container created");

            // Active items container (left half) - simplified, no title/button
            var activeAreaGO = PanelUI.CreateUIObject("ActiveArea", splitContainerGO.transform);
            var activeAreaLE = activeAreaGO.AddComponent<LayoutElement>();
            activeAreaLE.flexibleWidth = 1f;

            var activeAreaLayout = activeAreaGO.AddComponent<VerticalLayoutGroup>();
            activeAreaLayout.spacing = 5f;
            activeAreaLayout.padding = new RectOffset(5, 5, 5, 5);
            activeAreaLayout.childForceExpandWidth = true;
            activeAreaLayout.childControlWidth = true;
            activeAreaLayout.childControlHeight = true;
            activeAreaLayout.childAlignment = TextAnchor.UpperLeft;

            // Active items ScrollView
            _activeContainerGO = CreateScrollContainer(activeAreaGO, null);
            var activeLE = _activeContainerGO.AddComponent<LayoutElement>();
            activeLE.flexibleHeight = 1f;
            Log.Info($"[ModDataCheckView] Created active container: {_activeContainerGO.name}");

            // Ignored items container (right half) - simplified, no title/button
            var ignoredAreaGO = PanelUI.CreateUIObject("IgnoredArea", splitContainerGO.transform);
            var ignoredAreaLE = ignoredAreaGO.AddComponent<LayoutElement>();
            ignoredAreaLE.flexibleWidth = 1f;

            var ignoredAreaLayout = ignoredAreaGO.AddComponent<VerticalLayoutGroup>();
            ignoredAreaLayout.spacing = 5f;
            ignoredAreaLayout.padding = new RectOffset(5, 5, 5, 5);
            ignoredAreaLayout.childForceExpandWidth = true;
            ignoredAreaLayout.childControlWidth = true;
            ignoredAreaLayout.childControlHeight = true;
            ignoredAreaLayout.childAlignment = TextAnchor.UpperLeft;

            // Ignored items ScrollView
            _ignoredContainerGO = CreateScrollContainer(ignoredAreaGO, null);
            var ignoredLE = _ignoredContainerGO.AddComponent<LayoutElement>();
            ignoredLE.flexibleHeight = 1f;
            Log.Info($"[ModDataCheckView] Created ignored container: {_ignoredContainerGO.name}");
        }

        _disposableBag.Add(_panelBase);

        // 布局完成后，手动触发一次数据更新来填充UI
        Log.Info("[ModDataCheckView] Layout completed, triggering initial data update");
        UpdateChecklist(_vm.CurrentCheckResult);
    }

    /// <summary>
    /// 创建符合最佳实践的滚动容器
    /// </summary>
    private static GameObject CreateScrollContainer(GameObject parent, string? title)
    {
        // 创建 ScrollView 容器
        var scrollViewGO = new GameObject($"{title ?? "Content"}ScrollView");
        scrollViewGO.transform.SetParent(parent.transform, false);

        // ScrollView 必须添加 LayoutElement
        var scrollViewLE = scrollViewGO.AddComponent<LayoutElement>();
        scrollViewLE.flexibleHeight = 1f;
        scrollViewLE.preferredHeight = 300f;

        // 正确的 RectTransform 设置：ScrollView 填充父容器
        var scrollViewRT = scrollViewGO.GetComponent<RectTransform>();
        if (scrollViewRT == null)
        {
            scrollViewRT = scrollViewGO.AddComponent<RectTransform>();
        }

        scrollViewRT.anchorMin = Vector2.zero;
        scrollViewRT.anchorMax = Vector2.one;
        scrollViewRT.offsetMin = Vector2.zero;
        scrollViewRT.offsetMax = Vector2.zero;

        // 添加 ScrollRect 组件，灵敏度固定为 3
        var scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 3;

        // 创建 Viewport - 使用 RectMask2D + Image
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollViewGO.transform, false);

        // 正确的 RectTransform 设置：Viewport 填充 ScrollView
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        if (viewportRT == null)
        {
            viewportRT = viewportGO.AddComponent<RectTransform>();
        }

        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        // Viewport 必须有 RectMask2D 和 Image
        viewportGO.AddComponent<RectMask2D>();
        var viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);

        var viewportOutline = viewportGO.AddComponent<Outline>();
        viewportOutline.effectColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);
        viewportOutline.effectDistance = new Vector2(1, 1);

        scrollRect.viewport = viewportRT;

        // 创建 Content（从顶部开始）
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);

        // 正确的 Content RectTransform 设置
        var contentRT = contentGO.GetComponent<RectTransform>();
        if (contentRT == null)
        {
            contentRT = contentGO.AddComponent<RectTransform>();
        }

        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        scrollRect.content = contentRT;

        // Content 必须有 VerticalLayoutGroup + ContentSizeFitter
        var vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 5f;
        vLayout.padding = new RectOffset(10, 10, 10, 10);
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childAlignment = TextAnchor.UpperLeft;

        var sizeFitter = contentGO.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 返回 ScrollView，调用者需要获取其 Content 子对象
        return scrollViewGO;
    }

    /// <summary>
    /// 获取 ScrollView 的 Content 对象
    /// </summary>
    private static GameObject GetScrollContent(GameObject scrollView)
    {
        // 层级：ScrollView -> Viewport -> Content
        var viewport = scrollView.transform.GetChild(0);
        return viewport.GetChild(0).gameObject;
    }

    /// <summary>
    /// 调试容器状态
    /// </summary>
    private void DebugContainerStatus()
    {
        Log.Info($"[ModDataCheckView] === Container Status Debug ===");
        Log.Info($"[ModDataCheckView] ActiveContainer exists: {_activeContainerGO != null}");
        if (_activeContainerGO != null)
        {
            Log.Info($"[ModDataCheckView] ActiveContainer active: {_activeContainerGO.activeInHierarchy}");
            var activeContent = GetScrollContent(_activeContainerGO);
            Log.Info($"[ModDataCheckView] ActiveContent child count: {activeContent.transform.childCount}");
        }

        Log.Info($"[ModDataCheckView] IgnoredContainer exists: {_ignoredContainerGO != null}");
        if (_ignoredContainerGO != null)
        {
            Log.Info($"[ModDataCheckView] IgnoredContainer active: {_ignoredContainerGO.activeInHierarchy}");
            var ignoredContent = GetScrollContent(_ignoredContainerGO);
            Log.Info($"[ModDataCheckView] IgnoredContent child count: {ignoredContent.transform.childCount}");
        }

        Log.Info($"[ModDataCheckView] === End Container Status Debug ===");
    }
}

internal readonly struct ModDataCheckViewButtonOption
{
    public readonly string BackText;
    public readonly Action? OnApply;

    public ModDataCheckViewButtonOption(string backText, Action? onApply)
    {
        BackText = backText;
        OnApply = onApply;
    }
}
