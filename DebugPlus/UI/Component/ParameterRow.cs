using DebugPlus.Operations;
using TMPro;
using UnityEngine;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 一行参数（批 2b · A4 的语义侧）：标签 + <see cref="SliderField"/> + 把滑条动作用到 <see cref="Parameter"/> 上。
    ///
    /// 职责边界（2026-09-13 按用户拍板拆分）：**UI 的搭建与交互全在 SliderField**，
    /// 本类只做"值怎么读写、读数怎么写"这层语义 —— 读写的换算全在 Parameter 里，
    /// 本类**不直接调用任何游戏 API**。
    ///
    /// 用普通 MonoBehaviour 而不是 KMonoBehaviour：本行不需要订阅、事件系统等框架能力
    /// （对比 ConfigButton 因为要 Subscribe 才必须是 KMonoBehaviour）。
    /// </summary>
    public class ParameterRow : MonoBehaviour
    {
        private Parameter parameter;
        private SliderField sliderField;
        private TextMeshProUGUI labelText;

        public Parameter Parameter
        {
            get { return parameter; }
        }

        /// <summary>
        /// 绑定一个参数。注意顺序：**先设范围与初值，最后才接上 onChanged** ——
        /// 否则设初值本身就会触发一次回调，把从游戏读出来的现状当成用户操作写回去。
        /// </summary>
        public void Bind(Parameter parameter, SliderField sliderField, TextMeshProUGUI labelText)
        {
            this.parameter = parameter;
            this.sliderField = sliderField;
            this.labelText = labelText;

            if (labelText != null)
            {
                labelText.text = parameter.Label;
            }

            sliderField.SetRange(parameter.Min, parameter.Max, parameter.WholeNumbers,
                Mathf.Clamp(parameter.GetValue(), parameter.Min, parameter.Max));
            sliderField.SetFormatter(parameter.Display); // 单位与小数位的显示规则由 Parameter 决定
            sliderField.onChanged = OnValueChanged;
            sliderField.AttachListener(); // ★ 最后才挂监听：前面设的初值不算用户操作
        }

        private void OnValueChanged(float value)
        {
            if (parameter != null)
            {
                parameter.SetValue(value); // 拖动过程中每帧写值：纯写值、零时间依赖
            }
        }
    }
}
