namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

internal class UploadPanelVm : IDisposable
{
    internal UploadPanelData _data;

    private DisposableBag _disposableBag;

    private readonly ReactiveProperty<ModMetadata> _Metadata;
    private readonly ReactiveProperty<UploadForm> _form;
    private readonly ReactiveProperty<ChecklistItem[]> _checklist;

    // 新增：版本响应式属性
    private readonly ReactiveProperty<string> _version;

    // 14 个 Steam Tags 的 ReactiveProperty（需对 View 可访问，使用 internal）
    private readonly ReactiveProperty<bool> _weaponTag;
    private readonly ReactiveProperty<bool> _equipmentAndGearTag = null!;
    private readonly ReactiveProperty<bool> _lootAndEconomyTag = null!;
    private readonly ReactiveProperty<bool> _qualityOfLifeTag = null!;
    private readonly ReactiveProperty<bool> _cheatsAndExploitsTag = null!;
    private readonly ReactiveProperty<bool> _visualEnhancementsTag = null!;
    private readonly ReactiveProperty<bool> _soundTag = null!;
    private readonly ReactiveProperty<bool> _questAndProgressionTag = null!;
    private readonly ReactiveProperty<bool> _companionAndNpcTag = null!;
    private readonly ReactiveProperty<bool> _collectiblesTag = null!;
    private readonly ReactiveProperty<bool> _gameplayTag = null!;
    private readonly ReactiveProperty<bool> _multiplayerAndCoOpTag = null!;
    private readonly ReactiveProperty<bool> _utilityTag = null!;
    private readonly ReactiveProperty<bool> _medicalAndSurvivalTag = null!;

    public Observable<string> Title { get; }
    public Observable<string> ChangeNotes { get; }
    public Observable<bool> AllowBMLTail { get; }
    public Observable<bool> UpdateSteamDescription { get; }
    public Observable<ChecklistItem[]> Checklist { get; }

    // 新增：版本响应式属性暴露
    public Observable<string> Version => _version;

    // Tags 暴露只读 Observable
    public Observable<bool> WeaponTag => _weaponTag;
    public Observable<bool> EquipmentAndGearTag => _equipmentAndGearTag;
    public Observable<bool> LootAndEconomyTag => _lootAndEconomyTag;
    public Observable<bool> QualityOfLifeTag => _qualityOfLifeTag;
    public Observable<bool> CheatsAndExploitsTag => _cheatsAndExploitsTag;
    public Observable<bool> VisualEnhancementsTag => _visualEnhancementsTag;
    public Observable<bool> SoundTag => _soundTag;
    public Observable<bool> QuestAndProgressionTag => _questAndProgressionTag;
    public Observable<bool> CompanionAndNPCTag => _companionAndNpcTag;
    public Observable<bool> CollectiblesTag => _collectiblesTag;
    public Observable<bool> GameplayTag => _gameplayTag;
    public Observable<bool> MultiplayerAndCoOpTag => _multiplayerAndCoOpTag;
    public Observable<bool> UtilityTag => _utilityTag;
    public Observable<bool> MedicalAndSurvivalTag => _medicalAndSurvivalTag;

    public Observable<bool> AllChecklistPassed =>
        Checklist.Select(items => items.All(i => i.Status != ChecklistStatus.Fail));

    private readonly Subject<BmlSteamItemManager.WorkshopUploadProgress> _uploadSubject = new();
    internal Observable<BmlSteamItemManager.WorkshopUploadProgress> UploadProgressObservable { get; }

