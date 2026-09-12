# DebugPlus · 调试增强 — 项目设计文档

> 本文档是项目的**唯一事实来源**（单一定稿文档）。每次新会话先读本文件。
> 内容分工：设计决策写这里（稳定、低频更新）；会话进度/待办另维护 NEXT_STEPS.md（高频）。
> 依据：oni-mod-dev / oni-ui 技能，以及 2026 立项调研（参考代码与本体的代码研读）。
> 重大决策沿革（含**已作废决定**，保留以记录过程）：①总定位 = 补全缺氧缺失的 Debug 模式能力（集合式，可持续加）；②目标 UI 锁定右下沙盒工具区、不做 DebugButton 式顶栏；③代码形态定为以 PeterHan SandboxTools（MIT）为基底改造（用户 2026 拍板）；④2026 追加：自研**创造建筑套件**（创造发电机 + 各种创造输出泵），替代并屏蔽原版创造建筑——核心差异 = 本套件输出元素可带**温度与病菌**（原版缺失项）；⑤2026 P1 曾拍板"与上游同法源码内嵌 PLib"，P1 执行时**实测作废**：内嵌（vendoring）的 PLib 引入数百编译错误（含反编译产物迭代器残留），用户拍板"只要必须的！不行自己写！"；⑥2026 P1 终定：**零 PLib、零第三方运行时依赖**——上游 Mod 文件仅作逻辑蓝图，全部功能用游戏原生 API 自写，PLib 不引入、不发布；⑦2026 P1 图标定案：**直接复用游戏原版删除工具图标**（`destroy`），不自绘、不程序化；⑧2026 **撤销③**：改为**零上游代码**——不搬运 SandboxTools 任何代码，**且不再定位为 SandboxTools 的替代版**（借思路、不搬代码；NOTICE 不声明衍生、不作致谢）；⑨2026 **目标定稿**：玩家安装 **Debug Button + SandboxTools + 本 Mod** 三者组合出"完整的创造模式"；本 Mod 专职补**前两者加起来仍做不到的能力——"暂停状态下无法完成的非暂停操作"**（动物生长、植物成长、植物变异等）；⑩2026 去上游化清理**执行完成**：P1 旧"基底"产物 5 个文件**整体删除、不重写**——逐块比对确认其全部内容（分类清除工具、生成器额外分类、AETN 即时建造补铁）都与 SandboxTools 原文一一对应，重写即重复其能力（§二.1）。

## 一、项目概述

| 项目信息 | 内容 |
|---|---|
| Mod 名称 | DebugPlus · 调试增强 |
| staticID | `Weik.DP.DebugPlus`（发布后不可改，全局唯一） |
| 适用游戏 | 缺氧 Oxygen Not Included（含 DLC 需在立项时定版本目标） |
| 核心定位 | **补全缺氧 Debug 模式缺失能力的自研工具集合**；当前主线 = **暂停态操作**——让玩家在暂停下完成原本只能"解暂停跑一会儿"才能达成的状态变更 |
| 组合定位 | 玩家装 **Debug Button + SandboxTools + 本 Mod** = 完整创造模式；本 Mod 只补前两者做不到的部分 |
| 开发语言 | C#（.NET Framework 4.8）+ Harmony 2.x |
| 依赖 | **零第三方、零上游代码**：不内嵌/不引用/不发布任何第三方库，也不搬运任何 Mod 的源码；只用游戏自带 0Harmony + 游戏程序集 |
| 代码基线 | **无上游代码**。技术参考仅限**思路与 API 用法**（SandboxTools / Debug Button 等公开 Mod 的做法参考），不复制代码、不构成衍生 |
| 目标 UI 区域 | 实体详情屏**用户菜单按钮** + **本 Mod 自建模态弹窗**（自建的模态屏克隆原版 `ConfirmDialogScreen` 预制体） |
| 明确不做 | ① 左上角顶栏按钮组（TopLeftControlScreen 区域——Debug Button 的领域）；② **重复 Debug Button 与 SandboxTools 已有能力**（顶栏 Debug 总控、分类清除工具、生成器额外分类等一律不碰）；③ 依赖"绕过时间冻结"实现的效果（见 §二.2） |

