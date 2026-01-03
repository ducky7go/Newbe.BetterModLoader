namespace Newbe.BetterModLoader;

/// <summary>
/// Serialization-friendly version of ModInfo2 for JSON persistence.
/// Only stores Id, Tags, and OrderInGroup - OriginalModInfo is rebuilt at runtime.
/// </summary>
[Serializable]
internal class ModInfo2Serializable
{
    public string Id = string.Empty;
    public List<string> Tags = [];
    public bool IsEnabled;

    public ModInfo2Serializable()
    {
    }

    public ModInfo2Serializable(ModInfo2 modInfo2)
    {
        Id = modInfo2.Id;
        Tags = modInfo2.Tags?.ToList() ?? [];
        IsEnabled = modInfo2.IsEnabled;
    }

    public ModInfo2 ToModInfo2()
    {
        return new ModInfo2
        {
            Id = Id,
            Tags = Tags != null ? [..Tags] : [],
            IsEnabled = IsEnabled
        };
    }
}

/// <summary>
/// Serialization wrapper for ModGroup with ModInfo2Serializable.
/// </summary>
[Serializable]
internal class ModGroupSerializable
{
    public string Name = string.Empty;
    public string Description = string.Empty;
    public int Order;
    public List<ModInfo2Serializable> Mods = [];
    public List<string> Tags = [];

    public ModGroupSerializable()
    {
    }

    public ModGroupSerializable(ModGroup group)
    {
        Name = group.Name;
        Description = group.Description;
        Tags = group.Tags?.ToList() ?? [];
        Mods = group.Mods?.Select(m => new ModInfo2Serializable(m)).ToList() ?? [];
    }

    public ModGroup ToModGroup()
    {
        return new ModGroup(Name, Mods.Select(m => m.ToModInfo2()).ToList())
        {
            Name = Name,
            Description = Description,
            Tags = Tags != null ? [..Tags] : [],
        };
    }
}

/// <summary>
/// Serialization wrapper for config.
/// 注意：必须使用公共字段（field）而不是属性（property），因为 Unity JsonUtility 不支持属性
/// </summary>
[Serializable]
internal class ModGroupConfigSerializable
{
    [SerializeField] public List<ModGroupSerializable> Groups = [];
}

/// <summary>
/// 模组分组配置，负责持久化分组信息到 JSON 文件
/// 作为 MonoBehaviour 在启动时自动加载配置和扫描模组
/// </summary>
internal class ModGroupConfig : MonoBehaviour
{
    private static ModGroupConfig? _instance;

    /// <summary>
    /// 所有分组列表（包含默认分组）
    /// </summary>
    public List<ModGroup> Groups { get; set; } = [];

    public ModGroup? DefaultGroup
    {
        get { return Groups.FirstOrDefault(g => g.IsDefaultGroup); }
    }

    public ModGroup? BetterModLoaderGroup
    {
        get { return Groups.FirstOrDefault(g => g.Tags.Contains(ModGroupTags.BetterModLoader)); }
    }

    public ModGroup? EnablesInOldManager
    {
        get { return Groups.FirstOrDefault(g => g.Tags.Contains(ModGroupTags.EnablesInOldManager)); }
    }

    /// <summary>
    /// 提供按照分组顺序与分组内顺序排序后的 ModInfo 列表视图。
    /// - 先按分组的 Order 升序
    /// - 再按分组内的 OrderInGroup 升序
    /// 仅返回在当前分组配置中存在且能解析到 OriginalModInfo 的模组。
    /// 如存在未被分组但被扫描到的模组，确保在 SyncUnsortedMods 后默认分组会收纳它们，因此无需额外拼接。
    /// </summary>
    public IEnumerable<ModInfo2> SortedModInfos
    {
        get { return Groups.SelectMany(x => x.Mods); }
    }

