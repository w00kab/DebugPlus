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
        /// 绑定一个参数。**机制全在 <see cref="SliderField.Bind"/> 里**（含"先设范围初值 → 设格式化 →
        /// 最后挂监听"的顺序保证），本类只做一件事：把参数化的读写通道摊成三个委托交出去。
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

            sliderField.Bind(
                read: parameter.GetValue,
                write: parameter.SetValue,   // 拖动过程中每帧写值：纯写值、零时间依赖
                format: parameter.Display,
                min: parameter.Min,
                max: parameter.Max,
                wholeNumbers: parameter.WholeNumbers,
                initial: parameter.GetValue());
        }
    }
}
