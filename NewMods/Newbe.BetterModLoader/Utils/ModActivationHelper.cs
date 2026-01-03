using System.Reflection;

namespace Newbe.BetterModLoader.Utils;

/// <summary>
/// Helper to mark a mod's activation state via the game's internal API (SetShouldActivateMod).
/// NOTE: This does not immediately load/unload the mod; it only updates the persisted flag
/// so the game will or will not auto-activate it on the next launch or rescan.
/// </summary>
internal static class ModActivationHelper
{
    private static readonly MethodInfo ShouldActivateModMethod =
        typeof(ModManager).GetMethod("SetShouldActivateMod",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Try to mark a mod's activation flag for next launch.
    /// Returns true if the private API could be invoked; false if it failed.
    /// </summary>
    public static bool TrySetShouldActivateMod(ModInfo info, bool value)
    {
        try
        {
            var mm = ModManager.Instance;
            if (mm == null)
            {
                Log.Warn("[BetterModLoader] ModManager.Instance is null; cannot mark activation state.");
                return false;
            }

            ShouldActivateModMethod.Invoke(mm, [info, value]);
            Log.Debug($"[BetterModLoader] SetShouldActivateMod invoked for {info.name}, value={value}.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterModLoader] Failed to invoke SetShouldActivateMod: {ex.Message}");
            return false;
        }
    }
}
