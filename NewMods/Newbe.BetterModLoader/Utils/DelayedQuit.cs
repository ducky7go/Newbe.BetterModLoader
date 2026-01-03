namespace Newbe.BetterModLoader.Utils;

/// <summary>
/// 延迟指定秒数后退出应用程序的帮助组件
/// </summary>
internal class DelayedQuit : MonoBehaviour
{
    public float DelaySeconds = 2f;
    private float _elapsed;

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed >= DelaySeconds)
        {
            Log.Debug("[BetterModLoader] Quitting application after delay...");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
