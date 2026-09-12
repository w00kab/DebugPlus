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
  - ⚠️ **批 2a 的代码尚未提交**（工作区有 4 个改动文件 + 3 个新增文件，等用户指示）。
- 已部署：`%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\Debug Plus\`
  （批 2a 产物 `DebugPlus.dll` **11776 B**，2026-09-13 02:04；批 1 时为 4608 B）。
- ✅ **批 2a · M1 最小链外壳：已实现、编译、部署完成** → **⏸ 待用户实机验证**（见 §4 第 1–4 条）。
- ⏳ **批 2b（A4–A6 + A7b）尚未获批准**：仍是"已出方案、等点头"，**一行代码都还没写**。

## 1. 批 2 施工单：M1 最小链 + 植物生长进度

出工条件（plan.md §五 P1 出口）：**暂停状态下点植物 → 改配置 → 弹窗 → 拉动生长进度 → 当场变化**。

### 批 2a · 壳 —— ✅ 已完成（出口：游戏里能看见"按钮 + 弹窗"，能开能关）

| # | 文件（新建） | 内容 | 状态 |
|---|---|---|---|
| A1 | `Patches/UserMenu_AppendToScreen_Patch.cs` | `[HarmonyPatch(typeof(UserMenu), nameof(UserMenu.AppendToScreen), typeof(GameObject), typeof(UserMenuScreen))]` 的 **Prefix**：命中判定 → `DpConfigButton.EnsureOn(go)`。**必须是 Prefix**：事件在方法体内触发，Postfix 时本次按钮已交给屏幕 | ✅ |
| A2 | `UI/DpConfigButton.cs` | 实体身上的 `KMonoBehaviour`：`Subscribe<DpConfigButton>(493375141, 静态 IntraObjectHandler)` → `Game.Instance.userMenu.AddButton(gameObject, new ButtonInfo(图标, "修改配置", 点击, Action.NumActions, null, null, null, tooltip, true), 20f)`；`EnsureOn` = `GetComponent ?? AddComponent` + `InitializeComponent()`（幂等兜底，保证框架 `obj` 已赋值）+ `SubscribeOnce()`（`subscribed` 标志防重复订阅）；点击 → `DpConfigPanel.OpenFor(gameObject)`；**不做任何序列化**（读档后由 A1 重新挂上，**不影响存档**） | ✅ |
| A3 | `UI/DpConfigPanel.cs` | **自建**（不克隆预制体）`KModalScreen`：`new GameObject` + `AddComponent`（Awake → `OnPrefabInit` 建遮罩与内容区）→ `KScreenManager.AddExistingChild(ssOverlayCanvas, go)` → `Activate()`；内容区 = 标题 / 目标名 / 参数行位 / 关闭按钮；`pause = false`（时间中立）；已有实例只抬到最上层（不叠加） | ✅ |
| A7 | `STRINGS.cs` + `DebugPlusMod.cs` | 4 条独立中文 LocString（按钮 / tooltip / 面板标题 / 关闭 + 占位说明 + 无名兜底）；入口补 `Localization.RegisterForTranslation(typeof(STRINGS))`（本 Mod 自己的文本，与已删的上游那份无关） | ✅ |
| A8 | `plan.md` / `AGENT.md` / 本文件 | plan.md §3.1 新增**自建模态屏的框架依据**块 + 模态弹窗行改写 + §四 M1 落地细节；AGENT.md §5 增"框架程序集源码"行 + §8 进度；本文件重写 | ✅ |

**当前判定方式**：批 2a 暂时直接在 `UserMenu_AppendToScreen_Patch.IsConfigurable` 里认 `Growing`；批 2b 的 A5 会把它换成注册表查询（判定与能力同源）。

### 批 2b · 能力（出口：暂停下拖滑杆，植物当场变）—— ⏳ 待批准

| # | 文件（新建） | 内容 |
|---|---|---|
| A4 | `UI/DpParamRow.cs`、`UI/DpRowFactory.cs` | 行基类（标签 / 取值 / 写值 / 范围 / 单位 / 格式化）+ 参数行构建。**开工前先定滑杆来源**：原版那些 `SliderValue` 预制体是 `[SerializeField]` 引用（mod 取不到），要么克隆场景里一个现存实例，要么纯代码自建 `KSlider` + `KNumberInputField`（须先读 `Assembly-CSharp-firstpass\KSlider.cs`） |
| A5 | `Ops/DpOpRegistry.cs` | `实体特征 → 参数行定义列表` 注册表，初版只登记"植物生长进度"；A1 的命中判定改为查询它（不在注册表里的实体连按钮都不出现） |
| A6 | `Ops/DpGrowthOp.cs` | 读 `Growing.PercentGrown()` / 写 `Growing.OverrideMaturityLevel(percent)`。⚠️ **参数是 0–1 的比例，不是 0–100**（`Growing.cs:54-57`：`SetValue(GetMax() * percent)`）；UI 显百分比时自行换算。**纯写值、零时间依赖**（plan.md §二.2） |
| A7b | `STRINGS.cs` | 行标签与单位文案（"生长进度"、`%`） |

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

## 3. 必须留存的教训

1. **不可把游戏 `STRINGS` 的 LocString 字段直接赋给本 Mod 字段**：`LocString` 是引用类型（class），赋值后
   共享同一对象，`Localization.RegisterForTranslation` → `LocString.CreateLocStringKeys` 会对树内每个 LocString
   调 `SetKey` 改写键，从而**污染游戏自身的字符串键**（表现为部分翻译丢失）。一律使用**独立的中文默认值字符串**。
2. 日志一律 `UnityEngine.Debug.Log`，前缀 `[DebugPlus]`；不引入任何日志框架。
3. 反编译产物可能含迭代器状态机残留（无法编译），只能对照阅读、**绝不内嵌**。
4. **命名冲突**：缺氧有全局枚举 `Action`（以及高频名 `Debug` 等），写库类型时优先全限定（`System.Action`），别只靠 `using`。
5. **反编译产物已扩到框架程序集**（`缺氧本体代码\Assembly-CSharp-firstpass`）：凡涉及 UI / 屏幕 / 组件生命周期的施工，**先读它再写**，不要再凭记忆判断（它直接决定"能不能自建"这类问题）。

## 4. ⏸ 待用户实机验证（未验证前不得当成事实）

批 2a 专项（本轮就要看）：

1. 暂停状态下选中**植物**（刺花 / 小麦等），用户菜单里是否出现「修改配置」按钮；**图标是否正常**（不是空白方块）。
2. 点按钮 → 是否弹出居中面板；**面板外的点击是否被拦住**（不能操作下面界面）。
3. `Esc` 与面板「关闭」按钮是否都能关闭；**重复点按钮是否不会叠出第二个面板**。
4. 打开 / 关闭面板前后，**游戏速度与暂停状态是否原样不变**（`pause = false` 的验证点）。

plan.md §七 原有 4 条（留待 2b 及之后）：

5. 暂停下 `Growing.OverrideMaturityLevel` 后，植物外观/状态项是否**立即**刷新（还是需一次解暂停才换图）。
6. 暂停下 `Geyser.AddModification` 后，间歇泉描述/喷发参数是否立即刷新。
7. `BabyMonitor.Instance.SpawnAdult()` 在沙盒/本 Mod 生成出的实体上调用是否安全。
8. 用户菜单按钮在**非建筑实体**（植物/动物/间歇泉）上是否正常显示。

## 5. 待拍板 / 未决

- **批 2a 的代码是否提交（并推送）**：当前 4 改 3 增未入库，等用户指示（仓库纪律：等用户说"阶段完成"再提交）。
- **批 2b（A4–A6 + A7b）等用户批准**；其中 **A4 的滑杆来源**（克隆场景现存 `SliderValue` 实例 vs 纯代码自建 `KSlider`）开工前先定。
- 按钮图标现取原版现存 sprite **`action_switch_toggle`**（`ComplexFabricator.cs:243` 在用）；实机看效果后若要换，改一处字符串，并落进 `Assets/README.md`。
- `Patches/.gitkeep`、`UI/.gitkeep` 已被真实文件取代，是否删除待定（无害）。
- `CHANGELOG.md` 是否随工程建立：**仍未决**（未获批准，勿擅建）。
- M3 生成物初始化补全的触发方式：生成时自动 / 点开时按需（plan.md ❓1）。
- M4 创造建筑套件屏蔽清单（plan.md ❓4）。
