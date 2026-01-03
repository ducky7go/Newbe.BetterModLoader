using System.Text;
using Newbe.BetterModLoader.Audio;

namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

internal partial class NewUploadPanel : MonoBehaviour
{
    public static void Show(ModMetadata info)
    {
        Log.Debug("[NewUploadPanel] Show called, creating new panel instance.");
        var go = new GameObject("NewUploadPanel");
        // 由 UIFactory.CreatePanelBase 自动创建顶层 Canvas，无需挂载到主 Canvas
        var panel = go.AddComponent<NewUploadPanel>();
        var uploadPanelData = CreateData(info);
        var vm = new UploadPanelVm(uploadPanelData);
        panel.Init(vm);
    }

    private static UploadPanelData CreateData(ModMetadata metadata)
    {
        var dllPath = metadata.DllPath;
        var modFolderPath = Path.GetDirectoryName(dllPath) ?? string.Empty;
        var checkList = new List<ChecklistItem>();
        var initSuccess = ModManager.TryProcessModFolder(modFolderPath, out _);
        checkList.Add(ChecklistItem.IniCheck(initSuccess
            ? ChecklistStatus.Success
            : ChecklistStatus.Fail));
        var previewImagePath = Path.Combine(modFolderPath, "preview.png");
        var previewImageExists = File.Exists(previewImagePath);
        checkList.Add(ChecklistItem.PreviewImageCheck(previewImageExists
            ? ChecklistStatus.Success
            : ChecklistStatus.Fail));

        // Add preview image size validation
        if (previewImageExists)
        {
            var previewFileInfo = new FileInfo(previewImagePath);
            const long maxPreviewImageSize = 1024 * 1024; // 1MB
            checkList.Add(ChecklistItem.PreviewImageSizeCheck(previewFileInfo.Length <= maxPreviewImageSize
                ? ChecklistStatus.Success
                : ChecklistStatus.Fail));
        }

        checkList.Add(ChecklistItem.SteamIdCheck(metadata.PublishedFileId > 0
            ? ChecklistStatus.Success
            : ChecklistStatus.Unavailable));
        // Multi-language description check
        var availableLanguages = MultiLanguageContentHelper.GetAvailableLanguages(modFolderPath);
        if (availableLanguages.Count > 1)
        {
            checkList.Add(ChecklistItem.MultiLanguageDescriptionCheck(ChecklistStatus.Success));

            // Add description size validation for multi-language descriptions
            const long maxDescriptionSize = 8 * 1024; // 8KB
            var descriptionDir = Path.Combine(modFolderPath, "description");

            foreach (var language in availableLanguages)
            {
                var descriptionPath = Path.Combine(descriptionDir, $"{language}.md");
                if (File.Exists(descriptionPath))
                {
                    var descriptionContent = File.ReadAllText(descriptionPath);
                    var descriptionSize = Encoding.UTF8.GetByteCount(descriptionContent);
                    checkList.Add(ChecklistItem.DescriptionSizeCheck(
                        descriptionSize <= maxDescriptionSize ? ChecklistStatus.Success : ChecklistStatus.Fail,
                        language));
                }
            }
        }
        else
        {
            // Always fail if no multilingual descriptions found
            checkList.Add(ChecklistItem.MultiLanguageDescriptionCheck(ChecklistStatus.Unavailable));
        }

        // Multi-language title check
        var availableTitleLanguages = MultiLanguageContentHelper.GetAvailableTitleLanguages(modFolderPath);
        var titleStatus = availableTitleLanguages.Count > 1 ? ChecklistStatus.Success : ChecklistStatus.Unavailable;
        checkList.Add(ChecklistItem.MultiLanguageTitleCheck(titleStatus));

        var tags = new WorkshopTags();

        // 新增：从 info.ini 自动填充版本和标签
        var tagsStr = metadata.Tags.Split([','], StringSplitOptions.RemoveEmptyEntries);
        tags.Fill(tagsStr);

        var data = new UploadPanelData(metadata)
        {
            Checklist = checkList.ToArray(),
            Form =
            {
                AllowBmlTail = ModOptions.Load(CK.Upload.AllowBmlTail, true),
                UpdateSteamDescription =
                    metadata.PublishedFileId <= 0 || ModOptions.Load(CK.Upload.UpdateSteamDescription, false),
                Tags = tags,
                Version = metadata.Version,
            }
        };
        return data;
    }
}

