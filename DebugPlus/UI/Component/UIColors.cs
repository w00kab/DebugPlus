using UnityEngine;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 本 Mod 的**唯一色板**。纪律（用户 2026-09-13 明令，两条都不可违反）：
    ///   ① 颜色一律引用本类，**禁止任何控件里出现硬编码 `new Color(...)` / `Color.white`**；
    ///   ② 类里只有**颜色角色**（语义色 / 文字色 / 边框色 / 背景色），
    ///      **绝不允许出现"某某控件的某某部位"这类具体用处的命名**
    ///      —— 控件该用哪个角色由控件自己决定；改动风格只改本文件。
    ///
    /// 角色清单（用户定的 16 项 + 按角色补齐的背景档位）：
    ///   语义色：Primary / Success / Warning / Danger / Info
    ///   文字色：PrimaryText / RegularText / SecondaryText / Placeholder
    ///   边框色：BorderBase / BorderLight / BorderLighter / BorderExtralight
    ///   背景色：BackgroundB(浅) / BackgroundW(暖) / BackgroundWhite(白) / Background(中) / BackgroundD(深) / BackgroundDeep(最深)
    ///   特殊：Transparent
    ///
    /// 档位关系（相邻档必须看得出边界，故各自独立取值，不做"同色多角色"）：
    ///   BackgroundWhite > BackgroundW > BackgroundB > Background > BackgroundD > BackgroundDeep
    ///
    /// 禁用态一律由 <see cref="Disabled"/> 现算，**不单独占一个色值**。
    /// </summary>
    public static class UIColors
    {
        // ══════════════ 语义色 ══════════════

        /// <summary>主蓝</summary>
        public static readonly Color Primary = new Color32(58, 141, 224, 255);

        /// <summary>成功绿</summary>
        public static readonly Color Success = new Color32(82, 155, 46, 255);

        /// <summary>警告橙</summary>
        public static readonly Color Warning = new Color32(230, 162, 60, 255);

        /// <summary>危险红</summary>
        public static readonly Color Danger = new Color32(209, 64, 64, 255);

        /// <summary>信息灰</summary>
        public static readonly Color Info = new Color32(144, 147, 153, 255);

        // ══════════════ 文字色 ══════════════

        /// <summary>主文字（标题、正文、读数）</summary>
        public static readonly Color PrimaryText = new Color32(220, 224, 230, 255);

        /// <summary>常规文字（标签、按钮文字）</summary>
        public static readonly Color RegularText = new Color32(190, 196, 204, 255);

        /// <summary>次要文字（单位后缀、说明）</summary>
        public static readonly Color SecondaryText = new Color32(160, 166, 176, 255);

        /// <summary>占位符 / 极次要提示</summary>
        public static readonly Color Placeholder = new Color32(120, 126, 136, 255);

        // ══════════════ 边框色（由深到浅四档）══════════════

        /// <summary>基础边框（控件外框、分隔线）</summary>
        public static readonly Color BorderBase = new Color32(90, 99, 113, 255);

        /// <summary>次级边框</summary>
        public static readonly Color BorderLight = new Color32(118, 127, 141, 255);

        /// <summary>较轻边框</summary>
        public static readonly Color BorderLighter = new Color32(146, 154, 167, 255);

        /// <summary>最轻边框</summary>
        public static readonly Color BorderExtralight = new Color32(174, 181, 192, 255);

        // ══════════════ 背景色（由浅到深，相邻档可辨）══════════════

        /// <summary>纯白背景</summary>
        public static readonly Color BackgroundWhite = new Color32(255, 255, 255, 255);

        /// <summary>暖白背景</summary>
        public static readonly Color BackgroundW = new Color32(253, 251, 246, 255);

        /// <summary>浅背景</summary>
        public static readonly Color BackgroundB = new Color32(236, 239, 243, 255);

        /// <summary>中背景</summary>
        public static readonly Color Background = new Color32(52, 58, 68, 255);

        /// <summary>深背景（面板底）</summary>
        public static readonly Color BackgroundD = new Color32(38, 43, 51, 255);

        /// <summary>最深背景（凹槽层，比深背景再暗一档）</summary>
        public static readonly Color BackgroundDeep = new Color32(22, 26, 34, 255);

        // ══════════════ 特殊 ══════════════

        /// <summary>完全透明（不绘制的交互层 / 布局占位）</summary>
        public static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

        /// <summary>把任意颜色压暗为禁用态（禁用不占独立色值，现算）。</summary>
        public static Color Disabled(Color color)
        {
            return new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, color.a);
        }
    }
}
