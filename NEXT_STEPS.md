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
  - 核查结论：工程内 `.cs` 仅剩 `DebugPlusMod.cs` / `STRINGS.cs` / `Properties/AssemblyInfo.cs`；
    本地 `bin` 与已部署 DLL 扫描 `SandboxTools/FilteredDestroyTool/DestroyFilter/DestroyParameterMenu/
    PeterHan/PLib/PUtil/SpriteRegistry` **全 0 命中**；`obj` 构建缓存 0 残留引用。
- 已部署：`%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\Debug Plus\`（`DebugPlus.dll` 4608 B）
- ⏳ **批 2 尚未获逐项批准**：下方 A1–A8 是"**已出方案、等点头**"状态，**一行代码都还没写**。

## 1. 批 2 施工单：M1 最小链 + 植物生长进度

出工条件（plan.md §五 P1 出口）：**暂停状态下点植物 → 改配置 → 弹窗 → 拉动生长进度 → 当场变化**。

### 批 2a · 壳（出口：游戏里能看见"按钮 + 弹窗"，能开能关）

| # | 文件（新建） | 内容 |
|---|---|---|
| A1 | `Patches/UserMenu_AppendToScreen_Patch.cs` | `[HarmonyPatch(typeof(UserMenu), nameof(UserMenu.AppendToScreen), typeof(GameObject), typeof(UserMenuScreen))]` 的 **Prefix**：实体命中注册表 → 确保其身上有 `DpConfigButton`（`GetComponent ?? AddComponent` + **直接 `Subscribe`**，不依赖 `OnSpawn`，因运行时加组件的生命周期不确定） |
| A2 | `UI/DpConfigButton.cs` | `KMonoBehaviour`；`Subscribe((int)GameHashes.RefreshUserMenu, …)` → `Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(图标名, "修改配置", OnClick, 快捷键), 20f)`；点击 → `DpConfigPanel.OpenFor(gameObject)`。**不做任何序列化**（组件纯为 UI 存在，读档后由 A1 重新挂上，**不影响存档**） |
| A3 | `UI/DpConfigPanel.cs` | `KModalScreen` 子类：克隆原版壳预制体 → 自建内容区（标题 + 行容器 + 关闭/确认）+ 遮罩 + Esc 关闭；全局仅一个实例（复用不叠加） |
| A7 | `STRINGS.cs` + `DebugPlusMod.cs` | `STRINGS` 填按钮/弹窗标题文案（**独立中文字符串**，见 §3 教训）；入口补回 `Localization.RegisterForTranslation(typeof(STRINGS))`（本 Mod 自己的文本，与已删的上游那份无关） |
| A8 | `plan.md` / `AGENT.md` / 本文件 | 记录 A1 实测钩点；plan.md §四 M1 补"落地细节"，勾选本文件进度 |

### 批 2b · 能力（出口：暂停下拖滑杆，植物当场变）

| # | 文件（新建） | 内容 |
|---|---|---|
| A4 | `UI/DpParamRow.cs`、`UI/DpRowFactory.cs` | 行基类（标签 / 取值 / 写值 / 范围 / 单位 / 格式化）+ **从原版 `SliderValue` 实例克隆滑杆行**（`KSlider` + `KNumberInputField`），**不改原版预制体** |
| A5 | `Ops/DpOpRegistry.cs` | `实体特征 → 参数行定义列表` 注册表，初版只登记"植物生长进度"；**A1 的命中判定与它同源**（不在注册表里的实体连按钮都不出现） |
| A6 | `Ops/DpGrowthOp.cs` | 读 `Growing` 现值 / 写 `Growing.OverrideMaturityLevel(percent)`（0–100%，带钳制）。**纯写值、零时间依赖**（plan.md §二.2） |
| A7b | `STRINGS.cs` | 行标签与单位文案（"生长进度"、`%`） |

### 每步自验（agent 侧）

1. `build.ps1 -ProjectRoot "F:\ONI_ModDev\ONI_ModCode\Debug Plus"`（部署目录在工作区外 → 首次大概率被沙箱拒绝 → 对**同一条命令**申请一次提权重试）
2. 扫描新 DLL：无上游标识符
3. 游戏内验证交用户（见 §4）

## 2. 已实测的原版钩点（2026 源码逐行核对，施工直接用，勿再凭记忆）

- `GameHashes.RefreshUserMenu = 493375141`（`GameHashes.cs:113`）。
- `UserMenu.AppendToScreen(GameObject go, UserMenuScreen screen)`（`UserMenu.cs:37`）：
  `buttons.Clear()` → sliders.Clear() → **`go.Trigger(493375141, null)`** → 按 `sort_order` 排序 →
  `screen.AddButtons(...)`。
  ⚠️ **该事件是在"被选中实体自己的 GameObject"上触发的，不是全局事件** → 订阅者必须是**实体身上的组件**
  （原版 `Clearable` / `HarvestDesignatable` / `BuildingEnabledButton` 等 60+ 组件同款写法）。
  ⚠️ 因事件在方法体内触发，**挂载必须用 Prefix**：Postfix 时本次按钮已交给屏幕，要等下次刷新才出现。
- `UserMenu.AddButton(GameObject go, KIconButtonMenu.ButtonInfo button, float sort_order = 1f)`（`UserMenu.cs:16`）：
  内部会把 `button.onClick` 包一层"回调 + `Game.Instance.Trigger(1980521255, go)`"，**所以点击后菜单自己重建**，
  无需手动刷新。
- 刷新入口链：`Game.Instance.userMenu.Refresh(go)` = `Game.Instance.Trigger(1980521255, go)`（`UserMenu.cs:10`）
  → `UserMenuScreen.OnUIRefresh` → `UserMenuScreen.Refresh(go)`（`UserMenuScreen.cs:75`，**带 `go == selected` 守卫**，
  非当前选中对象直接 return） → `AppendToScreen`。
- `UserMenuScreen` 的 `buttonInfos` / `slidersInfos` / `sliders` / `selected` 均**私有** → 不要试图从外部灌按钮，
  走"实体自带组件 + `AddButton`"这条原版正道。

## 3. 必须留存的教训（原记录文件已删除，勿再丢）

1. **不可把游戏 `STRINGS` 的 LocString 字段直接赋给本 Mod 字段**：`LocString` 是引用类型（class），赋值后
   共享同一对象，`Localization.RegisterForTranslation` 会对该对象 `SetKey` 改写，从而**污染游戏自身的字符串键**
   （表现为部分翻译丢失）。一律使用**独立的中文默认值字符串**。
2. 日志一律 `UnityEngine.Debug.Log`，前缀 `[DebugPlus]`；不引入任何日志框架。
3. 反编译产物可能含迭代器状态机残留（无法编译），只能对照阅读、**绝不内嵌**。

## 4. ⏸ 待用户实机验证（plan.md §七，未验证前不得当成事实）

1. 暂停下 `Growing.OverrideMaturityLevel` 后，植物外观/状态项是否**立即**刷新（还是需一次解暂停才换图）。
2. 暂停下 `Geyser.AddModification` 后，间歇泉描述/喷发参数是否立即刷新。
3. `BabyMonitor.Instance.SpawnAdult()` 在沙盒/本 Mod 生成出的实体上调用是否安全。
4. 用户菜单按钮在**非建筑实体**（植物/动物/间歇泉）上是否正常显示。

## 5. 待拍板 / 未决

- 批 2 的 A1–A8：**等用户批准**（可拆 2a / 2b，或一次做完）。
- `ButtonInfo` 的图标名（必须是原版现存 sprite 名）——施工时挑定后实报，并落进 `Assets/README.md`。
- git 仓库是否建立、`CHANGELOG.md` 是否随工程建立（plan.md ❓3；`NEXT_STEPS.md` 已由本文件落实）。
- M3 生成物初始化补全的触发方式：生成时自动 / 点开时按需（plan.md ❓1）。
- M4 创造建筑套件屏蔽清单（plan.md ❓4）。
