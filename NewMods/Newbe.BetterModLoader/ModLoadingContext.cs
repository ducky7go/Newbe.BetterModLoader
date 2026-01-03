namespace Newbe.BetterModLoader;

/// <summary>
/// 加载模式：初始加载 vs 配置变更后重新加载
/// </summary>
public enum LoadingMode
{
    /// <summary>
    /// 游戏启动时的初始加载
    /// </summary>
    Initial,

    /// <summary>
    /// 配置变更后的重新加载（启用/禁用模组）
    /// </summary>
    ConfigChanged
}

/// <summary>
/// 单个模组的加载状态
/// </summary>
public enum ModLoadStatus
{
    Pending, // 等待加载
    Loading, // 加载中
    Success, // 成功
    Failed, // 失败
    Skipped // 跳过（禁用的模组）
}

/// <summary>
/// 单个模组的加载信息
/// </summary>
internal class ModLoadingItem
{
    public string ModId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ModLoadStatus Status { get; set; } = ModLoadStatus.Pending;
    public string? Message { get; set; }
    public bool WillEnable { get; set; } // 是否需要启用此模组
    public bool WillDisable { get; set; } // 是否需要禁用此模组
}

/// <summary>
/// 模组加载上下文：包含加载模式、待处理的模组列表及其状态
/// </summary>
internal class ModLoadingContext
{
    public LoadingMode Mode { get; set; }
    public List<ModLoadingItem> Items { get; set; } = [];
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 获取已完成的项目数量
    /// </summary>
    public int CompletedCount => Items.FindAll(x =>
        x.Status == ModLoadStatus.Success ||
        x.Status == ModLoadStatus.Failed ||
        x.Status == ModLoadStatus.Skipped).Count;

    /// <summary>
    /// 获取总项目数量
    /// </summary>
    public int TotalCount => Items.Count;

    /// <summary>
    /// 是否全部完成
    /// </summary>
    public bool IsCompleted => CompletedCount >= TotalCount;

    /// <summary>
    /// 获取成功数量
    /// </summary>
    public int SuccessCount => Items.FindAll(x => x.Status == ModLoadStatus.Success).Count;

    /// <summary>
    /// 获取失败数量
    /// </summary>
    public int FailedCount => Items.FindAll(x => x.Status == ModLoadStatus.Failed).Count;
}
