using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 「滑条 + 读数」一栏（批 2b · A4 重构后）：**只负责 UI**，不认识任何游戏类型。
    /// 术语：**滑条** = 整条可拖的控件；**滑块** = 上面那个被拖动的小方块。
    ///
    /// 为什么纯代码搭、不克隆预制体 —— 原版所有滑条行都来自预制体：
    /// MultiSliderSideScreen.cs:34 `Util.KInstantiateUI(this.sliderPrefab.gameObject, …)`，
    /// 而 `sliderPrefab` 是屏预制体上的 `[SerializeField]` 引用，**mod 拿不到**；
    /// 原版源码里也没有"代码自建滑条"的先例，所以这条只能按框架公开属性自己搭。
    ///
    /// 素材来源（plan.md §3.1 "参数行素材来源的框架依据"）：
    /// · 滑条用 **KSlider**（原版预制体里用的就是它；`handleRect` / `fillRect` 是 Slider 的**可写公开属性**）。
    /// · **不用 KNumberInputField**：`KInputField.inputField` 是 `[SerializeField] private`、
    ///   `field` 只读（KInputField.cs:10-16 / 105-106）⇒ 纯代码无法合法构造，读数改用 TMP 标签。
    ///
    /// 三条硬约束（踩过坑，勿删）：
    /// ① ⚠️ `KSlider.Awake()` 第一句是 `base.handleRect.gameObject.GetComponent&lt;ToolTip&gt;()`（KSlider.cs:40）
    ///    ⇒ handleRect 为空会 NRE。故**先把滑条 GameObject 置为不激活**，挂完 handleRect / fillRect 再激活。
    /// ② ⚠️ 本文件的 UI GameObject 一律由 `NewUIObject` 建，它已挂好 RectTransform
    ///    ⇒ 后续只能 `GetComponent&lt;RectTransform&gt;()` 取；再 `AddComponent` 一次会**返回 null**（不抛异常），
    ///    下一句给 null 设锚点才崩（2026-09-13 实机 NRE，IL 偏移 0x10A 定位）。
    /// ③ TMP 必须独占 GameObject 且显式赋 `Localization.FontAsset`（中文字形依赖它）。
    ///
    /// 布局规则（oni-ui）：装饰层 `raycastTarget = false`、可交互层用不透明底"撑住"命中；
    /// 本栏内部**不放 LayoutGroup**（子项靠锚点/尺寸定位），由外层 VLG 驱动它的高度。
    /// </summary>
    public class SliderField : MonoBehaviour
    {
        public class Style
        {
            /// <summary>滑条（含槽）整体高度。</summary>
            public float Height = 26f;
            /// <summary>滑块边长（宽度）。</summary>
            public float HandleSize = 18f;
            /// <summary>填充条与滑区相对滑条的内缩（两者同值，填充与滑块两端才对得上）。</summary>
            public float FillInset = 6f;
            /// <summary>读数宽度。</summary>
            public float ReadoutWidth = 56f;
            /// <summary>读数字号。</summary>
            public float ReadoutFontSize = 13f;
            /// <summary>读数与滑条右端的间隙。</summary>
            public float ReadoutGap = 6f;
            /// <summary>滑条槽底色。</summary>
            public Color TrackColor = UIColors.BackgroundDeep;
            /// <summary>填充条颜色。</summary>
            public Color FillColor = UIColors.Success;
            /// <summary>滑块颜色。</summary>
            public Color HandleColor = UIColors.BackgroundB;
            /// <summary>读数文字颜色。</summary>
            public Color ReadoutColor = UIColors.PrimaryText;
        }

        /// <summary>默认样式（想改观感就改这里：只影响"未显式传样式"的实例）。</summary>
        public static readonly Style DefaultStyle = new Style();

        /// <summary>本实例的样式（可创建后直接改字段，或调用 <see cref="ApplyStyle"/> 批量重刷）。</summary>
        public Style CurrentStyle { get; private set; }

        private KSlider slider;
        private TextMeshProUGUI readout;
        private System.Func<float, string> formatter;
        private bool listenerAttached;

        /// <summary>用户拖动/点击产生新值时触发；<b>程序化设值不会触发</b>（避免把初值写回游戏）。</summary>
        public System.Action<float> onChanged;

        /// <summary>建一整栏（自身 GO 已挂到 parent 下、已应用布局尺寸），返回组件。</summary>
        /// <param name="parent">父节点（通常是行 HLG）</param>
        /// <param name="height">本栏在行内的高度（≤0 = 用样式里的高度）</param>
        /// <param name="style">自定义样式（null = 用 <see cref="DefaultStyle"/>）</param>
        public static SliderField Create(Transform parent, float height = 0f, Style style = null)
        {
            Style current = style ?? DefaultStyle;
            float fieldHeight = height > 0f ? height : current.Height;

            GameObject root = NewUIObject("sliderField", parent);
            var rootLayout = root.AddComponent<LayoutElement>();
            // 宽度：显式压掉"首选宽度"（置 0），完全靠 flexibleWidth 吃掉行内剩余宽度。
            // 不写 preferredWidth 时它取默认值 -1，布局分配全靠推算，容易出现"滑条没占满"的观感。
            rootLayout.preferredWidth = 0f;
            rootLayout.minWidth = 0f;
            rootLayout.flexibleWidth = 1f;
            rootLayout.preferredHeight = fieldHeight;
            rootLayout.minHeight = fieldHeight;

            // 交互主体：不激活状态下把引用挂齐，最后一步再激活（约束 ①）。
            GameObject sliderObject = NewUIObject("slider", root.transform);
            sliderObject.SetActive(false);

            // 背景：可交互层的"命中外壳" —— 必须不透明且 raycastTarget = true，
            // 拖拽命中它之后事件沿层级冒泡到 Slider（透明/无 Graphic 都收不到点击）。
            GameObject background = NewUIObject("background", sliderObject.transform);
            Stretch(background);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = current.TrackColor; // 颜色只从样式（默认源自 UIColors）取
            backgroundImage.raycastTarget = true;

            // 填充区容器 + 填充条（Slider 驱动填充条的锚点，容器只提供矩形）
            GameObject fillArea = NewUIObject("fillArea", sliderObject.transform);
            StretchWithInset(fillArea, current.FillInset, current.FillInset);
            GameObject fill = NewUIObject("fill", fillArea.transform);
            var fillRect = Stretch(fill);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = current.FillColor;
            fillImage.raycastTarget = false; // 装饰层不拦射线

            // 滑块滑区容器 + 滑块。
            // 水平：与填充条同内缩（两端才对得上，滑块也不会越出滑条）；
            // 垂直：也要内缩半个滑块高 —— 否则滑块被撑满整条高度、变成一根竖条（不是方块）。
            GameObject handleArea = NewUIObject("handleArea", sliderObject.transform);
            StretchWithInset(handleArea, current.FillInset, current.HandleSize * 0.5f);
            GameObject handle = NewUIObject("handle", handleArea.transform);
            var handleRect = handle.GetComponent<RectTransform>(); // 约束 ②
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(current.HandleSize, 0f); // 高度随之等于滑块边长（正方形）
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = current.HandleColor;
            handleImage.raycastTarget = false;

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

            // 读数：与滑条同一栏、右对齐；TMP 独占一个 GameObject（约束 ③）。
            field.readout = CreateReadout(root.transform, current);

            sliderObject.SetActive(true);

            // 诊断日志：把三个关键几何量打出来（宽度异常时一眼能定位是行窄了、还是滑条没吃掉剩余宽度）
            Debug.Log("[DebugPlus] 滑条已建：栏宽=" + root.GetComponent<RectTransform>().rect.width.ToString("0.#")
                + " 滑条宽=" + sliderObject.GetComponent<RectTransform>().rect.width.ToString("0.#")
                + " 高=" + current.Height.ToString("0.#"));
            return field;
        }

        /// <summary>
        /// 用改过的样式重刷外观（尺寸与颜色）。
        /// 一般不需要调用：创建时传样式、或直接改 <see cref="CurrentStyle"/> 的字段即可。
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
        /// <param name="read">读当前值（本 Mod 的读值门槛、换算都由它负责）</param>
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

        private static TextMeshProUGUI CreateReadout(Transform parent, Style style)
        {
            GameObject go = NewUIObject("readout", parent);
            // ⚠️ 本栏内部没有 LayoutGroup，读数的 LayoutElement 是**无效**的
            //    ⇒ 必须用锚点自己定位：右对齐、占满高度、与滑条右端留一点间隙。
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-style.ReadoutGap, 0f);
            rect.sizeDelta = new Vector2(style.ReadoutWidth, 0f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (Localization.FontAsset != null)
            {
                tmp.font = Localization.FontAsset; // 约束 ③
            }
            tmp.text = "";
            tmp.fontSize = style.ReadoutFontSize;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.color = style.ReadoutColor;
            tmp.raycastTarget = false; // 装饰层不拦射线
            return tmp;
        }

        private static GameObject NewUIObject(string name, Transform parent)
        {
            return UIFactory.NewUIObject(name, parent); // 统一走构件工厂（约束 ② 只在这里落实一次）
        }

        private static RectTransform Stretch(GameObject go)
        {
            return UIFactory.Stretch(go); // 统一走构件工厂
        }

        private static void StretchWithInset(GameObject go, float horizontal, float vertical)
        {
            UIFactory.StretchWithInset(go, horizontal, vertical); // 统一走构件工厂
        }
    }
}
