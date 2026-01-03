namespace Newbe.BetterModLoader.DeveloperInfos;

/// <summary>
/// 上传面板主数据模型
/// </summary>
internal class DeveloperInfoData
{
    public double LogDirectorySizeInMB { get; set; }
    public string OriginalGameLogsFolder { get; set; }
    public string LocalModsFolder { get; set; }
    public string SteamModsFolder { get; set; }
    public string GamePersistentDataFolder { get; set; }
    public string BetterModeLoaderConfigFolder { get; set; }
    public string BetterModeLoaderConfigPath { get; set; }
    public string ModOptionsForAllFolder { get; set; }
    public string ModOptionsForEachFolder { get; set; }

    public IEnumerable<LabeledFolderPath> GetAllFolders()
    {
        yield return new LabeledFolderPath(L.UI.OriginalGameLogsFolder, OriginalGameLogsFolder);
        yield return new LabeledFolderPath(L.UI.DeveloperInfoLocalModsFolder, LocalModsFolder);
        yield return new LabeledFolderPath(L.UI.DeveloperInfoSteamModsFolder, SteamModsFolder);
        yield return new LabeledFolderPath(L.UI.DeveloperInfoGamePersistentDataFolder, GamePersistentDataFolder);
        yield return new LabeledFolderPath(L.UI.BmlConfigFolder, BetterModeLoaderConfigFolder);
        yield return new LabeledFolderPath(L.UI.BmlConfigPath, BetterModeLoaderConfigPath);
        yield return new LabeledFolderPath(L.UI.ModOptionsForAllFolder, ModOptionsForAllFolder);
        yield return new LabeledFolderPath(L.UI.ModOptionsForEachFolder, ModOptionsForEachFolder);
    }
}

public record struct LabeledFolderPath(string Label, string Path);
