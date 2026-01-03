using System.Diagnostics;
using System.Runtime.InteropServices;
using Steamworks;

/// <summary>
/// WebOpener configuration for opening strategy preferences
/// </summary>
public static class WebOpenerConfig
{
    /// <summary>
    /// Opening mode preference (only affects Windows HTTP/HTTPS URLs)
    /// </summary>
    public static WebOpenerMode Mode { get; set; } = WebOpenerMode.Auto;
}

/// <summary>
/// Web opening mode options
/// </summary>
public enum WebOpenerMode
{
    /// <summary>
    /// Try Steam overlay first, then fallback to external browser (Windows only)
    /// </summary>
    Auto,

    /// <summary>
    /// Only use Steam overlay (Windows only)
    /// </summary>
    SteamOnly,

    /// <summary>
    /// Always use external browser (Windows only)
    /// </summary>
    External
}

/// <summary>
/// 跨平台网页URL打开工具类，使用系统默认浏览器独立打开URL
/// Windows 平台有用户反馈使用 Application.OpenURL 会导致 Steam 误判浏览器为游戏一部分
/// </summary>
public static class WebOpener
{
    private static bool? _isSteamAvailable;

    /// <summary>
    /// 规范化文件协议URL，将Windows路径的反斜杠替换为正斜杠
    /// </summary>
    /// <param name="url">要规范化的URL</param>
    /// <returns>规范后的URL</returns>
    private static string NormalizeFileProtocolUrl(string url)
    {
        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            // 对于文件协议，将所有反斜杠替换为正斜杠以确保正确的URL格式
            return url.Replace('\\', '/');
        }

        // 非文件协议的URL保持不变
        return url;
    }

    /// <summary>
    /// 检测是否为文件协议URL
    /// </summary>
    /// <param name="url">要检查的URL</param>
    /// <returns>如果是文件协议返回true</returns>
    private static bool IsFileProtocol(string url)
    {
        return url.StartsWith("file://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 跨平台打开URL
    /// </summary>
    /// <param name="url">要打开的URL</param>
    /// <exception cref="ArgumentException">当URL为null或空时抛出</exception>
    /// <exception cref="PlatformNotSupportedException">当当前平台不支持时抛出</exception>
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Error("[WebOpener] URL cannot be null or empty");
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        try
        {
            var normalizedUrl = NormalizeFileProtocolUrl(url);
            Log.Info($"[WebOpener] Opening URL: {normalizedUrl}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (IsFileProtocol(normalizedUrl))
                {
                    // 文件协议：总是使用外部浏览器（Steam overlay 不支持）
                    OpenUrlWindows(normalizedUrl);
                }
                else
                {
                    // HTTP/HTTPS：根据配置选择策略
                    var opened = TryOpenWithWindowsStrategy(normalizedUrl);
                    if (!opened)
                    {
                        // 所有策略都失败，使用最后的回退
                        Log.Warn("[WebOpener] All strategies failed, falling back to process isolation");
                        OpenUrlWindows(normalizedUrl);
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS：使用进程隔离
                OpenUrlMacOS(normalizedUrl);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux：使用进程隔离
                OpenUrlLinux(normalizedUrl);
            }
            else
            {
                var platform = RuntimeInformation.OSDescription;
                Log.Error($"[WebOpener] Unsupported platform: {platform}");
                throw new PlatformNotSupportedException($"Platform {platform} is not supported");
            }

            Log.Info($"[WebOpener] Successfully opened URL: {url}");
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is PlatformNotSupportedException))
        {
            Log.Error(ex, $"[WebOpener] Failed to open URL: {url}");
            throw;
        }
    }

    /// <summary>
    /// Windows 平台的策略性打开
    /// </summary>
    /// <param name="url">要打开的URL</param>
    /// <returns>是否成功打开</returns>
    private static bool TryOpenWithWindowsStrategy(string url)
    {
        var mode = WebOpenerConfig.Mode;

        switch (mode)
        {
            case WebOpenerMode.SteamOnly:
                return TryOpenWithSteamOverlay(url);

            case WebOpenerMode.External:
                OpenUrlWindows(url);
                return true;

            case WebOpenerMode.Auto:
            default:
                // 先尝试 Steam overlay，失败则使用外部浏览器
                if (IsSteamAvailable() && TryOpenWithSteamOverlay(url))
                {
                    return true;
                }
                OpenUrlWindows(url);
                return true;
        }
    }

    /// <summary>
    /// 检测 Steam 是否可用
    /// </summary>
    /// <returns>Steam 是否可用</returns>
    private static bool IsSteamAvailable()
    {
        if (_isSteamAvailable.HasValue)
        {
            return _isSteamAvailable.Value;
        }

        // 直接访问 SteamManager 的公共属性
        _isSteamAvailable = SteamManager.Initialized;
        Log.Debug($"[WebOpener] SteamManager.Initialized: {_isSteamAvailable.Value}");
        return _isSteamAvailable.Value;
    }

    /// <summary>
    /// 尝试使用 Steam overlay 打开 URL
    /// </summary>
    /// <param name="url">要打开的URL</param>
    /// <returns>是否成功打开</returns>
    private static bool TryOpenWithSteamOverlay(string url)
    {
        try
        {
            // 直接调用 SteamFriends.ActivateGameOverlayToWebPage
            SteamFriends.ActivateGameOverlayToWebPage(url);
            Log.Info($"[WebOpener] Opened URL with Steam overlay: {url}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, $"[WebOpener] Failed to open URL with Steam overlay: {url}");
            return false;
        }
    }

    /// <summary>
    /// Windows平台打开URL（使用系统命令确保进程独立）
    /// </summary>
    private static void OpenUrlWindows(string url)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c start \"\" \"{url}\"",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start cmd.exe process");
        }

        Log.Info($"[WebOpener] Opened URL with Windows process isolation: {url}");
    }

    /// <summary>
    /// macOS平台打开URL（使用 open -g 确保进程独立）
    /// </summary>
    private static void OpenUrlMacOS(string url)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-g \"{url}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start open process on macOS");
            }

            Log.Info($"[WebOpener] Opened URL with macOS process isolation: {url}");
        }
        catch
        {
            Log.Warn("[WebOpener] Failed to use open command, falling back to Application.OpenURL");
            Application.OpenURL(url);
        }
    }

    /// <summary>
    /// Linux平台打开URL（使用 xdg-open 确保进程独立）
    /// </summary>
    private static void OpenUrlLinux(string url)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{url}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start xdg-open process on Linux");
            }

            Log.Info($"[WebOpener] Opened URL with Linux process isolation: {url}");
        }
        catch
        {
            Log.Warn("[WebOpener] Failed to use xdg-open, falling back to Application.OpenURL");
            Application.OpenURL(url);
        }
    }
}
