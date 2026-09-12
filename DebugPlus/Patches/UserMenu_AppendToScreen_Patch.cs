using DebugPlus.UI;
using HarmonyLib;
using UnityEngine;

namespace DebugPlus.Patches
{
    /// <summary>
    /// 用户菜单按钮注入点（M1 落地细节之一）。
    ///
    /// 原版证据 —— UserMenu.cs:37 AppendToScreen(GameObject go, UserMenuScreen screen)：
    ///   this.buttons.Clear(); → this.sliders.Clear(); → go.Trigger(493375141, null);
    ///   → 按 sort_order 升序排序 → screen.AddButtons(...)
    ///
    /// 两个关键结论：
    /// ① 该事件是在「被选中实体自己的 GameObject」上触发的（不是全局事件），
    ///    所以订阅者必须是**实体身上的组件**（原版 Clearable / HarvestDesignatable /
    ///    BuildingEnabledButton 等 60+ 组件同款写法）。
    /// ② 事件在方法体内触发，必须在它之前把组件挂好 —— 所以只能用 Prefix；
    ///    Postfix 时本次按钮已经交给屏幕，要等下一次刷新才出现。
    /// </summary>
    [HarmonyPatch(typeof(UserMenu), nameof(UserMenu.AppendToScreen), typeof(GameObject), typeof(UserMenuScreen))]
    internal static class UserMenu_AppendToScreen_Patch
    {
        private static void Prefix(GameObject go)
        {
            if (go == null || Game.Instance == null)
            {
                return;
            }
            if (!IsConfigurable(go))
            {
                return;
            }
            DpConfigButton.EnsureOn(go);
        }

        /// <summary>
        /// 批 2a 的临时命中判定：当前只有「植物生长进度」一项能力，因此直接认 Growing。
        /// 批 2b 的 A5（Ops/DpOpRegistry）会把它换成注册表查询 —— 判定与能力同源，
        /// 不在注册表里的实体连按钮都不会出现。
        /// </summary>
        private static bool IsConfigurable(GameObject go)
        {
            return go.GetComponent<Growing>() != null;
        }
    }
}
