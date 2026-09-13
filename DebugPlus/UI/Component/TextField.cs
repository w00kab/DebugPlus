using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 「文本输入框」控件：**纯代码构造**的原生 <see cref="TMP_InputField"/>（不派生原版
    /// <c>KInputTextField</c>，理由见下），外部只与本组件打交道。
    ///
    /// # 结构（照 TMP 官方 InputField 预制体的层级，自己搭）
    /// ```
    /// textField                       ← 本组件根：Image 不透明底 = 可交互层 + 命中面积
    ///   LayoutElement: preferredHeight = 框高, flexibleWidth = 1（宽度交给外层行布局）
    ///   ├── textArea                  ← textViewport（水平/垂直内缩 = 文字内边距；挂 RectMask2D 裁剪）
    ///   │     ├── text                ← textComponent（TMP 独占一个 GO）
    ///   │     ├── placeholder         ← 可选（Style.Placeholder 非空才建）
    ///   │     └── Caret               ← **TMP 自己在 OnEnable 里建**（见下）
    /// ```
    ///
    /// # 为什么可以纯代码构造（2026-09-13 逐行核对本体代码）
    /// ① `textViewport` / `textComponent` / `placeholder` 都是**可写公开属性**
    ///    （TMP_InputField.cs:378-421）⇒ 不用预制体也能把引用挂齐（与 `KSlider` 同理）。
    /// ② **光标不用我们管**：`TMP_InputField.OnEnable()`（:1231-1291）在 `m_TextComponent != null` 时
    ///    自己建光标（`new GameObject("Caret", typeof(TMP_SelectionCaret))`，:1250-1262），挂到
    ///    textComponent 的父节点（= textArea）下；`LateUpdate()`（:1612-1679）每帧调
    ///    `AssignPositioningIfNeeded()`（:3411-3424）把光标矩形同步成文字矩形。
    ///    ⇒ 我们唯一要保证的是「**OnEnable 跑之前 textComponent 已就位**」，
    ///    故沿用 KSlider 那招：先 `SetActive(false)` → 挂齐引用 → 最后 `SetActive(true)`。
    /// ③ 结束编辑的通知走 `onEndEdit`：它在 TMP 里**只由 `ReleaseSelection()` 发出**（:3969-3977），
    ///    而 `DeactivateInputField()`（:3986-4025）在 `m_ResetOnDeActivation` 为真且无纵向滚动条时必然调它
    ///    ⇒ 回车（`OnSubmit` :4036-4052）与点到别处失焦都会走到；本组件显式把 `m_ResetOnDeActivation` 置 true。
    ///    另：Esc 取消时 TMP **先**把文本还原成进入编辑前的原文（:4000-4003）再发 onEndEdit，
    ///    所以用 <see cref="WasCanceled"/> 判定即可避免"Esc 也算一次提交"。
    ///
    /// # 为什么**不派生**原版 `KInputTextField`
    /// 它比基类只多两件事——"值变化延迟通知（0.2s）"与手柄输入（KInputTextField.cs:9-54），与需求无关；
    /// 且它的无参构造在反编译产物里是 **private**（:45），派生/实例化的合法性都有疑问。
    /// 代价：原版 `CameraController.WithinInputField()`（CameraController.cs:527-540）只认
    /// `KInputTextField` / `InputField`，认不出本控件 ⇒ 聚焦期的热键由
    /// `Patches/InputHandler_HandleEvent_Patch.cs` 在**根入口**统一吞掉（那是本批 B6 的职责）。
    ///
    /// # 与吞键的分工
    /// 打字是 uGUI/TMP 直接读 Unity 输入（`Event.PopEvent`）完成的，**不经过游戏的 KInput 链**；
    /// 所以吞掉 KInput 事件不会影响输入框收字，只影响"同一次按键又被游戏热键消费一遍"。
    /// <see cref="IsEditing"/> 就是给那个补丁看的开关。
    ///
    /// # 纪律
    /// 颜色一律引用 <see cref="UIColors"/>（本类不含任何 `new Color(...)`）；尺寸常量集中在 <see cref="Style"/>；
    /// 装饰文本 `raycastTarget = false`，可交互底必须不透明（oni-ui 规则 12 / 14）。
    /// </summary>
    public class TextField : MonoBehaviour
    {
        /// <summary>外观样式（只放"控件自己的几何与颜色"；宽度由外层布局分配，不属于样式）。</summary>
        public class Style
        {
            /// <summary>框高（= 本组件 preferredHeight；组合方如 NumberField 会用它统一整行高度）。</summary>
            public float Height = 28f;
            /// <summary>文字左右内边距（= textArea 的水平内缩）。</summary>
            public float HorizontalInset = 6f;
            /// <summary>文字上下内边距（= textArea 的垂直内缩）。</summary>
            public float VerticalInset = 2f;
            /// <summary>文字字号。</summary>
            public float FontSize = 15f;
            /// <summary>文字对齐（数值框习惯居中；一般文本框靠左）。</summary>
            public TextAlignmentOptions Alignment = TextAlignmentOptions.MidlineLeft;
            /// <summary>字形上限（防超长串把布局撑坏）。</summary>
            public int CharacterLimit = 16;
            /// <summary>TMP 的内容类型：数值框用 <c>IntegerNumber</c> / <c>DecimalNumber</c>（顺带做按键过滤）。</summary>
            public TMP_InputField.ContentType ContentType = TMP_InputField.ContentType.Standard;
            /// <summary>空内容时的占位文字（空串 = 不建占位节点）。</summary>
            public string Placeholder = string.Empty;
            /// <summary>底色（**必须不透明**：alpha=0 的 Graphic 会被剔除，收不到点击与聚焦）。</summary>
            public Color BackgroundColor = UIColors.BackgroundDeep;
            /// <summary>文字色。</summary>
            public Color TextColor = UIColors.PrimaryText;
            /// <summary>占位文字色。</summary>
            public Color PlaceholderColor = UIColors.Placeholder;
            /// <summary>
            /// 光标色：TMP 默认的 `caretColor` 是深灰（50,50,50），落在深色底上根本看不见 ⇒ **必须显式给**。
            /// </summary>
            public Color CaretColor = UIColors.BackgroundWhite;
        }

        /// <summary>默认样式（未显式传样式时使用）。</summary>
        public static readonly Style DefaultStyle = new Style();

        /// <summary>全 Mod 正在编辑的输入框数量（供吞键补丁判定；不新增独立的状态类）。</summary>
        private static int editingCount;

        /// <summary>是否有本 Mod 的输入框正在编辑 —— 吞键补丁只认这一个开关。</summary>
        public static bool IsEditing
        {
            get { return editingCount > 0; }
        }

        /// <summary>
        /// 正在编辑的输入框**个数**（正常只可能是 0 或 1）。
        /// 单独暴露它而不只给 <see cref="IsEditing"/>，是因为计数**会漏**：组件销毁时机不由本类控制，
        /// 漏一次就永久卡在"编辑中"（整个游戏的键盘失灵），而 `IsEditing` 只能看出"卡了"、
        /// 看不出"卡在几" —— 面板的「控件自检 · 用法范例」区把这个数直接显示出来，肉眼即可判断是否泄漏。
        /// </summary>
        public static int EditingCount
        {
            get { return editingCount; }
        }

        /// <summary>本实例的样式。</summary>
        public Style CurrentStyle { get; private set; }

        private TMP_InputField input;
        private TextMeshProUGUI text;
        private bool editing;

        /// <summary>用户结束编辑时触发（回车提交 / 点到别处 / Esc 取消都算；Esc 取消时文本已被 TMP 还原）。</summary>
        public System.Action<string> onEndEdit;

        /// <summary>内容变化时触发（每次按键；程序化 <see cref="SetText"/> 默认不触发）。</summary>
        public System.Action<string> onChanged;

        /// <summary>当前文本（只读）。</summary>
        public string Text
        {
            get { return input != null ? input.text : string.Empty; }
        }

        /// <summary>本次结束编辑是否由 Esc 取消（TMP 的 `wasCanceled`，供外面决定"不提交"）。</summary>
        public bool WasCanceled
        {
            get { return input != null && input.wasCanceled; }
        }

        /// <summary>本实例是否正在编辑（用于"用户正在输入时别用程序化的值覆盖他"）。</summary>
        public bool IsFocused
        {
            get { return editing; }
        }

        // ══════════════════ 构建 ══════════════════

        /// <summary>
        /// 建一个输入框并挂到 <paramref name="parent"/> 下。高度按 <see cref="Style.Height"/> 交给布局组，
        /// 宽度默认 `flexibleWidth = 1`（吃满行内剩余）；要固定宽请在返回后自行改它的 LayoutElement。
        /// </summary>
        public static TextField Create(Transform parent, Style style = null)
        {
            Style current = style ?? DefaultStyle;

            GameObject root = UIFactory.NewUIObject("textField", parent);
            var layout = UIFactory.GetOrAddLayout(root);
            layout.preferredHeight = current.Height;
            layout.minHeight = current.Height;
            layout.flexibleWidth = 1f;

            // 🔴 先不激活：TMP_InputField 的 Awake/OnEnable 要等激活才跑，而 OnEnable 会 **立刻用 m_TextComponent
            //    建光标** ⇒ 必须等引用挂齐后再激活（与 SliderField 建 KSlider 是同一招，教训写在 NEXT_STEPS §3.7）。
            root.SetActive(false);

            var field = root.AddComponent<TextField>();
            field.CurrentStyle = current;

            // 可交互底：不透明 + 接射线（textArea/文字都在更上层，点击照样落到这里 → 冒泡到 TMP_InputField）
            Image background = UIFactory.AddBackground(root, current.BackgroundColor, true);

            // 视口：文字区（内缩 = 文字内边距）+ RectMask2D 裁剪（TMP 官方预制体的 TextArea 同款做法；
            // TMP_InputField.OnEnable 会取它算遮罩区域，:1270-1274）
            GameObject textArea = UIFactory.NewUIObject("textArea", root.transform);
            UIFactory.StretchWithInset(textArea, current.HorizontalInset, current.VerticalInset);
            textArea.AddComponent<RectMask2D>();

            field.text = UIFactory.CreateText(textArea.transform, "text", string.Empty,
                current.FontSize, current.TextColor, current.Alignment, false);
            UIFactory.Stretch(field.text.gameObject);

            TextMeshProUGUI placeholder = null;
            if (!string.IsNullOrEmpty(current.Placeholder))
            {
                placeholder = UIFactory.CreateText(textArea.transform, "placeholder", current.Placeholder,
                    current.FontSize, current.PlaceholderColor, current.Alignment, false);
                UIFactory.Stretch(placeholder.gameObject);
            }

            var inputField = root.AddComponent<TMP_InputField>();
            inputField.targetGraphic = background;
            inputField.transition = Selectable.Transition.None; // 纯代码不用 ColorTint（oni-ui 规则 13）
            inputField.textViewport = UIFactory.Rect(textArea);
            inputField.textComponent = field.text;              // ← 必须早于激活（见上）
            inputField.placeholder = placeholder;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.contentType = current.ContentType;
            inputField.characterLimit = current.CharacterLimit;
            inputField.caretColor = current.CaretColor;
            inputField.customCaretColor = true;
            inputField.resetOnDeActivation = true;              // 失焦即结束编辑并发 onEndEdit（:4010）
            inputField.restoreOriginalTextOnEscape = true;      // Esc 取消时还原原文（:4000）
            field.input = inputField;

            // UnityEvent 字段在 TMP 里有内联初始化（:4465 `= new SubmitEvent()`），仍留空判保险
            if (inputField.onSelect != null)
            {
                inputField.onSelect.AddListener(field.OnSelected);
            }
            if (inputField.onDeselect != null)
            {
                inputField.onDeselect.AddListener(field.OnDeselected);
            }
            if (inputField.onEndEdit != null)
            {
                inputField.onEndEdit.AddListener(field.OnEndEditInternal);
            }
            if (inputField.onValueChanged != null)
            {
                inputField.onValueChanged.AddListener(field.OnValueChangedInternal);
            }

            root.SetActive(true); // 此刻 TMP_InputField 的 OnEnable 跑 → 光标就位
            return field;
        }

        // ══════════════════ 取值 / 设值 ══════════════════

        /// <summary>
        /// 程序化设文本。
        /// ⚠️ 默认**不触发** <see cref="onChanged"/> —— 否则"把游戏现状灌进控件"会被当成一次用户输入。
        /// </summary>
        public void SetText(string value, bool notify = false)
        {
            if (input == null)
            {
                return;
            }
            if (notify)
            {
                input.text = value ?? string.Empty;
            }
            else
            {
                input.SetTextWithoutNotify(value ?? string.Empty);
            }
        }

        /// <summary>聚焦并进入编辑（等价于用户点进这个框）。</summary>
        public void Focus()
        {
            if (input == null || !input.IsActive() || !input.IsInteractable())
            {
                return;
            }
            input.Select();
            input.ActivateInputField();
        }

        /// <summary>主动结束编辑（失焦；TMP 会随之发出 onEndEdit）。</summary>
        public void Unfocus()
        {
            if (input != null)
            {
                input.DeactivateInputField();
            }
        }

        /// <summary>是否可交互（false 时点击与聚焦都不响应）。</summary>
        public void SetInteractable(bool interactable)
        {
            if (input != null)
            {
                input.interactable = interactable;
            }
        }

        /// <summary>是否只读（仍可聚焦，但按键不改变内容）。</summary>
        public void SetReadOnly(bool readOnly)
        {
            if (input != null)
            {
                input.readOnly = readOnly;
            }
        }

        // ══════════════════ 编辑态计数 ══════════════════

        private void OnSelected(string value)
        {
            EnterEditing();
        }

        private void OnDeselected(string value)
        {
            ExitEditing();
        }

        private void OnEndEditInternal(string value)
        {
            // 先退出编辑态：此刻开始（以及随后 onEndEdit 回调里的写值）不再属于"编辑中吞键"的窗口
            ExitEditing();
            if (onEndEdit != null)
            {
                onEndEdit(value);
            }
        }

        private void OnValueChangedInternal(string value)
        {
            if (onChanged != null)
            {
                onChanged(value);
            }
        }

        /// <summary>进入编辑：静态计数 +1（幂等：重复选中不算两次）。</summary>
        public void EnterEditing()
        {
            if (editing)
            {
                return;
            }
            editing = true;
            editingCount++;
        }

        /// <summary>退出编辑：静态计数 -1（幂等；计数永不为负）。</summary>
        public void ExitEditing()
        {
            if (!editing)
            {
                return;
            }
            editing = false;
            editingCount = Mathf.Max(0, editingCount - 1);
        }

        // 🔴 计数必须随组件消失归零：面板被 Deactivate 时是 Destroy(gameObject)，
        //    若不在这里减掉，吞键开关会永久卡在"编辑中"，整个游戏的键盘就废了。
        private void OnDisable()
        {
            ExitEditing();
        }

        private void OnDestroy()
        {
            ExitEditing();
        }
    }
}
