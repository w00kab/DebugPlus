using DebugPlus.Operations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 参数行的**布局工厂 + 类型分派**（批 3-3 · B8）：
    /// 一行 = 「固定宽标签 + 具体控件」，控件由**参数的类型**决定。
    ///
    /// 分派表（批 3-3 定案）：
    /// ```
    /// NumericParameter（Control = Slider） → SliderField（滑条 + 右侧读数）
    /// NumericParameter（Control = Number） → NumberField（步进键 + 可输入数值框 + 单位）
    /// ToggleParameter                      → ToggleField（勾选方块）
    /// ChoiceParameter                      → DropdownField（下拉：header + 浮层列表）
    /// 其它                                 → 一行"暂不支持"占位 + 一条警告日志（不崩、不白屏）
    /// ```
    /// 兜底那一支是**故意的**：以后加了新参数类型却忘了加分派，实机表现为一行明确的提示，
    /// 而不是空白行或整块面板挂掉。
    ///
    /// 职责边界（2026-09-13 按用户拍板拆分）：**控件本身的搭建/交互全在各控件类里**，
    /// 本文件只管"一行怎么排"、"按类型选哪个控件"、以及"把参数的读写通道接到控件上"。
    ///
    /// 布局一律走 <see cref="UIFactory"/>（布局组字段与锚点规则只在那一个文件里定义）：
    /// 行用 HorizontalLayoutGroup（childControl* = true、childForceExpand* = false），
    /// 标签给固定宽（否则 TMP 的 preferredWidth 会把控件挤没），控件栏 flexibleWidth = 1 吃掉剩余宽度；
    /// 只用「根 VLG → 行 HLG」一层嵌套，不在行内再套 LayoutGroup。
    ///
    /// ⚠️ 血的教训（2026-09-13 实机 NRE · IL 偏移 0x10A 定位）：UI 的 GO 一律由
    /// <see cref="UIFactory.NewUIObject"/> 建，它已经挂好 RectTransform ⇒ 后续**只能 GetComponent&lt;RectTransform&gt;() 取**，
    /// 再 AddComponent 一次会**返回 null**（不是抛异常），下一句给 null 设锚点才崩。
    /// </summary>
    public static class ParameterRowFactory
    {
        /// <summary>行高（ConfigPanel 按它算窗口高度）；各控件高度由各自 <c>Style</c> 决定，须容得下。</summary>
        public const float RowHeight = 36f;

        /// <summary>
        /// 行标签一列的固定宽度。
        /// `public`：ConfigPanel 的**自检/范例行**按同一套标签尺寸排（那边是过渡性质的行，不想为它复制常量）。
        /// </summary>
        public const float LabelWidth = 120f;

        /// <summary>行标签字号（同上，供自检行复用同一套尺寸口径）。</summary>
        public const float LabelFontSize = 15f;
        private const float RowSpacing = 8f;

        /// <summary>
        /// 建一行参数并绑定（行已挂到 parent 下、排在末尾），返回行组件。
        /// </summary>
        /// <param name="parameter">参数（决定用哪个控件）</param>
        /// <param name="parent">行父节点（面板根 VLG）</param>
        /// <param name="overlayLayer">面板的**浮层挂载点**（只有下拉用得到；没有则下拉退化为只读）</param>
        public static ParameterRow Create(Parameter parameter, Transform parent, Transform overlayLayer = null)
        {
            if (parameter == null)
            {
                return null;
            }

            GameObject row = UIFactory.NewUIObject("paramRow", parent);
            UIFactory.AddPreferredHeight(row, RowHeight);
            // 行 HLG：标签固定在左、控件栏靠 flexibleWidth 吃掉剩余宽度（forceExpand* 全 false 是默认值）
            UIFactory.AddHLG(row, alignment: TextAnchor.MiddleLeft, spacing: RowSpacing);

            TextMeshProUGUI label = CreateLabel(row, parameter.Label);
            var rowComponent = row.AddComponent<ParameterRow>();

            // ── 分派：数值（滑条 / 数值框）──
            var numeric = parameter as NumericParameter;
            if (numeric != null)
            {
                if (numeric.Control == NumericControl.Number)
                {
                    NumberField numberField = NumberField.Create(row.transform);
                    numberField.Bind(
                        read: numeric.GetValue,
                        write: numeric.SetValue,
                        // ⚠️ 用 Format（**不含单位**）：数值框的单位是独立一列（unit: 参数），
                        //    若传 Display 会在框里再拼一个 "%"，与单位列重复。
                        format: numeric.Format,
                        min: numeric.Min,
                        max: numeric.Max,
                        wholeNumbers: numeric.WholeNumbers,
                        step: 0f,               // 0 = 按范围推导档位（见 NumberField.DeriveStep）
                        unit: numeric.Unit,
                        initial: numeric.GetValue());
                    rowComponent.Initialize(parameter, numberField.Refresh);
                }
                else
                {
                    SliderField sliderField = SliderField.Create(row.transform);
                    sliderField.Bind(
                        read: numeric.GetValue,
                        write: numeric.SetValue,   // 拖动过程中每帧写值：纯写值、零时间依赖
                        format: numeric.Display,
                        min: numeric.Min,
                        max: numeric.Max,
                        wholeNumbers: numeric.WholeNumbers,
                        initial: numeric.GetValue());
                    rowComponent.Initialize(parameter, sliderField.Refresh);
                }
                return rowComponent;
            }

            // ── 分派：布尔 → 勾选方块 ──
            var toggle = parameter as ToggleParameter;
            if (toggle != null)
            {
                ToggleField toggleField = ToggleField.Create(row.transform, toggle.GetValue(), toggle.SetValue);
                rowComponent.Initialize(parameter, toggleField.Refresh);
                return rowComponent;
            }

            // ── 分派：选项 → 下拉 ──
            var choice = parameter as ChoiceParameter;
            if (choice != null)
            {
                DropdownField dropdownField = DropdownField.Create(row.transform, overlayLayer);
                dropdownField.Bind(choice.Options, choice.GetIndex, choice.SetIndex);
                rowComponent.Initialize(parameter, dropdownField.Refresh);
                return rowComponent;
            }

            // ── 兜底：认不出的参数类型 ──
            CreateUnsupportedNote(row, parameter);
            rowComponent.Initialize(parameter, null);
            return rowComponent;
        }

        /// <summary>
        /// 认不出的参数类型：建一行占位说明并打警告。
        /// 走的还是同一行的布局（标签 + 弹性列），所以外观上能立刻看出"是这一行没接上"。
        /// </summary>
        private static void CreateUnsupportedNote(GameObject row, Parameter parameter)
        {
            TextMeshProUGUI note = UIFactory.CreateText(row.transform, "unsupported",
                string.Format((string)STRINGS.UI.DEBUGPLUS.PANEL_PARAM_UNSUPPORTED, parameter.TypeName),
                LabelFontSize, UIColors.Warning, TextAlignmentOptions.MidlineLeft, false);
            UIFactory.AddFlexibleWidth(note.gameObject);
            UIFactory.AddPreferredHeight(note.gameObject, RowHeight);
            Debug.LogWarning("[DebugPlus] 参数类型未接入分派（请在 ParameterRowFactory 里加一支）：" +
                parameter.TypeName + " / " + parameter.Label);
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