internal partial class NewUploadPanel
{
    private UploadPanelVm? _vm;
    private DisposableBag _disposableBag;
    private Toggle _updateSteamDescriptionToggle = null!;
    private Toggle _allowBmlTailToggle = null!;
    private Slider _uploadProgressSlider = null!;
    private TextMeshProUGUI _uploadProgressText = null!;

    private TMP_InputField _changeNotesInput = null!;

    // 新增：版本输入字段
    private TMP_InputField _versionInput = null!;
    private GameObject _checklistContainer = null!;
    private Button _uploadBtn = null!;
    private Button _closeBtn = null!;
    private TextMeshProUGUI _uploadBtnText = null!;
    private readonly List<ChecklistItemView> _checklistViews = [];

    // Tags 14 个 Toggle
    private Toggle _weaponToggle = null!;
    private Toggle _equipmentAndGearToggle = null!;
    private Toggle _lootAndEconomyToggle = null!;
    private Toggle _qualityOfLifeToggle = null!;
    private Toggle _cheatsAndExploitsToggle = null!;
    private Toggle _visualEnhancementsToggle = null!;
    private Toggle _soundToggle = null!;
    private Toggle _questAndProgressionToggle = null!;
    private Toggle _companionAndNpcToggle = null!;
    private Toggle _collectiblesToggle = null!;
    private Toggle _gameplayToggle = null!;
    private Toggle _multiplayerAndCoOpToggle = null!;
    private Toggle _utilityToggle = null!;
    private Toggle _medicalAndSurvivalToggle = null!;

    private void OnDestroy()
    {
        _disposableBag.Dispose();
    }

