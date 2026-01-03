using Duckov.Modding.UI;
using HarmonyLib;
using Newbe.BetterModLoader.Deployments;
using Newbe.BetterModLoader.Audio;

namespace Newbe.BetterModLoader;

/// <summary>
/// BetterModLoader 主入口，确保单例模式（只允许一个实例存在）。
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    private const string HarmonyId = "bettermodloader";

    private static ModBehaviour? _instance;

    private void Awake()
    {
        // 单例检查：如已存在实例且不是自己，则销毁自身
        if (_instance != null && _instance != this)
        {
            Log.Warn("[BetterModLoader] Duplicate ModBehaviour detected, destroying this instance.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private object _harmony = null!;

    protected override void ModEnabled()
    {
        Log.Debug("[BetterModLoader] Initializing...");

        try
        {
            // 首次启用时：若未启用标记，说明用户刚勾选启用但还未完成迁移
            // 立即禁用自己，然后弹出迁移确认
            // 只有用户确认迁移后才会创建marker并重新启用，下次启动才真正接管
            var hasMarker = ModLoaderSaveSystem.IsActivationMarkerEnabled();
            if (!hasMarker)
            {
                Log.Current.Debug(
                    "[BetterModLoader] Activation marker not found. Marking self as disabled and asking user to migrate...");

                // 然后弹出迁移弹窗，让用户决定
                MigrationManager.StartMigration(
                    onConfirm: () => { Log.Debug("[BetterModLoader] Migration confirmed."); },
                    onCancel: () =>
                    {
                        Log.Debug("[BetterModLoader] User declined migration");
                        ModManager.Instance.DeactivateMod(info);
                    });

                // 不进行任何 Patch 或 UI 初始化
                return;
            }

            DeploymentManager.EnsureModsShouldBeActivatedByOldManager();

            PatchFull();

            UIResourceManager.Initialize();

            // 创建 BML 音频管理器实例
            var audioManagerGO = new GameObject("BMLAudioManager");
            audioManagerGO.AddComponent<BMLAudioManager>();

            Configs.ApplyAudioSettings();

            DeploymentManager.EnableConfigUpdateToDateWhenLoaderStarted();

            // 游戏启动时扫描初始配置并执行启用任务
            // （注意：这里只执行启用，不执行禁用，因为还没有运行的模组可以禁用）
            var jobs = BetterModManager.ScanActivationJobs();

            Log.Info($"[BetterModLoader] Found {jobs.Length} mod(s) to enable on startup.");
            foreach (var job in jobs)
            {
                Log.Info(
                    $"[BetterModLoader]   -> Enabling mod: {job.Info.OriginalModInfo.name} {job.Info.OriginalModInfo.displayName}");
            }

            // 直接显示模组激活进度面板
            ModActivationProgressPanel.Show(jobs, requiresShutdownDueToOrderChange: false, loadingMode: ModActivationProgressPanel.LoadingMode.GameStartup);

            // 触发游戏备份检查（异步执行，不阻塞模组加载）
            UniTask.RunOnThreadPool(() =>
            {
                try
                {
                    BmlBackupManager.TriggerBackupCheck();
                    Log.Debug("[BetterModLoader] BML backup check completed");
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "[BetterModLoader] BML backup check failed");
                }
            }).Forget();

            Refresh().Forget();
            LinkProvider.Instance.EnsureNetworkDeterminedAsync().Forget();

            Log.Debug("[BetterModLoader] Initialization completed successfully.");
        }
        catch (Exception ex)
        {
            Log.ErrorException("[BetterModLoader] Initialization failed", ex);
        }
    }

    private async UniTask Refresh()
    {
        await SteamModsLocalDataManager.Instance.EnsureCachedDependencyDataAsync();
        BmlSteamItemManager.Instance.RefreshUserSubMods();
    }

    protected override void ModDisabled()
    {
        // 取消订阅事件
        // BetterModManagerCanvas.ConfigSaved -= OnConfigSaved;
    }


    /// <summary>
    /// 应用完整接管模式的 Patch 集（拦截 ModManager 与 UI）
    /// </summary>
    private void PatchFull()
    {
        try
        {
            Log.Debug($"[BetterModLoader] ✓ Harmony instance ready with ID: {HarmonyId}");
            _harmony = new Harmony(HarmonyId);
            var h = (Harmony)_harmony;

            // 仅补丁需要的类，避免在未启用时拦截旧 UI
            var types = new[]
            {
                typeof(InterceptFadeGroupButton),
                typeof(InterceptShouldActivateModPatch),
                typeof(PatchModManagerIsModActive),
            };

            foreach (var t in types)
            {
                h.CreateClassProcessor(t).Patch();
            }

            var patchedMethods = h.GetPatchedMethods().ToList();
            Log.Debug($"[BetterModLoader] ✓ Full mode patched {patchedMethods.Count} methods");
        }
        catch (Exception ex)
        {
            Log.Error($"[BetterModLoader] Failed to apply FULL patches: {ex.Message}");
            Log.Error(ex, "Exception occurred");
        }
    }
}
