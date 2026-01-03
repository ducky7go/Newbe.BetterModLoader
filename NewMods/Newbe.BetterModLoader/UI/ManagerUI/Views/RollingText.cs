using System.Collections;

namespace Newbe.BetterModLoader.UI.ManagerUI.Views;

/// <summary>
/// RollingText 组件
/// 特性：
/// - 支持 direction = up / down
/// - 支持 duration（默认 5 秒）控制每条滚动时长
/// - 支持 loop 或 单次切换
/// - 支持淡入/淡出 与 位移动画
/// - 支持消息等级（MessageLevel），左侧带固定颜色条
/// - 整行可点击，外部可订阅 OnItemClicked 事件
/// 使用示例：
/// var rt = gameObject.AddComponent();
/// rt.SetItems(new[]{ new RollingText.RollingItem("文本", RollingText.MessageLevel.Info) });
/// rt.Direction = RollingText.DirectionType.Up;
/// rt.Duration = 5f;
/// rt.Loop = true;
/// rt.OnItemClicked += (idx, item) => Debug.Log(item.Text);
/// </summary>
public class RollingText : MonoBehaviour
{
    public enum DirectionType
    {
        Up,
        Down
    }

    public enum MessageLevel
    {
        Info,
        Warn,
        Error,
        Debug,
        Success,
        None
    }

    [Serializable]
    public class RollingItem
    {
        public string Text = string.Empty;
        public MessageLevel Level = MessageLevel.Info;
        public string Id = string.Empty;

        public RollingItem()
        {
        }

        public RollingItem(string text, MessageLevel level = MessageLevel.Info, string id = "")
        {
            Text = text;
            Level = level;
            Id = id;
        }
    }

    [SerializeField] private DirectionType _direction = DirectionType.Up;
    [SerializeField] private float _duration = 5f;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _fade = true;
    [SerializeField] private float _moveDistance = 24f;
    [SerializeField] private Color _infoColor = new Color(0.22f, 0.6f, 1f);
    [SerializeField] private Color _warnColor = new Color(1f, 0.64f, 0f);
    [SerializeField] private Color _errorColor = new Color(1f, 0.23f, 0.23f);
    [SerializeField] private Color _debugColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color _successColor = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color _noneColor = new Color(0f, 0f, 0f, 0f);

    public DirectionType Direction
    {
        get => _direction;
        set => _direction = value;
    }

    public float Duration
    {
        get => _duration;
        set => _duration = Mathf.Max(0.01f, value);
    }

    public bool Loop
    {
        get => _loop;
        set => _loop = value;
    }

    public bool Fade
    {
        get => _fade;
        set => _fade = value;
    }

    public float MoveDistance
    {
        get => _moveDistance;
        set => _moveDistance = Mathf.Max(1f, value);
    }

    public event Action<int, RollingItem>? OnItemClicked;

    [SerializeField] private List<RollingItem> _items = new List<RollingItem>();
    private int _currentIndex = -1;
    private Coroutine? _runner;

    private RectTransform _rootRect = null!;
    private Image _levelBar = null!;
    private RectTransform _textContainer = null!;
    private TextMeshProUGUI _textA = null!;
    private TextMeshProUGUI _textB = null!;
    private CanvasGroup _canvasA = null!;
    private CanvasGroup _canvasB = null!;
    private Button _clickButton = null!;

    // hover / click animation support
    private Vector3 _baseScale = Vector3.one;
    private Coroutine? _hoverCoroutine;
    private Coroutine? _clickRippleCoroutine;

    private bool _initialized = false;

    private void Awake()
    {
        InitLayout();
    }

    private void OnEnable()
    {
        StartRolling();
    }

    private void OnDisable()
    {
        StopRolling();
    }

    private void OnDestroy()
    {
        StopRolling();
    }

    public void SetItems(IEnumerable<RollingItem> items)
    {
        _items = new List<RollingItem>(items);
        _currentIndex = -1;
    }

    public void SetItems(params RollingItem[] items) => SetItems((IEnumerable<RollingItem>)items);

    public void AddItem(RollingItem item)
    {
        _items.Add(item);
    }

    public void ClearItems()
    {
        _items.Clear();
        _currentIndex = -1;
        UpdateTextImmediate(string.Empty, MessageLevel.None);
    }

    public void StartRolling()
    {
        if (!_initialized) InitLayout();
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }

