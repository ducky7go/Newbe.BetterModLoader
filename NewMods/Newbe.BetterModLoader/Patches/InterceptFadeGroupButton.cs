using Duckov.UI.Animations;
using HarmonyLib;
using Newbe.BetterModLoader.Main;

namespace Newbe.BetterModLoader;

/// <summary>
/// 拦截 FadeGroupButton 的 Execute 方法：
/// - 当名为 "Mods" 的按钮尝试打开 ModManagerUI 时进行拦截
/// - 显示自定义的 BetterModManagerCanvas 而不是原始的 ModManagerUI
/// </summary>
[HarmonyPatch(typeof(FadeGroupButton))]
internal static class InterceptFadeGroupButton
{
    [HarmonyPatch("Execute")]
    [HarmonyPrefix]
    public static bool Execute_Prefix(FadeGroupButton __instance, FadeGroup ___openOnClick)
    {
        try
        {
            if (__instance == null) return true;

            var go = __instance.gameObject;

            // 检查是否是名为 "Mods" 的按钮
            if (string.Equals(go.name, "Mods", StringComparison.OrdinalIgnoreCase))
            {
                // 检查 openOnClick 的 GameObject 名称是否为 "ModManagerUI"
                if (___openOnClick != null &&
                    string.Equals(___openOnClick.gameObject.name, "ModManagerUI", StringComparison.OrdinalIgnoreCase))
                {
                    // 这是 Mods 按钮尝试打开 ModManagerUI
                    // 显示我们的自定义界面
                    Call();

                    // 返回 false 跳过原始 Execute 方法
                    return false;
                }
            }

            // 其他按钮继续执行原逻辑
            return true;
        }
        catch (Exception ex)
        {
            Log.DebugException("[BetterModLoader] Exception in InterceptButtonPatch.Execute_Prefix", ex);
            return true; // 出错时不阻止原逻辑
        }
    }

    private static void Call()
    {
        try
        {
            BmlModManagerUIView.Show();
            Log.Current.Debug(
                "[BetterModLoader] ✓ Intercepted Mods FadeGroupButton -> BetterModManagerCanvas shown");
        }
        catch (Exception exInner)
        {
            Log.Current.DebugException(
                "[BetterModLoader] Error showing BetterModManagerCanvas in Execute patch: ", exInner);
        }
    }
}
