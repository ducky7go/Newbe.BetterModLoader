namespace Newbe.BetterModLoader;

/// <summary>
/// ModGroupConfig 的工作副本，用于 UI 层数据管理
/// 不依赖 MonoBehaviour，只管理数据和排序逻辑
/// </summary>
internal class ModGroupWorkingCopy
{
    /// <summary>
    /// 分组列表
    /// </summary>
    public List<ModGroup> Groups { get; set; } = [];

    /// <summary>
    /// 创建空的工作副本
    /// </summary>
    private ModGroupWorkingCopy()
    {
    }

    /// <summary>
    /// 从 ModGroupConfig 创建工作副本（深拷贝）
    /// </summary>
    public static ModGroupWorkingCopy CreateFrom(ModGroupConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var copy = new ModGroupWorkingCopy();

        // 深拷贝所有分组
        foreach (var group in config.Groups)
        {
            var groupCopy = new ModGroup(group.Name, new List<ModInfo2>())
            {
                Description = group.Description,
                IsDefaultGroup = group.IsDefaultGroup,
                Tags = [..group.Tags],
            };

            // 深拷贝模组列表
            foreach (var mod in group.Mods)
            {
                groupCopy.AddMod(new ModInfo2
                {
                    Id = mod.Id,
                    Tags = [..mod.Tags],
                    IsEnabled = mod.IsEnabled,
                    OriginalModInfo = mod.OriginalModInfo // 保持引用
                });
            }

            copy.Groups.Add(groupCopy);
        }

        return copy;
    }

    /// <summary>
    /// 创建此工作副本的深拷贝
    /// </summary>
    public ModGroupWorkingCopy DeepCopy()
    {
        var copy = new ModGroupWorkingCopy();

        foreach (var group in Groups)
        {
            var groupCopy = new ModGroup(group.Name, new List<ModInfo2>())
            {
                Description = group.Description,
                IsDefaultGroup = group.IsDefaultGroup,
                Tags = new HashSet<string>(group.Tags),
            };

            foreach (var mod in group.Mods)
            {
                groupCopy.AddMod(new ModInfo2
                {
                    Id = mod.Id,
                    Tags = [..mod.Tags],
                    IsEnabled = mod.IsEnabled,
                    OriginalModInfo = mod.OriginalModInfo
                });
            }

            copy.Groups.Add(groupCopy);
        }

        return copy;
    }

    /// <summary>
    /// 同步数据回 ModGroupConfig
    /// </summary>
    public void SyncTo(ModGroupConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // 清空原有数据
        config.Groups.Clear();

        // 深拷贝所有分组回去
        foreach (var group in Groups)
        {
            var groupCopy = new ModGroup(group.Name, new List<ModInfo2>())
            {
                Description = group.Description,
                IsDefaultGroup = group.IsDefaultGroup,
                Tags = new HashSet<string>(group.Tags),
            };

            foreach (var mod in group.Mods)
            {
                groupCopy.AddMod(new ModInfo2
                {
                    Id = mod.Id,
                    Tags = [..mod.Tags],
                    IsEnabled = mod.IsEnabled,
                    OriginalModInfo = mod.OriginalModInfo
                });
            }

            config.Groups.Add(groupCopy);
        }

        // 触发保存
        config.Save();
    }

    /// <summary>
    /// 获取排序后的所有模组信息（用于显示和对比）
    /// </summary>
    public List<ModInfo2> GetSortedModInfos()
    {
        var result = new List<ModInfo2>();

        // 按分组Order排序
        var sortedGroups = Groups.ToList();

        foreach (var group in sortedGroups)
        {
            // 每个分组内按OrderInGroup排序
            var sortedMods = group.Mods.ToList();
            result.AddRange(sortedMods);
        }

        return result;
    }

    /// <summary>
    /// 创建新分组
    /// </summary>
    public ModGroup CreateGroup(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name cannot be empty", nameof(name));
        }

        if (Groups.Any(g => g.Name == name))
        {
            throw new InvalidOperationException($"Group '{name}' already exists");
        }

        var newGroup = new ModGroup(name)
        {
            Description = string.Empty,
        };

        Groups.Add(newGroup);
        return newGroup;
    }

    /// <summary>
    /// 将模组移动到指定分组
    /// </summary>
    public void MoveModToGroup(ModInfo2 modInfo, ModGroup? fromGroup, ModGroup targetGroup)
    {
        if (modInfo == null)
        {
            throw new ArgumentNullException(nameof(modInfo));
        }

        if (targetGroup == null)
        {
            throw new ArgumentNullException(nameof(targetGroup));
        }

        // 从原分组中移除
        if (fromGroup != null)
        {
            fromGroup.RemoveMod(modInfo);
        }

        // 添加到目标分组
        if (!targetGroup.Mods.Contains(modInfo))
        {
            targetGroup.AddMod(modInfo);
        }
    }

    /// <summary>
    /// 生成确定性的快照字符串（用于对比是否有变更）
    /// </summary>
    public string BuildDeterministicSnapshot()
    {
        var snapshot = new
        {
            Groups = Groups.Select(g => new
            {
                g.Name,
                g.Description,
                IsUnsortedGroup = g.IsDefaultGroup,
                Tags = (g.Tags).OrderBy(t => t).ToArray(),
                Mods = g.Mods.Select(m => new
                {
                    m.Id,
                    Tags = (m.Tags).OrderBy(t => t).ToArray(),
                    m.IsEnabled
                }).ToArray()
            }).ToArray()
        };

        return JsonConvert.SerializeObject(snapshot, Formatting.None);
    }
}