    public void Init(UploadPanelVm vm)
    {
        _vm = vm;
        _disposableBag.Add(_vm);
        Layout();

        _vm.Title
            .Subscribe(x => { _panelBase.TitleText.text = x; })
            .AddTo(ref _disposableBag);

        _vm.AllowBMLTail
            .Subscribe(x => { _allowBmlTailToggle.isOn = x; })
            .AddTo(ref _disposableBag);

        _vm.UpdateSteamDescription
            .Subscribe(x => { _updateSteamDescriptionToggle.isOn = x; })
            .AddTo(ref _disposableBag);

        // VM -> UI Tags
        _vm.WeaponTag.Subscribe(v => _weaponToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.EquipmentAndGearTag.Subscribe(v => _equipmentAndGearToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.LootAndEconomyTag.Subscribe(v => _lootAndEconomyToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.QualityOfLifeTag.Subscribe(v => _qualityOfLifeToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.CheatsAndExploitsTag.Subscribe(v => _cheatsAndExploitsToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.VisualEnhancementsTag.Subscribe(v => _visualEnhancementsToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.SoundTag.Subscribe(v => _soundToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.QuestAndProgressionTag.Subscribe(v => _questAndProgressionToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.CompanionAndNPCTag.Subscribe(v => _companionAndNpcToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.CollectiblesTag.Subscribe(v => _collectiblesToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.GameplayTag.Subscribe(v => _gameplayToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.MultiplayerAndCoOpTag.Subscribe(v => _multiplayerAndCoOpToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.UtilityTag.Subscribe(v => _utilityToggle.isOn = v).AddTo(ref _disposableBag);
        _vm.MedicalAndSurvivalTag.Subscribe(v => _medicalAndSurvivalToggle.isOn = v).AddTo(ref _disposableBag);

        _vm.Checklist
            .Subscribe(items =>
            {
                if (items != null)
                {
                    UpdateChecklistView(items);
                }
            })
            .AddTo(ref _disposableBag);
        _vm.AllChecklistPassed
            .Subscribe(allPassed =>
            {
                if (allPassed)
                {
                    _uploadBtn.interactable = true;
                    _uploadBtnText.text = "开始上传";
                }
                else
                {
                    _uploadBtn.interactable = false;
                    _uploadBtnText.text = "检查未通过，无法上传";
                }
            })
            .AddTo(ref _disposableBag);
        _vm.UploadProgressObservable
            .Subscribe(progress =>
            {
                Log.Current.Debug(
                    $"[UploadProgress] Status: {progress.Status}, Message: {progress.Message}, Bytes: {progress.BytesProcessed}/{progress.BytesTotal}");

                switch (progress.Status)
                {
                    case BmlSteamItemManager.WorkshopUploadStatus.Pending:
                        _uploadProgressSlider.value = 0f;
                        _uploadProgressText.text = progress.Message;
                        EnableUserOperation();
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.Preparing:
                        _uploadProgressSlider.value = 0f;
                        _uploadProgressText.text = progress.Message;
                        DisableUserOperation();
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.RequestingId:
                        _uploadProgressSlider.value = 0.2f;
                        _uploadProgressText.text = progress.Message;
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.WritingIni:
                        _uploadProgressSlider.value = 0.3f;
                        _uploadProgressText.text = progress.Message;
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.StartingSteamUpload:
                        _uploadProgressSlider.value = 0.4f;
                        _uploadProgressText.text = progress.Message;
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.UploadingContent:
                        var percent = progress.BytesTotal > 0
                            ? 0.4f + 0.5f * ((float)progress.BytesProcessed / progress.BytesTotal)
                            : 0.4f;
                        _uploadProgressSlider.value = percent;
                        _uploadProgressText.text = progress.BytesTotal > 0
                            ? $"{progress.Message} ({percent:P0})"
                            : progress.Message;
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.UploadingTitles:
                        _uploadProgressSlider.value = 0.9f;
                        _uploadProgressText.text = progress.Message;
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.UploadingTranslations:
                        _uploadProgressSlider.value = 0.95f;
                        _uploadProgressText.text = progress.Message;
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.Completed:
                        _uploadProgressSlider.value = 1f;
                        _uploadProgressText.text = progress.Message;
                        EnableUserOperation();
                        break;
                    case BmlSteamItemManager.WorkshopUploadStatus.Failed:
                        _uploadProgressSlider.value = 0f;
                        _uploadProgressText.text = $"{L.Upload.ProgressFailed}{progress.Message}";
                        EnableUserOperation();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            })
            .AddTo(ref _disposableBag);

        _vm.UploadProgressObservable
            .Where(x => x.IsSuccess)
            .Subscribe(_ => { _vm.UpdateSteamId(); })
            .AddTo(ref _disposableBag);

        // bind input field changes to vm
        _changeNotesInput.OnValueChangedAsObservable()
            .Subscribe(text => { _vm!.SetChangeNotes(text); })
            .AddTo(ref _disposableBag);
        _allowBmlTailToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.ToggleAllowBMLTail(isOn);
            })
            .AddTo(ref _disposableBag);
        _updateSteamDescriptionToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.ToggleUpdateSteamDescription(isOn);
            })
            .AddTo(ref _disposableBag);

        // UI -> VM Tags with audio feedback
        _weaponToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagWeapon(isOn);
            })
            .AddTo(ref _disposableBag);

        _equipmentAndGearToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagEquipmentAndGear(isOn);
            })
            .AddTo(ref _disposableBag);

        _lootAndEconomyToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagLootAndEconomy(isOn);
            })
            .AddTo(ref _disposableBag);

        _qualityOfLifeToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagQualityOfLife(isOn);
            })
            .AddTo(ref _disposableBag);

        _cheatsAndExploitsToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagCheatsAndExploits(isOn);
            })
            .AddTo(ref _disposableBag);

