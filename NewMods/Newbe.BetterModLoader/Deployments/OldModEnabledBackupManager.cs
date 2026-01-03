using System.Reflection;

namespace Newbe.BetterModLoader.Deployments;

public abstract class OldModEnabledBackupManager
{
    private static readonly MethodInfo ShouldActivateModMethod =
        typeof(ModManager).GetMethod("ShouldActivateMod",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static bool ShouldActivateMod(ModInfo modInfo) =>
        (bool)ShouldActivateModMethod.Invoke(ModManager.Instance, [modInfo])!;

    /// <summary>
    /// gets the list of enabled mods in old manager, disable them via old API and return the list. List of mod names.
    /// </summary>
    /// <returns></returns>
    internal static List<string> MigrateEnablesInOldManager()
    {
        var backups = ModLoaderSaveSystem.LoadEnabledModsBackup();
        var oldMods = ModManager.modInfos.Where(x => !ModIdentifier.IsBetterModLoader(x)).ToList();
        var newDisables = new List<ModInfo>();
        foreach (var modInfo in oldMods)
        {
            if (ShouldActivateMod(modInfo))
            {
                if (!backups.Contains(modInfo.name))
                {
                    backups.Add(modInfo.name);
                    newDisables.Add(modInfo);
                    Log.Current.Debug(
                        $"[BetterModLoader] Backed up enabled mod from old system: {modInfo.name}");
                }
            }
        }

        ModLoaderSaveSystem.SaveEnabledModsBackup(backups);

        foreach (var modInfo in newDisables)
        {
            ModActivationHelper.TrySetShouldActivateMod(modInfo, false);
            Log.Current.Debug(
                $"[BetterModLoader] Disabled mod via old system API: {modInfo.name}");
        }

        return newDisables.Select(x => x.name).ToList();
    }
}
