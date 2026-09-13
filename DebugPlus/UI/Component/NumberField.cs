using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 「数值框」控件：**只负责 UI**（与 <see cref="SliderField"/> 同款分工），不认识任何游戏类型。
    /// 术语：**数值框** = 这一栏控件；**步进键** = 左右那两个 ◄ / ► 小按钮；**单位后缀** = 数值右侧的单位文本。
    ///
    /// # 结构（一栏 = 一个 HLG，全部子项由布局组排布）
    /// ```
    /// numberField                    ← 本组件根
    ///   LayoutElement: preferredHeight = Style.Height, flexibleWidth = 1
    ///   HorizontalLayoutGroup: spacing = Style.Spacing
    ///   ├── decrease                 ◄ 步进键（固定宽 = Style.StepButtonWidth）
    ///   ├── textField                ← <see cref="TextField"/>（弹性宽：吃满中间剩余）
    ///   ├── increase                 ► 步进键
    ///   └── unit                     单位后缀 TMP（固定宽；无单位时整体 SetActive(false)，布局自动跳过）
    /// ```
    ///
    /// # 「点击即编辑」为什么不用两个控件切换
    /// 数值显示与编辑**合用一个** <see cref="TextField"/>（TMP 输入框平时就是"显示文本"）——
    /// 点它即聚焦、即编辑，退出编辑即回到只读显示。这样一栏里只有一个文本控件，
    /// 不需要"读数/编辑器两个节点抢同一列"（那会逼出 HLG→HLG 嵌套，违反 oni-ui 规则 5）。
    ///
    /// # 步进档位（用户 2026-09-13 拍板：默认按范围推导 + 允许显式覆盖）
    /// <see cref="Style.Step"/> &gt; 0 时用它；为 0（默认）时按范围推导
    /// （见 <see cref="DeriveStep"/>：约 100 次点击走完全程，档位取整齐的 1/2/5×10ⁿ）。
    ///
    /// # 顺序契约（沿用 <see cref="SliderField.Bind"/> 的教训）
    /// `SetRange`（范围 + 初值）→ `SetFormatter` → `SetUnit` → **最后**才接上写回通道（`onChanged`）。
    /// 本组件的程序化路径（`SetRange` / `SetValue` / `Refresh`）**一律不写游戏**，
    /// 只有用户操作（按步进键 / 回车提交编辑）才会调写回。
    ///
    /// # 格式化约定
    /// `format` 只负责**数值本身**的显示形式（如 `"0.##"`），**不要在里拼单位** ——
    /// 单位是一列独立的 <see cref="Style.UnitWidth"/> 文本，且编辑时用户看到的必须是纯数字。
    ///
    /// # 纪律
    /// 颜色只引用 <see cref="UIColors"/>；尺寸常量集中在本类的 <see cref="Style"/>；
    /// 一栏内只挂一个布局组（HLG），不嵌套。
    /// </summary>
    public class NumberField : MonoBehaviour
    {
        /// <summary>外观样式（只放"控件自己的几何与颜色"；栏宽由外层行布局分配，不属于样式）。</summary>
        public class Style
        {
            /// <summary>整栏高（= 两键高 = 内嵌输入框高）。</summary>
            public float Height = 28f;
            /// <summary>栏内子项间距（布局组 spacing）。</summary>
            public float Spacing = 4f;
            /// <summary>步进键宽。</summary>
            public float StepButtonWidth = 26f;
            /// <summary>步进键文字字号。</summary>
            public float StepButtonFontSize = 16f;
            /// <summary>
            /// 减号键文字。⚠️ 默认用 ASCII 的 `"&lt;"`：ONI 的 SDF 字体不含部分 Unicode 装饰符号
            /// （见 NEXT_STEPS §3 / oni-ui 规则 6 的"装饰符号空白"一条），行有余量时可改成 `"◄"` 实机看效果。
            /// </summary>
            public string DecreaseLabel = "<";
            /// <summary>加号键文字（同上，可改 `"►"`）。</summary>
            public string IncreaseLabel = ">";
            /// <summary>单位后缀一列的宽度。</summary>
            public float UnitWidth = 26f;
            /// <summary>单位后缀字号。</summary>
            public float UnitFontSize = 13f;
            /// <summary>步进档位；**0（默认）= 按范围自动推导**（见 <see cref="DeriveStep"/>）。</summary>
            public float Step = 0f;
            /// <summary>
            /// 内嵌输入框的样式（数值框的默认口径：**居中**对齐）。
            /// ⚠️ 本类只**读**它、不改它（`Create` 里改的是实例上的 LayoutElement），
            /// 以免污染 <see cref="DefaultStyle"/> 里被所有实例共享的那一份。
            /// </summary>
            public TextField.Style FieldStyle = new TextField.Style { Alignment = TextAlignmentOptions.Center };
            /// <summary>步进键底色（不透明 —— 透明底收不到点击）。</summary>
            public Color StepButtonColor = UIColors.Background;
            /// <summary>步进键文字色。</summary>
            public Color StepButtonTextColor = UIColors.RegularText;
            /// <summary>单位后缀色（次要文字）。</summary>
            public Color UnitColor = UIColors.SecondaryText;
        }

        /// <summary>默认样式（未显式传样式时使用）。</summary>
        public static readonly Style DefaultStyle = new Style();

        /// <summary>本实例的样式。</summary>
        public Style CurrentStyle { get; private set; }

        private Button decreaseButton;
        private Button increaseButton;
        private TextField field;
        private TextMeshProUGUI unitText;

        private System.Func<float> read;
        private System.Func<float, string> formatter;

        private float min;
        private float max;
        private float step;
        private bool wholeNumbers;
        private float cachedValue;

        /// <summary>用户操作产生新值时触发（按步进键 / 回车提交）；<b>程序化设值不触发</b>。</summary>
        public System.Action<float> onChanged;

        /// <summary>当前值（程序化缓存，与控件显示同步）。</summary>
        public float Value
        {
            get { return cachedValue; }
        }

        /// <summary>当前生效的步进档位。</summary>
        public float StepSize
        {
            get { return step; }
        }

        // ══════════════════ 构建 ══════════════════

        /// <summary>
        /// 建一整栏并挂到 <paramref name="parent"/> 下（行内使用）。栏宽 `flexibleWidth = 1`（吃满剩余），
        /// 高度 = <see cref="Style.Height"/>。
        /// </summary>
        /// <param name="parent">父节点（通常是行 HLG）</param>
        /// <param name="style">自定义样式（null = 用 <see cref="DefaultStyle"/>）</param>
        public static NumberField Create(Transform parent, Style style = null)
        {
            Style current = style ?? DefaultStyle;

            GameObject root = UIFactory.NewUIObject("numberField", parent);
            var layout = UIFactory.GetOrAddLayout(root);
            layout.preferredHeight = current.Height;
            layout.minHeight = current.Height;
            layout.flexibleWidth = 1f;
            UIFactory.AddHLG(root, alignment: TextAnchor.MiddleLeft, spacing: current.Spacing);

            var component = root.AddComponent<NumberField>();
            component.CurrentStyle = current;
            component.min = 0f;
            component.max = 1f;
            component.wholeNumbers = false;
            component.step = DeriveStep(0f, 1f, false); // SetRange 之前的兜底档位（比例 0–1 ⇒ 0.01）

            component.decreaseButton = CreateStepButton(root.transform, "decrease",
                current.DecreaseLabel, current, component.OnDecreaseClicked);
            component.field = TextField.Create(root.transform, current.FieldStyle);
            component.increaseButton = CreateStepButton(root.transform, "increase",
                current.IncreaseLabel, current, component.OnIncreaseClicked);
            component.unitText = CreateUnit(root.transform, current);

            // 内嵌输入框的高度以本栏的框高为准（组合方决定整栏高，见 Style.FieldStyle 的注释）
            var fieldLayout = UIFactory.GetOrAddLayout(component.field.gameObject);
            fieldLayout.preferredHeight = current.Height;
            fieldLayout.minHeight = current.Height;

            component.field.onEndEdit = component.OnEditSubmitted;
            component.cachedValue = component.min;
            return component;
        }

        private static Button CreateStepButton(Transform parent, string name, string label,
            Style style, System.Action onClick)
        {
            // 纯代码按钮走 UIFactory.CreateButton（Button 而非 KButton，transition = None）
            return UIFactory.CreateButton(parent, name, label, onClick, style.StepButtonFontSize,
                style.StepButtonColor, style.StepButtonTextColor,
                width: style.StepButtonWidth, height: style.Height);
        }

        private static TextMeshProUGUI CreateUnit(Transform parent, Style style)
        {
            TextMeshProUGUI text = UIFactory.CreateText(parent, "unit", string.Empty,
                style.UnitFontSize, style.UnitColor, TextAlignmentOptions.MidlineLeft, false);
            UIFactory.AddFixedSize(text.gameObject, style.UnitWidth, style.Height);
            text.gameObject.SetActive(false); // 没单位时整体隐藏：布局组会跳过未激活子项，不留空档
            return text;
        }

        // ══════════════════ 绑定 / 取值设值 ══════════════════

        /// <summary>
        /// 🔴 **绑定接口（推荐用法）**：把"读哪个值、写回哪里、怎么显示、什么单位"一次性交给本组件，
        /// 由组件内部保证调用顺序 —— 调用方**再也不可能**把顺序写错。
        /// </summary>
        /// <param name="read">读当前值（null = 不主动读，值只由 <see cref="SetValue"/> 决定）</param>
        /// <param name="write">写回值（按步进键 / 提交编辑时调用；<b>写的是控件刻度上的值</b>）</param>
        /// <param name="format">数值显示格式化（null = 保留三位小数；**不要在里面拼单位**）</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="wholeNumbers">是否只取整数刻度</param>
        /// <param name="step">步进档位；≤ 0 表示按范围自动推导</param>
        /// <param name="unit">单位后缀（null / 空串 = 不显示单位列）</param>
        /// <param name="initial">初值；传 float.NaN 表示"从 <paramref name="read"/> 读"</param>
        public NumberField Bind(System.Func<float> read, System.Action<float> write,
            System.Func<float, string> format = null,
            float min = 0f, float max = 1f, bool wholeNumbers = false,
            float step = 0f, string unit = null, float initial = float.NaN)
        {
            float start = float.IsNaN(initial)
                ? (read != null ? read() : min)
                : initial;

            SetRange(min, max, wholeNumbers, step, start); // 范围 + 初值：不写游戏
            SetFormatter(format);
            SetUnit(unit);
            this.read = read;
            if (write != null)
            {
                onChanged = write; // 最后才接上写回通道
            }
            return this;
        }

        /// <summary>
        /// 设范围、步进上限与初值，**不通知任何回调**（初值是"从游戏读出来的现状"，不该被当成用户操作写回去）。
        /// </summary>
        /// <param name="step">步进档位；≤ 0 = 按范围自动推导</param>
        public void SetRange(float min, float max, bool wholeNumbers, float step, float initialValue)
        {
            // 范围写反了也照收：内部统一成 min ≤ max
            this.min = Mathf.Min(min, max);
            this.max = Mathf.Max(min, max);
            this.wholeNumbers = wholeNumbers;
            this.step = step > 0f ? step : DeriveStep(this.min, this.max, wholeNumbers);
            cachedValue = Clamp(initialValue);
            RefreshDisplay();
        }

        /// <summary>设置单位后缀（null / 空串 = 隐藏单位列）。</summary>
        public void SetUnit(string unit)
        {
            if (unitText == null)
            {
                return;
            }
            bool show = !string.IsNullOrEmpty(unit);
            unitText.gameObject.SetActive(show);
            if (show)
            {
                unitText.text = unit;
            }
        }

        /// <summary>设置数值显示格式化（由使用方提供显示规则）；设置后立即刷一次。</summary>
        public void SetFormatter(System.Func<float, string> formatter)
        {
            this.formatter = formatter;
            RefreshDisplay();
        }

        /// <summary>程序化设值（不触发 <see cref="onChanged"/>），用于"游戏侧变了要同步到界面"。</summary>
        public void SetValue(float value, bool notify = false)
        {
            cachedValue = Clamp(value);
            RefreshDisplay();
            if (notify && onChanged != null)
            {
                onChanged(cachedValue);
            }
        }

        /// <summary>按步进档位走一格（<paramref name="direction"/> 取 +1 / −1）；用户点 ◄ ► 走的就是它。</summary>
        public void StepBy(int direction)
        {
            if (direction == 0)
            {
                return;
            }
            float value = cachedValue + step * direction;
            // 消掉步长小于 1 时的浮点累加尾巴（0.1+0.1+0.1 = 0.30000000000000004 那类）
            if (step > 0f && step < 1f)
            {
                value = (float)System.Math.Round(value, StepDecimals(step));
            }
            SetValue(value, true);
        }

        /// <summary>
        /// 从游戏侧重读并刷新显示（用户正在编辑时**不覆盖**他手上的文本，只更新内部缓存）。
        /// </summary>
        public void Refresh()
        {
            if (read != null)
            {
                cachedValue = Clamp(read());
            }
            RefreshDisplay();
        }

        /// <summary>整体可交互开关（例如参数当前不可调时压掉整栏）。</summary>
        public void SetInteractable(bool interactable)
        {
            if (decreaseButton != null)
            {
                decreaseButton.interactable = interactable;
            }
            if (increaseButton != null)
            {
                increaseButton.interactable = interactable;
            }
            if (field != null)
            {
                field.SetInteractable(interactable);
            }
        }

        // ══════════════════ 步进档位推导 ══════════════════

        /// <summary>
        /// 按范围推导步进档位：让**约 100 次点击走完全程**，档位取整齐的 1 / 2 / 5 × 10ⁿ。
        /// 例：[0,100] → 1；[0,1000] → 10；[0,5000] → 50；[0,1]（比例） → 0.01。
        /// 整数刻度时至少为 1（避免推不出整数步长导致按了没反应）。
        /// </summary>
        public static float DeriveStep(float min, float max, bool wholeNumbers)
        {
            float span = Mathf.Abs(max - min);
            if (span <= 0f)
            {
                return wholeNumbers ? 1f : 0.01f;
            }

            float raw = span / 100f;
            float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(raw))); // raw 的量级（1/10/0.01…）
            float normalized = raw / magnitude;                              // 落在 [1,10)
            float nice = normalized <= 1f ? 1f
                : normalized <= 2f ? 2f
                : normalized <= 5f ? 5f
                : 10f;
            float derived = nice * magnitude;
            return wholeNumbers ? Mathf.Max(1f, Mathf.Round(derived)) : derived;
        }

        /// <summary>步长的小数位数（0.01 → 2、0.5 → 1、1 → 0），用于消浮点尾巴。</summary>
        private static int StepDecimals(float step)
        {
            if (step <= 0f)
            {
                return 0;
            }
            return Mathf.Clamp((int)Mathf.Floor(-Mathf.Log10(step)), 0, 6);
        }

        // ══════════════════ 内部 ══════════════════

        private void OnDecreaseClicked()
        {
            StepBy(-1);
        }

        private void OnIncreaseClicked()
        {
            StepBy(1);
        }

        /// <summary>
        /// 用户结束编辑（回车提交 / 点到别处 / Esc 取消）：
        /// Esc 取消（TMP 已把文本还原成原文）与解析失败都**静默回退到当前值**，绝不把半截输入写进游戏。
        /// </summary>
        private void OnEditSubmitted(string raw)
        {
            if (field == null)
            {
                return;
            }
            if (field.WasCanceled)
            {
                RefreshDisplay();
                return;
            }

            float parsed;
            bool parsedOk = float.TryParse((raw ?? string.Empty).Trim(),
                NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
            if (!parsedOk)
            {
                RefreshDisplay();
                return;
            }
            SetValue(parsed, true); // 用户提交 = 用户操作 ⇒ 写回
        }

        private void RefreshDisplay()
        {
            if (field == null)
            {
                return;
            }
            if (field.IsFocused)
            {
                return; // 用户正在输入：绝不覆盖他手上的文本（提交或取消时再刷）
            }
            field.SetText(Format(cachedValue));
        }

        private string Format(float value)
        {
            return formatter != null ? formatter(value) : value.ToString("0.###");
        }

        private float Clamp(float value)
        {
            if (wholeNumbers)
            {
                value = Mathf.Round(value);
            }
            if (value < min)
            {
                value = min;
            }
            if (value > max)
            {
                value = max;
            }
            return value;
        }
    }
}
