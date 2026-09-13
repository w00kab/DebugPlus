using DebugPlus.UI.Component;
using HarmonyLib;

namespace DebugPlus.Patches
{
    /// <summary>
    /// 编辑输入框时**吞掉游戏热键**（批 3-2 · B6）。
    ///
    /// # 为什么需要（原版只保护了一半）
    /// 原版自己就有一条"输入框聚焦就别处理热键"的规则：`CameraController.WithinInputField()`
    /// （CameraController.cs:527-540）查 `EventSystem.current.currentSelectedGameObject` 上有没有
    /// `KInputTextField` / `InputField`，命中就直接 return（:572 / :801）。
    /// **但它只被 CameraController 用** —— `SpeedControlScreen`（数字键改速度）、`ToolMenu`（工具快捷键）、
    /// `PlanScreen`、`OverlayMenu` 等热键消费者都没有这层保护；且本 Mod 的输入框是自建的原生
    /// `TMP_InputField`（不是 `KInputTextField`），连 CameraController 那条也认不出。
    /// ⇒ 打字时按键会**一边进输入框、一边触发游戏热键**（打 "3" 顺手改游戏速度），必须自己吞。
    ///
    /// # 为什么挂在 `KInputHandler.HandleEvent`（唯一的根入口）
    /// `KInputController.Dispatch()`（KInputController.cs:242-256）逐个事件调
    /// `this.inputHandler.HandleEvent(kinputEvent)` —— 全工程**只有这一处**调 `HandleEvent`
    /// （各子处理器走的是 `HandleKeyDown`/`HandleKeyUp`），所以它是按键进入游戏热键系统的**总闸门**：
    /// Prefix 返回 false 即整个派发链（含 `KScreenManager` 与各屏、各工具）都收不到这次按键。
    /// 另：`KInputEvent.Consumed` 是**公开可写**属性（KInputEvent.cs:19），置 true 语义上就是"本次事件已被消费"。
    ///
    /// # 口径：**只在"正在编辑"时吞**（用户 2026-09-13 拍板）
    /// 不按"面板展开"吞 —— Esc 关面板靠的是 `KScreenManager.OnKeyDown` 自栈顶派发到
    /// `KModalScreen.OnKeyDown`（KModalScreen.cs:123-139），它同样在这个根 handler 之下；
    /// 面板一展开就吞键会把 Esc 一起吞掉，直接打断已验收的"Esc 关面板"。
    /// 收窄到"编辑中"后手感是**两级 Esc**：编辑中按 Esc = TMP 自己退出编辑（并把文本还原，
    /// TMP_InputField.cs:4000-4003），再按一次 Esc 才关面板。
    ///
    /// # 为什么吞键不影响打字
    /// 输入框收字是 uGUI/TMP 直接读 Unity 输入（`Event.PopEvent`）完成的，**不经过 KInput 链**；
    /// 这里吞掉的只是"同一次按键又当成游戏热键消费一遍"。开关见 <see cref="TextField.IsEditing"/>。
    /// </summary>
    [HarmonyPatch(typeof(KInputHandler), nameof(KInputHandler.HandleEvent))]
    internal static class InputHandler_HandleEvent_Patch
    {
        private static bool Prefix(KInputEvent e)
        {
            if (!TextField.IsEditing)
            {
                return true; // 没在编辑：原样放行，游戏热键行为与不加补丁完全一致
            }
            if (e != null)
            {
                e.Consumed = true;
            }
            return false; // 拦截：本次按键不再进入任何热键处理器
        }
    }
}
