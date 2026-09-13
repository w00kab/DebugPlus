using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 「滑条 + 读数」一栏：**只负责 UI**，不认识任何游戏类型。
    /// 术语：**滑条** = 整条可拖的控件；**滑块** = 上面那个被拖动的小方块。
    ///
    /// # 布局控制（2026-09-13 按用户要求重做）
    /// **尺寸一律由布局组与 LayoutElement 决定，不用"固定像素 + 手动锚点"**：
    /// ```
    /// sliderField                       ← 栏（本组件根，供外层行 HLG 使用）
    ///   LayoutElement: preferredHeight = 栏高, flexibleWidth = 1   ← 宽度吃满行内剩余
    ///   HorizontalLayoutGroup: childControlW/H = true, spacing = 间隙
    ///   ├── bar                          LayoutElement: flexibleWidth = 1   ← 滑条占满左侧
    ///   │     Slider 本体 = 整栏高（滑块就占这块高度，**比槽底高**，见下）
    ///   │     ├── background             槽底（按槽高上下内缩）+ 可交互外壳
    ///   │     ├── fillArea → fill        Slider 驱动填充条（按槽高内缩，与槽底同高）
    ///   │     └── handleArea → handle    Slider 驱动滑块（水平与填充同内缩）
    ///   └── readout                      LayoutElement: preferredWidth = 读数宽  ← 固定一列
    /// ```
    /// 关键点：**读数是被布局组分配了一列宽度**，不是"用锚点压在滑条右端"——
    /// 后者会和滑条本体抢同一段绝对坐标，观感上就是"滑条没占满剩余宽度"。
    ///
    /// # 滑块比槽底高
    /// 槽底按 <see cref="Style.TrackHeight"/> 上下内缩，滑块占满**整栏高**（= <see cref="Style.HandleSize"/>）。
    /// 即 `HandleSize > TrackHeight`，滑块上下各探出 `(HandleSize − TrackHeight) / 2`。
    /// 等高时滑块会显得陷进槽里，略微探出才有"抓手浮在槽上"的层次。
    /// 注意这里**不是**拉长滑块——滑块的宽仍是 <see cref="Style.HandleSize"/>，探出只发生在垂直方向。
    ///
    /// # 三层颜色
    /// 绘制顺序就是层级顺序：**槽底(最下) → 填充条(中) → 滑块(最上)**，三者自然叠出来。
    /// 槽底 <see cref="UIColors.BorderBase"/>（与全 Mod 控件轮廓同色）、
    /// 填充条 <see cref="UIColors.Success"/>、滑块 <see cref="UIColors.BackgroundWhite"/>。
    ///
    /// # 为什么纯代码搭、不克隆预制体
    /// 原版所有滑条行都来自预制体：`MultiSliderSideScreen.cs:34`
    /// `Util.KInstantiateUI(this.sliderPrefab.gameObject, …)`，而 `sliderPrefab` 是屏预制体上的
    /// `[SerializeField]` 引用，**mod 拿不到**；原版源码里也没有"代码自建滑条"的先例。
    /// 素材：滑条用 **KSlider**（`handleRect` / `fillRect` 是基类 `Slider` 的**可写公开属性**）。
    ///
    /// # 三条硬约束（踩过坑，勿删）
    /// ① ⚠️ `KSlider.Awake()` 第一句取 `handleRect.gameObject`（KSlider.cs:40）⇒ handleRect 为空会 NRE。
    ///    故**先把滑条 GameObject 置为不激活**，挂完 handleRect / fillRect 再激活。
    /// ② ⚠️ 本文件的 UI GameObject 一律由 <see cref="UIFactory.NewUIObject"/> 建，它已挂好 RectTransform
    ///    ⇒ 后续只能 `GetComponent<RectTransform>()` 取；再 `AddComponent` 一次会**返回 null**（不抛异常），
    ///    下一句给 null 设锚点才崩（2026-09-13 实机 NRE，IL 偏移 0x10A 定位）。
    /// ③ TMP 必须独占 GameObject 且显式赋 `Localization.FontAsset`（中文字形依赖它）。
    ///
    /// 装饰层一律 `raycastTarget = false`；可交互层用不透明底"撑住"命中。
    /// </summary>
    public class SliderField : MonoBehaviour
    {
        /// <summary>
        /// 外观样式：**只放"控件内部的几何与颜色"**。
        /// 栏宽由外层行布局分配，读数宽由本栏的布局组分配 —— 都不属于样式。
        /// </summary>
        public class Style
        {
            /// <summary>栏高（= 滑块高度；本栏的 preferredHeight，也是滑条的命中区高度）。</summary>
            public float Height = 28f;
            /// <summary>槽底高度（细条本身）；必须 **小于 <see cref="Height"/>**，否则滑块塌陷进槽里。</summary>
            public float TrackHeight = 24f;
            /// <summary>
            /// 滑块边长（宽度）。
            /// ⚠️ 它同时是**滑块的垂直尺寸**（滑块占满栏高），故必须 **大于 <see cref="TrackHeight"/>**，
            /// 上下各探出半格；等高于槽底会显得陷进槽里。
            /// 它**不是**滑区的内缩量：内缩只由 <see cref="FillInset"/> 管。
            /// </summary>
            public float HandleSize = 28f;
            /// <summary>
            /// 滑条两端的水平内缩：**填充条与滑区共用**。
            /// 固定 **0**，与槽底（背景）的水平内缩完全一致。
            /// 只要大于 0，填充条就比槽底窄，**没被填充盖住的槽底会直接显示出来**（绿色旁露出槽底色）。
            /// </summary>
            public float FillInset = 0f;
            /// <summary>滑条与读数两列之间的间隙（布局组 spacing）。</summary>
            public float ColumnSpacing = 8f;
            /// <summary>读数一列的宽度（由布局组分配）。</summary>
            public float ReadoutWidth = 56f;
            /// <summary>读数字号。</summary>
            public float ReadoutFontSize = 13f;
            /// <summary>
            /// 槽底色：控件基础边框色（与全 Mod 的控件轮廓、分隔线同色）。
            /// 🔴 **用户明令指定为 <see cref="UIColors.BorderBase"/>，不要再自作主张改。**
            /// 颜色归属由用户定，**不许按"对比度够不够/看不看得清"之类的推理去改这个值**。
            /// </summary>
            public Color TrackColor = UIColors.BorderBase;
            /// <summary>填充条颜色。</summary>
            public Color FillColor = UIColors.Success;
            /// <summary>滑块颜色（纯白抓手，在槽底与绿色填充上都清晰可辨）。</summary>
            public Color HandleColor = UIColors.BackgroundWhite;
            /// <summary>读数文字颜色。</summary>
            public Color ReadoutColor = UIColors.PrimaryText;
        }

        /// <summary>默认样式（想改观感就改这里：只影响"未显式传样式"的实例）。</summary>
        public static readonly Style DefaultStyle = new Style();

        /// <summary>本实例的样式。</summary>
        public Style CurrentStyle { get; private set; }

        private KSlider slider;
        private TextMeshProUGUI readout;
        private System.Func<float, string> formatter;
        private bool listenerAttached;

        /// <summary>用户拖动/点击产生新值时触发；<b>程序化设值不会触发</b>（避免把初值写回游戏）。</summary>
        public System.Action<float> onChanged;

        /// <summary>
        /// 建一整栏并挂到 <paramref name="parent"/> 下（行内使用）。
        /// 栏高 = <see cref="Style.Height"/>（= 滑块高，同时是滑条的命中区高）：外层行的 HLG 按这个首选高度排布，
        /// 槽底会在栏内上下内缩成 <see cref="Style.TrackHeight"/> 的细条。
        /// </summary>
        /// <param name="parent">父节点（通常是行 HLG）</param>
        /// <param name="style">自定义样式（null = 用 <see cref="DefaultStyle"/>）</param>
        public static SliderField Create(Transform parent, Style style = null)
        {
            Style current = style ?? DefaultStyle;

            // ── 栏（布局单元）：高度给布局组，宽度全交给父级行布局的弹性分配 ──
            GameObject root = UIFactory.NewUIObject("sliderField", parent);
            var rootLayout = UIFactory.GetOrAddLayout(root);
            rootLayout.preferredHeight = current.Height;
            rootLayout.minHeight = current.Height;
            rootLayout.flexibleWidth = 1f; // 吃掉行内除固定列以外的全部剩余宽度
            UIFactory.AddHLG(root, spacing: current.ColumnSpacing,
                alignment: TextAnchor.MiddleLeft);

            // ── 滑条（左列：吃掉剩余宽度） ──
            // 🔴 高度必须显式给：本栏的 HLG 用默认 `childForceExpandHeight = false`（UIFactory 铁律），
            //    ⇒ 子项高度 = **子项自己的 preferredHeight**。这里若只给 flexibleWidth 而不给高度，
            //    bar 的 preferredHeight 就是 0 ⇒ bar 高 0 ⇒ 里面的 slider 铺满 0 ⇒ **槽底高度 0、根本不画**。
            //    实机症状极具误导性：右边 readout 因为有 AddFixedSize 一切正常，看着像"滑条颜色不对"，
            //    实际是那一列**根本没有像素**（2026-09-13 用户点破"不是颜色，是高度塌陷"）。
            //    口径与右列 AddFixedSize 保持一致。
            GameObject barHolder = UIFactory.NewUIObject("bar", root.transform);
            UIFactory.AddFlexibleWidth(barHolder);
            UIFactory.AddPreferredHeight(barHolder, current.Height);

            // 交互主体：不激活状态下把引用挂齐，最后一步再激活（约束 ①）。
            // 它占满整个栏高：**滑块的高度就是这块高度**，槽底再在里面上下内缩成细条。
            GameObject sliderObject = UIFactory.NewUIObject("slider", barHolder.transform);
            sliderObject.SetActive(false);
            UIFactory.Stretch(sliderObject);

            // 槽底的上下内缩 = (栏高 − 槽高) / 2 ⇒ 槽是居中的细条，滑块比它高、上下探出这半格。
            float trackInset = (current.Height - current.TrackHeight) * 0.5f;

            // 槽底：可交互层的"命中外壳" —— 必须不透明且 raycastTarget = true，
            // 拖拽命中它之后事件沿层级冒泡到 Slider（透明/无 Graphic 都收不到点击）。
            GameObject background = UIFactory.NewUIObject("background", sliderObject.transform);
            UIFactory.StretchWithInset(background, 0f, trackInset);
            var backgroundImage = UIFactory.AddBackground(background, current.TrackColor, true);

            // 填充区容器 + 填充条（Slider 驱动填充条的锚点，容器只提供矩形）
            // 水平内缩 = FillInset = 0，与槽底一致：填充条比槽底窄时，没被绿色盖住的槽底会显示出来。
            // 垂直同槽高内缩：填充条与槽底上下对齐。
            GameObject fillArea = UIFactory.NewUIObject("fillArea", sliderObject.transform);
            UIFactory.StretchWithInset(fillArea, current.FillInset, trackInset);
            GameObject fill = UIFactory.NewUIObject("fill", fillArea.transform);
            var fillRect = UIFactory.Stretch(fill);
            UIFactory.AddDecoration(fill, current.FillColor);

            // 滑块滑区容器 + 滑块（水平与填充同内缩；垂直**不内缩**，滑块占满栏高从而高于槽底）
            GameObject handleArea = UIFactory.NewUIObject("handleArea", sliderObject.transform);
            UIFactory.StretchWithInset(handleArea, current.FillInset, 0f);
            GameObject handle = UIFactory.NewUIObject("handle", handleArea.transform);
            var handleRect = handle.GetComponent<RectTransform>(); // 约束 ②
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(current.HandleSize, 0f); // 高 = 滑区高（= 栏高）
            UIFactory.AddDecoration(handle, current.HandleColor);

            // 最后挂 KSlider：此时引用已就位，激活后的 Awake 不会 NRE。
            var field = root.AddComponent<SliderField>();
            field.CurrentStyle = current;
            field.slider = sliderObject.AddComponent<KSlider>();
            field.slider.targetGraphic = backgroundImage;
            field.slider.transition = Selectable.Transition.None; // 纯代码不用 ColorTint（oni-ui）
            field.slider.direction = Slider.Direction.LeftToRight;
            field.slider.fillRect = fillRect;
            field.slider.handleRect = handleRect;
            // ⚠️ 监听器**不在这里挂**：设初值时若已挂上，会把从游戏读出来的现状当用户操作写回去。
            //    普通用法走 Bind()（内部保证顺序）；高级用法自己按 SetRange → SetFormatter → AttachListener 排。

            // ── 读数（右列：布局组分给它固定一列宽度） ──
            field.readout = CreateReadout(root.transform, current);

            sliderObject.SetActive(true);
            return field;
        }

        /// <summary>
        /// 用改过的样式重刷外观。
        /// ⚠️ 尺寸类改动（栏高/槽高/滑块/读数宽）在**创建时**已交给布局组，改样式后请重建本栏；
        /// 本方法只重刷读数文本。
        /// </summary>
        public void ApplyStyle()
        {
            Refresh();
        }

        /// <summary>
        /// 🔴 **绑定接口（推荐用法）**：把"读哪个值、写回哪里、怎么显示"一次性交给本组件，
        /// 由组件内部保证调用顺序 —— 调用方**再也不可能**把顺序写错。
        ///
        /// 顺序为什么重要：先 <see cref="SetRange"/>（含初值）→ 再 <see cref="SetFormatter"/> →
        /// **最后**才挂监听。若顺序颠倒，"从游戏读出来的现状"会被当成一次用户操作写回游戏。
        /// </summary>
        /// <param name="read">读当前值</param>
        /// <param name="write">写回值（拖动/点击时调用；<b>写的是 UI 刻度上的值</b>）</param>
        /// <param name="format">读数文本格式化（null = 用默认"保留一位小数"）</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="wholeNumbers">是否只取整数刻度</param>
        /// <param name="initial">初值；传 float.NaN 表示"从 <paramref name="read"/> 读"</param>
        public SliderField Bind(System.Func<float> read, System.Action<float> write,
            System.Func<float, string> format = null,
            float min = 0f, float max = 1f, bool wholeNumbers = false, float initial = float.NaN)
        {
            float start = float.IsNaN(initial)
                ? (read != null ? read() : min)
                : initial;

            SetRange(min, max, wholeNumbers, Mathf.Clamp(start, min, max));
            SetFormatter(format);
            if (write != null)
            {
                onChanged = write;
                AttachListener();
            }
            return this;
        }

        /// <summary>
        /// 设范围与初值，此时**不通知任何回调** —— 初值是"从游戏读出来的现状"，不该被当成用户操作写回去。
        /// ⚠️ 不能用 `Slider.Set(value, false)`：它是 **protected**（IL 里是 `family`），mod 访问不到（编译报 CS0122）。
        /// 顺手用法请优先用 <see cref="Bind"/>；顺序契约：先本方法，再 <see cref="SetFormatter"/>，最后 <see cref="AttachListener"/>。
        /// </summary>
        public void SetRange(float min, float max, bool wholeNumbers, float initialValue)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = initialValue; // 公开属性；此刻还没挂监听器，不会外泄回调
            Refresh();
        }

        /// <summary>程序化设值（不触发 <see cref="onChanged"/>），用于"游戏侧变了要同步到界面"。</summary>
        public void SetValue(float value)
        {
            slider.value = value;
            Refresh();
        }

        /// <summary>读数格式化回调（由使用方提供显示规则）；设置后立即刷一次读数。</summary>
        public void SetFormatter(System.Func<float, string> formatter)
        {
            this.formatter = formatter;
            Refresh();
        }

        /// <summary>挂上内部监听器（此后用户拖动/点击才会触发 <see cref="onChanged"/>）。幂等。</summary>
        public void AttachListener()
        {
            if (listenerAttached)
            {
                return;
            }
            listenerAttached = true;
            slider.onValueChanged.AddListener(OnSliderChanged);
        }

        /// <summary>按当前值刷新读数。<b>可覆写</b>：派生类可加额外视觉反馈（但不要在这里写游戏逻辑）。</summary>
        public virtual void Refresh()
        {
            if (readout == null)
            {
                return;
            }
            readout.text = formatter != null
                ? formatter(slider.value)
                : slider.value.ToString("0.#");
        }

        private void OnSliderChanged(float value)
        {
            Refresh();
            if (onChanged != null)
            {
                onChanged(value);
            }
        }

        /// <summary>读数一列：宽度由布局组分配（LayoutElement.preferredWidth），不设锚点尺寸。</summary>
        private static TextMeshProUGUI CreateReadout(Transform parent, Style style)
        {
            TextMeshProUGUI tmp = UIFactory.CreateText(parent, "readout", string.Empty,
                style.ReadoutFontSize, style.ReadoutColor, TextAlignmentOptions.MidlineRight, false);
            UIFactory.AddFixedSize(tmp.gameObject, style.ReadoutWidth, style.Height);
            return tmp;
        }
    }
}