## 二、核心设计哲学

1. **不重复既有 Mod 的能力（第一原则）**：Debug Button 管顶栏 Debug 总控，SandboxTools 管沙盒侧（分类清除、生成器分类扩展），本 Mod 只做它们做不到的。凡属它们领域的功能，**不做、不重写、不顶替**。
2. **时间中立（不可违反）**：不修改 `Time.timeScale` / `Time.deltaTime` / 任何调度器（`SimAndRenderScheduler` / `StateMachineUpdater`），不给任何模块补 tick，不模拟"过了多少秒"。理由是这类做法会让**全地图**持续行为一起恢复运行，与"暂停"语义直接矛盾。
   - **判据**：一个操作若需要"游戏时间继续走"才能完成 → **本 Mod 不做**。据此，可做的是：① 写状态值（点一下当场变）；② 生成/替换实体；③ 触发**该实体自己的**既有流程/事件。不可做的是：④ 任何持续效应（"让间歇泉从现在开始持续喷"、"让植物自己慢慢长"）。
3. **走原版既有结构，不自建平行体系**：交互链每一环都踩原版已有入口——选中用 `SelectTool`（不改选择流程）、挂按钮用 `Game.Instance.userMenu.AddButton` + `GameHashes.RefreshUserMenu`（不改预制体，不需要自研目标选择工具）、弹窗用原版 `KModalScreen` 与 `ConfirmDialogScreen` 预制体、参数控件克隆原版预制体。**能克隆原版就不自绘，能用原版就不新造。**
4. **只挂公开钩点**：一律走原版公开类/方法/事件；私有成员访问视为例外，必须"可空 + try/catch 兜底"，patch 失败不阻塞其余功能。
5. **机制跟随缺氧本体代码**：实现前先对照 `F:\ONI_ModDev\ONI_ModCode\缺氧本体代码\Assembly-CSharp` 核实 API；首次编译以玩家实际安装的 `Assembly-CSharp.dll` 为准。
6. **合规底线**：美术资源**复用游戏自带图标或自绘**（不采用他人仓库资源）；**不分发解包的游戏源码**；因零上游代码，不存在上游许可义务（NOTICE/LICENSE 只需本工程自己的 MIT）。
7. **全中文**：代码注释、UI 文本、文档一律中文；游戏内文本走 LocString / 翻译表体系（M5）。

## 三、调研结论（已确认事实，勿重复排查）

> 调研对象：`缺氧本体代码`（解包原版源码）、`【参考代码】sandTool`（= PeterHan SandboxTools，**仅思路参考，不搬运**）、`[参考代码] DebugButton`（= Sgt-Imalas Debug Buttons，顶栏按钮，**不采用**）。

### 3.1 原版机制地图

