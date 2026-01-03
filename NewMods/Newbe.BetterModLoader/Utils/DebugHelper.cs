namespace Newbe.BetterModLoader.Utils;

internal static class DebugHelper
{
    /// <summary>
    /// 递归输出指定 GameObject 及其所有子对象的布局信息
    /// </summary>
    internal static void LogHierarchyLayoutInfo(GameObject root, int depth = 0)
    {
        if (root == null) return;

        string indent = new string(' ', depth * 2);
        var rectTransform = root.GetComponent<RectTransform>();

        try
        {
            Log.Debug($"{indent}[{root.name}] ===========================");

            // 基本信息
            Log.Debug($"{indent}  Active: {root.activeInHierarchy}, Enabled: {root.activeSelf}");

            // RectTransform 信息
            if (rectTransform != null)
            {
                Log.Debug($"{indent}  RectTransform:");
                Log.Debug($"{indent}    - anchorMin: {rectTransform.anchorMin}, anchorMax: {rectTransform.anchorMax}");
                Log.Debug($"{indent}    - anchoredPosition: {rectTransform.anchoredPosition}");
                Log.Debug($"{indent}    - sizeDelta: {rectTransform.sizeDelta}");
                Log.Debug($"{indent}    - rect: {rectTransform.rect}");
                Log.Debug($"{indent}    - pivot: {rectTransform.pivot}");
            }

            // LayoutElement 信息
            var layoutElement = root.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                Log.Debug($"{indent}  LayoutElement:");
                Log.Debug($"{indent}    - minWidth: {layoutElement.minWidth}, minHeight: {layoutElement.minHeight}");
                Log.Current.Debug(
                    $"{indent}    - preferredWidth: {layoutElement.preferredWidth}, preferredHeight: {layoutElement.preferredHeight}");
                Log.Current.Debug(
                    $"{indent}    - flexibleWidth: {layoutElement.flexibleWidth}, flexibleHeight: {layoutElement.flexibleHeight}");
                Log.Debug($"{indent}    - ignoreLayout: {layoutElement.ignoreLayout}");
            }

            // HorizontalLayoutGroup 信息
            var hLayoutGroup = root.GetComponent<HorizontalLayoutGroup>();
            if (hLayoutGroup != null)
            {
                Log.Debug($"{indent}  HorizontalLayoutGroup:");
                Log.Debug($"{indent}    - spacing: {hLayoutGroup.spacing}");
                Log.Debug($"{indent}    - childAlignment: {hLayoutGroup.childAlignment}");
                Log.Current.Debug(
                    $"{indent}    - childControlWidth: {hLayoutGroup.childControlWidth}, childControlHeight: {hLayoutGroup.childControlHeight}");
                Log.Current.Debug(
                    $"{indent}    - childForceExpandWidth: {hLayoutGroup.childForceExpandWidth}, childForceExpandHeight: {hLayoutGroup.childForceExpandHeight}");
                Log.Current.Debug(
                    $"{indent}    - padding: L={hLayoutGroup.padding.left}, R={hLayoutGroup.padding.right}, T={hLayoutGroup.padding.top}, B={hLayoutGroup.padding.bottom}");
            }

            // VerticalLayoutGroup 信息
            var vLayoutGroup = root.GetComponent<VerticalLayoutGroup>();
            if (vLayoutGroup != null)
            {
                Log.Debug($"{indent}  VerticalLayoutGroup:");
                Log.Debug($"{indent}    - spacing: {vLayoutGroup.spacing}");
                Log.Debug($"{indent}    - childAlignment: {vLayoutGroup.childAlignment}");
                Log.Current.Debug(
                    $"{indent}    - childControlWidth: {vLayoutGroup.childControlWidth}, childControlHeight: {vLayoutGroup.childControlHeight}");
                Log.Current.Debug(
                    $"{indent}    - childForceExpandWidth: {vLayoutGroup.childForceExpandWidth}, childForceExpandHeight: {vLayoutGroup.childForceExpandHeight}");
                Log.Current.Debug(
                    $"{indent}    - padding: L={vLayoutGroup.padding.left}, R={vLayoutGroup.padding.right}, T={vLayoutGroup.padding.top}, B={vLayoutGroup.padding.bottom}");
            }

            // ContentSizeFitter 信息
            var sizeFitter = root.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
            {
                Log.Debug($"{indent}  ContentSizeFitter:");
                Log.Current.Debug(
                    $"{indent}    - horizontalFit: {sizeFitter.horizontalFit}, verticalFit: {sizeFitter.verticalFit}");
            }

            // ScrollRect 信息
            var scrollRect = root.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                Log.Debug($"{indent}  ScrollRect:");
                Log.Debug($"{indent}    - content: {(scrollRect.content != null ? scrollRect.content.name : "null")}");
                Log.Current.Debug(
                    $"{indent}    - viewport: {(scrollRect.viewport != null ? scrollRect.viewport.name : "null")}");
                Log.Debug($"{indent}    - horizontal: {scrollRect.horizontal}, vertical: {scrollRect.vertical}");
            }

            // TextMeshProUGUI 信息
            var tmpText = root.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                string preview;
                try
                {
                    var t = tmpText.text ?? string.Empty;
                    preview = t.Length <= 50 ? t : t.Substring(0, 50) + "...";
                }
                catch
                {
                    preview = "<text preview unavailable>";
                }

                Log.Debug($"{indent}  TextMeshProUGUI:");
                Log.Debug($"{indent}    - text: \"{preview}\"");
                Log.Debug($"{indent}    - fontSize: {tmpText.fontSize}, color: {tmpText.color}");
                Log.Debug($"{indent}    - alignment: {tmpText.alignment}");
                Log.Debug($"{indent}    - enabled: {tmpText.enabled}");
                Log.Current.Debug(
                    $"{indent}    - preferredWidth: {tmpText.preferredWidth}, preferredHeight: {tmpText.preferredHeight}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{indent}[LogHierarchyLayoutInfo] Exception: {ex.GetType().Name}: {ex.Message}");
        }

        // 递归处理所有子对象
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i).gameObject;
            LogHierarchyLayoutInfo(child, depth + 1);
        }
    }
}
