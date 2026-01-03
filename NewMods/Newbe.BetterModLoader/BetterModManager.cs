using System.Diagnostics;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading;

namespace Newbe.BetterModLoader;

/// <summary>
/// A drop-in replacement manager that intercepts all ModManager operations via Harmony patches
/// and executes custom logic while preserving ModManager events for compatibility.
/// </summary>
internal class BetterModManager : MonoBehaviour
{
    private static BetterModManager? _instance;

    #region Lifecycle events

    /// <summary>
    /// 当所有模组任务（启用/禁用）全部执行结束时触发。
    /// 参数为本次执行的所有任务及其结果。
    /// </summary>
    public static event Action<IReadOnlyList<ModOperationResult>>? AllModJobsCompleted;

    /// <summary>
    /// 表示一次模组任务的结果。
    /// </summary>
    public readonly struct ModOperationResult
    {
        public readonly ModOperation Job;
        public readonly bool Success;
        public readonly Exception? Exception;
        public readonly double DurationMs;

        public ModOperationResult(ModOperation job, bool success, Exception? exception, double durationMs)
        {
            Job = job;
            Success = success;
            Exception = exception;
            DurationMs = durationMs;
        }
    }

    /// <summary>
    /// 在尝试启用某个模组之前触发（静态事件）。
    /// </summary>
    public static event Action<ModInfo2>? ModEnabling;

    /// <summary>
    /// 在尝试启用某个模组之后触发（静态事件）。包含启用结果与失败原因（如有）。
    /// </summary>
    public static event Action<ModEnableCompletedEventArgs>? ModEnabled;

    /// <summary>
    /// 在禁用某个模组之前触发（静态事件）。
    /// </summary>
    public static event Action<ModInfo2>? ModDisabling;

    /// <summary>
    /// 在禁用某个模组之后触发（静态事件）。
    /// </summary>
    public static event Action<ModDisableCompletedEventArgs>? ModDisabled;

    /// <summary>
    /// 启用结果（是否顺利）。
    /// </summary>
    public enum ModEnableOutcome
    {
        /// <summary>
        /// 启用成功。
        /// </summary>
        Success = 0,

        /// <summary>
        /// 启用失败。
        /// </summary>
        Failed = 1,
    }

    /// <summary>
    /// 启用失败原因。
    /// </summary>
    public enum ModEnableFailureReason
    {
        /// <summary>
        /// 未知原因。
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 用户未允许启用模组。
        /// </summary>
        NotAllowed,

        /// <summary>
        /// 该模组已处于激活状态。
        /// </summary>
        AlreadyActive,

        /// <summary>
        /// 程序集加载失败（例如文件损坏或依赖缺失）。
        /// </summary>
        LoadAssemblyFailed,

        /// <summary>
        /// 未找到期望的 ModBehaviour 类型。
        /// </summary>
        TypeNotFound,

        /// <summary>
        /// 找到的类型并未继承自 Duckov.Modding.ModBehaviour。
        /// </summary>
        InvalidBehaviourType,

        /// <summary>
        /// AddComponent 过程中创建组件失败。
        /// </summary>
        CreateComponentFailed,

        /// <summary>
        ///  ModBehaviour.Setup 过程中发生异常。
        /// </summary>
        SetupFailed,
    }

    /// <summary>
    /// 禁用结果（是否顺利）。
    /// </summary>
    public enum ModDisableOutcome
    {
        /// <summary>
        /// 禁用成功。
        /// </summary>
        Success = 0,

        /// <summary>
        /// 禁用失败（例如模组本身就未激活）。
        /// </summary>
        Failed = 1,
    }

    /// <summary>
    /// 禁用失败原因。
    /// </summary>
    public enum ModDisableFailureReason
    {
        /// <summary>
        /// 未知原因。
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 该模组未处于激活状态，无需禁用。
        /// </summary>
        NotActive,

        /// <summary>
        /// 禁用过程中发生异常。
        /// </summary>
        Exception,
    }

