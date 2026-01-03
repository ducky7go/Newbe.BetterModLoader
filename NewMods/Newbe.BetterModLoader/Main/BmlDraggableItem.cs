using Newbe.BetterModLoader.Audio;

namespace Newbe.BetterModLoader.Main;

internal class BmlDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BmlModManagerUIView _view;
    private string _type; // "group" or "mod"
    private string _id;

    private Transform _originalParent;
    private int _originalIndex;
    private int _currentIndex;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private bool _isDragging;
    private Vector3 _originalScale;
    private bool _isHoveredByDrag = false;
    private int _dataIndex; // Actual position in the data list

    /// <summary>
    /// Event fired when a mod is dropped on a group
    /// </summary>
    public static event Action<BmlDraggableItem, BmlDraggableItem>? OnModDroppedOnGroup;

    /// <summary>
    /// Event fired when an item is reordered within its list
    /// </summary>
    public static event Action<BmlDraggableItem, int, int>? OnItemReordered;

    public string DataId => _id;
    public string ItemType => _type;

    public void Init(BmlModManagerUIView view, string type, string id, int dataIndex = -1)
    {
        _view = view;
        _type = type;
        _id = id;
        _dataIndex = dataIndex; // Store the actual data index
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        _originalIndex = transform.GetSiblingIndex();
        _currentIndex = _originalIndex;
        _isDragging = true;

        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false;

        // Play drag start sound
        BMLAudioManager.PlaySoundStatic(AudioEffectType.DragStart);

        Log.Debug($"[BmlDraggableItem] ===== BEGIN DRAG =====");
        Log.Debug($"[BmlDraggableItem] ID: '{_id}', Type: '{_type}'");
        Log.Debug($"[BmlDraggableItem] Original Index: {_originalIndex}");
        Log.Debug($"[BmlDraggableItem] Parent: {_originalParent?.name}, Child Count: {_originalParent?.childCount}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Update sibling index during drag for visual feedback
        UpdateSiblingIndexDuringDrag(eventData);

        // Check for group hover (for visual feedback)
        if (_type == "mod")
        {
            CheckHoverOverGroupItems(eventData);
            ProcessAutoScroll();
        }
    }

    private void UpdateSiblingIndexDuringDrag(PointerEventData eventData)
    {
        if (_originalParent == null) return;

        // Find items under the pointer
        List<RaycastResult> results = new();
        EventSystem.current?.RaycastAll(eventData, results);

        BmlDraggableItem? hoveredItem = null;
        foreach (var result in results)
        {
            if (result.gameObject == gameObject) continue;

            var item = result.gameObject.GetComponent<BmlDraggableItem>() ??
                       result.gameObject.GetComponentInParent<BmlDraggableItem>();

            // Only reorder within same parent and same type
            if (item != null && item.transform.parent == _originalParent && item._type == _type)
            {
                hoveredItem = item;
                break;
            }
        }

        if (hoveredItem != null)
        {
            var hoveredIndex = hoveredItem.transform.GetSiblingIndex();
            var currentIndex = transform.GetSiblingIndex();

            // Determine insert position based on mouse position
            var hoveredRect = hoveredItem.GetComponent<RectTransform>();
            if (hoveredRect != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    hoveredRect, eventData.position, eventData.pressEventCamera, out var localPoint);

                // If mouse is in upper half, insert before; otherwise insert after
                var targetIndex = localPoint.y > 0 ? hoveredIndex : hoveredIndex + 1;

                // Adjust for current position
                if (currentIndex < targetIndex)
                {
                    targetIndex--;
                }

                if (targetIndex != currentIndex && targetIndex >= 0 && targetIndex < _originalParent.childCount)
                {
                    Log.Debug($"[BmlDraggableItem] Moving '{_id}' from sibling {currentIndex} to {targetIndex}");
                    transform.SetSiblingIndex(targetIndex);
                    _currentIndex = targetIndex;
                    Log.Debug($"[BmlDraggableItem] New _currentIndex: {_currentIndex}");
                }
            }
        }
    }

    private ScrollRect _scrollRect;
    private const float EdgeScrollThreshold = 80f;
    private const float MaxScrollSpeed = 400f;
    private float _autoScrollVelocity = 0f;
    private Vector2 _lastPointerPosition;

    private void Update()
    {
        if (_isDragging && _autoScrollVelocity != 0f && _scrollRect != null)
        {
            var currentPos = _scrollRect.verticalNormalizedPosition;
            var contentHeight = _scrollRect.content != null ? _scrollRect.content.rect.height : 0f;
            var viewportHeight = _scrollRect.viewport.rect.height;

            if (contentHeight > viewportHeight)
            {
                var scrollDelta = (_autoScrollVelocity * Time.deltaTime) / (contentHeight - viewportHeight);
                var newPos = Mathf.Clamp01(currentPos + scrollDelta);
                _scrollRect.verticalNormalizedPosition = newPos;
            }
        }
    }

    private void ProcessAutoScroll()
    {
        if (_scrollRect == null)
        {
            _scrollRect = _view.GetModScrollRect();
            if (_scrollRect == null) return;
        }

        var viewport = _scrollRect.viewport;
        if (viewport == null) return;

        var viewportCorners = new Vector3[4];
        viewport.GetWorldCorners(viewportCorners);

        var viewportTop = viewportCorners[1].y;
        var viewportBottom = viewportCorners[0].y;
        var mouseY = Input.mousePosition.y;

        _autoScrollVelocity = 0f;

        if (mouseY > viewportTop)
        {
            var distanceAbove = mouseY - viewportTop;
            var t = Mathf.Clamp01(distanceAbove / EdgeScrollThreshold);
            _autoScrollVelocity = MaxScrollSpeed * t;
        }
        else if (mouseY < viewportBottom)
        {
            var distanceBelow = viewportBottom - mouseY;
            var t = Mathf.Clamp01(distanceBelow / EdgeScrollThreshold);
            _autoScrollVelocity = -MaxScrollSpeed * t;
        }
        else
        {
            var distanceToTop = viewportTop - mouseY;
            var distanceToBottom = mouseY - viewportBottom;

            if (distanceToTop < EdgeScrollThreshold)
            {
                var t = 1f - (distanceToTop / EdgeScrollThreshold);
                _autoScrollVelocity = MaxScrollSpeed * t;
            }
            else if (distanceToBottom < EdgeScrollThreshold)
            {
                var t = 1f - (distanceToBottom / EdgeScrollThreshold);
                _autoScrollVelocity = -MaxScrollSpeed * t;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _isDragging = false;
        _autoScrollVelocity = 0f;

        // Play drag release sound
        BMLAudioManager.PlaySoundStatic(AudioEffectType.DragRelease);

        var finalIndex = transform.GetSiblingIndex();
        var movedToGroup = false;

        Log.Debug($"[BmlDraggableItem] ===== END DRAG =====");
        Log.Debug($"[BmlDraggableItem] ID: '{_id}', Type: '{_type}'");
        Log.Debug($"[BmlDraggableItem] Original Index: {_originalIndex}");
        Log.Debug($"[BmlDraggableItem] Current Index (_currentIndex): {_currentIndex}");
        Log.Debug($"[BmlDraggableItem] Final Sibling Index: {finalIndex}");

        // Check drop target
        List<RaycastResult> results = new();
        EventSystem.current?.RaycastAll(eventData, results);

        BmlDraggableItem? targetGroupItem = null;
        foreach (var result in results)
        {
            if (result.gameObject == gameObject) continue;

            var item = result.gameObject.GetComponent<BmlDraggableItem>() ??
                       result.gameObject.GetComponentInParent<BmlDraggableItem>();

            if (item != null && item._type == "group")
            {
                targetGroupItem = item;
                break;
            }
        }

        // Handle mod dropped on group
        if (targetGroupItem != null && _type == "mod")
        {
            Log.Debug($"[BmlDraggableItem] Mod '{_id}' dropped on group '{targetGroupItem._id}'");
            OnModDroppedOnGroup?.Invoke(this, targetGroupItem);
            movedToGroup = true;
            transform.SetSiblingIndex(_originalIndex);
        }
        // Handle reordering within same list
        else if (!movedToGroup && finalIndex != _originalIndex)
        {
            // For mods: use the stored data index if available, otherwise calculate
            int originalDataIndex, newDataIndex;

            if (_type == "mod" && _dataIndex >= 0)
            {
                // Use stored data index and calculate new position
                originalDataIndex = _dataIndex;
                // Calculate how many positions we moved in the UI
                var uiDelta = _currentIndex - _originalIndex;
                newDataIndex = _dataIndex + uiDelta;

                Log.Debug($"[BmlDraggableItem] Mod reordered using stored index");
                Log.Debug($"[BmlDraggableItem]   Stored data index: {_dataIndex}");
                Log.Debug($"[BmlDraggableItem]   UI delta: {uiDelta} (from {_originalIndex} to {_currentIndex})");
                Log.Debug($"[BmlDraggableItem]   New data index: {newDataIndex}");
            }
            else
            {
                // For groups or fallback: calculate from UI positions
                originalDataIndex = CalculateDataIndex();
                newDataIndex = CalculateDataIndex();

                Log.Debug($"[BmlDraggableItem] Item '{_id}' reordered (calculated)");
                Log.Debug($"[BmlDraggableItem]   UI indices: {_originalIndex} → {_currentIndex}");
                Log.Debug($"[BmlDraggableItem]   Data indices: {originalDataIndex} → {newDataIndex}");
            }

            OnItemReordered?.Invoke(this, originalDataIndex, newDataIndex);
        }
        else
        {
            // No change, restore original position
            transform.SetSiblingIndex(_originalIndex);
        }

        // Reset all group hover states
        ResetAllGroupItemsHover();

        Log.Debug($"[BmlDraggableItem] End drag: {_id}");
    }

    /// <summary>
    /// Calculate the data index by finding the position of this item in the visible list
    /// For mods: find position among currently displayed mods
    /// For groups: count draggable group items before this position
    /// </summary>
    private int CalculateDataIndex()
    {
        if (_originalParent == null) return 0;

        // Collect all draggable items of the same type in sibling order
        List<BmlDraggableItem> items = new();
        for (var i = 0; i < _originalParent.childCount; i++)
        {
            var child = _originalParent.GetChild(i);
            var draggable = child.GetComponent<BmlDraggableItem>();
            if (draggable != null && draggable._type == _type)
            {
                items.Add(draggable);
            }
        }

        // Find our position in this list
        var dataIndex = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] == this)
            {
                dataIndex = i;
                break;
            }

            // Also check by ID in case the reference changed
            if (items[i]._id == _id)
            {
                dataIndex = i;
                break;
            }
        }

        return dataIndex;
    }

    private void CheckHoverOverGroupItems(PointerEventData eventData)
    {
        // Only mods check for group hover
        if (_type != "mod") return;

        List<RaycastResult> results = new();
        EventSystem.current?.RaycastAll(eventData, results);

        BmlDraggableItem? hoveredGroupItem = null;
        foreach (var result in results)
        {
            if (result.gameObject == gameObject) continue;

            var item = result.gameObject.GetComponent<BmlDraggableItem>() ??
                       result.gameObject.GetComponentInParent<BmlDraggableItem>();

            if (item != null && item._type == "group")
            {
                hoveredGroupItem = item;
                break;
            }
        }

        // Update all group items
        var allGroupItems = FindObjectsOfType<BmlDraggableItem>();
        foreach (var item in allGroupItems)
        {
            if (item._type != "group") continue;

            if (item == hoveredGroupItem && !item._isHoveredByDrag)
            {
                item.OnDragEnter();
            }
            else if (item != hoveredGroupItem && item._isHoveredByDrag)
            {
                item.OnDragExit();
            }
        }
    }

    private void ResetAllGroupItemsHover()
    {
        var allItems = FindObjectsOfType<BmlDraggableItem>();
        foreach (var item in allItems)
        {
            if (item._isHoveredByDrag)
            {
                item.OnDragExit();
            }
        }
    }

    private void OnDragEnter()
    {
        _isHoveredByDrag = true;
        if (_originalScale == Vector3.zero)
        {
            _originalScale = transform.localScale;
        }

        transform.localScale = _originalScale * 1.05f; // 稍微放大，更平滑

        // 增强视觉效果 - 使用UIResourceManager添加图标和美化边框
        var img = GetComponent<Image>();
        if (img != null)
        {
            if (_type == "group")
            {
                // 为分组项添加绿色高亮效果
                img.color = new Color(0.4f, 0.8f, 0.4f, 1f);

                // 添加成功图标
                var successSprite = UIResourceManager.GetSprite("check_success");
                // 检查是否已有图标，避免重复添加
                var existingIcon = transform.Find("DragIcon");
                if (existingIcon == null)
                {
                    var iconGo = new GameObject("DragIcon");
                    iconGo.transform.SetParent(transform, false);
                    var iconRect = iconGo.AddComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0.8f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.95f, 0.8f);
                    iconRect.offsetMin = Vector2.zero;
                    iconRect.offsetMax = Vector2.zero;

                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.sprite = successSprite;
                    iconImg.color = new Color(0.8f, 1f, 0.8f, 1f);
                }
            }
        }

        // 添加增强的边框效果
        var outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        outline.effectColor = _type == "group"
            ? new Color(0.2f, 1f, 0.2f, 1f) // 绿色边框表示可以拖入
            : new Color(1f, 0.8f, 0f, 1f); // 黄色边框表示拖拽状态
        outline.effectDistance = new Vector2(3, 3);

        // 添加发光效果
        var glow = GetComponent<Shadow>();
        if (glow == null)
        {
            glow = gameObject.AddComponent<Shadow>();
        }

        glow.effectColor = _type == "group"
            ? new Color(0.3f, 0.9f, 0.3f, 0.6f) // 绿色发光
            : new Color(0.8f, 0.6f, 0.2f, 0.6f); // 黄色发光
        glow.effectDistance = new Vector2(5, 5);

        Log.Debug($"[BmlDraggableItem] Drag entered {_type}: {_id}");
    }

    private void OnDragExit()
    {
        _isHoveredByDrag = false;
        if (_originalScale != Vector3.zero)
        {
            transform.localScale = _originalScale;
        }

        // 移除拖拽图标
        var existingIcon = transform.Find("DragIcon");
        if (existingIcon != null)
        {
            Destroy(existingIcon.gameObject);
        }

        // 恢复原始外观 - 根据类型恢复不同的默认样式
        var img = GetComponent<Image>();
        if (img != null)
        {
            // 这里不直接恢复颜色，让UI系统根据状态重新渲染
            // 这样可以与BmlModManagerUIView中的选择状态保持一致
        }

        // 移除边框效果
        var outline = GetComponent<Outline>();
        if (outline != null)
        {
            Destroy(outline);
        }

        // 移除发光效果
        var glow = GetComponent<Shadow>();
        if (glow != null)
        {
            Destroy(glow);
        }

        Log.Debug($"[BmlDraggableItem] Drag exited {_type}: {_id}");
    }
}
