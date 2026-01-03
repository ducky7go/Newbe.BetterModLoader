namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

internal class SteamModsLocalDataManager
{
    public const string CacheAllMods = "all_mods";
    public const string CacheAllModsLastUpdateTime = "all_mods_last_update_time";
    public const string UserSubscribedMods = "user_subscribed_mods";

    public static SteamModsLocalDataManager Instance { get; } = new();

    public List<SteamModDependencyInfo> GetUserSubscribedMods()
    {
        var json = ModOptions.ForId.LoadConfig<List<SteamModDependencyInfo>>(UserSubscribedMods);
        return json ?? [];
    }

    public void SetUserSubscribedMods(List<SteamModDependencyInfo> data)
    {
        ModOptions.ForId.SaveConfig(UserSubscribedMods, data);
    }

    private DateTimeOffset? GetLastUpdateTime()
    {
        try
        {
            var value = ModOptions.ForId.LoadConfig<long>(CacheAllModsLastUpdateTime);
            return DateTimeOffset.FromUnixTimeSeconds(value);
        }
        catch (Exception e)
        {
            // prevent previous invalid data from breaking everything
            Log.ErrorException("Failed to get last update time, resetting it.", e);
            return null;
        }
    }

    private void SetLastUpdateTime(DateTimeOffset time)
    {
        ModOptions.ForId.SaveConfig(CacheAllModsLastUpdateTime, time.ToUnixTimeSeconds());
    }

    public List<SteamModDependencyInfo> GetAllMods()
    {
        var json = ModOptions.ForId.LoadConfig<List<SteamModDependencyInfo>>(CacheAllMods);
        return json ?? [];
    }

    private void SetAllMods(List<SteamModDependencyInfo> data)
    {
        ModOptions.ForId.SaveConfig(CacheAllMods, data);
    }

    public UniTask EnsureCachedDependencyDataAsync()
    {
        var time = GetLastUpdateTime();
        if (time.HasValue && (DateTimeOffset.UtcNow - time.Value).TotalHours < 1)
        {
            return UniTask.CompletedTask;
        }

        return UniTask.RunOnThreadPool(async () =>
        {
            try
            {
                var data = await BmlSteamItemManager.Instance.QueryAllModsAsync();
                SetAllMods(data);
                SetLastUpdateTime(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Log.ErrorException("Failed to ensure cached dependency data", ex);
            }
        });
    }
}