    /// <summary>
    /// 单例实例
    /// </summary>
    public static ModGroupConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                EnsureCreated();
            }

            return _instance!;
        }
    }

    /// <summary>
    /// 确保 ModGroupConfig 实例已创建
    /// </summary>
    private static void EnsureCreated()
    {
        if (_instance != null) return;

        var go = new GameObject("[ModGroupConfig]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ModGroupConfig>();

        Log.Debug("ModGroupConfig instance created");
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFromFile();
    }

    /// <summary>
    /// 从文件加载配置（内部使用）
    /// </summary>
    private void LoadFromFile()
    {
        try
        {
            var serializable = ModLoaderSaveSystem.LoadConfig();
            if (serializable is not null)
            {
                Groups = serializable.Groups.Select(g => g.ToModGroup()).ToList();
                Log.Current.Debug(
                    $"Loaded mod group config with {Instance.Groups.Count} groups from file");
                return;
            }

            Log.Warn($"Config file exists but deserialization returned null");
        }
        catch (Exception ex)
        {
            Log.DebugException("Failed to load mod group config", ex);
        }
    }

    /// <summary>
    /// 保存配置到 JSON 文件
    /// </summary>
    public void Save()
    {
        try
        {
            // 确保按 Order 排序后再保存
            Groups = Groups.ToList();
            var serializable = new ModGroupConfigSerializable
            {
                Groups = Groups.Select(g => new ModGroupSerializable(g)).ToList()
            };

            ModLoaderSaveSystem.SaveConfig(serializable);


            Log.Debug($"Saved mod group config with {Groups.Count} groups");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save mod group config: {ex.Message}");
            Log.Error(ex, "Exception occurred");
        }
    }

    /// <summary>
    /// 同时重建所有 ModInfo2 的 OriginalModInfo 引用
    /// </summary>
    public void SyncUnsortedMods(List<ModInfo> allMods,
        List<string> enablesInOldManager)
    {
        EnsureBasicStructure(this);

        // 创建 ModId -> ModInfo 的映射
        var modInfoMap = allMods.ToDictionary(m => m.GetModId(), m => m);

        // 重建所有 ModInfo2 的 OriginalModInfo 引用
        var removedMods = new List<ModInfo2>();
        foreach (var modInfo2 in Groups.SelectMany(group => group.Mods))
        {
            if (modInfoMap.TryGetValue(modInfo2.Id, out var originalModInfo))
            {
                modInfo2.OriginalModInfo = originalModInfo;
            }
            else
            {
                // 标记为已删除的模组
                removedMods.Add(modInfo2);
            }
        }

        // 从各分组中移除已删除的模组
        foreach (var removedMod in removedMods)
        {
            Log.Debug("Removing deleted mod from groups: " + removedMod.Id);
            foreach (var group in Groups)
            {
                group.RemoveMod(removedMod);
            }
        }

        // 重新计算所有已分组的模组ID（因为可能有模组被移除）
        var allGroupedModIds = Groups.SelectMany(g => g.Mods.Select(m => m.Id)).ToHashSet();

        ModGroup? enablesInOldManagerGroup;

        foreach (var mod in allMods)
        {
            var modId = mod.GetModId();
            if (!allGroupedModIds.Contains(modId))
            {
                if (enablesInOldManager.Contains(mod.name))
                {
                    if (AddToSpecialGroup(mod, out var sg))
                    {
                        var info = AddModToGroup(mod, sg);
                        info.IsEnabled = true;
                    }
                    else
                    {
                        var g = EnsureEnablesInOldManagerGroup();
                        if (g!.Mods.All(m => m.Id != modId))
                        {
                            var info = AddModToGroup(mod, g);
                            info.IsEnabled = true;
                        }
                    }
                }
                else
                {
                    if (AddToSpecialGroup(mod, out var sg))
                    {
                        AddModToGroup(mod, sg);
                    }
                    else
                    {
                        AddModToGroup(mod, DefaultGroup);
                    }
                }
            }
        }

        OrderStructureCheck();

        Save();
        return;

        ModGroup EnsureEnablesInOldManagerGroup()
        {
            if (Instance.EnablesInOldManager == null)
            {
                var migrationEnabledGroup = new ModGroup(L.GroupName.EnabledBefore)
                {
                    Description = "Mods that were enabled at the time of migration."
                };

                // 添加专用标签，便于后续检索
                migrationEnabledGroup.Tags.Add(ModGroupTags.EnablesInOldManager);

                Instance.Groups.Add(migrationEnabledGroup);
                Log.Current.Debug(
                    "Created '迁移时启用的模组' group for migrated enabled mods from old manager.");
                enablesInOldManagerGroup = migrationEnabledGroup;
            }
            else
            {
                enablesInOldManagerGroup = Instance.EnablesInOldManager;
            }

            return enablesInOldManagerGroup;
        }

        ModGroup CreateNewGroupBeforeDefault(string n)
        {
            var newGroup = new ModGroup(n)
            {
                Description = "Auto-created group for migrated enabled mods from old manager."
            };

            var defaultGroup = Instance.DefaultGroup;
            if (defaultGroup == null)
            {
                throw new NullReferenceException("DefaultGroup is null, cannot create new group before it.");
            }

            var defaultIndex = Instance.Groups.IndexOf(defaultGroup);
            Instance.Groups.Insert(defaultIndex, newGroup);
            Log.Current.Debug(
                $"Created '{n}' group for migrated enabled mods from old manager.");
            return newGroup;
        }

        ModGroup CreateNewGroupAtEnd(string n)
        {
            var newGroup = new ModGroup(n)
            {
                Description = "Auto-created group for migrated enabled mods from old manager."
            };

            Instance.Groups.Add(newGroup);
            Log.Debug(
                $"Created '{n}' group for migrated enabled mods from old manager.");
            return newGroup;
        }

        bool AddToSpecialGroup(ModInfo mod, out ModGroup group)
        {
            if (ModIdentifier.IsCustomThings(mod))
            {
                group = CreateNewGroupBeforeDefault("薄荷你怎么");
                return true;
            }

            if (ModIdentifier.IsItemCatalog(mod))
            {
                group = CreateNewGroupAtEnd("建议置底");
                return true;
            }

            group = null!;
            return false;
        }
    }


    public void OrderStructureCheck()
    {
        // ensure better loader group exists and at first position
        {
            var bmlGroup = BetterModLoaderGroup;
            if (bmlGroup == null)
            {
                Log.Error("BetterModLoaderGroup is null, cannot perform order structure check.");
            }
            else if (Groups.IndexOf(bmlGroup) != 0)
            {
                Log.Warn(
                    "BetterModLoader group is not at first position, moving it now.");
                Groups.Remove(bmlGroup);
                Groups.Insert(0, bmlGroup);
            }

            if (bmlGroup != null)
            {
                // find better mod loader and ensure it is in the first group and first position
                var betterModLoaderInfo = SortedModInfos.FirstOrDefault(x =>
                    ModIdentifier.IsBetterModLoader(x.OriginalModInfo));
                var duckyLoggingInfo = SortedModInfos.FirstOrDefault(x =>
                    ModIdentifier.IsDuckyLogging(x.OriginalModInfo));
                if (betterModLoaderInfo == null)
                {
                    Log.Error(
                        "BetterModLoader mod info not found in any group, cannot perform order structure check.");
                }
                else
                {
                    if (duckyLoggingInfo is { IsEnabled: false })
                    {
                        duckyLoggingInfo.IsEnabled = true;
                    }

                    var duckyLoggingInfoIndexShouldBe = 0;
                    if (duckyLoggingInfo != null)
                    {
                        var currentDuckyGroup = Groups.FirstOrDefault(g => g.Mods.Contains(duckyLoggingInfo));
                        if (currentDuckyGroup != bmlGroup)
                        {
                            Log.Warn(
                                "Ducky.Logging mod is not in the BetterModLoader group, moving it now.");
                            currentDuckyGroup?.RemoveMod(duckyLoggingInfo);
                            bmlGroup.AddModAt(duckyLoggingInfo, duckyLoggingInfoIndexShouldBe);
                        }
                        else if (bmlGroup.IndexOfMod(duckyLoggingInfo) != duckyLoggingInfoIndexShouldBe)
                        {
                            Log.Warn(
                                "Ducky.Logging mod is not at first position in its group, moving it now.");
                            bmlGroup.RemoveMod(duckyLoggingInfo);
                            bmlGroup.AddModAt(duckyLoggingInfo, duckyLoggingInfoIndexShouldBe);
                        }
                    }

                    if (!betterModLoaderInfo.IsEnabled)
                    {
                        betterModLoaderInfo.IsEnabled = true;
                    }

                    var betterModLoaderInfoIndexShouldBe = duckyLoggingInfo != null ? 1 : 0;
                    var currentGroup = Groups.FirstOrDefault(g => g.Mods.Contains(betterModLoaderInfo));
                    if (currentGroup != bmlGroup)
                    {
                        Log.Warn(
                            "BetterModLoader mod is not in the BetterModLoader group, moving it now.");
                        currentGroup?.RemoveMod(betterModLoaderInfo);
                        bmlGroup.AddModAt(betterModLoaderInfo, betterModLoaderInfoIndexShouldBe);
                    }
                    else if (bmlGroup.IndexOfMod(betterModLoaderInfo) != betterModLoaderInfoIndexShouldBe)
                    {
                        Log.Warn(
                            "BetterModLoader mod is not at first position in its group, moving it now.");
                        bmlGroup.RemoveMod(betterModLoaderInfo);
                        bmlGroup.AddModAt(betterModLoaderInfo, betterModLoaderInfoIndexShouldBe);
                    }
                }
            }
        }

        if (DefaultGroup == null)
        {
            Log.Error("DefaultGroup is null, cannot perform order structure check.");
        }
    }

    private ModInfo2 AddModToGroup(ModInfo mod, ModGroup group)
    {
        var id = mod.GetModId();
        var info2 = SortedModInfos.FirstOrDefault(m => m.Id == id);
        if (info2 != null)
        {
            // remove from previous group
            var previousGroup = Groups.FirstOrDefault(g => g.Mods.Contains(info2));
            previousGroup?.RemoveMod(info2);

            // add to new group
            group.AddMod(info2);
            return info2;
        }

        var modInfo2 = new ModInfo2(mod)
        {
        };
        group.AddMod(modInfo2);
        return modInfo2;
    }

    /// <summary>
    /// 创建默认的分组结构
    /// 包括 BetterModLoader 分组和其他预定义分组
    /// </summary>
    private static void EnsureBasicStructure(ModGroupConfig config)
    {
        if (config.BetterModLoaderGroup == null)
        {
            // 创建 BetterModLoader 分组（Order = 0，排在最前面）
            var betterModLoaderGroup = new ModGroup(L.GroupName.BetterModLoader)
            {
                Description =
                    "Core mod management system that provides enhanced mod organization and loading capabilities."
            };

            // 该分组默认具有 top 标签：应固定在顶部区域
            betterModLoaderGroup.Tags.Add(ModGroupTags.Top);
            // 同时添加 BetterModLoader 专用标签，便于后续检索
            betterModLoaderGroup.Tags.Add(ModGroupTags.BetterModLoader);
            // locked 标签，防止用户误操作修改该分组内容
            betterModLoaderGroup.Tags.Add(ModGroupTags.Locked);
            config.Groups.Add(betterModLoaderGroup);
        }

        if (config.DefaultGroup == null)
        {
            // create Default group
            var defaultGroup = new ModGroup(L.GroupName.Default)
            {
                Description = "Mods that are not assigned to any specific group.",
                IsDefaultGroup = true
            };

            config.Groups.Add(defaultGroup);
        }
    }

    /// <summary>
    /// 删除分组配置文件（ModGroups.json）
    /// </summary>
    public static void DeleteConfigFile()
    {
        try
        {
            var configPath = ModLoaderSaveSystem.ConfigFilePath;
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
                Log.Debug($"Deleted group config: {configPath}");
            }
            else
            {
                Log.Debug($"Group config not found (already absent): {configPath}");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to delete group config: {ex.Message}");
        }
    }

    public ModGroupWorkingCopy? CreateWorkingCopy()
    {
        return ModGroupWorkingCopy.CreateFrom(this);
    }
}