        _visualEnhancementsToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagVisualEnhancements(isOn);
            })
            .AddTo(ref _disposableBag);

        _soundToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagSound(isOn);
            })
            .AddTo(ref _disposableBag);

        _questAndProgressionToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagQuestAndProgression(isOn);
            })
            .AddTo(ref _disposableBag);

        _companionAndNpcToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagCompanionAndNPC(isOn);
            })
            .AddTo(ref _disposableBag);

        _collectiblesToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagCollectibles(isOn);
            })
            .AddTo(ref _disposableBag);

        _gameplayToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagGameplay(isOn);
            })
            .AddTo(ref _disposableBag);

        _multiplayerAndCoOpToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagMultiplayerAndCoOp(isOn);
            })
            .AddTo(ref _disposableBag);

        _utilityToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagUtility(isOn);
            })
            .AddTo(ref _disposableBag);

        _medicalAndSurvivalToggle.OnValueChangedAsObservable()
            .Subscribe(isOn =>
            {
                BMLAudioManager.PlaySoundStatic(isOn ? AudioEffectType.ToggleOn : AudioEffectType.ToggleOff);
                _vm!.SetTagMedicalAndSurvival(isOn);
            })
            .AddTo(ref _disposableBag);
        _uploadBtn.OnClickAsObservable()
            .Subscribe(_ =>
            {
                BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                UploadToSteam().Forget();
            })
            .AddTo(ref _disposableBag);
    }

    private async UniTask UploadToSteam()
    {
        // Check if all validations passed before proceeding with upload
        var allChecklistPassed = await _vm!.AllChecklistPassed.FirstAsync();
        if (!allChecklistPassed)
        {
            // Log specific validation failures for debugging
            var failedItems = _vm._data.Checklist.Where(item => item.Status == ChecklistStatus.Fail).ToArray();
            foreach (var failedItem in failedItems)
            {
                Log.Warn($"[UploadToSteam] Validation failed: {failedItem.Description} - {failedItem.FailReason}");
            }

            Log.Warn("[UploadToSteam] Upload cancelled due to validation failures");
            // Show error message to user (existing UI will show failed checklist items)
            return;
        }

        var data = _vm._data;
        ModOptions.ForName.SaveConfig($"tags_for_{data.Metadata.Id}", data.Form.Tags);
        await _vm!.StartUpload();
    }

    private void DisableUserOperation()
    {
        _uploadBtn.interactable = false;
        _closeBtn.interactable = false;
        _changeNotesInput.interactable = false;
        _allowBmlTailToggle.interactable = false;
        _updateSteamDescriptionToggle.interactable = false;
    }

    private void EnableUserOperation()
    {
        _uploadBtn.interactable = true;
        _closeBtn.interactable = true;
        _changeNotesInput.interactable = true;
        _allowBmlTailToggle.interactable = true;
        _updateSteamDescriptionToggle.interactable = true;
    }

    private void UpdateChecklistView(ChecklistItem[] items)
    {
        // 清除现有的 checklist items
        foreach (var view in _checklistViews)
        {
            if (view != null && view.gameObject != null)
            {
                Destroy(view.gameObject);
            }
        }

        _checklistViews.Clear();

        // 创建新的 checklist items
        foreach (var item in items)
        {
            var view = ChecklistItemView.Create(_checklistContainer);
            view.UpdateView(item);
            _checklistViews.Add(view);
        }
    }
}

internal partial class NewUploadPanel
{
    private PanelBase _panelBase = null!;

