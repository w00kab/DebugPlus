using UnityEngine;

namespace DebugPlus.Operations
{
    /// <summary>
    /// 一个「可调参数」的抽象（批 2b · A4 的数据侧）。
    ///
    /// 职责边界：本类只描述**怎么读、怎么写、范围与显示**，完全不涉及界面；
    /// 界面侧由 `UI.ParameterRowFactory` 构建、`UI.ParameterRow` 绑定。这样以后加参数
    /// （温度、质量、间歇泉参数…）只需新增一个 Parameter 子类 + 往注册表登记，不动界面代码。
    ///
    /// 实例与目标实体**一一绑定**（构造时解析目标组件并持有），所以读/写方法不带参数。
    /// 纯写值、零时间依赖（plan.md §二.2）：任何实现都不得调用时间/调度器 API。
    /// </summary>
    public abstract class Parameter
    {
        protected Parameter(GameObject target)
        {
            Target = target;
        }

        /// <summary>参数所属的目标实体。</summary>
        public GameObject Target { get; private set; }

        /// <summary>行标签（中文，来自本项目自己的 STRINGS.UI.DEBUGPLUS）。</summary>
        public abstract string Label { get; }

        /// <summary>单位后缀（如百分比参数为 "%"；无单位返回空串）。</summary>
        public abstract string Unit { get; }

        public abstract float Min { get; }

        public abstract float Max { get; }

        /// <summary>滑杆是否只取整数刻度（百分比参数为 true）。</summary>
        public virtual bool WholeNumbers
        {
            get { return false; }
        }

        /// <summary>读当前值，单位与 Min/Max 一致。</summary>
        public abstract float GetValue();

        /// <summary>写值，单位与 Min/Max 一致；实现内部自行换算成游戏 API 需要的单位。</summary>
        public abstract void SetValue(float value);

        /// <summary>读数文本（默认保留一位小数）。</summary>
        public virtual string Display(float value)
        {
            return value.ToString("0.#") + Unit;
        }
    }
}
