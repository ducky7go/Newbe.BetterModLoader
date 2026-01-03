namespace Newbe.BetterModLoader;

/// <summary>
/// 模组加载器的持久化存储系统，参考游戏原生 SavesSystem 模式。
/// 负责集中管理模组分组配置、元数据和相关文件，使用 Application.persistentDataPath。
/// </summary>
internal static class ModLoaderSaveSystem
{
    private const string CompanyFolderName = "Newbe";
    private const string ModLoaderFolderName = "Newbe.BetterModLoader";
    private const string ConfigFileName = "ModGroupConfig.json";
    private const string BackupSuffix = ".bac";
    private const int BackupListCount = 3; // 保留最近3个配置备份

    /// <summary>
    /// Activation marker management for BetterModLoader full mode.
    /// </summary>
    private const string ActivationMarkerFileName = "bettermodloader-enabled.txt";

    private const string EnablsInOldManagerBackupFileName = "enabled-mods-backup.txt";

    private static bool _cached;
    private static DateTime _lastSavedTime = DateTime.MinValue;

    /// <summary>savesystem
    /// ModLoader 数据文件夹的完整路径
    /// </summary>
    public static string ModLoaderFolder =>
        Path.Combine(Application.persistentDataPath, CompanyFolderName, ModLoaderFolderName);

    /// <summary>
    /// 配置文件的完整路径
    /// </summary>
    public static string ConfigFilePath => Path.Combine(ModLoaderFolder, ConfigFileName);

    /// <summary>
    /// 是否正在保存
    /// </summary>
    public static bool IsSaving { get; private set; }

    /// <summary>
    /// 上次保存时间
    /// </summary>
    private static DateTime LastSavedTime
    {
        get
        {
            if (_lastSavedTime > DateTime.UtcNow)
            {
                _lastSavedTime = DateTime.UtcNow;
            }

            return _lastSavedTime;
        }
        set => _lastSavedTime = value;
    }

    /// <summary>
    /// 距离上次保存的时间
    /// </summary>
    private static TimeSpan TimeSinceLastSave => DateTime.UtcNow - LastSavedTime;

    /// <summary>
    /// 配置保存事件（在配置成功保存后触发）
    /// </summary>
    public static event Action? OnConfigSaved;

    /// <summary>
    /// 配置加载事件（在配置成功加载后触发）
    /// </summary>
    public static event Action? OnConfigLoaded;

    /// <summary>
    /// 确保 ModLoader 文件夹存在
    /// </summary>
    public static void EnsureModLoaderFolderExists()
    {
        if (!Directory.Exists(ModLoaderFolder))
        {
            Directory.CreateDirectory(ModLoaderFolder);
            Log.Debug($"[ModLoaderSaveSystem] Created ModLoader folder: {ModLoaderFolder}");
        }
    }


    /// <summary>
    /// Get the full path to the activation marker file.
    /// </summary>
    public static string GetActivationMarkerPath()
    {
        return Path.Combine(ModLoaderFolder, ActivationMarkerFileName);
    }

