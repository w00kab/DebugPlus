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
  （批 1 = 4608 B → 批 2a = 11776 B → 批 2b = 16896 B → **2b 修复版 = 16896 B**，
  2026-09-13 02:44；本地与部署目录 SHA256 一致 `151B93C62733FCB6…`）。
- ✅ **批 2a · M1 最小链外壳：已实现、编译、部署完成** → **⏸ 待用户实机验证**（见 §4 第 1–4 条）。
- ✅ **批 2b（A4–A6 + A7b）：已实现、编译、部署完成** → **⏸ 待用户实机验证**（见 §4 第 5–7 条）。
  **2b 的代码已提交**：`cd09869`（含 `UI/View`+`UI/Component` 拆分、`Ops`→`Operations`、去 `Dp` 前缀改名）。
- 🔧 **批 2b 实机首次运行即崩，已定位并修复（2026-09-13 02:44 重新部署，待复验）**：
  `NullReferenceException` 在 `ParameterRowFactory.CreateSlider` → `ConfigPanel.BuildParamRows`
  （点「修改配置」按钮时）。**定位手段**：`player.log` 栈带 IL 偏移 `[0x0010a]` → `ildasm` 反汇编本 Mod DLL →
  搜到 `IL_010a` 是 `ldloc.s handleRect` + `callvirt RectTransform::set_anchorMin` ⇒
  手柄的 `handleRect` 是 `null`。**根因**：`NewUIObject` 已挂过 `RectTransform`，
  建手柄时又 `handle.AddComponent<RectTransform>()` 一次 ⇒ **Unity 返回 `null`（不抛异常）**。
  修复：改为 `handle.GetComponent<RectTransform>()`（见 §3 教训 8/9）。改的是 `UI/Component/ParameterRowFactory.cs` 一处。

## 1. 批 2 施工单：M1 最小链 + 植物生长进度

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

### 批 2b · 能力（出口：暂停下拖滑杆，植物当场变）—— ✅ 已完成（出口待实机）

| # | 文件 | 内容 | 状态 |
|---|---|---|---|
| A4 | `UI/Component/ParameterRow.cs`、`UI/Component/ParameterRowFactory.cs` | 行绑定 + 纯代码建行（标签 TMP + `KSlider` + 读数 TMP）。**滑杆来源定案：纯代码自建 `KSlider`**——原版所有滑杆都来自预制体（`MultiSliderSideScreen.cs:34` `Util.KInstantiateUI(sliderPrefab…)`，`sliderPrefab` 是 `[SerializeField]`，mod 拿不到），原版没有"代码自建滑杆"先例；而 `Slider.handleRect` / `fillRect` 是可写公开属性 ⇒ 可自建。⚠️ `KSlider.Awake()` 第一句取 `handleRect.gameObject`（`KSlider.cs:40`）⇒ **先不激活、挂完 handleRect 再激活**。**不用 `KNumberInputField`**：`KInputField.inputField` 是 `[SerializeField] private`、`field` 只读（`KInputField.cs:10-16/105-106`）⇒ 数值改用 TMP 读数。`Bind` 顺序：先设范围/初值、**最后**订阅 `onValueChanged` | ✅ |
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

### 2.3 滑杆素材与生长状态（`Assembly-CSharp` / `firstpass`，2026-09-13 批 2b 核对）

- **原版所有滑杆行都来自预制体** ⇒ mod 只能自建：
  `MultiSliderSideScreen.cs:34` `Util.KInstantiateUI(this.sliderPrefab.gameObject, …)`、`:37` `component.GetReference<KSlider>("Slider")`；
  `sliderPrefab` 是屏预制体上的 `[SerializeField]` 引用，**mod 拿不到**；原版源码里**没有**"代码自建滑杆"的先例。
- **`KSlider` 可纯代码构造**：`KSlider : Slider`（`KSlider.cs:8`），需要的 `handleRect` / `fillRect` 是 `Slider` 的**可写公开属性**。
  ⚠️ 但 `KSlider.Awake()`（`KSlider.cs:31-41`）第一句 `base.handleRect.gameObject.GetComponent<ToolTip>()`
  ⇒ **handleRect 为空必 NRE** ⇒ 先让滑杆 GameObject **不激活**、挂完 `handleRect`/`fillRect` 再激活。
  另：`onDrag` / `onReleaseHandle` / `onPointerDown` / `onMove` 是 KSlider 自己的事件（`SliderSet.SetupSlider` 用的就是它们）；
  本 Mod 只需连续写值，故用基类 `Slider.onValueChanged`。
- **`KNumberInputField` 不可纯代码构造**：`KInputField.inputField` 为 `[SerializeField] private KInputTextField`，
  `field` 属性只读（`KInputField.cs:10-16 / 105-106`）⇒ 数值改用自建 TMP 读数标签。
