using static Newbe.BetterModLoader.UI.UIFactory;

namespace Newbe.BetterModLoader.UI.ManagerUI.Views;

/// <summary>
/// Bottom footer editor for creating new groups; handles expand/collapse and input focus.
/// </summary>
internal class GroupFooterEditorView
{
    private RectTransform _groupScrollViewRect;
    private RectTransform? _groupFooterRect;
    private GameObject? _groupAddButtonRoot;
    private GameObject? _groupAddEditorRoot;
    private TMP_InputField? _footerInputRef;
    private readonly MonoBehaviour _host; // for StartCoroutine

    public event Action<string>? OnCreateGroup;

    public GroupFooterEditorView(MonoBehaviour host, RectTransform groupScrollViewRect)
    {
        _host = host;
        _groupScrollViewRect = groupScrollViewRect;
    }

    public void Build(Transform groupPanelTransform)
    {
        // Footer container
        var groupFooter = CreateUIObject("GroupFooter", groupPanelTransform);
        var footerRect = groupFooter.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0.02f, 0f);
        footerRect.anchorMax = new Vector2(0.98f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.sizeDelta = new Vector2(0, 60);
        var footerBg = groupFooter.AddComponent<Image>();
        footerBg.color = new Color(0.15f, 0.15f, 0.19f, 0.95f);
        _groupFooterRect = footerRect;

        // Button root filling footer
        _groupAddButtonRoot = CreateUIObject("AddGroupButtonRoot", groupFooter.transform);
        SetAnchors(_groupAddButtonRoot, Vector2.zero, Vector2.one);
        var addBtnBg = _groupAddButtonRoot.AddComponent<Image>();
        addBtnBg.color = new Color(0.2f, 0.7f, 0.3f, 1f);
        var addBtn = _groupAddButtonRoot.AddComponent<Button>();
        addBtn.transition = Selectable.Transition.None;
        var addBtnTextObj = CreateUIObject("Text", _groupAddButtonRoot.transform);
        var addBtnTmp = addBtnTextObj.AddComponent<TextMeshProUGUI>();
        addBtnTmp.text = L.Group.FooterAddButton;
        addBtnTmp.fontSize = 20;
        addBtnTmp.color = Color.white;
        addBtnTmp.alignment = TextAlignmentOptions.Center;
        SetAnchors(addBtnTextObj, Vector2.zero, Vector2.one);
        addBtn.onClick.AddListener(Expand);

        UpdateGroupScrollBottomPadding();
    }

    public void Expand()
    {
        if (_groupFooterRect == null) return;
        _groupFooterRect.sizeDelta = new Vector2(_groupFooterRect.sizeDelta.x, 120);
        UpdateGroupScrollBottomPadding();

        if (_groupAddButtonRoot != null) _groupAddButtonRoot.SetActive(false);
        if (_groupAddEditorRoot == null && _groupFooterRect != null)
        {
            _groupAddEditorRoot = BuildFooterEditorUI(_groupFooterRect);
        }

        if (_groupAddEditorRoot != null) _groupAddEditorRoot.SetActive(true);

        if (_footerInputRef != null)
        {
            _host.StartCoroutine(FocusTMPInputNextFrame(_footerInputRef));
        }
    }

    public void Collapse()
    {
        if (_groupFooterRect == null) return;
        _groupFooterRect.sizeDelta = new Vector2(_groupFooterRect.sizeDelta.x, 60);
        UpdateGroupScrollBottomPadding();

        if (_groupAddButtonRoot != null) _groupAddButtonRoot.SetActive(true);
        if (_groupAddEditorRoot != null) _groupAddEditorRoot.SetActive(false);
    }

