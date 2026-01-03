namespace Newbe.BetterModLoader.ModDataChecks;

/// <summary>
/// 检查结果项，包含唯一ID和忽略状态
/// </summary>
internal abstract class CheckResultItem
{
    public string Id { get; }
    public bool IsIgnored { get; set; }

    protected CheckResultItem(string id)
    {
        Id = id;
    }
}

/// <summary>
/// 需要更新的模组检查项
/// </summary>
internal class NeedToUpdateCheckItem : CheckResultItem
{
    public SteamModDependencyInfo ModInfo { get; }

    public NeedToUpdateCheckItem(SteamModDependencyInfo modInfo)
        : base($"update_{modInfo.SteamId}")
    {
        ModInfo = modInfo;
    }
}

/// <summary>
/// 缺失依赖检查项
/// </summary>
internal class MissingDependencyCheckItem : CheckResultItem
{
    public SteamModDependencyInfo Mod { get; }
    public SteamModDependencyInfo MissingDependency { get; }

    public MissingDependencyCheckItem(SteamModDependencyInfo mod, SteamModDependencyInfo missingDependency)
        : base($"missing_{mod.SteamId}_{missingDependency.SteamId}")
    {
        Mod = mod;
        MissingDependency = missingDependency;
    }
}

/// <summary>
/// 模组加载顺序错误检查项
/// </summary>
internal class ModOrderErrorCheckItem : CheckResultItem
{
    public ModOrderError Error { get; }

    public ModOrderErrorCheckItem(ModOrderError error)
        : base($"order_{error.Mod.SteamId}_{error.Dependency.SteamId}")
    {
        Error = error;
    }
}

/// <summary>
/// CustomThings 依赖警告检查项
/// </summary>
internal class CustomThingsDependencyWarningItem : CheckResultItem
{
    public SteamModDependencyInfo Mod { get; }

    public CustomThingsDependencyWarningItem(SteamModDependencyInfo mod)
        : base($"customthings_warning_{mod.SteamId}")
    {
        Mod = mod;
    }
}

internal sealed class ModDataCheckResult
{
    public List<CheckResultItem> AllItems { get; } = [];

    /// <summary>
    /// 获取所有未被忽略的检查项
    /// </summary>
    public List<CheckResultItem> ActiveItems =>
        AllItems.Where(item => !item.IsIgnored).ToList();

    /// <summary>
    /// 获取所有被忽略的检查项
    /// </summary>
    public List<CheckResultItem> IgnoredItems =>
        AllItems.Where(item => item.IsIgnored).ToList();

    public bool IsAllPassed => AllItems.Count == 0;

    /// <summary>
    /// 检查是否有未被忽略的失败项
    /// </summary>
    public bool HasActiveFailures => ActiveItems.Count > 0;
}

internal readonly struct ModOrderError
{
    public SteamModDependencyInfo Mod { get; }
    public SteamModDependencyInfo Dependency { get; }

    public ModOrderError(SteamModDependencyInfo mod, SteamModDependencyInfo dependency)
    {
        Mod = mod;
        Dependency = dependency;
    }
}
