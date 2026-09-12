using DebugPlus.Operations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 参数行的**纯代码**构建（批 2b · A4）。
    ///
    /// 为什么纯代码而不是克隆预制体 —— 原版所有滑杆行都来自预制体：
    /// MultiSliderSideScreen.cs:34 `Util.KInstantiateUI(this.sliderPrefab.gameObject, ...)`，
    /// 而 `sliderPrefab` 是屏预制体上的 `[SerializeField]` 引用，**mod 拿不到**。
    /// 原版源码里没有任何"代码自建滑杆"的先例，所以这里是本 Mod 自己按框架公开属性搭的。
    ///
    /// 素材来源定案（写进 plan.md）：
    /// · 滑杆用 **KSlider**（原版预制体里用的就是这个类；`Slider.handleRect` / `fillRect`
    ///   是可写的公开属性 ⇒ 可以纯代码喂给它）。
    /// · **不用 KNumberInputField**：它的 `inputField` 是 `[SerializeField] private KInputTextField`
    ///   且 `field` 属性只读（KInputField.cs:10-16 / 105-106），纯代码无法合法构造 ⇒
    ///   数值改用手写的 TMP 读数标签。
    /// · ⚠️ KSlider.Awake() 第一句就是 `base.handleRect.gameObject.GetComponent&lt;ToolTip&gt;()`（KSlider.cs:40）：
    ///   handleRect 为空会 NRE ⇒ 本工厂**先把滑杆 GameObject 置为不激活**，挂完 handleRect / fillRect
    ///   再激活（Awake 在激活时才跑）。
    ///
    /// 布局规则（oni-ui）：新建 GO 先加 RectTransform；TMP 独占 GO 且必须设 Localization.FontAsset；
    /// 装饰层 raycastTarget = false、可交互层用不透明底"撑住"命中；只用「根 VLG → 行 HLG」一层嵌套。
    ///
    /// ⚠️ 血的教训（2026-09-13 实机 NRE · IL 偏移 0x10A 定位）：本文件的 GO 一律由 NewUIObject 建，
    /// 它已经挂好 RectTransform ⇒ 后续**只能 GetComponent&lt;RectTransform&gt;() 取**，
    /// 再 AddComponent 一次会**返回 null**（不是抛异常），下一句给 null 设锚点才崩。
    /// </summary>
    public static class ParameterRowFactory
    {
        public const float RowHeight = 40f;

        private const float LabelWidth = 120f;
        private const float ValueWidth = 56f;
        private const float SliderHeight = 22f;
        private const float HandleWidth = 14f;
        private const float FillInset = 2f;

        /// <summary>建一行参数并绑定，返回行组件（行已挂到 parent 下、排在末尾）。</summary>
        public static ParameterRow Create(Parameter parameter, Transform parent)
        {
            GameObject row = NewUIObject("paramRow", parent);
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = RowHeight;
            rowLayout.minHeight = RowHeight;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;  // ★ VLG 的子项是 HLG 时必须 false
            hlg.childForceExpandHeight = false; // ★ 不拉伸高度（否则 VLG 高度分配异常）

            // ① 标签
            TextMeshProUGUI label = CreateText(row, "label", parameter.Label, 15f, TextAlignmentOptions.MidlineLeft);
            AddFixedSize(label.gameObject, LabelWidth, SliderHeight);

            // ② 滑杆（吃掉剩余宽度）
            KSlider slider = CreateSlider(row);
            var sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1f; // 显式弹性（childForceExpandWidth = false 时仍生效）
            sliderLayout.preferredHeight = SliderHeight;
            sliderLayout.minHeight = SliderHeight;

            // ③ 读数
            TextMeshProUGUI valueText = CreateText(row, "value", "", 15f, TextAlignmentOptions.Center);
            AddFixedSize(valueText.gameObject, ValueWidth, SliderHeight);

            var rowComponent = row.AddComponent<ParameterRow>();
            rowComponent.Bind(parameter, slider, valueText);
            return rowComponent;
        }

        /// <summary>
        /// 建滑杆。内部结构照 Unity 原版滑杆的层级（Slider 会在运行时驱动 fill / handle 的锚点，
        /// 所以两者必须各自待在一个容器里，容器本身的矩形决定可视范围）：
        /// slider(KSlider) → background / fillArea → fill / handleArea → handle
        /// </summary>
        private static KSlider CreateSlider(GameObject row)
        {
            GameObject sliderGO = NewUIObject("slider", row.transform);
            // ★ 先不激活：KSlider 的 Awake 立刻要用 handleRect（见类注释），此时还没挂。
            sliderGO.SetActive(false);

            // 背景：可交互层的"命中外壳" —— 必须不透明且 raycastTarget = true，
            // 拖拽命中它之后事件沿层级冒泡到 Slider（透明/无 Graphic 都收不到点击）。
            GameObject background = NewUIObject("background", sliderGO.transform);
            Stretch(background);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.09f, 0.11f, 1f);
            backgroundImage.raycastTarget = true;

            // 填充区容器 + 填充条（Slider 驱动填充条的锚点，容器只提供矩形）
            GameObject fillArea = NewUIObject("fillArea", sliderGO.transform);
            StretchWithInset(fillArea, FillInset, FillInset);
            GameObject fill = NewUIObject("fill", fillArea.transform);
            var fillRect = Stretch(fill);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.36f, 0.7f, 0.45f, 1f);
            fillImage.raycastTarget = false; // 装饰层不拦射线

            // 手柄滑区容器 + 手柄（左右各留半个手柄宽，手柄才不会越界）
            GameObject handleArea = NewUIObject("handleArea", sliderGO.transform);
            StretchWithInset(handleArea, HandleWidth * 0.5f, HandleWidth * 0.5f);
            GameObject handle = NewUIObject("handle", handleArea.transform);
            // ★ 只能 GetComponent：NewUIObject 已经挂过 RectTransform，再 AddComponent 会返回 null
            //   （同一 GameObject 上不允许两个 RectTransform）—— 2026-09-13 实机 NRE 就死在这里。
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(HandleWidth, 0f); // 高度撑满滑区
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.88f, 0.9f, 0.92f, 1f);
            handleImage.raycastTarget = false;

            // 最后挂 KSlider：此时 handleRect / fillRect 已就位，激活后的 Awake 不会 NRE。
            var slider = sliderGO.AddComponent<KSlider>();
            slider.targetGraphic = backgroundImage;
            slider.transition = Selectable.Transition.None; // 纯代码不用 ColorTint（oni-ui 规则 11/13）
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            sliderGO.SetActive(true);
            return slider;
        }

        private static void AddFixedSize(GameObject go, float width, float height)
        {
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.preferredHeight = height;
            layout.minHeight = height;
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

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject go = NewUIObject(name, parent.transform);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (Localization.FontAsset != null)
            {
                tmp.font = Localization.FontAsset; // 中文字形必须显式指定字体资源
            }
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false; // 装饰层不拦射线
            return tmp;
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
            StretchWithInset(go, horizontal, horizontal, vertical, vertical);
        }

        private static void StretchWithInset(GameObject go, float left, float right, float bottom, float top)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
