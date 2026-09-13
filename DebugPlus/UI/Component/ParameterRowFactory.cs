using DebugPlus.Operations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 参数行的**布局工厂**：标签 + <see cref="SliderField"/> 组成一行，并绑定成 <see cref="ParameterRow"/>。
    ///
    /// 职责边界（2026-09-13 按用户拍板拆分）：**滑条本身的搭建/交互全在 SliderField**，
    /// 本文件只管"一行怎么排"以及"标签从哪来"。
    ///
    /// 布局一律走 <see cref="UIFactory"/>（布局组字段与锚点规则只在那一个文件里定义）：
    /// 行用 HorizontalLayoutGroup（childControl* = true、childForceExpand* = false），
    /// 标签给固定宽（否则 TMP 的 preferredWidth 会把滑条挤没），滑条栏 flexibleWidth = 1 吃掉剩余宽度；
    /// 只用「根 VLG → 行 HLG」一层嵌套，不在行内再套 LayoutGroup。
    ///
    /// ⚠️ 血的教训（2026-09-13 实机 NRE · IL 偏移 0x10A 定位）：UI 的 GO 一律由
    /// <see cref="UIFactory.NewUIObject"/> 建，它已经挂好 RectTransform ⇒ 后续**只能 GetComponent&lt;RectTransform&gt;() 取**，
    /// 再 AddComponent 一次会**返回 null**（不是抛异常），下一句给 null 设锚点才崩。
    /// </summary>
    public static class ParameterRowFactory
    {
        /// <summary>行高（ConfigPanel 按它算窗口高度）；滑条高度由 <see cref="SliderField.Style"/> 决定，须容得下。</summary>
        public const float RowHeight = 36f;

        /// <summary>
        /// 行标签一列的固定宽度。
        /// `public`：ConfigPanel 的**临时自检行**按同一套标签尺寸排（那边是过渡代码，不想为它复制常量）。
        /// </summary>
        public const float LabelWidth = 120f;

        /// <summary>行标签字号（同上，供自检行复用同一套尺寸口径）。</summary>
        public const float LabelFontSize = 15f;
        private const float RowSpacing = 8f;

        /// <summary>建一行参数并绑定（行已挂到 parent 下、排在末尾），返回行组件。</summary>
        public static ParameterRow Create(Parameter parameter, Transform parent)
        {
            GameObject row = UIFactory.NewUIObject("paramRow", parent);
            UIFactory.AddPreferredHeight(row, RowHeight);
            // 行 HLG：标签固定在左、滑条栏靠 flexibleWidth 吃掉剩余宽度（forceExpand* 全 false 是默认值）
            UIFactory.AddHLG(row, alignment: TextAnchor.MiddleLeft, spacing: RowSpacing);

            TextMeshProUGUI label = CreateLabel(row, parameter.Label);
            SliderField sliderField = SliderField.Create(row.transform); // 高度由栏自身 LayoutElement 交给行布局

            var rowComponent = row.AddComponent<ParameterRow>();
            rowComponent.Bind(parameter, sliderField, label);
            return rowComponent;
        }

        private static TextMeshProUGUI CreateLabel(GameObject row, string text)
        {
            // 标签的字体（中文字形）、不接射线、对齐规则都由 UIFactory 一处保证
            TextMeshProUGUI tmp = UIFactory.CreateText(row.transform, "label", text, LabelFontSize,
                UIColors.RegularText, TextAlignmentOptions.MidlineLeft, false);
            UIFactory.AddFixedSize(tmp.gameObject, LabelWidth, RowHeight);
            return tmp;
        }
    }
}
