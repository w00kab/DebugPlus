namespace DebugPlus
{
    /// <summary>
    /// Mod 字符串树根（M1 起开始使用）。键路径由 Localization.CreateLocStringKeys 生成，
    /// 形如 DebugPlus.STRINGS.UI.DEBUGPLUS.CONFIG_BUTTON。
    ///
    /// 两条硬规则：
    /// ① 类名必须叫 STRINGS（类名不同会破坏 LocString 键路径）。
    /// ② **一律使用独立的中文字符串做默认值**，严禁把游戏 STRINGS 里已有的 LocString 字段
    ///    赋给本 Mod 字段 —— LocString 是引用类型（class），赋值即共享同一对象，
    ///    而 Localization.RegisterForTranslation → LocString.CreateLocStringKeys 会对树内
    ///    每个 LocString 调 SetKey 改写键，从而**污染游戏自身的字符串键**（表现为部分翻译丢失）。
    /// </summary>
    public static class STRINGS
    {
        public static class UI
        {
            public static class DEBUGPLUS
            {
                public static LocString CONFIG_BUTTON = "修改配置";
                public static LocString CONFIG_BUTTON_TOOLTIP = "打开 DebugPlus 配置面板";

                public static LocString PANEL_TITLE = "修改配置";
                public static LocString PANEL_PENDING = "（批 2a 仅外壳：参数行将在批 2b 接入）";
                public static LocString PANEL_CLOSE = "关闭";
                public static LocString PANEL_NO_TARGET = "（未取得目标名称）";
            }
        }
    }
}
