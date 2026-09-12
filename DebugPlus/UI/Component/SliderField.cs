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
        private const float ReadoutWidth = 56f;
        private const float ReadoutFontSize = 15f;
        private const float SliderHeight = 44f;
        private const float HandleWidth = 28f;
        private const float FillInset = 6f;

        private KSlider slider;
        private TextMeshProUGUI readout;
        private System.Func<float, string> formatter;
        private bool listenerAttached;

        /// <summary>用户拖动/点击产生新值时触发；<b>程序化设值不会触发</b>（避免把初值写回游戏）。</summary>
        public System.Action<float> onChanged;

        /// <summary>建一整栏（自身 GameObject 已挂到 parent 下、已应用布局尺寸），返回组件。</summary>
        public static SliderField Create(Transform parent, float height)
        {
            GameObject root = NewUIObject("sliderField", parent);
            var rootLayout = root.AddComponent<LayoutElement>();
            rootLayout.flexibleWidth = 1f; // 吃掉行内剩余宽度（行 HLG 的 childForceExpandWidth = false 时仍生效）
            rootLayout.preferredHeight = height;
            rootLayout.minHeight = height;

            // 交互主体：不激活状态下把引用挂齐，最后一步再激活（约束 ①）。
            GameObject sliderObject = NewUIObject("slider", root.transform);
            sliderObject.SetActive(false);

            // 背景：可交互层的"命中外壳" —— 必须不透明且 raycastTarget = true，
            // 拖拽命中它之后事件沿层级冒泡到 Slider（透明/无 Graphic 都收不到点击）。
            GameObject background = NewUIObject("background", sliderObject.transform);
            Stretch(background);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.09f, 0.11f, 1f);
            backgroundImage.raycastTarget = true;

            // 填充区容器 + 填充条（Slider 驱动填充条的锚点，容器只提供矩形）
            GameObject fillArea = NewUIObject("fillArea", sliderObject.transform);
            StretchWithInset(fillArea, FillInset, FillInset);
            GameObject fill = NewUIObject("fill", fillArea.transform);
            var fillRect = Stretch(fill);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.36f, 0.7f, 0.45f, 1f);
            fillImage.raycastTarget = false; // 装饰层不拦射线

            // 滑块滑区容器 + 滑块（左右各留半个滑块宽，滑块才不会越界）
            GameObject handleArea = NewUIObject("handleArea", sliderObject.transform);
            StretchWithInset(handleArea, HandleWidth * 0.5f, HandleWidth * 0.5f);
            GameObject handle = NewUIObject("handle", handleArea.transform);
            var handleRect = handle.GetComponent<RectTransform>(); // 约束 ②
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(HandleWidth, 0f); // 高度撑满滑区
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.88f, 0.9f, 0.92f, 1f);
            handleImage.raycastTarget = false;

            // 最后挂 KSlider：此时引用已就位，激活后的 Awake 不会 NRE。
            var field = root.AddComponent<SliderField>();
            field.slider = sliderObject.AddComponent<KSlider>();
            field.slider.targetGraphic = backgroundImage;
            field.slider.transition = Selectable.Transition.None; // 纯代码不用 ColorTint（oni-ui）
            field.slider.direction = Slider.Direction.LeftToRight;
            field.slider.fillRect = fillRect;
            field.slider.handleRect = handleRect;
            // ⚠️ 监听器**不在这里挂**：SetRange 设初值时若已挂上，会把从游戏读出来的现状当用户操作写回去。
            //    由使用方在设完范围/初值后调用 AttachListener()（顺序契约见 ParameterRow.Bind）。

            // 读数：与滑条同一栏、右对齐；TMP 独占一个 GameObject（约束 ③）。
            field.readout = CreateReadout(root.transform);

            sliderObject.SetActive(true);
            return field;
        }

        /// <summary>
        /// 设范围与初值，此时**不通知任何回调** —— 初值是"从游戏读出来的现状"，不该被当成用户操作写回去。
        /// ⚠️ 不能用 `Slider.Set(value, false)`：它是 **protected**（IL 里是 `family`），mod 访问不到（编译报 CS0122）。
        /// 顺序契约：先本方法，再 <see cref="SetFormatter"/>，最后 <see cref="AttachListener"/>。
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

        /// <summary>按当前值刷新读数。</summary>
        public void Refresh()
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

        private static TextMeshProUGUI CreateReadout(Transform parent)
        {
            GameObject go = NewUIObject("readout", parent);
            // ⚠️ 本栏内部没有 LayoutGroup，读数的 LayoutElement 是**无效**的
            //    ⇒ 必须用锚点自己定位：右对齐、占满高度、与滑条右端留 2px 间隙。
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-2f, 0f);
            rect.sizeDelta = new Vector2(ReadoutWidth, 0f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (Localization.FontAsset != null)
            {
                tmp.font = Localization.FontAsset; // 约束 ③
            }
            tmp.text = "";
            tmp.fontSize = ReadoutFontSize;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.color = Color.white;
            tmp.raycastTarget = false; // 装饰层不拦射线
            return tmp;
        }

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>(); // 本文件后续一律 GetComponent 取（约束 ②）
            return go;
        }

        private static RectTransform Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void StretchWithInset(GameObject go, float horizontal, float vertical)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
