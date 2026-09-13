# NEXT_STEPS.md — DebugPlus · 调试增强 施工单与待办

> 本文件是**高频维护的施工单**（进度 / 待办 / 逐项方案）。
> **设计决策与唯一事实源仍是 plan.md**；协作纪律见 AGENT.md。
> 会话开始顺序：**plan.md（设计） → NEXT_STEPS.md（当前做什么） → AGENT.md（纪律）**。
> 图例：⏳ 待用户批准 / ✅ 已完成 / 🔧 施工中 / ⏸ 待用户实机验证

## 0. 当前状态（截至 2026-09-13）

- ✅ **P0 工程骨架**：编译 → 部署 → 游戏内加载通过（空 Mod）。
- ✅ **批 1 · 去上游化清理完成**（用户批准项 A：整体删除、**不重写**）：
  - 删除 5 个上游衍生文件 + 其目录：`Patches/SandboxToolsPatches.cs`、`UI/DestroyParameterMenu.cs`、
    `Tools/FilteredDestroyTool.cs`、`Tools/DestroyFilter.cs`、`SandboxToolsStrings.cs`（连 `Tools/` 目录一起删）。
    依据：逐块比对确认这 5 个文件的全部内容（分类清除工具、生成器额外分类、AETN 即时建造补铁）
    与 `【参考代码】sandTool\SandboxTools\SandboxToolsPatches.cs` 一一对应 → 重写即重复 SandboxTools
    能力（plan.md §二.1），故**不设替代文件**。
  - 改写：`DebugPlusMod.cs`（去掉上游字符串注册与框架说明）、`NOTICE`、`LICENSE`（删上游版权行）、
    `Assets/README.md`（图标方案回到未定）、`plan.md`、`AGENT.md`。
  - 核查结论：清理时工程内 `.cs` 仅剩 `DebugPlusMod.cs` / `STRINGS.cs` / `Properties/AssemblyInfo.cs`；
    本地 `bin` 与已部署 DLL 扫描 `SandboxTools/FilteredDestroyTool/DestroyFilter/DestroyParameterMenu/
    PeterHan/PLib/PUtil/SpriteRegistry` **全 0 命中**；`obj` 构建缓存 0 残留引用。
- ✅ **git 仓库已建立并推送成功**（2026-09-13，用户拍板：Public + 仓库名 `DebugPlus`）：
  - 本地仓库在工程根 `F:\ONI_ModDev\ONI_ModCode\Debug Plus`（**仓库根即工程根**，非 `DebugPlus/` 子目录）；
    远程 `origin` = `https://github.com/w00kab/DebugPlus.git`，分支 `main`（已设 upstream）。
  - 基线提交：`4aaf9e4`（去上游化后的干净基线）→ `ee138c3`（新增 `README.md`）→ `9bf8108`（git 状态文档同步）。
  - 自验：远程 `refs/heads/main` 提交号与本地一致；`raw.githubusercontent.com` 可取回 `README.md`/`.gitignore`；
    `bin/` 与 `*.dll` 经 `git check-ignore` 确认被忽略。
  - ⚠️ **批 2a 的代码已提交**：`2a29d44`（`feat(M1): 批 2a 实体配置弹窗外壳`，8 文件 / +534 −46）。