| 主题 | 结论 |
|---|---|
| 沙盒工具行 | `ToolMenu.CreateSandBoxTools()` 建 12 个官方工具到 `sandboxTools`（ToolCollection 列表，每项含 icon/hotkey/toolName/tooltip） |
| 工具激活链 | 点图标 → `ChooseTool` 用 `toolName` **字符串匹配** `PlayerController.tools[]` 中工具的 `GameObject.name` → `ActivateTool`。→ 注册新工具 = 新建挂组件的 GameObject 塞进 tools 数组，GO 名 = 类名 |
| 工具行显隐 | `Game.SandboxModeActive` 变化（事件 -1948169901）→ `ToggleSandboxUI` 整行显隐 |
| 参数面板 | `SandboxToolParameterMenu` 单例 KScreen。工具激活时 `OnActivateTool` → `DisableParameters()` + 点亮自己需要的行。行 = `SelectorValue`（下拉 + 可父子下钻的 `SearchFilter` 分类 + 搜索框）或 `SliderValue`（KSlider + KNumberInputField），运行时从 prefab 克隆 |
| 参数持久化 | `SandboxSettings`，键形如 `SandboxTools.SelectedEntity` / `BrushSize` 等 |
| 实体生成器 | `SandboxSpawnerTool`：读取下拉所选实体 → 分支：复制人走 `SpawnMinion`、有 Building 组件走 `BuildingDef.Build` 秒建、其余 `KInstantiate`。自带吸管键 `SandboxCopyElement`（点格复制**预制体名**，不做后续操作） |
| ⚠️ 生成器关键缺陷 | `ConfigureEntitySelector` 的候选列表**只收录命中内置分类的实体**（食物/生物/蛋/植物/种子/装备/彗星/工业品/矿石/瓶装液/罐装气…）→ **建筑、家具、装饰、遗迹、喷泉、工艺品等大量实体原版选不到、搜不到**。**本 Mod 不修这条**（属 SandboxTools 领域，见 §二.1 与 §四 M3）；本 Mod 关心的是它的下游：生成出来的实体**缺失初始化**（§3.2） |
| 选择管线 | 玩家选中实体走 `SelectTool`（`InterfaceTool.GetObjectUnderCursor<T>`），选中结果在 `SelectTool.Instance.selected`（`KSelectable`），**公开字段**，原版 DevTool 系列全用它 |
| 用户菜单按钮 | 实体详情屏第二面板的按钮区 = `Game.Instance.userMenu`（`UserMenuScreen : KIconButtonMenu`）。加按钮：`Subscribe((int)GameHashes.RefreshUserMenu, handler)` → 在 handler 里 `Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(...), sort_order)`；改状态后 `Game.Instance.userMenu.Refresh(gameObject)` 立即刷新文字。原版 `BuildingEnabledButton` 同款模式 |
| 模态弹窗 | `ConfirmDialogScreen : KModalScreen`，预制体在 `ScreenPrefabs.Instance.ConfirmDialogScreen`；`PopupConfirmDialog(text, on_confirm, on_cancel, configurable_text, on_configurable_clicked, title_text, confirm_text, cancel_text, image_sprite)`——**只有文字 + 三个按钮位，没有输入控件**；另有 `InfoDialogScreen`（自带私有 `contentContainer` + `AddUI<T>` / `AddSpacer` 等加内容入口）。本 Mod **两者都不克隆**，改为**自建 `KModalScreen` 子类**（可行性依据见下方框架事实块：`Activate()` 自足） |
| 参考价值（不采用） | DebugButton 在 `TopLeftControlScreen.OnActivate` 克隆 `sandboxToggle` 做左上按钮组、`IRender200ms` 周期刷三态——本 Mod **不复制**（那是它的领域，见 §一"明确不做"） |

> **用户菜单按钮注入链（2026 实测，源码逐行可查）**——本 Mod 唯一交互入口的实现依据：
> - `GameHashes.RefreshUserMenu = 493375141`（`GameHashes.cs:113`）。
> - `UserMenu.AppendToScreen(GameObject go, UserMenuScreen screen)`（`UserMenu.cs:37`）：`buttons.Clear()` → **`go.Trigger(493375141, null)`** → 按 `sort_order` 排序 → `screen.AddButtons(...)`。
>   ⚠️ 该事件是**在被选中实体自己的 GameObject 上**触发的，**不是全局事件** → 订阅者必须是**实体身上的组件**（原版 `Clearable` / `HarvestDesignatable` / `BuildingEnabledButton` 等 60+ 组件同款写法）。因事件在方法体内触发，**挂载必须用 Prefix**（用 Postfix 时本次按钮已交给屏幕，要等下次刷新才出现）。
> - `UserMenu.AddButton(GameObject go, ButtonInfo button, float sort_order = 1f)`（`UserMenu.cs:16`）：内部把 `onClick` 包一层"回调 + `Game.Instance.Trigger(1980521255, go)`" → **点击后菜单自己重建，无需手动刷新**。
> - 刷新链：`Game.Instance.userMenu.Refresh(go)` = `Game.Instance.Trigger(1980521255, go)`（`UserMenu.cs:10`）→ `UserMenuScreen.OnUIRefresh` → `UserMenuScreen.Refresh(go)`（`UserMenuScreen.cs:75`，**带 `go == selected` 守卫**）→ `AppendToScreen`。
> - `UserMenuScreen` 的 `buttonInfos` / `slidersInfos` / `sliders` / `selected` 均**私有** → 不自建平行通道，只走"实体自带组件 + `UserMenu.AddButton`"这条原版正道。

