using Newbe.BetterModLoader.Audio;

namespace Newbe.BetterModLoader.DeveloperModes;

/// <summary>
/// 列表项 UI，约定 prefab 包含子对象
/// - NameText (TextMeshProUGUI)
/// - Background (Image) 可选，用于高亮选中
/// </summary>
internal class LocalModListItemUI : MonoBehaviour
{
    private LocalModListItem _localModListItem = null!;

    // 用于存放该 UI 实例的订阅
    private DisposableBag _disposables;

    // IdText 已移除
    [SerializeField] public TextMeshProUGUI NameText = null!;
    [SerializeField] public Image Background = null!;
    [SerializeField] public Image PreviewImage = null!; // 小预览图（参考 ModListView 风格）

    /// <summary>
    /// 被点击时回调（由列表容器分派）
    /// </summary>
    public event Action<LocalModListItemUI>? OnClicked;

    public void SetLocalModListItem(LocalModListItem item)
    {
        // 先释放之前的订阅
        _disposables.Clear();

        // 修复：确保 Button 事件绑定
        var button = GetComponent<Button>();
        if (button != null)
        {
            button
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                    HandleClicked();
                })
                .AddTo(ref _disposables);
        }

        _localModListItem = item;

        // IdText 已移除，不再处理

        if (NameText != null)
        {
            var mi = _localModListItem.OriginalModInfo.Value;
            // 使用 game 原始 ModInfo 字段：displayName 优先，否则使用 name
            NameText.text = (mi.DisplayName ?? mi.Name) ?? string.Empty;
            _localModListItem.OriginalModInfo
                .Subscribe(m => { NameText.text = (m.DisplayName ?? m.Name); })
                .AddTo(ref _disposables);
        }

        // 预览图（如有）
        if (PreviewImage != null)
        {
            var tex = _localModListItem.Preview.Value;
            if (tex != null)
            {
                // 将贴图包装为 sprite 显示
                try
                {
                    PreviewImage.sprite =
                        Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    PreviewImage.color = Color.white;
                }
                catch
                {
                    PreviewImage.sprite = null;
                    PreviewImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
            else
            {
                PreviewImage.sprite = null;
                PreviewImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            _localModListItem.Preview
                .Subscribe(t =>
                {
                    if (t != null)
                    {
                        try
                        {
                            PreviewImage.sprite =
                                Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                            PreviewImage.color = Color.white;
                        }
                        catch
                        {
                            PreviewImage.sprite = null;
                            PreviewImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                        }
                    }
                    else
                    {
                        PreviewImage.sprite = null;
                        PreviewImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                })
                .AddTo(ref _disposables);
        }
    }

    public LocalModListItem GetLocalModListItem() => _localModListItem;

    public void SetSelected(bool selected)
    {
        // LeftStripe 功能已移除，不再显示选中状态的颜色条

        // 保持 Background 透明以避免覆盖视觉效果
        if (Background != null)
        {
            Background.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    // Unity UI Button 可以调用此方法，或者你也可以通过 EventTrigger 调用
    public void HandleClicked()
    {
        OnClicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
