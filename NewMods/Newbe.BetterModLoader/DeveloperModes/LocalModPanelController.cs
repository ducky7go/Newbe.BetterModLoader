namespace Newbe.BetterModLoader.DeveloperModes;

/// <summary>
/// 控制器：将列表与详情联动
/// </summary>
internal class LocalModPanelController : MonoBehaviour
{
    public LocalModListUI LocalModListUI = null!;
    public LocalModDetailsUI LocalModDetailsUI = null!;
    public Button btnFolder = null!;
    public Button btnSteam = null!;
    public Button btnUpload = null!;

    /// <summary>
    /// 持有响应式订阅的 DisposableBag
    /// </summary>
    internal DisposableBag _disposables;

    private void Start()
    {
        // 注册列表选中事件，详情面板同步渲染，按钮状态刷新
        Observable
            .FromEvent<LocalModListItem>(
                h => LocalModListUI.OnItemSelected += h,
                h => LocalModListUI.OnItemSelected -= h)
            .Subscribe(item =>
            {
                LocalModDetailsUI.UpdateFrom(item);
                UpdateButtonVisibility();
            })
            .AddTo(ref _disposables);

        // 使用响应式方式订阅按钮点击事件
        SetupButtonSubscriptions();

        // 构建本地 mod 列表
        var infos = ModGroupConfig.Instance.SortedModInfos.Where(x => !x.IsSteamWorkshopMod());
        var localModItems = infos
            .Select(info => new LocalModListItem(info.Id, info.OriginalModInfo, info.GetPreview()))
            .ToList();
        var localModItemList = new LocalModItemList(localModItems);
        LocalModListUI.SetLocalModItemList(localModItemList);

        // 初始化按钮显示状态
        UpdateButtonVisibility();
    }

    /// <summary>
    /// 响应式订阅按钮点击事件
    /// </summary>
    private void SetupButtonSubscriptions()
    {
        btnFolder.OnClickAsObservable()
            .Subscribe(OpenFolder)
            .AddTo(ref _disposables);

        btnSteam.OnClickAsObservable()
            .Subscribe(OpenSteam)
            .AddTo(ref _disposables);

        btnUpload.OnClickAsObservable()
            .Subscribe(OnUploadClicked)
            .AddTo(ref _disposables);
    }

    /// <summary>
    /// MonoBehaviour 生命周期：释放所有订阅
    /// </summary>
    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// 上传按钮点击事件：弹出上传面板
    /// </summary>
    private void OnUploadClicked(Unit _)
    {
        var selectedItem = LocalModDetailsUI.CurrentItem;
        if (selectedItem == null)
        {
            Log.Warn("[LocalModPanelController] Upload: 当前未选择任何 mod");
            return;
        }

        NewUploadPanel.Show(selectedItem.OriginalModInfo.Value);
    }

    /// <summary>
    /// 按当前选中项动态控制按钮显示
    /// </summary>
    private void UpdateButtonVisibility()
    {
        var selectedItem = LocalModDetailsUI.CurrentItem;

        if (selectedItem != null)
        {
            // Folder/Upload 只要有选中项就展示
            btnFolder.gameObject.SetActive(true);
            btnUpload.gameObject.SetActive(true);
            btnSteam.gameObject.SetActive(selectedItem.OriginalModInfo.Value.PublishedFileId > 0);
        }
        else
        {
            btnFolder.gameObject.SetActive(false);
            btnUpload.gameObject.SetActive(false);
            btnSteam.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 打开本地 mod 文件夹
    /// </summary>
    public void OpenFolder(Unit _)
    {
        try
        {
            // 获取当前选中的 mod
            var selectedItem = LocalModDetailsUI.CurrentItem;
            if (selectedItem == null)
            {
                Log.Warn("[DeveloperModePanelUIController] OpenFolder: 当前未选择任何 mod");
                return;
            }

            var modInfo = selectedItem.OriginalModInfo.Value;
            // 优先使用 modInfo.path，如果没有则回退到 Mods 目录
            var folderPath = !string.IsNullOrEmpty(modInfo.ModFolder)
                ? modInfo.ModFolder
                : Application.dataPath + "/../Mods";
            Log.Debug($"[DeveloperModePanelUIController] OpenFolder: {folderPath}");
            Application.OpenURL(folderPath);
        }
        catch (Exception ex)
        {
            Log.Error($"[DeveloperModePanelUIController] OpenFolder error: {ex}");
        }
    }

    /// <summary>
    /// 跳转 Steam Workshop 页面（根据当前选中的 mod）
    /// </summary>
    public void OpenSteam(Unit _)
    {
        try
        {
            var selectedItem = LocalModDetailsUI.CurrentItem;
            if (selectedItem == null)
            {
                Log.Warn("[DeveloperModePanelUIController] OpenSteam: 当前未选择任何 mod");
                return;
            }

            var modInfo = selectedItem.OriginalModInfo.Value;
            // 尝试从 modInfo 的 name 或其它字段推断 workshopId（如 name 为数字则视为 id）
            var workshopId = modInfo.PublishedFileId;
            var url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopId}";
            Log.Debug($"[DeveloperModePanelUIController] OpenSteam: {url}");
            Application.OpenURL(url);
        }
        catch (Exception ex)
        {
            Log.Error($"[DeveloperModePanelUIController] OpenSteam error: {ex}");
        }
    }
}