> **自建模态屏的框架依据（2026 反编译 `Assembly-CSharp-firstpass` 逐行核对）**——M1 弹窗不做预制体克隆的依据：
> - **运行时组件生命周期**：`KMonoBehaviour.Awake()`（`KMonoBehaviour.cs:35`）→ `InitializeComponent()`（:45，**public 且由 `isInitialized` 守卫，可安全重复调用**）→ `OnPrefabInit()`（:63）；`Start()`（:129）→ `Spawn()`（:141，`isSpawned` 守卫）→ `OnSpawn()`（:158）。
>   ⇒ **运行时 `AddComponent` 即完成框架初始化**（Awake 当场触发），**不需要预制体**；`Subscribe` 依赖的 `obj` 也在 `InitializeComponent` 内赋值（:56 / :253-255）。
> - `KScreen.Activate()`（`KScreen.cs:276-282`）：`SetActive(true)` → `KScreenManager.Instance.PushScreen(this)` → `OnActivate()` → `isActive = true`，**自足、不依赖预制体**；`KScreenManager.AddExistingChild(parent, go)`（`KScreenManager.cs:253-261`）= `SetParent(…, false)` + 同步 `layer`。
> - `KScreen.Deactivate()`（`KScreen.cs:290-303`）：`OnDeactivate()` → `PopScreen` → **`Destroy(gameObject)`** ⇒ 关闭即销毁，**不存在"复用同一实例"**，只能"已有实例就不再叠第二个"。
> - `KModalScreen.OnPrefabInit()`（`KModalScreen.cs:9-30`）自建全屏半透明遮罩（`Color32(0,0,0,160)` + `raycastTarget = true`）并置位 `ConsumeMouseScroll` / `activateOnSpawn`；`OnCmpEnable` / `OnCmpDisable` 负责禁用与恢复 `CameraController.DisableUserCameraControl`（:44-72）。
> - **按键与模态**：`KScreenManager.OnKeyDown`（`KScreenManager.cs:179-199`）自栈顶向下派发直到 `e.Consumed`；`KModalScreen.OnKeyDown`（`KModalScreen.cs:123-139`）消费 `Action.Escape` / 右键 → `Deactivate()` ⇒ **Esc 关闭与"必须先关掉才能做别的操作"都是原版机制**，本 Mod 不自己处理按键。`KScreenManager.Update()`（:161-176）遇到 `IsModal()` 的屏后停止向更下层传播 `ScreenUpdate`。
> - ⚠️ **`KModalScreen.pause` 默认 `true`**（`KModalScreen.cs:152`）：打开会 `SpeedControlScreen.Pause(false, false)`、关闭会 `Unpause(false)`——**会改掉玩家自己按下的暂停状态，与 §二.2 时间中立铁律冲突 ⇒ 本 Mod 显式 `pause = false`**。
> - ⚠️ **`Action` 名称冲突**：缺氧自带全局枚举 `Action`（`Action.Escape` / `Action.NumActions`），**命名空间成员优先于 `using System;` 导入**，故本 Mod 必须写 `System.Action`（原版源码通篇写 `System.Action(...)` 即此原因）。

### 3.2 关键事件/句柄速查

