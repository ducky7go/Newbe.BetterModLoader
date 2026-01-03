namespace Newbe.BetterModLoader.DeveloperModes;

/// <summary>
/// 依赖信息结构体
/// </summary>
internal class ModDependencyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;

    public ModDependencyInfo()
    {
    }

    public ModDependencyInfo(string name, string condition)
    {
        Name = name;
        Condition = condition;
    }
}
