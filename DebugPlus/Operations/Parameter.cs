using System.Collections.Generic;
using UnityEngine;

namespace DebugPlus.Operations
{
    /// <summary>
    /// 一个「可调参数」的抽象基类（批 2b · A4 起；**批 3-3 扩成家族**）。
    ///
    /// 职责边界：本家族只描述**怎么读、怎么写、范围与显示**，完全不涉及界面。
    /// 界面侧由 <c>UI.Component.ParameterRowFactory</c> 按**参数的具体类型分派**到对应控件，
    /// 再由 <c>UI.Component.ParameterRow</c> 把语义接上。这样加参数只需
    /// "新增一个子类 + 往注册表登记"，不动界面代码。
    ///
    /// 家族（批 3-3 定案。四种控件 = 三种参数类型 + 数值参数的两种显示形态）：
    /// ```
    /// Parameter                     本类：Target / Label
    ///   ├── NumericParameter        浮点数值（Control 决定显示成滑条还是数值框）
    ///   ├── ToggleParameter         布尔（显示成勾选方块）
    ///   └── ChoiceParameter         从若干选项里选一个（显示成下拉）
    /// ```
    /// 为什么按类型拆而不是"一个类 + 一个 Kind 枚举 + float 通吃"：
    /// 布尔与选项**没有"范围"也没有"单位"**，硬塞进 float 会逼出"1 表示勾上"这种约定，
    /// 读写两句都要靠注释提醒，是最容易写错的一类代码。
    ///
    /// 实例与目标实体**一一绑定**（构造时解析目标组件并持有），所以读/写方法不带参数。
    /// 纯写值、零时间依赖（plan.md §二.2）：任何实现都不得调用时间/调度器 API。
    ///
    /// ⚠️ 新增参数类型时**必须同时在 <c>ParameterRowFactory</c> 的分派里加一支**：
    /// 工厂对认不出的类型会建一行"暂不支持"占位并打警告（不崩、不白屏），
    /// 那是兜底，不是设计意图。
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

        /// <summary>参数类型名（仅供工厂打"暂不支持"日志时用；默认取真实类型名）。</summary>
        public virtual string TypeName
        {
            get { return GetType().Name; }
        }
    }

    /// <summary>
    /// 数值参数的**显示形态**（批 3-3 用户拍板：由参数自己声明，不由工厂按范围猜）。
    /// 两者读写的是同一个 float，只是"怎么让用户改"不同 —— 规则显式、可预测，
    /// 避免出现"为什么这一行长得不一样"要靠推理才能解释的情况。
    /// </summary>
    public enum NumericControl
    {
        /// <summary>滑条（默认）：连续拖动、所见即所得，适合"调个大概"的量。</summary>
        Slider = 0,

        /// <summary>数值框（步进键 + 可输入）：精确到个位、要能直接敲数字的量用它。</summary>
        Number = 1
    }

    /// <summary>
    /// 浮点数值参数。单位与范围由参数自己定，界面只照它显示。
    /// </summary>
    public abstract class NumericParameter : Parameter
    {
        protected NumericParameter(GameObject target)
            : base(target)
        {
        }

        /// <summary>单位后缀（如百分比参数为 "%"；无单位返回空串）。</summary>
        public abstract string Unit { get; }

        public abstract float Min { get; }

        public abstract float Max { get; }

        /// <summary>是否只取整数刻度（百分比参数为 true）。</summary>
        public virtual bool WholeNumbers
        {
            get { return false; }
        }

        /// <summary>
        /// 显示形态（滑条 / 数值框）。默认滑条 —— 已有参数（生长进度）的观感因此**一点不变**。
        /// </summary>
        public virtual NumericControl Control
        {
            get { return NumericControl.Slider; }
        }

        /// <summary>读当前值，单位与 Min/Max 一致。</summary>
        public abstract float GetValue();

        /// <summary>写值，单位与 Min/Max 一致；实现内部自行换算成游戏 API 需要的单位。</summary>
        public abstract void SetValue(float value);

        /// <summary>
        /// **数值本身**的显示形式，**不含单位**（默认保留一位小数）。
        /// 🔴 子类要改显示规则就覆写**本方法**，不要去覆写 <see cref="Display"/> —— 原因：
        /// 「数值框」控件的单位是**独立的一列**（<c>NumberField</c> 的 `unit` 列），
        /// 它必须拿到纯数字；而「滑条」的读数是**一个文本列**，要自带单位。
        /// 由 <see cref="Display"/> = <see cref="Format"/> + <see cref="Unit"/> 统一保证两者口径一致，
        /// 否则会出现「框里显示 50%、旁边单位列又写一个 %」这种拼两遍的错。
        /// </summary>
        public virtual string Format(float value)
        {
            return value.ToString("0.#");
        }

        /// <summary>**带单位**的显示（滑条读数用）：基类实现就是 <see cref="Format"/> + <see cref="Unit"/>。</summary>
        public virtual string Display(float value)
        {
            return Format(value) + Unit;
        }
    }

    /// <summary>
    /// 布尔参数（显示成勾选方块）。例：动物是否驯服、M4 创造建筑是否屏蔽。
    /// </summary>
    public abstract class ToggleParameter : Parameter
    {
        protected ToggleParameter(GameObject target)
            : base(target)
        {
        }

        public abstract bool GetValue();

        public abstract void SetValue(bool value);

        /// <summary>勾选状态的文字形式（默认"是 / 否"，取自本项目自己的 STRINGS）。</summary>
        public virtual string Display(bool value)
        {
            return (string)(value ? STRINGS.UI.DEBUGPLUS.VALUE_YES : STRINGS.UI.DEBUGPLUS.VALUE_NO);
        }
    }

    /// <summary>
    /// 选项参数（显示成下拉）：从若干字符串选项里选一个。
    /// 读写的是**下标**而不是字符串 —— 显示文本可以随语言变，下标不会，
    /// 把"翻译过的名字"当键去写游戏是最容易埋下的一种错。
    /// </summary>
    public abstract class ChoiceParameter : Parameter
    {
        protected ChoiceParameter(GameObject target)
            : base(target)
        {
        }

        /// <summary>选项文本列表（顺序即下标顺序；界面只读它）。</summary>
        public abstract IList<string> Options { get; }

        /// <summary>读当前选中的下标。</summary>
        public abstract int GetIndex();

        /// <summary>写选中的下标（实现内部自行换算成游戏 API 要的形式，如元素 ID / 变异类型）。</summary>
        public abstract void SetIndex(int index);

        /// <summary>把下标变成显示文本（越界返回空串，由界面兜底显示"（无选项）"）。</summary>
        public virtual string DisplayIndex(int index)
        {
            IList<string> options = Options;
            if (options == null || index < 0 || index >= options.Count)
            {
                return string.Empty;
            }
            return options[index] ?? string.Empty;
        }
    }
}