| 事件 ID | 含义 |
|---|---|
| 1798162660 | 叠层(Overlay)切换 |
| 1557339983 | 即时建造等 Debug 状态刷新 |
| -1948169901 | 沙盒模式开关 → 工具行显隐 |
| `GameHashes.RefreshUserMenu` | 用户菜单重建（挂自定义按钮的时机） |
| `GameHashes.CopySettings` = -905833192 | 原版"复制设置"工具（可选支持：把配置复制到同类实体） |
| `GameHashes.NewGameSpawn` = 1119167081 | 世界生成时的初始化广播（`AgeMonitor.RandomizeAge` / `Growing` 随机成熟度都挂它；**沙盒生成器/本 Mod 操作不会触发它**） |

### 3.3 暂停态操作的技术事实（本 Mod 核心依据，均为原版公开入口）

| 目标 | 原版公开入口 | 暂停下是否成立 |
|---|---|---|
| 植物生长进度/成熟度 | `Growing.OverrideMaturityLevel(percent)`、`Growing.ClampGrowthToHarvest()`、`Growing.PercentGrown()`、`Growing.ResetGrowth()` | ✅ 直接写 amount 值，当场生效 |
| 植物变异 | `MutantPlant`（DLC 变异配置） | ✅ 直接配置 |
| 动物年龄 | `AgeMonitor.Instance.age`（`AmountInstance`，公开字段）、`AgeMonitor.Def.adultThreshold` | ✅ 值可当场写 |
| 幼体→成体 | `BabyMonitor.Instance.SpawnAdult()`（公开方法，内部把各 amount **按比例**迁移到成体，`BabyMonitor.cs:96-104`），或 `BabyMonitor.Def.configureAdultOnMaturation` 钩子 | ✅ 当场替换实体，不需等待轮询 |
| 野性/驯服 | `WildnessMonitor.Instance.DebugTame()`（公开方法，内部状态机跳转 + 标签/图标/效果一站处理）、`Db.Get().Amounts.Wildness.Lookup(go)` | ✅ 直接写值/调用 |
| 血量 | `Health.hitPoints`（amount 的 get/set） | ✅ 直接写值 |
| 间歇泉/火山参数 | `Geyser.AddModification(Geyser.GeyserModification)`（公开方法，内部 `UpdateModifier()` → `ApplyConfigurationEmissionValues()` 重刷 emitter）；字段含 `massPerCycleModifier` / `iterationDurationModifier` / `iterationPercentageModifier` / `temperatureModifier` / `maxPressureModifier` / `modifyElement`+`newElement` | ✅ 改参数当场生效 |
| ⚠️ 间歇泉"立即喷发一次" | — | ❌ **做不到**：喷发是时长以秒计的元素排放过程，由 sim 在时间推进中逐 tick 执行；暂停时 sim 不推进。除非"绕过时间冻结"（§二.2 禁止） |

### 3.4 风险与版本

- 缺氧本体代码快照含 DLC3（义体人）门槛，参考代码亦较新；正式编译前先跑技能环境检查锁定实际游戏版本。
- 禁止分发解包的游戏源码；本文档结论基于本地研读，不搬运原版代码原文。
- 私有成员访问全部走"可空 + 异常兜底"模式。
- **待实测项**：暂停状态下 `Geyser.AddModification` 后 emitter 数值是否立即刷新、野生↔驯服切换是否引发 UI/标签异常、`SpawnAdult` 在沙盒生成实体上的调用是否安全——列为首期工具的实机验证点。

### 3.5 许可与合规结论（2026 重定稿）

| 对象 | 许可 | 结论 |
|---|---|---|
| SandboxTools **代码** | MIT（Copyright (c) 2024 Peter Han） | **不采用**。本 Mod 不搬运其任何代码，故无衍生义务；NOTICE 不声明衍生、不作致谢（用户 2026 拍板） |
| SandboxTools **图片** | CC BY-NC-SA 4.0 | 不采用；图标复用游戏原版或自绘 |
| PLib | MIT | 不引入（沿革⑤⑥结论不变） |
| 游戏解包源码 | 禁分发 | 只读参考，不搬运、不发布 |