    private void Layout()
    {
        // 创建 PanelBase（已自动包含 toolbar/title 行布局）
        _panelBase = PanelUI.CreatePanelBase();

        // 配置 LeftToolbar 的 HorizontalLayoutGroup
        (_closeBtn, _) = _panelBase.AddTitleToolbarButton(L.Upload.ButtonBack, PanelBase.ButtonColorTheme.SkyBlue);
        _closeBtn.onClick.AddListener(() =>
        {
            BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
            Destroy(gameObject);
        });
        (_uploadBtn, _uploadBtnText) =
            _panelBase.AddTitleToolbarButton(L.Upload.ButtonBack, PanelBase.ButtonColorTheme.Green);

        // add form at left side
        {
            // create label for change notes
            var changeNotesLabel = new GameObject("ChangeNotesLabel");
            var labelText = changeNotesLabel.AddComponent<TextMeshProUGUI>();
            labelText.text = L.Upload.LabelChangeNotes;
            labelText.fontSize = 18;
            labelText.color = Color.white;
            changeNotesLabel.transform.SetParent(_panelBase.LeftPanelLayout.transform, false);
            var labelLE = changeNotesLabel.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 30f;
            labelLE.flexibleHeight = 0f;

            // create input field for change notes
            var changeNotesInput = new GameObject("ChangeNotesInput");
            changeNotesInput.transform.SetParent(_panelBase.LeftPanelLayout.transform, false);

            // Add background image for input field
            var inputBg = changeNotesInput.AddComponent<Image>();
            inputBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            var inputField = changeNotesInput.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

            // Create text component for input field
            var textComponent = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            textComponent.transform.SetParent(changeNotesInput.transform, false);
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            PanelUI.SetAnchors(textComponent.gameObject, Vector2.zero, Vector2.one);
            var textRect = textComponent.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            inputField.textComponent = textComponent;

            // Create placeholder
            var placeholder = new GameObject("Placeholder").AddComponent<TextMeshProUGUI>();
            placeholder.transform.SetParent(changeNotesInput.transform, false);
            placeholder.text = L.Upload.LabelChangeNotesPlaceholder;
            placeholder.fontSize = 16;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholder.fontStyle = FontStyles.Italic;
            PanelUI.SetAnchors(placeholder.gameObject, Vector2.zero, Vector2.one);
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.offsetMin = new Vector2(5, 5);
            placeholderRect.offsetMax = new Vector2(-5, -5);

            inputField.placeholder = placeholder;
            _changeNotesInput = inputField;

            var inputLE = changeNotesInput.AddComponent<LayoutElement>();
            inputLE.preferredHeight = 150f;
            inputLE.flexibleHeight = 0f;

            // create toggle for AllowBMLTail
            _allowBmlTailToggle = PanelUI.CreateToggle(_panelBase.LeftPanel, L.Upload.LabelAllowBmlFooter, 18);

            // create toggle for UpdateSteamDescription
            _updateSteamDescriptionToggle =
                PanelUI.CreateToggle(_panelBase.LeftPanel, L.Upload.LabelUpdateSteamDesc, 18);

            // create tags section
            CreateTagsSection(_panelBase.LeftPanel);
        }

        // create right side content panel
        {
            // 创建上传进度条
            var (slider, centerText) = PanelUI.CreateProgressBar(_panelBase.RightPanel, L.Upload.ProgressTitle, 30f);
            _uploadProgressSlider = slider;
            _uploadProgressText = centerText;

            // 创建 Checklist 标题
            var checklistTitleGO = new GameObject("ChecklistTitle");
            var checklistTitle = checklistTitleGO.AddComponent<TextMeshProUGUI>();
            checklistTitle.text = L.Upload.ChecklistTitle;
            checklistTitle.fontSize = 18;
            checklistTitle.color = Color.white;
            checklistTitle.alignment = TextAlignmentOptions.Left;
            checklistTitleGO.transform.SetParent(_panelBase.RightPanel.transform, false);
            var titleLE = checklistTitleGO.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 30f;
            titleLE.flexibleHeight = 0f;

            // 创建 Checklist 容器（简单的垂直布局容器，不使用 ScrollView）
            _checklistContainer = CreateChecklistContainer(_panelBase.RightPanel);
        }

        _disposableBag.Add(_panelBase);
    }