    private GameObject BuildFooterEditorUI(RectTransform parent)
    {
        var root = CreateUIObject("AddGroupEditor", parent);
        SetAnchors(root, Vector2.zero, Vector2.one);

        var v = root.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.padding = new RectOffset(10, 10, 10, 10);
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        // Input
        var inputRoot = CreateUIObject("Input", root.transform);
        SetAnchors(inputRoot, Vector2.zero, Vector2.one);
        var inputLE = inputRoot.AddComponent<LayoutElement>();
        inputLE.preferredHeight = 48;
        var inputBg = inputRoot.AddComponent<Image>();
        inputBg.color = new Color(1f, 1f, 1f, 0.95f);

        var viewport = CreateUIObject("Viewport", inputRoot.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = new Vector2(0, 0);
        vpRect.anchorMax = new Vector2(1, 1);
        vpRect.offsetMin = new Vector2(10, 8);
        vpRect.offsetMax = new Vector2(-10, -8);
        viewport.AddComponent<RectMask2D>();

        var placeholderGO = CreateUIObject("Placeholder", viewport.transform);
        var placeholderTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderTMP.text = L.Group.FooterPlaceholder;
        placeholderTMP.fontSize = 20;
        placeholderTMP.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderTMP.alignment = TextAlignmentOptions.Left;
        SetAnchors(placeholderGO, Vector2.zero, Vector2.one);

        var textGO = CreateUIObject("Text", viewport.transform);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = string.Empty;
        textTMP.fontSize = 20;
        textTMP.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        textTMP.alignment = TextAlignmentOptions.Left;
        SetAnchors(textGO, Vector2.zero, Vector2.one);

        var tmpInput = inputRoot.AddComponent<TMP_InputField>();
        tmpInput.textViewport = vpRect;
        tmpInput.textComponent = textTMP;
        tmpInput.placeholder = placeholderTMP;
        tmpInput.interactable = true;
        tmpInput.richText = false;
        tmpInput.caretWidth = 2;
        tmpInput.lineType = TMP_InputField.LineType.SingleLine;
        tmpInput.onSelect.AddListener(_ =>
        {
            tmpInput.caretPosition = tmpInput.text != null ? tmpInput.text.Length : 0;
            tmpInput.ForceLabelUpdate();
        });
        // 回车键确认创建分组
        tmpInput.onSubmit.AddListener(text =>
        {
            var name = text != null ? text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                Log.Warn("[BetterModLoader] Group name is empty");
                return;
            }

            OnCreateGroup?.Invoke(name);
            Collapse();
            tmpInput.text = string.Empty;
            tmpInput.DeactivateInputField();
        });
        tmpInput.ActivateInputField();
        _footerInputRef = tmpInput;

        // Buttons row
        var buttonsRow = CreateUIObject("Buttons", root.transform);
        var buttonsLE = buttonsRow.AddComponent<LayoutElement>();
        buttonsLE.preferredHeight = 36;
        var buttonsHL = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        buttonsHL.spacing = 12f;
        buttonsHL.padding = new RectOffset(0, 0, 0, 0);
        buttonsHL.childAlignment = TextAnchor.MiddleCenter;
        buttonsHL.childControlWidth = true;
        buttonsHL.childControlHeight = true;
        buttonsHL.childForceExpandWidth = true;
        buttonsHL.childForceExpandHeight = true;

        var okBtn = CreateSmallIconButton(buttonsRow.transform, "O");
        okBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            var name = tmpInput.text != null ? tmpInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                Log.Warn("[BetterModLoader] Group name is empty");
                return;
            }

            OnCreateGroup?.Invoke(name);
            Collapse();
            tmpInput.text = string.Empty;
            tmpInput.DeactivateInputField();
        });

        var cancelBtn = CreateSmallIconButton(buttonsRow.transform, "X");
        cancelBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            Collapse();
            tmpInput.text = string.Empty;
            tmpInput.DeactivateInputField();
        });

        void SetupStretch(GameObject btnObj, Color baseColor)
        {
            var le = btnObj.GetComponent<LayoutElement>() ?? btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = -1;
            le.flexibleWidth = 1;
            le.preferredHeight = 36;
            var img = btnObj.GetComponent<Image>();
            if (img != null) img.color = baseColor;
            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                var colors = btn.colors;
                colors.normalColor = baseColor;
                colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.15f);
                colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.15f);
                btn.colors = colors;
            }
        }

        SetupStretch(okBtn, new Color(0.2f, 0.7f, 0.3f, 1f));
        SetupStretch(cancelBtn, new Color(0.8f, 0.2f, 0.2f, 1f));

        return root;
    }

    private System.Collections.IEnumerator FocusTMPInputNextFrame(TMP_InputField input)
    {
        yield return null;
        if (input != null)
        {
            input.ActivateInputField();
            input.Select();
            input.caretPosition = input.text != null ? input.text.Length : 0;
        }
    }

    private void UpdateGroupScrollBottomPadding()
    {
        if (_groupScrollViewRect == null || _groupFooterRect == null) return;
        var bottomPad = _groupFooterRect.sizeDelta.y + 6f;
        var offMin = _groupScrollViewRect.offsetMin;
        _groupScrollViewRect.offsetMin = new Vector2(offMin.x, bottomPad);
    }
}