## 四、模块框架

> 各模块只定义"能放什么、怎么挂"，具体功能条目经确认后填入扩展槽。

### M0 · 工程骨架
- `DebugPlus.sln` / `DebugPlus\DebugPlus.csproj`（.NET Framework 4.8、AnyCPU、通配符导入源码）
- `mod_info.yaml`：APIVersion 2、UTF-8 **无 BOM**、staticID `Weik.DP.DebugPlus`
- `DebugPlusMod.cs`（`UserMod2`，每 DLL 唯一、非 abstract）、`Properties/AssemblyInfo.cs`
- **`NOTICE` / `LICENSE`**：本工程自己的 MIT（无上游声明段）
- 目录：`Patches/`、`UI/`（弹窗与行工厂）、`Ops/`（暂停态操作定义与动作集）、`Spawner/`（生成物初始化补全）、`Assets/`、`STRINGS.cs`

### M1 · 实体配置弹窗框架层（UI）★ 本 Mod 门面
职责：把"点中实体 → 一个按钮 → 弹窗改配置"这条链搭起来，供所有操作模块复用。
- **用户菜单按钮注入**：对选中实体在 `RefreshUserMenu` 时机加"修改配置"按钮；不改任何预制体、不改选择流程
  - **落地（批 2a 已实现）**：`Patches/UserMenu_AppendToScreen_Patch.cs` 在 `UserMenu.AppendToScreen` 的 **Prefix** 里判定目标并调 `DpConfigButton.EnsureOn(go)`；`UI/DpConfigButton.cs` 是**挂在实体身上的 `KMonoBehaviour`**，用 `Subscribe<DpConfigButton>(493375141, 静态 IntraObjectHandler)`（照 `Clearable.cs:17/222` 原版写法）→ `Game.Instance.userMenu.AddButton(gameObject, new ButtonInfo(...), 20f)`。组件**不做任何序列化**，读档后由 A1 重新挂上，**不影响存档**；订阅由 `subscribed` 标志保证只做一次（防框架回调与直接调用重复）
- **模态弹窗**（**自建，不克隆预制体**——依据见 §3.1 框架事实块）
  - **落地（批 2a 已实现）**：`UI/DpConfigPanel.cs : KModalScreen`；`new GameObject` + `AddComponent`（Awake → `OnPrefabInit` 生成遮罩与内容区）→ `KScreenManager.AddExistingChild(GameScreenManager.Instance.ssOverlayCanvas, go)` → `Activate()`
  - 内容区自建：标题 / 目标名 / 参数行位 / 关闭按钮；布局遵守 oni-ui 规则（根 VLG → 行 HLG 一层嵌套、`childForceExpandHeight = false`、TMP 显式赋 `Localization.FontAsset`、纯代码用 `Button` 且 `transition = None`、装饰层 `raycastTarget = false`）
  - **时间中立**：`pause = false`（原版默认 `true` 会改掉玩家自己按下的暂停）；关闭走 `Deactivate()`（原版会销毁实例，故"全局仅一个"实现为"已有实例就不再叠第二个"）
- **参数行工厂**：克隆原版控件预制体——滑杆/数字输入（对应原版 `SliderValue` 的 `KSlider` + `KNumberInputField`）、勾选（`MultiToggle`）、下拉（原版选择器行）；每行声明"取值/写值/范围/单位"
- **实体能力模板注册表**：`实体特征 → 参数行定义列表`（选中实体后按特征匹配，生成对应行；无匹配则不显示按钮或提示"该实体无可调参数"）
- 扩展槽：自研参数行类型（如"元素选择 + 温度 + 病菌"复合行）、二级面板（后续创造建筑套件用）

