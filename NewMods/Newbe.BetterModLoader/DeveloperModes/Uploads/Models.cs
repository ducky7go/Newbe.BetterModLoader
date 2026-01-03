namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

/// <summary>
/// 上传面板主数据模型
/// </summary>
internal class UploadPanelData(ModMetadata metadata)
{
    public ModMetadata Metadata { get; set; } = metadata;
    public UploadForm Form { get; set; } = new();
    public ChecklistItem[] Checklist { get; set; } = [];
}

/// <summary>
/// 校验清单项
/// </summary>
internal class ChecklistItem
{
    public string Id { get; set; } = string.Empty;
    public ChecklistStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string FailReason { get; set; } = string.Empty;

    public static ChecklistItem IniCheck(ChecklistStatus status)
    {
        return new ChecklistItem
        {
            Id = nameof(IniCheck),
            Status = status,
            Description = L.Upload.CheckListIniCheckDescription,
            FailReason = L.Upload.CheckListIniCheckFailReason
        };
    }

    public static ChecklistItem SteamIdCheck(ChecklistStatus status)
    {
        return new ChecklistItem
        {
            Id = nameof(SteamIdCheck),
            Status = status,
            Description = L.Upload.CheckListSteamIdCheckDescription,
            FailReason = L.Upload.CheckListSteamIdCheckFailReason
        };
    }


    public static ChecklistItem MultiLanguageDescriptionCheck(ChecklistStatus status)
    {
        return new ChecklistItem
        {
            Id = nameof(MultiLanguageDescriptionCheck),
            Status = status,
            Description = L.Upload.CheckListMultiLanguageDescriptionCheckDescription,
            FailReason = L.Upload.CheckListMultiLanguageDescriptionCheckFailReason
        };
    }

    public static ChecklistItem MultiLanguageTitleCheck(ChecklistStatus status)
    {
        return new ChecklistItem
        {
            Id = nameof(MultiLanguageTitleCheck),
            Status = status,
            Description = L.Upload.CheckListMultiLanguageTitleCheckDescription,
            FailReason = L.Upload.CheckListMultiLanguageTitleCheckFailReason
        };
    }


    public static ChecklistItem PreviewImageCheck(ChecklistStatus status)
    {
        return new ChecklistItem
        {
            Id = nameof(PreviewImageCheck),
            Status = status,
            Description = L.Upload.CheckListPreviewImageCheckDescription,
            FailReason = L.Upload.CheckListPreviewImageCheckFailReason
        };
    }

    // 新增：版本验证清单项
    public static ChecklistItem VersionCheck(ChecklistStatus status)
    {
        return new ChecklistItem
        {
            Id = nameof(VersionCheck),
            Status = status,
            Description = L.Upload.CheckListVersionCheckDescription,
            FailReason = L.Upload.CheckListVersionCheckFailReason
        };
    }

    // 新增：预览图片大小验证清单项
    public static ChecklistItem PreviewImageSizeCheck(ChecklistStatus status, string? language = null)
    {
        return new ChecklistItem
        {
            Id = nameof(PreviewImageSizeCheck),
            Status = status,
            Description = L.Upload.CheckListPreviewImageSizeCheckDescription,
            FailReason = string.IsNullOrEmpty(language)
                ? L.Upload.CheckListPreviewImageSizeCheckFailReason
                : string.Format(L.Upload.CheckListPreviewImageSizeCheckFailReason, language)
        };
    }

    // 描述大小验证清单项
    public static ChecklistItem DescriptionSizeCheck(ChecklistStatus status, string language = "")
    {
        return new ChecklistItem
        {
            Id = nameof(DescriptionSizeCheck),
            Status = status,
            Description = string.IsNullOrEmpty(language)
                ? L.Upload.CheckListDescriptionSizeCheckDescription
                : string.Format(L.Upload.CheckListDescriptionSizeCheckDescription, language),
            FailReason = string.IsNullOrEmpty(language)
                ? L.Upload.CheckListDescriptionSizeCheckFailReason
                : string.Format(L.Upload.CheckListDescriptionSizeCheckFailReason, language)
        };
    }
}

public enum ChecklistStatus
{
    Success,
    Fail,
    Unavailable
}

/// <summary>
/// Steam Workshop Tags 选择结果（纯数据结构）
/// </summary>
internal class WorkshopTags
{
    /// <summary>
    /// 武器相关（Weapon）：包含新增或修改的武器、枪械、近战武器等内容。
    /// </summary>
    public bool Weapon { get; set; }

    /// <summary>
    /// 装备与物资（Equipment & Gear）：包含背包、防具、附件、战术装备等。
    /// </summary>
    public bool EquipmentAndGear { get; set; }

    /// <summary>
    /// 掉落与经济（Loot & Economy）：影响战利品分布、交易、金钱与资源获取的改动。
    /// </summary>
    public bool LootAndEconomy { get; set; }

    /// <summary>
    /// 生活质量（Quality of Life）：提升使用便捷性、界面优化、操作改进等非数值平衡改动。
    /// </summary>
    public bool QualityOfLife { get; set; }

    /// <summary>
    /// 作弊与利用（Cheats & Exploits）：包含调试、作弊、跳过机制或利用系统漏洞的功能。
    /// </summary>
    public bool CheatsAndExploits { get; set; }

