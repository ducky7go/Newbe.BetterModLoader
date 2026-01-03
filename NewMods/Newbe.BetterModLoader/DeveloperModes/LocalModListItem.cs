namespace Newbe.BetterModLoader.DeveloperModes;

/// <summary>
/// 数据模型：本地 mod 列表项
/// </summary>
[Serializable]
internal class LocalModListItem
{
    public ReactiveProperty<string> Id { get; }

    public ReactiveProperty<ModMetadata> OriginalModInfo { get; }

    // Optional preview texture for list visual (来自 ModInfo2.GetPreview())
    public ReactiveProperty<Texture2D?> Preview { get; }

    public LocalModListItem(
        string id,
        ModInfo originalModInfo,
        Texture2D? preview = null)
    {
        Id = new ReactiveProperty<string>(id);
        OriginalModInfo = new ReactiveProperty<ModMetadata>(new ModMetadata(originalModInfo));
        Preview = new ReactiveProperty<Texture2D?>(preview);
    }
}
