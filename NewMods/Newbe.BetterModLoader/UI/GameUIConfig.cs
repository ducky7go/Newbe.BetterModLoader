namespace Newbe.BetterModLoader.UI
{
    /// <summary>
    /// 游戏UI颜色配置，模仿DuckMarket.Core的模式
    /// </summary>
    public static class GameUIConfig
    {
        public static class Colors
        {
            // 背景颜色
            public static readonly Color PanelBackground = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            public static readonly Color HeaderBackground = new Color(0.2f, 0.2f, 0.25f, 1f);
            public static readonly Color PanelBorder = new Color(0.3f, 0.3f, 0.35f, 0.8f);

            // 状态颜色
            public static readonly Color SuccessGreen = new Color(0.2f, 0.9f, 0.3f, 1f);
            public static readonly Color WarningYellow = new Color(1f, 0.8f, 0.2f, 1f);
            public static readonly Color ErrorRed = new Color(1f, 0.2f, 0.2f, 1f);
            public static readonly Color LoadingOrange = new Color(1f, 0.6f, 0.2f, 1f);
            public static readonly Color DisabledBlue = new Color(0.4f, 0.7f, 1f, 1f);

            // 文本颜色
            public static readonly Color HeaderText = new Color(0.95f, 0.95f, 1f, 1f);
            public static readonly Color NormalText = Color.white;
            public static readonly Color MutedText = new Color(0.7f, 0.7f, 0.7f, 1f);

            // 按钮颜色
            public static readonly Color ButtonNormal = new Color(0.2f, 0.2f, 0.2f, 1f);
            public static readonly Color ButtonClose = new Color(0.9f, 0.3f, 0.3f, 1f);
            public static readonly Color ButtonCloseHighlight = new Color(1f, 0.4f, 0.4f, 1f);
            public static readonly Color ButtonClosePressed = new Color(0.8f, 0.2f, 0.2f, 1f);
        }

        public static class Effects
        {
            public static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.5f);
            public static readonly Vector2 ShadowDistance = new Vector2(2, -2);
            public static readonly Color OutlineColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);
            public static readonly Vector2 OutlineDistance = new Vector2(2, 2);
        }

        public static class Gradients
        {
            public static readonly Color[] PanelGradient = new Color[]
            {
                new Color(0.12f, 0.12f, 0.16f, 0.98f),
                new Color(0.18f, 0.18f, 0.22f, 0.98f)
            };

            public static readonly Color[] HeaderGradient = new Color[]
            {
                new Color(0.25f, 0.25f, 0.35f, 0.9f),
                new Color(0.15f, 0.15f, 0.20f, 0.9f)
            };
        }
    }
}