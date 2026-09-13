using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 「下拉选择」控件：**只负责 UI**，不认识任何游戏类型。
    /// 术语：**header** = 行内那条可点的当前值栏；**列表** = 展开后浮在面板之上的选项列。
    ///
    /// # 结构（一行 = 一个 HLG；列表挂在**浮层挂载点**下，不参与行布局）
    /// ```
    /// dropdownField                       ← 本组件根（在行内，宽 flexibleWidth = 1）
    ///   Image 不透明底 + Button           ← 可交互层（点它展开）
    ///   HorizontalLayoutGroup
    ///   ├── caption                       当前选项文本（弹性宽）
    ///   └── arrow                         "v"（固定宽）
    ///
    /// overlayLayer（面板提供的浮层挂载点，见 ConfigPanel.OverlayLayer）
    ///   └── dropdownOverlay               铺满整屏、ignoreLayout、初始隐藏
    ///         ├── catcher                 铺满整屏的**点击承接层**（点它就收列表）
    ///         └── list                    选项列表（Image 底 + ScrollRect）
    ///               └── viewport          RectMask2D（裁剪）
    ///                     └── content     VLG + ContentSizeFitter
    ///                           ├── item0  Button + Image + TMP
    ///                           └── ...
    /// ```
    ///
    /// # 为什么必须自建（2026-09-13 调研结论，别再重走）
    /// ① 缺氧本体**没有任何"点一下弹浮层"的下拉**：`KDropdownMenu` 不存在；本体 `DropDown` 的
    ///    `Build()` 要求行 prefab 带 `KButton` + `DropDownEntry`（其 `label` 是 `LocText`）+
    ///    `LayoutElement`（DropDown.cs:102/123/204-207）⇒ 等于"必须克隆预制体"，违反本 Mod 约束；
    ///    `SandboxToolParameterMenu.SelectorValue` 是"行内就地折叠的 ScrollPanel"，随父面板一起被裁剪；
    ///    `ComplexFabricatorSideScreen` / `MaterialSelector` 是图标网格。
    /// ② `TMP_Dropdown` 理论上可拼，但 `Show()` 对 template 层级有**5 处无判空硬依赖**
    ///    （TMP_Dropdown.cs:500 取 template 上的 Canvas、506-507 取 DropdownItem），搭错只在点开时爆，
    ///    且所有 setter 都会调 `RefreshShownValue()`、`Hide()` 走协程延迟销毁 ⇒ 不划算。
    /// ③ 我们要的每个零件都已经是本 Mod 自己验证过的：`Image` + `Button`（transition = None）、TMP 独占 GO、
    ///    布局组的尺寸纪律。
    ///
    /// # 浮层定位（oni-ui 五步法的第 ①②③④⑤ 步）
    /// ① `ignoreLayout = true`（浮层父节点本就没有布局组，这行是防将来有人加布局组）；
    /// ② 展开时把浮层与列表都抬到最上层（`SetAsLastSibling`）；
    /// ③ 世界坐标定位：`header.TransformPoint(左下角)` → `浮层.InverseTransformPoint`；
    ///    下方放不下就翻到 header 上方（本体 `DropDown.cs:206-209` 同款思路）；
    /// ④ 挂在**面板的浮层挂载点**下（不是行里，否则会被行矩形裁掉）；
    /// ⑤ 用 `SetActive` 控制显隐，不参与布局计算。
    /// ⚠️ 全程不建 `Canvas` / `GraphicRaycaster`：IL2CPP 运行时 `AddComponent&lt;Canvas&gt;()` 返回 null（oni-ui 规则 9）。
    ///
    /// # 与成熟用例的对照（用户 2026-09-13 指定参考同作者的「Applied Logistics Network」）
    /// 参考 `ALN_Dropdown.cs` / `ALN_DropdownSelect.cs`（那边线上跑了很久），**采纳**：
    /// ① 结构同构：Header（可点栏 + 标签 + 箭头）与列表**分成两个挂载点**，列表挂"面板顶层"而不是行里；
    /// ② 箭头由控件自己按展开态翻转（收起 ▼ / 展开 ▲，ALN 的 `UpdateArrow`）；
    /// ③ 点外关闭靠一个铺满的点击遮罩，且**顺序必须是"先遮罩、后列表"**（ALN `Open` 里的两次
    ///    `SetAsLastSibling`）—— 反了就会把列表自己的点击也拦掉；
    /// ④ 每次打开把列表滚回顶部（ALN `scrollRect.verticalNormalizedPosition = 1f`）；
    /// ⑤ 生命周期兜底：父级被隐藏/销毁时把展开态收掉（ALN 的 `LifecycleWatcher`；本组件用 `OnDisable`）。
    /// **偏离**（都是本项目的纪律，不是疏忽）：
    /// ① Esc 的处理位置不同 —— ALN 在 `KInputHandler.HandleKeyDown` 上做全局 Prefix + 静态登记表
    ///    （因为它要把 Esc 从整个键链上吞掉，还配了 KeyUp 的同步吞）。本 Mod 的面板本身就是
    ///    **唯一**的模态屏，Esc 只会走到 `ConfigPanel.OnKeyDown` 这一条路，所以在那里拦一下即可：
    ///    不必新增补丁、不必引入静态登记表（少一处会泄漏的状态）；`KModalScreen.OnKeyUp`
    ///    只做 `Consumed = true`，不存在"Up 阶段再关一次"的问题，故也不需要吞 KeyUp。
    /// ② 列表项不做 hover / 选中底色 —— 本 Mod 的色板里没有"比中背景再亮一档的暗色"，
    ///    加色是用户的决定权（色板纪律：加色需批准），故只把**当前选中项的文字**提亮。
    /// ③ 列表不用 `ALN_ScrollableList` 那套骨架：本 Mod 只需要"选项列 + 必要时滚动"，
    ///    用 `RectMask2D + VLG + ContentSizeFitter + ScrollRect` 就够（少一层抽象）。
    ///
    /// # 点外关闭与两级 Esc
    /// 展开时 `catcher` 铺满整屏并**不透明可点** ⇒ 面板别处的点击先被它接住（收列表，不落到下层控件上）。
    /// Esc 的收尾由面板负责：`ConfigPanel.OnKeyDown` 发现"有展开的下拉"就先收起列表并消费按键，
    /// 这样第二次 Esc 才关面板（与输入框编辑期的两级 Esc 口径一致）。
    ///
    /// # 顺序契约（沿用 <see cref="SliderField.Bind"/> 的教训）
    /// 初值来自 `read`，**程序化设值不写回**；只有用户选中某一项才调写回通道。
    /// </summary>
    public class DropdownField : MonoBehaviour
    {
        /// <summary>外观样式（只放"控件自己的几何与颜色"；宽度由外层行布局分配，不属于样式）。</summary>
        public class Style
        {
            /// <summary>header 高（= 本栏 preferredHeight，与数值框/滑条同高）。</summary>
            public float Height = 28f;
            /// <summary>header 内 caption 与 arrow 的间距。</summary>
            public float Spacing = 4f;
            /// <summary>当前选项文字字号。</summary>
            public float CaptionFontSize = 15f;
            /// <summary>箭头列的固定宽。</summary>
            public float ArrowWidth = 18f;
            /// <summary>箭头字号。</summary>
            public float ArrowFontSize = 12f;
            /// <summary>
            /// 收起态箭头（点击可展开）。
            /// ⚠️ 符号选型参考了同作者另一个 Mod「Applied Logistics Network」的成熟用例
            /// `ALN_DropdownSelect.UpdateArrow`（它用 `▼ / ▲` 且实机正常），故此处也用 `▼ / ▲`；
            /// 若实机上出现空白方块（ONI 的 SDF 字体缺字形，见 oni-ui 规则 6），
            /// 把这两个值换成 ASCII 的 `"v" / "^"` 即可，不必改动代码其它地方。
            /// </summary>
            public string ArrowClosedLabel = "▼";
            /// <summary>展开态箭头（点击可收起）。</summary>
            public string ArrowOpenLabel = "▲";
            /// <summary>列表项高。</summary>
            public float ItemHeight = 26f;
            /// <summary>列表项文字字号。</summary>
            public float ItemFontSize = 14f;
            /// <summary>列表项文字的左内缩。</summary>
            public float ItemTextInset = 6f;
            /// <summary>列表内边距（四周）。</summary>
            public float ListPadding = 4f;
            /// <summary>header 与列表之间的缝隙。</summary>
            public float ListGap = 2f;
            /// <summary>列表最多同时显示几项（超出部分靠滚动看；见本类说明的"未验证项"）。</summary>
            public int MaxVisibleItems = 8;
            /// <summary>列表宽的下限（header 很窄时也不至于挤成一条）。</summary>
            public float MinListWidth = 120f;
            /// <summary>与浮层四边的最小间距（超出就把列表挪回来）。</summary>
            public float ScreenMargin = 4f;
            /// <summary>header 底色（不透明 —— 透明底收不到点击）。</summary>
            public Color HeaderColor = UIColors.Background;
            /// <summary>当前选项文字色。</summary>
            public Color CaptionColor = UIColors.RegularText;
            /// <summary>箭头色。</summary>
            public Color ArrowColor = UIColors.SecondaryText;
            /// <summary>列表底色（比面板底再浅一档，形成"浮起来"的层次）。</summary>
            public Color ListColor = UIColors.BackgroundD;
            /// <summary>列表项底色。</summary>
            public Color ItemColor = UIColors.Background;
            /// <summary>列表项文字色。</summary>
            public Color ItemTextColor = UIColors.RegularText;
            /// <summary>**当前选中项**的文字色（只改文字，不改底色：勾选态底色的归属留给色板纪律）。</summary>
            public Color ItemSelectedTextColor = UIColors.PrimaryText;
            /// <summary>点击承接层的基色（实际颜色由 <see cref="UIColors.ClickCatcher"/> 现算）。</summary>
            public Color CatcherColor = UIColors.BackgroundDeep;
        }

        /// <summary>默认样式（未显式传样式时使用）。</summary>
        public static readonly Style DefaultStyle = new Style();

        /// <summary>本实例的样式。</summary>
        public Style CurrentStyle { get; private set; }

        private Image headerImage;
        private Button headerButton;
        private TextMeshProUGUI captionText;
        private TextMeshProUGUI arrowText;

        private Transform overlayLayer;
        private GameObject overlayRoot;
        private GameObject catcher;
        private RectTransform listRect;
        private RectTransform contentRect;
        private readonly List<GameObject> itemObjects = new List<GameObject>();
        private readonly List<TextMeshProUGUI> itemLabels = new List<TextMeshProUGUI>();

        private IList<string> options;
        private System.Func<int> readIndex;
        private int index;
        private bool expanded;
        private bool interactable = true;

        /// <summary>用户选中某一项时触发（写回**下标**）；<b>程序化设值不触发</b>。</summary>
        public System.Action<int> onChanged;

        /// <summary>当前选中的下标。</summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>列表当前是否展开（面板据此实现"两级 Esc"）。</summary>
        public bool IsExpanded
        {
            get { return expanded; }
        }

        // ══════════════════ 构建 ══════════════════

        /// <summary>
        /// 建一个下拉并挂到 <paramref name="parent"/> 下（行内使用）。
        /// </summary>
        /// <param name="parent">父节点（通常是行 HLG）</param>
        /// <param name="overlayLayer">面板提供的**浮层挂载点**（列表挂它下面才能盖住面板且不被裁剪）</param>
        /// <param name="style">自定义样式（null = 用 <see cref="DefaultStyle"/>）</param>
        public static DropdownField Create(Transform parent, Transform overlayLayer, Style style = null)
        {
            Style current = style ?? DefaultStyle;

            GameObject root = UIFactory.NewUIObject("dropdownField", parent);
            var layout = UIFactory.GetOrAddLayout(root);
            layout.preferredHeight = current.Height;
            layout.minHeight = current.Height;
            layout.flexibleWidth = 1f; // 吃掉行内除标签以外的剩余宽度
            UIFactory.AddHLG(root, alignment: TextAnchor.MiddleLeft, spacing: current.Spacing);

            var field = root.AddComponent<DropdownField>();
            field.CurrentStyle = current;
            field.overlayLayer = overlayLayer;

            // header 的可交互层：不透明底 + Button（纯代码不用 KButton，transition = None）
            field.headerImage = UIFactory.AddBackground(root, current.HeaderColor, true);
            field.headerButton = root.AddComponent<Button>();
            field.headerButton.targetGraphic = field.headerImage;
            field.headerButton.transition = Selectable.Transition.None;
            field.headerButton.onClick.AddListener(field.OnHeaderClicked);

            field.captionText = UIFactory.CreateText(root.transform, "caption", string.Empty,
                current.CaptionFontSize, current.CaptionColor, TextAlignmentOptions.MidlineLeft, false);
            UIFactory.AddFlexibleWidth(field.captionText.gameObject);
            UIFactory.AddPreferredHeight(field.captionText.gameObject, current.Height);

            field.arrowText = UIFactory.CreateText(root.transform, "arrow", current.ArrowClosedLabel,
                current.ArrowFontSize, current.ArrowColor, TextAlignmentOptions.Center, false);
            UIFactory.AddFixedSize(field.arrowText.gameObject, current.ArrowWidth, current.Height);

            field.BuildOverlay();
            return field;
        }

        /// <summary>
        /// 建浮层侧（点击承接层 + 选项列表）。全部挂在 <paramref name="overlayLayer"/> 下、初始隐藏。
        /// 没有浮层挂载点时**不建列表**（退化为只读显示并打一条警告）而不是崩掉。
        /// </summary>
        private void BuildOverlay()
        {
            if (overlayLayer == null)
            {
                Debug.LogWarning("[DebugPlus] 下拉控件没拿到浮层挂载点，本控件将退化为只读显示。");
                return;
            }

            overlayRoot = UIFactory.NewUIObject("dropdownOverlay", overlayLayer);
            UIFactory.Stretch(overlayRoot);
            UIFactory.GetOrAddLayout(overlayRoot).ignoreLayout = true; // ① 脱离布局流

            // 点击承接层：铺满整屏；颜色由 UIColors.ClickCatcher 现算（alpha 必须非零，否则收不到点击）
            catcher = UIFactory.NewUIObject("catcher", overlayRoot.transform);
            UIFactory.Stretch(catcher);
            Image catcherImage = UIFactory.AddBackground(catcher, UIColors.ClickCatcher(CurrentStyle.CatcherColor), true);
            var catcherButton = catcher.AddComponent<Button>();
            catcherButton.targetGraphic = catcherImage;
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(Collapse);

            // 列表本体
            GameObject list = UIFactory.NewUIObject("list", overlayRoot.transform);
            listRect = UIFactory.AnchorTopLeft(list, CurrentStyle.MinListWidth, CurrentStyle.ItemHeight);
            UIFactory.AddBackground(list, CurrentStyle.ListColor, true);

            var scrollRect = list.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.inertia = false;

            // 视口：用 RectMask2D 而不是 Image + Mask —— Mask 需要一个 Graphic 当模板，
            // 而透明的 Graphic 会被剔除（与点击承接层同一个 alpha 问题）；RectMask2D 不需要 Graphic
            //（本 Mod 的 TextField 里用的就是它）。
            GameObject viewport = UIFactory.NewUIObject("viewport", list.transform);
            UIFactory.StretchWithInset(viewport, CurrentStyle.ListPadding, CurrentStyle.ListPadding);
            viewport.AddComponent<RectMask2D>();

            GameObject content = UIFactory.NewUIObject("content", viewport.transform);
            contentRect = UIFactory.Rect(content);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            // 行铺满内容宽度：forceExpandWidth = true + 每项只给自己的 preferredHeight
            UIFactory.AddVLG(content, forceExpandWidth: true, alignment: TextAnchor.UpperLeft);
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = UIFactory.Rect(viewport);
            scrollRect.content = contentRect;

            overlayRoot.SetActive(false);
        }

        // ══════════════════ 绑定 / 取值设值 ══════════════════

        /// <summary>
        /// 🔴 **绑定接口（推荐用法）**：把"选项从哪来、读哪个下标、写回哪里"一次性交给本组件。
        /// 初值从 <paramref name="readIndex"/> 取，**不写回**（现状不该被当成用户操作）。
        /// </summary>
        public DropdownField Bind(IList<string> options, System.Func<int> readIndex, System.Action<int> writeIndex)
        {
            this.options = options;
            this.readIndex = readIndex;
            index = readIndex != null ? readIndex() : 0;

            RebuildItems();
            RefreshCaption();
            ApplyItemColors();

            if (writeIndex != null)
            {
                onChanged = writeIndex; // 最后才接上写回通道
            }
            return this;
        }

        /// <summary>程序化设下标（不触发 <see cref="onChanged"/>）。</summary>
        public void SetIndex(int newIndex, bool notify = false)
        {
            int count = options != null ? options.Count : 0;
            int clamped = count > 0 ? Mathf.Clamp(newIndex, 0, count - 1) : 0;
            bool changed = clamped != index;
            index = clamped;
            RefreshCaption();
            ApplyItemColors();
            if (notify && changed && onChanged != null)
            {
                onChanged(index);
            }
        }

        /// <summary>从参数侧重读下标并刷新显示（"游戏侧现值变了要刷一下"时用）。</summary>
        public void Refresh()
        {
            if (readIndex != null)
            {
                SetIndex(readIndex());
            }
        }

        /// <summary>整体可交互开关（禁用时压暗、不响应点击，并收起已展开的列表）。</summary>
        public void SetInteractable(bool newInteractable)
        {
            interactable = newInteractable;
            if (headerButton != null)
            {
                headerButton.interactable = newInteractable;
            }
            if (headerImage != null)
            {
                headerImage.color = newInteractable
                    ? CurrentStyle.HeaderColor
                    : UIColors.Disabled(CurrentStyle.HeaderColor);
            }
            if (captionText != null)
            {
                captionText.color = newInteractable
                    ? CurrentStyle.CaptionColor
                    : UIColors.Disabled(CurrentStyle.CaptionColor);
            }
            if (arrowText != null)
            {
                arrowText.color = newInteractable
                    ? CurrentStyle.ArrowColor
                    : UIColors.Disabled(CurrentStyle.ArrowColor);
            }
            if (!newInteractable)
            {
                Collapse();
            }
        }

        // ══════════════════ 展开 / 收起 ══════════════════

        /// <summary>
        /// 展开列表。没有选项、没有浮层挂载点、或已展开时什么都不做。
        /// ⚠️ 列表展开期间，点 header 会被**点击承接层**先接住 ⇒ 效果同样是"收起来"，
        /// 所以不需要在 header 上做 toggle 判断（两条路径都落到 <see cref="Collapse"/>）。
        /// </summary>
        public void Expand()
        {
            if (expanded || overlayRoot == null || !interactable)
            {
                return;
            }
            int count = options != null ? options.Count : 0;
            if (count == 0)
            {
                return; // 无选项就不弹一个空列表
            }

            // ② 抬到最上层：浮层整体 + 本下拉自己的浮层（同屏可能有别的浮层）
            if (overlayLayer != null)
            {
                overlayLayer.SetAsLastSibling();
            }
            overlayRoot.transform.SetAsLastSibling();

            AlignList(); // ③ 定位（在显示之前算好，避免闪一下）
            overlayRoot.SetActive(true);
            expanded = true;
            UpdateArrow();

            // 每次打开都从顶部开始看（列表可能上一次被拖到中间过）
            ScrollRect scroll = listRect != null ? listRect.GetComponent<ScrollRect>() : null;
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>收起列表（未展开时无操作）。</summary>
        public void Collapse()
        {
            if (!expanded || overlayRoot == null)
            {
                return;
            }
            expanded = false;
            overlayRoot.SetActive(false); // ⑤ 显隐用 SetActive，不参与布局计算
            UpdateArrow();
        }

        /// <summary>箭头方向（收起 = <see cref="Style.ArrowClosedLabel"/>；展开 = <see cref="Style.ArrowOpenLabel"/>）。</summary>
        private void UpdateArrow()
        {
            if (arrowText != null)
            {
                arrowText.text = expanded ? CurrentStyle.ArrowOpenLabel : CurrentStyle.ArrowClosedLabel;
            }
        }

        // ══════════════════ 内部 ══════════════════

        private void OnHeaderClicked()
        {
            if (!interactable)
            {
                return;
            }
            if (expanded)
            {
                Collapse();
                return;
            }
            Expand();
        }

        private void OnItemClicked(int itemIndex)
        {
            SetIndex(itemIndex, true); // 用户选中 = 用户操作，写回
            Collapse();
        }

        /// <summary>列表高度：最多显示 <see cref="Style.MaxVisibleItems"/> 项，超出部分靠滚动看。</summary>
        private float ListHeight(int count)
        {
            int visible = Mathf.Min(count, CurrentStyle.MaxVisibleItems);
            return visible * CurrentStyle.ItemHeight + CurrentStyle.ListPadding * 2f;
        }

        /// <summary>
        /// ③ 世界坐标定位：把列表左上角对齐到 header 左下角下方；
        /// 下方放不下就翻到 header 上方；再横向夹进浮层范围内。
        /// 不用 Canvas/Camera（IL2CPP 下 `AddComponent&lt;Canvas&gt;()` 返回 null）。
        /// </summary>
        private void AlignList()
        {
            if (listRect == null || overlayRoot == null)
            {
                return;
            }
            int count = options != null ? options.Count : 0;
            RectTransform headerRect = UIFactory.Rect(gameObject);
            RectTransform overlayRect = UIFactory.Rect(overlayRoot);

            float listWidth = Mathf.Max(headerRect.rect.width, CurrentStyle.MinListWidth);
            float listHeight = ListHeight(count);
            UIFactory.AnchorTopLeft(listRect.gameObject, listWidth, listHeight);
            // 内容高度按项数给死（ContentSizeFitter 也会算，但显式给值让"刚展开那一帧"就是对的）
            contentRect.sizeDelta = new Vector2(0f, count * CurrentStyle.ItemHeight);

            Vector3 worldBottomLeft = headerRect.TransformPoint(
                new Vector3(headerRect.rect.xMin, headerRect.rect.yMin, 0f));
            Vector3 local = overlayRect.InverseTransformPoint(worldBottomLeft);

            float top = local.y - CurrentStyle.ListGap; // pivot = (0,1) ⇒ 这个 y 是列表**顶边**
            float bottomLimit = overlayRect.rect.yMin + CurrentStyle.ScreenMargin;
            if (top - listHeight < bottomLimit)
            {
                // 翻到 header 上方：列表底边落在 header 顶边之上
                Vector3 worldTopLeft = headerRect.TransformPoint(
                    new Vector3(headerRect.rect.xMin, headerRect.rect.yMax, 0f));
                float headerTop = overlayRect.InverseTransformPoint(worldTopLeft).y;
                top = headerTop + CurrentStyle.ListGap + listHeight;
            }

            float left = local.x;
            float rightLimit = overlayRect.rect.xMax - CurrentStyle.ScreenMargin;
            if (left + listWidth > rightLimit)
            {
                left = rightLimit - listWidth;
            }

            listRect.localPosition = new Vector3(left, top, 0f);
        }

        /// <summary>按选项列表重建列表项（选项变了才需要调用）。</summary>
        private void RebuildItems()
        {
            for (int i = 0; i < itemObjects.Count; i++)
            {
                if (itemObjects[i] != null)
                {
                    Destroy(itemObjects[i]);
                }
            }
            itemObjects.Clear();
            itemLabels.Clear();

            if (contentRect == null || options == null)
            {
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                int captured = i; // 闭包捕获：每一项钉住自己那一个下标
                string label = options[i] ?? string.Empty;

                GameObject item = UIFactory.NewUIObject("item" + i, contentRect);
                UIFactory.AddPreferredHeight(item, CurrentStyle.ItemHeight);
                Image itemImage = UIFactory.AddBackground(item, CurrentStyle.ItemColor, true);
                var itemButton = item.AddComponent<Button>();
                itemButton.targetGraphic = itemImage;
                itemButton.transition = Selectable.Transition.None;
                itemButton.onClick.AddListener(delegate { OnItemClicked(captured); });

                TextMeshProUGUI text = UIFactory.CreateText(item.transform, "label", label,
                    CurrentStyle.ItemFontSize, CurrentStyle.ItemTextColor,
                    TextAlignmentOptions.MidlineLeft, false);
                UIFactory.StretchWithInset(text.gameObject, CurrentStyle.ItemTextInset, 0f);

                itemObjects.Add(item);
                itemLabels.Add(text);
            }
        }

        private void RefreshCaption()
        {
            if (captionText == null)
            {
                return;
            }
            int count = options != null ? options.Count : 0;
            if (count == 0)
            {
                captionText.text = (string)STRINGS.UI.DEBUGPLUS.PANEL_CHOICE_EMPTY;
                return;
            }
            captionText.text = index >= 0 && index < count ? (options[index] ?? string.Empty) : string.Empty;
        }

        /// <summary>只把**当前选中项**的文字提亮（不改底色：状态底色的归属留给色板纪律，不在这里发明）。</summary>
        private void ApplyItemColors()
        {
            for (int i = 0; i < itemLabels.Count; i++)
            {
                if (itemLabels[i] != null)
                {
                    itemLabels[i].color = i == index
                        ? CurrentStyle.ItemSelectedTextColor
                        : CurrentStyle.ItemTextColor;
                }
            }
        }

        // 面板被关掉时是 Destroy(gameObject)：列表随浮层一起销毁，这里只是把状态摆正，
        // 免得将来有人给本组件加"静态展开登记"时留下悬空引用（TextField.EditingCount 的教训）。
        // ⚠️ 列表浮层与 header **不在同一条父链上**（列表挂在面板的浮层挂载点下），
        //    所以这里必须显式收起，不能只把标志位改成 false。
        private void OnDisable()
        {
            Collapse();
        }
    }
}
