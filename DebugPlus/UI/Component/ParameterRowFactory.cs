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
    /// 布局规则（oni-ui）：行用 HorizontalLayoutGroup（childControl* = true、childForceExpand* = false），
    /// 标签给固定宽（否则 TMP 的 preferredWidth 会把滑条挤没），滑条栏 flexibleWidth = 1 吃掉剩余宽度；
    /// 只用「根 VLG → 行 HLG」一层嵌套，不在行内再套 LayoutGroup。
    ///
    /// ⚠️ 血的教训（2026-09-13 实机 NRE · IL 偏移 0x10A 定位）：本文件的 GO 一律由 NewUIObject 建，
    /// 它已经挂好 RectTransform ⇒ 后续**只能 GetComponent&lt;RectTransform&gt;() 取**，
    /// 再 AddComponent 一次会**返回 null**（不是抛异常），下一句给 null 设锚点才崩。
    /// </summary>
    public static class ParameterRowFactory
    {
        /// <summary>行高（ConfigPanel 按它算窗口高度）；必须容得下 SliderField 的滑条高度。</summary>
        public const float RowHeight = 56f;

        private const float LabelWidth = 120f;
        private const float LabelFontSize = 15f;
        private const float RowSpacing = 8f;

        /// <summary>建一行参数并绑定（行已挂到 parent 下、排在末尾），返回行组件。</summary>
        public static ParameterRow Create(Parameter parameter, Transform parent)
        {
            GameObject row = NewUIObject("paramRow", parent);
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = RowHeight;
            rowLayout.minHeight = RowHeight;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = RowSpacing;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;  // ★ VLG 的子项是 HLG 时必须 false
            hlg.childForceExpandHeight = false; // ★ 不拉伸高度（否则 VLG 高度分配异常）

            TextMeshProUGUI label = CreateLabel(row, parameter.Label);
            SliderField sliderField = SliderField.Create(row.transform, RowHeight);

            var rowComponent = row.AddComponent<ParameterRow>();
            rowComponent.Bind(parameter, sliderField, label);
            return rowComponent;
        }

        private static TextMeshProUGUI CreateLabel(GameObject row, string text)
        {
            GameObject go = NewUIObject("label", row.transform);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = LabelWidth;
            layout.minWidth = LabelWidth;
            layout.preferredHeight = RowHeight;
            layout.minHeight = RowHeight;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (Localization.FontAsset != null)
            {
                tmp.font = Localization.FontAsset; // 中文字形必须显式指定字体资源
            }
            tmp.text = text;
            tmp.fontSize = LabelFontSize;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = UIColors.RegularText; // 颜色一律走 UIColors（唯一色板）
            tmp.raycastTarget = false; // 装饰层不拦射线
            return tmp;
        }

        /// <summary>
        /// 新建 UI 用 GameObject：先挂 RectTransform，再加其它 UI 组件（oni-ui 规则 1）。
        /// ⚠️ 用本方法建出来的 GO 已有 RectTransform，调用方必须用 GetComponent 取，不能再 AddComponent。
        /// </summary>
        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
