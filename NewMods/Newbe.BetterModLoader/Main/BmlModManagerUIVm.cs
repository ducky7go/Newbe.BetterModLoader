using Newbe.BetterModLoader.Deployments;
using Newbe.BetterModLoader.ModDataChecks;

namespace Newbe.BetterModLoader.Main;

internal class BmlModManagerUIVm : IDisposable
{
    private DisposableBag _disposableBag = new();

    /// <summary>
    /// 配置变更信息
    /// </summary>
    private readonly struct ModChanges
    {
        public readonly List<BetterModManager.ModOperation> JobsToDisable;
        public readonly List<BetterModManager.ModOperation> JobsToEnable;
        public readonly bool OrderChanged;

        public ModChanges(List<BetterModManager.ModOperation> jobsToDisable,
            List<BetterModManager.ModOperation> jobsToEnable,
            bool orderChanged)
        {
            JobsToDisable = jobsToDisable;
            JobsToEnable = jobsToEnable;
            OrderChanged = orderChanged;
        }
    }

    // Data Source
    private ModGroupWorkingCopy _workingConfig;
    private ModGroupWorkingCopy _originalConfigCopy;
    private string _originalSnapshot;

    // State
    public ObservableList<ModGroup> Groups { get; } = new();
    public ObservableList<ModInfo2> CurrentGroupMods { get; } = new();

    public ReactiveProperty<ModGroup?> SelectedGroup { get; } = new();

    public ReactiveProperty<bool> IsDirty { get; } = new(false);

    // Commands/Actions
    public Subject<Unit> OnApplyChanges { get; } = new();
    public Subject<Unit> OnDiscardChanges { get; } = new();
    public Subject<Unit> OnRevertRequested { get; } = new();
    public Subject<string> OnCreateGroup { get; } = new();
    public Subject<(ModGroup Group, string NewName)> OnRenameGroup { get; } = new();
    public Subject<ModGroup> OnCancelRename { get; } = new();

    // Events
    public Subject<Unit> RequestClose { get; } = new();
    public Subject<Unit> OnModListRefreshed { get; } = new();
    public Subject<Unit> OnGroupListChanged { get; } = new(); // Signal when group list needs rebuild

