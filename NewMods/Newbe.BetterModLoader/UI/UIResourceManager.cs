using System.Reflection;
using Object = UnityEngine.Object;

namespace Newbe.BetterModLoader.UI;

/// <summary>
/// UI资源管理器 - 负责加载和缓存UI相关的PNG资源
/// </summary>
public static class UIResourceManager
{
    private static readonly Dictionary<string, Sprite> _spriteCache = new();
    private static bool _isInitialized = false;
    private static readonly object _lockObject = new object();
    private static Assembly _executingAssembly;
    private static string _resourcesFolderPath;

    /// <summary>
    /// 初始化资源管理器
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        lock (_lockObject)
        {
            if (_isInitialized) return;

            try
            {
                // 获取当前程序集
                _executingAssembly = Assembly.GetExecutingAssembly();

                // 获取程序集所在位置并设置Resources文件夹路径
                var assemblyLocation = _executingAssembly.Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                _resourcesFolderPath = Path.Combine(assemblyDirectory!, "Resources");

                // 在调试模式下输出诊断信息
#if DEBUG
                Log.Debug($"[UIResourceManager] Assembly location: {assemblyLocation}");
                Log.Debug($"[UIResourceManager] Resources folder path: {_resourcesFolderPath}");
                DiagnoseResourceLoading();
#endif

                LoadUIResources();
                _isInitialized = true;
                Log.Info($"[UIResourceManager] Initialized with {_spriteCache.Count} resources loaded successfully");

                if (_spriteCache.Count == 0)
                {
                    Log.Warn(
                        "[UIResourceManager] No resources were loaded! Check resource paths and folder configuration.");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[UIResourceManager] Failed to initialize: {e.Message}");
                Log.Error($"[UIResourceManager] Stack trace: {e.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 获取精灵资源
    /// </summary>
    /// <param name="resourceKey">资源键名</param>
    /// <returns>精灵对象，如果未找到则返回null</returns>
    public static Sprite GetSprite(string resourceKey)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        _spriteCache.TryGetValue(resourceKey, out var sprite);
        return sprite;
    }

    /// <summary>
    /// 获取已加载资源数量
    /// </summary>
    public static int LoadedResourceCount => _spriteCache.Count;

    /// <summary>
    /// 清理资源缓存
    /// </summary>
    public static void ClearCache()
    {
        lock (_lockObject)
        {
            foreach (var kvp in _spriteCache)
            {
                if (kvp.Value != null)
                {
                    Object.DestroyImmediate(kvp.Value);
                }
            }

            _spriteCache.Clear();
            _isInitialized = false;
            Log.Debug("[UIResourceManager] Cache cleared");
        }
    }

    /// <summary>
    /// 重新加载所有资源
    /// </summary>
    public static void ReloadResources()
    {
        ClearCache();
        Initialize();
    }

    /// <summary>
    /// 加载UI资源
    /// </summary>
    private static void LoadUIResources()
    {
        // 按钮资源
        LoadResource("close_normal", "PNG/Red/Default/button_round_depth_gradient.png");
        LoadResource("close_hover", "PNG/Red/Default/button_round_gloss.png");
        LoadResource("logs_normal", "PNG/Blue/Default/button_rectangle_depth_gradient.png");
        LoadResource("logs_hover", "PNG/Blue/Default/button_rectangle_gloss.png");
        LoadResource("success_bg", "PNG/Green/Default/button_rectangle_gradient.png");
        LoadResource("warning_bg", "PNG/Yellow/Default/button_rectangle_gradient.png");
        LoadResource("error_bg", "PNG/Red/Default/button_rectangle_gradient.png");

        // 图标资源
        LoadResource("check_success", "PNG/Blue/Default/check_round_color.png");
        LoadResource("cross_error", "PNG/Blue/Default/icon_cross.png");
        LoadResource("star_best", "PNG/Blue/Default/star.png");
        LoadResource("circle_info", "PNG/Blue/Default/icon_circle.png");
        LoadResource("square_bg", "PNG/Blue/Default/icon_square.png");

        // 箭头资源
        LoadResource("arrow_up", "PNG/Blue/Default/arrow_basic_n.png");
        LoadResource("arrow_down", "PNG/Blue/Default/arrow_basic_s.png");
        LoadResource("arrow_left", "PNG/Blue/Default/arrow_basic_w.png");
        LoadResource("arrow_right", "PNG/Blue/Default/arrow_basic_e.png");

        // 状态图标
        LoadResource("checkmark", "PNG/Blue/Default/icon_checkmark.png");
        LoadResource("outline_circle", "PNG/Blue/Default/icon_outline_circle.png");
        LoadResource("outline_square", "PNG/Blue/Default/icon_outline_square.png");

        // 其他装饰性图标
        LoadResource("star_outline", "PNG/Blue/Default/star_outline.png");
        LoadResource("star_depth", "PNG/Blue/Default/star_outline_depth.png");
    }

    /// <summary>
    /// 从文件系统加载PNG文件并创建精灵
    /// </summary>
    /// <param name="key">资源键名</param>
    /// <param name="relativePath">Resources文件夹中的相对路径</param>
    private static void LoadResource(string key, string relativePath)
    {
        try
        {
            if (string.IsNullOrEmpty(_resourcesFolderPath))
            {
                Log.Error("[UIResourceManager] Resources folder path not initialized");
                return;
            }

            // 构建完整的文件路径
            var fullPath = Path.Combine(_resourcesFolderPath, relativePath);

            if (File.Exists(fullPath))
            {
                // 读取PNG文件的字节数据
                var bytes = File.ReadAllBytes(fullPath);

                // 创建Texture2D
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    // 创建Sprite
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    _spriteCache[key] = sprite;

                    Log.Debug(
                        $"[UIResourceManager] Loaded resource: {relativePath} as {key} ({tex.width}x{tex.height})");
                }
                else
                {
                    Log.Warn($"[UIResourceManager] Failed to load image data from {relativePath}");
                    Object.Destroy(tex);
                }
            }
            else
            {
                Log.Warn($"[UIResourceManager] Resource file not found: {fullPath}");
            }
        }
        catch (Exception e)
        {
            Log.Error($"[UIResourceManager] Failed to load {relativePath}: {e.Message}");
        }
    }

    /// <summary>
    /// 诊断资源加载问题
    /// </summary>
    private static void DiagnoseResourceLoading()
    {
        try
        {
            if (string.IsNullOrEmpty(_resourcesFolderPath))
            {
                Log.Error("[UIResourceManager] Resources folder path is null or empty");
                return;
            }

            if (!Directory.Exists(_resourcesFolderPath))
            {
                Log.Error($"[UIResourceManager] Resources folder does not exist: {_resourcesFolderPath}");
                return;
            }

            // 检查PNG文件夹结构
            var pngPath = Path.Combine(_resourcesFolderPath, "PNG");
            if (Directory.Exists(pngPath))
            {
                Log.Debug($"[UIResourceManager] PNG folder found: {pngPath}");
                var colorFolders = Directory.GetDirectories(pngPath);
                Log.Debug(
                    $"[UIResourceManager] Found {colorFolders.Length} color folders: {string.Join(", ", colorFolders.Select(Path.GetFileName))}");
            }
            else
            {
                Log.Error($"[UIResourceManager] PNG folder not found: {pngPath}");
            }
        }
        catch (Exception e)
        {
            Log.Error($"[UIResourceManager] DiagnoseResourceLoading error: {e.Message}");
        }
    }
}
