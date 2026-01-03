using System.Text;
using System.Text.RegularExpressions;
using Steamworks;
using Unity.VisualScripting;

namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

internal class BmlSteamItemManager : MonoBehaviour
{
    private CallResult<SteamUGCQueryCompleted_t> CRSteamUGCQueryCompleted;

    private CallResult<CreateItemResult_t> CRCreateItemResult;

    private CallResult<SubmitItemUpdateResult_t>? CRSubmitItemUpdate;

    private UGCQueryHandle_t activeQueryHandle;

    private static readonly List<SteamUGCDetails_t> ugcDetailsCache = [];

    private bool createItemResultFired;

    private CreateItemResult_t createItemResult;

    public static BmlSteamItemManager Instance
    {
        get
        {
            if (field == null)
            {
                var managerGo = new GameObject("BmlSteamItemManager");
                var instance = managerGo.AddComponent<BmlSteamItemManager>();
                DontDestroyOnLoad(managerGo);
                field = instance;
            }

            return field;
        }
    }

    public static ulong punBytesProcess { get; private set; }

    public static ulong punBytesTotal { get; private set; }

    public bool UploadSucceed { get; private set; }

    private void OnEnable()
    {
        SendQueryDetailsRequest();
    }

    private void OnDisable()
    {
    }

    public void SendQueryDetailsRequest()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

        if (CRSteamUGCQueryCompleted == null)
        {
            CRSteamUGCQueryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(OnSteamUGCQueryCompleted);
        }

        var hashSet = new HashSet<PublishedFileId_t>();
        var numSubscribedItems = SteamUGC.GetNumSubscribedItems();
        var array = new PublishedFileId_t[numSubscribedItems];
        SteamUGC.GetSubscribedItems(array, numSubscribedItems);
        hashSet.AddRange(array);
        foreach (var modInfo in ModManager.modInfos)
        {
            if (modInfo.publishedFileId != 0L)
            {
                hashSet.Add((PublishedFileId_t)modInfo.publishedFileId);
            }
        }