    /// <summary>
    /// 视觉增强（Visual Enhancements）：材质、光影、界面皮肤、特效等视觉相关增强。
    /// </summary>
    public bool VisualEnhancements { get; set; }

    /// <summary>
    /// 声音（Sound）：音乐、环境音效、武器音效、语音等音频相关改动。
    /// </summary>
    public bool Sound { get; set; }

    /// <summary>
    /// 任务与进程（Quest & Progression）：任务系统、成长线路、阶段推进相关内容。
    /// </summary>
    public bool QuestAndProgression { get; set; }

    /// <summary>
    /// 同伴与 NPC（Companion & NPC）：新增或修改随从、NPC 行为、交互等。
    /// </summary>
    public bool CompanionAndNPC { get; set; }

    /// <summary>
    /// 收集品（Collectibles）：成就物品、特殊收藏、稀有元素等可收集目标。
    /// </summary>
    public bool Collectibles { get; set; }

    /// <summary>
    /// 游戏玩法（Gameplay）：核心机制、战斗系统、移动与交互等广义玩法改动。
    /// </summary>
    public bool Gameplay { get; set; }

    /// <summary>
    /// 多人与联机（Multiplayer & Co-op）：与多人协作或联机交互相关的功能。
    /// </summary>
    public bool MultiplayerAndCoOp { get; set; }

    /// <summary>
    /// 实用工具（Utility）：辅助脚本、调试工具、分析、性能或管理类功能。
    /// </summary>
    public bool Utility { get; set; }

    /// <summary>
    /// 医疗与生存（Medical & Survival）：生命值、治疗、状态、生存系统相关的改动。
    /// </summary>
    public bool MedicalAndSurvival { get; set; }

    public void Fill(string[] tagsStr)
    {
        foreach (var tag in tagsStr)
        {
            switch (tag)
            {
                case "Weapon":
                    Weapon = true;
                    break;
                case "Equipment & Gear":
                    EquipmentAndGear = true;
                    break;
                case "Loot & Economy":
                    LootAndEconomy = true;
                    break;
                case "Quality of Life":
                    QualityOfLife = true;
                    break;
                case "Cheats & Exploits":
                    CheatsAndExploits = true;
                    break;
                case "Visual Enhancements":
                    VisualEnhancements = true;
                    break;
                case "Sound":
                    Sound = true;
                    break;
                case "Quest & Progression":
                    QuestAndProgression = true;
                    break;
                case "Companion & NPC":
                    CompanionAndNPC = true;
                    break;
                case "Collectibles":
                    Collectibles = true;
                    break;
                case "Gameplay":
                    Gameplay = true;
                    break;
                case "Multiplayer & Co-op":
                    MultiplayerAndCoOp = true;
                    break;
                case "Utility":
                    Utility = true;
                    break;
                case "Medical & Survival":
                    MedicalAndSurvival = true;
                    break;
            }
        }
    }
}

/// <summary>
/// WorkshopTags 扩展方法，用于转换为 Steam API 所需的标签字符串数组。
/// 仅返回值为 true 的标签，若全部为 false 则返回空数组。
/// </summary>
internal static class WorkshopTagsExtensions
{
    /// <summary>
    /// 将 WorkshopTags 转换为 Steam API 需要的标签字符串数组。
    /// 仅包含被选中的（值为 true）标签，顺序遵循定义顺序。
    /// </summary>
    /// <param name="tags">用户选择的标签结构</param>
    /// <returns>Steam API 所需的标签字符串数组；若无选择返回空数组。</returns>
    public static string[] ToSteamTags(this WorkshopTags tags)
    {
        List<string> list = new List<string>();
        if (tags.Weapon) list.Add("Weapon");
        if (tags.EquipmentAndGear) list.Add("Equipment & Gear");
        if (tags.LootAndEconomy) list.Add("Loot & Economy");
        if (tags.QualityOfLife) list.Add("Quality of Life");
        if (tags.CheatsAndExploits) list.Add("Cheats & Exploits");
        if (tags.VisualEnhancements) list.Add("Visual Enhancements");
        if (tags.Sound) list.Add("Sound");
        if (tags.QuestAndProgression) list.Add("Quest & Progression");
        if (tags.CompanionAndNPC) list.Add("Companion & NPC");
        if (tags.Collectibles) list.Add("Collectibles");
        if (tags.Gameplay) list.Add("Gameplay");
        if (tags.MultiplayerAndCoOp) list.Add("Multiplayer & Co-op");
        if (tags.Utility) list.Add("Utility");
        if (tags.MedicalAndSurvival) list.Add("Medical & Survival");
        return list.ToArray();
    }
}

/// <summary>
/// 上传表单数据
/// </summary>
internal class UploadForm
{
    public string ChangeNotes { get; set; } = string.Empty;
    public bool AllowBmlTail { get; set; }
    public bool UpdateSteamDescription { get; set; }

    /// <summary>
    /// 用户在上传时选择的 Steam Workshop 标签集合。
    /// </summary>
    public WorkshopTags Tags { get; set; } = null!;

    // 新增：版本字段
    public string Version { get; set; } = string.Empty;

    // 新增：从 info.ini 自动填充的标记
    public bool TagsAutoPopulated { get; set; }
    public bool VersionAutoPopulated { get; set; }
}
