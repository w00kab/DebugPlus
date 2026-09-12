# DebugPlus · 调试增强

> 补全《缺氧》（Oxygen Not Included）缺失的 **Debug 模式能力**的自研集合式 Mod。

[![平台](https://img.shields.io/badge/ONI-Spaced_Out_%2B-blue)](https://www.klei.com/games/oxygen-not-included)
[![许可](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![状态](https://img.shields.io/badge/status-开发中-orange)](#-当前进度)

**⚠️ 当前处于早期开发阶段，尚无可用发布版**，进度见下方「当前进度」与 [`NEXT_STEPS.md`](NEXT_STEPS.md)。

---

## 📖 这是什么

缺氧自带的 Debug 模式功能有限。本 Mod 的目标不是重造轮子，而是**只补别人做不到的那一块**。

**组合定位（2026 定稿）**：

```
Debug Button  +  SandboxTools  +  DebugPlus  =  完整创造模式
   顶栏工具         沙盒侧工具        暂停态操作
```

- **Debug Button** 管顶栏工具 —— 本 Mod 不碰。
- **SandboxTools** 管沙盒侧（分类清除、生成器分类等）—— 本 Mod 不碰、不重写、不顶替。
- **本 Mod 专职**：前两者加起来仍然做不到的 **「暂停状态下无法完成的非暂停操作」**——
  动物生长、植物成长、植物变异等。这些操作在原版里必须让时间跑起来才能生效，
  本 Mod 让它们**在暂停下拉个滑条就当场生效**。

### 功能规划

| 模块 | 内容 | 状态 |
|---|---|---|
| M1 | 实体用户菜单「修改配置」按钮 + 模态参数弹窗 | 🔧 开发中 |
| M2 | 植物生长进度（暂停下即时生效） | 🔧 开发中 |
| M3 | 生成物初始化补全 | ⏳ 规划中 |
| M4 | 创造建筑套件 | ⏳ 规划中 |
| M5 | 多语言本地化体系 | ⏳ 规划中 |

完整设计与决策依据见 [`plan.md`](plan.md)。

---

## 🔒 开发铁律

这几条不是口号，是本工程的硬性边界：

- **时间中立**：不碰 `Time.timeScale` / `Time.deltaTime` / 任何调度器，不给模块补 tick。
  需要「时间继续走」才成立的操作**一律不做**（例如「让间歇泉立即喷一次」）。
- **零第三方依赖**：只用游戏自带的 Harmony（`0Harmony`）+ 游戏本体程序集，不引用任何外部 Mod 框架。
- **零上游代码**：不搬运、不派生、不保留任何第三方 Mod 的源码或上游类名。
  早期开发阶段的派生文件已于 2026 年**整体删除**，详见 [`NOTICE`](NOTICE)。
- **不重复既有 Mod**：别人已覆盖的领域不碰、不顶替。
- **不分发游戏源码**：只挂公开钩点，不随 Mod 分发任何解包的游戏程序集。
- **全中文**：注释、UI、文档一律中文；游戏内文本走 `LocString`。

---

## 🎮 环境要求

| 项 | 要求 |
|---|---|
| 游戏 | 《缺氧》Spaced Out!（`supportedContent: ALL`） |
| 最低构建号 | `720697` |
| Mod API 版本 | 2 |
| 目标框架 | .NET Framework 4.8 / AnyCPU |

---

## 📦 安装

**暂无发布版本。** 首个可用版本发布前，请直接从源码构建（见下节）。

发布后将从 [Releases](../../releases) 页面下载 `DebugPlus.dll`，放入：

```
%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\DebugPlus\
```

---

## 🔨 从源码构建

需要已安装《缺氧》本体（用于引用游戏程序集）与 .NET Framework 4.8 开发环境。

```powershell
git clone https://github.com/w00kab/DebugPlus.git
cd DebugPlus
dotnet build DebugPlus.sln -c Release
```

> 工程通过 `**/*.cs` 通配符编译（排除 `obj`），新增 `.cs` 文件无需改工程。引用均为游戏自带程序集且
> `Private=False`，不随 Mod 分发。

**⚠️ 必须先改引用路径**：`DebugPlus/DebugPlus.csproj` 中的游戏程序集 `HintPath` 是开发机上的
**绝对路径**（例如 `F:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\...`）。
如果你机器上的游戏装在别处，编译前请把 `<HintPath>` 批量改成本机的
`...\OxygenNotIncluded_Data\Managed\` 目录，或改用 `ONIManagedDir` 之类的 MSBuild 属性统一替换。

需要引用的游戏程序集：`0Harmony`、`Assembly-CSharp`、`Assembly-CSharp-firstpass`、
`UnityEngine`、`UnityEngine.CoreModule`、`UnityEngine.UI`、`UnityEngine.UIModule`、
`UnityEngine.InputLegacyModule`、`UnityEngine.TextRenderingModule`、`Unity.TextMeshPro`。

---

## 📂 仓库结构

```
DebugPlus/
├── DebugPlus/
│   ├── Patches/          # Harmony 补丁类（Xxx_目标_Patch）
│   ├── UI/
│   │   ├── View/         # 屏 / 面板（模态弹窗等）
│   │   └── Component/    # 控件与构建工厂（菜单按钮、参数行等）
│   ├── Operations/       # 暂停态操作定义与动作集
│   ├── Spawner/          # 生成增强
│   ├── Assets/           # 图标资源说明
│   ├── STRINGS.cs        # 本地化字符串（类名必须叫 STRINGS）
│   ├── DebugPlusMod.cs   # Mod 入口（KMod.UserMod2）
│   ├── mod.yaml          # 显示名与 staticID
│   └── mod_info.yaml     # 支持范围与版本
├── plan.md               # 设计唯一事实源
├── NEXT_STEPS.md         # 施工单与待办
├── AGENT.md              # 协作者须知（含 AI 协作纪律）
├── NOTICE                # 来源与第三方依赖声明
└── LICENSE               # MIT
```

---

## 🚧 当前进度

- ✅ **P0 工程骨架**：编译 → 部署 → 游戏内加载验证通过（空 Mod）。
- ✅ **去上游化清理**：删除全部上游衍生文件，工程内仅剩自研代码。
- 🔧 **P1**：打通「用户菜单按钮 → 模态弹窗 → 参数行」最小链，并落地植物生长进度。

详细施工项与待批准方案见 [`NEXT_STEPS.md`](NEXT_STEPS.md)。

---

## 📄 许可

本项目采用 [MIT 许可](LICENSE)。

本 Mod 为独立自研实现，不含任何第三方 Mod 的代码，不构成衍生作品；
运行期零外部依赖（仅游戏自带 `0Harmony` + 游戏本体程序集）。
详见 [`NOTICE`](NOTICE)。

《缺氧》（Oxygen Not Included）为 Klei Entertainment 的商标与版权作品，本项目与之无隶属关系。