    public UploadPanelVm(UploadPanelData data)
    {
        _data = data;

        _Metadata = new ReactiveProperty<ModMetadata>(data.Metadata)
            .AddTo(ref _disposableBag);
        _form = new ReactiveProperty<UploadForm>(data.Form)
            .AddTo(ref _disposableBag);
        _checklist = new ReactiveProperty<ChecklistItem[]>(data.Checklist ?? [])
            .AddTo(ref _disposableBag);

        // 初始化 Tag ReactiveProperty（从 data.Form.Tags 读取初始值）
        _weaponTag = new ReactiveProperty<bool>(data.Form.Tags.Weapon).AddTo(ref _disposableBag);
        _equipmentAndGearTag = new ReactiveProperty<bool>(data.Form.Tags.EquipmentAndGear).AddTo(ref _disposableBag);
        _lootAndEconomyTag = new ReactiveProperty<bool>(data.Form.Tags.LootAndEconomy).AddTo(ref _disposableBag);
        _qualityOfLifeTag = new ReactiveProperty<bool>(data.Form.Tags.QualityOfLife).AddTo(ref _disposableBag);
        _cheatsAndExploitsTag = new ReactiveProperty<bool>(data.Form.Tags.CheatsAndExploits).AddTo(ref _disposableBag);
        _visualEnhancementsTag =
            new ReactiveProperty<bool>(data.Form.Tags.VisualEnhancements).AddTo(ref _disposableBag);
        _soundTag = new ReactiveProperty<bool>(data.Form.Tags.Sound).AddTo(ref _disposableBag);
        _questAndProgressionTag =
            new ReactiveProperty<bool>(data.Form.Tags.QuestAndProgression).AddTo(ref _disposableBag);
        _companionAndNpcTag = new ReactiveProperty<bool>(data.Form.Tags.CompanionAndNPC).AddTo(ref _disposableBag);
        _collectiblesTag = new ReactiveProperty<bool>(data.Form.Tags.Collectibles).AddTo(ref _disposableBag);
        _gameplayTag = new ReactiveProperty<bool>(data.Form.Tags.Gameplay).AddTo(ref _disposableBag);
        _multiplayerAndCoOpTag =
            new ReactiveProperty<bool>(data.Form.Tags.MultiplayerAndCoOp).AddTo(ref _disposableBag);
        _utilityTag = new ReactiveProperty<bool>(data.Form.Tags.Utility).AddTo(ref _disposableBag);
        _medicalAndSurvivalTag =
            new ReactiveProperty<bool>(data.Form.Tags.MedicalAndSurvival).AddTo(ref _disposableBag);

        // 新增：初始化版本响应式属性
        _version = new ReactiveProperty<string>(data.Form.Version).AddTo(ref _disposableBag);

        // TODO localize
        Title = _Metadata
            .Select(x => $"上传 Mod - {x.DisplayName}")
            .DistinctUntilChanged();
        ChangeNotes = _form
            .Select(x => x.ChangeNotes)
            .DistinctUntilChanged();
        AllowBMLTail = _form
            .Select(x => x.AllowBmlTail)
            .DistinctUntilChanged();
        UpdateSteamDescription = _form
            .Select(x => x.UpdateSteamDescription)
            .DistinctUntilChanged();
        Checklist = _checklist.AsObservable();

        // 合并 Subject 和 BmlSteamItemManager 的事件为一个 Observable，并提供初始值
        UploadProgressObservable = Observable.Merge(
            Observable.Return(new BmlSteamItemManager.WorkshopUploadProgress
            {
                Status = BmlSteamItemManager.WorkshopUploadStatus.Pending,
                Message = "等待上传...",
                BytesProcessed = 0,
                BytesTotal = 0
            }),
            _uploadSubject,
            Observable.FromEvent<BmlSteamItemManager.WorkshopUploadProgress>(
                h => BmlSteamItemManager.OnUploadProgress += h,
                h => BmlSteamItemManager.OnUploadProgress -= h
            )
        );
    }

    public void ToggleAllowBMLTail(bool enabled)
    {
        _data.Form.AllowBmlTail = enabled;
        ModOptions.Save(CK.Upload.AllowBmlTail, enabled);
    }

    public void ToggleUpdateSteamDescription(bool enabled)
    {
        _data.Form.UpdateSteamDescription = enabled;
        ModOptions.Save(CK.Upload.UpdateSteamDescription, enabled);
    }

    public void SetChangeNotes(string changeNotes)
    {
        _data.Form.ChangeNotes = changeNotes;
    }

    public void UpdateSteamId()
    {
        ModManager.Rescan();
        var info = ModManager.modInfos.FirstOrDefault(x => x.path == _data.Metadata.ModFolder);
        if (info.publishedFileId > 0)
        {
            _data.Metadata.PublishedFileId = info.publishedFileId;
            Log.Debug($"[UploadPanelVm] Updated Steam ID to {_data.Metadata.PublishedFileId}");
        }
    }

    public async UniTask StartUpload()
    {
        _uploadSubject.OnNext(new BmlSteamItemManager.WorkshopUploadProgress
        {
            Status = BmlSteamItemManager.WorkshopUploadStatus.Preparing,
            Message = "准备上传..."
        });

        const string tail =
            "[hr][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3596571766]Submit via BML[/url]";
        var changeNotes = _data.Form.AllowBmlTail
            ? _data.Form.ChangeNotes + tail
            : _data.Form.ChangeNotes;
        var uploadDesc = _data.Form.UpdateSteamDescription || _data.Metadata.PublishedFileId <= 0;

        _uploadSubject.OnNext(new BmlSteamItemManager.WorkshopUploadProgress
        {
            Status = BmlSteamItemManager.WorkshopUploadStatus.RequestingId,
            Message = "开始请求 Steam 上传..."
        });
        Log.Debug($"[UploadPanelVm] Upload description: {uploadDesc}");

        var currentForm = GetCurrentForm();
        await BmlSteamItemManager.Instance!.UploadWorkshopItem(_data.Metadata.ModFolder,
            currentForm.Tags,
            uploadDesc,
            _data.Form.AllowBmlTail ? tail : null,
            changeNotes);
    }

