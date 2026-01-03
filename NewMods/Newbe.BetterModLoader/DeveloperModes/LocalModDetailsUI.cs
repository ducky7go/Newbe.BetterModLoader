namespace Newbe.BetterModLoader.DeveloperModes;

/// <summary>
/// 详情面板 UI，约定 prefab 包含 NameText, DescriptionText 两个 TextMeshProUGUI
/// </summary>
internal class LocalModDetailsUI : MonoBehaviour
{
    public LocalModListItem? CurrentItem { get; private set; }

    // 用于存放该 UI 实例的订阅
    private DisposableBag _disposables = new();

    [SerializeField] public TextMeshProUGUI NameText = null!;
    [SerializeField] public TextMeshProUGUI DisplayNameText = null!;

    [SerializeField] public TextMeshProUGUI TagsText = null!;

    // 新增：版本显示字段
    [SerializeField] public TextMeshProUGUI VersionText = null!;
    [SerializeField] public Transform LanguageDetectionPanel = null!;
    [SerializeField] public Transform DependenciesPanel = null!;
    [SerializeField] public Image PreviewImage = null!;

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// 从列表项数据更新详情显示（用于简单快速更新，不依赖 LocalModDetails 对象）
    /// </summary>
    public void UpdateFrom(LocalModListItem item)
    {
        CurrentItem = item;
        if (item == null)
        {
            // 新增：重置所有详情字段内容，避免残留
            if (DisplayNameText != null) DisplayNameText.text = string.Empty;
            if (NameText != null) NameText.text = string.Empty;
            if (TagsText != null) TagsText.text = string.Empty;
            if (VersionText != null) VersionText.text = string.Empty;
            if (LanguageDetectionPanel != null)
            {
                foreach (Transform child in LanguageDetectionPanel)
                {
                    Destroy(child.gameObject);
                }
            }

            if (DependenciesPanel != null)
            {
                foreach (Transform child in DependenciesPanel)
                {
                    Destroy(child.gameObject);
                }
            }

            if (PreviewImage != null)
            {
                PreviewImage.sprite = null;
                PreviewImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            return;
        }

        var mi = item.OriginalModInfo.Value;
        var displayName = mi.DisplayName ?? string.Empty;
        var modName = mi.Name ?? string.Empty;
        // 第一行横排：左图片，右侧竖排显示"显示名称"和"名称"
        if (PreviewImage != null && item.Preview.Value != null)
        {
            var tex = item.Preview.Value;
            PreviewImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            PreviewImage.color = Color.white;
        }

        if (DisplayNameText != null)
        {
            DisplayNameText.text = displayName;
        }
        else
        {
            Log.Error("[UpdateFrom] DisplayNameText is NULL!");
        }

        if (NameText != null)
        {
            NameText.text = modName;
        }
        else
        {
            Log.Error("[UpdateFrom] NameText is NULL!");
        }

        // 新增：显示版本信息
        if (VersionText != null)
        {
            VersionText.text = mi.Version;
        }
        else
        {
            Log.Error("[UpdateFrom] VersionText is NULL!");
        }

        // Force layout update after setting text
        Canvas.ForceUpdateCanvases();

        // 修改：使用动态标签替代硬编码的 "Mod"
        if (TagsText != null)
        {
            TagsText.text = mi.Tags;
        }
        else
        {
            Log.Error("[UpdateFrom] TagsText is NULL!");
        }

        // Create language detection table
        if (LanguageDetectionPanel != null)
        {
            // Clear existing table content first
            foreach (Transform child in LanguageDetectionPanel)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            CreateLanguageDetectionTable(LanguageDetectionPanel, item);
            Log.Debug($"[LocalModDetailsUI] Created language detection table for mod: {modName}");
        }
        else
        {
            Log.Error("[LocalModDetailsUI] LanguageDetectionPanel is null!");
        }

        if (DependenciesPanel != null)
        {
            foreach (Transform child in DependenciesPanel)
            {
                Destroy(child.gameObject);
            }

            // TODO update dependencies from ModInfo if available
            var dependencies = new List<ModDependencyInfo>
            {
                new ModDependencyInfo("game", ">1.33.0")
            };
            if (dependencies != null)
            {
                foreach (var dep in dependencies)
                {
                    var go = new GameObject("DependencyItem");
                    go.transform.SetParent(DependenciesPanel, false);
                    var layout = go.AddComponent<HorizontalLayoutGroup>();
                    layout.childAlignment = TextAnchor.MiddleLeft;
                    layout.spacing = 8f;
                    var nameObj = new GameObject("DepName");
                    nameObj.transform.SetParent(go.transform, false);
                    var nameText = nameObj.AddComponent<TextMeshProUGUI>();
                    nameText.text = dep.Name;
                    nameText.fontSize = 18;
                    nameText.color = Color.white;
                    var condObj = new GameObject("DepCond");
                    condObj.transform.SetParent(go.transform, false);
                    var condText = condObj.AddComponent<TextMeshProUGUI>();
                    condText.text = dep.Condition;
                    condText.fontSize = 16;
                    condText.color = Color.gray;
                }
            }
        }
    }

    /// <summary>
    /// Creates a table showing detected language files for titles and descriptions
    /// </summary>
    private static void CreateLanguageDetectionTable(Transform parent, LocalModListItem item)
    {
        Log.Debug($"[CreateLanguageDetectionTable] Starting table creation for item: {item.Id.Value}");
        // Table title
        var titleGO = new GameObject("TableTitle");
        titleGO.transform.SetParent(parent, false);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Language Files Detection";
        titleText.fontSize = 18;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;

        // Table header row
        var headerRowGO = new GameObject("HeaderRow");
        headerRowGO.transform.SetParent(parent, false);
        var headerLayout = headerRowGO.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.padding = new RectOffset(5, 5, 5, 5);
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;

        // Header cells
        CreateHeaderCell(headerRowGO.transform, "Lang Code", 80f);
        CreateHeaderCell(headerRowGO.transform, "Title in title/*.md", 200f);
        CreateHeaderCell(headerRowGO.transform, "Description in description/*.md", 120f);

        // Language code mappings (same as in MultiLanguageContentHelper)
        var languageMap = new Dictionary<string, string>
        {
            { "en", "english" },
            { "zh", "schinese" },
            { "zh-hant", "tchinese" },
            { "ja", "japanese" },
            { "ko", "koreana" },
            { "ru", "russian" },
            { "de", "german" },
            { "fr", "french" },
            { "es", "latam" },
            { "pt", "brazilian" }
        };

        // Create rows for each language
        var modInfo = item.OriginalModInfo.Value;
        foreach (var kvp in languageMap)
        {
            var gameLangCode = kvp.Key;

            // Check title file
            var titleFilePath = Path.Combine(modInfo.ModFolder, "title", $"{gameLangCode}.md");
            string titleContent = modInfo.DisplayName ?? "N/A";
            if (File.Exists(titleFilePath))
            {
                try
                {
                    titleContent = File.ReadAllText(titleFilePath).Trim();
                }
                catch (Exception)
                {
                    // Ignore read errors, keep fallback
                }
            }

            // Check description file
            var descFilePath = Path.Combine(modInfo.ModFolder, "description", $"{gameLangCode}.md");
            var descFileExists = File.Exists(descFilePath);
            var descFileName = $"description/{gameLangCode}.md";

            // Create row
            CreateLanguageRow(parent, gameLangCode, titleContent, descFileName, descFileExists);
        }

        Log.Debug($"[CreateLanguageDetectionTable] Table creation completed with {languageMap.Count} languages");
    }

    private static void CreateHeaderCell(Transform parent, string text, float preferredWidth)
    {
        var cellGO = new GameObject("HeaderCell");
        cellGO.transform.SetParent(parent, false);
        var cellText = cellGO.AddComponent<TextMeshProUGUI>();
        cellText.text = text;
        cellText.fontSize = 14;
        cellText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        cellText.fontStyle = FontStyles.Bold;
        cellText.alignment = TextAlignmentOptions.Left;

        var layoutElement = cellGO.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = 25f;
    }

    private static void CreateLanguageRow(Transform parent, string langCode, string titleContent, string descFileName,
        bool descFileExists)
    {
        var rowGO = new GameObject($"Row_{langCode}");
        rowGO.transform.SetParent(parent, false);
        var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10f;
        rowLayout.padding = new RectOffset(5, 5, 5, 5);
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;

        // Lang Code cell
        CreateCell(rowGO.transform, langCode, 80f, new Color(0.7f, 0.9f, 1f, 1f));

        // Title content cell
        CreateCell(rowGO.transform, titleContent, 200f, Color.white);

        // Description file cell - only show if file exists, otherwise leave empty
        var color = descFileExists ? new Color(0.7f, 1f, 0.7f, 1f) : new Color(1f, 0.7f, 0.7f, 1f);
        CreateCell(rowGO.transform, descFileName, 120f, color);
    }

    private static void CreateCell(Transform parent, string text, float preferredWidth, Color textColor)
    {
        var cellGO = new GameObject("Cell");
        cellGO.transform.SetParent(parent, false);
        var cellText = cellGO.AddComponent<TextMeshProUGUI>();
        cellText.text = text;
        cellText.fontSize = 13;
        cellText.color = textColor;
        cellText.alignment = TextAlignmentOptions.Left;
        cellText.enableWordWrapping = true;

        var layoutElement = cellGO.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = 20f;
    }
}