        var hAPICall =
            SteamUGC.SendQueryUGCRequest(SteamUGC.CreateQueryUGCDetailsRequest(hashSet.ToArray(), (uint)hashSet.Count));
        CRSteamUGCQueryCompleted.Set(hAPICall);
        new StringBuilder();
    }

    private void OnSteamUGCQueryCompleted(SteamUGCQueryCompleted_t completed, bool bIOFailure)
    {
        if (bIOFailure)
        {
            Log.Error("Steam UGC Query failed", gameObject);
            ModManager.Instance.ScanAndActivateMods();
            return;
        }

        var handle = completed.m_handle;
        var unNumResultsReturned = completed.m_unNumResultsReturned;
        for (var num = 0u; num < unNumResultsReturned; num++)
        {
            SteamUGC.GetQueryUGCResult(handle, num, out var pDetails);
            ugcDetailsCache.Add(pDetails);
        }

        SteamUGC.ReleaseQueryUGCRequest(handle);
        ModManager.Instance.ScanAndActivateMods();
    }

    public async UniTask<PublishedFileId_t> RequestNewWorkshopItemID()
    {
        if (!SteamManager.Initialized)
        {
            return default;
        }

        if (CRCreateItemResult == null)
        {
            CRCreateItemResult = CallResult<CreateItemResult_t>.Create(OnCreateItemResult);
        }

        Log.Debug("Requesting new PublishedFileId");
        createItemResultFired = false;
        var hAPICall = SteamUGC.CreateItem((AppId_t)3167020u, EWorkshopFileType.k_EWorkshopFileTypeFirst);
        CRCreateItemResult.Set(hAPICall, delegate(CreateItemResult_t result, bool failure)
        {
            Log.Debug("Creat Item Result Fired B");
            createItemResultFired = true;
            createItemResult = result;
        });
        while (!createItemResultFired)
        {
            await UniTask.Yield();
        }

        if (createItemResult.m_eResult != EResult.k_EResultOK)
        {
            Log.Error($"Failed to create workshop item.\nResult: {createItemResult.m_eResult}");
            return default(PublishedFileId_t);
        }

        return createItemResult.m_nPublishedFileId;
    }

    private void OnCreateItemResult(CreateItemResult_t result, bool bIOFailure)
    {
        Log.Debug("Creat Item Result Fired A");
        createItemResultFired = true;
        createItemResult = result;
    }

    public async UniTask<bool> UploadWorkshopItem(string path,
        WorkshopTags tags,
        bool uploadDesc,
        string? descriptionTail,
        string? changeNote)
    {
        if (!SteamManager.Initialized)
        {
            OnUploadProgress?.Invoke(new WorkshopUploadProgress
            {
                Status = WorkshopUploadStatus.Failed,
                Message = L.Upload.ProgressSteamNotInitialized
            });
            return false;
        }

        OnUploadProgress?.Invoke(new WorkshopUploadProgress
        {
            Status = WorkshopUploadStatus.Preparing,
            Message = $"{L.Upload.ProgressPreparing}: {path}"
        });
        Log.Debug("Begin uploading mod: \n" + path);
        if (!ModManager.TryProcessModFolder(path, out var modInfo, isSteamItem: false, 0uL))
        {
            OnUploadProgress?.Invoke(new WorkshopUploadProgress
            {
                Status = WorkshopUploadStatus.Failed,
                Message = string.Format(L.Upload.ProgressFailedToProcessMod, path)
            });
            Log.Error("Failed to process mod folder:\n" + path);
            return false;
        }

        if (!modInfo.dllFound)
        {
            OnUploadProgress?.Invoke(new WorkshopUploadProgress
            {
                Status = WorkshopUploadStatus.Failed,
                Message = string.Format(L.Upload.ProgressModDllNotFound, modInfo.dllPath)
            });
            Log.Error("Mod's dll file not found.\n" + modInfo.dllPath);
            return false;
        }

        var isNewItem = false;
        if (modInfo.publishedFileId == 0L)
        {
            isNewItem = true;
            OnUploadProgress?.Invoke(new WorkshopUploadProgress
            {
                Status = WorkshopUploadStatus.RequestingId,
                Message = $"{L.Upload.ProgressRequestingId}: {modInfo.name}"
            });
            Log.Debug("Requesting PublishedFileId for mod " + modInfo.name + " \n" + path);
            var publishedFileIdT = await RequestNewWorkshopItemID();
            if (publishedFileIdT.m_PublishedFileId == 0L)
            {
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.Failed,
                    Message = L.Upload.ProgressFailedToRequestId
                });
                Log.Debug("Failed to request PublishedFileId.");
                return false;
            }

            modInfo.publishedFileId = publishedFileIdT.m_PublishedFileId;
            try
            {
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.WritingIni,
                    Message = $"{L.Upload.ProgressWritingIni}: {modInfo.name}"
                });
                Log.Debug($"MOD [{modInfo.name}] ({modInfo.publishedFileId}) Writing mod's info.ini  \n{path}");
                WriteModInfoINI(modInfo);
            }
            catch (Exception exception)
            {
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.Failed,
                    Message = L.Upload.ProgressFailedToWriteInfo,
                    Exception = exception
                });
                Log.Error(exception,
                    $"Successfully created steam workshop item (PublishedFileId:{publishedFileIdT}). But failed to write to info.ini.");
                return false;
            }
        }

        OnUploadProgress?.Invoke(new WorkshopUploadProgress
        {
            Status = WorkshopUploadStatus.StartingSteamUpload,
            Message = $"{L.Upload.ProgressStartingSteamUpload}: {modInfo.name}"
        });
        var handle =
            SteamUGC.StartItemUpdate((AppId_t)3167020u, (PublishedFileId_t)modInfo.publishedFileId);

        var pendingLangUpdates = new Dictionary<string, string>();
        var multiLangTitles = new Dictionary<string, string>();

        // Multi-language title and description support
        if (isNewItem || uploadDesc)
        {
            // Load all available language titles from the mod folder
            multiLangTitles = MultiLanguageContentHelper.LoadMultiLanguageTitles(modInfo.path, modInfo.displayName);

            // Load all available language descriptions from the mod folder
            var multiLangDescriptions = MultiLanguageContentHelper.LoadMultiLanguageDescriptions(modInfo.path);

            string primaryTitle;
            if (multiLangDescriptions.Count > 0)
            {
                // Find English or use the first one as primary
                string primaryDesc;

                string primaryLang;
                if (multiLangDescriptions.ContainsKey("english"))
                {
                    primaryLang = "english";
                    primaryDesc = multiLangDescriptions["english"];
                    multiLangDescriptions.Remove("english");
                }
                else
                {
                    var first = multiLangDescriptions.First();
                    primaryLang = first.Key;
                    primaryDesc = first.Value;
                    multiLangDescriptions.Remove(first.Key);
                }

                // Get the primary title
                primaryTitle = multiLangTitles.GetValueOrDefault(primaryLang, modInfo.displayName);

                // Report progress for setting primary title and description
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.UploadingTitles,
                    Message = $"{L.Upload.ProgressUploadingTitles}: {primaryLang}"
                });

                // Set primary on main handle
                SteamUGC.SetItemUpdateLanguage(handle, primaryLang);
                SteamUGC.SetItemTitle(handle, primaryTitle);
                var mainDesc = string.IsNullOrEmpty(descriptionTail) ? primaryDesc : primaryDesc + descriptionTail;
                SteamUGC.SetItemDescription(handle, mainDesc);
                Log.Debug(
                    "[BmlSteamItemManager] Set primary title and description for language: {primaryLang}, title:{Title}, description:{Description}",
                    primaryLang, primaryTitle, mainDesc);

                // Queue other languages for updates
                foreach (var kvp in multiLangDescriptions)
                {
                    var lang = kvp.Key;
                    var desc = kvp.Value;
                    var title = multiLangTitles.GetValueOrDefault(lang, modInfo.displayName);

                    // Store both title and description for this language
                    pendingLangUpdates[lang] = desc;
                    multiLangTitles[lang] = title;
                }
            }
            else
            {
                // No description files found, use default description
                const string defaultDescription = "No description provided.";
                primaryTitle = multiLangTitles.GetValueOrDefault("english", modInfo.displayName);

                // Report progress for setting default title and description
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.UploadingTitles,
                    Message = $"{L.Upload.ProgressUploadingTitles} (no description files found)"
                });

                SteamUGC.SetItemUpdateLanguage(handle, "english");
                SteamUGC.SetItemTitle(handle, primaryTitle);
                SteamUGC.SetItemDescription(handle, defaultDescription + descriptionTail);
                Log.Debug("[BmlSteamItemManager] Using default description (no description files found)");

                // Still queue other language titles if available
                foreach (var kvp in multiLangTitles)
                {
                    if (kvp.Key != "english")
                    {
                        pendingLangUpdates[kvp.Key] = ""; // Empty description for now
                        multiLangTitles[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        else
        {
            // Report progress for setting title only (no description update)
            OnUploadProgress?.Invoke(new WorkshopUploadProgress
            {
                Status = WorkshopUploadStatus.UploadingTitles,
                Message = L.Upload.ProgressUploadingTitles
            });

            // No description update, use original title
            SteamUGC.SetItemTitle(handle, modInfo.displayName);
        }


        SteamUGC.SetItemVisibility(handle,
            ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);

        // 根据上传选择的 WorkshopTags 生成 Steam 标签列表（始终包含基础 "Mod" 标签）
        var extraTags = tags.ToSteamTags();
        if (extraTags.Length > 0)
        {
            Log.Info("Setting extra tags: {Tags}", string.Join(", ", extraTags));
            SteamUGC.SetItemTags(handle, extraTags);
        }

        var text = Path.Combine(modInfo.path, "preview.png").Replace('\\', '/');
        Log.Info("Setting preview image: {Path}", text);
        SteamUGC.SetItemPreview(handle, text);
        var path2 = modInfo.path.Replace('\\', '/');
        Log.Info("Setting item content path: {Path}", path2);
        var flag = SteamUGC.SetItemContent(handle, path2);
        Log.Info("SetItemContent returned: {Result}", flag);
        var submitCallDone = false;
        var submitResult = default(SubmitItemUpdateResult_t);
        Log.Debug($"MOD [{modInfo.name}] ({modInfo.publishedFileId}) Begin uploading to steam.  \n{path}");

        // 创建 CallResult 并保存为实例字段，防止过早释放
        CRSubmitItemUpdate ??= CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdateResult);

        void OnSubmitItemUpdateResult(SubmitItemUpdateResult_t result, bool failure)
        {
            Log.Debug($"MOD [{modInfo.name}] Upload callback fired. Result: {result.m_eResult}, Failure: {failure}");
            submitCallDone = true;
            submitResult = result;
        }

        Log.Info("Submitting item update to Steam..., PublishedFileId: {PublishedFileId}, changeNote: {ChangeNote}",
            modInfo.publishedFileId, changeNote!);
        var hAPICall = SteamUGC.SubmitItemUpdate(handle, changeNote);
        CRSubmitItemUpdate.Set(hAPICall, OnSubmitItemUpdateResult);

        // 添加超时机制：最多等待 5 分钟
        const float timeoutSeconds = 300f;
        var elapsedTime = 0f;
        var lastProgressTime = Time.realtimeSinceStartup;
        ulong lastBytesProcessed = 0;

        while (!submitCallDone)
        {
            SteamUGC.GetItemUpdateProgress(handle, out var punBytesProcessed, out var num);
            punBytesProcess = punBytesProcessed;
            punBytesTotal = num;

            // 只在进度变化或每秒时报告一次
            var currentTime = Time.realtimeSinceStartup;
            var progressChanged = punBytesProcessed != lastBytesProcessed;
            var shouldReport = progressChanged || (currentTime - lastProgressTime >= 1f);

            if (shouldReport)
            {
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.UploadingContent,
                    BytesProcessed = punBytesProcessed,
                    BytesTotal = num,
                    Message = $"{L.Upload.ProgressUploadingContent}: {punBytesProcessed}/{num} bytes"
                });
                lastProgressTime = currentTime;
                lastBytesProcessed = punBytesProcessed;
            }

            // 检查超时
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= timeoutSeconds)
            {
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.Failed,
                    Message = string.Format(L.Upload.ProgressUploadTimeout, timeoutSeconds) +
                              $" Last progress: {punBytesProcessed}/{num} bytes"
                });
                Log.Debug($"MOD [{modInfo.name}] Upload timeout after {elapsedTime}s");
                return false;
            }

            // 降低 CPU 占用：每 100ms 检查一次而不是每帧
            await UniTask.Delay(100);
        }

        Log.Debug($"MOD [{modInfo.name}] ({modInfo.publishedFileId}) Upload result returned.  \n{path}");
        if (submitResult.m_eResult != EResult.k_EResultOK)
        {
            OnUploadProgress?.Invoke(new WorkshopUploadProgress
            {
                Status = WorkshopUploadStatus.Failed,
                Message = string.Format(L.Upload.ProgressFailedToUpload, submitResult.m_eResult)
            });
            Log.Error($"Failed to upload mod.\nResult:{submitResult.m_eResult} \npath: {modInfo.path}");
            return false;
        }

        if (submitResult.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            SteamFriends.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{modInfo.publishedFileId}");
        }

        // Process pending language updates sequentially
        if (pendingLangUpdates.Count > 0)
        {
            var currentLangIndex = 0;
            foreach (var kvp in pendingLangUpdates)
            {
                currentLangIndex++;
                var title = multiLangTitles.GetValueOrDefault(kvp.Key, modInfo.displayName);
                OnUploadProgress?.Invoke(new WorkshopUploadProgress
                {
                    Status = WorkshopUploadStatus.UploadingTranslations,
                    Message = string.Format(L.Upload.ProgressUploadingTranslations, currentLangIndex,
                        pendingLangUpdates.Count, kvp.Key)
                });

                await UpdateItemTitleAndDescription(modInfo.publishedFileId, kvp.Key, title,
                    string.IsNullOrEmpty(descriptionTail) ? kvp.Value : kvp.Value + descriptionTail);
            }
        }

        UploadSucceed = true;
        OnUploadProgress?.Invoke(new WorkshopUploadProgress
        {
            Status = WorkshopUploadStatus.Completed,
            Message = $"{L.Upload.ProgressCompleted}: {modInfo.name}"
        });
        return true;
    }

    public static bool IsOwner(ModInfo info)
    {
        if (!SteamManager.Initialized)
        {
            return false;
        }

        if (info.publishedFileId == 0L)
        {
            return false;
        }

        foreach (var item in ugcDetailsCache)
        {
            if (item.m_nPublishedFileId.m_PublishedFileId == info.publishedFileId)
            {
                return item.m_ulSteamIDOwner == SteamUser.GetSteamID().m_SteamID;
            }
        }

        return false;
    }

    private async UniTask<bool> UpdateItemTitleAndDescription(ulong publishedFileId, string lang, string title,
        string desc)
    {
        Log.Debug($"[BmlSteamItemManager] Starting update for language: {lang}, Title: {title}");
        var handle = SteamUGC.StartItemUpdate((AppId_t)3167020u, (PublishedFileId_t)publishedFileId);
        SteamUGC.SetItemUpdateLanguage(handle, lang);
        SteamUGC.SetItemTitle(handle, title);
        SteamUGC.SetItemDescription(handle, desc);

        var submitCallDone = false;
        var submitResult = default(SubmitItemUpdateResult_t);

        // Ensure CallResult is created
        CRSubmitItemUpdate ??= CallResult<SubmitItemUpdateResult_t>.Create((result, failure) => { });

        var hAPICall = SteamUGC.SubmitItemUpdate(handle, "");
        CRSubmitItemUpdate.Set(hAPICall, (result, failure) =>
        {
            submitCallDone = true;
            submitResult = result;
        });

        // Wait loop
        var timeout = 30f;
        var elapsed = 0f;
        while (!submitCallDone && elapsed < timeout)
        {
            await Task.Delay(100);
            elapsed += 0.1f;
        }

        if (!submitCallDone)
        {
            Log.Error($"[BmlSteamItemManager] Timeout updating title/description for language: {lang}");
            return false;
        }

        if (submitResult.m_eResult != EResult.k_EResultOK)
        {
            Log.Error(
                $"[BmlSteamItemManager] Failed to update title/description for language: {lang}. Result: {submitResult.m_eResult}");
            return false;
        }

        Log.Debug($"[BmlSteamItemManager] Successfully updated title/description for language: {lang}");
        return true;
    }

    private static void WriteModInfoINI(ModInfo modInfo)
    {
        var path = Path.Combine(modInfo.path, "info.ini");
        if (File.Exists(path))
            File.Delete(path);
        using var fileStream = File.Create(path);
        var streamWriter = new StreamWriter((Stream)fileStream);
        streamWriter.WriteLine("name = " + modInfo.name);
        streamWriter.WriteLine("displayName = " + modInfo.displayName);
        var description = modInfo.description;
        string str;
        try
        {
            str = Regex.Escape(description);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred");
            str = "Cannot escape description";
        }

        streamWriter.WriteLine("description = " + str);
        streamWriter.WriteLine("");
        streamWriter.WriteLine($"publishedFileId = {modInfo.publishedFileId}");
        streamWriter.Close();
    }

    public enum WorkshopUploadStatus
    {
        Pending,
        Preparing,
        RequestingId,
        WritingIni,
        StartingSteamUpload,
        UploadingContent,
        UploadingTitles,
        UploadingTranslations,
        Completed,
        Failed
    }

    public struct WorkshopUploadProgress
    {
        public WorkshopUploadStatus Status { get; set; }
        public ulong BytesProcessed { get; set; }
        public ulong BytesTotal { get; set; }
        public string Message { get; set; }
        public Exception? Exception { get; set; }
        public bool IsSuccess => Status == WorkshopUploadStatus.Completed;
    }

    /// <summary>
    /// 获取所有已订阅物品及其依赖信息的异步方法（新版：使用批量查询策略）
    /// </summary>
    /// <returns>包含所有订阅物品及其递归依赖项的列表，热门mod中包含Title字段</returns>
    public List<SteamModDependencyInfo> RefreshUserSubMods()
    {
        if (!SteamManager.Initialized)
        {
            Log.Debug("[BmlSteamItemManager] Steam未初始化，无法获取订阅物品依赖信息");
            return [];
        }

        // 步骤1: 获取用户订阅列表
        var numSubscribedItems = SteamUGC.GetNumSubscribedItems();
        var subscribedItems = new PublishedFileId_t[numSubscribedItems];
        SteamUGC.GetSubscribedItems(subscribedItems, numSubscribedItems);
        var subscribedSet = new HashSet<ulong>(subscribedItems.Select(x => x.m_PublishedFileId));

        Log.Debug($"[BmlSteamItemManager] 找到 {numSubscribedItems} 个已订阅物品");

        var allMods = SteamModsLocalDataManager.Instance.GetAllMods();

        // 步骤3: 构建字典方便查找
        var modDict = allMods.ToDictionary(m => m.SteamId);

        // 步骤4: 匹配用户订阅的mod
        var result = new List<SteamModDependencyInfo>();
        var matchedCount = 0;
        var unmatchedCount = 0;

        foreach (var itemId in subscribedSet)
        {
            if (modDict.TryGetValue(itemId, out var modInfo))
            {
                // 在查询结果中找到了
                modInfo.IsSubscribed = true;
                // get local install time
                modInfo.LocalInstallTime = GetLocalInstallTime(itemId);
                result.Add(modInfo);
                matchedCount++;
            }
            else
            {
                // 未在查询结果中找到的mod，创建基础信息（无依赖项数据）
                var basicInfo = new SteamModDependencyInfo
                {
                    SteamId = itemId,
                    IsSubscribed = true,
                    LocalInstallTime = GetLocalInstallTime(itemId),
                    Title = null, // 未在查询结果中，无法获取标题
                    DependencyIds = []
                };
                result.Add(basicInfo);
                unmatchedCount++;
            }
        }

        Log.Debug(
            $"[BmlSteamItemManager] 依赖信息获取完成 - 总计: {result.Count} 个物品, 在查询结果中匹配: {matchedCount}, 未匹配: {unmatchedCount}");

        SteamModsLocalDataManager.Instance.SetUserSubscribedMods(result);

        return result;
    }

    /// <summary>
    /// 查询所有创意工坊mod的异步方法（按总订阅数排序，批量获取Title、更新时间和依赖项）
    /// </summary>
    /// <param name="maxMods">最大查询数量，默认为null表示获取所有可用的mod</param>
    /// <returns>mod的信息列表，包含Title、依赖项和更新时间信息（均在批量查询中一次性获取）</returns>
    public async UniTask<List<SteamModDependencyInfo>> QueryAllModsAsync(uint? maxMods = null)
    {
        const uint pageSize = 50; // Steam UGC每页最多50条

        var allMods = new List<SteamModDependencyInfo>();

        if (maxMods.HasValue)
        {
            Log.Debug($"[BmlSteamItemManager] 开始批量查询热门{maxMods.Value}个mod（每页{pageSize}条，批量获取Title、更新时间和依赖项）");
        }
        else
        {
            Log.Debug($"[BmlSteamItemManager] 开始批量查询所有mod（每页{pageSize}条，批量获取Title、更新时间和依赖项）");
        }

        uint page = 1;
        uint? totalMatchingMods = null;

        while (true)
        {
            // 如果设置了最大数量且已达到，停止查询
            if (maxMods.HasValue && allMods.Count >= maxMods.Value)
            {
                Log.Debug($"[BmlSteamItemManager] 已达到目标数量 {maxMods.Value}，停止查询");
                break;
            }

            Log.Debug($"[BmlSteamItemManager] 正在查询第 {page} 页...");

            // 创建查询：按总订阅数排序
            var queryHandle = SteamUGC.CreateQueryAllUGCRequest(
                EUGCQuery.k_EUGCQuery_RankedByTotalUniqueSubscriptions, // 按总订阅数排序
                EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items,
                (AppId_t)3167020u, // Duckov的AppId
                (AppId_t)3167020u,
                page
            );

            // 设置返回选项：批量获取Title、更新时间（m_rtimeUpdated）和Children（依赖项）
            SteamUGC.SetReturnChildren(queryHandle, true); // 重要：批量返回依赖项信息
            SteamUGC.SetReturnMetadata(queryHandle, false); // 不需要元数据
            SteamUGC.SetReturnLongDescription(queryHandle, false); // 不需要详细描述

            // 发送查询并等待结果
            var queryCompleted = false;
            SteamUGCQueryCompleted_t queryResult = default;

            using (var callResult = CallResult<SteamUGCQueryCompleted_t>.Create((result, failure) =>
                   {
                       queryCompleted = true;
                       queryResult = result;
                   }))
            {
                var hAPICall = SteamUGC.SendQueryUGCRequest(queryHandle);
                callResult.Set(hAPICall);

                // 等待查询完成，最多等待10秒
                var timeout = 0f;
                while (!queryCompleted && timeout < 10f)
                {
                    await UniTask.Delay(100);
                    timeout += 0.1f;
                }

                if (!queryCompleted)
                {
                    Log.Debug($"[BmlSteamItemManager] 第 {page} 页查询超时");
                    SteamUGC.ReleaseQueryUGCRequest(queryHandle);
                    break;
                }
            }

            // 处理查询结果
            var numResults = queryResult.m_unNumResultsReturned;
            var totalMatching = queryResult.m_unTotalMatchingResults;

            // 第一次查询时记录总数
            if (!totalMatchingMods.HasValue)
            {
                totalMatchingMods = totalMatching;
                Log.Debug($"[BmlSteamItemManager] Steam Workshop共有 {totalMatching} 个符合条件的mod");
            }

            Log.Debug($"[BmlSteamItemManager] 第 {page} 页返回 {numResults} 条结果（总匹配数: {totalMatching}）");

            if (numResults == 0)
            {
                Log.Debug($"[BmlSteamItemManager] 第 {page} 页无结果，停止查询");
                SteamUGC.ReleaseQueryUGCRequest(queryHandle);
                break;
            }

            // 批量提取每个mod的信息（Title、OnlineUpdateTime、DependencyIds均已通过单次查询获取）
            for (uint i = 0; i < numResults; i++)
            {
                if (SteamUGC.GetQueryUGCResult(queryHandle, i, out var details))
                {
                    var steamId = details.m_nPublishedFileId.m_PublishedFileId;

                    // 构建ModDependencyInfo对象（所有字段均从批量查询中获取，无需额外单独查询）
                    var modInfo = new SteamModDependencyInfo
                    {
                        SteamId = steamId,
                        IsSubscribed = false, // 稍后会更新
                        OnlineUpdateTime =
                            DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeUpdated).UtcDateTime, // 批量获取的更新时间
                        Title = details.m_rgchTitle, // 批量获取的标题
                        DependencyIds = []
                    };

                    // 批量获取依赖项信息（Children）
                    var numChildren = details.m_unNumChildren;
                    if (numChildren > 0)
                    {
                        var childIds = new PublishedFileId_t[numChildren];
                        if (SteamUGC.GetQueryUGCChildren(queryHandle, i, childIds, numChildren))
                        {
                            for (uint j = 0; j < numChildren; j++)
                            {
                                modInfo.DependencyIds.Add(childIds[j].m_PublishedFileId);
                            }
                        }
                    }

                    allMods.Add(modInfo);

                    // 如果设置了最大数量且已达到，提前退出内层循环
                    if (maxMods.HasValue && allMods.Count >= maxMods.Value)
                    {
                        break;
                    }
                }
            }

            SteamUGC.ReleaseQueryUGCRequest(queryHandle);

            // 判断是否继续翻页
            if (numResults < pageSize)
            {
                // 当前页未满，说明已到最后一页
                Log.Debug($"[BmlSteamItemManager] 第 {page} 页未满（{numResults}/{pageSize}），已到达最后一页");
                break;
            }

            if (totalMatchingMods.HasValue && allMods.Count >= totalMatchingMods.Value)
            {
                // 已获取所有匹配的mod
                Log.Debug($"[BmlSteamItemManager] 已获取所有 {totalMatchingMods.Value} 个匹配的mod");
                break;
            }

            page++;
        }

        Log.Debug($"[BmlSteamItemManager] mod批量查询完成，共获取 {allMods.Count} 个mod信息（Title、更新时间、依赖项均已批量获取）");

        return allMods;
    }

    /// <summary>
    /// 获取本地安装时间
    /// </summary>
    /// <param name="steamId">Steam物品ID</param>
    /// <returns>本地安装时间（UTC），如果未安装则返回null</returns>
    private DateTime? GetLocalInstallTime(ulong steamId)
    {
        var fileId = new PublishedFileId_t(steamId);
        var itemState = SteamUGC.GetItemState(fileId);

        if ((itemState & (uint)EItemState.k_EItemStateInstalled) != 0)
        {
            if (SteamUGC.GetItemInstallInfo(fileId, out var sizeOnDisk, out var folder, 1024, out var timeStamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(timeStamp).UtcDateTime;
            }
        }

        return null;
    }


    public static event Action<WorkshopUploadProgress>? OnUploadProgress;
}
