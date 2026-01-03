namespace Newbe.BetterModLoader.Deployments;

/// <summary>
/// 负责模组迁移和回退的核心逻辑，供 UI 层调用。
/// </summary>
internal static class DeploymentManager
{
    /// <summary>
    /// 执行迁移逻辑：
    /// 1. 记录当前已启用的所有模组（除 BetterModLoader 外）
    /// 2. 使用原生 API 禁用所有其他模组
    /// 3. 保存配置
    /// 4. 创建启用标记
    /// 5. 备份已启用模组列表
    /// </summary>
    public static void PerformMigration()
    {
        Log.Debug("[BetterModLoader] ======== Starting migration process ========");

        var modManager = ModManager.Instance;
        if (modManager == null)
        {
            Log.Warn("[BetterModLoader] ModManager instance not found, migration skipped.");
            return;
        }

        EnsureModsShouldBeActivatedByOldManager();

        // 创建启用标记
        ModLoaderSaveSystem.CreateActivationMarker();

        var preEnables = OldModEnabledBackupManager.MigrateEnablesInOldManager();
        var modInfos = ModManager.modInfos;
        ModGroupConfig.Instance.SyncUnsortedMods(modInfos, preEnables);

        Log.Debug("[BetterModLoader] ========= Migration process completed ========");
    }

    public static void EnsureModsShouldBeActivatedByOldManager()
    {
        var modInfos = ModManager.modInfos;
        var order = -9999;
        // enable duckyLogging if exists
        var duckyLoggingMod = modInfos.FirstOrDefault(ModIdentifier.IsDuckyLogging);
        if (!string.IsNullOrEmpty(duckyLoggingMod.name))
        {
            Log.Info("Ducky.Logging mod found, enabling it for better logging support.");
            ModManager.SetModPriority(duckyLoggingMod.name, order++);
            ModActivationHelper.TrySetShouldActivateMod(duckyLoggingMod, true);
        }

        // ensure to enable, 这样下次启动才能正常接管
        var info = modInfos.First(ModIdentifier.IsBetterModLoader);
        ModManager.SetModPriority(info.name, order++); // 确保最高优先级
        ModActivationHelper.TrySetShouldActivateMod(info, true);
    }

    public static void EnableConfigUpdateToDateWhenLoaderStarted()
    {
        Log.Debug("[BetterModLoader] ========= Starting DeploymentManager initialization ========");
        var modManager = ModManager.Instance;
        if (modManager == null)
        {
            Log.Warn("[BetterModLoader] ModManager instance not found, migration skipped.");
            return;
        }

        var preEnables = OldModEnabledBackupManager.MigrateEnablesInOldManager();
        if (preEnables.Count > 0)
        {
            Log.Current.Warn(
                "[BetterModLoader] There are even some enabled mods in the old manager during startup migration. This should not happen. But already migrated.");
        }

        ModGroupConfig.Instance.SyncUnsortedMods(ModManager.modInfos, preEnables);

        Log.Debug("[BetterModLoader] ========= DeploymentManager initialization completed ========");
    }

    /// <summary>
    /// 回退到旧版模组管理器：
    /// 1. 恢复备份的模组启用状态
    /// 2. 禁用 BetterModLoader
    /// 3. 删除启用标记和分组配置
    /// 4. 删除备份文件
    /// </summary>
    public static void PerformRollback()
    {
        try
        {
            Log.Debug("[BetterModLoader] ========= Starting rollback process ========");

            // 读取备份文件
            var backedUpModNames = ModLoaderSaveSystem.LoadEnabledModsBackup();
            if (backedUpModNames.Count == 0)
            {
                Log.Current.Warn(
                    "[BetterModLoader] No enabled mods found in backup. Proceeding with rollback without restoring mods.");
            }
            else
            {
                Log.Debug($"[BetterModLoader] Found {backedUpModNames.Count} mods in backup.");
                foreach (var modName in backedUpModNames)
                {
                    try
                    {
                        var modInfo =
                            ModGroupConfig.Instance.SortedModInfos.FirstOrDefault(m =>
                                m.OriginalModInfo.name == modName);
                        if (modInfo != null && modInfo.OriginalModInfo.name != null)
                        {
                            bool marked = ModActivationHelper.TrySetShouldActivateMod(modInfo.OriginalModInfo, true);
                            if (marked)
                            {
                                Log.Debug($"[BetterModLoader] Marked to enable next launch: {modName}");
                            }
                            else
                            {
                                Log.Warn(
                                    $"[BetterModLoader] Failed to mark mod for enable: {modName}");
                            }
                        }
                        else
                        {
                            Log.Warn($"[BetterModLoader] Mod not found: {modName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[BetterModLoader] Failed to mark mod {modName}: {ex.Message}");
                    }
                }

                // 删除备份文件
                if (ModLoaderSaveSystem.DeleteEnabledModsBackup())
                {
                    Log.Debug("[BetterModLoader] Deleted backup file after rollback.");
                }
                else
                {
                    Log.Warn("[BetterModLoader] Failed to delete backup file after rollback.");
                }
            }

            // 标记 BetterModLoader 为下次禁用
            Log.Debug("[BetterModLoader] Marking BetterModLoader to disable next launch...");
            var betterModLoaderInfo =
                ModGroupConfig.Instance.SortedModInfos.First(x => ModIdentifier.IsBetterModLoader(x.OriginalModInfo));
            if (betterModLoaderInfo.OriginalModInfo.name != null)
            {
                bool marked = ModActivationHelper.TrySetShouldActivateMod(betterModLoaderInfo.OriginalModInfo, false);
                if (marked)
                {
                    Log.Debug("[BetterModLoader] BetterModLoader marked to disable.");
                }
                else
                {
                    Log.Warn("[BetterModLoader] Failed to mark BetterModLoader for disable.");
                }
            }
            else
            {
                Log.Warn("[BetterModLoader] Could not find BetterModLoader in mod list.");
            }

            // 删除启用标记
            if (ModLoaderSaveSystem.DeleteActivationMarker())
            {
                Log.Debug("[BetterModLoader] Activation marker deleted.");
            }

            // 删除分组配置文件（ModGroups.json）
            ModGroupConfig.DeleteConfigFile();

            Log.Debug("[BetterModLoader] ========== Rollback process completed ========");
        }
        catch (Exception ex)
        {
            Log.Error($"[BetterModLoader] Rollback failed: {ex.Message}");
            Log.Error(ex, "Exception occurred");
        }
    }

    /// <summary>
    /// 将已启用的模组列表保存到备份文件
    /// </summary>
    public static void SaveEnabledModsBackup(List<string> enabledMods)
    {
        ModLoaderSaveSystem.SaveEnabledModsBackup(enabledMods);
    }
}
