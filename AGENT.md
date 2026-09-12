# agent.md — DebugPlus · 调试增强 协作者须知

> 面向 AI 协作者与人类开发者。**先读 plan.md（设计唯一事实源），再读本文件（协作纪律）**。
> 会话开始时：读 **plan.md（设计）→ NEXT_STEPS.md（当前施工单/待办）→ 本文件（纪律）**；
> 查 dsh 任务看板是否有本工程任务（按看板纪律认领）。

## 0. 铁律（2026 用户明示，置顶，不可违反）

1. **思考必须使用中文**——所有分析、推理、注释、汇报一律用中文表达。
2. **每次修改必须先给方案！每个修改对应一个同意**——任何文件/代码的创建、修改、删除
   都必须先列明该项修改的具体方案，**用户逐项批准后才可执行**；未被批准的项目不得动手，
   不合并、不"顺手"、不捎带。
3. 方案先行（既有铁律）：plan.md 是唯一事实来源（单一定稿文档）；plan.md 之外的设计变更
   先提案、经用户拍板后同步修订 plan.md 与相关文档（LICENSE/NOTICE/agent.md 等）。

## 1. 项目一句话

补全缺氧（ONI）**缺失的 Debug 模式能力**的自研集合式 Mod（可持续追加）；**零第三方
依赖、零上游代码**（只用游戏自带 0Harmony + 游戏程序集，不搬运任何 Mod 源码，也不
做 SandboxTools 的替代版）。

**组合定位（2026 定稿）**：玩家安装 **Debug Button + SandboxTools + 本 Mod** = 完整
创造模式；本 Mod 专职补前两者加起来仍做不到的能力——**"暂停状态下无法完成的非暂停
操作"**（动物生长、植物成长、植物变异等）。

- **时间中立（铁律）**：不碰 `Time.timeScale` / `Time.deltaTime` / 任何调度器，不给模块
  补 tick。需要"时间继续走"才成立的操作**不做**（如"让间歇泉立即喷一次"）。
- **不重复既有 Mod**：Debug Button 管顶栏、SandboxTools 管沙盒侧（分类清除、生成器
  分类等），这些领域一律不碰、不重写、不顶替。
- **目标 UI 区域**：实体详情屏用户菜单的"修改配置"按钮 + 自建模态弹窗（不做
  DebugButton 式顶栏、也不做右下沙盒工具行）。

## 2. 锁定事实（勿改、勿重问）

| 项 | 锁定值 |
|---|---|
| 显示名 / staticID | `DebugPlus · 调试增强` / `Weik.DP.DebugPlus`（发布后不可改） |
| 技术底座 | 纯 Harmony + **零第三方依赖、零上游代码**（不内嵌/不引用/不搬运任何 Mod 源码；仅用游戏自带 0Harmony + 游戏程序集） |
| 组合定位 | Debug Button + SandboxTools + 本 Mod = 完整创造模式；本 Mod 只补前两者做不到的（**暂停态操作**） |
| 时间中立 | 不碰 timeScale / deltaTime / 调度器；需时间推进才成立的操作不做（铁律） |
| 语言 | 全中文（注释、UI、文档）；游戏内文本走 LocString |
| 目标框架 | .NET Framework 4.8 / AnyCPU（无空格）/ 通配符编译（`**/*.cs` 排除 obj） |
| 入口 | `DebugPlusMod : KMod.UserMod2`（每 DLL 唯一、非 abstract；OnLoad(Harmony)） |
| 本地化类名 | 必须叫 `STRINGS`（类名不同破坏 LocString 键路径）；翻译体系按 M5 建 |
| yaml | `mod_info.yaml`：APIVersion 2、UTF-8 无 BOM、minimumSupportedBuild 720697 |
| 许可 | 只有本工程自己的 MIT；**不含上游声明段**（零上游代码 → 无衍生义务；NOTICE 不声明衍生、不作致谢） |
| 美术 | 复用游戏自带图标或自绘（不采用他人仓库资源） |
| 底线 | 禁止分发解包游戏源码；只挂公开钩点；私有访问走可空 Detour+兜底 |

## 3. 构建与部署

- 环境检查：`C:\Users\魏锴\.agents\skills\oni-mod-dev\scripts\check-env.ps1`
- 编译+部署：`build.ps1 -ProjectRoot "F:\ONI_ModDev\ONI_ModCode\Debug Plus"`（Release 默认）
- 部署目标：`%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\Debug Plus\`
  （workspace 外 → 大概率触发沙箱拒绝 → 对同一条命令申请一次提权重试）
- 日志：`%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\player.log`（前缀 `[DebugPlus]`）
- 远程仓库：`https://github.com/w00kab/DebugPlus`（`origin`，分支 `main`）。仓库根 = 工程根。
  ⚠️ 推送需认证 → 受限沙箱下 git 凭证管理器会失败（`sh.exe: couldn't create signal pipe`），
  此时需在放宽策略下重跑同一条 `git push`（勿改用其它方式绕过凭据）。
  ⚠️ 本机 hosts 把 github 全系域名指向 `127.0.0.1`（`#S302`）→ 浏览器打不开网页版，但命令行 git 可连。
- 真机验证由用户启动游戏执行；编译/部署自验后必须汇报"做了什么/如何验证/风险/下一步"

## 4. 目录与命名（plan.md §四）

结构：`Patches/`（补丁类）、`UI/`（用户菜单按钮、模态弹窗、参数行工厂）、`Ops/`（暂停态操作定义与动作集）、
`Spawner/`（生成增强）、`Assets/`（图标说明）、根 `STRINGS.cs`。
命名：补丁类 `Xxx_目标_Patch`；自研 `DpXxxTool`/`DpXxxPanel`/`DpXxxOp`/`DpXxxRow`。
**禁止保留任何上游类名**（零上游代码 → 不搬运、不派生、不保留上游痕迹）。