    /// <summary>
    /// 模组禁用完成事件参数。
    /// </summary>
    public sealed class ModDisableCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 模组信息。
        /// </summary>
        public ModInfo2 Info { get; }

        /// <summary>
        /// 禁用结果。
        /// </summary>
        public ModDisableOutcome Outcome { get; }

        /// <summary>
        /// 失败原因（仅当 Outcome 为 Failed 时有效）。
        /// </summary>
        public ModDisableFailureReason? FailureReason { get; }

        /// <summary>
        /// 可选的错误消息。
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// 可选的异常。
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// 持续时间（毫秒）。
        /// </summary>
        public double DurationMs { get; }

        public ModDisableCompletedEventArgs(
            ModInfo2 info,
            ModDisableOutcome outcome,
            ModDisableFailureReason? failureReason = null,
            string? errorMessage = null,
            Exception? exception = null,
            double durationMs = 0)
        {
            Info = info;
            Outcome = outcome;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
            Exception = exception;
            DurationMs = durationMs;
        }
    }

    /// <summary>
    /// 模组启用完成事件参数。
    /// </summary>
    public sealed class ModEnableCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 模组信息（分组包装）。
        /// </summary>
        public ModInfo2 Info { get; }

        /// <summary>
        /// 已创建的行为实例；失败时为 null。
        /// </summary>
        public Duckov.Modding.ModBehaviour? Behaviour { get; }

        /// <summary>
        /// 启用结果。
        /// </summary>
        public ModEnableOutcome Outcome { get; }

        /// <summary>
        /// 失败原因（仅当 Outcome 为 Failed 时有效）。
        /// </summary>
        public ModEnableFailureReason? FailureReason { get; }

        /// <summary>
        /// 可选的错误消息。
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// 可选的异常。
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// 持续时间（毫秒）。
        /// </summary>
        public double DurationMs { get; }

        public ModEnableCompletedEventArgs(
            ModInfo2 info,
            Duckov.Modding.ModBehaviour? behaviour,
            ModEnableOutcome outcome,
            ModEnableFailureReason? failureReason = null,
            string? errorMessage = null,
            Exception? exception = null,
            double durationMs = 0)
        {
            Info = info;
            Behaviour = behaviour;
            Outcome = outcome;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
            Exception = exception;
            DurationMs = durationMs;
        }
    }

    private static void RaiseModEnabling(ModInfo2 info)
    {
        try
        {
            ModEnabling?.Invoke(info);
        }
        catch (Exception ex)
        {
            Log.DebugException("failed to raise ModEnabling event", ex);
        }
    }

    private static void RaiseModEnabled(ModEnableCompletedEventArgs args)
    {
        try
        {
            ModEnabled?.Invoke(args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred");
        }
    }

    private static void RaiseModDisabling(ModInfo2 info)
    {
        try
        {
            ModDisabling?.Invoke(info);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred");
        }
    }

    private static void RaiseModDisabled(ModDisableCompletedEventArgs args)
    {
        try
        {
            ModDisabled?.Invoke(args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred");
        }
    }

    #endregion

    /// <summary>
    /// 已激活的模组字典，使用 ModId 作为键
    /// </summary>
    private readonly Dictionary<string, Duckov.Modding.ModBehaviour> _activeMods = new();

    // ================= 串行队列与状态字段（并发队列模式） =================
    // 使用线程安全的并发队列和信号量机制替代 Channel
    private readonly ConcurrentQueue<ModOperation> _operationQueue = new();
    private readonly SemaphoreSlim _queueSignal = new(0, int.MaxValue);
    private volatile bool _isProcessing;
    private UniTaskCompletionSource? _processingTask;
    private readonly CancellationTokenSource _queueCts = new();

    // 会话与队列控制
    private bool _queueInitialized;
    private bool _isBroadcasting;
    private int _pendingOps;
    private Guid _currentSessionId;
    private Stopwatch? _sessionStopwatch;

    // 当前会话结果收集
    private List<ModOperationResult>? _currentSessionResults;

    // 防重复激活/禁用集（幂等保护）
    private readonly HashSet<string> _activating = [];
    private readonly HashSet<string> _deactivating = [];

    // 已启用标记（幂等防重复启用）
    private readonly HashSet<string> _enabledMods = [];

    // 程序集 + 类型缓存，避免重复 Assembly.LoadFrom
    private readonly Dictionary<string, (Assembly Asm, Type? ModType)> _assemblyCache = new();

    // 应用退出标记：用于诊断外部销毁/退出导致的队列中断
    private bool _appQuitting;

    /// <summary>
    /// 模组分组配置（使用单例实例）
    /// </summary>
    public static ModGroupConfig GroupConfig => ModGroupConfig.Instance;

    public static BetterModManager Instance
    {
        get
        {
            // 若已有存活实例则直接返回（Unity 对象可能被标记销毁但引用非 null）
            if (_instance != null && _instance)
            {
                return _instance;
            }

            EnsureCreated();
            return _instance!;
        }
    }

    // 单例存活状态快速判断：用于外部入口避免重复创建或访问失效实例
    internal static bool IsAlive => _instance != null && _instance;

    public static void EnsureCreated()
    {
        // 始终使用“专用根级宿主”承载，避免被 ModBehaviour/场景切换等外部对象销毁
        if (_instance != null && _instance)
        {
            return;
        }

        var host = new GameObject("[BML] BetterModManagerHost");
        // 可选：避免被保存到场景/Prefab
        host.hideFlags = HideFlags.DontSave;
        UnityEngine.Object.DontDestroyOnLoad(host);
        var mgr = host.AddComponent<BetterModManager>();
        _instance = mgr;

        Log.Debug("[BML] Manager host created and marked DontDestroyOnLoad");
    }

    /// <summary>
    /// 初始化串行操作队列（Awake 中一次性建立）
    /// </summary>
    private void Awake()
    {
        // 单例守卫：任何途径产生的第二实例立即销毁，避免竞争
        if (_instance != null && _instance != this)
        {
            Log.Debug("[BML] Duplicate BetterModManager detected in Awake, destroying self");
            DestroyImmediate(gameObject);
            return;
        }

        if (_instance == null)
        {
            _instance = this;
            // 脱离父级，保证根级持久化
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
            }

            DontDestroyOnLoad(gameObject);
            Log.Debug("[BML] BetterModManager singleton initialized (Awake)");
        }
        else
        {
            // 已存在单例但仍执行队列初始化（防止首次 Awake 未完成）
            DontDestroyOnLoad(gameObject);
        }

        EnsureQueueInitialized();
    }

    private void OnDestroy()
    {
        // 销毁诊断：用于定位"下一帧前被销毁/外部销毁"导致的队列中断
        Log.Debug(
            $"[BML] OnDestroy enter. pendingOps={_pendingOps}, queueInitialized={_queueInitialized}, appQuitting={_appQuitting}");
        try
        {
            try
            {
                _queueCts.Cancel();
            }
            catch
            {
            }

            try
            {
                _queueSignal.Dispose();
            }
            catch
            {
            }

            // 等待处理任务完成（带超时）
            if (_processingTask != null && _processingTask.Task.Status != UniTaskStatus.Succeeded)
            {
                try
                {
                    _processingTask.Task.Timeout(TimeSpan.FromSeconds(5)).Forget();
                }
                catch
                {
                    // ignore timeout
                }
            }
        }
        catch
        {
            // ignore
        }

        Log.Debug("[BML] Operation queue disposed");

        // 单例归零，避免外部保留失效引用导致 EnsureCreated() 逻辑混淆
        if (_instance == this)
        {
            _instance = null;
            Log.Debug("[BML] Singleton cleared on destroy");
        }
    }

    /// <summary>
    /// 确保串行操作队列已初始化。
    /// </summary>
    private void EnsureQueueInitialized()
    {
        if (_queueInitialized)
        {
            return;
        }

        Log.Debug("[BML] Operation queue initializing");

        // 初始化并发队列系统
        _queueInitialized = true;
        Log.Debug("[BML] Operation queue ready");
    }

    private static bool ShouldActivate(ModInfo2 info)
    {
        var modId = info.Id;
        // 在所有分组中查找这个模组的启用状态
        foreach (var group in GroupConfig.Groups)
        {
            var modInfo2 = group.Mods.FirstOrDefault(m => m.Id == modId);
            if (modInfo2 != null)
            {
                return modInfo2.IsEnabled;
            }
        }

        return false;
    }

    private static void SetShouldActivate(ModInfo2 info, bool value)
    {
        var modId = info.Id;
        // 在所有分组中查找并更新这个模组的启用状态
        foreach (var group in GroupConfig.Groups)
        {
            var modInfo2 = group.Mods.FirstOrDefault(m => m.Id == modId);
            if (modInfo2 != null)
            {
                modInfo2.IsEnabled = value;
                return;
            }
        }
    }

    // 已废弃：不再写回或排序 Duckov.Modding.ModManager.modInfos；请使用 GroupConfig.SortedModInfos 获取排序后的视图。

    private static void RaiseOnModActivated(ModInfo info, Duckov.Modding.ModBehaviour b)
    {
        try
        {
            var fld = typeof(ModManager).GetField("OnModActivated",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var del = fld?.GetValue(null) as Action<ModInfo, Duckov.Modding.ModBehaviour>;
            del?.Invoke(info, b);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred");
        }
    }

    /// <summary>
    /// 触发 ModManager 的 OnModWillBeDeactivated 事件，以确保与原生 ModManager 的兼容性。
    /// </summary>
    /// <param name="info">要禁用的模组信息</param>
    /// <param name="behaviour">模组行为实例</param>
    private static void RaiseOnModWillBeDeactivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
    {
        try
        {
            var fld = typeof(ModManager).GetField("OnModWillBeDeactivated",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var del = fld?.GetValue(null) as Action<ModInfo, Duckov.Modding.ModBehaviour>;
            del?.Invoke(info, behaviour);
            Log.Info("[BML] Raised OnModWillBeDeactivated event for mod " + info.name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BML] Failed to raise OnModWillBeDeactivated event");
        }
    }

    private static void RaiseOnModStatusChanged()
    {
        try
        {
            var fld = typeof(ModManager).GetField("OnModStatusChanged",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var del = fld?.GetValue(null) as Action;
            del?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred");
        }
    }

    /// <summary>
    /// 检查指定模组是否处于激活状态。
    /// </summary>
    /// <param name="info">模组信息（ModInfo2 类型）</param>
    /// <param name="instance">模组行为实例</param>
    /// <returns>如果模组处于激活状态则返回 true</returns>
    public static bool IsActive(ModInfo2 info, out Duckov.Modding.ModBehaviour instance)
    {
        var modId = info.Id;
        if (!Instance._activeMods.TryGetValue(modId, out instance))
        {
            return false;
        }

        if (instance is not null)
        {
            return true;
        }

        // 失效引用自清理：Unity 对象可能被销毁后变成 null
        Instance._activeMods.Remove(modId);
        Log.Debug($"[BML] Detected destroyed behaviour reference removed for mod {modId}");
        return false;
    }

    private static Duckov.Modding.ModBehaviour? ActivateMod(ModInfo2 i)
    {
        var startTime = Time.realtimeSinceStartup;
        var info = i.OriginalModInfo;
        var modId = info.GetModId();

        // 统一的耗时计算方法（在方法作用域顶部声明，供早期返回使用）
        double ElapsedMs() => (Time.realtimeSinceStartup - startTime) * 1000.0;

        // 幂等保护：激活中
        if (!Instance._activating.Add(modId))
        {
            Log.Debug($"[BML] Activate busy (already processing) {modId}");
            RaiseModEnabled(new ModEnableCompletedEventArgs(
                i,
                null,
                ModEnableOutcome.Failed,
                ModEnableFailureReason.AlreadyActive,
                "Busy - activation already in progress",
                null,
                ElapsedMs()));
            return null;
        }

        try
        {
            // before enable
            RaiseModEnabling(i);

            if (!ModManager.AllowActivatingMod)
            {
                Log.Error(
                    "[BetterModLoader] Activating mod not allowed! User must allow mods in the agreement UI.");
                RaiseModEnabled(new ModEnableCompletedEventArgs(
                    i,
                    null,
                    ModEnableOutcome.Failed,
                    ModEnableFailureReason.NotAllowed,
                    "Activating mod not allowed",
                    null,
                    ElapsedMs()));
                return null;
            }

            if (IsActive(i, out var existing))
            {
                Log.Error($"[BetterModLoader] Mod {info.name} instance already exists! Abort. Path: {info.path}",
                    existing);
                RaiseModEnabled(new ModEnableCompletedEventArgs(
                    i,
                    existing,
                    ModEnableOutcome.Failed,
                    ModEnableFailureReason.AlreadyActive,
                    "Mod instance already exists",
                    null,
                    ElapsedMs()));
                return null;
            }

            var dllPath = info.dllPath;
            var ns = info.name;
            Log.Debug("[BetterModLoader] Loading mod dll at path: " + dllPath);

            // 程序集加载 + 缓存
            Type? type;
            try
            {
                if (!Instance._assemblyCache.TryGetValue(dllPath, out var cached))
                {
                    var asm = Assembly.LoadFrom(dllPath);
                    var modType = asm.GetType(ns + ".ModBehaviour");
                    Instance._assemblyCache[dllPath] = (asm, modType);
                    type = modType;
                    Log.Debug($"[BML] Assembly loaded & cached: {dllPath}");
                }
                else
                {
                    type = cached.ModType;
                    Log.Debug($"[BML] Assembly cache hit: {dllPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred");
                var msg = "Mod loading failed: " + ns + "\n" + ex.Message;
                ModManager.OnModLoadingFailed?.Invoke(info.dllPath, msg);
                RaiseModEnabled(new ModEnableCompletedEventArgs(
                    i,
                    null,
                    ModEnableOutcome.Failed,
                    ModEnableFailureReason.LoadAssemblyFailed,
                    msg,
                    ex,
                    ElapsedMs()));
                SetShouldActivate(i, false);
                return null;
            }

            if (type == null || !typeof(Duckov.Modding.ModBehaviour).IsAssignableFrom(type))
            {
                Log.Error("[BetterModLoader] Cannot load mod. A type named " + ns +
                          ".ModBehaviour is expected and should inherit from Duckov.Modding.ModBehaviour.");
                var reason = type == null
                    ? ModEnableFailureReason.TypeNotFound
                    : ModEnableFailureReason.InvalidBehaviourType;
                RaiseModEnabled(new ModEnableCompletedEventArgs(
                    i,
                    null,
                    ModEnableOutcome.Failed,
                    reason,
                    "ModBehaviour type invalid or not found",
                    null,
                    ElapsedMs()));
                SetShouldActivate(i, false);
                return null;
            }

            var go = new GameObject(ns);
            Duckov.Modding.ModBehaviour? behaviour;
            try
            {
                behaviour = go.AddComponent(type) as Duckov.Modding.ModBehaviour;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred");
                Log.Error("[BetterModLoader] Failed to create component for mod " + ns);
                Destroy(go);
                RaiseModEnabled(new ModEnableCompletedEventArgs(
                    i,
                    null,
                    ModEnableOutcome.Failed,
                    ModEnableFailureReason.CreateComponentFailed,
                    "Failed to create component",
                    ex,
                    ElapsedMs()));
                SetShouldActivate(i, false);
                return null;
            }

            if (behaviour == null)
            {
                Destroy(go);
                Log.Error("[BetterModLoader] Failed to create component for mod " + ns);
                RaiseModEnabled(new ModEnableCompletedEventArgs(
                    i,
                    null,
                    ModEnableOutcome.Failed,
                    ModEnableFailureReason.CreateComponentFailed,
                    "Failed to create component (null)",
                    null,
                    ElapsedMs()));
                SetShouldActivate(i, false);
                return null;
            }

            // Parent under original ModManager
            var parent = ModManager.Instance != null ? ModManager.Instance.transform : null;
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }

            Log.Debug("[BetterModLoader] Mod Loaded: " + info.name);
            try
            {
                behaviour.Setup(ModManager.Instance, info);
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception occurred");
                Log.Error("[BetterModLoader] Mod Setup failed for mod " + ns);
                Destroy(go);
                RaiseModEnabled(new ModEnableCompletedEventArgs(
                    i,
                    null,
                    ModEnableOutcome.Failed,
                    ModEnableFailureReason.SetupFailed,
                    "Failed to create component",
                    e,
                    ElapsedMs()));
                SetShouldActivate(i, false);
                return null;
            }

            Instance._activeMods[modId] = behaviour;

            try
            {
                RaiseOnModActivated(info, behaviour);
                RaiseOnModStatusChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred");
            }

            RaiseModEnabled(new ModEnableCompletedEventArgs(
                i,
                behaviour,
                ModEnableOutcome.Success,
                null,
                null,
                null,
                ElapsedMs()));

            SetShouldActivate(i, true);
            return behaviour;
        }
        finally
        {
            Instance._activating.Remove(modId);
        }
    }

    public static void DeactivateMod(ModInfo2 info)
    {
        var sw = Stopwatch.StartNew();
        var modId = info.Id;

        // 幂等保护：正在禁用
        if (Instance._deactivating.Contains(modId))
        {
            Log.Debug($"[BML] Deactivate busy (already processing) {modId}");
            RaiseModDisabled(new ModDisableCompletedEventArgs(
                info,
                ModDisableOutcome.Failed,
                ModDisableFailureReason.NotActive,
                "Busy - deactivation already in progress",
                null,
                sw.Elapsed.TotalMilliseconds));
            return;
        }

        Instance._deactivating.Add(modId);
        try
        {
            // 检查模组是否处于激活状态
            if (!Instance._activeMods.TryGetValue(modId, out var behaviour) || behaviour == null)
            {
                Log.Warn($"[BetterModManager] Cannot deactivate mod '{modId}': not active");
                RaiseModDisabled(new ModDisableCompletedEventArgs(
                    info,
                    ModDisableOutcome.Failed,
                    ModDisableFailureReason.NotActive,
                    "Mod is not currently active",
                    null,
                    sw.Elapsed.TotalMilliseconds));
                return;
            }

            // before disable
            RaiseModDisabling(info);

            // Trigger ModManager's OnModWillBeDeactivated event for compatibility
            RaiseOnModWillBeDeactivated(info.OriginalModInfo, behaviour);

            Exception? caughtException = null;
            try
            {
                behaviour.NotifyBeforeDeactivate();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred");
                caughtException = ex;
            }

            // 从字典中移除
            Instance._activeMods.Remove(modId);

            try
            {
                Destroy(behaviour.gameObject);
                RaiseOnModStatusChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred");
                caughtException ??= ex;
            }

            SetShouldActivate(info, false);

            sw.Stop();

            var outcome = caughtException == null ? ModDisableOutcome.Success : ModDisableOutcome.Failed;
            var failureReason =
                caughtException != null ? ModDisableFailureReason.Exception : (ModDisableFailureReason?)null;

            RaiseModDisabled(new ModDisableCompletedEventArgs(
                info,
                outcome,
                failureReason,
                caughtException?.Message,
                caughtException,
                sw.Elapsed.TotalMilliseconds));
        }
        finally
        {
            Instance._deactivating.Remove(modId);
        }
    }

    #region Activation jobs API

    /// <summary>
    /// 表示一次启用/禁用操作的任务。
    /// </summary>
    public readonly struct ModOperation(ModActivationAction action, ModInfo2 info, int order)
    {
        public readonly ModActivationAction Action = action;
        public readonly ModInfo2 Info = info;
        public readonly int Order = order;
    }

    public enum ModActivationAction
    {
        Enable = 0,
        Disable = 1,
    }

    /// <summary>
    /// 扫描当前配置，返回需要执行的任务（默认仅包含启用任务，按分组排序）。
    /// </summary>
    public static ModOperation[] ScanActivationJobs()
    {
        var jobs = new List<ModOperation>();
        if (!ModManager.AllowActivatingMod)
        {
            return jobs.ToArray();
        }

        int order = 0;
        foreach (var info in GroupConfig.SortedModInfos)
        {
            if (ModIdentifier.IsActivateByOldModLoader(info.OriginalModInfo))
            {
                continue;
            }

            var isActive = IsActive(info, out var _);
            var should = ShouldActivate(info);

            if (should && !isActive)
            {
                jobs.Add(new ModOperation(ModActivationAction.Enable, info, order++));
            }
        }

        return jobs.ToArray();
    }

    /// <summary>
    /// 按顺序执行一系列任务。
    /// </summary>
    public IEnumerable<ModOperationResult> ProcessModOperations(IEnumerable<ModOperation> jobs)
    {
        var ordered = jobs.OrderBy(j => j.Order).ToList();
        var instance = Instance;

        // 确保队列初始化，避免入队前订阅未建立
        instance.EnsureQueueInitialized();

        instance._currentSessionId = Guid.NewGuid();
        instance._pendingOps = ordered.Count;
        instance._currentSessionResults = new List<ModOperationResult>(ordered.Count);
        instance._sessionStopwatch = Stopwatch.StartNew();

        Log.Debug($"[BML] Session {instance._currentSessionId} START. jobs={ordered.Count}");

        // 入队（串行消费）
        foreach (var job in ordered)
        {
            Log.Debug($"[BML] Enqueue job {job.Action} {job.Info.Id} order={job.Order}");
            EnqueueOperation(job);
        }

        // 由于当前实现队列在主线程同步（Defer + Concat），此处 results 已全部生成
        if (instance._currentSessionResults != null)
        {
            foreach (var r in instance._currentSessionResults)
            {
                yield return r;
            }
        }
    }

    /// <summary>
    /// 执行单个任务（供外部逐步驱动 UI 刷新时调用）。
    /// </summary>
    public static ModOperationResult ProcessModOperation(ModOperation op)
    {
        var sw = Stopwatch.StartNew();
        Exception? caught = null;
        bool success = false;

        try
        {
            if (op.Action == ModActivationAction.Enable)
            {
                // 幂等防重复启用
                var id = op.Info.Id;
                if (Instance._enabledMods.Contains(id))
                {
                    Log.Debug($"[BML] Skip duplicated enable: {id}");
                    success = true; // 视为成功且跳过
                }
                else
                {
                    var behaviour = ActivateMod(op.Info);
                    success = behaviour != null;
                    if (success)
                    {
                        Instance._enabledMods.Add(id);
                    }
                }
            }
            else if (op.Action == ModActivationAction.Disable)
            {
                DeactivateMod(op.Info);
                // DeactivateMod 自身会报告事件；若未抛异常视作成功
                success = true;

                // 维护幂等集合
                var id = op.Info.Id;
                if (!Instance._enabledMods.Remove(id))
                {
                    Log.Debug($"[BML] Disable requested but not marked enabled: {id}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred");
            caught = ex;
        }

        sw.Stop();
        return new ModOperationResult(op, success, caught, sw.Elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// 将操作加入队列并启动处理器（如果尚未运行）。
    /// </summary>
    private void EnqueueOperation(ModOperation operation)
    {
        _operationQueue.Enqueue(operation);

        // 发送信号通知有新操作
        _queueSignal.Release();

        // 启动处理器（如果尚未运行）
        TryStartProcessor();
    }

    /// <summary>
    /// 尝试启动队列处理器（如果尚未运行）。
    /// </summary>
    private void TryStartProcessor()
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;
        _processingTask = new UniTaskCompletionSource();
        ProcessQueueAsync(_queueCts.Token).Forget();
    }

    /// <summary>
    /// 并发队列消费循环：使用信号量等待新操作，逐项让出一帧再执行。
    /// </summary>
    private async UniTask ProcessQueueAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 等待队列信号（有新操作或取消）
                await _queueSignal.WaitAsync(ct);

                // 处理队列中的所有操作
                while (_operationQueue.TryDequeue(out var operation))
                {
                    await UniTask.Yield(PlayerLoopTiming.TimeUpdate);
                    Log.Debug($"[BML] Begin op after frame-yield: {operation.Info.Id}, action={operation.Action}");

                    ModOperationResult result;
                    var start = Time.realtimeSinceStartup;
                    try
                    {
                        result = await ProcessModOperationAsync(operation, ct);
                    }
                    catch (Exception exOp)
                    {
                        // 单操作异常：记录并构造失败结果，保持循环继续
                        Log.Error("[BML] Operation error (single op)", exOp);
                        var durationMs = (Time.realtimeSinceStartup - start) * 1000.0;
                        result = new ModOperationResult(operation, false, exOp, durationMs);
                    }

                    _currentSessionResults?.Add(result);
                    OnOpCompleted();
                }
            }
        }
        catch (Exception exLoop)
        {
            // 循环级异常：记录并在 finally 中根据条件决定是否重启
            Log.Error("[BML] Operation queue error (processor loop)", exLoop);
        }
        finally
        {
            _isProcessing = false;
            _processingTask?.TrySetResult();

            // 若不是销毁/退出 且 还未取消，则自动重启
            if (!ct.IsCancellationRequested && !_appQuitting && this != null && gameObject != null)
            {
                Log.Debug("[BML] Processor restarting after error or graceful drain");
                TryStartProcessor();
            }
            else
            {
                Log.Debug("[BML] Processor stopped (canceled or quitting)");
            }
        }
    }

    /// <summary>
    /// 单个操作的异步包装（当前同步实现，预留后续异步扩展点）。
    /// </summary>
    private UniTask<ModOperationResult> ProcessModOperationAsync(ModOperation op, CancellationToken ct)
    {
        // 目前 ProcessModOperation 为完全同步；若后续需要真正异步 I/O 可在此扩展。
        var result = ProcessModOperation(op);
        return UniTask.FromResult(result);
    }

    /// <summary>
    /// 单个操作完成回调：递减 _pendingOps；当为 0 触发批次完成事件
    /// </summary>
    private void OnOpCompleted()
    {
        var left = Interlocked.Decrement(ref _pendingOps);
        if (left > 0)
        {
            return;
        }

        // 所有操作完成
        if (_isBroadcasting)
        {
            Log.Debug("[BML] Skip broadcast (re-entrancy protected)");
            return;
        }

        _isBroadcasting = true;
        try
        {
            _sessionStopwatch?.Stop();
            var duration = _sessionStopwatch?.Elapsed.TotalMilliseconds ?? 0;
            var snapshot = (_currentSessionResults ?? []).AsReadOnly();
            Log.Debug($"[BML] Session {_currentSessionId} END. ops={snapshot.Count} durationMs={duration:F2}");
            try
            {
                AllModJobsCompleted?.Invoke(snapshot);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "AllModJobsCompleted handler failed");
            }
        }
        finally
        {
            _isBroadcasting = false;
            _currentSessionResults = null;
            _sessionStopwatch = null;
        }
    }

    private void OnApplicationQuit()
    {
        _appQuitting = true;
    }

    #endregion
}
