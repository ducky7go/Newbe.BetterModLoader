namespace Newbe.BetterModLoader.DeveloperModes;

/// <summary>
/// 列表容器 UI：负责用 prefab 实例化列表项并管理选中状态
/// </summary>
internal class LocalModListUI : MonoBehaviour
{
    public Transform ContentParent = null!;
    public GameObject LocalModListItemPrefab = null!;

    private LocalModItemList _localModItemList = null!;

    // 用于存放该 UI 实例的订阅（将来扩展用）
    private DisposableBag _disposables = new();

    private readonly List<LocalModListItemUI> _itemUIs = [];

    /// <summary>
    /// 项被选中时触发，参数为对应的数据模型
    /// </summary>
    public event Action<LocalModListItem>? OnItemSelected;

    public void SetLocalModItemList(LocalModItemList itemList)
    {
        _localModItemList = itemList;
        RebuildList();
    }

    private void RebuildList()
    {
        // 清理已有 UI
        foreach (var ui in _itemUIs)
        {
            if (ui != null)
            {
                Destroy(ui.gameObject);
            }
        }

        _itemUIs.Clear();

        if (_localModItemList == null || _localModItemList.Items == null) return;

        foreach (var item in _localModItemList.Items)
        {
            var go = Instantiate(LocalModListItemPrefab, ContentParent);
            var ui = go.GetComponent<LocalModListItemUI>();
            if (ui == null) continue;

            ui.SetLocalModListItem(item);
            ui.OnClicked += OnItemUIClicked;
            ui.SetSelected(false);
            _itemUIs.Add(ui);
        }

        // 默认不选中任何项
        // if (_itemUIs.Count > 0)
        // {
        //     SelectIndex(0);
        // }
    }

    private void OnItemUIClicked(LocalModListItemUI ui)
    {
        var item = ui.GetLocalModListItem();
        OnItemSelected?.Invoke(item);
        foreach (var i in _itemUIs)
        {
            i.SetSelected(i == ui);
        }
    }

    public void SelectIndex(int index)
    {
        if (index < 0 || index >= _itemUIs.Count) return;
        var ui = _itemUIs[index];
        OnItemSelected?.Invoke(ui.GetLocalModListItem());
        foreach (var i in _itemUIs)
        {
            i.SetSelected(i == ui);
        }
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