    public BmlModManagerUIVm()
    {
        // Initialize Data
        _workingConfig = ModGroupConfig.Instance.CreateWorkingCopy();
        _originalConfigCopy = _workingConfig.DeepCopy();
        _originalSnapshot = _workingConfig.BuildDeterministicSnapshot();

        // Populate Groups
        foreach (var group in _workingConfig.Groups)
        {
            Groups.Add(group);
        }

        // Select first group by default
        if (Groups.Count > 0)
        {
            SelectedGroup.Value = Groups[0];
        }

        // Bind SelectedGroup to CurrentGroupMods
        SelectedGroup.Subscribe(group => { RefreshModList(); }).AddTo(ref _disposableBag);

        // Handle Create Group
        OnCreateGroup.Subscribe(name =>
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_workingConfig.Groups.Any(g => g.Name == name))
            {
                Log.Warn($"[BML] Group '{name}' already exists");
                return;
            }

            var newGroup = _workingConfig.CreateGroup(name);
            Groups.Add(newGroup);
            OnGroupListChanged.OnNext(Unit.Default); // Trigger rebuild
            CheckDirty();
        }).AddTo(ref _disposableBag);

        // Handle Apply
        OnApplyChanges.Subscribe(_ => ApplyChanges()).AddTo(ref _disposableBag);

        // Handle Discard (for the "Cancel and Exit" button)
        OnDiscardChanges.Subscribe(_ => DiscardChanges()).AddTo(ref _disposableBag);

        // Handle Revert (for the red danger button in top-right)
        OnRevertRequested.Subscribe(_ => ShowRevertConfirmation()).AddTo(ref _disposableBag);

        // Handle Rename Group
        OnRenameGroup.Subscribe(data =>
        {
            var (group, newName) = data;
            RenameGroup(group, newName);
        }).AddTo(ref _disposableBag);

        // Handle Cancel Rename (no action needed, just for UI consistency)
        OnCancelRename.Subscribe(_ => { }).AddTo(ref _disposableBag);
    }

    /// <summary>
    /// 设置模组的启用/禁用状态
    /// </summary>
    public void SetModEnabled(string modId, bool enabled)
    {
        var mod = FindMod(modId);
        if (mod == null) return;

        if (mod.IsEnabled == enabled) return; // 状态未改变

        mod.IsEnabled = enabled;
        RefreshModList(); // 刷新UI显示
        CheckDirty();
    }

    /// <summary>
    /// 切换模组的启用/禁用状态
    /// </summary>
    public void ToggleModEnabled(string modId)
    {
        var mod = FindMod(modId);
        if (mod == null) return;

        mod.IsEnabled = !mod.IsEnabled;
        RefreshModList(); // 刷新UI显示
        CheckDirty();
    }

    /// <summary>
    /// 启用当前分组中的所有模组
    /// </summary>
    public void EnableAllModsInCurrentGroup()
    {
        var group = SelectedGroup.Value;
        if (group == null) return;

        var anyChanged = false;
        foreach (var mod in group.Mods)
        {
            if (!mod.IsEnabled)
            {
                mod.IsEnabled = true;
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            RefreshModList();
            CheckDirty();
        }
    }

    /// <summary>
    /// 禁用当前分组中的所有模组
    /// </summary>
    public void DisableAllModsInCurrentGroup()
    {
        var group = SelectedGroup.Value;
        if (group == null) return;

        var anyChanged = false;
        foreach (var mod in group.Mods)
        {
            if (mod.IsEnabled)
            {
                mod.IsEnabled = false;
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            RefreshModList();
            CheckDirty();
        }
    }

    private ModInfo2? FindMod(string modId)
    {
        foreach (var group in _workingConfig.Groups)
        {
            var mod = group.Mods.FirstOrDefault(m => m.Id == modId);
            if (mod != null) return mod;
        }

        return null;
    }

    public void MoveMod(string modId, string targetGroupName)
    {
        var targetGroup = _workingConfig.Groups.FirstOrDefault(g => g.Name == targetGroupName);
        if (targetGroup == null) return;

        if (targetGroup.Tags != null && targetGroup.Tags.Contains(ModGroupTags.Locked))
        {
            ToastManager.ShowWarning(L.Group.LockedAddRemoveWarning);
            return;
        }

        ModInfo2? modInfo = null;
        ModGroup? fromGroup = null;

        foreach (var group in _workingConfig.Groups)
        {
            var found = group.Mods.FirstOrDefault(m => m.Id == modId);
            if (found != null)
            {
                modInfo = found;
                fromGroup = group;
                break;
            }
        }

        if (modInfo == null || fromGroup == null) return;
        if (fromGroup == targetGroup) return;

        _workingConfig.MoveModToGroup(modInfo, fromGroup, targetGroup);

        // Update UI Lists
        // Since we are using ObservableList manually populated from the underlying data,
        // we need to refresh the lists if they are affected.

        // If the source or target group is currently selected, refresh the mod list.
        if (SelectedGroup.Value == fromGroup || SelectedGroup.Value == targetGroup)
        {
            RefreshModList();
        }

        // Trigger a refresh of group counts (if we bind them)
        // In this simple VM, we might rely on the View to observe collection changes
        // or we can force a property update if needed.
        // For ObservableList, adding/removing items triggers events.
        // But here we modified the *content* of the groups (the Mods list inside ModGroup).
        // ModGroup.Mods is a List<ModInfo2>, not observable.
        // So we might need to signal that the groups have changed.

        // Actually, for the Group List UI, it usually displays the count.
        // We can force a refresh of the Groups list or use a specific signal.
        // For now, let's just rely on the fact that we might need to re-bind or signal updates.
        // A simple way is to replace the group object in the list to trigger update,
        // or have a separate mechanism.
        // Let's just call CheckDirty for now.

        CheckDirty();
    }

    public void MoveModUp(string modId)
    {
        var group = SelectedGroup.Value;
        if (group == null) return;
        var mod = group.Mods.FirstOrDefault(m => m.Id == modId);
        if (mod == null) return;
        var index = group.IndexOfMod(mod);
        if (index > 0) ReorderMod(modId, index - 1);
    }

    public void MoveModDown(string modId)
    {
        var group = SelectedGroup.Value;
        if (group == null) return;
        var mod = group.Mods.FirstOrDefault(m => m.Id == modId);
        if (mod == null) return;
        var index = group.IndexOfMod(mod);
        if (index < group.Mods.Count - 1) ReorderMod(modId, index + 1);
    }

    public void MoveModToTop(string modId)
    {
        ReorderMod(modId, 0);
    }

    public void MoveModToBottom(string modId)
    {
        var group = SelectedGroup.Value;
        if (group == null) return;
        ReorderMod(modId, group.Mods.Count - 1);
    }

    public void ReorderMod(string modId, int newIndex)
    {
        var group = SelectedGroup.Value;
        if (group == null)
        {
            Log.Warn($"[BML VM] ReorderMod: No group selected");
            return;
        }

        var mod = group.Mods.FirstOrDefault(m => m.Id == modId);
        if (mod == null)
        {
            Log.Warn($"[BML VM] Mod '{modId}' not found in group '{group.Name}'");
            return;
        }

        var oldIndex = group.IndexOfMod(mod);

        if (oldIndex == newIndex)
        {
            Log.Debug($"[BML VM] Index unchanged, skipping reorder");
            return;
        }

        group.RemoveMod(mod);

        // Clamp index
        var clampedIndex = newIndex;
        if (newIndex > group.Mods.Count) clampedIndex = group.Mods.Count;
        if (newIndex < 0) clampedIndex = 0;

        if (clampedIndex != newIndex)
        {
            Log.Debug($"[BML VM] Clamped index from {newIndex} to {clampedIndex}");
        }

        group.AddModAt(mod, clampedIndex);

        // Log final order
        Log.Debug($"[BML VM] Final mod order in group:");
        for (var i = 0; i < group.Mods.Count; i++)
        {
            Log.Debug($"[BML VM]   [{i}] {group.Mods[i].Id}");
        }

        RefreshModList();
        CheckDirty();

        Log.Debug($"[BML VM] ===== ReorderMod END =====");
    }

    public void DeleteGroup(ModGroup groupToDelete)
    {
        if (groupToDelete == null)
        {
            Log.Warn("[BML VM] DeleteGroup: Group is null");
            return;
        }

        // Check if group has mods
        if (groupToDelete.Mods.Count > 0)
        {
            Log.Warn(
                $"[BML VM] DeleteGroup: Cannot delete group '{groupToDelete.Name}' because it contains {groupToDelete.Mods.Count} mods");
            ToastManager.ShowWarning("无法删除包含模组的分组");
            return;
        }

        // Check if group is locked
        if (groupToDelete.Tags != null && groupToDelete.Tags.Contains(ModGroupTags.Locked))
        {
            Log.Warn($"[BML VM] DeleteGroup: Cannot delete locked group '{groupToDelete.Name}'");
            ToastManager.ShowWarning("无法删除锁定的分组");
            return;
        }

        // Check if this is the last group
        if (_workingConfig.Groups.Count <= 1)
        {
            Log.Warn("[BML VM] DeleteGroup: Cannot delete the last remaining group");
            ToastManager.ShowWarning("无法删除最后一个分组");
            return;
        }

        // Remove from working config
        var removed = _workingConfig.Groups.Remove(groupToDelete);
        if (!removed)
        {
            Log.Warn($"[BML VM] DeleteGroup: Group '{groupToDelete.Name}' not found in working config");
            return;
        }

        // Remove from observable list
        Groups.Remove(groupToDelete);

        // If deleted group was selected, select another group
        if (SelectedGroup.Value == groupToDelete)
        {
            SelectedGroup.Value = Groups.Count > 0 ? Groups[0] : null;
        }

        CheckDirty();
        OnGroupListChanged.OnNext(Unit.Default); // Trigger rebuild
        Log.Info($"[BML VM] Deleted group '{groupToDelete.Name}'");
        ToastManager.ShowSuccess($"已删除分组 '{groupToDelete.Name}'");
    }

    /// <summary>
    /// 重命名分组
    /// </summary>
    public void RenameGroup(ModGroup group, string newName)
    {
        if (group == null)
        {
            Log.Warn("[BML VM] RenameGroup: Group is null");
            return;
        }

        // Trim whitespace
        newName = newName?.Trim() ?? string.Empty;

        // Check if name is empty
        if (string.IsNullOrEmpty(newName))
        {
            Log.Warn("[BML VM] RenameGroup: New name is empty");
            ToastManager.ShowWarning(L.Group.RenameEmpty);
            return;
        }

        // Check if name is the same as current
        if (group.Name == newName)
        {
            // No change needed, but trigger UI update to close rename mode
            OnGroupListChanged.OnNext(Unit.Default);
            return;
        }

        // Check if group is locked
        if (group.Tags != null && group.Tags.Contains(ModGroupTags.Locked))
        {
            Log.Warn($"[BML VM] RenameGroup: Cannot rename locked group '{group.Name}'");
            ToastManager.ShowWarning(L.Group.RenameNotAllowed);
            return;
        }

        // Check if name already exists
        if (_workingConfig.Groups.Any(g => g.Name == newName))
        {
            Log.Warn($"[BML VM] RenameGroup: Group '{newName}' already exists");
            ToastManager.ShowWarning(L.Group.RenameExists);
            return;
        }

        // Store old name for logging
        var oldName = group.Name;

        // Update group name
        group.Name = newName;

        // Update the working config
        var workingGroup = _workingConfig.Groups.FirstOrDefault(g => g == group);
        if (workingGroup != null)
        {
            workingGroup.Name = newName;
        }

        CheckDirty();
        OnGroupListChanged.OnNext(Unit.Default); // Trigger rebuild
        Log.Info($"[BML VM] Renamed group from '{oldName}' to '{newName}'");
        ToastManager.ShowSuccess(L.Group.RenameCompleteCurrent);
    }

    public void ReorderGroup(string groupName, int oldIndex, int newIndex)
    {
        var group = _workingConfig.Groups.FirstOrDefault(g => g.Name == groupName);
        if (group == null) return;

        // Use partitioned logic: Top groups vs Others
        var allOrdered = _workingConfig.Groups.ToList();
        var topList = allOrdered.Where(g => g.Tags != null && g.Tags.Contains(ModGroupTags.Top)).ToList();
        var otherList = allOrdered.Where(g => !(g.Tags != null && g.Tags.Contains(ModGroupTags.Top))).ToList();

        var movingIsTop = group.Tags != null && group.Tags.Contains(ModGroupTags.Top);

        if (movingIsTop)
        {
            topList.Remove(group);
            var insert = Mathf.Clamp(newIndex, 0, topList.Count);
            topList.Insert(insert, group);
        }
        else
        {
            otherList.Remove(group);
            var insert = Mathf.Clamp(newIndex, 0, otherList.Count);
            otherList.Insert(insert, group);
        }

        // Merge and update
        var merged = new List<ModGroup>(topList.Count + otherList.Count);
        merged.AddRange(topList);
        merged.AddRange(otherList);

        _workingConfig.Groups = merged;

        // Update observable list
        Groups.Clear();
        foreach (var g in merged)
        {
            Groups.Add(g);
        }

        CheckDirty();
        OnGroupListChanged.OnNext(Unit.Default); // Trigger rebuild
        Log.Debug($"[BML] Reordered group '{groupName}' from {oldIndex} to {newIndex}");
    }

    public bool IsRefreshing { get; private set; }

    private void RefreshModList()
    {
        var group = SelectedGroup.Value;
        if (group == null)
        {
            CurrentGroupMods.Clear();
            return;
        }

        // Batch update to minimize rebuild events
        IsRefreshing = true;
        try
        {
            CurrentGroupMods.Clear();
            foreach (var mod in group.Mods)
            {
                CurrentGroupMods.Add(mod);
            }
        }
        finally
        {
            IsRefreshing = false;
            OnModListRefreshed.OnNext(Unit.Default);
        }
    }

    public void CheckDirty()
    {
        var currentSnapshot = _workingConfig.BuildDeterministicSnapshot();
        IsDirty.Value = currentSnapshot != _originalSnapshot;
    }

    private void ApplyChanges()
    {
        var infos = SteamModsLocalDataManager.Instance.GetUserSubscribedMods();
        if (infos.Count == 0)
        {
            DoSave();
            return;
        }

        var steamMods = _workingConfig.GetSortedModInfos()
            .Where(x => x.OriginalModInfo.isSteamItem && x.IsEnabled)
            .ToList();
        if (steamMods.Count == 0)
        {
            DoSave();
            return;
        }

        ModDataCheckView.Show(steamMods, infos, DoSave);

        void DoSave()
        {
            try
            {
                // Calculate changes BEFORE saving config
                var changes = CalculateModChanges();

                _workingConfig.SyncTo(ModGroupConfig.Instance);
                ModGroupConfig.Instance.Save();

                // Update snapshot
                _originalSnapshot = _workingConfig.BuildDeterministicSnapshot();
                _originalConfigCopy = _workingConfig.DeepCopy();
                CheckDirty();

                // Trigger mod activation with saved changes
                TriggerModActivation(changes);

                RequestClose.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[BML] Apply changes failed");
            }
        }
    }

    private void DiscardChanges()
    {
        _workingConfig = _originalConfigCopy.DeepCopy();
        _workingConfig.SyncTo(ModGroupConfig.Instance); // Revert runtime config too?

        // Refresh UI
        Groups.Clear();
        foreach (var group in _workingConfig.Groups)
        {
            Groups.Add(group);
        }

        if (Groups.Count > 0) SelectedGroup.Value = Groups[0];

        CheckDirty();
        RequestClose.OnNext(Unit.Default);
    }

    private void ShowRevertConfirmation()
    {
        RollbackManager.ShowRevertConfirmation(
            onConfirm: DiscardChanges,
            onCancel: () =>
            {
                /* 用户取消，不做任何操作 */
            }
        );
    }

    private void TriggerModActivation(ModChanges changes)
    {
        try
        {
            Log.Info(
                $"[BML] Config saved: {changes.JobsToDisable.Count} to disable, {changes.JobsToEnable.Count} to enable, order changed: {changes.OrderChanged}");

            // Merge disable and enable jobs
            var allJobs = new List<BetterModManager.ModOperation>();
            allJobs.AddRange(changes.JobsToDisable);
            allJobs.AddRange(changes.JobsToEnable);

            ModActivationProgressPanel.Show(allJobs.ToArray(), changes.OrderChanged);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BML] Failed to trigger mod activation");
        }
    }

    private ModChanges CalculateModChanges()
    {
        var jobsToDisable = new List<BetterModManager.ModOperation>();
        var jobsToEnable = new List<BetterModManager.ModOperation>();
        var orderChanged = false;

        // Get original and current mod lists
        var originalMods = _originalConfigCopy.GetSortedModInfos().ToList();
        var currentMods = _workingConfig.GetSortedModInfos().ToList();

        // Check for order changes by comparing mod sequences
        if (originalMods.Count != currentMods.Count ||
            !originalMods.Select(m => m.Id).SequenceEqual(currentMods.Select(m => m.Id)))
        {
            orderChanged = true;
        }

        // Create lookup dictionaries for efficient comparison
        var originalModDict = originalMods.ToDictionary(m => m.Id, m => m);
        var currentModDict = currentMods.ToDictionary(m => m.Id, m => m);

        // Find mods that existed before but are now disabled or removed
        int disableOrder = 0;
        foreach (var originalMod in originalMods)
        {
            if (!currentModDict.TryGetValue(originalMod.Id, out var currentMod) ||
                !ShouldActivateMod(currentMod))
            {
                // Mod was disabled or removed
                if (ShouldActivateMod(originalMod))
                {
                    jobsToDisable.Add(new BetterModManager.ModOperation(
                        BetterModManager.ModActivationAction.Disable,
                        originalMod,
                        disableOrder++
                    ));
                }
            }
        }

        // Find mods that need to be enabled
        int enableOrder = 0;
        foreach (var currentMod in currentMods)
        {
            if (ShouldActivateMod(currentMod))
            {
                if (!originalModDict.TryGetValue(currentMod.Id, out var originalMod) ||
                    !ShouldActivateMod(originalMod))
                {
                    // Mod is newly enabled
                    jobsToEnable.Add(new BetterModManager.ModOperation(
                        BetterModManager.ModActivationAction.Enable,
                        currentMod,
                        enableOrder++
                    ));
                }
                // If mod existed before and is still active, no action needed
            }
        }

        return new ModChanges(jobsToDisable, jobsToEnable, orderChanged);
    }

    private static bool ShouldActivateMod(ModInfo2 modInfo)
    {
        // Use the same logic as BetterModManager.ShouldActivate
        if (ModIdentifier.IsActivateByOldModLoader(modInfo.OriginalModInfo))
        {
            return false;
        }

        return modInfo.IsEnabled;
    }

    public void Dispose()
    {
        _disposableBag.Dispose();
    }
}
