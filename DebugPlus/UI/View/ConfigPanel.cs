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

        /// <summary>
        /// 临时自检区每一行的**统一**高度（分区标题 / 数值 / 输入 / 勾选 / 状态五行同高）。
        /// ⚠️ 统一取值是刻意的：窗口高度只需"行数 × 本值"，不必在第二处复述整个自检区的构成
        /// （两处不一致就会出现"面板高度与实际内容对不上"的隐性 bug）。
        /// 本值须容得下 <see cref="NumberField"/> / <see cref="TextField"/> 的 28 高。
        /// </summary>
        private const float SelfTestRowHeight = 32f;

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

        // ══════════ 临时自检区字段（批 3-2 挂载用；批 3-3 类型分派挂上后**整块删除**）══════════

        /// <summary>
        /// 自检区开关。用 `static readonly` 而**不是** `const`：`if (const 常量)` 会触发
        /// CS0162「不可达代码」警告，而非编译期常量没有这个副作用 —— 改这一行即可整体关掉自检区。
        /// </summary>
        private static readonly bool SelfTest = true;

        private NumberField selfTestNumber;
        private TextField selfTestText;
        private ToggleField selfTestToggle;
        private TextMeshProUGUI selfTestStatus;

        /// <summary>自检区的"模型"：三个只属于本面板的字段 —— 控件只读写它们，**不碰任何游戏对象**。</summary>
        private float selfTestNumberValue = 50f;
        private string selfTestTextValue = STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_TEXT_INITIAL;
        private bool selfTestToggleValue;

        /// <summary>自检区已建行数（由 <see cref="AddSelfTestRow"/> 累加，窗口高度按它算间距）。</summary>
        private int selfTestRowsBuilt;

        /// <summary>状态行上次的文本（每帧只比一次字符串，变了才写，避免无谓的文本重建）。</summary>
        private string lastSelfTestStatus;

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
        /// 绑定目标：写目标名 → 按注册表建参数行 → 建临时自检区 → 无参数行时才显示提示 → 按行数重算窗口高度。
        /// 调用时机在 Activate() 之前（OpenFor 里），所以这里是"先摆好再显示"。
        /// </summary>
        private void SetTarget(GameObject target)
        {
            if (targetText != null)
            {
                targetText.text = TargetName(target);
            }

            int rowCount = BuildParamRows(target);
            int selfTestRows = BuildSelfTestRows(); // 临时自检区（批 3-3 删除，见方法注释）

            if (noteRow != null)
            {
                noteRow.SetActive(rowCount == 0); // 布局会跳过不激活的子项，不留空档
            }
            if (windowRect != null)
            {
                windowRect.sizeDelta = new Vector2(WindowWidth, ComputeHeight(rowCount, selfTestRows));
            }

            Debug.Log("[DebugPlus] 配置面板已打开：" + TargetName(target) + "（参数行 " + rowCount +
                " 行，自检行 " + selfTestRows + " 行）");
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
                InsertBeforeButtonRow(row.gameObject);
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
        /// </summary>
        private static float ComputeHeight(int rowCount, int selfTestRows)
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
            if (selfTestRows > 0)
            {
                children += selfTestRows * SelfTestRowHeight;
                gaps += selfTestRows;
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

        // ══════════════════════════════════════════════════════════════════════════════
        // 🔧 临时自检区（批 3-2 的实机入口）—— **批 3-3 类型分派挂上后整块删除**
        //
        // 为什么会有这一块：批 3-2 只交付构件（NumberField / TextField / 编辑期吞键），
        // 而面板里没有任何参数行用它 ⇒ 实机上根本看不见、点不到，bug 也就无从发现。
        // 但批 3-3 的参数类型分派还没设计成型，此刻硬做出来等于把未定型的形状锁死。
        // 折中：用一块**明确标注"临时"**的自检区走最短路径把三个构件摆出来 ——
        // "控件本身是否成立"先被验证，再由批 3-3 用真正的类型分派替换掉它。
        //
        // 副作用承诺：三条自检行的读写**只落在本面板自己的三个字段**
        // （selfTestNumberValue / selfTestTextValue / selfTestToggleValue）——
        // 不接触任何游戏对象、不写存档、不碰时间（时间中性铁律不受影响）。
        //
        // 删除清单（一次删干净，不留死代码）：
        //   本注释块 + Update() + SelfTestStatusText() + BuildSelfTestRows() + CreateSelfTestRow()
        //   + AddSelfTestRow() + OnSelfTest* 三个回调 + 类顶部"临时自检区字段"那一段
        //   + SelfTestRowHeight 常量 + SetTarget() 里调 BuildSelfTestRows 与 ComputeHeight 的两行
        //   + ComputeHeight 的第二个参数及它末尾那段 + STRINGS 里的 PANEL_SELFTEST_* 几条
        //   （InsertBeforeButtonRow 不删：它是参数行与自检行共用的规则，本来就该有。）
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 自检区的轮询：只刷新那一条状态行。
        /// 为什么要每帧看：<see cref="TextField.EditingCount"/> 是**会漏**的量（组件销毁时机不由我们控制），
        /// 漏一次就永久卡在"编辑中"、整局键盘失灵 —— 这个数必须当场可见，不能等打完日志再翻文件。
        /// 代价是每帧一次字符串比较，变了才写，不产生每帧的文本重建。
        /// ⚠️ 随自检区一起删除。
        /// </summary>
        private void Update()
        {
            if (selfTestStatus == null)
            {
                return;
            }
            string text = SelfTestStatusText();
            if (text != lastSelfTestStatus)
            {
                lastSelfTestStatus = text;
                selfTestStatus.text = text;
            }
        }

        /// <summary>
        /// 自检状态行文本：吞键补丁（B6）的现场读数 —— 是否正在编辑 + 编辑计数 + 数值框回读。
        /// 数值框回读显示的是**模型侧**的值（<see cref="OnSelfTestNumberChanged"/> 写进来的），
        /// 与数值框自己显示的数字互为印证：两者不一致就说明写回通道没接通。
        /// </summary>
        private string SelfTestStatusText()
        {
            // 显式转 string：LocString 到 string 是隐式转换，但 string.Format 的重载很多，写明不留歧义
            return string.Format((string)STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_STATUS,
                TextField.IsEditing
                    ? (string)STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_YES
                    : (string)STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_NO,
                TextField.EditingCount,
                selfTestNumberValue);
        }

        /// <summary>
        /// 建整个自检区，返回本区实际占用的**行数**（窗口高度按它算间距，
        /// 故行数由 <see cref="AddSelfTestRow"/> 逐个累加，不手写常量 —— 手写会和实际行数悄悄分家）。
        /// 五行分别是：分区标题 / 数值框 / 输入框 / 勾选方块 / 状态行。
        /// </summary>
        private int BuildSelfTestRows()
        {
            selfTestRowsBuilt = 0;
            if (!SelfTest || windowRoot == null)
            {
                return 0;
            }

            // ① 分区标题：明写"临时"，免得以后被当成正式界面的一部分
            AddSelfTestRow(CreateCenteredRow(windowRoot, "selfTestTitle",
                STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_TITLE, SelfTestRowHeight, 14f).gameObject);

            // ② 数值框：范围 0–100、整数刻度、单位 % ⇒ 档位由 DeriveStep 推成 1（本批正要看的推导结果）
            GameObject numberRow = CreateSelfTestRow("selfTestNumber",
                STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_NUMBER);
            AddSelfTestRow(numberRow);
            selfTestNumber = NumberField.Create(numberRow.transform);
            selfTestNumber.Bind(
                read: null, // 不主动读任何东西：值只来自下面那个自检字段
                write: OnSelfTestNumberChanged,
                min: 0f,
                max: 100f,
                wholeNumbers: true,
                unit: STRINGS.UI.DEBUGPLUS.UNIT_PERCENT,
                initial: selfTestNumberValue);

            // ③ 输入框：回车 / 失焦提交、Esc 取消都只打日志，不改任何东西
            GameObject textRow = CreateSelfTestRow("selfTestText",
                STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_TEXT);
            AddSelfTestRow(textRow);
            selfTestText = TextField.Create(textRow.transform);
            selfTestText.SetText(selfTestTextValue);
            selfTestText.onEndEdit = OnSelfTestTextEndEdit;

            // ④ 勾选方块：顺手把第三个构件也过一眼（它与数值框同屏，才能看出三种控件的高度是否齐）
            GameObject toggleRow = CreateSelfTestRow("selfTestToggle",
                STRINGS.UI.DEBUGPLUS.PANEL_SELFTEST_TOGGLE);
            AddSelfTestRow(toggleRow);
            selfTestToggle = ToggleField.Create(toggleRow.transform, selfTestToggleValue,
                OnSelfTestToggleChanged);

            // ⑤ 状态行（内容由 Update 每帧刷）
            selfTestStatus = CreateCenteredRow(windowRoot, "selfTestStatus",
                SelfTestStatusText(), SelfTestRowHeight, 13f);
            AddSelfTestRow(selfTestStatus.gameObject);

            return selfTestRowsBuilt;
        }

        /// <summary>
        /// 自检区的一行：标签列（与参数行**同宽同字号**，取自 <see cref="ParameterRowFactory"/>）
        /// + 空白内容区，内容由调用方往 <paramref name="row"/> 的 transform 下挂。
        /// 结构与本 Mod 的参数行一致：行 HLG + 固定宽标签 + 弹性内容列，不在行内再套布局组。
        /// </summary>
        private GameObject CreateSelfTestRow(string name, string label)
        {
            GameObject row = UIFactory.NewUIObject(name, windowRoot.transform);
            UIFactory.AddPreferredHeight(row, SelfTestRowHeight);
            UIFactory.AddHLG(row, alignment: TextAnchor.MiddleLeft, spacing: Spacing);

            TextMeshProUGUI text = UIFactory.CreateText(row.transform, "label", label,
                ParameterRowFactory.LabelFontSize, UIColors.RegularText,
                TextAlignmentOptions.MidlineLeft, false);
            UIFactory.AddFixedSize(text.gameObject, ParameterRowFactory.LabelWidth, SelfTestRowHeight);
            return row;
        }

        /// <summary>把自检行插到按钮行之前并计入行数（顺序即创建顺序：自检区落在参数行**下方**）。</summary>
        private void AddSelfTestRow(GameObject row)
        {
            if (row == null)
            {
                return;
            }
            InsertBeforeButtonRow(row);
            selfTestRowsBuilt++;
        }

        /// <summary>自检 · 数值框：用户按 ◄ ► 或回车提交时触发（程序化设值不会走到这里）。</summary>
        private void OnSelfTestNumberChanged(float value)
        {
            selfTestNumberValue = value; // 只写本面板字段：状态行会把它显示出来，供与控件显示互证
            Debug.Log("[DebugPlus] 自检 · 数值框 onChanged：" +
                value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 自检 · 输入框：结束编辑（回车 / 点到别处 / Esc 取消）。
        /// Esc 取消时 TMP 已把文本还原成原文，故用 <see cref="TextField.WasCanceled"/> 判定是否采纳 ——
        /// 这正是 <see cref="NumberField"/> 内部防"半截输入写进游戏"的同一条判据。
        /// </summary>
        private void OnSelfTestTextEndEdit(string value)
        {
            bool canceled = selfTestText != null && selfTestText.WasCanceled;
            if (!canceled)
            {
                selfTestTextValue = value ?? string.Empty;
            }
            Debug.Log("[DebugPlus] 自检 · 输入框 onEndEdit（" +
                (canceled ? "Esc 取消，未采纳" : "采纳") + "）：\"" + (value ?? string.Empty) + "\"");
        }

        /// <summary>自检 · 勾选方块：用户点击时触发。</summary>
        private void OnSelfTestToggleChanged(bool value)
        {
            selfTestToggleValue = value;
            Debug.Log("[DebugPlus] 自检 · 勾选 onChanged：" + (value ? "是" : "否"));
        }
    }
}
