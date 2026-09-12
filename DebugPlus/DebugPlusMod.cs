using HarmonyLib;
using KMod;
using UnityEngine;

namespace DebugPlus
{
    /// <summary>
    /// DebugPlus · 调试增强 — Mod 入口（每个 DLL 唯一 UserMod2，位于 KMod 命名空间；规则见 oni-mod-dev 技能）。
    /// 加载顺序：base.OnLoad(harmony) 内部即 harmony.PatchAll(this.assembly)
    /// （原版 KMod\UserMod2.cs:29），本 Mod 全部 [HarmonyPatch] 类由此一次性应用；
    /// 随后打印加载日志。
    /// 说明：本 Mod 零第三方依赖、零上游代码——不使用任何第三方框架库，因此不需要额外的
    /// 初始化链（没有框架引导、没有补丁管理器注册、没有版本检查）。
    /// 游戏内文本（LocString）在需要时由对应模块自行向 Localization 注册（M5）。
    /// </summary>
    public class DebugPlusMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[DebugPlus] DebugPlus · 调试增强 已加载");
        }
    }
}
