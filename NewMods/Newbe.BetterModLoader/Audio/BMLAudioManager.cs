namespace Newbe.BetterModLoader.Audio;

/// <summary>
/// BML 音效类型枚举
/// </summary>
public enum AudioEffectType
{
    /// <summary>
    /// 开关启用音效
    /// </summary>
    ToggleOn,

    /// <summary>
    /// 开关禁用音效
    /// </summary>
    ToggleOff,

    /// <summary>
    /// 按钮点击音效
    /// </summary>
    ButtonClick,

    /// <summary>
    /// 拖拽开始音效
    /// </summary>
    DragStart,

    /// <summary>
    /// 拖拽释放音效
    /// </summary>
    DragRelease
}

/// <summary>
/// BML 音效管理器 - 与游戏原生 AudioManager 集成
/// 使用 FMOD 系统播放音频，统一音量控制
/// </summary>
public class BMLAudioManager : MonoBehaviour
{
    public static BMLAudioManager Instance { get; private set; } = null!;

    private readonly Dictionary<AudioEffectType, string> _audioFilePaths = new()
    {
        // { AudioEffectType.ToggleOn, "switch-a.ogg" },
        // { AudioEffectType.ToggleOff, "switch-b.ogg" },
        // { AudioEffectType.ButtonClick, "click-a.ogg" },
        // { AudioEffectType.DragStart, "tap-a.ogg" },
        // { AudioEffectType.DragRelease, "tap-b.ogg" },
        { AudioEffectType.ToggleOn, "092_Pause_04.wav" },
        { AudioEffectType.ToggleOff, "098_Unpause_04.wav" },
        { AudioEffectType.ButtonClick, "051_use_item_01.wav" },
        { AudioEffectType.DragStart, "071_Unequip_01.wav" },
        { AudioEffectType.DragRelease, "070_Equip_10.wav" }
    };

    private readonly Dictionary<AudioEffectType, string> _cachedFilePaths = new();
    private readonly HashSet<AudioEffectType> _enabledEffects = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化音频系统，缓存文件路径
    /// </summary>
    private void Initialize()
    {
        Log.Info("[BMLAudioManager] Initializing BML Audio System using game's AudioManager");

        if (!Duckov.AudioManager.Initialized)
        {
            Log.Warn("[BMLAudioManager] Game AudioManager not yet initialized, will try later");
        }
        else
        {
            Log.Info("[BMLAudioManager] Game AudioManager is ready");
        }

        // 预计算并缓存所有音频文件的物理路径
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var resourcesPath = Path.Combine(assemblyDirectory!, "Resources", "Sounds");

        var missingFiles = new List<string>();
        foreach (var kvp in _audioFilePaths)
        {
            var filePath = Path.Combine(resourcesPath, kvp.Value);
            _cachedFilePaths[kvp.Key] = filePath;

            if (!File.Exists(filePath))
            {
                missingFiles.Add(filePath);
            }
        }

        if (missingFiles.Count > 0)
        {
            Log.Error($"[BMLAudioManager] Missing audio files: {string.Join(", ", missingFiles)}");
        }
        else
        {
            Log.Info("[BMLAudioManager] All audio files found and ready");
        }

        // 初始化时就设定好哪些音效类型允许播放
        InitializeEnabledEffects();
    }

    /// <summary>
    /// 初始化启用的音效类型
    /// </summary>
    private void InitializeEnabledEffects()
    {
        foreach (AudioEffectType effectType in Enum.GetValues(typeof(AudioEffectType)))
        {
            if (Configs.ShouldPlayAudio(effectType))
            {
                _enabledEffects.Add(effectType);
                Log.Debug($"[BMLAudioManager] Enabled audio effect: {effectType}");
            }
        }

        Log.Info($"[BMLAudioManager] Initialized with {_enabledEffects.Count} enabled audio effects");
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="effectType">音效类型</param>
    public void PlaySound(AudioEffectType effectType)
    {
        // 使用初始化时缓存的启用状态检查，不再动态查询配置
        if (!_enabledEffects.Contains(effectType))
        {
            Log.Debug($"[BMLAudioManager] Effect type {effectType} not enabled during initialization");
            return;
        }

        // 检查游戏音频系统是否初始化
        if (!Duckov.AudioManager.Initialized)
        {
            Log.Warn("[BMLAudioManager] Game AudioManager not initialized, skipping audio playback");
            return;
        }

        // 获取缓存的文件路径
        if (!_cachedFilePaths.TryGetValue(effectType, out var filePath))
        {
            Log.Error($"[BMLAudioManager] No cached file path for effect type: {effectType}");
            return;
        }

        try
        {
            // 使用游戏原生 AudioManager 播放自定义音效
            // 传递 gameObject 以便 FMOD 正确处理音频位置
            UniTask.RunOnThreadPool(() =>
                Duckov.AudioManager.PostCustomSFX(filePath, gameObject: null, loop: false)).Forget();
        }
        catch (Exception e)
        {
            Log.Error($"[BMLAudioManager] Exception playing audio {_audioFilePaths[effectType]}: {e.Message}");
        }
    }

    /// <summary>
    /// 静态播放方法，保持向后兼容性
    /// </summary>
    /// <param name="effectType">音效类型</param>
    public static void PlaySoundStatic(AudioEffectType effectType)
    {
        if (Instance != null)
        {
            Instance.PlaySound(effectType);
        }
        else
        {
            Log.Warn("[BMLAudioManager] Instance not available for static play call");
        }
    }

    /// <summary>
    /// 获取已加载的音频剪辑数量
    /// </summary>
    public int LoadedClipCount => _audioFilePaths.Count;

    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public bool IsInitialized => Duckov.AudioManager.Initialized && _cachedFilePaths.Count > 0;
}