    /// <summary>
    /// Check whether BetterModLoader full mode is activated.
    /// </summary>
    public static bool IsActivationMarkerEnabled()
    {
        try
        {
            return File.Exists(GetActivationMarkerPath());
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to check activation marker: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create the activation marker file, enabling full mode on next startup.
    /// Safe to call repeatedly.
    /// </summary>
    public static void CreateActivationMarker()
    {
        try
        {
            var dir = Path.GetDirectoryName(GetActivationMarkerPath());
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(GetActivationMarkerPath(), $"enabled at {DateTime.UtcNow:o}\n");
            Log.Debug($"[ModLoaderSaveSystem] ✓ Activation marker created at: {GetActivationMarkerPath()}");
        }
        catch (Exception ex)
        {
            Log.DebugException("[ModLoaderSaveSystem] Failed to create activation marker", ex);
        }
    }

    /// <summary>
    /// Delete the activation marker file.
    /// </summary>
    public static bool DeleteActivationMarker()
    {
        var markerPath = GetActivationMarkerPath();
        try
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
                Log.Debug($"[ModLoaderSaveSystem] Activation marker deleted: {markerPath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to delete activation marker: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Read the activation marker file contents.
    /// </summary>
    public static string? ReadActivationMarker()
    {
        var markerPath = GetActivationMarkerPath();
        try
        {
            if (File.Exists(markerPath))
            {
                return File.ReadAllText(markerPath);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to read activation marker: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 保存模组分组配置
    /// </summary>
    public static void SaveConfig(ModGroupConfigSerializable config)
    {
        if (config == null)
        {
            Log.Warn("[ModLoaderSaveSystem] Cannot save null config");
            return;
        }

        try
        {
            IsSaving = true;
            LastSavedTime = DateTime.UtcNow;

            EnsureModLoaderFolderExists();

            // 创建备份
            CreateBackup();

            // 序列化配置
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);

            // 写入文件
            File.WriteAllText(ConfigFilePath, json);

            _cached = true;

            Log.Debug($"[ModLoaderSaveSystem] Config saved successfully with {config.Groups.Count} groups");

            OnConfigSaved?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error($"[ModLoaderSaveSystem] Failed to save config: {ex.Message}");
            Log.Error(ex, "Exception occurred");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 加载模组分组配置
    /// </summary>
    public static ModGroupConfigSerializable? LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                Log.Debug("[ModLoaderSaveSystem] Config file not found, returning empty config");
                return new ModGroupConfigSerializable();
            }

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonConvert.DeserializeObject<ModGroupConfigSerializable>(json);

            if (config == null)
            {
                Log.Warn("[ModLoaderSaveSystem] Deserialization returned null, attempting restore from backup");
                return RestoreFromBackup();
            }

            _cached = true;
            Log.Debug($"[ModLoaderSaveSystem] Config loaded successfully with {config.Groups.Count} groups");

            OnConfigLoaded?.Invoke();
            return config;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModLoaderSaveSystem] Failed to load config: {ex.Message}");
            Log.Error(ex, "Exception occurred");

            Log.Debug("[ModLoaderSaveSystem] Attempting to restore from backup...");
            return RestoreFromBackup();
        }
    }

    /// <summary>
    /// 创建配置备份
    /// </summary>
    private static void CreateBackup()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return;
            }

            // 使用时间戳备份
            var timestamp = DateTime.UtcNow.ToBinary();
            var backupPath = $"{ConfigFilePath}{BackupSuffix}.{timestamp:D19}";

            File.Copy(ConfigFilePath, backupPath, overwrite: true);

            // 清理旧备份（只保留最近的 BackupListCount 个）
            CleanupOldBackups();

            Log.Debug($"[ModLoaderSaveSystem] Created backup: {Path.GetFileName(backupPath)}");
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to create backup: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理旧备份，只保留最近的几个
    /// </summary>
    private static void CleanupOldBackups()
    {
        try
        {
            var backupPattern = $"{Path.GetFileName(ConfigFilePath)}{BackupSuffix}.*";
            var backupFiles = Directory.GetFiles(ModLoaderFolder, backupPattern)
                .OrderByDescending(f => f)
                .ToList();

            if (backupFiles.Count > BackupListCount)
            {
                foreach (var oldBackup in backupFiles.Skip(BackupListCount))
                {
                    File.Delete(oldBackup);
                    Log.Debug($"[ModLoaderSaveSystem] Deleted old backup: {Path.GetFileName(oldBackup)}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to cleanup old backups: {ex.Message}");
        }
    }

    /// <summary>
    /// 从备份恢复配置
    /// </summary>
    private static ModGroupConfigSerializable? RestoreFromBackup()
    {
        try
        {
            var backupPattern = $"{Path.GetFileName(ConfigFilePath)}{BackupSuffix}.*";
            var backupFiles = Directory.GetFiles(ModLoaderFolder, backupPattern)
                .OrderByDescending(f => f)
                .ToList();

            foreach (var backupFile in backupFiles)
            {
                try
                {
                    Log.Debug($"[ModLoaderSaveSystem] Attempting to restore from: {Path.GetFileName(backupFile)}");

                    var json = File.ReadAllText(backupFile);
                    var config = JsonConvert.DeserializeObject<ModGroupConfigSerializable>(json);

                    if (config != null && config.Groups != null)
                    {
                        // 恢复成功，复制备份到主配置文件
                        File.Copy(backupFile, ConfigFilePath, overwrite: true);
                        Log.Current.Debug(
                            $"[ModLoaderSaveSystem] Successfully restored from backup: {Path.GetFileName(backupFile)}");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(
                        $"[ModLoaderSaveSystem] Failed to restore from {Path.GetFileName(backupFile)}: {ex.Message}");
                    continue;
                }
            }

            Log.Warn("[ModLoaderSaveSystem] All backup restore attempts failed, returning empty config");
            return new ModGroupConfigSerializable();
        }
        catch (Exception ex)
        {
            Log.Error($"[ModLoaderSaveSystem] Critical error in restore process: {ex.Message}");
            Log.Error(ex, "Exception occurred");
            return new ModGroupConfigSerializable();
        }
    }

    /// <summary>
    /// 获取所有备份信息
    /// </summary>
    public static List<BackupInfo> GetBackupList()
    {
        var backups = new List<BackupInfo>();

        try
        {
            if (!Directory.Exists(ModLoaderFolder))
            {
                return backups;
            }

            var backupPattern = $"{Path.GetFileName(ConfigFilePath)}{BackupSuffix}.*";
            var backupFiles = Directory.GetFiles(ModLoaderFolder, backupPattern)
                .OrderByDescending(f => f)
                .ToList();

            foreach (var backupFile in backupFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(backupFile);
                    var parts = fileName.Split('.');
                    if (parts.Length > 0 && long.TryParse(parts[^1], out var timestamp))
                    {
                        var time = DateTime.FromBinary(timestamp);
                        backups.Add(new BackupInfo
                        {
                            Path = backupFile,
                            Time = time,
                            Exists = true
                        });
                    }
                }
                catch
                {
                    // 忽略无法解析的备份文件
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to get backup list: {ex.Message}");
        }

        return backups;
    }

    /// <summary>
    /// 手动从指定备份恢复
    /// </summary>
    public static bool RestoreFromSpecificBackup(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                Log.Warn($"[ModLoaderSaveSystem] Backup file not found: {backupPath}");
                return false;
            }

            var json = File.ReadAllText(backupPath);
            var config = JsonConvert.DeserializeObject<ModGroupConfigSerializable>(json);

            if (config == null || config.Groups == null)
            {
                Log.Warn($"[ModLoaderSaveSystem] Invalid backup file: {backupPath}");
                return false;
            }

            File.Copy(backupPath, ConfigFilePath, overwrite: true);
            Log.Debug($"[ModLoaderSaveSystem] Successfully restored from: {Path.GetFileName(backupPath)}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModLoaderSaveSystem] Failed to restore from specific backup: {ex.Message}");
            Log.Error(ex, "Exception occurred");
            return false;
        }
    }

    /// <summary>
    /// 删除所有配置和备份（危险操作）
    /// </summary>
    public static void DeleteAllConfigData()
    {
        try
        {
            if (Directory.Exists(ModLoaderFolder))
            {
                Directory.Delete(ModLoaderFolder, recursive: true);
                Log.Debug("[ModLoaderSaveSystem] All config data deleted");
            }

            _cached = false;
        }
        catch (Exception ex)
        {
            Log.Error($"[ModLoaderSaveSystem] Failed to delete config data: {ex.Message}");
            Log.Error(ex, "Exception occurred");
        }
    }

    /// <summary>
    /// Get the full path to enabled-mods-backup.txt.
    /// Use this to locate the backup file for enabled mods.
    /// </summary>
    private static string GetEnabledModsBackupPath()
    {
        return Path.Combine(ModLoaderFolder, EnablsInOldManagerBackupFileName);
    }

    /// <summary>
    /// Save the list of enabled mods to enabled-mods-backup.txt.
    /// This method is thread-safe and logs errors.
    /// Call this when you need to backup the current enabled mods state.
    /// </summary>
    /// <param name="enabledMods">List of enabled mod names.</param>
    public static void SaveEnabledModsBackup(List<string> enabledMods)
    {
        try
        {
            var backupPath = GetEnabledModsBackupPath();
            var lines = new List<string>
            {
                "# BetterModLoader - Enabled Mods Backup",
                $"# Created at: {DateTime.UtcNow:o}",
                "# This file records the mods that were enabled before migration.",
                ""
            };
            lines.AddRange(enabledMods);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            File.WriteAllLines(backupPath, lines);
            Log.Debug($"[ModLoaderSaveSystem] Saved backup of {enabledMods.Count} enabled mods to: {backupPath}");
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to save enabled mods backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Load the list of enabled mods from enabled-mods-backup.txt.
    /// Returns an empty list if the file does not exist or is invalid.
    /// </summary>
    /// <returns>List of enabled mod names.</returns>
    public static List<string> LoadEnabledModsBackup()
    {
        var backupPath = GetEnabledModsBackupPath();
        var result = new List<string>();
        try
        {
            if (!File.Exists(backupPath))
            {
                Log.Warn($"[ModLoaderSaveSystem] Backup file not found: {backupPath}");
                return result;
            }

            var lines = File.ReadAllLines(backupPath);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                {
                    result.Add(line.Trim());
                }
            }

            Log.Debug($"[ModLoaderSaveSystem] Loaded {result.Count} enabled mods from backup.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to load enabled mods backup: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Delete enabled-mods-backup.txt.
    /// Returns true if the file was deleted, false otherwise.
    /// </summary>
    /// <returns>True if deleted, false otherwise.</returns>
    public static bool DeleteEnabledModsBackup()
    {
        var backupPath = GetEnabledModsBackupPath();
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                Log.Debug($"[ModLoaderSaveSystem] Deleted backup file: {backupPath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModLoaderSaveSystem] Failed to delete enabled mods backup: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Check if enabled-mods-backup.txt exists.
    /// </summary>
    /// <returns>True if backup exists, false otherwise.</returns>
    public static bool EnabledModsBackupExists()
    {
        return File.Exists(GetEnabledModsBackupPath());
    }

    /// <summary>
    /// 备份信息结构
    /// </summary>
    public struct BackupInfo
    {
        public string Path;
        public DateTime Time;
        public bool Exists;
    }
}
