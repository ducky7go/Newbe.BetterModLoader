namespace Newbe.BetterModLoader;

/// <summary>
/// ModInfo 扩展方法
/// </summary>
internal static class ModInfoExtensions
{
    /// <summary>
    /// 获取模组的唯一标识 ID
    /// - Steam 模组：使用 publishedFileId
    /// - 本地模组：使用文件夹名（从 path 提取）
    /// </summary>
    public static string GetModId(this ModInfo modInfo)
    {
        if (modInfo.isSteamItem && modInfo.publishedFileId != 0)
        {
            return $"steam_{modInfo.publishedFileId}";
        }

        // 本地模组：从 path 中提取文件夹名作为 ID
        // 例如："/path/to/Mods/MyMod" -> "local_MyMod"
        var folderName = Path.GetFileName(modInfo.path);
        if (string.IsNullOrEmpty(folderName))
        {
            // 降级到使用 name（如果提取文件夹名失败）
            folderName = modInfo.name;
        }

        return $"local_{folderName}";
    }
}