- 已部署：`%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\Debug Plus\`
  （体积演进：批 1 = 4608 B → 批 2a = 11776 B → 批 2b = 16896 B → 批 3 = 27136 B → 批 3-2 = 33792 B
  → 批 3-2.5 = 36352 B → **批 3-3 = 47104 B**）。
  **当前部署件**：`DebugPlus.dll` = 47104 B，SHA256 `6CA996440EA8F7A2…`（2026-09-13 14:21:53），
  本地 `bin/Release` 与部署目录 SHA256 一致。
  ⚠️ **部署纪律**：游戏在跑时 DLL 被锁 ⇒ 部署会失败（编译仍成功，`bin/Release` 已是新版）；
  DLL **只在游戏启动时加载** ⇒ 部署完必须**重开游戏**才生效。改完要部署前先确认游戏已退出。
- ✅ **批 3-2 · 数值框 + 输入框 + 编辑期吞键：已实现、编译、部署完成** → **⏸ 待实机验证**
  （实机入口由批 3-2.5 的临时自检区提供，见下）。
- ✅ **批 3-2.5 · 临时控件自检区：已完成（编译 + 部署通过）** → **⏸ 待实机验证**（见 §4 第 13–18 条）：
  在 `ConfigPanel` **底部**加了一块明写"临时"的「控件自检」区（分区标题 / 数值框 / 输入框 / 勾选 / 状态行），
  让批 3-2 的三个构件在实机上**可见、可点、可被证伪**。用户 2026-09-13 拍板方案 A ——
  **不新开测试面板、不把入口挂复制人**：植物那条「修改配置」入口本就通了（用户 13:35 已确认滑条出现），
  而复制人身上没有 `IManageGrowingStates`、今天**没有**这个按钮，为临时内容新开一条永久入口是负收益。
  ⚠️ 批 3-3 起这块区**不再是临时物**（用户拍板"留着，当做后续界面使用的典范"）：改用真分派渲染，
  并加了滑条行与下拉行，见下条。
- ✅ **批 3-3 · 面板浮层挂载点与参数类型分派：已完成（编译 + 部署通过）** → **⏸ 待实机验证**（见 §4 第 19–27 条）：
  B7 面板浮层挂载点（下拉列表的挂载根，绕过窗口矩形裁剪）+ B8 参数类型家族与 `ParameterRowFactory` 分派
  （数值→滑条/数值框、布尔→勾选、选项→下拉）+ 新构件 `DropdownField`（自建 header + 自绘浮层列表）
  + 自检区升级为「控件自检 · 用法范例」（六行四控件全部走真分派）。详见 §1.5 批 3-3。
- 批 3-2 的代码已提交：`d1c8d13`（`feat(M1): 批 3-2 数值框、输入框与编辑期吞键`，3 文件 / +804）。
- 🔧 **批 3 · 滑条排障（2026-09-13 下午，用户实机逐步定位）** —— 两个真实根因都是**几何**，不是颜色：
  ① `bar` 列漏给高度 ⇒ 整条滑条 0 像素（看不见，但**能拖**，因为拖动命中的是滑条根物体）；
  ② 填充条水平内缩比槽底大 8px ⇒ **没被填充盖住的槽底直接显示**（绿色旁露底色）。
  我在 ① 之前误诊为颜色问题、连改三版颜色，均被用户否；**教训已写进 `SliderField.cs` 注释**。
  用户 13:35 已确认"滑条出现了"；那 5 个调试提交已**压成 1 个** `364f3f3`（用户要求）。
  留待用户复验：绿条两侧是否还露槽底色。
- 批 3 的提交序列（本地，**均未推送**）：
  `f2d3085` 批 3-1 四个新组件 → `4004e43` 色板纯角色化 + 绑定收进组件 → `14d7878` 布局收进 UIFactory
  → `93640ac` 滑条尺寸/几何 → `46a830e` 滑条栏改布局组驱动 → `364f3f3` 滑条不可见与盖不住槽底修复。
- ✅ **批 2a · M1 最小链外壳：已实现、编译、部署完成** → **⏸ 待用户实机验证**（见 §4 第 1–4 条）。
- ✅ **批 2b（A4–A6 + A7b）：已实现、编译、部署完成** → **⏸ 待用户实机验证**（见 §4 第 5–7 条）。
  **2b 的代码已提交**：`cd09869`（含 `UI/View`+`UI/Component` 拆分、`Ops`→`Operations`、去 `Dp` 前缀改名）。
- 🔧 **批 2b 实机首次运行即崩，已定位并修复（2026-09-13 02:44 重新部署，待复验）**：
  `NullReferenceException` 在 `ParameterRowFactory.CreateSlider` → `ConfigPanel.BuildParamRows`
  （点「修改配置」按钮时）。**定位手段**：`player.log` 栈带 IL 偏移 `[0x0010a]` → `ildasm` 反汇编本 Mod DLL →
  搜到 `IL_010a` 是 `ldloc.s handleRect` + `callvirt RectTransform::set_anchorMin` ⇒
  滑块的 `handleRect` 是 `null`。**根因**：`NewUIObject` 已挂过 `RectTransform`，
  建滑块时又 `handle.AddComponent<RectTransform>()` 一次 ⇒ **Unity 返回 `null`（不抛异常）**。
  修复：改为 `handle.GetComponent<RectTransform>()`（见 §3 教训 8/9）。
  同一批还做了两件事：**`SliderField` 组件抽出**（A4 重构，用户拍板 A 案）与**滑条尺寸放大**
  （滑条高 22→**44**、滑块宽 14→**28**、行高 40→**56**、读数宽 56→**64**，用户反馈"太小了，×2 差不多"）。

## 1.5 批 3 施工单：UI 构件化（进行中）

> 用户 2026-09-13 拍板的总方向：**颜色全部接入 UIColors；控件逻辑尽量内聚到组件类里**
> （添加 / 设置样式 / 绑定变量都做成组件自己的能力），可暴露可覆写的接口，但内部必须有默认实现。
> 颜色纪律：**每种用处只有一种颜色**，且**不许出现"具体用处"的颜色名**（角色名才行，用户明令）。

### 批 3-1 · 构件工厂 + 色板 + 首批控件 —— ✅ 已完成并提交

- 新增 `UI/Component/`：`UIColors.cs`（唯一色板）、`UISpriteFactory.cs`（程序化圆角/圆形/白块 sprite）、
  `UIFactory.cs`（**唯一**允许碰布局组字段与锚点的文件）、`SliderField.cs`、`ToggleField.cs`、
  `ParameterRow.cs`、`ParameterRowFactory.cs`。
- 色板收敛为**纯角色命名**（用户口径）：语义色 Primary/Success/Warning/Danger/Info，
  文字色 PrimaryText/RegularText/SecondaryText/Placeholder，
  边框色 BorderBase/BorderLight/BorderLighter/BorderExtralight，
  背景色 BackgroundWhite/BackgroundW/BackgroundB/Background/BackgroundD/BackgroundDeep，加 Transparent。
  **绝不新增"某控件某部位"式命名**（加色需用户批准）。
- 滑条栏改为**布局组驱动**：读数从"绝对锚点压右端"改为"布局分配的一列"；
  **高度一律定值（LayoutElement）**，宽度交给布局组（弹性列 `flexibleWidth`）。
- 组件约定：静态 `Create(parent, style?)` + 内嵌 `Style` 类（含 `DefaultStyle`）
  + `Bind(read, write, …)` 把调用顺序（范围/初值 → 格式化 → 挂监听）封在组件内部。

### 批 3-2 · 数值输入与按键拦截 —— ✅ 已完成（编译+部署通过，⏸ 待实机验证）

- **B4 `UI/Component/NumberField.cs`（已实现）**：数值框 = 一栏 HLG：
  `◄ 步进键` / `TextField`（弹性宽）/ `► 步进键` / `单位后缀`（无单位时整体隐藏，布局自动跳过）。
  **数值显示与编辑合用一个 `<TextField>`**（点它即聚焦编辑，退出编辑回到只读显示）——
  这样一栏里只有一个文本控件，避免"读数与编辑器两个节点抢同一列"而被迫 HLG→HLG 嵌套（oni-ui 规则 5）。
  - 与 `SliderField` 同款约定：静态 `Create(parent, style)` + 内嵌 `Style`(+`DefaultStyle`) +
    `Bind(read, write, format, min, max, wholeNumbers, step, unit, initial)`；
    顺序契约 = 先 `SetRange`（范围+初值，不写游戏）→ `SetFormatter` → `SetUnit` → 最后接上写回通道。
  - **步进档位（用户 2026-09-13 拍板：默认推导 + 可覆盖）**：`Style.Step > 0` 用它；
    否则 `DeriveStep(min, max, wholeNumbers)` 按范围推导 —— 让约 100 次点击走完全程，
    档位取整齐的 1/2/5×10ⁿ（[0,100]→1、[0,1000]→10、[0,5000]→50、[0,1]→0.01；整数刻度至少 1）。
  - 提交语义：回车提交；**Esc 取消**（TMP 会先把文本还原成原文 ⇒ 用 `TextField.WasCanceled` 判定，
    不写回）与**解析失败**都静默回退到当前值，绝不把半截输入写进游戏。
  - `format` 只负责数值本身的显示形式，**不在里面拼单位**（单位是独立一列，编辑时必须是纯数字）。
  - 步进键文字默认用 ASCII 的 `"<"` / `">"`：ONI 的 SDF 字体不含部分 Unicode 装饰符号
    （§3 与 oni-ui 规则 6 的"装饰符号空白"），行有余量时可改 `Style.DecreaseLabel/IncreaseLabel` 为 `◄/►` 实机看效果。
- **B5 `UI/Component/TextField.cs`（已实现）**：内部组合**原生** `TMP_InputField`（**不派生** `KInputTextField`），
  结构 `root(不透明可交互底 Image)` → `textArea(RectMask2D；= textViewport)` → `text(独占 GO；= textComponent)`。
  - 三条依据（逐行核对本体代码，见文件头注释）：`textViewport`/`textComponent`/`placeholder` 是可写公开属性；
    **光标完全交给 TMP**（`OnEnable` :1248-1263 自建 Caret、`LateUpdate` :1612-1679 每帧 `AssignPositioningIfNeeded` 同步）；
    `onEndEdit` 只由 `ReleaseSelection()` 发出，而 `DeactivateInputField` 在 `resetOnDeActivation` 为真时必调它 ⇒
    回车与失焦都会走到（`resetOnDeActivation` 已在代码里显式置 true）。
    ⇒ 唯一要守的是"**OnEnable 跑之前 textComponent 已就位**"：先 `SetActive(false)` → 挂引用 → 再激活。
  - 不派生 `KInputTextField` 的理由：它只多"值变化延迟通知 / 手柄输入"，且其无参构造在反编译里是 **private**。
  - 静态 `TextField.IsEditing`（编辑计数，`OnDisable/OnDestroy` 必归零）是吞键补丁唯一认的开关。
- **B6 `Patches/InputHandler_HandleEvent_Patch.cs`（已实现）**：`[HarmonyPatch(typeof(KInputHandler), nameof(KInputHandler.HandleEvent))]`
  的 Prefix —— `TextField.IsEditing` 为真时 `e.Consumed = true; return false;`。
  - 目标选择依据：`KInputController.Dispatch()`（:242-256）是全工程**唯一**调 `HandleEvent` 的地方 ⇒ 它是热键总闸门；
    `KInputEvent.Consumed` 是公开可写属性（:19）。**已对游戏真源 `Assembly-CSharp-firstpass.dll` 反射复核签名成立**。
  - **口径收窄（用户 2026-09-13 拍板）**：只在"**正在编辑**"时吞，**不按"面板展开"吞** ——
    Esc 关面板靠 `KScreenManager.OnKeyDown` 派发到 `KModalScreen.OnKeyDown`，它同样在这个根 handler 之下，
    面板一展开就吞键会把 Esc 一起吞掉、打断已验收行为。收窄后是**两级 Esc**：编辑中 Esc = 退出编辑，再按才关面板。
  - 吞键不妨碍打字：输入框收字是 TMP 直接读 Unity 输入（`Event.PopEvent`），不走 KInput 链。
  - 文件/类名按命名规范跟随真实补丁目标（施工单原写 `InputHandler_HandleKeyDown_Patch`，实际目标是 `HandleEvent`）。

- 参考事实（勿重复踩）：`KNumberInputField` **不可纯代码构造**（`KInputField.inputField` 是
  `[SerializeField] private KInputTextField`，`field` 只读）⇒ 数值控件只能自建；
  `plan.md` §3.1 那句"不使用数字输入框"**指的是不用原版 `KNumberInputField`**，
  **不排斥自建 `NumberField`**，两者不矛盾。
- 本批补记的原版事实（都写进代码注释了）：原版"输入框聚焦就不处理热键"只有
  `CameraController.WithinInputField()`（CameraController.cs:527-540）这一处，且**只保护 CameraController 自己**
  （:572 / :801），其余热键消费者（SpeedControlScreen / ToolMenu / PlanScreen / OverlayMenu）都没有这层保护。

### 批 3-2.5 · 临时控件自检区（实机入口）—— ✅ 已完成（编译 + 部署通过，⏸ 待实机验证）

> **为什么会有这一批**：批 3-2 只交付构件，而 `ConfigPanel` 里没有任何行用它 ⇒ 实机上看不见、点不到、
> bug 也无从发现；但批 3-3 的参数类型分派（B8）尚未设计成型，此刻硬做出来等于把未定型的形状锁死。
> 折中：用一块**明确标注"临时"**的自检区走最短路径，把三个构件摆到台面上，先验证"控件本身是否成立"。

- 改动面（4 文件）：
  - `UI/View/ConfigPanel.cs`：`SelfTest` 开关 + `BuildSelfTestRows()` + `AddSelfTestRow()` +
    `CreateSelfTestRow()` + `SelfTestStatusText()` + `Update()` 轮询 + 三个 `OnSelfTest*` 回调；
    `SetTarget` 里多一行调用；`ComputeHeight` 多一个"自检行数"参数（窗口高度按行数重算）。
    顺手把"把行插到按钮行之前"抽成 `InsertBeforeButtonRow`，参数行与自检行共用同一条规则。
  - `UI/Component/TextField.cs`：新增只读静态 `EditingCount`（计数**个数**，不只 `IsEditing`）。
  - `UI/Component/ParameterRowFactory.cs`：`LabelWidth` / `LabelFontSize` 由 `private const` 提为
    **`public const`**（纯可见性；自检行复用同一套标签尺寸，不复制常量）。
  - `STRINGS.cs`：新增 `PANEL_SELFTEST_*` 8 条。
- 自检区五行（全部只读写**本面板自己的三个字段**，不碰游戏对象、不写存档、不碰时间）：
  | 行 | 控件 | 看什么 |
  |---|---|---|
  | 分区标题 | — | 明写"临时 · 批 3-3 挂载后删" |
  | 数值框 | `NumberField` | 范围 0–100 整数 + 单位 % ⇒ `DeriveStep` 应推成 **1**；`<` `>` 是不是空白方块 |
  | 输入框 | `TextField` | 初值「点我输入中文abc」；回车/失焦提交、Esc 取消只打日志 |
  | 勾选 | `ToggleField` | 三种控件同屏看高度是否齐 |
  | 状态行 | TMP + `Update` 轮询 | **编辑中 ｜ 计数 N ｜ 数值 N** —— B6 吞键的现场读数 |
- **状态行的两处互证设计**（不是装饰）：
  ① 「计数」直接暴露 `TextField.EditingCount` —— 它是**会漏**的量（组件销毁时机不由我们控制），
     漏一次即永久卡在"编辑中"、整局键盘失灵，必须当场可见而不是打完日志翻文件；
  ② 「数值」显示的是**模型侧**回读（`OnSelfTestNumberChanged` 写进来的），与数值框自己显示的数字
     互为印证，两者不一致即"写回通道没接通"。
- 关键实现事实（已核对本体源码）：`KScreen` 只有 `ScreenUpdate(bool)`、`KModalScreen` 与 `KMonoBehaviour`
  **都没有** `Update()` ⇒ 在 `ConfigPanel` 里写 `private void Update()` 不会顶掉基类回调。
- 自检区开关用 `static readonly bool` 而非 `const`：`if (const 常量)` 会触发 CS0162 不可达代码警告。
- 编译 + 部署已通过（36352 B，SHA256 `D127680A2A0FA84A…`，与本地 `bin/Release` 一致）；
  部署件扫描：`EditingCount` / `BuildSelfTestRows` / `SelfTestStatusText` / `PANEL_SELFTEST_STATUS` 全命中，
  `KInputTextField` 与全部上游标识符 0 命中。
- ⚠️ **删除时机与清单（已被批 3-3 取代，留档）**：原计划"批 3-3 把参数类型分派挂上后整块删干净"；
  用户 2026-09-13 在批 3-3 开工前拍板**改为长期保留**（"留着，当做后续界面使用的典范"）⇒
  该删除清单**作废**，`ConfigPanel.cs` 顶部那段注释已改写为"用法范例区"的说明；
  `InsertBeforeButtonRow` 本来就不删（参数行与自检行共用）。

### 批 3-3 · 面板挂载与参数类型分派 —— ✅ 已完成（编译 + 部署通过，⏸ 待实机验证）

> 用户 2026-09-13 拍板四点：①「自检区**留着**！当做后续界面使用的典范」；
> ②下拉走**自建 header + 自绘浮层列表**；③数值参数的显示形态**由参数自己声明**；
> ④下拉关闭语义 = **外部点击拦截 + 两级 Esc**。

- **B7 浮层挂载点（`ConfigPanel.OverlayLayer`）**：面板根下与 `window` **平级**的一层，
  铺满面板根、**不挂任何布局组**、`SetAsLastSibling` 常驻最上层。存在的唯一理由：
  下拉列表**必须盖在面板之上、又不许参与行布局** —— 挂在 `window` 里会被窗口矩形裁掉、被根 VLG 接管尺寸。
  这与同作者另一 Mod（Applied Logistics Network）成熟实现的 `Build(parent, dropdownRoot)` 口径一致：
  "面板顶层做列表挂载根，绕过裁剪"。
- **B8 参数类型分派（`ParameterRowFactory`）**：`Parameter` 由"只有浮点"拆成家族 ——
  | 参数类型 | 控件 | 说明 |
  |---|---|---|
  | `NumericParameter`（`Control = Slider`） | `SliderField` | 默认形态；生长进度**观感一字未改** |
  | `NumericParameter`（`Control = Number`） | `NumberField` | 精确输入 + 步进键 + 独立单位列 |
  | `ToggleParameter` | `ToggleField` | 布尔 |
  | `ChoiceParameter` | `DropdownField`（新建） | 读写下标，选项文本由参数提供 |
  | 其它（未接分派） | 一行"暂不支持该参数类型：Xxx"占位 + 警告日志 | **刻意兜底**：漏接分派时是一行明确提示，不是空白行或整块面板挂掉 |
  `Control` 由**参数自己声明**（用户拍板）：规则显式、可预测，不搞"按范围猜用的是哪种控件"。
- **`DropdownField`（新构件，约 500 行带注释）**：header（当前值 + ▼ 箭头 + 可点）
  + 浮层列表（点击承接层 / `RectMask2D` 视口 / VLG + `ContentSizeFitter` / `ScrollRect`）。
  定位照 oni-ui 五步法：世界坐标 → 浮层本地坐标 + 下方放不下翻到上方 + 横向夹回屏内；
  **全程不建 `Canvas`/`GraphicRaycaster`**（IL2CPP 下 `AddComponent<Canvas>()` 返回 null）。
  点外关闭 = 铺满的**点击承接层**，顺序必须"先承接层、后列表"（照 ALN `Open` 的两次 `SetAsLastSibling`）。
- **两级 Esc**：`ConfigPanel.OnKeyDown` 覆写 —— 有下拉展开且按的确实是 Esc 时，先收起列表并消费这一下；
  否则原样交给 base（`KModalScreen.OnKeyDown` 关面板）。与输入框编辑期的两级 Esc 口径一致。
  为什么不像 ALN 那样在 `KInputHandler.HandleKeyDown` 上做全局 Prefix + 静态登记表：
  本 Mod 的面板是**唯一**的模态屏，Esc 只会走到这一条路，不必新增补丁、不必引入会泄漏的静态状态
  （`KModalScreen.OnKeyUp` 只做 `Consumed = true`，也不存在"Up 阶段再关一次"的问题）。
- **自检区升级为「控件自检 · 用法范例」（用户拍板长期保留）**：原来是手搭的 5 行，
  现在**滑条 / 数值框 / 勾选 / 下拉四行都走真的 `ParameterRowFactory` 分派**（值只落在面板自己的字段上），
  所以它同时是"控件怎么用"的活范例 + 新控件的现场验证台；输入框那一行仍手搭
  （纯文本今天不是参数类型：参数只管"会被写回某处的值"），注释里写明了原因。
  六行内容：分区标题 / 滑条（0–1 两位小数）/ 数值框（0–100 整数 %）/ 输入框 / 勾选 / 下拉（6 选项）/ 状态行。
- 改动面（8 文件）：`Operations/Parameter.cs`（拆家族 + `Format`/`Display` 分离）、
  `Operations/GrowthOperation.cs`（改基类、改覆写 `Format`）、
  `UI/Component/DropdownField.cs`（**新建**）、`UI/View/SelfCheckParameters.cs`（**新建**）、
  `UI/Component/ParameterRowFactory.cs`（分派）、`UI/Component/ParameterRow.cs`（变薄为"参数 + Refresh"）、
  `UI/Component/ToggleField.cs`（补 `read`/`Refresh`，与另两个控件同形）、
  `UI/View/ConfigPanel.cs`（浮层挂载点 + 自检区 + 两级 Esc）、
  另 `UIFactory.AnchorTopLeft` / `UIColors.ClickCatcher` / `ToggleField.Refresh` / `STRINGS` 若干键。
- 🔴 **编译期自己抓到的坑（已修，写进注释）**：数值框的单位是**独立一列**，若把带单位的
  `Display` 传给 `NumberField.Bind(format:)`，框里会显示"50%"、旁边单位列再写一个 "%" ⇒ 拼两遍。
  修法：`NumericParameter` 拆成 `Format`（**不含单位**，子类覆写它）+ `Display`（= Format + Unit，
  给滑条读数用）。生长进度因此改为覆写 `Format`，滑条读数仍是「50%」，观感不变。
- 自验：编译 + 部署通过（**47104 B**，SHA256 `6CA996440EA8F7A2…` @ 2026-09-13 14:21:53，与本地 `bin/Release` 一致）；
  部署件扫描：`DropdownField` / `NumericParameter` / `ToggleParameter` / `ChoiceParameter` /
  `SelfCheckNumericParameter` / `ClickCatcher` / `AnchorTopLeft` / `PANEL_SELFCHECK_STATUS` 全命中；
  `KInputTextField` / `KNumberInputField` / 全部上游标识符 **0 命中**，
  且 `ALN_` / `Applied_Logistics` **0 命中**（参考了结构，没有搬一行代码）。
- 构建过程记录（一次部署失败，非代码问题）：14:19:35 部署的中间版（SHA `2856293A…`）**含上面那条
  "单位拼两遍"的 bug**；用户随后启动游戏（DLL 被锁）⇒ 修正版编译成功但部署被拒；用户关掉游戏后
  14:21:53 重新部署成功。**"游戏在跑时部署会失败、且 DLL 只在启动时加载"这两条要记住**：
  改完要部署时先确认游戏已退出，部署完必须让用户**重开**游戏才生效。
- ⚠️ **未验证项（如实记录）**：列表滚动（`ScrollRect` + `MaxVisibleItems = 8`）在当前自检区
  **不会被触发**（只有 6 个选项）⇒ 滚动属于"搭好了但没验过"；箭头用 `▼ / ▲`
  （依据是 ALN 的成熟用例，若实机出现空白方块，把 `Style` 里两个值换成 ASCII `v` / `^` 即可）。

### 批 3 已踩的坑（写进代码注释了，此处留索引）

- **布局列必须显式给高度**：本栏 HLG 用默认 `childForceExpandHeight = false`，
  子项高度取各自 `preferredHeight`；`bar` 列只给 `flexibleWidth` ⇒ 高 0 ⇒
  **整条滑条一个像素都没有**（实机表现为"滑条看不见"，且右侧读数值正常，极具误导性）。
- **共享边界的两层内缩量必须一致**：填充条水平内缩（曾 6、又加 2 成 8）而槽底为 0 ⇒
  **没被填充盖住的槽底会直接显示出来**（绿色旁露槽底色）。现 `FillInset = 0`。
- **色板归属是用户的决定权**：我两次以"对比度不够"为由擅改槽底色，均被否。

## 2. 批 2 施工单：M1 最小链 + 植物生长进度

出工条件（plan.md §五 P1 出口）：**暂停状态下点植物 → 改配置 → 弹窗 → 拉动生长进度 → 当场变化**。

### 批 2a · 壳 —— ✅ 已完成（出口：游戏里能看见"按钮 + 弹窗"，能开能关）

| # | 文件（新建） | 内容 | 状态 |
|---|---|---|---|
| A1 | `Patches/UserMenu_AppendToScreen_Patch.cs` | `[HarmonyPatch(typeof(UserMenu), nameof(UserMenu.AppendToScreen), typeof(GameObject), typeof(UserMenuScreen))]` 的 **Prefix**：命中判定 → `ConfigButton.EnsureOn(go)`。**必须是 Prefix**：事件在方法体内触发，Postfix 时本次按钮已交给屏幕 | ✅ |
| A2 | `UI/Component/ConfigButton.cs` | 实体身上的 `KMonoBehaviour`：`Subscribe<ConfigButton>(493375141, 静态 IntraObjectHandler)` → `Game.Instance.userMenu.AddButton(gameObject, new ButtonInfo(图标, "修改配置", 点击, Action.NumActions, null, null, null, tooltip, true), 20f)`；`EnsureOn` = `GetComponent ?? AddComponent` + `InitializeComponent()`（幂等兜底，保证框架 `obj` 已赋值）+ `SubscribeOnce()`（`subscribed` 标志防重复订阅）；点击 → `ConfigPanel.OpenFor(gameObject)`；**不做任何序列化**（读档后由 A1 重新挂上，**不影响存档**） | ✅ |
| A3 | `UI/View/ConfigPanel.cs` | **自建**（不克隆预制体）`KModalScreen`：`new GameObject` + `AddComponent`（Awake → `OnPrefabInit` 建遮罩与内容区）→ `KScreenManager.AddExistingChild(ssOverlayCanvas, go)` → `Activate()`；内容区 = 标题 / 目标名 / 参数行位 / 关闭按钮；`pause = false`（时间中立）；已有实例只抬到最上层（不叠加） | ✅ |
| A7 | `STRINGS.cs` + `DebugPlusMod.cs` | 4 条独立中文 LocString（按钮 / tooltip / 面板标题 / 关闭 + 占位说明 + 无名兜底）；入口补 `Localization.RegisterForTranslation(typeof(STRINGS))`（本 Mod 自己的文本，与已删的上游那份无关） | ✅ |
| A8 | `plan.md` / `AGENT.md` / 本文件 | plan.md §3.1 新增**自建模态屏的框架依据**块 + 模态弹窗行改写 + §四 M1 落地细节；AGENT.md §5 增"框架程序集源码"行 + §8 进度；本文件重写 | ✅ |

**命中判定（批 2b 起）**：不再有任何临时判定，A1 的 Prefix 直接查 `Operations/OperationRegistry.IsConfigurable(go)`，
与面板的 `BuildParameters` 同源 —— **不在注册表里的实体连按钮都不出现**。批 2a 那句 `go.GetComponent<Growing>() != null` 已删除。

### 批 2b · 能力（出口：暂停下拖滑条，植物当场变）—— ✅ 已完成（出口待实机）

| # | 文件 | 内容 | 状态 |
|---|---|---|---|
| A4 | `UI/Component/SliderField.cs`（滑条+读数组件）、`UI/Component/ParameterRow.cs`、`UI/Component/ParameterRowFactory.cs` | 行绑定 + 纯代码建行（标签 TMP + `KSlider` + 读数 TMP）。**滑条来源定案：纯代码自建 `KSlider`**——原版所有滑条都来自预制体（`MultiSliderSideScreen.cs:34` `Util.KInstantiateUI(sliderPrefab…)`，`sliderPrefab` 是 `[SerializeField]`，mod 拿不到），原版没有"代码自建滑条"先例；而 `Slider.handleRect` / `fillRect` 是可写公开属性 ⇒ 可自建。⚠️ `KSlider.Awake()` 第一句取 `handleRect.gameObject`（`KSlider.cs:40`）⇒ **先不激活、挂完 handleRect 再激活**。**不用 `KNumberInputField`**：`KInputField.inputField` 是 `[SerializeField] private`、`field` 只读（`KInputField.cs:10-16/105-106`）⇒ 数值改用 TMP 读数。顺序：先设范围/初值 → 设格式化 → **最后** `AttachListener()`（`Slider.Set(float,bool)` 是 **protected**，mod 用不了，只能走公开的 `value` 属性 + 延后挂监听） | ✅ |
| A5 | `Operations/OperationRegistry.cs` | `IOperation` 接口 + 登记表（静态构造里登记 `GrowthOperation`）；`IsConfigurable` / `BuildParameters` **同源**；A1 的临时判定（`GetComponent<Growing>()`）已删除，改查注册表 | ✅ |
| A6 | `Operations/GrowthOperation.cs` | 取组件照原版 `PlantBranchGrower.cs:402-403`：`GetComponent<IManageGrowingStates>()` 优先、`GetSMI<IManageGrowingStates>()` 兜底（⇒ 植物与树枝类都覆盖，不硬编码 `Growing`）；读 `PercentGrown()×100`、写 `OverrideMaturityLevel(v/100)` —— ⚠️ **写入口收 0–1 比例**（`Growing.cs:54-58`）。**纯写值、零时间依赖** | ✅ |
| A7b | `STRINGS.cs` | 新增 `PARAM_GROWTH`「生长进度」、`UNIT_PERCENT`「%」；`PANEL_PENDING` 改为 `PANEL_NO_PARAMS`「（该实体暂无可调参数）」（无参数行时才显示） | ✅ |
| A4b | `UI/View/ConfigPanel.cs` | 接参数行：`SetTarget` → `BuildParamRows`（逐行插在按钮行之前，都是根 VLG 直接子节点，不嵌套 VLG）→ 按行数**动态算窗口高度**；日志带行数 | ✅ |

**命名（用户 2026-09-13 定）**：目录 `UI/View`（屏/面板）+ `UI/Component`（控件/工厂）+ `Operations/`（操作与参数）；
类名**不加前缀**（靠命名空间 `DebugPlus.*` 区分）、**一律写全禁缩写**（`Operation` 不写 `Op`、`Parameter` 不写 `Param`）。
→ 已按此把 `DpOpRegistry`/`DpGrowthOp`/`DpParamRow`/`DpRowFactory`/`DpConfigButton`/`DpConfigPanel`/`DpParam` 全部改名，
并同步 plan.md §四 目录清单与命名规范表。

### 每步自验（agent 侧）

1. `& "C:\Users\魏锴\.agents\skills\oni-mod-dev\scripts\build.ps1" -ProjectRoot "F:\ONI_ModDev\ONI_ModCode\Debug Plus"`
   （部署目录在工作区外 → 首次必被沙箱拒绝 → 对**同一条命令**申请一次提权重试）
2. 扫描新 DLL：无上游标识符
3. 游戏内验证交用户（见 §4）

## 2. 已实测的来源依据（勿再凭记忆）

### 2.1 用户菜单按钮链（`Assembly-CSharp`，源码逐行）

- `GameHashes.RefreshUserMenu = 493375141`（`GameHashes.cs:113`）。
- `UserMenu.AppendToScreen(GameObject go, UserMenuScreen screen)`（`UserMenu.cs:37`）：
  `buttons.Clear()` → sliders.Clear() → **`go.Trigger(493375141, null)`** → 按 `sort_order` 升序排序 →
  `screen.AddButtons(...)`。
  ⚠️ **该事件是在"被选中实体自己的 GameObject"上触发的，不是全局事件** → 订阅者必须是**实体身上的组件**
  （原版 `Clearable` / `HarvestDesignatable` / `BuildingEnabledButton` 等 60+ 组件同款写法）。
  ⚠️ 因事件在方法体内触发，**挂载必须用 Prefix**：Postfix 时本次按钮已交给屏幕，要等下次刷新才出现。
- `UserMenu.AddButton(GameObject go, KIconButtonMenu.ButtonInfo button, float sort_order = 1f)`（`UserMenu.cs:16`）：
  内部会把 `button.onClick` 包一层"回调 + `Game.Instance.Trigger(1980521255, go)`"，**所以点击后菜单自己重建**，
  无需手动刷新。
- 刷新入口链：`Game.Instance.userMenu.Refresh(go)` = `Game.Instance.Trigger(1980521255, go)`（`UserMenu.cs:10`）
  → `UserMenuScreen.OnUIRefresh` → `UserMenuScreen.Refresh(go)`（`UserMenuScreen.cs:75`，**带 `go == selected` 守卫**）→ `AppendToScreen`。
- `UserMenuScreen` 的 `buttonInfos` / `slidersInfos` / `sliders` / `selected` 均**私有** → 不要试图从外部灌按钮，
  走"实体自带组件 + `AddButton`"这条原版正道。
- 原版订阅写法样板：`Clearable.cs:12/17`（`OnPrefabInit` 里 `base.Subscribe<T>(int 字面量, 静态 IntraObjectHandler<T>)`）
  + `:133`（handler 里 `AddButton`）+ `:222`（静态委托转发）。
- `KIconButtonMenu.ButtonInfo` 构造（`KIconButtonMenu.cs:354`）：
  `(string iconName, string text, System.Action on_click, Action shortcutKey, Action<GameObject> on_refresh, Action<ButtonInfo> on_create, Texture texture, string tooltipText, bool is_interactable)`。
- `KSelectableExtensions.GetProperName(this GameObject)`（`KSelectableExtensions.cs:18`）：**没有 `KSelectable` 时返回空串**，需自行兜底 `go.name`。

### 2.2 Klei UI 框架（`Assembly-CSharp-firstpass`，2026-09-13 用户反编译后逐行核对）

- **运行时组件生命周期**：`KMonoBehaviour.Awake()`（`:35`）→ `InitializeComponent()`（`:45`，**public + `isInitialized` 守卫，可重复调用**）→ `OnPrefabInit()`（`:63`）；`Start()`（`:129`）→ `Spawn()`（`:141`，`isSpawned` 守卫）→ `OnSpawn()`（`:158`）。
  ⇒ **运行时 `AddComponent` 就完成框架初始化，不需要预制体**；`Subscribe` 依赖的 `obj` 在 `InitializeComponent` 内赋值（`:56` / `:253-255`）。
- `KScreen.Activate()`（`KScreen.cs:276-282`）：`SetActive(true)` → `KScreenManager.Instance.PushScreen(this)` → `OnActivate()` → `isActive = true`（**自足、无需预制体**）。
- `KScreen.Deactivate()`（`:290-303`）：`OnDeactivate()` → `PopScreen` → **`Destroy(gameObject)`** ⇒ 关闭即销毁，**不能复用实例**，只能"已有实例就不叠第二个"。
- `KScreenManager.AddExistingChild(parent, go)`（`:253-261`）= `SetParent(…, false)` + 同步 `layer`；`PushScreen`/`PopScreen` 后按 `GetSortKey()` 重排栈。
- `KModalScreen.OnPrefabInit()`（`KModalScreen.cs:9-30`）自建全屏半透明遮罩（`Color32(0,0,0,160)`、`raycastTarget = true`）并置 `ConsumeMouseScroll`/`activateOnSpawn`；`OnCmpEnable`/`OnCmpDisable` 管 `CameraController.DisableUserCameraControl`（`:44-72`）。
- 按键与模态：`KScreenManager.OnKeyDown`（`:179-199`）自栈顶向下派发至 `e.Consumed`；`KModalScreen.OnKeyDown`（`KModalScreen.cs:123-139`）消费 `Action.Escape` / 右键 → `Deactivate()`；`KScreenManager.Update()`（`:161-176`）遇到 `IsModal()` 的屏后停止向下层传播 `ScreenUpdate`。
- `GameScreenManager.Instance.ssOverlayCanvas` 是 **public GameObject**（`GameScreenManager.cs:150`），游戏内 UI 的父节点。
- ⚠️ **`KModalScreen.pause` 默认 `true`**（`KModalScreen.cs:152`）：打开会 `SpeedControlScreen.Pause(false, false)`、关闭会 `Unpause(false)` → **会改掉玩家自己按下的暂停状态，与时间中立铁律冲突 ⇒ 本 Mod 显式设 `false`**。
- ⚠️ **`Action` 名称冲突**：缺氧自带全局枚举 `Action`（`Action.Escape`/`Action.NumActions`），**命名空间成员优先于 `using System;` 导入** ⇒ 本 Mod 一律写 `System.Action`（原版源码通篇如此，即此原因）。

### 2.3 滑条素材与生长状态（`Assembly-CSharp` / `firstpass`，2026-09-13 批 2b 核对）

- **原版所有滑条行都来自预制体** ⇒ mod 只能自建：
  `MultiSliderSideScreen.cs:34` `Util.KInstantiateUI(this.sliderPrefab.gameObject, …)`、`:37` `component.GetReference<KSlider>("Slider")`；
  `sliderPrefab` 是屏预制体上的 `[SerializeField]` 引用，**mod 拿不到**；原版源码里**没有**"代码自建滑条"的先例。
- **`KSlider` 可纯代码构造**：`KSlider : Slider`（`KSlider.cs:8`），需要的 `handleRect` / `fillRect` 是 `Slider` 的**可写公开属性**。
  ⚠️ 但 `KSlider.Awake()`（`KSlider.cs:31-41`）第一句 `base.handleRect.gameObject.GetComponent<ToolTip>()`
  ⇒ **handleRect 为空必 NRE** ⇒ 先让滑条 GameObject **不激活**、挂完 `handleRect`/`fillRect` 再激活。
  另：`onDrag` / `onReleaseHandle` / `onPointerDown` / `onMove` 是 KSlider 自己的事件（`SliderSet.SetupSlider` 用的就是它们）；
  本 Mod 只需连续写值，故用基类 `Slider.onValueChanged`。
  ⚠️ **`Slider.Set(float, bool)` 是 `family`（protected）**（`UnityEngine.UI.dll` IL 实测）⇒ mod 调不到（CS0122）；
  程序化设值只能走公开的 `value` 属性，并且**把 `onValueChanged` 的挂载推到设完初值之后**（否则初值会外泄成"用户操作"）。
- **`KNumberInputField` 不可纯代码构造**：`KInputField.inputField` 为 `[SerializeField] private KInputTextField`，
  `field` 属性只读（`KInputField.cs:10-16 / 105-106`）⇒ 数值改用自建 TMP 读数标签。
- **原版"滑条+数值+标签"样板**：`SliderSet.SetupSlider`（`SliderSet.cs:9-33`）、`SetTarget`（`:36-68`）、`SetValue`（`:96-121`）。
- **生长状态的原版取法**（`PlantBranchGrower.cs:402-403`）：`GetComponent<IManageGrowingStates>()` 优先、
  `gameObject.GetSMI<IManageGrowingStates>()` 兜底（树枝类是 SMI 实现）。
  接口定义在 `IManageGrowingStates.cs:10/16`；`Growing` 实现它（`Growing.cs:9`）。
  读 `PercentGrown()`（`Growing.cs:121-124` = `maturity.value / GetMax()`）；
  写 `OverrideMaturityLevel(percent)`（`Growing.cs:54-58` = `maturity.SetValue(GetMax() * percent)`）⇒ **收 0–1 比例**。
  原版自己也把 `PercentGrown()` 乘 100 显示（`CreatureStatusItems.cs:240/253`）。

## 3. 必须留存的教训

1. **不可把游戏 `STRINGS` 的 LocString 字段直接赋给本 Mod 字段**：`LocString` 是引用类型（class），赋值后
   共享同一对象，`Localization.RegisterForTranslation` → `LocString.CreateLocStringKeys` 会对树内每个 LocString
   调 `SetKey` 改写键，从而**污染游戏自身的字符串键**（表现为部分翻译丢失）。一律使用**独立的中文默认值字符串**。
2. 日志一律 `UnityEngine.Debug.Log`，前缀 `[DebugPlus]`；不引入任何日志框架。
3. 反编译产物可能含迭代器状态机残留（无法编译），只能对照阅读、**绝不内嵌**。
4. **命名冲突**：缺氧有全局枚举 `Action`（以及高频名 `Debug` 等），写库类型时优先全限定（`System.Action`），别只靠 `using`。
5. **反编译产物已扩到框架程序集**（`缺氧本体代码\Assembly-CSharp-firstpass`）：凡涉及 UI / 屏幕 / 组件生命周期的施工，**先读它再写**，不要再凭记忆判断（它直接决定"能不能自建"这类问题）。
6. **"能不能纯代码构造"必须逐个查字段所有权**：`Button` 能用而 `KButton` 不能（`soundPlayer` 是 `[SerializeField]`）、
   `KSlider` 能用而 `KNumberInputField` 不能（`inputField` 是 `[SerializeField] private` 且只读）——
   判据是**它依赖的引用是不是可以运行时赋值的公开成员**，不是类名像不像 UI 组件。
7. **Unity 组件的 Awake 陷阱**：往**已激活**的 GameObject 上 `AddComponent` 会立刻跑 Awake，
   若该 Awake 要读尚未赋值的 `[SerializeField]` 式引用就会 NRE ⇒ 需要时"**先 SetActive(false) → 挂好引用 → 再激活**"（本 Mod 建 `KSlider` 用的就是这招）。
8. 🔴 **`AddComponent<RectTransform>()` 在已有 RectTransform 的 GameObject 上返回 `null`（不抛异常！）**：
   2026-09-13 实机批 2b 崩溃的直接原因 —— `NewUIObject` 已挂过 RectTransform，建滑块时又 `AddComponent` 一次拿到 `null`，
   下一句 `handleRect.anchorMin = …` 才 NRE。**凡是"有没有 RectTransform"不确定的 GO，一律 `GetComponent<RectTransform>()`**。
   （ini-ui 规则 1 本来就写了这条，之前只在 ConfigPanel 里守住了，工厂里漏了 ⇒ 规则要在**每个新建 UI 的辅助方法**里落实。）
9. **定位 mod 自身 NRE 的正确姿势（本轮验证有效，以后照做）**：
   ① `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\player.log` 里的 IL2CPP/Mono 栈带 **IL 偏移**（如 `[0x0010a]`）；
   ② `ildasm /out=… /item:命名空间.类 本Mod的DebugPlus.dll` 反汇编自己（**不用任何第三方反编译器**）；
   ③ 在反汇编里搜 `IL_010a`，看它到底在调谁 —— 本次直接读到 `ldloc.s handleRect` + `callvirt RectTransform::set_anchorMin`
   ⇒ 一秒锁定 `handleRect` 为 null，免去"加日志 → 重编 → 再让用户复现"的多轮往返。
   注意：`Release`（`DebugType=pdbonly` + `optimize+`）的行号不可靠，**以 IL 偏移为准**。
10. **"能不能调"还要看访问修饰符，不只看字段所有权**：`Slider.Set(float, bool)` 在 IL 里是 `family`（protected）
   ⇒ mod 编译期直接 CS0122。同类陷阱：想用某"看起来是给代码用"的内部方法前，先 `ildasm` 看修饰符。
   本 Mod 的规避法：程序化设值走公开 `value` 属性 + **把监听器挂载推迟到设完初值之后**（`SliderField.AttachListener`）。

## 4. ⏸ 待用户实机验证（未验证前不得当成事实）

批 2a 专项（本轮就要看）：

1. 暂停状态下选中**植物**（刺花 / 小麦等），用户菜单里是否出现「修改配置」按钮；**图标是否正常**（不是空白方块）。
2. 点按钮 → 是否弹出居中面板；**面板外的点击是否被拦住**（不能操作下面界面）。
3. `Esc` 与面板「关闭」按钮是否都能关闭；**重复点按钮是否不会叠出第二个面板**。
4. 打开 / 关闭面板前后，**游戏速度与暂停状态是否原样不变**（`pause = false` 的验证点）。

批 2b 专项（本轮新增，接在 2a 之后一起看）：

5. 面板里是否出现**「生长进度」一行**（标签 + 滑条 + 右侧百分比读数），读数与植物当前进度是否对得上（可先看原版植物状态项里的成熟度百分比）。
6. **拖动滑块**：读数是否跟着变；**植物当场变化**（外观换图 / 成熟度状态项变化），**无需解暂停**。
7. 滑条**能拖、能点**（点滑条空白处是否直接跳值）；**面板外的点击仍被拦住**；关掉面板后植物状态保持你拖到的值。
8. **尺寸**：滑条高 44 / 滑块 28×28（用户 2026-09-13 反馈原 22 太小 ⇒ 已 ×2）；
   看滑块在**两端**是否越出滑条、右侧 `%` 读数在**满格**时会不会被滑块压住（读数宽 64、右端留 2px 间隙）。

plan.md §七 原有 4 条（留待后续批次）：

9. 暂停下 `Growing.OverrideMaturityLevel` 后，植物外观/状态项是否**立即**刷新（还是需一次解暂停才换图）—— 与第 6 条重叠，一并看。
10. 暂停下 `Geyser.AddModification` 后，间歇泉描述/喷发参数是否立即刷新。
11. `BabyMonitor.Instance.SpawnAdult()` 在沙盒/本 Mod 生成出的实体上调用是否安全。
12. 用户菜单按钮在**非建筑实体**（植物/动物/间歇泉）上是否正常显示。

批 3-2 / 3-2.5 / 3-3 专项（**本轮就要看** —— 入口：暂停下选中**植物** → 用户菜单「修改配置」 →
面板**底部**的「— 控件自检 · 用法范例 —」区；真正的参数行「生长进度」在它**上方**。
该区七行：分区标题 / 滑条 / 数值框 / 输入框 / 勾选 / 下拉 / 状态行）：

13. **自检区本身**：面板底部是否出现 5 行（分区标题 / 数值框 / 输入框 / 勾选 / 状态行）？
    数值框是不是 `[<][50][>][%]` 四段、**`<` `>` 不是空白方块**（用 ASCII 就是防这个）、
    四段是否都在框内没被裁掉？三种控件的高度看起来是否齐（自检行高统一 32）？
14. **数值框**：`<` / `>` 是否各走一格（范围 0–100 且整数刻度 ⇒ 档位应推导为 **1**，约 100 次点到满）；
    点数值是否进入编辑、回车是否提交并回到只读显示；**状态行的「数值」是否跟着变**
    （跟着变 = 写回通道通了，不跟着变 = `onChanged` 没接上）。
15. **编辑态手感**：**Esc = 退出编辑且不写回**（文本还原），解析失败（空串 / 半截输入 / 中文）也**不写值**；
    **两级 Esc**（编辑中 Esc 退编辑、再按 Esc 关面板）是否符合预期。
16. **吞键是否生效**：编辑中按 `1/2/3`（游戏速度）、空格（暂停）、`WASD`（镜头）是否**不再触发游戏热键**；
    光标 / 选区 / 打字是否一切正常（吞键不该影响打字）。
17. 🔴 **吞键是否会漏 / 会卡死**（最需要盯的一种回归）：状态行的「计数」必须始终是 **0 或 1**；
    **编辑中直接点「关闭」或按 Esc 关面板 → 再打开面板 → 计数必须回到 0**；
    关掉面板后立刻按 `1/2/3` 与空格，热键必须恢复正常
    （计数若没归零 ⇒ 吞键补丁永久生效 ⇒ **整局键盘失灵**，只能重开游戏）。
18. **输入框外观**：底 / 文字 / **光标**（TMP 默认光标是深灰，代码里显式改成白）是否都看得见；
    初值「点我输入中文abc」是否完整显示；超长文本是否被裁剪在框内（`CharacterLimit = 16`）。

批 3-3 专项（自检区行数从 5 变 7，**先看这几条**）：

19. **新增的滑条行（第 2 行「滑条」）**：是 0–1 的小数滑条，读数是否形如 `0.35`（两位小数）；
    拖动是否有反应、**与上面那行「生长进度」的观感是否一致**（两者应同宽同高，只是范围/格式不同）。
20. **数值框行的单位不再重复**（本轮修掉的那个 bug）：应是 `[<][50][>][%]` ——
    框内**只有数字**，`%` 只出现在最右那一列；若框内出现「50%」且右边还有一个 `%` ⇒ 修错了。
21. **`下 拉`行（新控件，重点）**：header 是否显示当前选项（初值应是「选项3」）+ 右侧箭头；
    **箭头是否显示** —— 代码用的是 `▼ / ▲`（依据是同作者另一个 Mod 的成熟用例）；
    若实机是空白方块，告诉我，我把 `DropdownField.Style` 的 `ArrowClosedLabel/ArrowOpenLabel`
    换成 ASCII 的 `v / ^`（一处改动）。
22. **点开下拉**：列表是否出现在 header **正下方**、宽度与 header 对齐、盖在面板之上、
    **6 个选项是否都在**（「选项1 … 选项6」）、当前项文字是否比其它项亮；
    点某一项 → header 文字是否立刻跟着换、**状态行的「下拉」是否同步变成同一项**
    （同步 = 写回通道通了）、列表是否自动收起。
23. **点外拦截**：列表展开时点面板别处（比如「生长进度」滑条）—— 应当**只收起列表、不动滑条**
    （这就是那个几乎不可见的点击承接层在起作用）；**不该出现"既收列表又拖了滑条"**。
24. **两级 Esc（下拉的）**：列表展开时按 Esc → 只收列表、**面板不关**；再按 Esc → 关面板。
25. **浮层挂载点是否真的"浮"起来**：面板内容很多（现在就有 8 行），列表展开时
    **有没有被面板边界裁掉**、有没有把窗口撑高；列表底边接近窗口底边时，是否**自动翻到 header 上方**。
26. **下拉列表的"已知未验项"**：滚动（`ScrollRect` + 最多显示 8 项）当前**不会被触发**
    （只有 6 个选项）⇒ 这条**本轮不用管**，等真有一长串选项时再专门验。
27. **别忘**：本条是"用法范例"，以后每加一个控件/参数类型都先在这里见人 ——
    若你觉得这七行的顺序/命名不像"范例"，直接说，改它比改文档便宜。

## 5. 待拍板 / 未决

- **是否推送仍未定**：`origin/main` 已到 `3211342`（此前的批 3 提交已推送），
  本次批 3-2 的提交尚在本地位列（等用户说推再推，仓库纪律：不擅自 push）。
  ⚠️ 用户 2026-09-13 明确要求：**一次调试不要每小步都提交** —— 改完并验证通过后再提交，
  中间探索性改动先不提交；那 5 个滑条调试提交已按用户要求压成 1 个。
- ✅ **「批 3-2 尚缺实机入口」已解决**，且**口径在批 3-3 变化**（用户 2026-09-13 拍板）：
  这块区**从"临时物"变成了长期保留的「控件自检 · 用法范例」** ——
  用户原话"自检区留着！当做后续界面使用的典范"。所以：
  · 它现在**走真的 `ParameterRowFactory` 分派**（滑条/数值框/勾选/下拉四行都是真参数行，
    值只落在面板自己的字段上），既是活范例，也是新控件的现场验证台；
  · 原先计划里那份"批 3-3 挂载后整块删干净"的删除清单**作废**（`ConfigPanel.cs` 里的注释也已改写）；
  · 仍**不删** `InsertBeforeButtonRow`（参数行与自检行共用的规则）。
  · 唯一保留的"临时味"是它写在 `ConfigPanel` 里、写入只落面板字段 —— 这是刻意的：
    它**不许**有任何游戏副作用，否则就不再是"安全的自检台"。
- 测试区**没走**的两条路（留档，免得以后重走）：① 独立测试面板 + 把入口挂到复制人身上 ——
  复制人没有 `IManageGrowingStates`、今天没有「修改配置」按钮，要为临时内容新开一条**永久**注入路径；
  ② 把自检行做成真 `Parameter` + 走 `ParameterRowFactory` 分派 —— 当初怕"在测试名义下把 B8 的形状定死"，
  而批 3-3 里 B8 真的设计成型了，所以这条**已经走了**（现在它是"用真分派验证真分派"，反而更值）。
- ❓**新增待拍板**：下拉**列表项**要不要 hover 底色？现在的做法是只把**当前选中项的文字**提亮，
  不做 hover —— 因为本 Mod 色板里**没有"比中背景再亮一档的暗色"**（`Background` 之上直接跳到浅色档），
  加色是你的决定权（色板纪律：加色需批准）。要加就请给个角色色，我按角色用，不加就用现状。
- **滑条来源已定案**（批 2b 执行中拍板，已写进 plan.md §3.1）：**纯代码自建 `KSlider`**，不用 `KNumberInputField`；
  若实机上滑条手感/外观不满意，可换的余地是"克隆场景里现存的原版滑条实例"（需要先有带滑条界面的建筑被选中）。
- 按钮图标现取原版现存 sprite **`action_switch_toggle`**（`ComplexFabricator.cs:243` 在用）；实机看效果后若要换，改一处字符串，并落进 `Assets/README.md`。
- `Patches/.gitkeep`、`UI/.gitkeep` 已被真实文件取代，是否删除待定（无害）。
- `CHANGELOG.md` 是否随工程建立：**仍未决**（未获批准，勿擅建）。
- M3 生成物初始化补全的触发方式：生成时自动 / 点开时按需（plan.md ❓1）。
- M4 创造建筑套件屏蔽清单（plan.md ❓4）。
