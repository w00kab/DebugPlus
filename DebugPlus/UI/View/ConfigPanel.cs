using System.Collections.Generic;
using System.Globalization;
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
    /// 面板结构（批 3-3 起）：
    /// ```
    /// ConfigPanel（铺满 ssOverlayCanvas，KModalScreen 的遮罩是它的子节点）
    ///   ├── window            居中窗口：根 VLG + 标题 / 目标名 / 参数行… / 按钮行
    ///   └── overlay           🔴 **浮层挂载点**（B7）：铺满、**无布局组**、永远在最上层
    ///         └── …           下拉列表这类"要盖在面板之上、又不参与行布局"的东西挂这里
    /// ```
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

        /// <summary>自检区的下拉范例给几个选项（当前 6 个 ≤ MaxVisibleItems，故列表不需要滚动即可全见）。</summary>
        private const int SelfCheckOptionCount = 6;

        /// <summary>当前唯一实例（UnityEngine.Object 的 == null 对已销毁对象成立，天然处理关闭后的空引用）。</summary>
        private static ConfigPanel instance;

        private GameObject windowRoot;
        private RectTransform windowRect;
        private RectTransform overlayRect;
        private TextMeshProUGUI targetText;
        private GameObject noteRow;
        private GameObject buttonRow;

        // ══════════ 控件自检 · 用法范例区（批 3-2 起；批 3-3 起走真分派，长期保留）══════════

        /// <summary>
        /// 自检区开关。用 `static readonly` 而**不是** `const`：`if (const 常量)` 会触发
        /// CS0162「不可达代码」警告，而非编译期常量没有这个副作用 —— 改这一行即可整体关掉自检区。
        /// </summary>
        private static readonly bool SelfCheck = true;

        /// <summary>自检区已建行数（由 <see cref="AddSelfCheckRow"/> 累加，窗口高度按它算间距）。</summary>
        private int selfCheckRowsBuilt;

        /// <summary>自检区的"模型"：五个只属于本面板的字段 —— 控件只读写它们，**不碰任何游戏对象**。</summary>
        private float selfCheckSliderValue = 0.35f;
        private float selfCheckNumberValue = 50f;
        private string selfCheckTextValue = STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_TEXT_INITIAL;
        private bool selfCheckToggleValue;
        private int selfCheckChoiceIndex = 2;
        private IList<string> selfCheckOptions;

        private TextField selfCheckText;
        private TextMeshProUGUI selfCheckStatus;

        /// <summary>状态行上次的文本（每帧只比一次字符串，变了才写，避免无谓的文本重建）。</summary>
        private string lastSelfCheckStatus;

        /// <summary>
        /// 打开面板。若已有面板，则只把它抬到最上层（不叠加第二个）。
        /// 参数行会加在 windowRoot 的 VerticalLayoutGroup 下（关闭按钮行之前）。
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
            panel.InitializeComponent(); // 幂等兜底（框架已初始化时立刻返回）
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
        /// 按键处理 —— 只加一条本 Mod 自己的规矩：**两级 Esc**。
        ///
        /// 为什么要覆盖：原版 <c>KModalScreen.OnKeyDown</c>（KModalScreen.cs:133-136）见到 Escape 就
        /// `Deactivate()`，而批 3-2 给输入框定的是"**编辑中 Esc = 退出编辑，再按才关面板**"；
        /// 批 3-3 的下拉同理："**列表展开时 Esc = 先收起列表，再按才关面板**"，
        /// 否则一次 Esc 会把用户的面板和没选完的列表一起带走。
        ///
        /// 只在**确实有下拉展开**且**按的确实是 Esc**（`TryConsume` 成功）时才消费这一下，
        /// 其余情况一律原样交给 base —— 面板展开时"所有按键都被模态屏吃掉"是原版行为，不改。
        /// </summary>
        public override void OnKeyDown(KButtonEvent e)
        {
            if (!e.Consumed && HasExpandedDropdown() && e.TryConsume(global::Action.Escape))
            {
                CollapseExpandedDropdowns();
                return; // 这一下 Esc 已经用于"收起列表"，不再传给 base（那会关掉整个面板）
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// 绑定目标：写目标名 → 按注册表建参数行 → 建自检区 → 无参数行时才显示提示 → 按行数重算窗口高度。
        /// 调用时机在 Activate() 之前（OpenFor 里），所以这里是"先摆好再显示"。
        /// </summary>
        private void SetTarget(GameObject target)
        {
            if (targetText != null)
            {
                targetText.text = TargetName(target);
            }

            int rowCount = BuildParamRows(target);
            int selfCheckRows = BuildSelfCheckRows();

            if (noteRow != null)
            {
                noteRow.SetActive(rowCount == 0); // 布局会跳过不激活的子项，不留空档
            }
            if (windowRect != null)
            {
                windowRect.sizeDelta = new Vector2(WindowWidth, ComputeHeight(rowCount, selfCheckRows));
            }

            Debug.Log("[DebugPlus] 配置面板已打开：" + TargetName(target) + "（参数行 " + rowCount +
                " 行，自检行 " + selfCheckRows + " 行）");
        }

        /// <summary>
        /// 按注册表构建参数行（批 3-3 起由 <see cref="ParameterRowFactory"/> 按**参数类型**分派控件），
        /// 逐行插到按钮行之前。全部是根 VLG 的**直接子节点** —— 规则：不在 ONI 里嵌套 VLG → VLG。
        /// </summary>
        private int BuildParamRows(GameObject target)
        {
            var parameters = OperationRegistry.BuildParameters(target);
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterRow row = ParameterRowFactory.Create(parameters[i], windowRoot.transform, overlayRect);
                InsertBeforeButtonRow(row != null ? row.gameObject : null);
            }
            return parameters.Count;
        }

        /// <summary>
        /// 把一行插到按钮行之前（参数行与自检行共用同一规则）。
        /// 按钮行为空时不必处理：新建的行本来就在末尾，而按钮行是最后建的。
        /// </summary>
        private void InsertBeforeButtonRow(GameObject row)
        {
            if (row != null && buttonRow != null)
            {
                row.transform.SetSiblingIndex(buttonRow.transform.GetSiblingIndex());
            }
        }

        /// <summary>
        /// 窗口高度按实际行数算（标题 + 目标名 + 参数行 / 提示 + 自检区 + 按钮行 + 间距 + 内边距）。
        /// 参数行与自检行**同高**（都用 <see cref="ParameterRowFactory.RowHeight"/>），
        /// 所以两边可以合并算，不必在第二处复述自检区的构成。
        /// </summary>
        private static float ComputeHeight(int paramRowCount, int selfCheckRowCount)
        {
            float children = TitleHeight + TextRowHeight + ButtonRowHeight;
            float gaps = 2f;
            if (paramRowCount > 0)
            {
                children += paramRowCount * ParameterRowFactory.RowHeight;
                gaps += paramRowCount;
            }
            else
            {
                children += NoteRowHeight;
                gaps += 1f;
            }
            if (selfCheckRowCount > 0)
            {
                children += selfCheckRowCount * ParameterRowFactory.RowHeight;
                gaps += selfCheckRowCount;
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
        /// 自建内容区：窗口（不透明底）→ 垂直布局 → 标题 / 目标名 / 占位说明 / 按钮行；
        /// 再建**浮层挂载点**。
        /// 布局一律走 <see cref="UIFactory"/>（规则只在那一个文件里定义，此处不再手写布局组字段）。
        /// </summary>
        private void BuildWindow()
        {
            windowRoot = UIFactory.NewUIObject("window", transform);
            windowRect = UIFactory.AnchorCenter(windowRoot, WindowWidth, ComputeHeight(0, 0));
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

            // 🔴 浮层挂载点（批 3-3 · B7）：与 window **平级**、铺满面板根、**不挂任何布局组**、
            //    永远排在最上层。下拉列表这类"必须盖在面板之上、又不许参与行布局"的东西挂它下面：
            //    挂在 window 里会被窗口矩形裁掉、会被根 VLG 接管尺寸（那是行，不是浮层）。
            GameObject overlay = UIFactory.NewUIObject("overlay", transform);
            overlayRect = UIFactory.Stretch(overlay);
            overlay.transform.SetAsLastSibling(); // 渲染在 window 之上
        }

        /// <summary>
        /// 建一行居中文字（子节点按顺序插到根 VLG 末尾）。注意：按钮行是**后建的**，
        /// 参数行与自检行由 <see cref="InsertBeforeButtonRow"/> 在按钮行之前插入。
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

        // ══════════════════════════════════════════════════════════════════════════════
        // 控件自检 · 用法范例区
        //
        // 它是什么：本 Mod 每一种控件的**实机入口 + 用法范例**。
        //   ① 入口 —— 批 3-2 只交付构件时，面板里没有任何行用到它们 ⇒ 实机看不见、点不到、
        //      bug 也无从发现。这块区把控件摆到台面上；
        //   ② 范例 —— 四种控件（滑条 / 数值框 / 勾选 / 下拉）在这里各占一行，
        //      且**走的是真的 ParameterRowFactory 分派**（不是手搭的假行），
        //      所以它同时演示了"自定义参数怎么写"（见 SelfCheckParameters.cs）与
        //      "工厂怎么用"，以后新增控件/参数类型也先在这里见人。
        //
        // 副作用承诺：本区五个值的读写**只落在本面板自己的字段**
        // （selfCheckSliderValue / selfCheckNumberValue / selfCheckTextValue /
        //   selfCheckToggleValue / selfCheckChoiceIndex）——
        // 不接触任何游戏对象、不写存档、不碰时间（时间中性铁律不受影响）。
        //
        // 输入框那一行是**手搭**的：它不属于参数的类型家族
        // （参数只管"会被写回某处的值"，纯文本输入今天没有这种去处），
        // 所以它示范的是"直接用控件"而不是"工厂分派"。以后真有了文本参数，再补一个参数类型 + 一支分派。
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 自检区的轮询：只刷新那一条状态行。
        /// 为什么要每帧看：<see cref="TextField.EditingCount"/> 是**会漏**的量（组件销毁时机不由我们控制），
        /// 漏一次就永久卡在"编辑中"、整局键盘失灵 —— 这个数必须当场可见，不能等打完日志再翻文件。
        /// 代价是每帧一次字符串比较，变了才写，不产生每帧的文本重建。
        /// </summary>
        private void Update()
        {
            if (selfCheckStatus == null)
            {
                return;
            }
            string text = SelfCheckStatusText();
            if (text != lastSelfCheckStatus)
            {
                lastSelfCheckStatus = text;
                selfCheckStatus.text = text;
            }
        }

        /// <summary>
        /// 自检状态行文本：吞键补丁（B6）的现场读数 + 两条写回通道的互证。
        /// 「数值」「下拉」显示的是**模型侧**的值（各 OnSelfCheck* 回调写进来的），
        /// 与控件自己显示的内容互为印证：两者不一致就说明写回通道没接通。
        /// </summary>
        private string SelfCheckStatusText()
        {
            string choice = selfCheckOptions != null &&
                selfCheckChoiceIndex >= 0 && selfCheckChoiceIndex < selfCheckOptions.Count
                ? selfCheckOptions[selfCheckChoiceIndex]
                : string.Empty;

            // 显式转 string：LocString 到 string 是隐式转换，但 string.Format 的重载很多，写明不留歧义
            return string.Format((string)STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_STATUS,
                TextField.IsEditing
                    ? (string)STRINGS.UI.DEBUGPLUS.VALUE_YES
                    : (string)STRINGS.UI.DEBUGPLUS.VALUE_NO,
                TextField.EditingCount,
                selfCheckNumberValue,
                choice);
        }

        /// <summary>
        /// 建整个自检区，返回本区实际占用的**行数**（窗口高度按它算，故行数由
        /// <see cref="AddSelfCheckRow"/> 逐个累加，不手写常量 —— 手写会和实际行数悄悄分家）。
        /// 七行：分区标题 / 滑条 / 数值框 / 输入框 / 勾选 / 下拉 / 状态行。
        /// </summary>
        private int BuildSelfCheckRows()
        {
            selfCheckRowsBuilt = 0;
            if (!SelfCheck || windowRoot == null)
            {
                return 0;
            }

            // ① 分区标题：明写这是自检与范例，免得被当成正式界面的一部分
            AddSelfCheckRow(CreateCenteredRow(windowRoot, "selfCheckTitle",
                STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_TITLE,
                ParameterRowFactory.RowHeight, 14f).gameObject);

            // ② 滑条：0–1 的小数刻度（与上面「生长进度」的 0–100 整数不同，
            //    顺带演示"读数格式由参数自己定"：0.00 两位小数）
            AddSelfCheckRow(CreateParameterRow("selfCheckSlider", new SelfCheckNumericParameter(gameObject,
                STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_SLIDER,
                ReadSelfCheckSlider, WriteSelfCheckSlider,
                0f, 1f, false, NumericControl.Slider, string.Empty, "0.00")));

            // ③ 数值框：范围 0–100、整数刻度、单位 % ⇒ 档位由 DeriveStep 推成 1
            AddSelfCheckRow(CreateParameterRow("selfCheckNumber", new SelfCheckNumericParameter(gameObject,
                STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_NUMBER,
                ReadSelfCheckNumber, WriteSelfCheckNumber,
                0f, 100f, true, NumericControl.Number, STRINGS.UI.DEBUGPLUS.UNIT_PERCENT)));

            // ④ 输入框：手搭（见上方说明），回车 / 失焦提交、Esc 取消都只打日志，不改任何东西
            GameObject textRow = CreateSelfCheckRow("selfCheckText", STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_TEXT);
            AddSelfCheckRow(textRow);
            selfCheckText = TextField.Create(textRow.transform);
            selfCheckText.SetText(selfCheckTextValue);
            selfCheckText.onEndEdit = OnSelfCheckTextEndEdit;

            // ⑤ 勾选方块
            AddSelfCheckRow(CreateParameterRow("selfCheckToggle", new SelfCheckToggleParameter(gameObject,
                STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_TOGGLE,
                ReadSelfCheckToggle, WriteSelfCheckToggle)));

            // ⑥ 下拉：选项文本由参数提供、读写的是下标（连"选中项"也走同一套写回通道）
            selfCheckOptions = BuildSelfCheckOptions();
            AddSelfCheckRow(CreateParameterRow("selfCheckChoice", new SelfCheckChoiceParameter(gameObject,
                STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_CHOICE,
                selfCheckOptions, ReadSelfCheckChoice, WriteSelfCheckChoice)));

            // ⑦ 状态行（内容由 Update 每帧刷）
            selfCheckStatus = CreateCenteredRow(windowRoot, "selfCheckStatus",
                SelfCheckStatusText(), ParameterRowFactory.RowHeight, 13f);
            AddSelfCheckRow(selfCheckStatus.gameObject);

            return selfCheckRowsBuilt;
        }

        /// <summary>自检区的下拉选项文本（选项{1} … 选项{n}）。</summary>
        private static IList<string> BuildSelfCheckOptions()
        {
            var options = new List<string>(SelfCheckOptionCount);
            for (int i = 1; i <= SelfCheckOptionCount; i++)
            {
                options.Add(string.Format((string)STRINGS.UI.DEBUGPLUS.PANEL_SELFCHECK_OPTION, i));
            }
            return options;
        }

        /// <summary>
        /// 经**真分派**建一行参数（行名改成可辨认的名字，便于 Unity 层级里找）。
        /// 自检区与面板正常参数走的是同一个工厂方法 —— 这正是本区的价值所在。
        /// </summary>
        private GameObject CreateParameterRow(string name, Parameter parameter)
        {
            ParameterRow row = ParameterRowFactory.Create(parameter, windowRoot.transform, overlayRect);
            if (row == null)
            {
                return null;
            }
            row.gameObject.name = name;
            return row.gameObject;
        }

        /// <summary>
        /// 自检区的一行：标签列（与参数行**同宽同字号**，取自 <see cref="ParameterRowFactory"/>）
        /// + 空白内容区，内容由调用方往 <paramref name="row"/> 的 transform 下挂。
        /// 结构与本 Mod 的参数行一致：行 HLG + 固定宽标签 + 弹性内容列，不在行内再套布局组。
        /// </summary>
        private GameObject CreateSelfCheckRow(string name, string label)
        {
            GameObject row = UIFactory.NewUIObject(name, windowRoot.transform);
            UIFactory.AddPreferredHeight(row, ParameterRowFactory.RowHeight);
            UIFactory.AddHLG(row, alignment: TextAnchor.MiddleLeft, spacing: Spacing);

            TextMeshProUGUI text = UIFactory.CreateText(row.transform, "label", label,
                ParameterRowFactory.LabelFontSize, UIColors.RegularText,
                TextAlignmentOptions.MidlineLeft, false);
            UIFactory.AddFixedSize(text.gameObject, ParameterRowFactory.LabelWidth, ParameterRowFactory.RowHeight);
            return row;
        }

        /// <summary>把自检行插到按钮行之前并计入行数（顺序即创建顺序：自检区落在参数行**下方**）。</summary>
        private void AddSelfCheckRow(GameObject row)
        {
            if (row == null)
            {
                return;
            }
            InsertBeforeButtonRow(row);
            selfCheckRowsBuilt++;
        }

        // ── 自检区五个字段的读写通道（参数与控件只经这些方法，绝不直接碰字段）──

        private float ReadSelfCheckSlider()
        {
            return selfCheckSliderValue;
        }

        private void WriteSelfCheckSlider(float value)
        {
            selfCheckSliderValue = value;
            Debug.Log("[DebugPlus] 自检 · 滑条：" + value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private float ReadSelfCheckNumber()
        {
            return selfCheckNumberValue;
        }

        private void WriteSelfCheckNumber(float value)
        {
            selfCheckNumberValue = value; // 状态行会把它显示出来，供与控件显示互证
            Debug.Log("[DebugPlus] 自检 · 数值框：" +
                value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private bool ReadSelfCheckToggle()
        {
            return selfCheckToggleValue;
        }

        private void WriteSelfCheckToggle(bool value)
        {
            selfCheckToggleValue = value;
            Debug.Log("[DebugPlus] 自检 · 勾选：" + (value ? "是" : "否"));
        }

        private int ReadSelfCheckChoice()
        {
            return selfCheckChoiceIndex;
        }

        private void WriteSelfCheckChoice(int index)
        {
            selfCheckChoiceIndex = index;
            string text = selfCheckOptions != null && index >= 0 && index < selfCheckOptions.Count
                ? selfCheckOptions[index]
                : string.Empty;
            Debug.Log("[DebugPlus] 自检 · 下拉：下标 " + index + "（" + text + "）");
        }

        /// <summary>
        /// 自检 · 输入框：结束编辑（回车 / 点到别处 / Esc 取消）。
        /// Esc 取消时 TMP 已把文本还原成原文，故用 <see cref="TextField.WasCanceled"/> 判定是否采纳 ——
        /// 这正是 <see cref="NumberField"/> 内部防"半截输入写进游戏"的同一条判据。
        /// </summary>
        private void OnSelfCheckTextEndEdit(string value)
        {
            bool canceled = selfCheckText != null && selfCheckText.WasCanceled;
            if (!canceled)
            {
                selfCheckTextValue = value ?? string.Empty;
            }
            Debug.Log("[DebugPlus] 自检 · 输入框 onEndEdit（" +
                (canceled ? "Esc 取消，未采纳" : "采纳") + "）：\"" + (value ?? string.Empty) + "\"");
        }

        // ── 下拉展开状态（两级 Esc 用）──
        // 用"在面板子树里找"而不是静态登记表：面板一关就是 Destroy，静态表会留下悬空引用
        //（TextField.EditingCount 那次教训）；这里按键才查一次，代价可以忽略。

        private bool HasExpandedDropdown()
        {
            DropdownField[] fields = GetComponentsInChildren<DropdownField>(true);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null && fields[i].IsExpanded)
                {
                    return true;
                }
            }
            return false;
        }

        private void CollapseExpandedDropdowns()
        {
            DropdownField[] fields = GetComponentsInChildren<DropdownField>(true);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null)
                {
                    fields[i].Collapse();
                }
            }
        }
    }
}
