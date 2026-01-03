namespace Newbe.BetterModLoader;

using HarmonyLib;

/// <summary>
/// Patch ModManager.ShouldActivateMod 使其始终返回 false，交由 BetterModLoader 控制激活逻辑。
/// </summary>
[HarmonyPatch(typeof(ModManager))]
public static class InterceptShouldActivateModPatch
{
    private static int _callCount = 0;

    [HarmonyPatch("ShouldActivateMod")]
    [HarmonyPostfix]
    public static void ShouldActivateModPostfix(ref bool __result)
    {
        __result = false;
        if (_callCount == 0)
        {
            Log.Debug("[BML] Intercepted ModManager.ShouldActivateMod, forcibly returning false.");
            _callCount++;
        }
    }
}
