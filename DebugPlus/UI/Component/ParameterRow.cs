using DebugPlus.Operations;
using TMPro;
using UnityEngine;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 一行参数（批 2b · A4 的交互侧）：标签 + 滑杆 + 读数。
    ///
    /// 挂在本行的 GameObject 上，只负责"把滑杆的动作用到 Parameter 上，再把当前值写成读数"；
    /// 值的读写、单位换算全部在 Parameter 里，本类**不直接调用任何游戏 API**。
    ///
    /// 用普通 MonoBehaviour 而不是 KMonoBehaviour：本行不需要订阅、事件系统等框架能力
    /// （对比 ConfigButton 因为要 Subscribe 才必须是 KMonoBehaviour）。
    /// </summary>
    public class ParameterRow : MonoBehaviour
    {
        private Parameter parameter;
        private KSlider slider;
        private TextMeshProUGUI valueText;

        public Parameter Parameter
        {
            get { return parameter; }
        }

        /// <summary>
        /// 绑定一个参数。注意顺序：**先设范围与初值，最后才订阅** ——
        /// 否则设初值本身就会触发一次 onValueChanged 把值写回游戏（无意义的多余写入）。
        /// </summary>
        public void Bind(Parameter parameter, KSlider slider, TextMeshProUGUI valueText)
        {
            this.parameter = parameter;
            this.slider = slider;
            this.valueText = valueText;

            slider.minValue = parameter.Min;
            slider.maxValue = parameter.Max;
            slider.wholeNumbers = parameter.WholeNumbers;
            slider.value = Mathf.Clamp(parameter.GetValue(), parameter.Min, parameter.Max);
            slider.onValueChanged.AddListener(OnSliderChanged);

            Refresh();
        }

        private void OnSliderChanged(float value)
        {
            if (parameter != null)
            {
                parameter.SetValue(value); // 拖动过程中每帧写值：纯写值、零时间依赖
            }
            Refresh();
        }

        /// <summary>把当前滑杆值写进读数标签。</summary>
        public void Refresh()
        {
            if (valueText != null && parameter != null && slider != null)
            {
                valueText.text = parameter.Display(slider.value);
            }
        }
    }
}