    private void CreateTagsSection(GameObject parent)
    {
        // 创建标题
        var titleGO = new GameObject("TagsTitle");
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Workshop Tags";
        titleText.fontSize = 18;
        titleText.color = Color.white;
        titleGO.transform.SetParent(parent.transform, false);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 30f;
        titleLE.flexibleHeight = 0f;

        // 创建容器（带背景）
        var containerGO = new GameObject("TagsContainer");
        containerGO.transform.SetParent(parent.transform, false);
        var containerBg = containerGO.AddComponent<Image>();
        containerBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        var containerLE = containerGO.AddComponent<LayoutElement>();
        containerLE.preferredHeight = 240f;
        containerLE.flexibleHeight = 0f;

        // 创建网格布局容器
        var gridLayout = containerGO.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(240f, 30f);
        gridLayout.spacing = new Vector2(10f, 8f);
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        // 创建14个标签
        _weaponToggle = CreateCompactToggle(containerGO, L.Upload.TagWeapon);
        _equipmentAndGearToggle = CreateCompactToggle(containerGO, L.Upload.TagEquipmentAndGear);
        _lootAndEconomyToggle = CreateCompactToggle(containerGO, L.Upload.TagLootAndEconomy);
        _qualityOfLifeToggle = CreateCompactToggle(containerGO, L.Upload.TagQualityOfLife);
        _cheatsAndExploitsToggle = CreateCompactToggle(containerGO, L.Upload.TagCheatsAndExploits);
        _visualEnhancementsToggle = CreateCompactToggle(containerGO, L.Upload.TagVisualEnhancements);
        _soundToggle = CreateCompactToggle(containerGO, L.Upload.TagSound);
        _questAndProgressionToggle = CreateCompactToggle(containerGO, L.Upload.TagQuestAndProgression);
        _companionAndNpcToggle = CreateCompactToggle(containerGO, L.Upload.TagCompanionAndNPC);
        _collectiblesToggle = CreateCompactToggle(containerGO, L.Upload.TagCollectibles);
        _gameplayToggle = CreateCompactToggle(containerGO, L.Upload.TagGameplay);
        _multiplayerAndCoOpToggle = CreateCompactToggle(containerGO, L.Upload.TagMultiplayerAndCoOp);
        _utilityToggle = CreateCompactToggle(containerGO, L.Upload.TagUtility);
        _medicalAndSurvivalToggle = CreateCompactToggle(containerGO, L.Upload.TagMedicalAndSurvival);
    }

    private static Toggle CreateCompactToggle(GameObject parent, string labelText)
    {
        // 创建根对象
        var toggleGO = new GameObject($"Toggle_{labelText}");
        toggleGO.transform.SetParent(parent.transform, false);

        // 添加 Toggle 组件
        var toggle = toggleGO.AddComponent<Toggle>();

        // 添加背景
        var bgImage = toggleGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
        toggle.targetGraphic = bgImage;

        // 创建水平布局
        var hLayout = toggleGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8f;
        hLayout.padding = new RectOffset(8, 8, 5, 5);
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;

        // 创建 Checkmark 容器
        var checkmarkBgGO = new GameObject("Checkmark Background");
        checkmarkBgGO.transform.SetParent(toggleGO.transform, false);
        var checkmarkBgRect = checkmarkBgGO.AddComponent<RectTransform>();
        checkmarkBgRect.sizeDelta = new Vector2(18f, 18f);

        var checkmarkBgImage = checkmarkBgGO.AddComponent<Image>();
        checkmarkBgImage.color = new Color(0.15f, 0.15f, 0.17f, 1f);

        var checkmarkBgLE = checkmarkBgGO.AddComponent<LayoutElement>();
        checkmarkBgLE.preferredWidth = 18f;
        checkmarkBgLE.preferredHeight = 18f;
        checkmarkBgLE.flexibleWidth = 0f;
        checkmarkBgLE.flexibleHeight = 0f;

        // 创建 Checkmark
        var checkmarkGO = new GameObject("Checkmark");
        checkmarkGO.transform.SetParent(checkmarkBgGO.transform, false);
        var checkmarkRect = checkmarkGO.AddComponent<RectTransform>();
        PanelUI.SetAnchors(checkmarkGO, Vector2.zero, Vector2.one);
        checkmarkRect.offsetMin = new Vector2(3, 3);
        checkmarkRect.offsetMax = new Vector2(-3, -3);

        var checkmarkImage = checkmarkGO.AddComponent<Image>();
        checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        toggle.graphic = checkmarkImage;

        // 创建标签文本
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(toggleGO.transform, false);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = 14;
        labelTMP.color = Color.white;
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.enableWordWrapping = false;
        labelTMP.overflowMode = TextOverflowModes.Ellipsis;

        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f;
        labelLE.minWidth = 100f;

        // 设置 Toggle 转换
        toggle.transition = Selectable.Transition.ColorTint;
        var colors = toggle.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        // 统一设置禁用状态为红色
        colors.disabledColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        toggle.colors = colors;

        return toggle;
    }

