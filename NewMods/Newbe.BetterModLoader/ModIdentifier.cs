namespace Newbe.BetterModLoader;

/// <summary>
/// 提供模组身份识别的工具方法
/// </summary>
internal static class ModIdentifier
{
    /// <summary>
    /// BetterModLoader 的 Steam Workshop ID
    /// </summary>
    public const ulong BetterModLoaderSteamId = 3596571766UL;

    /// <summary>
    /// CustomThings 的 Steam Workshop ID
    /// </summary>
    public const ulong CustomThingsBaseSteamId = 3591672666UL;

    /// <summary>
    /// 全物品图鉴 的 Steam Workshop ID
    /// </summary>
    public const ulong ItemCatalogSteamId = 3598920431UL;

    private const ulong DuckyLoggingLiteEdiSteamId = 3608583782UL;
    private const ulong DuckyLoggingDeveloperEdiSteamId = 3612064456UL;

    private const string DuckyLoggingLiteEdiName = "Ducky.Logging.LiteEdition";
    private const string DuckyLoggingDeveloperEdition = "Ducky.Logging.DeveloperEdition";

    /// <summary>
    /// BetterModLoader 的本地名称
    /// </summary>
    public const string BetterModLoaderName = "Newbe.BetterModLoader";

    /// <summary>
    /// 判断给定的 ModInfo 是否为 BetterModLoader 模组
    /// </summary>
    public static bool IsBetterModLoader(ModInfo mod)
    {
        // 通过 Steam ID 识别
        if (mod is { isSteamItem: true, publishedFileId: BetterModLoaderSteamId })
        {
            return true;
        }

        // 通过名称识别
        if (mod.name == BetterModLoaderName)
        {
            return true;
        }

        return false;
    }

    public static bool IsDuckyLogging(ModInfo mod)
    {
        // 通过 Steam ID 识别
        if (mod is { isSteamItem: true, publishedFileId: DuckyLoggingLiteEdiSteamId })
        {
            return true;
        }

        if (mod is { isSteamItem: true, publishedFileId: DuckyLoggingDeveloperEdiSteamId })
        {
            return true;
        }

        // 通过名称识别
        if (mod.name == DuckyLoggingLiteEdiName)
        {
            return true;
        }

        if (mod.name == DuckyLoggingDeveloperEdition)
        {
            return true;
        }

        return false;
    }

    public static bool IsActivateByOldModLoader(ModInfo mod)
    {
        return IsBetterModLoader(mod) || IsDuckyLogging(mod);
    }

    public static bool IsCustomThings(ModInfo mod)
    {
        return mod is { isSteamItem: true, publishedFileId: CustomThingsBaseSteamId };
    }

    public static bool IsCustomThings(ulong steamId)
    {
        return steamId == CustomThingsBaseSteamId;
    }

    /// <summary>
    /// 判断给定的 ModInfo 是否为 全物品图鉴 模组
    /// </summary>
    public static bool IsItemCatalog(ModInfo mod)
    {
        return mod is { isSteamItem: true, publishedFileId: ItemCatalogSteamId };
    }

    public static bool IsItemCatalog(ulong steamId)
    {
        return steamId == ItemCatalogSteamId;
    }
}
