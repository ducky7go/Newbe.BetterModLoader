namespace Newbe.BetterModLoader;

public record ModMetadata
{
    public ModMetadata(ModInfo info)
    {
        Id = info.GetModId();
        ModFolder = info.path;
        Name = info.name;
        DisplayName = info.displayName;
        Description = info.description;
        DllFound = info.dllFound;
        IsSteamItem = info.isSteamItem;
        PublishedFileId = info.publishedFileId;

        // 新增：版本和标签字段
        Version = info.version == "?" ? "N/A" : info.version;
        Tags = string.IsNullOrEmpty(info.tags)
            ? "No tags"
            : string.Join(", ",
                info.tags.Split([','], StringSplitOptions.RemoveEmptyEntries)
            );
    }

    public string Id { get; }
    public string ModFolder { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public bool DllFound { get; }
    public bool IsSteamItem { get; }
    public ulong PublishedFileId { get; set; }

    // 新增：版本和标签属性
    public string Version { get; }
    public string Tags { get; }

    public string DllPath => Path.Combine(ModFolder, Name + ".dll");
    public string DescriptionFilePath => Path.Combine(ModFolder, "description.md");
    public string PreviewImagePath => Path.Combine(ModFolder, "preview.png");

    public string DescriptionOrFileContent
    {
        get
        {
            var descFile = DescriptionFilePath;
            if (File.Exists(descFile))
            {
                return File.ReadAllText(descFile);
            }

            return Description;
        }
    }
}