    private static GameObject CreateChecklistContainer(GameObject parent)
    {
        // 创建容器
        var containerGO = new GameObject("ChecklistContainer");
        containerGO.transform.SetParent(parent.transform, false);

        var containerLE = containerGO.AddComponent<LayoutElement>();
        containerLE.flexibleHeight = 1f; // 占据剩余空间
        containerLE.preferredHeight = 450f; // 增加高度以适应更长的提示文本

        // 添加背景
        var containerBg = containerGO.AddComponent<Image>();
        containerBg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        // 添加垂直布局
        var vLayout = containerGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 10f;
        vLayout.padding = new RectOffset(15, 15, 15, 15);
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childAlignment = TextAnchor.UpperLeft;

        return containerGO;
    }
}

internal class ChecklistItemView : MonoBehaviour
{
    public Image statusIndicator = null!;
    public TextMeshProUGUI descriptionText = null!;
    public TextMeshProUGUI failReasonText = null!;

    public void UpdateView(ChecklistItem item)
    {
        if (statusIndicator == null || descriptionText == null || failReasonText == null)
        {
            return;
        }

        switch (item.Status)
        {
            case ChecklistStatus.Success:
                statusIndicator.color = Color.green;
                descriptionText.color = Color.white;
                failReasonText.gameObject.SetActive(false);
                break;
            case ChecklistStatus.Fail:
                statusIndicator.color = Color.red;
                descriptionText.color = Color.white;
                failReasonText.color = Color.red;
                failReasonText.text = $"{L.Upload.CheckFailPrefix}{item.FailReason}";
                failReasonText.gameObject.SetActive(true);
                break;
            case ChecklistStatus.Unavailable:
                statusIndicator.color = Color.gray;
                descriptionText.color = Color.gray;
                failReasonText.color = Color.gray;
                failReasonText.text = $"{L.Upload.CheckFailPrefix}{item.FailReason}";
                failReasonText.gameObject.SetActive(true);
                break;
        }

        descriptionText.text = item.Description;
    }

    /// <summary>
    /// 创建一个 ChecklistItem 视图元素（作为 prefab）
    /// </summary>
    /// <param name="parent">父对象</param>
    /// <returns>ChecklistItemView 组件</returns>
    public static ChecklistItemView Create(GameObject parent)
    {
        // 创建容器
        var itemGO = PanelUI.CreateUIObject("ChecklistItem", parent.transform);
        var itemLE = itemGO.AddComponent<LayoutElement>();
        itemLE.preferredHeight = -1f; // 不使用固定高度
        itemLE.flexibleHeight = 0f;

        // 添加背景
        var bgImg = itemGO.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);

        // 创建主水平布局
        var hLayout = itemGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 10f;
        hLayout.padding = new RectOffset(10, 10, 5, 5);
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // 状态指示器（圆点）
        var indicatorGO = PanelUI.CreateUIObject("StatusIndicator", itemGO.transform);
        var indicatorImg = indicatorGO.AddComponent<Image>();
        indicatorImg.color = Color.gray;
        var indicatorRect = indicatorGO.GetComponent<RectTransform>();
        indicatorRect.sizeDelta = new Vector2(16, 16);

        var indicatorLE = indicatorGO.AddComponent<LayoutElement>();
        indicatorLE.preferredWidth = 16f;
        indicatorLE.preferredHeight = 16f;
        indicatorLE.flexibleWidth = 0f;
        indicatorLE.flexibleHeight = 0f;

        // 创建内容容器（垂直布局）
        var contentGO = PanelUI.CreateUIObject("Content", itemGO.transform);
        var vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 2f;
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;

        var contentLE = contentGO.AddComponent<LayoutElement>();
        contentLE.flexibleWidth = 1f;

        // 描述文本
        var descGO = PanelUI.CreateUIObject("Description", contentGO.transform);
        var descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 16;
        descText.color = Color.white;
        descText.alignment = TextAlignmentOptions.Left;
        var descLE = descGO.AddComponent<LayoutElement>();
        descLE.flexibleWidth = 1f;

