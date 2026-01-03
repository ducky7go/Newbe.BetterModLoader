namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

/// <summary>
/// 表示Steam Workshop模组的依赖信息
/// </summary>
[Serializable]
internal class SteamModDependencyInfo
{
    /// <summary>
    /// Steam Workshop物品ID
    /// </summary>
    public ulong SteamId { get; set; }

    /// <summary>
    /// 是否已订阅
    /// </summary>
    public bool IsSubscribed { get; set; }

    /// <summary>
    /// 本地安装时间（UTC）
    /// </summary>
    public DateTime? LocalInstallTime { get; set; }

    /// <summary>
    /// 线上更新时间（UTC）
    /// </summary>
    public DateTime? OnlineUpdateTime { get; set; }

    /// <summary>
    /// 依赖项ID列表
    /// </summary>
    public List<ulong> DependencyIds { get; set; } = [];

    /// <summary>
    /// Mod标题名称（从Steam Workshop获取）
    /// </summary>
    public string? Title { get; set; }

    public bool IsUpToDate
    {
        get
        {
            // no need to update if no local install time
            if (!LocalInstallTime.HasValue)
            {
                return true;
            }

            // no need to update if no online update time
            if (!OnlineUpdateTime.HasValue)
            {
                return true;
            }

            return LocalInstallTime.Value >= OnlineUpdateTime.Value;
        }
    }
}
