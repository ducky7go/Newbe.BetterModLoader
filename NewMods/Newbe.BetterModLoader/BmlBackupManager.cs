using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Newbe.BetterModLoader;

/// <summary>
/// Manages automatic game backup functionality for BML.
/// Provides intelligent backup based on DLL hash comparison and async execution.
/// </summary>
public static class BmlBackupManager
{
    private static readonly ConcurrentDictionary<string, string> DLLHashCache = new();

    /// <summary>
    /// Gets or sets whether the BML backup functionality is enabled.
    /// Default is true.
    /// </summary>
    public static bool BackupEnabled
    {
        get => Configs.BackupEnabled;
        set => Configs.BackupEnabled = value;
    }

    /// <summary>
    /// Gets or sets the maximum number of backups to maintain.
    /// Default is 3.
    /// </summary>
    public static int MaxBackups
    {
        get => Configs.MaxBackups;
        set => Configs.MaxBackups = value;
    }

    /// <summary>
    /// Triggers an asynchronous backup check and creation if needed.
    /// Should be called during mod activation process.
    /// </summary>
    /// <returns>A task that represents the backup operation</returns>
    public static void TriggerBackupCheck()
    {
        if (!BackupEnabled)
        {
            Log.Debug("[BmlBackupManager] Backup is disabled, skipping backup check");
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Debug("[BmlBackupManager] Starting BML backup check");

            var bmlAssemblyPath = GetBmlAssemblyPath();

            var bmlFolder = Path.GetDirectoryName(bmlAssemblyPath);
            if (string.IsNullOrEmpty(bmlFolder))
            {
                Log.Warn("[BmlBackupManager] Failed to get BML folder path");
                return;
            }

            var currentHash = CalculateFileHash(bmlAssemblyPath);
            var backupMetadata = LoadBackupMetadata();

            var needsBackup = backupMetadata.All(m => m.DllHash != currentHash);

            if (needsBackup)
            {
                Log.Info($"[BmlBackupManager] BML assembly has changed (hash: {currentHash[..8]}...), creating backup");
                CreateBackup(bmlFolder, currentHash);
            }
            else
            {
                Log.Debug($"[BmlBackupManager] BML assembly unchanged (hash: {currentHash[..8]}...), skipping backup");
            }

            // Clean up old backups
            CleanupOldBackups();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BmlBackupManager] Failed to perform BML backup check");
        }
        finally
        {
            stopwatch.Stop();
            Log.Debug($"[BmlBackupManager] BML backup check completed in {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    /// <summary>
    /// Gets the path to the BML assembly DLL file.
    /// </summary>
    /// <returns>Path to the BML assembly</returns>
    private static string GetBmlAssemblyPath()
    {
        try
        {
            // Get the current executing assembly (BML assembly)
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var assemblyPath = assembly.Location;

            Log.Debug($"[BmlBackupManager] Found BML assembly at: {assemblyPath}");
            return Path.GetFullPath(assemblyPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BmlBackupManager] Failed to get BML assembly path");
            return string.Empty;
        }
    }

    /// <summary>
    /// Calculates SHA256 hash of a file.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>SHA256 hash as hex string</returns>
    private static string CalculateFileHash(string filePath)
    {
        if (DLLHashCache.TryGetValue(filePath, out var cachedHash))
        {
            return cachedHash;
        }

        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            DLLHashCache.TryAdd(filePath, hashString);
            return hashString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[BmlBackupManager] Failed to calculate hash for file: {filePath}");
            throw;
        }
    }

    /// <summary>
    /// Creates a backup of the entire BML folder.
    /// </summary>
    /// <param name="bmlFolder">Path to the BML folder to backup</param>
    /// <param name="dllHash">Hash of the BML assembly</param>
    /// <returns>A task that represents the backup operation</returns>
    private static void CreateBackup(string bmlFolder, string dllHash)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFolderName = $"BML_{timestamp}";
        var backupPath = Path.Combine(ModLoaderSaveSystem.ModLoaderFolder, "Backups", backupFolderName);

        try
        {
            Log.Info($"[BmlBackupManager] Creating backup at: {backupPath}");

            // Ensure backup directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

            // Copy entire folder asynchronously
            CopyFolder(bmlFolder, backupPath);

            // Save backup metadata
            var metadata = new BackupMetadata
            {
                BackupTime = DateTime.Now,
                SourceFolder = bmlFolder,
                BackupFolder = backupPath,
                DllHash = dllHash,
                BackupSize = CalculateFolderSize(backupPath)
            };

            SaveBackupMetadata(metadata);

            Log.Info($"[BmlBackupManager] Backup created successfully ({metadata.BackupSize / 1024.0 / 1024.0:F1} MB)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[BmlBackupManager] Failed to create backup at: {backupPath}");

            // Clean up partial backup if it exists
            try
            {
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                    Log.Debug($"[BmlBackupManager] Cleaned up partial backup: {backupPath}");
                }
            }
            catch (Exception cleanupEx)
            {
                Log.Warn(cleanupEx, $"[BmlBackupManager] Failed to clean up partial backup: {backupPath}");
            }

            throw;
        }
    }

    /// <summary>
    /// Copies a folder and all its contents asynchronously.
    /// </summary>
    /// <param name="sourcePath">Source folder path</param>
    /// <param name="destinationPath">Destination folder path</param>
    /// <returns>A task that represents the copy operation</returns>
    private static void CopyFolder(string sourcePath, string destinationPath)
    {
        // Create all subdirectories
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, dirPath);
            var destinationDir = Path.Combine(destinationPath, relativePath);
            Directory.CreateDirectory(destinationDir);
        }

        // Copy all files
        var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
        var totalFiles = files.Length;
        var processedFiles = 0;

        foreach (var filePath in files)
        {
            try
            {
                var relativePath = Path.GetRelativePath(sourcePath, filePath);
                var destinationFile = Path.Combine(destinationPath, relativePath);

                File.Copy(filePath, destinationFile, true);
                processedFiles++;

                // Log progress every 100 files
                if (processedFiles % 100 == 0)
                {
                    Log.Debug($"[BmlBackupManager] Copied {processedFiles}/{totalFiles} files");
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, $"[BmlBackupManager] Failed to copy file: {filePath}");
            }
        }

        Log.Debug($"[BmlBackupManager] Completed copying {processedFiles} files");
    }

    /// <summary>
    /// Calculates the total size of a folder in bytes.
    /// </summary>
    /// <param name="folderPath">Path to the folder</param>
    /// <returns>Total size in bytes</returns>
    private static long CalculateFolderSize(string folderPath)
    {
        try
        {
            return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Select(filePath => new FileInfo(filePath).Length)
                .Sum();
        }
        catch (Exception ex)
        {
            Log.Warn(ex, $"[BmlBackupManager] Failed to calculate folder size: {folderPath}");
            return 0;
        }
    }

    /// <summary>
    /// Loads backup metadata from storage.
    /// </summary>
    /// <returns>List of backup metadata</returns>
    private static List<BackupMetadata> LoadBackupMetadata()
    {
        return Configs.BmlBackupMetadata;
    }

    /// <summary>
    /// Saves backup metadata to storage.
    /// </summary>
    /// <param name="metadata">Metadata to save</param>
    private static void SaveBackupMetadata(BackupMetadata metadata)
    {
        try
        {
            var allMetadata = LoadBackupMetadata();
            allMetadata.Add(metadata);

            Configs.BmlBackupMetadata = allMetadata;

            Log.Debug($"[BmlBackupManager] Saved backup metadata for: {metadata.BackupFolder}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BmlBackupManager] Failed to save backup metadata");
        }
    }

    /// <summary>
    /// Cleans up old backups beyond the maximum limit.
    /// </summary>
    /// <returns>A task that represents the cleanup operation</returns>
    private static void CleanupOldBackups()
    {
        try
        {
            var metadata = LoadBackupMetadata()
                .OrderBy(m => m.BackupTime)
                .ToList();

            var maxBackups = MaxBackups;
            if (metadata.Count <= maxBackups)
            {
                return;
            }

            var backupsToDelete = metadata.Take(metadata.Count - maxBackups);
            var deletedCount = 0;

            foreach (var oldBackup in backupsToDelete)
            {
                try
                {
                    if (Directory.Exists(oldBackup.BackupFolder))
                    {
                        Directory.Delete(oldBackup.BackupFolder, true);
                        Log.Debug($"[BmlBackupManager] Deleted old backup: {oldBackup.BackupFolder}");
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, $"[BmlBackupManager] Failed to delete old backup: {oldBackup.BackupFolder}");
                }
            }

            // Update metadata by removing deleted backups
            var remainingMetadata = metadata.Skip(metadata.Count - maxBackups).ToList();
            Configs.BmlBackupMetadata = remainingMetadata;

            Log.Info($"[BmlBackupManager] Cleaned up {deletedCount} old backup(s)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BmlBackupManager] Failed to cleanup old backups");
        }
    }

    /// <summary>
    /// Gets information about all existing backups.
    /// </summary>
    /// <returns>List of backup information</returns>
    public static List<BackupInfo> GetBackupInfo()
    {
        try
        {
            var metadata = LoadBackupMetadata()
                .OrderByDescending(m => m.BackupTime)
                .ToList();

            return metadata.Select(m => new BackupInfo
            {
                BackupTime = m.BackupTime,
                BackupName = Path.GetFileName(m.BackupFolder),
                BackupPath = m.BackupFolder,
                BackupSize = m.BackupSize,
                DllHash = m.DllHash,
                IsValid = Directory.Exists(m.BackupFolder)
            }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BmlBackupManager] Failed to get backup info");
            return [];
        }
    }
}

#region Public Data Models

/// <summary>
/// Public information about backups.
/// </summary>
public class BackupInfo
{
    public DateTime BackupTime { get; set; }

    public string BackupName { get; set; } = string.Empty;

    public string BackupPath { get; set; } = string.Empty;

    public long BackupSize { get; set; }

    public string DllHash { get; set; } = string.Empty;

    public bool IsValid { get; set; }
}

#endregion