    /// <summary>
    /// 根据当前 ReactiveProperty 构建最新表单（包含 Tags）
    /// </summary>
    /// <returns>UploadForm</returns>
    private UploadForm GetCurrentForm()
    {
        return new UploadForm
        {
            ChangeNotes = _data.Form.ChangeNotes,
            AllowBmlTail = _data.Form.AllowBmlTail,
            UpdateSteamDescription = _data.Form.UpdateSteamDescription,
            Tags = new WorkshopTags
            {
                Weapon = _data.Form.Tags.Weapon,
                EquipmentAndGear = _data.Form.Tags.EquipmentAndGear,
                LootAndEconomy = _data.Form.Tags.LootAndEconomy,
                QualityOfLife = _data.Form.Tags.QualityOfLife,
                CheatsAndExploits = _data.Form.Tags.CheatsAndExploits,
                VisualEnhancements = _data.Form.Tags.VisualEnhancements,
                Sound = _data.Form.Tags.Sound,
                QuestAndProgression = _data.Form.Tags.QuestAndProgression,
                CompanionAndNPC = _data.Form.Tags.CompanionAndNPC,
                Collectibles = _data.Form.Tags.Collectibles,
                Gameplay = _data.Form.Tags.Gameplay,
                MultiplayerAndCoOp = _data.Form.Tags.MultiplayerAndCoOp,
                Utility = _data.Form.Tags.Utility,
                MedicalAndSurvival = _data.Form.Tags.MedicalAndSurvival,
            }
        };
    }

    public void SetTagWeapon(bool value)
    {
        _weaponTag.Value = value;
        _data.Form.Tags.Weapon = value;
    }

    public void SetTagEquipmentAndGear(bool value)
    {
        _equipmentAndGearTag.Value = value;
        _data.Form.Tags.EquipmentAndGear = value;
    }

    public void SetTagLootAndEconomy(bool value)
    {
        _lootAndEconomyTag.Value = value;
        _data.Form.Tags.LootAndEconomy = value;
    }

    public void SetTagQualityOfLife(bool value)
    {
        _qualityOfLifeTag.Value = value;
        _data.Form.Tags.QualityOfLife = value;
    }

    public void SetTagCheatsAndExploits(bool value)
    {
        _cheatsAndExploitsTag.Value = value;
        _data.Form.Tags.CheatsAndExploits = value;
    }

    public void SetTagVisualEnhancements(bool value)
    {
        _visualEnhancementsTag.Value = value;
        _data.Form.Tags.VisualEnhancements = value;
    }

    public void SetTagSound(bool value)
    {
        _soundTag.Value = value;
        _data.Form.Tags.Sound = value;
    }

    public void SetTagQuestAndProgression(bool value)
    {
        _questAndProgressionTag.Value = value;
        _data.Form.Tags.QuestAndProgression = value;
    }

    public void SetTagCompanionAndNPC(bool value)
    {
        _companionAndNpcTag.Value = value;
        _data.Form.Tags.CompanionAndNPC = value;
    }

    public void SetTagCollectibles(bool value)
    {
        _collectiblesTag.Value = value;
        _data.Form.Tags.Collectibles = value;
    }

    public void SetTagGameplay(bool value)
    {
        _gameplayTag.Value = value;
        _data.Form.Tags.Gameplay = value;
    }

    public void SetTagMultiplayerAndCoOp(bool value)
    {
        _multiplayerAndCoOpTag.Value = value;
        _data.Form.Tags.MultiplayerAndCoOp = value;
    }

    public void SetTagUtility(bool value)
    {
        _utilityTag.Value = value;
        _data.Form.Tags.Utility = value;
    }

    public void SetTagMedicalAndSurvival(bool value)
    {
        _medicalAndSurvivalTag.Value = value;
        _data.Form.Tags.MedicalAndSurvival = value;
    }

    // 新增：版本设置方法
    public void SetVersion(string version)
    {
        _version.Value = version;
        _data.Form.Version = version;
    }

    public void Dispose()
    {
        _disposableBag.Dispose();
    }
}
