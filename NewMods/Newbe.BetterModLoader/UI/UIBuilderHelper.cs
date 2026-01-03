namespace Newbe.BetterModLoader.UI
{
    /// <summary>
    /// UI 构建辅助工具，参考 DuckMarket.Core 的 UIBuilder
    /// 提供创建标准 UI 控件的方法，确保与游戏 UI 风格一致
    /// </summary>
    public static class UIBuilderHelper
    {
        /// <summary>
        /// 创建 Canvas
        /// </summary>
        public static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas");
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            go.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        /// <summary>
        /// 创建面板
        /// </summary>
        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = color;

            return image;
        }

        /// <summary>
        /// 创建按钮
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string buttonText, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 40);

            var image = go.AddComponent<Image>();
            image.color = color;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // 创建文本
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);

            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return button;
        }

        /// <summary>
        /// 创建按钮（使用默认颜色）
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string buttonText)
        {
            return CreateButton(parent, name, buttonText, GameUIConfig.Colors.ButtonNormal);
        }

        /// <summary>
        /// 创建文本
        /// </summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string content,
            Color color, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 100);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;

            return text;
        }

        /// <summary>
        /// 创建图片
        /// </summary>
        public static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);

            var image = go.AddComponent<Image>();
            image.color = color;

            return image;
        }

        /// <summary>
        /// 创建滚动视图
        /// </summary>
        public static ScrollRect CreateScrollRect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 300);

            var bgImage = go.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 创建 Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(go.transform, false);

            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            scrollRect.viewport = viewportRect;

            // 创建 Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);

            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var layoutGroup = contentGo.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childScaleWidth = false;
            layoutGroup.childScaleHeight = false;

            var contentSizeFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;

            return scrollRect;
        }

        /// <summary>
        /// 创建带背景的文本面板
        /// </summary>
        public static GameObject CreateTextPanel(Transform parent, string name, string text,
            Color bgColor, Color textColor, int fontSize = 14)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            // 背景
            var bgImage = go.AddComponent<Image>();
            bgImage.color = bgColor;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 40);

            // 文本
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);

            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);

            var textComponent = textGo.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = textColor;

            return go;
        }

        /// <summary>
        /// 创建网格布局组
        /// </summary>
        public static GridLayoutGroup CreateGridLayout(Transform parent, Vector2 cellSize,
            Vector2 spacing, Vector2 padding, GridLayoutGroup.Corner startCorner,
            GridLayoutGroup.Axis startAxis)
        {
            var layoutGroup = parent.gameObject.AddComponent<GridLayoutGroup>();
            layoutGroup.cellSize = cellSize;
            layoutGroup.spacing = spacing;
            layoutGroup.padding = new RectOffset((int)padding.x, (int)padding.x,
                (int)padding.y, (int)padding.y);
            layoutGroup.startCorner = startCorner;
            layoutGroup.startAxis = startAxis;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.constraint = GridLayoutGroup.Constraint.Flexible;

            return layoutGroup;
        }

        /// <summary>
        /// 应用阴影效果
        /// </summary>
        public static void AddShadow(GameObject target, Color? color = null, Vector2? distance = null)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color ?? GameUIConfig.Effects.ShadowColor;
            shadow.effectDistance = distance ?? GameUIConfig.Effects.ShadowDistance;
        }

        /// <summary>
        /// 应用描边效果
        /// </summary>
        public static void AddOutline(GameObject target, Color? color = null, Vector2? distance = null)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color ?? GameUIConfig.Effects.OutlineColor;
            outline.effectDistance = distance ?? GameUIConfig.Effects.OutlineDistance;
        }

        /// <summary>
        /// 设置 RectTransform 为全填充
        /// </summary>
        public static void SetStretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 设置 RectTransform 为顶部填充
        /// </summary>
        public static void SetStretchTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, height);
        }

        /// <summary>
        /// 设置 RectTransform 为底部填充
        /// </summary>
        public static void SetStretchBottom(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(0, height);
        }
    }
}