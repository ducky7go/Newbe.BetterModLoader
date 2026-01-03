namespace Newbe.BetterModLoader.UI;

/// <summary>
/// Toast 提示管理器 - 在屏幕中央显示临时提示信息
/// </summary>
internal static class ToastManager
{
    /// <summary>
    /// 显示成功提示（绿色背景）
    /// </summary>
    public static void ShowSuccess(string message, float duration = 3f)
    {
        ShowWithColor(message, new Color(0.2f, 0.6f, 0.2f, 0.9f), duration);
    }

    /// <summary>
    /// 显示错误提示（红色背景）
    /// </summary>
    public static void ShowError(string message, float duration = 3f)
    {
        ShowWithColor(message, new Color(0.8f, 0.2f, 0.2f, 0.9f), duration);
    }

    /// <summary>
    /// 显示警告提示（黄色背景）
    /// </summary>
    public static void ShowWarning(string message, float duration = 3f)
    {
        ShowWithColor(message, new Color(0.8f, 0.6f, 0.1f, 0.9f), duration);
    }

    /// <summary>
    /// 显示自定义颜色的 Toast
    /// </summary>
    private static void ShowWithColor(string message, Color backgroundColor, float duration = 3f, int fontSize = 32)
    {
        try
        {
            var toast = new GameObject("[BML_Toast]");
            var canvas = toast.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20000;
            canvas.overrideSorting = true;

            var canvasScaler = toast.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            toast.AddComponent<GraphicRaycaster>();
            UnityEngine.Object.DontDestroyOnLoad(toast);

            var bg = CreateUIObject("Background", toast.transform);
            var img = bg.AddComponent<Image>();
            img.color = backgroundColor;

            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(900, 120);

            var textObj = CreateUIObject("Text", bg.transform);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 20);
            textRect.offsetMax = new Vector2(-20, -20);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = message;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            Log.Debug($"[BetterModLoader] Toast shown: {message}");

            var autoDestroy = toast.AddComponent<AutoDestroy>();
            autoDestroy.Lifetime = duration;
        }
        catch (Exception ex)
        {
            Log.Warn($"[BetterModLoader] Failed to show toast: {ex.Message}");
        }
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    /// <summary>
    /// 自动销毁组件
    /// </summary>
    private class AutoDestroy : MonoBehaviour
    {
        public float Lifetime = 3f;
        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
