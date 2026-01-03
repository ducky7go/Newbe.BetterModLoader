namespace Newbe.BetterModLoader.ModDataChecks;

/// <summary>
/// ModDataChecks 模组的配置管理类
/// 遵循项目中 Configs 类的最佳实践
/// </summary>
public static class ModDataChecksConfigs
{
    private const string IgnoredChecksKey = "bml_mod_data_checks_ignored";

    /// <summary>
    /// 获取被忽略的检查项ID列表
    /// </summary>
    public static List<string> GetIgnoredChecks()
    {
        try
        {
            return ModOptions.Load<List<string>>(IgnoredChecksKey, [])!;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[ModDataChecksConfigs] Failed to load ignored checks, using empty list");
            return new List<string>();
        }
    }

    /// <summary>
    /// 设置被忽略的检查项ID列表
    /// </summary>
    public static void SetIgnoredChecks(List<string> ignoredIds)
    {
        try
        {
            ModOptions.Save(IgnoredChecksKey, ignoredIds);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[ModDataChecksConfigs] Failed to save ignored checks");
        }
    }
}
