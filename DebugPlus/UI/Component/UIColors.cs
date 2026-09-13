using UnityEngine;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 本 Mod 的**唯一色板**：所有 UI 颜色的集中定义处。
    ///
    /// 纪律：任何地方需要颜色都引用本类的静态字段，**不要在控件里硬编码 Color(...)**；
    /// 想改整体风格只改本文件，全 UI 同步生效。
    ///
    /// 取值口径（批 3-1 定，做法参考成熟配色体系的分层思路，非搬运）：
    /// ① 语义色：主蓝 / 成功绿 / 警告橙 / 危险红 / 信息灰；
    /// ② 文字色分「浅底用」与「深底用」两套 —— 本 Mod 的弹窗是深色底（见 <see cref="BackgroundPanel"/>），
    ///    所以默认文字用 <see cref="TextOnDark"/>，而白底控件（数值框内衬）里要用 <see cref="TextPrimary"/>；
    /// ③ 边框、背景、控件轨道分开定义，避免"同一个灰到处写、改一处漏一处"。
    ///
    /// ⚠️ 与本 Mod 现有硬编码颜色的关系：`ConfigPanel` / `SliderField` 里的散装 Color 值**暂不改动**
    /// （改动归后续批次），新控件一律走本色板。
    /// </summary>
    public static class UIColors
    {
        // ────────────────── ① 语义色 ──────────────────

        /// <summary>主蓝（选中态、强调）</summary>
        public static readonly Color Primary = new Color32(58, 141, 224, 255);

        /// <summary>主蓝淡（选区/填充，半透明）</summary>
        public static readonly Color PrimaryFaint = new Color32(58, 141, 224, 60);

        /// <summary>主蓝悬停底（不透明预混合色：白底叠主蓝，用于叠在边框上的填充，避免透底色发灰）</summary>
        public static readonly Color PrimaryHoverBackground = new Color32(223, 237, 250, 255);

        /// <summary>主蓝按压（加深）</summary>
        public static readonly Color PrimaryPressed = new Color32(46, 113, 179, 255);

        /// <summary>成功绿（勾选"开"的填充）</summary>
        public static readonly Color Success = new Color32(82, 155, 46, 255);

        /// <summary>危险红（错误态/非法输入）</summary>
        public static readonly Color Danger = new Color32(209, 64, 64, 255);

        /// <summary>信息灰（次要文字、状态点）</summary>
        public static readonly Color Info = new Color32(144, 147, 153, 255);

        // ────────────────── ② 文字色 ──────────────────

        /// <summary>深底上的文字（本 Mod 弹窗是深色底，默认用这个；纯白太刺眼，故用浅灰）</summary>
        public static readonly Color TextOnDark = new Color32(220, 224, 230, 255);

        /// <summary>白底上的主文字</summary>
        public static readonly Color TextPrimary = new Color32(48, 49, 51, 255);

        /// <summary>白底上的次要文字</summary>
        public static readonly Color TextSecondary = new Color32(96, 98, 102, 255);

        /// <summary>占位符/提示文字（半透明）</summary>
        public static readonly Color TextPlaceholder = new Color32(192, 196, 204, 160);

        /// <summary>禁用态文字（半透明）</summary>
        public static readonly Color TextDisabled = new Color32(150, 154, 160, 110);

        // ────────────────── ③ 边框 ──────────────────

        /// <summary>控件边框（输入框/下拉/勾选框的外框）</summary>
        public static readonly Color BorderBase = new Color32(144, 147, 153, 255);

        /// <summary>次级边框（分区线）</summary>
        public static readonly Color BorderLight = new Color32(163, 167, 174, 255);

        // ────────────────── ④ 背景 ──────────────────

        /// <summary>弹窗主背景（深色，不透明 —— 面板必须不透明才能拦住射线）</summary>
        public static readonly Color BackgroundPanel = new Color32(38, 43, 51, 255);

        /// <summary>控件底（滑条槽/勾选框未选中填充）</summary>
        public static readonly Color BackgroundControl = new Color32(52, 58, 68, 255);

        /// <summary>白底控件内衬（数值输入框/下拉表头）</summary>
        public static readonly Color BackgroundWarm = new Color32(253, 251, 246, 255);

        /// <summary>纯白</summary>
        public static readonly Color White = Color.white;

        // ────────────────── ⑤ 控件轨道与装饰 ──────────────────

        /// <summary>控件轨道灰（勾选框未选中、分隔线）</summary>
        public static readonly Color ControlTrack = new Color32(90, 97, 110, 255);

        /// <summary>滑条填充（生长进度等"量"的填充条）</summary>
        public static readonly Color SliderFill = new Color32(92, 178, 115, 255);

        /// <summary>滑条槽底</summary>
        public static readonly Color SliderTrack = new Color32(20, 23, 28, 255);

        /// <summary>滑块（被拖动的小方块）</summary>
        public static readonly Color SliderHandle = new Color32(224, 230, 235, 255);

        /// <summary>完全透明（占位/布局撑高用）</summary>
        public static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

        /// <summary>禁用态整体压暗（配合 <see cref="TextDisabled"/> 用于不可交互控件）</summary>
        public static Color Disabled(Color color)
        {
            return new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, color.a * 0.7f);
        }
    }
}
