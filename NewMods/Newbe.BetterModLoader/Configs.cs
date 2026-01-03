using Newbe.BetterModLoader.Audio;

namespace Newbe.BetterModLoader;

/// <summary>
/// Centralized configuration management for Better Mod Loader (BML).
/// Follows the established Configs class pattern for consistent ModOptions management.
/// </summary>
public static class Configs
{
    private const string ForceRestartOnOrderChangesKey = "bml_developer_force_restart_setting";

    /// <summary>
    /// Gets or sets whether mod order changes require a forced game restart.
    /// Default is true to maintain backward compatibility.
    /// </summary>
    public static bool ForceRestartOnOrderChanges
    {
        get => GetForceRestartSetting();
        set => SetForceRestartSetting(value);
    }

    private static bool GetForceRestartSetting()
    {
        try
        {
            var result = ModOptions.Load(ForceRestartOnOrderChangesKey, true);
            Log.Debug($"[Configs] Force restart setting loaded: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load force restart setting, using default");
            return true;
        }
    }

    private static void SetForceRestartSetting(bool value)
    {
        try
        {
            ModOptions.Save(ForceRestartOnOrderChangesKey, value);
            Log.Debug($"[Configs] Force restart setting saved: {value}");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save force restart setting");
        }
    }

    /// <summary>
    /// Gets the current force restart setting from mod options.
    /// This can be used by other components to check if restart is required.
    /// </summary>
    /// <returns>True if restart is required on order changes, false otherwise</returns>
    public static bool ShouldForceRestartOnOrderChanges()
    {
        var result = GetForceRestartSetting();
        Log.Debug($"[Configs] ShouldForceRestartOnOrderChanges called, returning: {result}");
        return result;
    }


    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        try
        {
            ForceRestartOnOrderChanges = true;
            BackupEnabled = true;
            MaxBackups = 3;
            BmlBackupMetadata = [];

            // Audio settings defaults (volume is controlled by game AudioManager)
            AudioEnabled = true;
            ToggleAudioEnabled = true;
            ClickAudioEnabled = true;
            DragAudioEnabled = true;

            Log.Debug("[Configs] All settings reset to default");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to reset settings to default");
        }
    }

    /// <summary>
    /// Gets all current BML configuration values as a formatted string for debugging.
    /// </summary>
    /// <returns>String representation of current configuration state</returns>
    public static string GetDebugInfo()
    {
        try
        {
            return
                $"BML Configs - ForceRestartOnOrderChanges: {ForceRestartOnOrderChanges}, " +
                $"BackupEnabled: {BackupEnabled}, MaxBackups: {MaxBackups}, " +
                $"AudioEnabled: {AudioEnabled}, Volume: controlled by game SFX settings, " +
                $"ToggleAudioEnabled: {ToggleAudioEnabled}, ClickAudioEnabled: {ClickAudioEnabled}, DragAudioEnabled: {DragAudioEnabled}";
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to get debug info");
            return "BML Configs - Error retrieving configuration";
        }
    }

    #region Backup Configuration

    private const string BackupEnabledKey = "bml_backup_enabled";
    private const string MaxBackupsKey = "bml_max_backups";
    private const string BackupMetadataKey = "bml_backup_metadata";

    /// <summary>
    /// Gets or sets whether the BML backup functionality is enabled.
    /// Default is true.
    /// </summary>
    public static bool BackupEnabled
    {
        get => GetBackupEnabled();
        set => SetBackupEnabled(value);
    }

    /// <summary>
    /// Gets or sets the maximum number of backups to maintain.
    /// Default is 3, clamped between 1 and 10.
    /// </summary>
    public static int MaxBackups
    {
        get => GetMaxBackups();
        set => SetMaxBackups(value);
    }

    /// <summary>
    /// Gets or sets the backup metadata list.
    /// </summary>
    internal static List<BackupMetadata> BmlBackupMetadata
    {
        get => GetBackupMetadata();
        set => SetBackupMetadata(value);
    }

    private static bool GetBackupEnabled()
    {
        try
        {
            var result = ModOptions.Load(BackupEnabledKey, true);
            Log.Debug($"[Configs] Backup enabled setting loaded: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load backup enabled setting, using default");
            return true;
        }
    }

    private static void SetBackupEnabled(bool value)
    {
        try
        {
            ModOptions.Save(BackupEnabledKey, value);
            Log.Debug($"[Configs] Backup enabled setting saved: {value}");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save backup enabled setting");
        }
    }

    private static int GetMaxBackups()
    {
        try
        {
            var result = ModOptions.Load(MaxBackupsKey, 3);
            Log.Debug($"[Configs] Max backups setting loaded: {result}");
            return Math.Max(1, Math.Min(10, result)); // Clamp between 1 and 10
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load max backups setting, using default");
            return 3;
        }
    }

    private static void SetMaxBackups(int value)
    {
        try
        {
            var clampedValue = Math.Max(1, Math.Min(10, value));
            ModOptions.Save(MaxBackupsKey, clampedValue);
            Log.Debug($"[Configs] Max backups setting saved: {clampedValue}");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save max backups setting");
        }
    }

    private static List<BackupMetadata> GetBackupMetadata()
    {
        try
        {
            return ModOptions.Load(BackupMetadataKey, new List<BackupMetadata>())!;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load backup metadata, using empty list");
            return new List<BackupMetadata>();
        }
    }

    private static void SetBackupMetadata(List<BackupMetadata> value)
    {
        try
        {
            ModOptions.Save(BackupMetadataKey, value);
            Log.Debug("[Configs] Backup metadata saved");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save backup metadata");
        }
    }

    #endregion

    #region Audio Configuration

    private const string AudioEnabledKey = "bml_audio_enabled";
    private const string ToggleAudioEnabledKey = "bml_toggle_audio_enabled";
    private const string ClickAudioEnabledKey = "bml_click_audio_enabled";
    private const string DragAudioEnabledKey = "bml_drag_audio_enabled";

    /// <summary>
    /// Gets or sets whether BML audio effects are enabled.
    /// Default is true.
    /// </summary>
    public static bool AudioEnabled
    {
        get => GetAudioEnabled();
        set => SetAudioEnabled(value);
    }

    /// <summary>
    /// Gets or sets whether toggle switch audio effects are enabled.
    /// Default is true.
    /// </summary>
    public static bool ToggleAudioEnabled
    {
        get => GetToggleAudioEnabled();
        set => SetToggleAudioEnabled(value);
    }

    /// <summary>
    /// Gets or sets whether button click audio effects are enabled.
    /// Default is true.
    /// </summary>
    public static bool ClickAudioEnabled
    {
        get => GetClickAudioEnabled();
        set => SetClickAudioEnabled(value);
    }

    /// <summary>
    /// Gets or sets whether drag and drop audio effects are enabled.
    /// Default is true.
    /// </summary>
    public static bool DragAudioEnabled
    {
        get => GetDragAudioEnabled();
        set => SetDragAudioEnabled(value);
    }

    private static bool GetAudioEnabled()
    {
        try
        {
            var result = ModOptions.Load(AudioEnabledKey, true);
            Log.Debug($"[Configs] Audio enabled setting loaded: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load audio enabled setting, using default");
            return true;
        }
    }

    private static void SetAudioEnabled(bool value)
    {
        try
        {
            ModOptions.Save(AudioEnabledKey, value);
            Log.Debug($"[Configs] Audio enabled setting saved: {value}");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save audio enabled setting");
        }
    }

    private static bool GetToggleAudioEnabled()
    {
        try
        {
            var result = ModOptions.Load(ToggleAudioEnabledKey, true);
            Log.Debug($"[Configs] Toggle audio enabled setting loaded: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load toggle audio enabled setting, using default");
            return true;
        }
    }

    private static void SetToggleAudioEnabled(bool value)
    {
        try
        {
            ModOptions.Save(ToggleAudioEnabledKey, value);
            Log.Debug($"[Configs] Toggle audio enabled setting saved: {value}");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save toggle audio enabled setting");
        }
    }

    private static bool GetClickAudioEnabled()
    {
        try
        {
            var result = ModOptions.Load(ClickAudioEnabledKey, true);
            Log.Debug($"[Configs] Click audio enabled setting loaded: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load click audio enabled setting, using default");
            return true;
        }
    }

    private static void SetClickAudioEnabled(bool value)
    {
        try
        {
            ModOptions.Save(ClickAudioEnabledKey, value);
            Log.Debug($"[Configs] Click audio enabled setting saved: {value}");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save click audio enabled setting");
        }
    }

    private static bool GetDragAudioEnabled()
    {
        try
        {
            var result = ModOptions.Load(DragAudioEnabledKey, true);
            Log.Debug($"[Configs] Drag audio enabled setting loaded: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to load drag audio enabled setting, using default");
            return true;
        }
    }

    private static void SetDragAudioEnabled(bool value)
    {
        try
        {
            ModOptions.Save(DragAudioEnabledKey, value);
            Log.Debug($"[Configs] Drag audio enabled setting saved: {value}");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to save drag audio enabled setting");
        }
    }

    /// <summary>
    /// Applies audio settings to the BMLAudioManager.
    /// This should be called after initialization and when settings change.
    /// Note: Volume is now controlled by game's AudioManager SFX bus.
    /// </summary>
    public static void ApplyAudioSettings()
    {
        try
        {
            // Volume is now handled by AudioManager's SFX bus, no need to apply volume here
            Log.Debug(
                $"[Configs] Audio settings applied: Enabled={AudioEnabled}, Volume controlled by game SFX settings");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[Configs] Failed to apply audio settings");
        }
    }

    /// <summary>
    /// Gets whether audio should be played based on current settings.
    /// </summary>
    /// <param name="audioType">Type of audio effect</param>
    /// <returns>True if audio should be played, false otherwise</returns>
    public static bool ShouldPlayAudio(AudioEffectType audioType)
    {
        if (!AudioEnabled) return false;

        return audioType switch
        {
            AudioEffectType.ToggleOn or AudioEffectType.ToggleOff => ToggleAudioEnabled,
            AudioEffectType.ButtonClick => ClickAudioEnabled,
            AudioEffectType.DragStart or AudioEffectType.DragRelease => DragAudioEnabled,
            _ => true
        };
    }

    #endregion
}

/// <summary>
/// Internal metadata for backups.
/// </summary>
internal class BackupMetadata
{
    public DateTime BackupTime { get; set; }

    public string SourceFolder { get; set; } = string.Empty;

    public string BackupFolder { get; set; } = string.Empty;

    public string DllHash { get; set; } = string.Empty;

    public long BackupSize { get; set; }
}
