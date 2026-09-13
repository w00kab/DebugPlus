using DebugPlus.Operations;
using UnityEngine;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 一行参数的**语义手柄**（批 2b · A4 起；批 3-3 变薄）。
    ///
    /// 为什么现在这么薄：批 3-3 把"按参数类型选控件"这件事交给了
    /// <see cref="ParameterRowFactory"/>，绑定各控件又是各控件自己的 <c>Bind</c> 管的，
    /// 所以本类只剩两件真正属于"行"的事：
    ///   ① 记住这一行绑的是哪个 <see cref="Parameter"/>（面板/未来的刷新逻辑要按行找到它）；
    ///   ② 提供一个统一的 <see cref="Refresh"/>，把**游戏侧现值**重读回控件 ——
    ///      调用方不必知道这一行下面装的是滑条、数值框、勾选还是下拉。
    ///
    /// 职责边界（用户 2026-09-13 拍板的拆分）：**UI 的搭建与交互全在控件类里**，
    /// 读写的换算全在 <see cref="Parameter"/> 里，本类**不直接调用任何游戏 API**。
    ///
    /// 用普通 MonoBehaviour 而不是 KMonoBehaviour：本行不需要订阅、事件系统等框架能力
    /// （对比 ConfigButton 因为要 Subscribe 才必须是 KMonoBehaviour）。
    /// </summary>
    public class ParameterRow : MonoBehaviour
    {
        private Parameter parameter;
        private System.Action refresh;

        /// <summary>本行绑定的参数（未绑定时为 null）。</summary>
        public Parameter Parameter
        {
            get { return parameter; }
        }

        /// <summary>
        /// 由 <see cref="ParameterRowFactory"/> 在绑定完成后调用：
        /// 记下参数与"把游戏侧现值同步回控件"的动作（各控件自己的 <c>Refresh</c>）。
        /// </summary>
        public void Initialize(Parameter parameter, System.Action refresh)
        {
            this.parameter = parameter;
            this.refresh = refresh;
        }

        /// <summary>
        /// 从参数侧重读并刷新本行的控件（当前值可能已被别处改动时用）。
        /// 不触发任何写回 —— 刷新不是用户操作。
        /// </summary>
        public void Refresh()
        {
            if (refresh != null)
            {
                refresh();
            }
        }
    }
}
