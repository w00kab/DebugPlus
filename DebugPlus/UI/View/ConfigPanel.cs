using DebugPlus.Operations;
using DebugPlus.UI.Component;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPlus.UI.View
{
    /// <summary>
    /// DebugPlus 配置面板（M1 落地细节之三）：**自建**的 KModalScreen 模态弹窗。
    ///
    /// 为什么可以不用预制体自建 —— 每一条都有反编译源码依据：
    /// ① KScreen.Activate() 是自足的原版公开方法（KScreen.cs:276-282）：
    ///    SetActive(true) → KScreenManager.Instance.PushScreen(this) → OnActivate() → isActive = true。
    ///    入栈之后 KScreenManager.OnKeyDown 会自栈顶向下把按键交给它（KScreenManager.cs:179-199），
    ///    而 KModalScreen.OnKeyDown 会消费 Escape / 右键 → Deactivate()（KModalScreen.cs:133-136）
    ///    ⇒ 「Esc 关闭」和「必须先关掉才能做别的操作」都是原版机制，本 Mod 不自己处理按键。
    /// ② KModalScreen.OnPrefabInit() 自己就生成全屏半透明遮罩
    ///    （KModalScreen.cs:12/16-30：Image Color32(0,0,0,160) + raycastTarget = true），
    ///    并在 OnCmpEnable/OnCmpDisable 里禁用/恢复镜头操作（KModalScreen.cs:44-72）。
    /// ③ 运行时 AddComponent 出来的组件会走 Awake → InitializeComponent → OnPrefabInit
    ///    （KMonoBehaviour.cs:35/41/45/63），即直接 AddComponent 就完成框架初始化；
    ///    InitializeComponent 由 isInitialized 守卫，可安全重复调用（KMonoBehaviour.cs:47-50）。
    ///    Subscribe 之类的框架能力依赖 InitializeComponent 里赋值的 obj（KMonoBehaviour.cs:56/253-255）。
    ///
    /// 时间中性（plan.md §二.2）：把 KModalScreen.pause 显式设为 false —— 原版默认 true 会在打开时
    /// 强制暂停、关闭时解除暂停，会把玩家自己按下的暂停状态改掉。本面板**绝不改动游戏速度与暂停状态**，
    /// 也不触碰任何时间/调度器 API。
    ///
    /// 生命周期：KScreen.Deactivate() 末尾会 Destroy(gameObject)（KScreen.cs:299-302），
    /// 所以不存在「复用同一个实例」，改为「已有实例就不再叠第二个」。
    /// </summary>
    public class ConfigPanel : KModalScreen
    {
        private const float WindowWidth = 460f;
        private const float TitleHeight = 34f;
        private const float TextRowHeight = 26f;
        private const float NoteRowHeight = 52f;
        private const float ButtonRowHeight = 44f;
        private const float ButtonWidth = 150f;
        private const float ButtonHeight = 36f;

        /// <summary>窗口内边距的上下合计 + 子项间距（与 BuildWindow 里的 RectOffset / spacing 一致）。</summary>
        private const float PanelPaddingVertical = 28f;

        private const float Spacing = 8f;

        /// <summary>当前唯一实例（UnityEngine.Object 的 == null 对已销毁对象成立，天然处理关闭后的空引用）。</summary>
        private static ConfigPanel instance;

        private GameObject windowRoot;
        private RectTransform windowRect;
        private TextMeshProUGUI targetText;
        private GameObject noteRow;
        private GameObject buttonRow;

        /// <summary>
        /// 打开面板。若已有面板，则只把它抬到最上层（不叠加第二个）。
        /// 批 2b 的参数行会加在 windowRoot 的 VerticalLayoutGroup 下（关闭按钮行之前）。
        /// </summary>
        public static void OpenFor(GameObject target)
        {
            if (target == null)
            {
                return;
            }
            if (instance != null)
            {
                instance.transform.SetAsLastSibling();
                return;
            }
            if (KScreenManager.Instance == null || GameScreenManager.Instance == null ||
                GameScreenManager.Instance.ssOverlayCanvas == null)
            {
                Debug.LogWarning("[DebugPlus] 屏幕管理器尚未就绪，无法打开配置面板。");
                return;
            }

            var go = new GameObject("ConfigPanel");
            // 规则：新建 GameObject 先加 RectTransform，再加其它 UI 组件（LayoutElement 等会因
            // [RequireComponent] 自动补一个，重复添加会 NRE）。
            var rootRect = go.AddComponent<RectTransform>();
            Stretch(rootRect);

            // Awake → InitializeComponent → OnPrefabInit：遮罩与内容区在此生成。
            var panel = go.AddComponent<ConfigPanel>();
            panel.InitializeComponent(); // 幂等兜底（框架已初始化时立即返回）
            panel.SetTarget(target);

            // 原版入栈路径：AddExistingChild（SetParent + 同步 layer）→ Activate（入栈 + OnActivate）
            KScreenManager.AddExistingChild(GameScreenManager.Instance.ssOverlayCanvas, go);
            panel.transform.SetAsLastSibling();
            panel.Activate();

            instance = panel;
        }
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit(); // KModalScreen：建遮罩、ConsumeMouseScroll = true、activateOnSpawn = true
            pause = false;       // 时间中性：不改动游戏速度与暂停状态
            BuildWindow();
        }

        protected override void OnDeactivate()
        {
            if (instance == this)
            {
                instance = null;
            }
            base.OnDeactivate();
        }

        protected override void OnCleanUp()
        {
            if (instance == this)
            {
                instance = null;
            }
            base.OnCleanUp();
        }

        /// <summary>
        /// 绑定目标：写目标名 → 按注册表建参数行 → 无参数行时才显示提示 → 按行数重算窗口高度。
        /// 调用时机在 Activate() 之前（OpenFor 里），所以这里是"先摆好再显示"。
        /// </summary>
        private void SetTarget(GameObject target)
        {
            if (targetText != null)
            {
                targetText.text = TargetName(target);
            }

            int rowCount = BuildParamRows(target);

            if (noteRow != null)
            {
                noteRow.SetActive(rowCount == 0); // 布局会跳过不激活的子项，不留空档
            }
            if (windowRect != null)
            {
                windowRect.sizeDelta = new Vector2(WindowWidth, ComputeHeight(rowCount));
            }

            Debug.Log("[DebugPlus] 配置面板已打开：" + TargetName(target) + "（参数行 " + rowCount + " 行）");
        }

        /// <summary>
        /// 按注册表构建参数行，逐行插到按钮行之前。
        /// 全部是根 VLG 的**直接子节点** —— 规则：不在 ONI 里嵌套 VLG → VLG。
        /// </summary>
        private int BuildParamRows(GameObject target)
        {
            var parameters = OperationRegistry.BuildParameters(target);
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterRow row = ParameterRowFactory.Create(parameters[i], windowRoot.transform);
                if (buttonRow != null)
                {
                    row.transform.SetSiblingIndex(buttonRow.transform.GetSiblingIndex());
                }
            }
            return parameters.Count;
        }

        /// <summary>窗口高度按实际行数算（标题 + 目标名 + 参数行 / 提示 + 按钮行 + 间距 + 内边距）。</summary>
        private static float ComputeHeight(int rowCount)
        {
            float children = TitleHeight + TextRowHeight + ButtonRowHeight;
            float gaps = 2f;
            if (rowCount > 0)
            {
                children += rowCount * ParameterRowFactory.RowHeight;
                gaps += rowCount;
            }
            else
            {
                children += NoteRowHeight;
                gaps += 1f;
            }
            return children + gaps * Spacing + PanelPaddingVertical;
        }

        private static string TargetName(GameObject target)
        {
            if (target == null)
            {
                return STRINGS.UI.DEBUGPLUS.PANEL_NO_TARGET;
            }
            string name = target.GetProperName(); // KSelectableExtensions.GetProperName（无 KSelectable 时返回 ""）
            return string.IsNullOrEmpty(name) ? target.name : name;
        }

        /// <summary>
        /// 自建内容区：窗口（不透明底）→ 垂直布局 → 标题 / 目标名 / 占位说明 / 按钮行。
        /// 布局规则（oni-ui）：只用「根 VLG → 行 HLG」一层嵌套，禁止 VLG→VLG；
        /// VLG 的 childForceExpandHeight = false；HLG 的 childForceExpandHeight = false。
        /// </summary>
        private void BuildWindow()
        {
            windowRoot = new GameObject("window");
            windowRoot.transform.SetParent(transform, false);
            var rect = windowRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(WindowWidth, ComputeHeight(0));
            windowRect = rect;

            var image = windowRoot.AddComponent<Image>();
            image.color = new Color(0.15f, 0.17f, 0.2f, 1f); // 不透明底
            image.raycastTarget = true;                      // 拦住面板区域的点击

            var vlg = windowRoot.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = Spacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false; // ★ 不拉伸高度

            CreateText(windowRoot, "title", STRINGS.UI.DEBUGPLUS.PANEL_TITLE, TitleHeight, 20f);
            TextMeshProUGUI target = CreateText(windowRoot, "target", "", TextRowHeight, 16f);
            targetText = target;
            // 参数行由 SetTarget → BuildParamRows 插在下面的按钮行之前；无参数行时这条提示才显示。
            noteRow = CreateText(windowRoot, "note", STRINGS.UI.DEBUGPLUS.PANEL_NO_PARAMS, NoteRowHeight, 14f).gameObject;

            CreateButtonRow(windowRoot);
        }

        private void CreateButtonRow(GameObject window)
        {
            buttonRow = new GameObject("buttonRow");
            buttonRow.transform.SetParent(window.transform, false);
            buttonRow.AddComponent<RectTransform>();
            buttonRow.AddComponent<LayoutElement>().preferredHeight = ButtonRowHeight;

            var hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = Spacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;  // ★ VLG 子项为 HLG 时必须 false
            hlg.childForceExpandHeight = false; // ★ 规则：HLG 不作为 VLG 子项时也不拉伸高度

            CreateButton(buttonRow, STRINGS.UI.DEBUGPLUS.PANEL_CLOSE, Close);
        }

        private void CreateButton(GameObject parent, string label, System.Action onClick)
        {
            var go = new GameObject("button");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = ButtonWidth;
            layout.minWidth = ButtonWidth;
            layout.preferredHeight = ButtonHeight;
            layout.minHeight = ButtonHeight;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.82f, 0.84f, 0.86f, 1f); // 可点击元素底色必须不透明
            image.raycastTarget = true;

            // 纯代码场景用 Button（KButton 的 soundPlayer 是 [SerializeField]，纯代码会 NRE）；
            // transition = None：只用 onClick，避免 ColorTint 相乘导致颜色变深残留。
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate
            {
                onClick();
            });

            CreateText(go, "label", label, 0f, 16f, false, new Color(0.1f, 0.1f, 0.1f, 1f));
        }

        /// <summary>创建一行文字（TMP 独占一个 GameObject：TMP 与 Image 同 GO 会冲突）。</summary>
        private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, float height, float fontSize)
        {
            return CreateText(parent, name, text, height, fontSize, true, Color.white);
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, float height,
            float fontSize, bool useLayout, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            if (useLayout)
            {
                go.AddComponent<LayoutElement>().preferredHeight = height;
            }
            else
            {
                Stretch(rect); // 按钮内的文字铺满按钮，由锚点控制尺寸
            }

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (Localization.FontAsset != null)
            {
                tmp.font = Localization.FontAsset; // 中文字形必须显式指定字体资源
            }
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false; // 装饰层不拦射线
            return tmp;
        }

        private void Close()
        {
            Deactivate(); // KScreen.Deactivate：OnDeactivate → PopScreen → Destroy(gameObject)
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
