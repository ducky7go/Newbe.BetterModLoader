using Steamworks;

namespace Newbe.BetterModLoader.ModDataChecks;

internal class ModDataChecksVm : IDisposable
{
    private ModDataCheckResult _data;
    private DisposableBag _disposableBag;

    private readonly ReactiveProperty<ModDataCheckResult> _checkResult;

    public Observable<ModDataCheckResult> CheckResult { get; }
    public ModDataCheckResult CurrentCheckResult => _checkResult.Value;

    public ModDataChecksVm(ModDataCheckResult data)
    {
        _data = data;

        Log.Info(
            $"[ModDataChecksVm] Constructor called with AllItems: {data.AllItems.Count}, ActiveItems: {data.ActiveItems.Count}, IgnoredItems: {data.IgnoredItems.Count}");

        // Data already has ignored state applied from Show() methods
        _checkResult = new ReactiveProperty<ModDataCheckResult>(data)
            .AddTo(ref _disposableBag);

        CheckResult = _checkResult.AsObservable();

        Log.Info("[ModDataChecksVm] Constructor completed");
    }

    public void Dispose()
    {
        _disposableBag.Dispose();
    }

    public void SubscribeAutoUpdateToAllNeededMods()
    {
        SteamFriends.ActivateGameOverlayToWebPage(CK.Links.AutoUpdateModSteamWorkshopPage);
    }

    /// <summary>
    /// 忽略指定的检查项
    /// </summary>
    public void IgnoreItem(CheckResultItem item)
    {
        if (item != null)
        {
            item.IsIgnored = true;
            SaveIgnoredState();
            // Force UI refresh by triggering the property change
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 恢复指定的检查项
    /// </summary>
    public void RestoreItem(CheckResultItem item)
    {
        if (item != null)
        {
            item.IsIgnored = false;
            SaveIgnoredState();
            // Force UI refresh by triggering the property change
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 强制触发状态变化通知
    /// </summary>
    private void NotifyStateChanged()
    {
        // Create a new ModDataCheckResult instance to force ReactiveProperty to detect change
        var newData = new ModDataCheckResult();

        // Copy all items with their current ignore states
        foreach (var item in _data.AllItems)
        {
            newData.AllItems.Add(item);
        }

        // Update the data reference and trigger change
        _data = newData;
        _checkResult.Value = _data;
    }

    /// <summary>
    /// 忽略所有当前检查项
    /// </summary>
    public void IgnoreAll()
    {
        foreach (var item in _data.ActiveItems)
        {
            item.IsIgnored = true;
        }

        SaveIgnoredState();
        NotifyStateChanged();
    }

    /// <summary>
    /// 恢复所有被忽略的检查项
    /// </summary>
    public void RestoreAll()
    {
        foreach (var item in _data.IgnoredItems)
        {
            item.IsIgnored = false;
        }

        SaveIgnoredState();
        NotifyStateChanged();
    }

    /// <summary>
    /// 自动移除已解决问题的忽略项
    /// </summary>
    public void CleanupResolvedIgnoredItems()
    {
        var hasChanges = false;

        // 移除不再存在于当前检查结果中的忽略项
        var ignoredIds = ModDataChecksConfigs.GetIgnoredChecks();
        var currentIds = GetAllCheckIds();

        foreach (var ignoredId in ignoredIds)
        {
            if (!currentIds.Contains(ignoredId))
            {
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            // 重新过滤忽略列表，只保留当前存在的项目
            var validIgnoredIds = ignoredIds.Intersect(currentIds).ToList();
            ModDataChecksConfigs.SetIgnoredChecks(validIgnoredIds);

            // 重新应用忽略状态到当前数据
            var updatedIgnoredIds = ModDataChecksConfigs.GetIgnoredChecks();
            foreach (var item in _data.AllItems)
            {
                item.IsIgnored = updatedIgnoredIds.Contains(item.Id);
            }

            _checkResult.Value = _data;
        }
    }

    /// <summary>
    /// 获取所有当前检查项的ID列表
    /// </summary>
    private HashSet<string> GetAllCheckIds()
    {
        var ids = new HashSet<string>();

        foreach (var item in _data.AllItems)
        {
            ids.Add(item.Id);
        }

        return ids;
    }


    /// <summary>
    /// 保存忽略状态到ModOptions
    /// </summary>
    private void SaveIgnoredState()
    {
        var ignoredIds = new List<string>();

        foreach (var item in _data.IgnoredItems)
        {
            ignoredIds.Add(item.Id);
        }

        ModDataChecksConfigs.SetIgnoredChecks(ignoredIds);
    }
}
