namespace Newbe.BetterModLoader;

/// <summary>
/// Tag constants for ModGroup.
/// </summary>
internal static class ModGroupTags
{
    /// <summary>
    /// Tag for the default unsorted group.
    /// </summary>
    public const string Default = "default";

    /// <summary>
    /// Tag for groups that must stay at the top of the list.
    /// Other groups cannot be moved above the top boundary.
    /// </summary>
    public const string Top = "top";

    /// <summary>
    /// Tag for the Better Mod Loader group, useful for identifying/searching this special group.
    /// </summary>
    public const string BetterModLoader = "better-mod-loader";

    /// <summary>
    /// Tag for locked groups: no adding/removing mods, no reordering within the group.
    /// </summary>
    public const string Locked = "locked";

    /// <summary>
    /// Tag for the group containing mods that were enabled during migration from the old mod manager.
    /// </summary>
    public const string EnablesInOldManager = "enables-in-old-manager";
}

/// <summary>
/// Represents a group of mods with custom ordering.
/// </summary>
[Serializable]
internal class ModGroup
{
    /// <summary>
    /// Display name of the group.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Description of the group.
    /// </summary>
    public string Description = string.Empty;

    /// <summary>
    /// List of extended mod information in this group, in order.
    /// </summary>

    public IReadOnlyList<ModInfo2> Mods => _mods;

    /// <summary>
    /// Tags for this group (e.g., "default" for the unsorted group).
    /// </summary>
    public HashSet<string> Tags = [];

    [SerializeField] private List<ModInfo2> _mods = [];

    public void AddMod(ModInfo2 modInfo) => AddModAt(modInfo, Mods.Count);

    public void AddModAt(ModInfo2 modInfo, int index)
    {
        if (modInfo == null) throw new ArgumentNullException(nameof(modInfo));
        _mods.Insert(index, modInfo);
    }

    public void RemoveMod(ModInfo2 modInfo)
    {
        if (modInfo == null) throw new ArgumentNullException(nameof(modInfo));
        _mods.RemoveAll(x => x.Id == modInfo.Id);
    }

    public void PopulateMods(IEnumerable<ModInfo2> mods)
    {
        if (mods == null) throw new ArgumentNullException(nameof(mods));
        _mods.Clear();
        _mods.AddRange(mods);
    }

    public int IndexOfMod(ModInfo2 modInfo)
    {
        if (modInfo == null) throw new ArgumentNullException(nameof(modInfo));
        return _mods.FindIndex(x => x.Id == modInfo.Id);
    }

    /// <summary>
    /// Special flag for the default "Unsorted" group.
    /// Checks if the "default" tag exists.
    /// </summary>
    public bool IsDefaultGroup
    {
        get => Tags.Contains(ModGroupTags.Default);
        set
        {
            if (value)
            {
                Tags.Add(ModGroupTags.Default);
            }
            else
            {
                Tags.Remove(ModGroupTags.Default);
            }
        }
    }

    public ModGroup()
    {
    }

    public ModGroup(string name, IEnumerable<ModInfo2>? mods = null!)
    {
        Name = name;
        if (mods != null)
        {
            _mods = mods.ToList();
        }
    }
}
