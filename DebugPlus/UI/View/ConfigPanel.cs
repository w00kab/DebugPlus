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

            var go = UIFactory.NewUIObject("ConfigPanel", null);
            UIFactory.Stretch(go); // 铺满遮罩（KModalScreen 的遮罩是它的父级）

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
        /// 布局一律走 <see cref="UIFactory"/>（规则只在那一个文件里定义，此处不再手写布局组字段）。
        /// </summary>
        private void BuildWindow()
        {
            windowRoot = UIFactory.NewUIObject("window", transform);
            windowRect = UIFactory.AnchorCenter(windowRoot, WindowWidth, ComputeHeight(0));
            UIFactory.AddBackground(windowRoot, UIColors.BackgroundD, true); // 不透明底：拦住面板区域的点击

            // 根 VLG：撑满宽度 + 子项顶部居中（唯一用 forceExpandWidth=true 的地方）
            UIFactory.AddVLG(windowRoot,
                forceExpandWidth: true,
                alignment: TextAnchor.UpperCenter,
                spacing: Spacing,
                padding: new RectOffset(16, 16, 14, 14));

            CreateCenteredRow(windowRoot, "title", STRINGS.UI.DEBUGPLUS.PANEL_TITLE, TitleHeight, 20f);
            targetText = CreateCenteredRow(windowRoot, "target", "", TextRowHeight, 16f);
            // 参数行由 SetTarget → BuildParamRows 插在下面的按钮行之前；无参数行时这条提示才显示。
            noteRow = CreateCenteredRow(windowRoot, "note", STRINGS.UI.DEBUGPLUS.PANEL_NO_PARAMS,
                NoteRowHeight, 14f).gameObject;

            CreateButtonRow(windowRoot);
        }

        /// <summary>
        /// 建一行居中文字（子节点按顺序插到根 VLG 末尾）。注意：按钮行是**后建的**，
        /// 参数行由 <see cref="BuildParamRows"/> 在按钮行之前插入。
        /// </summary>
        private static TextMeshProUGUI CreateCenteredRow(GameObject parent, string name, string text,
            float height, float fontSize)
        {
            TextMeshProUGUI tmp = UIFactory.CreateCenteredText(parent.transform, name, text,
                fontSize, UIColors.PrimaryText);
            UIFactory.AddPreferredHeight(tmp.gameObject, height);
            return tmp;
        }

        private void CreateButtonRow(GameObject window)
        {
            buttonRow = UIFactory.NewUIObject("buttonRow", window.transform);
            UIFactory.AddPreferredHeight(buttonRow, ButtonRowHeight);
            // 行 HLG：子项居中、不拉伸（forceExpand* 全 false 是默认值，故无需写出）
            UIFactory.AddHLG(buttonRow, alignment: TextAnchor.MiddleCenter, spacing: Spacing);

            UIFactory.CreateButton(buttonRow.transform, "button", STRINGS.UI.DEBUGPLUS.PANEL_CLOSE,
                Close, 16f, UIColors.Background, UIColors.RegularText,
                width: ButtonWidth, height: ButtonHeight);
        }

        private void Close()
        {
            Deactivate(); // KScreen.Deactivate：OnDeactivate → PopScreen → Destroy(gameObject)
        }
    }
}