- **原版"滑杆+数值+标签"样板**：`SliderSet.SetupSlider`（`SliderSet.cs:9-33`）、`SetTarget`（`:36-68`）、`SetValue`（`:96-121`）。
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
   2026-09-13 实机批 2b 崩溃的直接原因 —— `NewUIObject` 已挂过 RectTransform，建手柄时又 `AddComponent` 一次拿到 `null`，
   下一句 `handleRect.anchorMin = …` 才 NRE。**凡是"有没有 RectTransform"不确定的 GO，一律 `GetComponent<RectTransform>()`**。
   （ini-ui 规则 1 本来就写了这条，之前只在 ConfigPanel 里守住了，工厂里漏了 ⇒ 规则要在**每个新建 UI 的辅助方法**里落实。）
9. **定位 mod 自身 NRE 的正确姿势（本轮验证有效，以后照做）**：
   ① `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\player.log` 里的 IL2CPP/Mono 栈带 **IL 偏移**（如 `[0x0010a]`）；
   ② `ildasm /out=… /item:命名空间.类 本Mod的DebugPlus.dll` 反汇编自己（**不用任何第三方反编译器**）；
   ③ 在反汇编里搜 `IL_010a`，看它到底在调谁 —— 本次直接读到 `ldloc.s handleRect` + `callvirt RectTransform::set_anchorMin`
   ⇒ 一秒锁定 `handleRect` 为 null，免去"加日志 → 重编 → 再让用户复现"的多轮往返。
   注意：`Release`（`DebugType=pdbonly` + `optimize+`）的行号不可靠，**以 IL 偏移为准**。

## 4. ⏸ 待用户实机验证（未验证前不得当成事实）

批 2a 专项（本轮就要看）：

1. 暂停状态下选中**植物**（刺花 / 小麦等），用户菜单里是否出现「修改配置」按钮；**图标是否正常**（不是空白方块）。
2. 点按钮 → 是否弹出居中面板；**面板外的点击是否被拦住**（不能操作下面界面）。
3. `Esc` 与面板「关闭」按钮是否都能关闭；**重复点按钮是否不会叠出第二个面板**。
4. 打开 / 关闭面板前后，**游戏速度与暂停状态是否原样不变**（`pause = false` 的验证点）。

批 2b 专项（本轮新增，接在 2a 之后一起看）：

5. 面板里是否出现**「生长进度」一行**（标签 + 滑杆 + 右侧百分比读数），读数与植物当前进度是否对得上（可先看原版植物状态项里的成熟度百分比）。
6. **拖动滑杆**：读数是否跟着变；**植物当场变化**（外观换图 / 成熟度状态项变化），**无需解暂停**。
7. 滑杆**能拖、能点**（点滑杆空白处是否直接跳值）；**面板外的点击仍被拦住**；关掉面板后植物状态保持你拖到的值。

plan.md §七 原有 4 条（留待后续批次）：

8. 暂停下 `Growing.OverrideMaturityLevel` 后，植物外观/状态项是否**立即**刷新（还是需一次解暂停才换图）—— 与第 6 条重叠，一并看。
9. 暂停下 `Geyser.AddModification` 后，间歇泉描述/喷发参数是否立即刷新。
10. `BabyMonitor.Instance.SpawnAdult()` 在沙盒/本 Mod 生成出的实体上调用是否安全。
11. 用户菜单按钮在**非建筑实体**（植物/动物/间歇泉）上是否正常显示。

## 5. 待拍板 / 未决

- **是否推送仍未定**：本地 `main` 领先 `origin/main` **4 个提交**（`2a29d44` 批 2a、`cd09869` 批 2b + 改名、`8fcfced` 文档同步、`bdfce31` 2b 崩溃修复），
  等用户说推再推（仓库纪律：不擅自 push）。
- **滑杆来源已定案**（批 2b 执行中拍板，已写进 plan.md §3.1）：**纯代码自建 `KSlider`**，不用 `KNumberInputField`；
  若实机上滑杆手感/外观不满意，可换的余地是"克隆场景里现存的原版滑杆实例"（需要先有带滑杆界面的建筑被选中）。
- 按钮图标现取原版现存 sprite **`action_switch_toggle`**（`ComplexFabricator.cs:243` 在用）；实机看效果后若要换，改一处字符串，并落进 `Assets/README.md`。
- `Patches/.gitkeep`、`UI/.gitkeep` 已被真实文件取代，是否删除待定（无害）。
- `CHANGELOG.md` 是否随工程建立：**仍未决**（未获批准，勿擅建）。
- M3 生成物初始化补全的触发方式：生成时自动 / 点开时按需（plan.md ❓1）。
- M4 创造建筑套件屏蔽清单（plan.md ❓4）。
