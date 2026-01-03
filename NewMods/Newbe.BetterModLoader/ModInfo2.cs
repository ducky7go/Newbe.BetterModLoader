namespace Newbe.BetterModLoader;

/// <summary>
/// Extended mod information based on the original ModInfo.
/// Adds unique ID and tagging system for better categorization.
/// </summary>
[Serializable]
internal class ModInfo2
{
    /// <summary>
    /// Unique identifier for the mod.
    /// Format: "steam_{publishedFileId}" for Steam Workshop mods, "local_{name}" for local mods.
    /// </summary>
    public string Id = string.Empty;

    /// <summary>
    /// Reference to the original ModInfo from the game.
    /// This is not serialized - rebuilt during Rescan.
    /// </summary>
    [NonSerialized] public ModInfo OriginalModInfo;

    /// <summary>
    /// Collection of tags for categorization and filtering.
    /// Examples: "harmony-based", "item-expansion", "ui-mod", "gameplay-mod"
    /// </summary>
    public HashSet<string> Tags = [];

    /// <summary>
    /// Whether this mod is enabled (should be activated on startup).
    /// </summary>
    public bool IsEnabled;

    public ModInfo2()
    {
    }

    public ModInfo2(ModInfo originalModInfo)
    {
        OriginalModInfo = originalModInfo;
        Id = originalModInfo.GetModId();
        Tags = [];
    }

    /// <summary>
    /// Create ModInfo2 from original ModInfo with tags.
    /// </summary>
    public ModInfo2(ModInfo originalModInfo, HashSet<string> tags) : this(originalModInfo)
    {
        if (tags != null)
        {
            Tags = new HashSet<string>(tags);
        }
    }

    /// <summary>
    /// Check if this mod has a specific tag.
    /// </summary>
    public bool HasTag(string tag)
    {
        return Tags != null && Tags.Contains(tag);
    }

    /// <summary>
    /// Add a tag to this mod.
    /// </summary>
    public void AddTag(string tag)
    {
        if (Tags == null)
        {
            Tags = [];
        }

        Tags.Add(tag);
    }

    /// <summary>
    /// Remove a tag from this mod.
    /// </summary>
    public void RemoveTag(string tag)
    {
        Tags?.Remove(tag);
    }

    /// <summary>
    /// Get display name from original ModInfo.
    /// </summary>
    public string GetDisplayName()
    {
        return OriginalModInfo.name ?? Id;
    }

    /// <summary>
    /// Get preview image from original ModInfo.
    /// </summary>
    public Texture2D? GetPreview()
    {
        return OriginalModInfo.preview;
    }

    public bool IsSteamWorkshopMod()
    {
        return OriginalModInfo.isSteamItem;
    }
}
