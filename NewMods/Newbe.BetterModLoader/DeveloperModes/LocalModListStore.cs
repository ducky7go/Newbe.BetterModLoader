namespace Newbe.BetterModLoader.DeveloperModes;

/// <summary>
/// 列表数据容器
/// </summary>
[Serializable]
internal class LocalModItemList
{
    // 使用标准 List，避免运行时缺少 ObservableList 类型导致的 TypeLoadException
    public IReadOnlyList<LocalModListItem> Items { get; }

    public ReactiveProperty<LocalModListItem> Selected { get; }

    public LocalModItemList(List<LocalModListItem> items)
    {
        Items = items ?? [];
        Selected = new ReactiveProperty<LocalModListItem>();
    }

    public void SetSelected(LocalModListItem item)
    {
        Selected.Value = item;
    }
}