        if (_items == null || _items.Count == 0) return;
        _runner = StartCoroutine(RollCoroutine());
    }

    public void StopRolling()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }

    public void SetColorForLevel(MessageLevel level, Color color)
    {
        switch (level)
        {
            case MessageLevel.Info: _infoColor = color; break;
            case MessageLevel.Warn: _warnColor = color; break;
            case MessageLevel.Error: _errorColor = color; break;
            case MessageLevel.Debug: _debugColor = color; break;
            case MessageLevel.Success: _successColor = color; break;
            case MessageLevel.None: _noneColor = color; break;
        }
    }

    private void InitLayout()
    {
        if (_initialized) return;
        var rt = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        _rootRect = rt;

        var barGO = new GameObject("LevelBar", typeof(RectTransform));
        barGO.transform.SetParent(transform, false);
        var barRect = barGO.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(0f, 1f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.sizeDelta = new Vector2(6f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 0f);
        _levelBar = barGO.AddComponent<Image>();
        _levelBar.raycastTarget = false;
        _levelBar.color = _infoColor;

        var tcGO = new GameObject("TextContainer", typeof(RectTransform));
        tcGO.transform.SetParent(transform, false);
        _textContainer = tcGO.GetComponent<RectTransform>();
        _textContainer.anchorMin = new Vector2(0f, 0f);
        _textContainer.anchorMax = new Vector2(1f, 1f);
        _textContainer.pivot = new Vector2(0.5f, 0.5f);
        _textContainer.offsetMin = new Vector2(8f, 0f);
        _textContainer.offsetMax = new Vector2(0f, 0f);

        _textA = CreateTMP("TextA", _textContainer);
        _textB = CreateTMP("TextB", _textContainer);

        _canvasA = _textA.gameObject.AddComponent<CanvasGroup>();
        _canvasB = _textB.gameObject.AddComponent<CanvasGroup>();
        _canvasA.blocksRaycasts = false;
        _canvasB.blocksRaycasts = false;
        _textA.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        _textB.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;

        var btnGO = new GameObject("ClickArea", typeof(RectTransform));
        btnGO.transform.SetParent(transform, false);
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 0f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.offsetMin = new Vector2(0f, 0f);
        btnRect.offsetMax = new Vector2(0f, 0f);
        _clickButton = btnGO.AddComponent<Button>();
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;
        _clickButton.targetGraphic = img;

        // enable color tint for hover/pressed visual feedback
        _clickButton.transition = Selectable.Transition.ColorTint;
        var cb = _clickButton.colors;
        cb.normalColor = new Color(1f, 1f, 1f, 0f);
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        cb.disabledColor = new Color(1f, 1f, 1f, 0f);
        _clickButton.colors = cb;

        // EventTrigger to handle pointer enter/exit/down for animations
        var et = btnGO.AddComponent<EventTrigger>();
        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => StartHover(true));
        et.triggers.Add(entryEnter);

        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => StartHover(false));
        et.triggers.Add(entryExit);

        var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) =>
        {
            if (_clickRippleCoroutine != null) StopCoroutine(_clickRippleCoroutine);
            _clickRippleCoroutine = StartCoroutine(ClickRippleCoroutine());
        });
        et.triggers.Add(entryDown);

        // keep existing click behavior
        _clickButton.onClick.AddListener(OnClickInternal);

        _initialized = true;
        // cache base scale for hover / click animations
        _baseScale = transform.localScale;
    }

    private TextMeshProUGUI CreateTMP(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(0f, 0f);
        rect.offsetMax = new Vector2(0f, 0f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.text = string.Empty;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private IEnumerator RollCoroutine()
    {
        if (_items.Count == 0) yield break;
        _currentIndex = GetNextIndex(_currentIndex);
        var current = _items[_currentIndex];
        UpdateTextImmediate(current.Text, current.Level);
        yield return null;
        _canvasA.alpha = 1f;
        _canvasB.alpha = 0f;

        while (true)
        {
            var wait = 0f;
            while (wait < _duration)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            var nextIndex = GetNextIndex(_currentIndex);
            if (nextIndex == _currentIndex && !_loop) yield break;
            var next = _items[nextIndex];
            yield return StartCoroutine(AnimateSwap(current, next));
            _currentIndex = nextIndex;
            current = next;
            if (!_loop && _currentIndex == _items.Count - 1)
            {
                if (_duration > 0f)
                {
                    var t = 0f;
                    while (t < _duration)
                    {
                        t += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                yield break;
            }
        }
    }

    private int GetNextIndex(int current)
    {
        if (_items == null || _items.Count == 0) return -1;
        if (current < 0) return 0;
        var next = current + 1;
        if (next >= _items.Count) return _loop ? 0 : current;
        return next;
    }

    private IEnumerator AnimateSwap(RollingItem from, RollingItem to)
    {
        var aIsVisible = _canvasA.alpha >= 0.5f;
        var visibleText = aIsVisible ? _textA : _textB;
        var visibleCanvas = aIsVisible ? _canvasA : _canvasB;
        var hiddenText = aIsVisible ? _textB : _textA;
        var hiddenCanvas = aIsVisible ? _canvasB : _canvasA;

        hiddenText.text = to.Text;
        UpdateLevelBar(to.Level);

        var visibleRect = visibleText.GetComponent<RectTransform>();
        var hiddenRect = hiddenText.GetComponent<RectTransform>();
        visibleRect.anchoredPosition = Vector2.zero;
        hiddenRect.anchoredPosition = Vector2.zero;

        if (Fade) hiddenCanvas.alpha = 0f;
        else hiddenCanvas.alpha = 1f;

        var dirMultiplier = (_direction == DirectionType.Up) ? 1f : -1f;
        var move = new Vector2(0f, dirMultiplier * MoveDistance);

        float animTime = Mathf.Min(0.3f, _duration * 0.25f);
        float t = 0f;
        while (t < animTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / animTime);
            p = p * p * (3f - 2f * p);
            visibleRect.anchoredPosition = Vector2.Lerp(Vector2.zero, -move, p);
            hiddenRect.anchoredPosition = Vector2.Lerp(move, Vector2.zero, p);
            if (Fade)
            {
                visibleCanvas.alpha = Mathf.Lerp(1f, 0f, p);
                hiddenCanvas.alpha = Mathf.Lerp(0f, 1f, p);
            }

            yield return null;
        }

        visibleRect.anchoredPosition = -move;
        hiddenRect.anchoredPosition = Vector2.zero;
        visibleCanvas.alpha = 0f;
        hiddenCanvas.alpha = 1f;
        yield return null;
    }

    private void UpdateTextImmediate(string text, MessageLevel level)
    {
        if (_textA == null || _textB == null) return;
        _textA.text = text;
        _textB.text = string.Empty;
        _canvasA.alpha = 1f;
        _canvasB.alpha = 0f;
        UpdateLevelBar(level);
    }

    private void UpdateLevelBar(MessageLevel level)
    {
        if (_levelBar == null) return;
        _levelBar.color = GetColorByLevel(level);
    }

    private Color GetColorByLevel(MessageLevel level)
    {
        switch (level)
        {
            case MessageLevel.Info: return _infoColor;
            case MessageLevel.Warn: return _warnColor;
            case MessageLevel.Error: return _errorColor;
            case MessageLevel.Debug: return _debugColor;
            case MessageLevel.Success: return _successColor;
            case MessageLevel.None: return _noneColor;
            default: return _noneColor;
        }
    }

    // Hover / click animation helpers
    private void StartHover(bool enter)
    {
        if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = StartCoroutine(HoverScaleCoroutine(enter));
    }

    private IEnumerator HoverScaleCoroutine(bool enter)
    {
        const float dur = 0.08f;
        float t = 0f;
        Vector3 from = transform.localScale;
        Vector3 to = enter ? _baseScale * 1.03f : _baseScale;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            transform.localScale = Vector3.Lerp(from, to, p);
            yield return null;
        }

        transform.localScale = to;
        _hoverCoroutine = null;
    }

    private IEnumerator ClickRippleCoroutine()
    {
        if (_clickButton == null) yield break;
        var img = _clickButton.targetGraphic as Image;
        if (img == null) yield break;
        var originalColor = img.color;
        var highlight = new Color(originalColor.r, originalColor.g, originalColor.b,
            Mathf.Clamp01(0.18f + originalColor.a));
        const float dur = 0.12f;
        float t = 0f;
        Vector3 fromScale = transform.localScale;
        Vector3 midScale = _baseScale * 0.98f;

        // press in
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            img.color = Color.Lerp(originalColor, highlight, Mathf.Sin(p * Mathf.PI));
            transform.localScale = Vector3.Lerp(fromScale, midScale, p);
            yield return null;
        }

        // release back
        t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            img.color = Color.Lerp(highlight, originalColor, p);
            transform.localScale = Vector3.Lerp(midScale, _baseScale, p);
            yield return null;
        }

        img.color = originalColor;
        transform.localScale = _baseScale;
        _clickRippleCoroutine = null;
    }

    private void OnClickInternal()
    {
        if (_currentIndex >= 0 && _currentIndex < _items.Count)
        {
            var item = _items[_currentIndex];
            try
            {
                OnItemClicked?.Invoke(_currentIndex, item);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred");
            }
        }
    }
}