## 5. 参考区（**仅思路参考，禁止搬运代码**）

| 内容 | 位置 |
|---|---|
| SandboxTools 参考资料 | `F:\ONI_ModDev\ONI_ModCode\【参考代码】sandTool`（原文路径 `【参考代码】ONIMods…` 在本机**不存在**） |
| 顶栏对照（不采用其领域） | `[参考代码] DebugButton` |
| 用户菜单按钮 / 模态弹窗范式参考 | `F:\ONI_ModDev\ONI_ModCode\Applied Logistics Network`（`ALN_MaterialRequester.SetCraftingControl` 的 `RefreshUserMenu` 模式） |
| 游戏 API 解包源码（只读，禁分发） | `F:\ONI_ModDev\ONI_ModCode\缺氧本体代码\Assembly-CSharp`（游戏本体逻辑） |
| 游戏框架程序集源码（只读，禁分发） | `F:\ONI_ModDev\ONI_ModCode\缺氧本体代码\Assembly-CSharp-firstpass`（**2026-09-13 由用户反编译**：`KScreen` / `KMonoBehaviour` / `KScreenManager` / `GameScreenManager` / `Util` / `KSlider` 等 Klei UI 框架类型全在这里，UI 施工**必须先查这里**，勿凭记忆写） |
| 游戏程序集真源（编译引用） | `F:\...\OxygenNotIncluded_Data\Managed\` |

> 教训（P1）：反编译产物可能含迭代器状态机残留（如 `PPatchTools.<DoReplaceMethodCalls>d__11`）
> 无法编译，只能对照。**本 Mod 一律只读参考、不内嵌**。

## 6. 会话工作流

1) 读 plan.md + agent.md；2) 查 dsh 任务看板（认领纪律）；3) API 不明先读本体源码
（`缺氧本体代码\Assembly-CSharp`），不凭空猜；4) 任何改动先按 §0 规则 2 列方案等批准；
5) **判断铁律**：任何新功能先问"是否需要时间推进才成立"——需要则不做（时间中立）；
6) 自验（编译/部署）；7) 中文汇报（做了什么 / 如何验证 / 风险 / 下一步），游戏内验证交用户。

## 7. 未定事项（勿擅动）

- ~~git 仓库是否建立~~：**已建立**（2026-09-13，`https://github.com/w00kab/DebugPlus`，Public，分支 `main`）；
  `CHANGELOG.md` 是否建立**仍未决**（勿擅建）
- 首版目标游戏版本/DLC 范围（❓2）；M3 生成物初始化补全的触发方式（生成时自动 / 点开时按需，❓1）
- M4 创造建筑套件：屏蔽清单待盘点 + 用户勾选（❓4）
- plan.md §七 的 4 条实机验证点（植物/间歇泉刷新、`SpawnAdult` 安全性、非建筑实体用户菜单）未验证前不得当成已定事实

## 8. 当前阶段指针（进度以 NEXT_STEPS.md 为准）

- P0 工程骨架：✅ 已游戏内加载验证（player.log 见 `[DebugPlus] … 已加载（P0 空骨架）`）
- **2026 定位重定稿**：撤销"SandboxTools 基底"，改**零上游代码 + 暂停态操作**主线；
  文档已同步（plan.md 重定稿 + 本文件）。
- ✅ **去上游化清理已完成**（用户批准项 A：整体删除、不重写）：已删除
  `Patches/SandboxToolsPatches.cs`、`UI/DestroyParameterMenu.cs`、`Tools/FilteredDestroyTool.cs`、
  `Tools/DestroyFilter.cs`、`SandboxToolsStrings.cs` 共 5 个文件——逐块比对确认其全部内容
  （分类清除工具、生成器额外分类、AETN 即时建造补铁）与被参考的上游原文一一对应，重写即重复其
  能力（plan.md 二.1），故**不设替代文件**。同时 `DebugPlusMod.cs` 去掉上游字符串注册与 PLib 说明，
  `NOTICE` / `LICENSE` / `Assets\README.md` 改写为独立自研口径（无上游声明段）。
  工程内现有源文件仅 `DebugPlusMod.cs` / `STRINGS.cs` / `Properties\AssemblyInfo.cs` 三个。
- 新 P1 剩余内容：打通"用户菜单按钮 → 模态弹窗 → 参数行"最小链，并落地**植物生长进度**
  （暂停下拉动即生效）；首个真实补丁落在 `Patches/`（命名 `Xxx_目标_Patch`）
- ✅ **批 2a（M1 最小链外壳）已实现并编译部署**（2026-09-13，待用户实机验证）：
  `Patches/UserMenu_AppendToScreen_Patch.cs`（Prefix 注入）、`UI/DpConfigButton.cs`（实体身上的用户菜单按钮）、
  `UI/DpConfigPanel.cs`（自建 `KModalScreen` 弹窗）、`STRINGS.cs` + `DebugPlusMod.cs`（本 Mod 自己的
  LocString 注册）。**关键结论已沉淀进 plan.md §3.1 的"自建模态屏的框架依据"块**
  （`KScreen.Activate()` 自足、`KMonoBehaviour` 运行时 `AddComponent` 即初始化、`Deactivate()` 会销毁实例、
  `pause` 默认 `true` 必须显式关掉、全局 `Action` 枚举会遮蔽 `System.Action`）。
- ⏳ **批 2b（参数行 + 植物生长进度）尚未批准**；本地 git 仓库已初始化并提交批 1 后的干净基线
  （`4aaf9e4`，分支 `main`），**远程未推送**。