        // 失败原因文本（默认隐藏）
        var failReasonGO = PanelUI.CreateUIObject("FailReason", contentGO.transform);
        var failReasonText = failReasonGO.AddComponent<TextMeshProUGUI>();
        failReasonText.fontSize = 14;
        failReasonText.color = new Color(1f, 0.5f, 0.5f, 1f);
        failReasonText.alignment = TextAlignmentOptions.Left;
        failReasonText.enableWordWrapping = true; // 启用文本换行
        failReasonText.overflowMode = TextOverflowModes.Truncate; // 防止文本溢出容器

        // 添加组件并赋值
        var view = itemGO.AddComponent<ChecklistItemView>();

        view.statusIndicator = indicatorImg;
        view.descriptionText = descText;
        view.failReasonText = failReasonText;

        return view;
    }

    /// <summary>
    /// 解析 info.ini 文件以获取版本和标签信息用于上传表单自动填充
    /// </summary>
    /// <param name="metadata">模组元数据</param>
    /// <returns>元组 (版本字符串, WorkshopTags, 是否自动填充了标签)</returns>
    private static (string version, WorkshopTags tags, bool tagsPopulated) ParseInfoIniForUpload(ModMetadata metadata)
    {
        var version = string.Empty;
        var workshopTags = new WorkshopTags();
        var tagsPopulated = false;

        try
        {
            var infoIniPath = Path.Combine(metadata.ModFolder, "info.ini");
            if (!File.Exists(infoIniPath))
            {
                return (version, workshopTags, tagsPopulated);
            }

            var content = File.ReadAllText(infoIniPath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || !trimmedLine.Contains("="))
                    continue;

                var parts = trimmedLine.Split('=', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key.ToLowerInvariant())
                {
                    case "version":
                        if (!string.IsNullOrEmpty(value) && value != "?")
                        {
                            version = value;
                        }

                        break;

                    case "tags":
                        if (!string.IsNullOrEmpty(value) && value != "?")
                        {
                            var tagArray = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim().ToLowerInvariant())
                                .Where(t => !string.IsNullOrEmpty(t))
                                .ToArray();

                            foreach (var tag in tagArray)
                            {
                                switch (tag)
                                {
                                    case "weapon":
                                    case "weapons":
                                        workshopTags.Weapon = true;
                                        break;
                                    case "equipment":
                                    case "gear":
                                        workshopTags.EquipmentAndGear = true;
                                        break;
                                    case "loot":
                                    case "economy":
                                        workshopTags.LootAndEconomy = true;
                                        break;
                                    case "quality of life":
                                    case "qol":
                                        workshopTags.QualityOfLife = true;
                                        break;
                                    case "cheats":
                                    case "exploits":
                                        workshopTags.CheatsAndExploits = true;
                                        break;
                                    case "visual":
                                    case "visual enhancements":
                                        workshopTags.VisualEnhancements = true;
                                        break;
                                    case "sound":
                                    case "audio":
                                        workshopTags.Sound = true;
                                        break;
                                    case "quest":
                                    case "progression":
                                        workshopTags.QuestAndProgression = true;
                                        break;
                                    case "companion":
                                    case "npc":
                                        workshopTags.CompanionAndNPC = true;
                                        break;
                                    case "collectibles":
                                    case "collectable":
                                        workshopTags.Collectibles = true;
                                        break;
                                    case "gameplay":
                                        workshopTags.Gameplay = true;
                                        break;
                                    case "multiplayer":
                                    case "co-op":
                                        workshopTags.MultiplayerAndCoOp = true;
                                        break;
                                    case "utility":
                                    case "tool":
                                        workshopTags.Utility = true;
                                        break;
                                    case "medical":
                                    case "survival":
                                        workshopTags.MedicalAndSurvival = true;
                                        break;
                                }
                            }

                            tagsPopulated = true;
                        }

                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[ParseInfoIniForUpload] Error parsing info.ini: {ex.Message}");
        }

        return (version, workshopTags, tagsPopulated);
    }
}
