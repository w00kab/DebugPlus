using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 「勾选方块」控件：一个 22×22 的可点击方块（边框 + 填充 + ✓），用于布尔参数
    /// （例：动物是否驯服、M4 创造建筑是否屏蔽）。
    ///
    /// 结构（两层 Image，职责分明）：
    ///   根 GO：`Image` 边框（圆角 sprite + `Image.color` 上色）—— **它就是可交互层**
    ///     └ `Fill` 子节点：内缩 2px 的填充（勾选 = 成功绿，未勾选 = 控件轨道灰），`raycastTarget = false`
    ///     └ `Check` 子节点：TMP 的 "✓"，`raycastTarget = false`
    ///
    /// 三条踩坑防护（尺寸全靠 LayoutElement 显式给，不给布局组"算出来是 0"的机会）：
    /// ① **根/go 必须显式 `preferredWidth` + `minWidth`** —— 只给 `flexibleWidth` 时，
    ///    在 `childForceExpandWidth = false` 的行里宽度会算成 0，方块被挤出可视区（或被兄弟按钮压住）；
    /// ② **高度也必须显式给**（`preferredHeight` + `minHeight`）—— 不设时会被 `Image` 的
    ///    `preferredHeight = 0` 覆盖，方块高度变 0、行内看不见；
    /// ③ 交互层**必须不透明且 `raycastTarget = true`**（本控件的边框 Image 即交互层），
    ///    装饰层一律 `raycastTarget = false`，否则点击会被填充/文字拦掉。
    ///
    /// 命名口径（本 Mod 纪律）：术语是**勾选方块**（不用 "Toggle 开关"，
    /// 因为滑动式开关是另一种控件，待需要时另建）。
    /// </summary>
    public class ToggleField : MonoBehaviour
    {
        /// <summary>方块边长（px）。</summary>
        public const float Size = 22f;

        /// <summary>
        /// 外观样式：尺寸 + 四处颜色。默认值即本 Mod 的统一风格（颜色取自 <see cref="UIColors"/>）；
        /// 想给某个实例换观感，构造一个改过的样式传进 <see cref="Create"/>，**不要改这里的默认值**。
        /// </summary>
        public class Style
        {
            /// <summary>方块边长。</summary>
            public float Size = 22f;
            /// <summary>填充层相对边框的内缩。</summary>
            public float FillInset = 2f;
            /// <summary>✓ 标记字号。</summary>
            public float CheckFontSize = 13f;
            /// <summary>边框色（未勾选与勾选共用同一个框）。</summary>
            public Color BorderColor = UIColors.BorderBase;
            /// <summary>填充色（勾选 = 成功绿；未勾选 = 中背景）。</summary>
            public Color CheckedColor = UIColors.Success;
            /// <summary>未勾选时的填充色。</summary>
            public Color UncheckedColor = UIColors.Background;
            /// <summary>✓ 标记颜色。</summary>
            public Color CheckMarkColor = UIColors.BackgroundB;
        }

        /// <summary>默认样式（未显式传样式时使用）。</summary>
        public static readonly Style DefaultStyle = new Style();

        /// <summary>本实例的样式。</summary>
        public Style CurrentStyle { get; private set; }

        private Image borderImage;
        private Image fillImage;
        private TextMeshProUGUI checkText;
        private Button button;

        private bool value;
        private bool interactable = true;

        /// <summary>读当前状态的通道（<see cref="Bind"/> 时记下，供 <see cref="Refresh"/> 用）。</summary>
        private System.Func<bool> read;

        /// <summary>用户点击改变勾选状态时触发；<b>程序化 <see cref="SetValue"/> 默认不触发</b>。</summary>
        public System.Action<bool> onChanged;

        /// <summary>当前是否勾选。</summary>
        public bool Value
        {
            get { return value; }
        }

        // ══════════════════ 构建 ══════════════════

        /// <summary>
        /// 建一个勾选方块并挂到 <paramref name="parent"/> 下（已挂 LayoutElement 尺寸，行内直接可用）。
        /// </summary>
        /// <param name="parent">父节点（通常是行 HLG 容器）</param>
        /// <param name="initialValue">初始勾选状态</param>
        /// <param name="onChanged">点击回调（参数为点击后的状态）</param>
        public static ToggleField Create(Transform parent, bool initialValue = false,
            System.Action<bool> onChanged = null, Style style = null)
        {
            Style current = style ?? DefaultStyle;
            GameObject root = UIFactory.NewUIObject("toggleField", parent);
            // ①②：宽度与高度都显式给（只给 flexibleWidth 会被算成 0 宽）
            UIFactory.AddFixedSize(root, current.Size, current.Size);

            var field = root.AddComponent<ToggleField>();
            field.CurrentStyle = current;
            field.onChanged = onChanged;
            field.value = initialValue;
            field.BuildVisual();
            field.ApplyVisual();
            return field;
        }

        /// <summary>
        /// 🔴 **绑定接口**：把"读哪个值、写回哪里"交给本组件 —— 勾选由用户点击触发时，
        /// 组件直接把新状态写回（调用方不必再写一个回调方法）。
        /// 初值同样在挂载前写入且**不触发**写回（现状不该被当成用户操作）。
        /// </summary>
        /// <param name="read">读当前状态</param>
        /// <param name="write">写回状态</param>
        /// <param name="initial">初值；传 null 表示"从 <paramref name="read"/> 读"</param>
        public ToggleField Bind(System.Func<bool> read, System.Action<bool> write, bool? initial = null)
        {
            this.read = read; // 记下读通道：Refresh() 靠它重读
            bool start = initial ?? (read != null && read());
            value = start;
            ApplyVisual();
            if (write != null)
            {
                onChanged = write;
            }
            return this;
        }

        /// <summary>
        /// 从参数侧重读并同步到界面（"游戏侧现值变了要刷一下"时用）。
        /// 与 <see cref="SetValue"/> 一样**不触发** <see cref="onChanged"/> —— 刷新不是用户操作。
        /// </summary>
        public void Refresh()
        {
            if (read != null)
            {
                SetValue(read());
            }
        }

        /// <summary>
        /// 建一个「勾选方块 + 说明文字」的横向组合（自带 HLG 容器，直接当一行用）。
        /// 顺序：方块在左、文字在右（ONI 原版勾选项的习惯顺序）。
        /// </summary>
        /// <param name="parent">父节点（通常是面板的根 VLG）</param>
        /// <param name="label">说明文字</param>
        /// <param name="rowHeight">行高（同时是容器的 preferredHeight）</param>
        /// <param name="onChanged">点击回调</param>
        /// <param name="initialValue">初始勾选状态</param>
        public static ToggleField CreateWithLabel(Transform parent, string label, float rowHeight,
            bool initialValue = false, System.Action<bool> onChanged = null)
        {
            GameObject row = UIFactory.NewUIObject("toggleRow", parent);
            UIFactory.AddPreferredHeight(row, rowHeight);
            // ⚠️ 一行只能有一个布局组：直接挂 HLG（不再经过 VLG）
            UIFactory.AddHLG(row, alignment: TextAnchor.MiddleLeft, spacing: 8f);

            ToggleField field = Create(row.transform, initialValue, onChanged);

            var text = UIFactory.CreateText(row.transform, "label", label, 15f, UIColors.RegularText,
                TextAlignmentOptions.MidlineLeft, false);
            UIFactory.AddFlexibleWidth(text.gameObject);
            UIFactory.AddPreferredHeight(text.gameObject, rowHeight);
            return field;
        }

        private void BuildVisual()
        {
            // 边框 = 可交互层（不透明 + 接射线，③）
            borderImage = UIFactory.AddBackground(gameObject, CurrentStyle.BorderColor, true,
                UISpriteFactory.GetRoundedRect(32, 4));

            // 填充层：内缩
            GameObject fill = UIFactory.NewUIObject("fill", transform);
            UIFactory.StretchWithInset(fill, CurrentStyle.FillInset, CurrentStyle.FillInset);
            fillImage = UIFactory.AddDecoration(fill, CurrentStyle.UncheckedColor,
                UISpriteFactory.GetRoundedRect(32, 3));

            // ✓ 标记（TMP 独占 GO + 显式中文字体资源）
            checkText = UIFactory.CreateText(transform, "check", "✓", CurrentStyle.CheckFontSize,
                CurrentStyle.CheckMarkColor, TextAlignmentOptions.Center, false);
            UIFactory.Stretch(checkText.gameObject);

            // 点击交互：transition = None（只用 onClick，避免 ColorTint 把方块颜色相乘变色）
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = borderImage;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnClick);
        }

        // ══════════════════ 取值 / 设值 ══════════════════

        /// <summary>
        /// 程序化设值（用于"游戏侧现状要同步到界面"）。
        /// ⚠️ 默认**不触发** <see cref="onChanged"/> —— 否则"从游戏读现状 → 设进控件"会被当成一次用户操作写回游戏。
        /// </summary>
        /// <param name="newValue">新状态</param>
        /// <param name="notify">是否触发 onChanged（默认 false）</param>
        public void SetValue(bool newValue, bool notify = false)
        {
            if (value == newValue)
            {
                return;
            }
            value = newValue;
            ApplyVisual();
            Notify(notify);
        }

        /// <summary>设置是否可交互（禁用时压暗且不响应点击，但仍会吞掉点击以免穿透到下层）。</summary>
        public void SetInteractable(bool newInteractable)
        {
            interactable = newInteractable;
            if (button != null)
            {
                button.interactable = newInteractable;
            }
            ApplyVisual();
        }

        // ══════════════════ 内部 ══════════════════

        private void OnClick()
        {
            if (!interactable)
            {
                return;
            }
            value = !value;
            ApplyVisual();
            Notify(true);
        }

        /// <summary>按当前状态刷新视觉（勾选 = 绿填充 + ✓；未勾选 = 中背景 + 无 ✓）。<b>可覆写</b>。</summary>
        protected virtual void ApplyVisual()
        {
            if (fillImage != null)
            {
                fillImage.color = value ? CurrentStyle.CheckedColor : CurrentStyle.UncheckedColor;
            }
            if (checkText != null)
            {
                checkText.text = value ? "✓" : string.Empty;
                checkText.color = interactable
                    ? CurrentStyle.CheckMarkColor
                    : UIColors.Disabled(CurrentStyle.CheckMarkColor);
            }
            if (borderImage != null)
            {
                borderImage.color = interactable
                    ? CurrentStyle.BorderColor
                    : UIColors.Disabled(CurrentStyle.BorderColor);
            }
        }

        private void Notify(bool notify)
        {
            if (!notify || onChanged == null)
            {
                return;
            }
            onChanged(value);
        }
    }
}