### M2 · 暂停态操作层（Ops）★ 本 Mod 核心
职责：承载"暂停下可执行"的具体操作实现。每条操作 = 一个自包含的动作（写值 / 生成替换 / 触发该实体自身流程），**不得依赖时间推进**。
- 操作基座：`实体特征 → 操作集合`，与 M1 的模板注册表同源
- 首批操作（按 §3.3 已验证的公开入口实现）：
  1. **植物生长进度**（`Growing`）——首个落地项，作为"暂停态操作"命题的最小验证
  2. 植物变异（`MutantPlant`，DLC 门控）
  3. 动物年龄（`AgeMonitor`）+ 立即成体（`BabyMonitor.Instance.SpawnAdult()`）
  4. 动物野性/驯服（`WildnessMonitor`）+ 血量（`Health`）
  5. 间歇泉/火山参数（`Geyser.AddModification`）——喷发量、喷发期、冷却期、温度、元素、病菌；**不含"立即喷发"**
- 约束：**每次操作只影响被操作的那一个实体**；不得触发全局效果
- 扩展槽：新种类的操作在此注册；M3 生成物初始化补全与 M4 创造建筑套件的"放置时带温度/病菌"也走此层的"生成替换/写值"形态

### M3 · 生成物初始化补全层（Spawner）
职责：补原版生成器"**生成出来 ≠ 按配置成型**"的缺陷——沙盒生成器只做 `KInstantiate` / `BuildingDef.Build` / `SpawnMinion`，**不触发 `GameHashes.NewGameSpawn`**（§3.2），于是世界生成时才做的初始化（`Growing` 随机成熟度、`AgeMonitor.RandomizeAge`、间歇泉 `Geyser.OnSpawn` 的参数套用等）在沙盒生成物上一律缺失。
- **生成即成型**：给沙盒生成出来的实体补上缺失的初始化，然后交由 M1/M2 的**同一套模板与操作**接管（点面板或点实体，写值逻辑只有一份）
- 覆盖对象：间歇泉/火山（套用参数）、植物（成熟度/变异）、动物（年龄/驯养）
- ⚠️ **本层不做"生成器分类扩展 / 实体候选列表重建"**——那是 **SandboxTools 的自带能力**（其 `SandboxToolParameterMenu.ConfigureEntitySelector` 补丁新增漫游者/遗迹道具/工艺品/间歇泉火山等分类）；本 Mod 不重复（§一"明确不做"、§二.1）
- 扩展槽：自研生成行为（如"生成即带指定温度/病菌"，与 M4 合流）

### M4 · 创造建筑套件（原计划保留，转入暂停态操作语境）
- 自研"创造发电机 + 各种创造输出泵"，**替代并屏蔽原版创造建筑**；核心差异 = 输出元素可带**温度与病菌**（原版缺失项）
- 装置形态（已定）：**接管道输出**（对齐原版创造建筑的管道式）；固体泵形态待 P2 盘点原版创造建筑后对齐
- 可调参数（面板行）：输出元素、温度、病菌种类 + 数量；屏蔽清单待盘点原版创造建筑后**列给用户勾选确认**

### M5 · 公共层
- 文本（LocString 体系 / 翻译表）、日志与容错、图标（复用游戏自带 / 自绘）
- 与 Debug Button / SandboxTools 的**共存说明**（玩家向文档，非代码检测；三者领域不重叠，无需二选一）

### 命名规范

| 类型 | 示例 |
|---|---|
| Mod ID | `Weik.DP.DebugPlus` |
| Mod 入口 | `DebugPlusMod` |
| 补丁类 | `Xxx_目标_Patch`（如 `UserMenu_OnRefresh_Patch`） |
| 自研类型 | `DpXxxTool` / `DpXxxPanel` / `DpXxxOp` / `DpXxxRow` |
| 禁用 | 不保留任何上游类名（零上游代码） |

## 五、阶段路线

