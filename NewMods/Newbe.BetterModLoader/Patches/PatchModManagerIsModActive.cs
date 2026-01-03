using System.Reflection;
using HarmonyLib;
using Saves;
using System.Collections.Generic;

namespace Newbe.BetterModLoader;

/// <summary>
/// Harmony patch to redirect ModManager.IsModActive queries to BetterModManager.
/// This ensures that external code querying mod status gets accurate results
/// when BetterModManager is handling mod activation/deactivation.
/// </summary>
[HarmonyPatch]
internal class PatchModManagerIsModActive
{
    /// <summary>
    /// 核心逻辑：检查模组是否激活的静态方法，可供其他地方调用
    /// </summary>
    /// <param name="info">要检查的模组信息</param>
    /// <param name="instance">输出的模组实例</param>
    /// <returns>模组是否激活</returns>
    public static bool CheckModActive(ModInfo info, out Duckov.Modding.ModBehaviour instance)
    {
        instance = null;

        try
        {
            // 通过 ModId 查找对应的 ModInfo2
            var modId = info.GetModId();
            var modInfo2 = BetterModManager.GroupConfig.SortedModInfos.FirstOrDefault(mi => mi.Id == modId);

            if (modInfo2 == null)
            {
                Log.Debug($"[BML] IsActive: Mod not found in BML registry for {info.name}");
                return false;
            }

            // Directly use the new ModInfo overload of BetterModManager.IsActive
            bool isActive = BetterModManager.IsActive(modInfo2, out var behaviour);

            if (isActive)
            {
                instance = behaviour;
                Log.Debug($"[BML]: Redirected check for {info.name} = {isActive}");
                return true;
            }

            // load from Saves as fallback (legacy support)
            // do no invoke ModManager to get active mod list to avoid circular call and activation ModManagerUI and others
            var defaultEnable = SavesSystem.LoadGlobal<bool>("ModActive_" + info.name);
            Log.Info("[BML] IsActive fallback load from Saves for mod {ModName}, result: {Result}",
                info.name, defaultEnable);
            return defaultEnable;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BML] CheckModActive: Error checking mod activity");
            return false;
        }
    }

    /// <summary>
    /// Target the IsModActive method in ModManager.
    /// </summary>
    /// <returns>MethodBase for the IsModActive method</returns>
    [HarmonyTargetMethods]
    static IEnumerable<MethodBase> TargetMethods()
    {
        // Target IsModActive(ModInfo info, out ModBehaviour instance)
        yield return AccessTools.Method(typeof(ModManager), "IsModActive",
            [typeof(ModInfo), typeof(Duckov.Modding.ModBehaviour).MakeByRefType()]);
    }

    /// <summary>
    /// Prefix method to redirect IsModActive calls to BetterModManager.
    /// Since this patch is only active when BML is alive, we can safely redirect.
    /// </summary>
    /// <param name="info">ModInfo parameter</param>
    /// <param name="instance">ModBehaviour instance to return</param>
    /// <param name="__result">Result to return</param>
    /// <param name="__originalMethod">The original method being called</param>
    /// <returns>false to skip original method</returns>
    [HarmonyPrefix]
    static bool Prefix(ModInfo info, out Duckov.Modding.ModBehaviour instance, ref bool __result,
        MethodBase __originalMethod)
    {
        try
        {
            __result = CheckModActive(info, out instance);
            return false; // Skip original method
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BML] Patch_ModManager_IsModActive: Error in patch, falling back to original");
            instance = null;
            __result = false;
            return true; // On error, execute original method
        }
    }
}
