using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 本 Mod 所有 UI 的**统一构件工厂**：把「建 GameObject / 设锚点 / 配布局组 / 配 LayoutElement /
    /// 建 TMP 文本」这些每次都要重复的样板收敛到一处。
    ///
    /// 🔴 铁律（2026-09-13 实机 NRE 换来，IL 偏移 0x10A 定位）：
    /// 本类**所有**建 GO 的入口都由 <see cref="NewUIObject"/> 完成，它已经挂好 RectTransform。
    /// ⇒ 调用方之后**只能 `GetComponent&lt;RectTransform&gt;()` 取**；
    ///   对已有 RectTransform 的 GO 再 `AddComponent&lt;RectTransform&gt;()`，
    ///   **Unity 返回 null 且不抛异常**，下一句给 null 设锚点才会崩。
    /// 本类内部也一律用 GetComponent，不留反例。
    ///
    /// 布局规则（oni-ui，抄法来自 layout group 的实际踩坑史，非搬运代码）：
    /// · `childControlWidth/Height` 默认 true —— 父布局接管子项尺寸（子项按各自 LayoutElement 排布）；
    /// · `childForceExpandHeight` **一律 false** —— 置 true 会把 1px 分隔线/文字拉成整行色条，也会让
    ///   作为 VLG 子项的 HLG 高度分配异常；
    /// · `childForceExpandWidth` 默认 false —— 需要弹性的子项自己设 `LayoutElement.flexibleWidth = 1`，
    ///   因为 forceExpandWidth=true 会把 `flexibleWidth=0` 的固定标签/按钮也强行拉宽；
    /// · 嵌套只允许「VLG → 行 HLG」一层，禁止 VLG → VLG。
    /// </summary>
    public static class UIFactory
    {
        // ══════════════════ 建对象 ══════════════════

        /// <summary>
        /// 新建 UI 用 GameObject：设父级 + 挂 RectTransform。
        /// ⚠️ 用本方法建出来的 GO **已有 RectTransform**，之后只能用 <see cref="Rect"/> 取，不能再 AddComponent。
        /// </summary>
        public static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        /// <summary>取 RectTransform（本类建的 GO 都有；万一没有则补挂，双保险）。</summary>
        public static RectTransform Rect(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }
            return rect;
        }

        // ══════════════════ 定位 ══════════════════

        /// <summary>铺满父级（四边贴合）。</summary>
        public static RectTransform Stretch(GameObject go)
        {
            var rect = Rect(go);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>铺满父级并按像素内缩（四边各缩 `horizontal`/`vertical`）。</summary>
        public static RectTransform StretchWithInset(GameObject go, float horizontal, float vertical)
        {
            var rect = Rect(go);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
            return rect;
        }

        /// <summary>固定尺寸 + 居中锚点（用于绝对定位的浮层/小方块）。</summary>
        public static RectTransform AnchorCenter(GameObject go, float width, float height)
        {
            var rect = Rect(go);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>靠父级右侧锚定（下拉行内小按钮那种"不参与布局"的定位）。</summary>
        public static RectTransform AnchorRightMiddle(GameObject go, float width, float height,
            float rightOffset)
        {
            var rect = Rect(go);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(-rightOffset, 0f);
            return rect;
        }

        // ══════════════════ 布局组 ══════════════════

        /// <summary>
        /// 挂 VerticalLayoutGroup 并配置。默认「受控、不拉伸」的安全基态：
        /// control* = true、forceExpand* = false。需要撑满宽时传 `forceExpandWidth: true`。
        /// </summary>
        public static VerticalLayoutGroup AddVLG(GameObject host,
            bool controlWidth = true,
            bool controlHeight = true,
            bool forceExpandWidth = false,
            bool forceExpandHeight = false,
            TextAnchor alignment = TextAnchor.UpperLeft,
            float spacing = 0f,
            RectOffset padding = null)
        {
            if (host == null)
            {
                return null;
            }
            var group = host.AddComponent<VerticalLayoutGroup>();
            Configure(group, controlWidth, controlHeight, forceExpandWidth, forceExpandHeight,
                alignment, spacing, padding);
            return group;
        }

        /// <summary>挂 HorizontalLayoutGroup 并配置（默认口径同 <see cref="AddVLG"/>）。</summary>
        public static HorizontalLayoutGroup AddHLG(GameObject host,
            bool controlWidth = true,
            bool controlHeight = true,
            bool forceExpandWidth = false,
            bool forceExpandHeight = false,
            TextAnchor alignment = TextAnchor.UpperLeft,
            float spacing = 0f,
            RectOffset padding = null)
        {
            if (host == null)
            {
                return null;
            }
            var group = host.AddComponent<HorizontalLayoutGroup>();
            Configure(group, controlWidth, controlHeight, forceExpandWidth, forceExpandHeight,
                alignment, spacing, padding);
            return group;
        }

        /// <summary>
        /// 就地配置已存在的布局组 —— **所有字段全部显式赋值**（含默认值），
        /// 杜绝"只配了部分字段"导致的隐式行为差异。
        /// </summary>
        public static T Configure<T>(T group,
            bool controlWidth = true,
            bool controlHeight = true,
            bool forceExpandWidth = false,
            bool forceExpandHeight = false,
            TextAnchor alignment = TextAnchor.UpperLeft,
            float spacing = 0f,
            RectOffset padding = null) where T : HorizontalOrVerticalLayoutGroup
        {
            if (group == null)
            {
                return group;
            }
            group.childControlWidth = controlWidth;
            group.childControlHeight = controlHeight;
            group.childForceExpandWidth = forceExpandWidth;
            group.childForceExpandHeight = forceExpandHeight;
            group.childAlignment = alignment;
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset(0, 0, 0, 0);
            return group;
        }

        // ══════════════════ LayoutElement ══════════════════

        /// <summary>只设高度（preferred + min 同值），宽交由布局组决定。</summary>
        public static LayoutElement AddPreferredHeight(GameObject go, float height)
        {
            var layout = GetOrAddLayout(go);
            layout.preferredHeight = height;
            layout.minHeight = height;
            return layout;
        }

        /// <summary>固定宽高（preferred + min 同值）—— 固定尺寸的控件/按钮用这个。</summary>
        public static LayoutElement AddFixedSize(GameObject go, float width, float height)
        {
            var layout = GetOrAddLayout(go);
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.preferredHeight = height;
            layout.minHeight = height;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
            return layout;
        }

        /// <summary>吃掉行内剩余宽度（行 HLG 的 `childForceExpandWidth = false` 时仍然生效）。</summary>
        public static LayoutElement AddFlexibleWidth(GameObject go, float weight = 1f)
        {
            var layout = GetOrAddLayout(go);
            layout.flexibleWidth = weight;
            return layout;
        }

        /// <summary>
        /// 取 LayoutElement，没有才挂。
        /// ⚠️ 不直接 AddComponent 是因为它可能已被前一次 AddPreferredHeight 之类挂上，
        /// 重复挂会叠加出两个组件、尺寸互相打架。
        /// </summary>
        public static LayoutElement GetOrAddLayout(GameObject go)
        {
            var layout = go.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = go.AddComponent<LayoutElement>();
            }
            return layout;
        }

        // ══════════════════ 文本（TMP） ══════════════════

        /// <summary>
        /// 建一段 TMP 文本。
        /// ⚠️ TMP **必须独占一个 GameObject**（与 Image 同 GO 会互相冲突）；
        /// 且**必须显式赋 `Localization.FontAsset`**，否则中文字形不显示。
        /// 装饰层默认 `raycastTarget = false`（不拦射线）。
        /// </summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string text,
            float fontSize, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft,
            bool wrapping = true)
        {
            GameObject go = NewUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (Localization.FontAsset != null)
            {
                tmp.font = Localization.FontAsset;
            }
            tmp.text = text ?? string.Empty;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            if (!wrapping)
            {
                // 大数值/长标签不折行（折行会把行高撑变形）
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
            }
            return tmp;
        }

        // ══════════════════ 按钮 ══════════════════

        /// <summary>
        /// 建一个纯代码按钮并挂好文字子节点。
        /// ⚠️ 用 `Button` 而**不用 `KButton`**：后者的 `soundPlayer` 是 `[SerializeField]`，
        /// 纯代码构造会在 Awake 里 NRE。
        /// ⚠️ `transition = None`：只用 onClick，避免 ColorTint 相乘导致颜色越点越深的残留观感。
        /// 底色默认走 <see cref="UIColors.Background"/>（不透明，否则收不到点击）。
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label,
            System.Action onClick, float fontSize = 15f,
            Color? background = null, Color? labelColor = null)
        {
            GameObject go = NewUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.color = background ?? UIColors.Background;
            image.raycastTarget = true; // 可交互元素必须不透明 + 接射线

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null)
            {
                button.onClick.AddListener(delegate { onClick(); });
            }

            var text = CreateText(go.transform, "label", label, fontSize,
                labelColor ?? UIColors.RegularText, TextAlignmentOptions.Center, false);
            Stretch(text.gameObject);
            return button;
        }

        // ══════════════════ 背景层 ══════════════════

        /// <summary>
        /// 给 GO 挂一层 Image 底色（可交互容器用：**必须不透明且 raycastTarget = true**，
        /// 否则子元素之间点击无法冒泡命中）。
        /// </summary>
        public static Image AddBackground(GameObject go, Color color, bool raycastTarget = true,
            Sprite sprite = null)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced; // 圆角 9-slice
            }
            return image;
        }

        /// <summary>装饰层：不接射线的纯色块（填充条、分隔线、色带）。</summary>
        public static Image AddDecoration(GameObject go, Color color, Sprite sprite = null)
        {
            return AddBackground(go, color, false, sprite);
        }
    }
}