| 阶段 | 内容 | 出口条件 |
|---|---|---|
| P0 | M0 工程骨架 + 许可文件 + 环境校验 | 编译→部署→游戏内可加载（空 Mod）——**已完成** |
| P1（重定稿） | **工程清理（去上游化）+ 首个暂停态操作**：把 SandboxTools 衍生文件**整体删除、不重写**（分类清除工具、生成器额外分类、AETN 即时建造补铁——逐块比对确认全部对应上游原文）；打通 M1 最小链（用户菜单按钮 + 模态弹窗 + 一个滑杆行）并落地"植物生长进度" | 暂停状态下点植物 → 改配置 → 弹窗 → 拉动生长进度 → **当场**变化（无需解暂停） |
| P2 | M2 其余操作（动物年龄/成体/野性/血量、植物变异）+ M1 行工厂扩充（下拉/勾选/复合行） | 逐项在暂停下生效 |
| P3 | 间歇泉/火山参数（M2 第 5 项）+ M3 生成物初始化补全 | 参数当场生效；沙盒生成出来的间歇泉/植物点开即为"已按配置成型" |
| P4+ | M4 创造建筑套件（含屏蔽清单盘点与用户勾选） | 逐项验收 |
| 收尾 | 汉化核验、CHANGELOG、版本发布（version-workflow） | **仅当用户明示"阶段完成"才执行** |

## 六、决策记录

✅ **已定**（勿再问）：
1. 组合定位 = Debug Button + SandboxTools + 本 Mod = 完整创造模式；本 Mod 只补前两者做不到的（2026 定稿）。
2. 本 Mod 主线 = **暂停态操作**：让玩家在暂停下完成原本需"解暂停跑一会儿"的状态变更（动物生长、植物成长、植物变异等）。
3. 时间中立铁律：不碰 timeScale/deltaTime/调度器；需要时间推进才成立的操作**不做**（"立即喷发"属此类）。
4. **零上游代码**：不搬运 SandboxTools 任何代码、不再定位为其替代版、不做同装检测/二选一（撤销沿革③）。
5. 交互形态：详情屏**一个**"修改配置"按钮 → **模态弹窗**（克隆原版 `ConfirmDialogScreen` 预制体）→ 参数行**克隆原版控件预制体**。
6. 不重复 Debug Button（顶栏）与 SandboxTools（分类清除、生成器分类等）的既有能力。
7. 名称 = 显示名 `DebugPlus · 调试增强`、staticID `Weik.DP.DebugPlus`（发布后不可改）。
8. 技术底座 = 纯 Harmony + 零第三方依赖（沿革⑤⑥不变）。

❓ **仍待定**：
1. 沙盒生成物的初始化补全（M3）触发方式：生成时自动补，还是只在玩家点开时按需补？
2. 首版目标游戏版本 / 是否含 Bionic（DLC3）等内容兼容？
3. ~~是否初始化 git 仓库~~ → **已建立**（2026-09-13，`https://github.com/w00kab/DebugPlus`，Public，分支 `main`，仓库根 = 工程根）；`CHANGELOG.md` 是否随工程一起建立**仍未决**。（**NEXT_STEPS.md 已建立**并投入使用：施工单/待办归它，设计决策归本文件）
4. M4 创造建筑套件的屏蔽清单（待盘点原版创造建筑后列给用户勾选）。

## 七、待用户确认的实机验证点（P1 出口前）

1. 暂停下 `Growing.OverrideMaturityLevel` 后，植物外观/状态项是否立即刷新（还是需要一次解暂停才换图）。
2. 暂停下 `Geyser.AddModification` 后，间歇泉描述/喷发参数是否立即刷新。
3. `BabyMonitor.Instance.SpawnAdult()` 在沙盒/本 Mod 生成出的实体上调用是否安全（原版该路径由状态机内部触发）。
4. 用户菜单按钮在**非建筑实体**（植物/动物/间歇泉）上是否正常显示（原版该模式多用于建筑）。
